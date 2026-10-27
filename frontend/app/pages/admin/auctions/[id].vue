<script setup lang="ts">
import type {
  AuctionDetailDto,
  AuctionItemDto,
  BidComparisonDto,
  BidDto,
  CompanyDto,
  PagedResult,
} from "~/types/api";
import {
  ClockIcon,
  CurrencyDollarIcon,
  BoltIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  PlusIcon,
  UserPlusIcon,
  ArrowsRightLeftIcon,
  TrophyIcon,
  DocumentMagnifyingGlassIcon,
} from "@heroicons/vue/24/outline";
import { CheckCircleIcon as CheckCircleSolid } from "@heroicons/vue/24/solid";

const route = useRoute();
const auctionId = route.params.id as string;

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();
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

// Track which item's award map (itemId -> bidId) to show awarded state inline
const awardedBidPerItem = reactive<Record<string, string>>({});

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
  Paused: "amber",
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
    case "Paused":
      return { step: 3, label: t("auctions.detail.workflow.paused") };
    case "Ended":
      return { step: 4, label: t("auctions.detail.workflow.ended") };
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

// Whether this auction is in the "evaluate" stage
const isEvaluationStage = computed(() =>
  auction.value ? ["Ended", "Awarded"].includes(auction.value.status) : false,
);

const evaluationSummary = computed(() => {
  if (!isEvaluationStage.value || !auction.value) return null;
  const items = auction.value.items;
  const total = items.length;
  const awarded = items.filter((it) => awardedBidPerItem[it.id] || (comparisons[it.id] && comparisons[it.id].some((b) => b.status === "Winning"))).length;
  return { total, awarded };
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

    // Auto-load all comparisons when in evaluation stage
    if (["Ended", "Awarded"].includes(detail.status)) {
      await loadAllComparisons(detail.items);
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.detail.loadError")));
  } finally {
    loading.value = false;
  }
}

async function loadAllComparisons(items: AuctionItemDto[]) {
  await Promise.allSettled(items.map((it) => loadComparison(it.id)));
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

async function loadComparison(itemId: string) {
  await runAction(async () => {
    comparisons[itemId] = await api.get<BidComparisonDto[]>(`/api/auctions/${auctionId}/items/${itemId}/bids/compare`);
    // Detect any already-winning bids
    const winner = comparisons[itemId].find((b) => b.status === "Winning");
    if (winner) awardedBidPerItem[itemId] = winner.bidId;
  });
}

async function award(itemId: string, bidId: string) {
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/items/${itemId}/award`, { winningBidId: bidId, notes: null });
    awardedBidPerItem[itemId] = bidId;
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

  // Başka bir admin (veya başka sekme) durumu değiştirdiğinde sayfa ve durum paneli kendini yeniler.
  conn.on("AuctionStatusChanged", (payload: { status: string }) => {
    liveLog.value = [t("auctions.detail.log.statusChanged", { status: t(`enums.auctionStatus.${payload.status}`) }), ...liveLog.value].slice(0, 20);
    load();
  });
});

onBeforeUnmount(() => {
  if (ticker) clearInterval(ticker);
  disconnect();
});
</script>

<template>
  <!-- Spinner only on first load: live refreshes (bids, extensions) must not remount the page/panels. -->
  <LoadingSpinner v-if="loading && !auction" />

  <div v-else-if="auction" class="space-y-6">
    <!-- Page header with breadcrumb -->
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div>
        <div class="mb-1 flex items-center gap-1 text-xs text-slate-400">
          <NuxtLink to="/admin/auctions" class="hover:text-slate-600">{{ $t("nav.auctions") }}</NuxtLink>
          <ChevronRightIcon class="h-3 w-3" />
          <span class="text-slate-500">{{ auction.title }}</span>
        </div>
        <h1 class="text-xl font-bold text-slate-800">{{ auction.title }}</h1>
        <p v-if="auction.description" class="mt-1 text-sm text-slate-500">{{ auction.description }}</p>
        <p v-if="auction.workspaceName || auction.responsibleUserName" class="mt-1 text-xs text-slate-500">
          <span v-if="auction.workspaceName">{{ $t("auctions.detail.workspace") }}: <span class="font-medium text-slate-700">{{ auction.workspaceName }}</span></span>
          <span v-if="auction.workspaceName && auction.responsibleUserName"> · </span>
          <span v-if="auction.responsibleUserName">{{ $t("auctions.detail.responsible") }}: <span class="font-medium text-slate-700">{{ auction.responsibleUserName }}</span></span>
        </p>
        <StepBadge v-if="workflowStep" class="mt-2" :step="workflowStep.step" :total="4" :label="workflowStep.label" />
      </div>
      <AppBadge :tone="statusTone[auction.status]" class="mt-1">
        {{ $t(`enums.auctionStatus.${auction.status}`) }}
      </AppBadge>
    </div>

    <!-- KPI row -->
    <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
      <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-2 text-xs text-slate-500">
          <ArrowsRightLeftIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.detail.type") }}
        </div>
        <div class="mt-1 font-semibold text-slate-800">{{ $t(`enums.auctionType.${auction.auctionType}`) }}</div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-2 text-xs text-slate-500">
          <CurrencyDollarIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.detail.currency") }}
        </div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.currencyCode }}</div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-2 text-xs text-slate-500">
          <ClockIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.detail.timeLeft") }}
        </div>
        <div class="mt-1 font-mono font-semibold" :class="auction.status === 'Live' ? 'text-emerald-600' : auction.status === 'Paused' ? 'text-amber-600' : 'text-slate-500'">
          {{ auction.status === "Live" ? timeLeftLabel : auction.status === "Paused" ? $t("enums.auctionStatus.Paused") : formatDateTime(auction.endAt) }}
        </div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-2 text-xs text-slate-500">
          <BoltIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.detail.extensionRule") }}
        </div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.extensionRuleMinutes }} {{ $t("auctions.detail.minutesShort") }}</div>
      </div>
    </div>

    <!-- Durum yönetimi: yayınla / başlat / duraklat / devam / bitir / yeniden aç / iptal + süre uzatma + geçmiş -->
    <AuctionStatusPanel :auction-id="auctionId" :status="auction.status" :can-manage="isAdminRole()" @changed="load" />

    <!-- Davetsiz tedarikçilerin katılım talepleri: cari onayı → ön teklif kabulü / red -->
    <ParticipationRequestsPanel v-if="auction.status !== 'Draft'" :auction-id="auctionId" @changed="load" />

    <!-- ============================================================ -->
    <!-- EVALUATION MODE: Ended / Awarded                            -->
    <!-- ============================================================ -->
    <template v-if="isEvaluationStage">
      <div class="rounded-xl border border-indigo-200 bg-indigo-50 px-5 py-4">
        <div class="flex items-center gap-2">
          <TrophyIcon class="h-5 w-5 text-indigo-500" />
          <span class="font-semibold text-indigo-800">{{ $t("auctions.evaluation.heading") }}</span>
        </div>
        <div v-if="evaluationSummary" class="mt-1 text-sm text-indigo-600">
          <CheckCircleSolid v-if="evaluationSummary.awarded === evaluationSummary.total" class="inline h-4 w-4 text-emerald-500 mr-1" />
          {{ $t("auctions.evaluation.progress", { awarded: evaluationSummary.awarded, total: evaluationSummary.total }) }}
        </div>
      </div>

      <div class="space-y-4">
        <BidComparisonPanel
          v-for="item in auction.items"
          :key="item.id"
          :item="item"
          :bids="comparisons[item.id] ?? []"
          :auction-status="auction.status"
          :currency-code="auction.currencyCode"
          :awarded-bid-id="awardedBidPerItem[item.id] ?? null"
          @award="(bidId) => award(item.id, bidId)"
        />
      </div>
    </template>

    <!-- ============================================================ -->
    <!-- LIVE / DRAFT / SCHEDULED MODE                               -->
    <!-- ============================================================ -->
    <template v-else>
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <!-- Items -->
        <div class="lg:col-span-2 space-y-4">
          <AppCard
            v-for="item in auction.items"
            :key="item.id"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0">
                <div class="font-semibold text-slate-800">{{ item.description }}</div>
                <div class="mt-0.5 text-xs text-slate-500">
                  {{ item.quantity }} {{ unitLabel(item.unitOfMeasure) }} · {{ item.bidCount }} {{ $t("auctions.detail.bidCount") }}
                </div>
              </div>
              <div class="text-right shrink-0">
                <div class="text-xs text-slate-500">{{ $t("auctions.detail.currentBest") }}</div>
                <div class="font-mono text-lg font-bold text-emerald-600">
                  {{ item.currentBestAmount != null ? item.currentBestAmount.toLocaleString() : "—" }}
                </div>
              </div>
            </div>

            <button
              class="mt-3 flex items-center gap-1.5 text-xs font-medium text-indigo-600 hover:text-indigo-700"
              @click="loadComparison(item.id)"
            >
              <DocumentMagnifyingGlassIcon class="h-3.5 w-3.5" />
              {{ $t("auctions.detail.compareBids") }}
            </button>

            <div v-if="comparisons[item.id]" class="mt-3 overflow-x-auto">
              <table class="w-full text-xs">
                <thead>
                  <tr class="border-b border-slate-100 text-left uppercase text-slate-400">
                    <th class="pb-1.5">{{ $t("auctions.detail.compareTable.company") }}</th>
                    <th class="pb-1.5">{{ $t("auctions.detail.compareTable.amount") }}</th>
                    <th class="pb-1.5">{{ $t("auctions.detail.compareTable.normalized") }}</th>
                    <th class="pb-1.5">{{ $t("common.status") }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="b in comparisons[item.id]" :key="b.bidId" class="border-b border-slate-50">
                    <td class="py-1.5">{{ b.companyName }}</td>
                    <td class="py-1.5 font-mono">{{ b.amount }} {{ b.currencyCode }}</td>
                    <td class="py-1.5 font-mono">{{ b.normalizedAmountInBaseCurrency.toFixed(2) }}</td>
                    <td class="py-1.5">
                      <AppBadge tone="slate">{{ $t(`enums.bidStatus.${b.status}`) }}</AppBadge>
                    </td>
                  </tr>
                </tbody>
              </table>
              <EmptyState v-if="comparisons[item.id].length === 0">{{ $t("auctions.detail.emptyBids") }}</EmptyState>
            </div>
          </AppCard>

          <!-- Add item form (Draft only) -->
          <form
            v-if="auction.status === 'Draft'"
            class="rounded-xl border border-dashed border-slate-300 bg-white p-5 shadow-sm"
            @submit.prevent="addItem"
          >
            <h3 class="text-sm font-semibold text-slate-700">{{ $t("auctions.detail.addItem") }}</h3>
            <div class="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-4">
              <input
                v-model="newItem.description"
                :placeholder="$t('auctions.detail.form.description')"
                required
                class="sm:col-span-2 rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-400 focus:outline-none focus:ring-1 focus:ring-indigo-400"
              />
              <input
                v-model.number="newItem.quantity"
                type="number"
                min="0.01"
                step="0.01"
                :placeholder="$t('auctions.detail.form.quantity')"
                required
                class="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-400 focus:outline-none focus:ring-1 focus:ring-indigo-400"
              />
              <select v-model="newItem.unitOfMeasure" class="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-400 focus:outline-none focus:ring-1 focus:ring-indigo-400">
                <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ unitLabel(u) }}</option>
              </select>
            </div>
            <div class="mt-3 flex items-center gap-3">
              <input
                v-model.number="newItem.targetPrice"
                type="number"
                min="0"
                step="0.01"
                :placeholder="$t('auctions.detail.form.targetPrice')"
                class="w-40 rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-400 focus:outline-none focus:ring-1 focus:ring-indigo-400"
              />
              <AppButton type="submit" variant="secondary">
                <PlusIcon class="h-4 w-4" />
                {{ $t("auctions.detail.add") }}
              </AppButton>
            </div>
          </form>
        </div>

        <!-- Sidebar: invitations + live log -->
        <div class="space-y-4">
          <AppCard :title="$t('auctions.detail.invitedCompanies')">
            <ul v-if="auction.invitations.length > 0" class="divide-y divide-slate-50 -mx-5 -mb-5 mt-1">
              <li
                v-for="inv in auction.invitations"
                :key="inv.id"
                class="flex items-center justify-between px-5 py-2.5"
              >
                <span class="min-w-0 text-sm text-slate-700">
                  <span class="block truncate">{{ inv.companyName }}</span>
                  <span v-if="inv.origin !== 'Direct'" class="block text-[11px] text-slate-400">{{ $t(`auctions.detail.origin.${inv.origin}`) }}</span>
                </span>
                <AppBadge tone="slate">{{ $t(`enums.invitationStatus.${inv.status}`) }}</AppBadge>
              </li>
            </ul>
            <EmptyState v-else class="mt-3">{{ $t("auctions.detail.emptyInvites") }}</EmptyState>

            <div v-if="['Draft', 'Scheduled'].includes(auction.status)" class="mt-4 flex gap-2">
              <select
                v-model="inviteCompanyId"
                class="flex-1 rounded-lg border border-slate-300 px-2.5 py-1.5 text-sm focus:border-indigo-400 focus:outline-none"
              >
                <option value="" disabled>{{ $t("auctions.detail.selectCompany") }}</option>
                <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
              </select>
              <AppButton @click="invite">
                <UserPlusIcon class="h-4 w-4" />
                {{ $t("auctions.detail.invite") }}
              </AppButton>
            </div>
          </AppCard>

          <InviteLinksPanel :auction-id="auctionId" :companies="companies" :status="auction.status" @created="load" />

          <AppCard :title="$t('auctions.detail.liveLog')">
            <ul v-if="liveLog.length > 0" class="space-y-1">
              <li v-for="(entry, i) in liveLog" :key="i" class="flex items-start gap-1.5 text-xs text-slate-500">
                <BoltIcon class="mt-0.5 h-3 w-3 shrink-0 text-amber-400" />
                {{ entry }}
              </li>
            </ul>
            <EmptyState v-else class="mt-3">{{ $t("auctions.detail.emptyLog") }}</EmptyState>
          </AppCard>
        </div>
      </div>
    </template>
  </div>
</template>
