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
const { t } = useI18n();
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
    toast.error(apiErrorMessage(error, t("auctions.detail.actionError")));
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
      return { step: 1, label: t("auctions.detail.workflow.draft") };
    case "Scheduled":
      return { step: 2, label: t("auctions.detail.workflow.scheduled") };
    case "Live":
      return { step: 3, label: t("auctions.detail.workflow.live") };
    default:
      return null;
  }
});

const unitOfMeasures = ["Piece", "Kilogram", "Gram", "Liter", "Meter", "SquareMeter", "CubicMeter", "Box", "Pallet", "Hour", "Service"];
const unitLabel = (u: string) => t(`enums.unitOfMeasure.${u}`);

const timeLeftLabel = computed(() => {
  if (!auction.value) return "";
  const diffMs = new Date(auction.value.endAt).getTime() - now.value;
  if (diffMs <= 0) return t("auctions.detail.timeExpired");
  const totalSeconds = Math.floor(diffMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return t("auctions.detail.timeLeftFormat", { minutes, seconds });
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
    toast.error(apiErrorMessage(error, t("auctions.detail.loadError")));
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
    toast.success(t("auctions.detail.itemAddedToast"));
    await load();
  });
}

async function invite() {
  if (!inviteCompanyId.value) return;
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/invitations`, { companyIds: [inviteCompanyId.value] });
    inviteCompanyId.value = "";
    toast.success(t("auctions.detail.invitedToast"));
    await load();
  });
}

async function publish() {
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/publish`);
    toast.success(t("auctions.detail.publishedToast"));
    await load();
  });
}

async function cancelAuction() {
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/cancel`, t("auctions.detail.cancelReason"));
    toast.success(t("auctions.detail.cancelledToast"));
    await load();
  });
}

async function extend() {
  if (!auction.value) return;
  await runAction(async () => {
    const newEndAt = new Date(new Date(auction.value!.endAt).getTime() + extendMinutes.value * 60_000).toISOString();
    await api.post(`/api/auctions/${auctionId}/extend`, {
      newEndAt,
      reason: t("auctions.detail.extendReason", { minutes: extendMinutes.value }),
    });
    toast.success(t("auctions.detail.extendedToast", { minutes: extendMinutes.value }));
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
    toast.success(t("auctions.detail.winnerSelectedToast"));
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
    liveLog.value = [t("auctions.detail.log.newBid", { amount: bid.amount, status: bid.status }), ...liveLog.value].slice(0, 20);
    load();
  });

  conn.on("AuctionExtended", (payload: { newEndAt: string; reason: string }) => {
    liveLog.value = [t("auctions.detail.log.extended", { reason: payload.reason }), ...liveLog.value].slice(0, 20);
    if (auction.value) auction.value.endAt = payload.newEndAt;
  });

  conn.on("AuctionStarted", () => {
    liveLog.value = [t("auctions.detail.log.started"), ...liveLog.value].slice(0, 20);
    load();
  });

  conn.on("AuctionEnded", () => {
    liveLog.value = [t("auctions.detail.log.ended"), ...liveLog.value].slice(0, 20);
    load();
  });

  conn.on("ItemAwarded", () => {
    liveLog.value = [t("auctions.detail.log.awarded"), ...liveLog.value].slice(0, 20);
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
      <AppBadge :tone="statusTone[auction.status]">{{ $t(`enums.auctionStatus.${auction.status}`) }}</AppBadge>
    </div>

    <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
      <AppCard>
        <div class="text-xs text-slate-500">{{ $t("auctions.detail.type") }}</div>
        <div class="mt-1 font-semibold text-slate-800">{{ $t(`enums.auctionType.${auction.auctionType}`) }}</div>
      </AppCard>
      <AppCard>
        <div class="text-xs text-slate-500">{{ $t("auctions.detail.currency") }}</div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.currencyCode }}</div>
      </AppCard>
      <AppCard>
        <div class="text-xs text-slate-500">{{ $t("auctions.detail.timeLeft") }}</div>
        <div class="mt-1 font-mono font-semibold text-emerald-700">{{ timeLeftLabel }}</div>
      </AppCard>
      <AppCard>
        <div class="text-xs text-slate-500">{{ $t("auctions.detail.extensionRule") }}</div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.extensionRuleMinutes }} {{ $t("auctions.detail.minutesShort") }}</div>
      </AppCard>
    </div>

    <div v-if="isAdminRole()" class="flex flex-wrap items-center gap-2 rounded-xl border border-amber-200 bg-amber-50 p-4">
      <span class="text-sm font-medium text-amber-800">{{ $t("auctions.detail.adminControl") }}</span>
      <AppButton v-if="auction.status === 'Draft'" @click="publish">
        {{ $t("auctions.detail.publish") }}
      </AppButton>
      <template v-if="auction.status === 'Live'">
        <input v-model.number="extendMinutes" type="number" min="1" class="w-20 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        <AppButton variant="secondary" @click="extend">
          {{ $t("auctions.detail.extendNow") }}
        </AppButton>
      </template>
      <AppButton
        v-if="!['Ended', 'Awarded', 'Cancelled'].includes(auction.status)"
        variant="danger"
        @click="cancelAuction"
      >
        {{ $t("auctions.detail.cancelAuction") }}
      </AppButton>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <div class="lg:col-span-2 space-y-4">
        <AppCard v-for="item in auction.items" :key="item.id">
          <div class="flex items-center justify-between">
            <div>
              <div class="font-semibold text-slate-800">{{ item.description }}</div>
              <div class="text-xs text-slate-500">{{ item.quantity }} {{ unitLabel(item.unitOfMeasure) }} · {{ item.bidCount }} {{ $t("auctions.detail.bidCount") }}</div>
            </div>
            <div class="text-right">
              <div class="text-xs text-slate-500">{{ $t("auctions.detail.currentBest") }}</div>
              <div class="font-mono text-lg font-bold text-emerald-700">
                {{ item.currentBestAmount ?? "-" }}
              </div>
            </div>
          </div>

          <button class="mt-3 text-xs font-medium text-indigo-600 hover:underline" @click="loadComparison(item.id)">
            {{ $t("auctions.detail.compareBids") }}
          </button>

          <div v-if="comparisons[item.id]" class="mt-2 overflow-x-auto">
            <table class="w-full text-xs">
              <thead>
                <tr class="border-b border-slate-100 text-left uppercase text-slate-400">
                  <th class="pb-1">{{ $t("auctions.detail.compareTable.company") }}</th>
                  <th class="pb-1">{{ $t("auctions.detail.compareTable.amount") }}</th>
                  <th class="pb-1">{{ $t("auctions.detail.compareTable.normalized") }}</th>
                  <th class="pb-1">{{ $t("common.status") }}</th>
                  <th class="pb-1"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="b in comparisons[item.id]" :key="b.bidId" class="border-b border-slate-50">
                  <td class="py-1">{{ b.companyName }}</td>
                  <td class="py-1 font-mono">{{ b.amount }} {{ b.currencyCode }}</td>
                  <td class="py-1 font-mono">{{ b.normalizedAmountInBaseCurrency.toFixed(2) }}</td>
                  <td class="py-1">{{ $t(`enums.bidStatus.${b.status}`) }}</td>
                  <td class="py-1 text-right">
                    <button
                      v-if="auction.status !== 'Awarded'"
                      class="rounded bg-indigo-600 px-2 py-0.5 text-white"
                      @click="award(item.id, b.bidId)"
                    >
                      {{ $t("auctions.detail.selectWinner") }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
            <EmptyState v-if="comparisons[item.id].length === 0">{{ $t("auctions.detail.emptyBids") }}</EmptyState>
          </div>
        </AppCard>

        <form v-if="auction.status === 'Draft'" class="rounded-xl border border-dashed border-slate-300 bg-white p-5" @submit.prevent="addItem">
          <h3 class="text-sm font-semibold text-slate-700">{{ $t("auctions.detail.addItem") }}</h3>
          <div class="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-4">
            <input v-model="newItem.description" :placeholder="$t('auctions.detail.form.description')" required class="sm:col-span-2 rounded-md border border-slate-300 px-3 py-2 text-sm" />
            <input v-model.number="newItem.quantity" type="number" min="0.01" step="0.01" :placeholder="$t('auctions.detail.form.quantity')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
            <select v-model="newItem.unitOfMeasure" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
              <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ unitLabel(u) }}</option>
            </select>
          </div>
          <AppButton type="submit" variant="secondary" class="mt-3">{{ $t("auctions.detail.add") }}</AppButton>
        </form>
      </div>

      <div class="space-y-4">
        <AppCard>
          <h3 class="text-sm font-semibold text-slate-700">{{ $t("auctions.detail.invitedCompanies") }}</h3>
          <ul v-if="auction.invitations.length > 0" class="mt-3 space-y-2 text-sm">
            <li v-for="inv in auction.invitations" :key="inv.id" class="flex items-center justify-between">
              <span>{{ inv.companyName }}</span>
              <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ $t(`enums.invitationStatus.${inv.status}`) }}</span>
            </li>
          </ul>
          <EmptyState v-else class="mt-3">{{ $t("auctions.detail.emptyInvites") }}</EmptyState>

          <div v-if="['Draft', 'Scheduled'].includes(auction.status)" class="mt-3 flex gap-2">
            <select v-model="inviteCompanyId" class="flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm">
              <option value="" disabled>{{ $t("auctions.detail.selectCompany") }}</option>
              <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
            <AppButton @click="invite">{{ $t("auctions.detail.invite") }}</AppButton>
          </div>
        </AppCard>

        <AppCard>
          <h3 class="text-sm font-semibold text-slate-700">{{ $t("auctions.detail.liveLog") }}</h3>
          <ul v-if="liveLog.length > 0" class="mt-3 space-y-1 text-xs text-slate-500">
            <li v-for="(entry, i) in liveLog" :key="i">{{ entry }}</li>
          </ul>
          <EmptyState v-else class="mt-3">{{ $t("auctions.detail.emptyLog") }}</EmptyState>
        </AppCard>
      </div>
    </div>
  </div>
</template>
