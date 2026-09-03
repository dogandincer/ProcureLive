<script setup lang="ts">
import type { AuctionDetailDto, AuctionItemDto, BidDto } from "~/types/api";

const route = useRoute();
const auctionId = route.params.id as string;

const api = useApi();
const { connectAndJoin, disconnect } = useAuctionHub();
const { currentUser } = useAuth();

const auction = ref<AuctionDetailDto | null>(null);
const loading = ref(true);
const now = ref(Date.now());
const bidAmounts = reactive<Record<string, number | null>>({});
const bidErrors = reactive<Record<string, string>>({});
const placingBid = reactive<Record<string, boolean>>({});
const feed = ref<{ text: string; time: number }[]>([]);
const declineReason = ref("");
const showDecline = ref(false);
const flashItem = ref<string | null>(null);
const actionError = ref("");
const accessDenied = ref(false);
const loadError = ref("");

const myInvitation = computed(() =>
  auction.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId) ?? null,
);

const canBid = computed(() => myInvitation.value && ["Accepted", "Participating"].includes(myInvitation.value.status));

const timeLeftLabel = computed(() => {
  if (!auction.value) return "";
  const diffMs = new Date(auction.value.endAt).getTime() - now.value;
  if (diffMs <= 0) return "Süre doldu";
  const totalSeconds = Math.floor(diffMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}dk ${seconds}sn`;
});

const isUrgent = computed(() => {
  if (!auction.value) return false;
  return new Date(auction.value.endAt).getTime() - now.value <= auction.value.extensionRuleMinutes * 60_000;
});

async function load() {
  loading.value = true;
  try {
    auction.value = await api.get<AuctionDetailDto>(`/api/auctions/${auctionId}`);
  } catch (error: any) {
    if (error?.response?.status === 403) {
      accessDenied.value = true;
    } else {
      loadError.value = apiErrorMessage(error, "İhale yüklenemedi.");
    }
  } finally {
    loading.value = false;
  }
}

async function accept() {
  actionError.value = "";
  try {
    await api.post(`/api/auctions/${auctionId}/invitations/respond`, { accept: true, declineReason: null });
    await load();
  } catch (error: any) {
    actionError.value = apiErrorMessage(error, "İşlem gerçekleştirilemedi.");
  }
}

async function decline() {
  actionError.value = "";
  try {
    await api.post(`/api/auctions/${auctionId}/invitations/respond`, {
      accept: false,
      declineReason: declineReason.value || "Belirtilmedi",
    });
    showDecline.value = false;
    // Declining revokes read access to this auction (server-side), so re-fetching would 403.
    // Update the invitation status locally instead of reloading.
    const invitation = auction.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId);
    if (invitation) invitation.status = "Declined";
  } catch (error: any) {
    actionError.value = apiErrorMessage(error, "İşlem gerçekleştirilemedi.");
  }
}

async function placeBid(item: AuctionItemDto) {
  const amount = bidAmounts[item.id];
  if (!amount || !auction.value) return;

  bidErrors[item.id] = "";
  placingBid[item.id] = true;

  try {
    await api.post(`/api/auctions/${auctionId}/items/${item.id}/bids`, {
      amount,
      currencyId: auction.value.currencyId,
    });
    bidAmounts[item.id] = null;
    // Don't rely solely on the SignalR echo for the bidder's own UI — refresh directly so the new
    // best-amount/bid-count shows immediately even if the realtime connection is slow or not yet up.
    await load();
    pushFeed(`Teklifiniz gönderildi: ${amount} ${auction.value.currencyCode}`);
  } catch (error: any) {
    bidErrors[item.id] = apiErrorMessage(error, "Teklif verilemedi.");
  } finally {
    placingBid[item.id] = false;
  }
}

function pushFeed(text: string) {
  feed.value = [{ text, time: Date.now() }, ...feed.value].slice(0, 30);
}

let ticker: ReturnType<typeof setInterval> | null = null;

onMounted(async () => {
  await load();
  if (accessDenied.value || !auction.value) return;

  ticker = setInterval(() => {
    now.value = Date.now();
  }, 1000);

  // Connect regardless of invitation status: even before accepting, the supplier should see the
  // countdown/start/end/extension events live. Bidding itself stays gated separately (canBid + backend).
  const conn = await connectAndJoin(auctionId);

  conn.on("BidPlaced", (bid: BidDto) => {
    pushFeed(`${bid.isMine ? "Siz" : bid.companyName || "Bir katılımcı"} ${bid.amount} teklif verdi`);
    flashItem.value = bid.auctionItemId;
    setTimeout(() => (flashItem.value = null), 1500);
    load();
  });

  conn.on("AuctionExtended", (payload: { newEndAt: string; reason: string }) => {
    pushFeed(`⏱️ Süre uzatıldı: ${payload.reason}`);
    if (auction.value) auction.value.endAt = payload.newEndAt;
  });

  conn.on("AuctionEnded", () => {
    pushFeed("🏁 İhale sona erdi");
    load();
  });

  conn.on("ItemAwarded", (payload: { auctionItemId: string; winningCompanyId: string }) => {
    const won = payload.winningCompanyId === currentUser.value?.companyId;
    pushFeed(won ? "🎉 Bir kalemi kazandınız!" : "Bir kalem için kazanan belirlendi.");
    load();
  });
});

onBeforeUnmount(() => {
  if (ticker) clearInterval(ticker);
  disconnect();
});
</script>

<template>
  <div v-if="accessDenied" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
    <p class="text-sm font-medium text-slate-600">Bu ihaleye erişiminiz yok. Daveti reddetmiş olabilirsiniz.</p>
  </div>

  <p v-else-if="loadError" class="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{{ loadError }}</p>

  <div v-else-if="auction" class="space-y-6">
    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ auction.title }}</h1>
        <p class="mt-1 text-sm text-slate-500">{{ auction.description }}</p>
      </div>
      <div class="text-right">
        <div class="text-xs text-slate-500">Durum</div>
        <div class="font-semibold" :class="auction.status === 'Live' ? 'text-emerald-600' : 'text-slate-700'">{{ auction.status }}</div>
      </div>
    </div>

    <div
      class="rounded-xl border p-5 text-center"
      :class="isUrgent && auction.status === 'Live' ? 'border-rose-300 bg-rose-50' : 'border-slate-200 bg-white'"
    >
      <div class="text-xs uppercase tracking-wide text-slate-500">Bitişe Kalan Süre</div>
      <div class="mt-1 font-mono text-3xl font-bold" :class="isUrgent ? 'text-rose-600' : 'text-slate-800'">
        {{ timeLeftLabel }}
      </div>
      <div v-if="isUrgent && auction.status === 'Live'" class="mt-1 text-xs text-rose-600">
        Son dakika teklifleri süreyi otomatik uzatır (anti-sniping)
      </div>
    </div>

    <div v-if="actionError" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ actionError }}</span>
      <button class="font-semibold" @click="actionError = ''">Kapat</button>
    </div>

    <div v-if="myInvitation?.status === 'Invited'" class="rounded-xl border border-amber-200 bg-amber-50 p-5">
      <p class="text-sm font-medium text-amber-800">Bu ihaleye davetlisiniz. Katılmak için daveti kabul edin.</p>
      <div class="mt-3 flex gap-2">
        <button class="rounded-md bg-emerald-600 px-4 py-2 text-sm font-semibold text-white" @click="accept">Daveti Kabul Et</button>
        <button class="rounded-md bg-rose-600 px-4 py-2 text-sm font-semibold text-white" @click="showDecline = true">Reddet</button>
      </div>
      <div v-if="showDecline" class="mt-3 flex gap-2">
        <input v-model="declineReason" placeholder="Red gerekçesi (zorunlu)" class="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <button class="rounded-md bg-rose-600 px-4 py-2 text-sm font-semibold text-white" @click="decline">Onayla</button>
      </div>
    </div>

    <div v-else-if="myInvitation?.status === 'Declined'" class="rounded-xl border border-rose-200 bg-rose-50 p-5 text-sm text-rose-700">
      Bu ihale davetini reddettiniz; teklif veremezsiniz.
    </div>

    <div class="space-y-4">
      <div
        v-for="item in auction.items"
        :key="item.id"
        class="rounded-xl border bg-white p-5 transition-colors"
        :class="flashItem === item.id ? 'border-emerald-400 bg-emerald-50' : 'border-slate-200'"
      >
        <div class="flex items-center justify-between">
          <div>
            <div class="font-semibold text-slate-800">{{ item.description }}</div>
            <div class="text-xs text-slate-500">{{ item.quantity }} {{ item.unitOfMeasure }} · {{ item.bidCount }} teklif</div>
          </div>
          <div class="text-right">
            <div class="text-xs text-slate-500">Güncel En İyi Teklif</div>
            <div class="font-mono text-2xl font-bold text-emerald-700">
              {{ item.currentBestAmount ?? "-" }} <span class="text-sm">{{ auction.currencyCode }}</span>
            </div>
          </div>
        </div>

        <div v-if="canBid && auction.status === 'Live'" class="mt-4 flex items-center gap-2">
          <input
            v-model.number="bidAmounts[item.id]"
            type="number"
            step="0.01"
            :placeholder="`Teklifiniz (${auction.currencyCode})`"
            class="w-48 rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <button
            class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
            :disabled="placingBid[item.id]"
            @click="placeBid(item)"
          >
            {{ placingBid[item.id] ? "Gönderiliyor..." : "Teklif Ver" }}
          </button>
        </div>
        <p v-if="bidErrors[item.id]" class="mt-2 text-sm text-rose-600">{{ bidErrors[item.id] }}</p>
        <p v-if="!canBid" class="mt-3 text-xs text-slate-400">Teklif verebilmek için davetin kabul edilmiş olması gerekir.</p>
        <p v-else-if="auction.status !== 'Live'" class="mt-3 text-xs text-slate-400">İhale şu anda canlı değil.</p>
      </div>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <h3 class="text-sm font-semibold text-slate-700">Canlı Akış</h3>
      <ul class="mt-3 space-y-1 text-sm text-slate-600">
        <li v-for="(entry, i) in feed" :key="i">{{ entry.text }}</li>
        <li v-if="feed.length === 0" class="text-slate-400">Henüz olay yok</li>
      </ul>
    </div>
  </div>
</template>
