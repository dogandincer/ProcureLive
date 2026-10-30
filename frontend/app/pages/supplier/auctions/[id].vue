<script setup lang="ts">
import type { AuctionDetailDto, AuctionFeedEntryDto, AuctionItemDto, BidDto } from "~/types/api";

const route = useRoute();
const auctionId = route.params.id as string;

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { connectAndJoin, disconnect } = useAuctionHub();
const { currentUser } = useAuth();
const { subscription, isPlus, refresh: refreshSubscription } = useSubscription();
const { formatDate, formatDateTime, formatDuration, formatMoney } = useDateFormat();

const auction = ref<AuctionDetailDto | null>(null);
const loading = ref(true);
const now = ref(Date.now());
const bidAmounts = reactive<Record<string, number | null>>({});
const bidErrors = reactive<Record<string, string>>({});
const placingBid = reactive<Record<string, boolean>>({});
// bidId ile tutulur: kendi teklifimizin anonim yayını gelirse "siz" satırıyla değiştirilir (çift satır olmaz).
const feed = ref<{ id: string | null; text: string; time: number }[]>([]);
const myBidIds = new Set<string>();
const declineReason = ref("");
const showDecline = ref(false);
const flashItem = ref<string | null>(null);
const accessDenied = ref(false);
const joining = ref(false);

const myInvitation = computed(() =>
  auction.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId) ?? null,
);

const canBid = computed(() => myInvitation.value && ["Accepted", "Participating"].includes(myInvitation.value.status));
// Kapalı zarf: rakiplerin tutarı ve sıralama gösterilmez (sunucu da göndermez).
const sealed = computed(() => !!auction.value && ["SealedBid", "ReverseSealed"].includes(auction.value.auctionType));
const higherIsBetter = computed(() => auction.value?.auctionType === "English");

// Kısıtlı görünüm: sunucu kalem/fiyat/ek/şart verisini hiç göndermedi; sayfa yalnızca vitrin + bulanık iskelet gösterir.
const restricted = computed(() => !!auction.value && auction.value.accessLevel !== "Full");
// Canlı ihalede teklif alanları önce gelir (mobilde şartları geçmeden teklif verilebilsin); diğer durumlarda şartlar önce okunur.
const termsFirst = computed(() => auction.value?.status !== "Live");
const sectionNavItems = computed(() => [
  { id: "items", label: t("sectionNav.items"), count: auction.value?.items.length },
  { id: "terms", label: t("sectionNav.terms") },
  { id: "feed", label: t("sectionNav.feed") },
  { id: "qa", label: t("sectionNav.qa") },
]);
const showQa = computed(() => !!auction.value && !restricted.value && !!myInvitation.value && myInvitation.value.status !== "Declined");
const qaOpen = computed(() => !!auction.value && ["Scheduled", "Live", "Paused"].includes(auction.value.status));
const restrictedLevel = computed(() => (auction.value?.accessLevel === "NoAccess" ? "NoAccess" : "QuotaExceeded"));
const showJoin = computed(() => !!auction.value && auction.value.canJoin && !myInvitation.value);
const showParticipation = computed(() => {
  const a = auction.value;
  if (!a || myInvitation.value) return false;
  return a.requiresApproval || (!!a.myParticipationRequest && a.myParticipationRequest.status !== "Accepted");
});

// Planlı ihalede başlangıca, canlıda bitişe kalan süre.
const countdown = computed(() => {
  const a = auction.value;
  if (!a) return { label: "", value: "" };
  if (a.status === "Scheduled") {
    return { label: t("auctions.live.startsIn"), value: formatDuration(new Date(a.startAt).getTime() - now.value), sub: formatDateTime(a.startAt) };
  }
  if (["Ended", "Awarded", "Cancelled"].includes(a.status)) {
    return { label: t("auctions.live.endedAt"), value: formatDateTime(a.actualEndAt ?? a.endAt) };
  }
  return { label: t("auctions.live.timeLeft"), value: formatDuration(new Date(a.endAt).getTime() - now.value), sub: formatDateTime(a.endAt) };
});

const isUrgent = computed(() => {
  if (!auction.value || auction.value.status !== "Live") return false;
  return new Date(auction.value.endAt).getTime() - now.value <= auction.value.extensionRuleMinutes * 60_000;
});

function rankLabel(item: AuctionItemDto): string {
  if (item.myRank == null) return "";
  return item.myRank === 1
    ? t("auctions.live.youLead")
    : t("auctions.live.yourRank", { rank: item.myRank, total: item.competitorCount ?? item.myRank });
}

/** Kabul edilmiş ön teklifin bu kalemdeki sonucu (ilk teklif oldu / en iyiyi geçmediği için atlandı). */
function preOfferLine(itemId: string) {
  const r = auction.value?.myParticipationRequest;
  if (!r || r.status !== "Accepted") return null;
  const line = r.lines.find((l) => l.auctionItemId === itemId);
  return line && line.appliedAsBid != null ? line : null;
}

async function load() {
  loading.value = true;
  try {
    auction.value = await api.get<AuctionDetailDto>(`/api/auctions/${auctionId}`);
  } catch (error: any) {
    if (error?.response?.status === 403) {
      accessDenied.value = true;
    } else {
      toast.error(apiErrorMessage(error, t("auctions.live.loadError")));
    }
  } finally {
    loading.value = false;
  }
}

function itemName(itemId: string) {
  return auction.value?.items.find((i) => i.id === itemId)?.description ?? "";
}

function feedText(entry: { isMine: boolean; amount: number | null; item: string }) {
  const amount = entry.amount != null && entry.amount > 0 ? formatMoney(entry.amount, auction.value?.currencyCode) : null;
  if (entry.isMine) return t("auctions.live.feed.myBid", { item: entry.item, amount: amount ?? "" });
  return amount ? t("auctions.live.feed.otherBid", { item: entry.item, amount }) : t("auctions.live.feed.otherBidSealed", { item: entry.item });
}

async function loadFeed() {
  try {
    const rows = await api.get<AuctionFeedEntryDto[]>(`/api/auctions/${auctionId}/feed`);
    rows.filter((r) => r.isMine).forEach((r) => myBidIds.add(r.bidId));
    feed.value = rows.map((r) => ({
      id: r.bidId,
      time: new Date(r.bidTime).getTime(),
      text: feedText({ isMine: r.isMine, amount: r.amount, item: r.itemDescription }),
    }));
  } catch {
    // akış geçmişi yardımcı bilgidir
  }
}

async function handleQuotaError(error: any): Promise<boolean> {
  if (apiErrorCode(error) !== "QuotaExceeded") return false;
  toast.error(t("subscription.quotaReachedToast"));
  // Sunucu artık bu ihaleyi kısıtlı döndürecek; canlı bağlantıyı da kapat.
  disconnect();
  await Promise.all([load(), refreshSubscription()]);
  return true;
}

async function accept() {
  try {
    await api.post(`/api/auctions/${auctionId}/invitations/respond`, { accept: true, declineReason: null });
    toast.success(t("auctions.live.acceptedToast"));
    await Promise.all([load(), refreshSubscription()]);
  } catch (error: any) {
    if (!(await handleQuotaError(error))) toast.error(apiErrorMessage(error, t("auctions.live.actionError")));
  }
}

async function join() {
  joining.value = true;
  try {
    await api.post(`/api/auctions/${auctionId}/join`);
    toast.success(t("auctions.live.joinedToast"));
    await Promise.all([load(), refreshSubscription()]);
  } catch (error: any) {
    if (!(await handleQuotaError(error))) toast.error(apiErrorMessage(error, t("auctions.live.actionError")));
  } finally {
    joining.value = false;
  }
}

async function decline() {
  try {
    await api.post(`/api/auctions/${auctionId}/invitations/respond`, {
      accept: false,
      declineReason: declineReason.value || t("common.notSpecified"),
    });
    showDecline.value = false;
    // Declining revokes read access to this auction (server-side), so re-fetching would 403.
    // Update the invitation status locally instead of reloading.
    const invitation = auction.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId);
    if (invitation) invitation.status = "Declined";
    toast.success(t("auctions.live.declinedToast"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.live.actionError")));
  }
}

async function placeBid(item: AuctionItemDto) {
  const amount = bidAmounts[item.id];
  if (!amount || !auction.value) return;

  bidErrors[item.id] = "";
  placingBid[item.id] = true;

  try {
    const bid = await api.post<BidDto>(`/api/auctions/${auctionId}/items/${item.id}/bids`, {
      amount,
      currencyId: auction.value.currencyId,
    });
    bidAmounts[item.id] = null;
    myBidIds.add(bid.id);
    feed.value = [{ id: bid.id, time: Date.now(), text: feedText({ isMine: true, amount, item: item.description }) },
      ...feed.value.filter((f) => f.id !== bid.id)].slice(0, 30);
    // Don't rely solely on the SignalR echo for the bidder's own UI — refresh directly so the new
    // best-amount/bid-count shows immediately even if the realtime connection is slow or not yet up.
    await load();
  } catch (error: any) {
    bidErrors[item.id] = apiErrorMessage(error, t("auctions.live.bidError"));
  } finally {
    placingBid[item.id] = false;
  }
}

function pushFeed(text: string, id: string | null = null) {
  feed.value = [{ id, text, time: Date.now() }, ...feed.value].slice(0, 30);
}

// Satınalmacı talebi onayladı / kabul etti / reddetti → sayfa yenilenmeden yansır.
const lastNotification = useLastNotification();
watch(lastNotification, (n) => {
  if (n?.type === "ParticipationRequest" && n.relatedEntityId === auctionId) {
    Promise.all([load(), refreshSubscription(), loadFeed()]);
  }
});

let ticker: ReturnType<typeof setInterval> | null = null;

onMounted(async () => {
  await Promise.all([load(), refreshSubscription()]);
  if (accessDenied.value || !auction.value) return;

  ticker = setInterval(() => {
    now.value = Date.now();
  }, 1000);

  // Restricted view: no live group (the hub would refuse it anyway — same access rule server-side).
  if (restricted.value) return;
  await loadFeed();

  // Connect regardless of invitation status: even before accepting, the supplier should see the
  // countdown/start/end/extension events live. Bidding itself stays gated separately (canBid + backend).
  const conn = await connectAndJoin(auctionId);

  // Sunucu teklifleri tedarikçiye anonim (kapalı zarfta tutarsız) yollar; kendi teklifimiz zaten akışta.
  conn.on("BidPlaced", (bid: BidDto) => {
    if (!myBidIds.has(bid.id)) {
      pushFeed(feedText({ isMine: false, amount: bid.amount, item: itemName(bid.auctionItemId) }), bid.id);
    }
    flashItem.value = bid.auctionItemId;
    setTimeout(() => (flashItem.value = null), 1500);
    load();
  });

  conn.on("AuctionExtended", (payload: { newEndAt: string; reason: string }) => {
    pushFeed(t("auctions.live.feed.extended", { reason: payload.reason }));
    if (auction.value) auction.value.endAt = payload.newEndAt;
  });

  conn.on("AuctionStarted", () => {
    pushFeed(t("auctions.live.feed.started"));
    load();
  });

  conn.on("AuctionEnded", () => {
    pushFeed(t("auctions.live.feed.ended"));
    load();
  });

  // Admin elle durum değiştirdi (başlat / duraklat / devam / bitir / yeniden aç / iptal) — sayfa yenilenmeden yansır.
  conn.on("AuctionStatusChanged", (payload: { status: string; reason: string | null }) => {
    const status = t(`enums.auctionStatus.${payload.status}`);
    pushFeed(payload.reason
      ? t("auctions.live.feed.statusChangedReason", { status, reason: payload.reason })
      : t("auctions.live.feed.statusChanged", { status }));
    load();
    loadFeed();
  });

  conn.on("ItemAwarded", (payload: { auctionItemId: string; winningCompanyId: string }) => {
    const won = payload.winningCompanyId === currentUser.value?.companyId;
    pushFeed(won ? t("auctions.live.feed.wonItem") : t("auctions.live.feed.itemAwarded"));
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
    <p class="text-sm font-medium text-slate-600">{{ $t("auctions.live.accessDenied") }}</p>
    <NuxtLink to="/supplier/auctions" class="mt-2 inline-block text-xs font-medium text-indigo-600 hover:underline">← {{ $t("restricted.backToList") }}</NuxtLink>
  </div>

  <LoadingSpinner v-else-if="loading && !auction" />

  <!-- Kısıtlı görünüm (kota dolu / yetkisiz): yalnızca sunucunun bıraktığı vitrin bilgisi + bulanık iskelet -->
  <div v-else-if="auction && restricted" class="space-y-6">
    <div v-if="restrictedLevel === 'QuotaExceeded'" class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <div class="flex items-center gap-2">
          <h1 class="text-xl font-bold text-slate-800">{{ auction.title }}</h1>
          <AppBadge tone="amber">{{ $t("restricted.QuotaExceeded.short") }}</AppBadge>
        </div>
        <p class="mt-1 text-sm text-slate-500">
          {{ auction.ownerCompanyName }}
          <span v-if="auction.referenceCode" class="font-mono text-xs text-slate-400">· {{ auction.referenceCode }}</span>
        </p>
      </div>
      <div class="grid grid-cols-3 gap-4 text-right text-xs text-slate-500">
        <div>
          <div>{{ $t("auctions.live.status") }}</div>
          <div class="mt-0.5 text-sm font-semibold text-slate-700">{{ $t(`enums.auctionStatus.${auction.status}`) }}</div>
        </div>
        <div>
          <div>{{ $t("restricted.itemCount") }}</div>
          <div class="mt-0.5 text-sm font-semibold text-slate-700">{{ auction.itemCount }}</div>
        </div>
        <div>
          <div>{{ $t("auctions.supplierList.table.endsAt") }}</div>
          <div class="mt-0.5 text-sm font-semibold text-slate-700">{{ formatDateTime(auction.endAt) }}</div>
        </div>
      </div>
    </div>

    <RestrictedOverlay :level="restrictedLevel" :rows="Math.min(Math.max(auction.itemCount, 3), 5)">
      <template #extra>
        <p v-if="restrictedLevel === 'QuotaExceeded' && subscription && !isPlus" class="mt-3 text-xs text-slate-400">
          {{ $t("subscription.quotaUsage", { used: subscription.joinedCount, limit: subscription.quotaLimit }) }}
        </p>
        <NuxtLink to="/supplier/auctions" class="mt-3 block text-xs font-medium text-indigo-600 hover:underline">
          ← {{ $t("restricted.backToList") }}
        </NuxtLink>
      </template>
    </RestrictedOverlay>
  </div>

  <div v-else-if="auction" class="space-y-6">
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div class="min-w-0">
        <h1 class="text-xl font-bold text-slate-800">{{ auction.title }}</h1>
        <p class="mt-1 text-sm text-slate-500">
          {{ auction.ownerCompanyName }} · {{ $t(`enums.auctionType.${auction.auctionType}`) }}
          <span v-if="auction.referenceCode" class="font-mono text-xs text-slate-400">· {{ auction.referenceCode }}</span>
        </p>
        <p v-if="auction.description" class="mt-1 text-sm text-slate-500">{{ auction.description }}</p>
      </div>
      <div class="text-right">
        <div class="text-xs text-slate-500">{{ $t("auctions.live.status") }}</div>
        <div class="font-semibold" :class="auction.status === 'Live' ? 'text-emerald-600' : 'text-slate-700'">
          {{ $t(`enums.auctionStatus.${auction.status}`) }}
        </div>
      </div>
    </div>

    <!-- Duraklatıldı: süre işlemez, teklif alınmaz; geri sayım yerine durum gösterilir -->
    <div v-if="auction.status === 'Paused'" class="rounded-xl border border-amber-300 bg-amber-50 p-5 text-center">
      <div class="text-xs uppercase tracking-wide text-amber-600">{{ $t("auctions.live.timeLeft") }}</div>
      <div class="mt-1 text-2xl font-bold text-amber-700">{{ $t("auctions.live.pausedTime") }}</div>
      <div class="mt-1 text-sm text-amber-700">{{ $t("auctions.live.pausedBanner") }}</div>
    </div>
    <div
      v-else
      class="rounded-xl border p-5 text-center"
      :class="isUrgent ? 'border-rose-300 bg-rose-50' : 'border-slate-200 bg-surface'"
      data-testid="countdown"
    >
      <div class="text-xs uppercase tracking-wide text-slate-500">{{ countdown.label }}</div>
      <div class="mt-1 text-3xl font-bold tabular-nums" :class="isUrgent ? 'text-rose-600' : 'text-slate-800'">
        {{ countdown.value }}
      </div>
      <div v-if="countdown.sub" class="mt-0.5 text-xs text-slate-400">{{ countdown.sub }}</div>
      <div v-if="isUrgent" class="mt-1 text-xs text-rose-600">
        {{ $t("auctions.live.antiSniping") }}
      </div>
    </div>

    <div v-if="myInvitation?.status === 'Invited'" class="rounded-xl border border-amber-200 bg-amber-50 p-5">
      <p class="text-sm font-medium text-amber-800">{{ $t("auctions.live.invitedBanner") }}</p>
      <div class="mt-3 flex gap-2">
        <AppButton @click="accept">{{ $t("auctions.live.acceptInvite") }}</AppButton>
        <AppButton variant="danger-outline" @click="showDecline = true">{{ $t("common.decline") }}</AppButton>
      </div>
      <div v-if="showDecline" class="mt-3 flex gap-2">
        <input v-model="declineReason" :placeholder="$t('auctions.live.declineReasonRequired')" :aria-label="$t('auctions.live.declineReasonRequired')" class="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <AppButton variant="danger" @click="decline">{{ $t("common.confirm") }}</AppButton>
      </div>
    </div>

    <div v-else-if="myInvitation?.status === 'Declined'" class="rounded-xl border border-rose-200 bg-rose-50 p-5 text-sm text-rose-700">
      {{ $t("auctions.live.declinedBanner") }}
    </div>

    <!-- Davetsiz ama görünür açık ihale (davet eden firmanın diğer ihalesi / Plus vitrini) -->
    <div v-else-if="showJoin" class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-indigo-200 bg-indigo-50 p-5">
      <div>
        <p class="text-sm font-medium text-indigo-800">{{ $t("auctions.live.joinBanner") }}</p>
        <p v-if="subscription && !isPlus" class="mt-0.5 text-xs text-indigo-600">
          {{ $t("subscription.quotaRemaining", { n: subscription.remainingJoins }) }}
        </p>
      </div>
      <AppButton :loading="joining" @click="join">{{ $t("auctions.live.joinCta") }}</AppButton>
    </div>

    <!-- Onaylı cari değil: katılım talebi + ön teklif, ardından talep adımlarının durumu -->
    <ParticipationRequestCard v-if="showParticipation" :auction="auction" @changed="load" />

    <SectionNav :sections="sectionNavItems" />

    <div v-if="termsFirst" id="terms"><AuctionTermsCard :auction="auction" /></div>

    <p v-if="sealed" class="rounded-lg border border-slate-200 bg-slate-50 px-4 py-2 text-xs text-slate-500">{{ $t("auctions.live.sealedNote") }}</p>

    <div id="items" class="space-y-4">
      <div
        v-for="item in auction.items"
        :key="item.id"
        class="rounded-xl border bg-surface p-5 transition-colors"
        :class="flashItem === item.id ? 'border-emerald-400 bg-emerald-50' : 'border-slate-200'"
        data-testid="supplier-item"
      >
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div class="min-w-0">
            <div class="font-semibold text-slate-800">{{ item.description }}</div>
            <div class="text-xs text-slate-500">{{ item.quantity.toLocaleString() }} {{ $t(`enums.unitOfMeasure.${item.unitOfMeasure}`) }} · {{ item.bidCount }} {{ $t("auctions.live.bidCount") }}</div>
            <div v-if="item.requestedDeliveryDate || item.specification" class="mt-1 space-y-0.5 text-xs text-slate-500">
              <div v-if="item.requestedDeliveryDate">{{ $t("auctions.terms.requestedDelivery") }}: {{ formatDate(item.requestedDeliveryDate) }}</div>
              <div v-if="item.specification" class="whitespace-pre-line">{{ $t("auctions.terms.specification") }}: {{ item.specification }}</div>
              <p v-if="preOfferLine(item.id)" class="mt-1.5 text-xs" :class="preOfferLine(item.id)!.appliedAsBid ? 'text-emerald-700' : 'text-amber-700'" data-testid="pre-offer-outcome">
              {{ preOfferLine(item.id)!.appliedAsBid
                ? $t("participation.supplierApplied", { amount: formatMoney(preOfferLine(item.id)!.unitPrice) })
                : $t("participation.supplierSkipped", { amount: formatMoney(preOfferLine(item.id)!.unitPrice) }) }}
            </p>
          </div>
            <p v-if="preOfferLine(item.id)" class="mt-1.5 text-xs" :class="preOfferLine(item.id)!.appliedAsBid ? 'text-emerald-700' : 'text-amber-700'" data-testid="pre-offer-outcome">
              {{ preOfferLine(item.id)!.appliedAsBid
                ? $t("participation.supplierApplied", { amount: formatMoney(preOfferLine(item.id)!.unitPrice) })
                : $t("participation.supplierSkipped", { amount: formatMoney(preOfferLine(item.id)!.unitPrice) }) }}
            </p>
          </div>
          <div v-if="!sealed" class="text-right">
            <div class="text-xs text-slate-500">{{ $t("auctions.live.currentBestUnit") }}</div>
            <div class="font-mono text-2xl font-bold text-emerald-700">{{ item.currentBestAmount != null ? formatMoney(item.currentBestAmount) : "—" }} <span class="text-sm">{{ auction.currencyCode }}</span></div>
          </div>
        </div>

        <!-- Kendi teklifim ve sıram (kapalı zarfta yalnızca kendi teklifim) -->
        <div v-if="item.myBestAmount != null" class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 rounded-lg bg-slate-50 px-3 py-2 text-sm" data-testid="my-bid">
          <span class="text-slate-600">{{ $t("auctions.live.yourBest") }}: <b class="font-mono text-slate-800">{{ formatMoney(item.myBestAmount, auction.currencyCode) }}</b></span>
          <span class="text-xs text-slate-500">{{ $t("auctions.evaluation.lineTotal") }}: {{ formatMoney(item.myBestAmount * item.quantity, auction.currencyCode) }}</span>
          <AppBadge v-if="item.myRank != null" :tone="item.myRank === 1 ? 'emerald' : 'amber'">{{ rankLabel(item) }}</AppBadge>
        </div>

        <div v-if="canBid && auction.status === 'Live'" class="mt-4 flex flex-wrap items-end gap-2">
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">
            {{ $t("auctions.live.unitPriceLabel", { currency: auction.currencyCode }) }}
            <input
              v-model.number="bidAmounts[item.id]"
              type="number"
              step="0.01"
              min="0"
              :placeholder="higherIsBetter ? $t('auctions.live.higherHint') : $t('auctions.live.lowerHint')"
              class="w-56 rounded-md border border-slate-300 px-3 py-2 text-sm font-normal"
            />
          </label>
          <button
            class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
            :disabled="placingBid[item.id] || !bidAmounts[item.id]"
            @click="placeBid(item)"
          >
            {{ placingBid[item.id] ? $t("auctions.live.submitting") : $t("auctions.live.placeBid") }}
          </button>
          <span v-if="bidAmounts[item.id]" class="pb-2 text-xs text-slate-500">
            {{ $t("auctions.evaluation.lineTotal") }}: {{ formatMoney((bidAmounts[item.id] ?? 0) * item.quantity, auction.currencyCode) }}
          </span>
        </div>
        <p v-if="bidErrors[item.id]" class="mt-2 text-sm text-rose-600">{{ bidErrors[item.id] }}</p>
        <p v-if="!canBid" class="mt-3 text-xs text-slate-400">
          {{ showParticipation ? $t("participation.bidAfterAccept") : $t("auctions.live.needAcceptedInvite") }}
        </p>
        <p v-else-if="auction.status !== 'Live'" class="mt-3 text-xs text-slate-400">{{ $t("auctions.live.notLive") }}</p>
      </div>
    </div>

    <div v-if="!termsFirst" id="terms"><AuctionTermsCard :auction="auction" /></div>

    <AppCard id="feed">
      <h3 class="text-sm font-semibold text-slate-700">{{ $t("auctions.live.liveFeed") }}</h3>
      <ul v-if="feed.length > 0" class="mt-3 space-y-1 text-sm text-slate-600" data-testid="live-feed">
        <li v-for="(entry, i) in feed" :key="entry.id ?? i">
          <span class="mr-2 text-xs text-slate-400">{{ formatDateTime(new Date(entry.time)) }}</span>{{ entry.text }}
        </li>
      </ul>
      <EmptyState v-else class="mt-3">{{ $t("auctions.live.emptyFeed") }}</EmptyState>
    </AppCard>

    <ProcessQaPanel v-if="showQa" kind="auction" :process-id="auction.id" :internal="false" :can-ask="qaOpen" />
  </div>
</template>
