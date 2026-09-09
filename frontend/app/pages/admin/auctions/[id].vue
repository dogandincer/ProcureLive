<script setup lang="ts">
import type {
  AuctionDetailDto,
  AuctionItemDto,
  BidComparisonDto,
  BidDto,
  CompanyDto,
  PagedResult,
} from "~/types/api";

const route = useRoute();
const auctionId = route.params.id as string;

const api = useApi();
const toast = useToast();
const { connectAndJoin, disconnect } = useAuctionHub();
const { isAdminRole } = useAuth();

const auction = ref<AuctionDetailDto | null>(null);
const companies = ref<CompanyDto[]>([]);
const loading = ref(true);
const now = ref(Date.now());
const liveLog = ref<string[]>([]);

const comparisons = reactive<Record<string, BidComparisonDto[]>>({});
const inviteCompanyId = ref("");
const newItem = reactive({ description: "", quantity: 1, unitOfMeasure: "Piece", targetPrice: null as number | null });
const extendMinutes = ref(5);

async function runAction(action: () => Promise<void>) {
  try {
    await action();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "İşlem gerçekleştirilemedi."));
  }
}

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Scheduled: "amber",
  Live: "emerald",
  Ended: "slate",
  Awarded: "indigo",
  Cancelled: "rose",
};

const workflowStep = computed(() => {
  if (!auction.value) return null;
  switch (auction.value.status) {
    case "Draft":
      return { step: 1, label: "Taslak: Kalem & Cari Davet Edin" };
    case "Scheduled":
      return { step: 2, label: "Yayınlandı: Başlangıç Bekleniyor" };
    case "Live":
      return { step: 3, label: "Canlı: Teklifler Toplanıyor" };
    default:
      return null;
  }
});

const unitOfMeasures = ["Piece", "Kilogram", "Gram", "Liter", "Meter", "SquareMeter", "CubicMeter", "Box", "Pallet", "Hour", "Service"];

const timeLeftLabel = computed(() => {
  if (!auction.value) return "";
  const diffMs = new Date(auction.value.endAt).getTime() - now.value;
  if (diffMs <= 0) return "Süre doldu";
  const totalSeconds = Math.floor(diffMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}dk ${seconds}sn`;
});

async function load() {
  loading.value = true;
  try {
    const [detail, companyPage] = await Promise.all([
      api.get<AuctionDetailDto>(`/api/auctions/${auctionId}`),
      api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100, companyType: "Supplier" }),
    ]);
    auction.value = detail;
    companies.value = companyPage.items;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "İhale yüklenemedi."));
  } finally {
    loading.value = false;
  }
}

async function addItem() {
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/items`, {
      productId: null,
      description: newItem.description,
      quantity: newItem.quantity,
      unitOfMeasure: newItem.unitOfMeasure,
      targetPrice: newItem.targetPrice,
      sortOrder: (auction.value?.items.length ?? 0) + 1,
    });
    newItem.description = "";
    toast.success("Kalem eklendi.");
    await load();
  });
}

async function invite() {
  if (!inviteCompanyId.value) return;
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/invitations`, { companyIds: [inviteCompanyId.value] });
    inviteCompanyId.value = "";
    toast.success("Cari davet edildi.");
    await load();
  });
}

async function publish() {
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/publish`);
    toast.success("İhale yayınlandı.");
    await load();
  });
}

async function cancelAuction() {
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/cancel`, "Admin tarafından iptal edildi");
    toast.success("İhale iptal edildi.");
    await load();
  });
}

async function extend() {
  if (!auction.value) return;
  await runAction(async () => {
    const newEndAt = new Date(new Date(auction.value!.endAt).getTime() + extendMinutes.value * 60_000).toISOString();
    await api.post(`/api/auctions/${auctionId}/extend`, {
      newEndAt,
      reason: `Admin manuel süre uzatma (+${extendMinutes.value} dk)`,
    });
    toast.success(`Süre ${extendMinutes.value} dk uzatıldı.`);
    await load();
  });
}

async function loadComparison(itemId: string) {
  await runAction(async () => {
    comparisons[itemId] = await api.get<BidComparisonDto[]>(`/api/auctions/${auctionId}/items/${itemId}/bids/compare`);
  });
}

async function award(itemId: string, bidId: string) {
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/items/${itemId}/award`, { winningBidId: bidId, notes: null });
    toast.success("Kazanan seçildi.");
    await load();
    await loadComparison(itemId);
  });
}

let ticker: ReturnType<typeof setInterval> | null = null;

onMounted(async () => {
  await load();
  if (!auction.value) return;

  ticker = setInterval(() => {
    now.value = Date.now();
  }, 1000);

  const conn = await connectAndJoin(auctionId);

  conn.on("BidPlaced", (bid: BidDto) => {
    liveLog.value = [`Yeni teklif: ${bid.amount} (${bid.status})`, ...liveLog.value].slice(0, 20);
    load();
  });

  conn.on("AuctionExtended", (payload: { newEndAt: string; reason: string }) => {
    liveLog.value = [`Süre uzatıldı: ${payload.reason}`, ...liveLog.value].slice(0, 20);
    if (auction.value) auction.value.endAt = payload.newEndAt;
  });

  conn.on("AuctionStarted", () => {
    liveLog.value = ["İhale canlıya geçti", ...liveLog.value].slice(0, 20);
    load();
  });

  conn.on("AuctionEnded", () => {
    liveLog.value = ["İhale sona erdi", ...liveLog.value].slice(0, 20);
    load();
  });

  conn.on("ItemAwarded", () => {
    liveLog.value = ["Bir kalem için kazanan seçildi", ...liveLog.value].slice(0, 20);
    load();
  });
});

onBeforeUnmount(() => {
  if (ticker) clearInterval(ticker);
  disconnect();
});
</script>

<template>
  <LoadingSpinner v-if="loading" />

  <div v-else-if="auction" class="space-y-6">
    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ auction.title }}</h1>
        <p class="mt-1 text-sm text-slate-500">{{ auction.description }}</p>
        <StepBadge v-if="workflowStep" class="mt-2" :step="workflowStep.step" :total="3" :label="workflowStep.label" />
      </div>
      <AppBadge :tone="statusTone[auction.status]">{{ auction.status }}</AppBadge>
    </div>

    <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
      <AppCard>
        <div class="text-xs text-slate-500">Tür</div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.auctionType }}</div>
      </AppCard>
      <AppCard>
        <div class="text-xs text-slate-500">Para Birimi</div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.currencyCode }}</div>
      </AppCard>
      <AppCard>
        <div class="text-xs text-slate-500">Bitişe Kalan</div>
        <div class="mt-1 font-mono font-semibold text-emerald-700">{{ timeLeftLabel }}</div>
      </AppCard>
      <AppCard>
        <div class="text-xs text-slate-500">Süre Uzatma Kuralı</div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.extensionRuleMinutes }} dk</div>
      </AppCard>
    </div>

    <div v-if="isAdminRole()" class="flex flex-wrap items-center gap-2 rounded-xl border border-amber-200 bg-amber-50 p-4">
      <span class="text-sm font-medium text-amber-800">Admin Kontrolü:</span>
      <AppButton v-if="auction.status === 'Draft'" @click="publish">
        Yayınla
      </AppButton>
      <template v-if="auction.status === 'Live'">
        <input v-model.number="extendMinutes" type="number" min="1" class="w-20 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        <AppButton variant="secondary" @click="extend">
          Süreyi Anında Uzat
        </AppButton>
      </template>
      <AppButton
        v-if="!['Ended', 'Awarded', 'Cancelled'].includes(auction.status)"
        variant="danger"
        @click="cancelAuction"
      >
        İhaleyi İptal Et
      </AppButton>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <div class="lg:col-span-2 space-y-4">
        <AppCard v-for="item in auction.items" :key="item.id">
          <div class="flex items-center justify-between">
            <div>
              <div class="font-semibold text-slate-800">{{ item.description }}</div>
              <div class="text-xs text-slate-500">{{ item.quantity }} {{ item.unitOfMeasure }} · {{ item.bidCount }} teklif</div>
            </div>
            <div class="text-right">
              <div class="text-xs text-slate-500">Güncel En İyi</div>
              <div class="font-mono text-lg font-bold text-emerald-700">
                {{ item.currentBestAmount ?? "-" }}
              </div>
            </div>
          </div>

          <button class="mt-3 text-xs font-medium text-indigo-600 hover:underline" @click="loadComparison(item.id)">
            Teklifleri Karşılaştır (çoklu para birimi normalize edilmiş)
          </button>

          <div v-if="comparisons[item.id]" class="mt-2 overflow-x-auto">
            <table class="w-full text-xs">
              <thead>
                <tr class="border-b border-slate-100 text-left uppercase text-slate-400">
                  <th class="pb-1">Cari</th>
                  <th class="pb-1">Tutar</th>
                  <th class="pb-1">Normalize</th>
                  <th class="pb-1">Durum</th>
                  <th class="pb-1"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="b in comparisons[item.id]" :key="b.bidId" class="border-b border-slate-50">
                  <td class="py-1">{{ b.companyName }}</td>
                  <td class="py-1 font-mono">{{ b.amount }} {{ b.currencyCode }}</td>
                  <td class="py-1 font-mono">{{ b.normalizedAmountInBaseCurrency.toFixed(2) }}</td>
                  <td class="py-1">{{ b.status }}</td>
                  <td class="py-1 text-right">
                    <button
                      v-if="auction.status !== 'Awarded'"
                      class="rounded bg-indigo-600 px-2 py-0.5 text-white"
                      @click="award(item.id, b.bidId)"
                    >
                      Kazanan Seç
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
            <EmptyState v-if="comparisons[item.id].length === 0">Henüz teklif yok</EmptyState>
          </div>
        </AppCard>

        <form v-if="auction.status === 'Draft'" class="rounded-xl border border-dashed border-slate-300 bg-white p-5" @submit.prevent="addItem">
          <h3 class="text-sm font-semibold text-slate-700">Kalem Ekle</h3>
          <div class="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-4">
            <input v-model="newItem.description" placeholder="Açıklama" required class="sm:col-span-2 rounded-md border border-slate-300 px-3 py-2 text-sm" />
            <input v-model.number="newItem.quantity" type="number" min="0.01" step="0.01" placeholder="Miktar" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
            <select v-model="newItem.unitOfMeasure" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
              <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ u }}</option>
            </select>
          </div>
          <AppButton type="submit" variant="secondary" class="mt-3">Ekle</AppButton>
        </form>
      </div>

      <div class="space-y-4">
        <AppCard>
          <h3 class="text-sm font-semibold text-slate-700">Davetli Cariler</h3>
          <ul v-if="auction.invitations.length > 0" class="mt-3 space-y-2 text-sm">
            <li v-for="inv in auction.invitations" :key="inv.id" class="flex items-center justify-between">
              <span>{{ inv.companyName }}</span>
              <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ inv.status }}</span>
            </li>
          </ul>
          <EmptyState v-else class="mt-3">Henüz davet yok</EmptyState>

          <div v-if="['Draft', 'Scheduled'].includes(auction.status)" class="mt-3 flex gap-2">
            <select v-model="inviteCompanyId" class="flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm">
              <option value="" disabled>Cari seç...</option>
              <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
            <AppButton @click="invite">Davet Et</AppButton>
          </div>
        </AppCard>

        <AppCard>
          <h3 class="text-sm font-semibold text-slate-700">Canlı Akış</h3>
          <ul v-if="liveLog.length > 0" class="mt-3 space-y-1 text-xs text-slate-500">
            <li v-for="(entry, i) in liveLog" :key="i">{{ entry }}</li>
          </ul>
          <EmptyState v-else class="mt-3">Henüz olay yok</EmptyState>
        </AppCard>
      </div>
    </div>
  </div>
</template>
