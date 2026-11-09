<script setup lang="ts">
import type {
  AuctionDetailDto,
  AuctionFeedEntryDto,
  AuctionItemDto,
  BidComparisonDto,
  BidDto,
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
  PencilSquareIcon,
  TrashIcon,
  ArrowDownTrayIcon,
} from "@heroicons/vue/24/outline";
import { CheckCircleIcon as CheckCircleSolid } from "@heroicons/vue/24/solid";

const route = useRoute();
const auctionId = route.params.id as string;

const api = useApi();
const accessState = ref<"forbidden" | "notFound" | null>(null);
const toast = useToast();
const { t } = useI18n();
const { formatDate, formatDateTime, formatDuration, formatMoney } = useDateFormat();
const { connectAndJoin, disconnect } = useAuctionHub();
const { isAdminRole } = useAuth();
const { can } = usePermissions();

// Bölüm gezintisi (sayfada var olan bölümler gösterilir).
const sectionNavItems = computed(() => [
  { id: "status", label: t("sectionNav.status") },
  { id: "items", label: isEvaluationStage.value ? t("sectionNav.evaluation") : t("sectionNav.items"), count: auction.value?.items.length },
  { id: "orders", label: t("sectionNav.orders") },
  { id: "participation", label: t("sectionNav.participation") },
  { id: "terms", label: t("sectionNav.terms") },
  { id: "qa", label: t("sectionNav.qa") },
]);

// ---- Sonuçtan sipariş: kalem bazlı kazananlar → tedarikçi başına bir sipariş taslağı ----
const orderList = ref<{ reload: () => Promise<void> } | null>(null);
const creatingOrders = ref(false);
async function createOrders() {
  creatingOrders.value = true;
  try {
    const ids = await api.post<string[]>(`/api/auctions/${auctionId}/purchase-orders`);
    toast.success(t("orders.createdToast", { n: ids.length }));
    if (ids.length === 1) await navigateTo(`/admin/orders/${ids[0]}`);
    else await orderList.value?.reload();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("orders.actionError")));
  } finally {
    creatingOrders.value = false;
  }
}

const auction = ref<AuctionDetailDto | null>(null);
const loading = ref(true);
const now = ref(Date.now());
const liveLog = ref<string[]>([]);

const comparisons = reactive<Record<string, BidComparisonDto[]>>({});
const inviteCompanyId = ref<string | null>(null);
const inviteCompanyName = ref<string | null>(null);
const { loadSupplierRows } = useSupplierLookup();
const newItem = reactive({ description: "", quantity: 1, unitOfMeasure: "Piece", targetPrice: null as number | null });

// ---- Taslak düzenleme, kalem düzenle/sil, davet geri alma, Excel dışa aktarma ----
const showDraftEditor = ref(false);
const editingItemId = ref<string | null>(null);
const itemForm = reactive({ description: "", quantity: 1, unitOfMeasure: "Piece", targetPrice: null as number | null, requestedDeliveryDate: "", specification: "" });
const pendingDelete = ref<AuctionItemDto | null>(null);
const pendingRevoke = ref<{ id: string; companyName: string } | null>(null);
const busyEdit = ref(false);
const { exportRows } = useExcelExport();

function startEditItem(item: AuctionItemDto) {
  editingItemId.value = item.id;
  Object.assign(itemForm, {
    description: item.description, quantity: item.quantity, unitOfMeasure: item.unitOfMeasure, targetPrice: item.targetPrice,
    requestedDeliveryDate: item.requestedDeliveryDate ? item.requestedDeliveryDate.slice(0, 10) : "", specification: item.specification ?? "",
  });
}

async function saveItem() {
  if (!editingItemId.value) return;
  busyEdit.value = true;
  await runAction(async () => {
    await api.put(`/api/auctions/${auctionId}/items/${editingItemId.value}`, {
      ...itemForm,
      requestedDeliveryDate: itemForm.requestedDeliveryDate ? `${itemForm.requestedDeliveryDate}T12:00:00Z` : null,
      specification: itemForm.specification || null,
    });
    editingItemId.value = null;
    toast.success(t("auctions.edit.itemSaved"));
    await load();
  });
  busyEdit.value = false;
}

async function confirmDeleteItem() {
  const item = pendingDelete.value;
  if (!item) return;
  busyEdit.value = true;
  await runAction(async () => {
    await api.delete(`/api/auctions/${auctionId}/items/${item.id}`);
    toast.success(t("auctions.edit.itemDeleted"));
    await load();
  });
  pendingDelete.value = null;
  busyEdit.value = false;
}

async function confirmRevoke() {
  const inv = pendingRevoke.value;
  if (!inv) return;
  busyEdit.value = true;
  await runAction(async () => {
    await api.delete(`/api/auctions/${auctionId}/invitations/${inv.id}`);
    toast.success(t("auctions.edit.invitationRevoked"));
    await load();
  });
  pendingRevoke.value = null;
  busyEdit.value = false;
}

// Tüm kalemlerin tüm teklifleri tek sayfada (karşılaştırmalar yüklü değilse önce yüklenir).
async function exportBids() {
  if (!auction.value) return;
  await loadAllComparisons(auction.value.items);
  const rows = auction.value.items.flatMap((it) => (comparisons[it.id] ?? []).map((b) => ({ it, b })));
  exportRows(`${auction.value.referenceCode ?? "ihale"}-teklifler`, t("auctions.export.sheet"), [
    { header: t("auctions.export.item"), value: (r) => r.it.description, width: 40 },
    { header: t("auctions.export.quantity"), value: (r) => r.it.quantity },
    { header: t("auctions.export.company"), value: (r) => r.b.companyName, width: 34 },
    { header: t("auctions.export.unitPrice"), value: (r) => r.b.amount },
    { header: t("auctions.export.total"), value: (r) => r.b.amount * r.it.quantity },
    { header: t("auctions.export.currency"), value: (r) => r.b.currencyCode },
    { header: t("auctions.export.normalized"), value: (r) => r.b.normalizedAmountInBaseCurrency },
    { header: t("auctions.export.bidTime"), value: (r) => formatDateTime(r.b.bidTime), width: 18 },
    { header: t("auctions.export.status"), value: (r) => (r.b.isAwarded ? t("auctions.evaluation.winner") : t(`enums.bidStatus.${r.b.status}`)) },
  ], rows);
}

// Kazanan seçimi geri alınamaz: önce onay penceresi.
const pendingAward = ref<{ item: AuctionItemDto; bid: BidComparisonDto } | null>(null);
const awarding = ref(false);

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

const timeLeftLabel = computed(() => (auction.value ? formatDuration(new Date(auction.value.endAt).getTime() - now.value) : ""));
const startsInLabel = computed(() => (auction.value ? formatDuration(new Date(auction.value.startAt).getTime() - now.value) : ""));

// Whether this auction is in the "evaluate" stage
const isEvaluationStage = computed(() =>
  auction.value ? ["Ended", "Awarded"].includes(auction.value.status) : false,
);

const evaluationSummary = computed(() => {
  if (!isEvaluationStage.value || !auction.value) return null;
  const items = auction.value.items;
  const total = items.length;
  const awarded = items.filter((it) => comparisons[it.id]?.some((b) => b.isAwarded)).length;
  return { total, awarded };
});

async function load() {
  loading.value = true;
  try {
    const detail = await api.get<AuctionDetailDto>(`/api/auctions/${auctionId}`);
    auction.value = detail;

    // Auto-load all comparisons when in evaluation stage
    if (["Ended", "Awarded"].includes(detail.status)) {
      await loadAllComparisons(detail.items);
    }
  } catch (error: any) {
    // Yetki yoksa / kayıt yoksa boş sayfa ve kaybolan bildirim yerine açıklayıcı durum gösterilir.
    const code = error?.response?.status ?? error?.statusCode;
    if (code === 403 || code === 404) accessState.value = code === 403 ? "forbidden" : "notFound";
    else toast.error(apiErrorMessage(error, t("auctions.detail.loadError")));
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
    inviteCompanyId.value = null;
    inviteCompanyName.value = null;
    toast.success(t("auctions.detail.invitedToast"));
    await load();
  });
}

async function loadComparison(itemId: string) {
  await runAction(async () => {
    comparisons[itemId] = await api.get<BidComparisonDto[]>(`/api/auctions/${auctionId}/items/${itemId}/bids/compare`);
  });
}

async function confirmAward() {
  const p = pendingAward.value;
  if (!p) return;
  awarding.value = true;
  await runAction(async () => {
    await api.post(`/api/auctions/${auctionId}/items/${p.item.id}/award`, { winningBidId: p.bid.bidId, notes: null });
    toast.success(t("auctions.detail.winnerSelectedToast"));
    pendingAward.value = null;
    await load();
    await loadComparison(p.item.id);
  });
  awarding.value = false;
}

// Canlı akış sayfa açılınca son tekliflerle dolar; sonrası SignalR ile gelir.
function feedLine(company: string | null, amount: number | null, item: string) {
  return t("auctions.detail.log.newBidFrom", {
    company: company || t("auctions.live.feed.someone"),
    amount: formatMoney(amount, auction.value?.currencyCode),
    item,
  });
}

async function loadFeed() {
  try {
    const rows = await api.get<AuctionFeedEntryDto[]>(`/api/auctions/${auctionId}/feed`);
    liveLog.value = rows.map((r) => `${formatDateTime(r.bidTime)} · ${feedLine(r.companyName, r.amount, r.itemDescription)}`);
  } catch {
    // akış geçmişi yardımcı bilgidir; hata sayfayı bozmasın
  }
}

let ticker: ReturnType<typeof setInterval> | null = null;

onMounted(async () => {
  await load();
  if (!auction.value) return;
  await loadFeed();

  ticker = setInterval(() => {
    now.value = Date.now();
  }, 1000);

  const conn = await connectAndJoin(auctionId);

  conn.on("BidPlaced", (bid: BidDto) => {
    const item = auction.value?.items.find((i) => i.id === bid.auctionItemId)?.description ?? "";
    liveLog.value = [`${formatDateTime(bid.bidTime)} · ${feedLine(bid.companyName, bid.amount, item)}`, ...liveLog.value].slice(0, 30);
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
  <AccessState v-if="accessState" :kind="accessState" back-to="/admin/auctions" :back-label="$t('nav.auctions')" />
  <LoadingSpinner v-else-if="loading && !auction" />

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
        <!-- İşyeri + sorumlu satınalmacı (katılım talepleri sorumluya düşer); düzenlenebilir -->
        <AuctionAssignment :auction="auction" @changed="load" />
        <button
          v-if="auction.status === 'Draft'"
          type="button"
          class="mt-2 inline-flex items-center gap-1 text-xs font-medium text-indigo-600 hover:underline"
          data-testid="edit-draft"
          @click="showDraftEditor = true"
        >
          <PencilSquareIcon class="h-3.5 w-3.5" />{{ $t("auctions.edit.open") }}
        </button>
        <AuctionDraftEditor :auction="auction" :open="showDraftEditor" @close="showDraftEditor = false" @saved="showDraftEditor = false; load()" />
        <StepBadge v-if="workflowStep" class="mt-2" :step="workflowStep.step" :total="4" :label="workflowStep.label" />
      </div>
      <AppBadge :tone="statusTone[auction.status]" class="mt-1">
        {{ $t(`enums.auctionStatus.${auction.status}`) }}
      </AppBadge>
    </div>

    <!-- KPI row -->
    <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
      <div class="rounded-xl border border-slate-200 bg-surface p-4 shadow-sm">
        <div class="flex items-center gap-2 text-xs text-slate-500">
          <ArrowsRightLeftIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.detail.type") }}
        </div>
        <div class="mt-1 font-semibold text-slate-800">{{ $t(`enums.auctionType.${auction.auctionType}`) }}</div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-surface p-4 shadow-sm">
        <div class="flex items-center gap-2 text-xs text-slate-500">
          <CurrencyDollarIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.detail.currency") }}
        </div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.currencyCode }}</div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-surface p-4 shadow-sm">
        <div class="flex items-center gap-2 text-xs text-slate-500">
          <ClockIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.detail.timeLeft") }}
        </div>
        <div class="mt-1 font-mono font-semibold" :class="auction.status === 'Live' ? 'text-emerald-600' : auction.status === 'Paused' ? 'text-amber-600' : 'text-slate-500'">
          {{ auction.status === "Live" ? timeLeftLabel : auction.status === "Paused" ? $t("enums.auctionStatus.Paused") : auction.status === "Scheduled" ? $t("auctions.detail.startsIn", { time: startsInLabel }) : formatDateTime(auction.endAt) }}
        </div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-surface p-4 shadow-sm">
        <div class="flex items-center gap-2 text-xs text-slate-500">
          <BoltIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.detail.extensionRule") }}
        </div>
        <div class="mt-1 font-semibold text-slate-800">{{ auction.extensionRuleMinutes }} {{ $t("auctions.detail.minutesShort") }}</div>
      </div>
    </div>


    <!-- Uzun sayfada bölümlere atlama (yapışkan) -->
    <SectionNav :sections="sectionNavItems" />

    <!-- Durum yönetimi: yayınla / başlat / duraklat / devam / bitir / yeniden aç / iptal + süre uzatma + geçmiş -->
    <div id="status" class="scroll-mt-28">
      <AuctionStatusPanel :auction-id="auctionId" :status="auction.status" :can-manage="isAdminRole() || can('auctions.manage')" :is-admin="isAdminRole()" @changed="load" />
    </div>

    <div id="items" class="scroll-mt-28 space-y-6">


    <!-- ============================================================ -->
    <!-- EVALUATION MODE: Ended / Awarded                            -->
    <!-- ============================================================ -->
    <template v-if="isEvaluationStage">
      <div class="rounded-xl border border-indigo-200 bg-indigo-50 px-5 py-4">
        <div class="flex items-center gap-2">
          <TrophyIcon class="h-5 w-5 text-indigo-500" />
          <span class="font-semibold text-indigo-800">{{ $t("auctions.evaluation.heading") }}</span>
          <button type="button" class="ml-auto inline-flex items-center gap-1 rounded-md border border-indigo-200 bg-surface px-2.5 py-1 text-xs font-medium text-indigo-700 hover:bg-indigo-100" data-testid="export-bids" @click="exportBids">
            <ArrowDownTrayIcon class="h-4 w-4" />{{ $t("auctions.export.button") }}
          </button>
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
          :auction-type="auction.auctionType"
          :currency-code="auction.currencyCode"
          @award="(bid) => (pendingAward = { item, bid })"
        />
      </div>

      <AppCard v-if="evaluationSummary && evaluationSummary.awarded > 0" id="orders" class="scroll-mt-28" :title="$t('orders.sectionTitle')" :description="$t('orders.sectionHint')">
        <div class="space-y-3" data-testid="auction-orders">
          <AppButton v-if="can('orders.manage')" :loading="creatingOrders" data-testid="create-orders" @click="createOrders">{{ $t("orders.createFromAuction") }}</AppButton>
          <PurchaseOrderList ref="orderList" internal :source-id="auctionId" compact />
        </div>
      </AppCard>

      <ConfirmDialog
        :open="!!pendingAward"
        :title="$t('auctions.evaluation.confirmTitle')"
        :confirm-label="$t('auctions.evaluation.selectWinner')"
        :loading="awarding"
        @confirm="confirmAward"
        @cancel="pendingAward = null"
      >
        <template v-if="pendingAward">
          <p>{{ $t("auctions.evaluation.confirmBody", { company: pendingAward.bid.companyName, item: pendingAward.item.description }) }}</p>
          <p class="mt-2 font-mono text-slate-800">
            {{ formatMoney(pendingAward.bid.amount, pendingAward.bid.currencyCode) }} × {{ pendingAward.item.quantity.toLocaleString() }}
            = {{ formatMoney(pendingAward.bid.amount * pendingAward.item.quantity, pendingAward.bid.currencyCode) }}
          </p>
          <p class="mt-2 text-xs text-amber-700">{{ $t("auctions.evaluation.confirmIrreversible") }}</p>
        </template>
      </ConfirmDialog>
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
                  {{ item.quantity.toLocaleString() }} {{ unitLabel(item.unitOfMeasure) }} · {{ item.bidCount }} {{ $t("auctions.detail.bidCount") }}
                  <template v-if="item.targetPrice"> · {{ $t("auctions.evaluation.targetUnit") }}: {{ formatMoney(item.targetPrice, auction.currencyCode) }}</template>
                </div>
                <div v-if="item.requestedDeliveryDate || item.specification" class="mt-1 space-y-0.5 text-xs text-slate-500">
                  <div v-if="item.requestedDeliveryDate">{{ $t("auctions.terms.requestedDelivery") }}: {{ formatDate(item.requestedDeliveryDate) }}</div>
                  <div v-if="item.specification" class="whitespace-pre-line">{{ $t("auctions.terms.specification") }}: {{ item.specification }}</div>
                </div>
              </div>
              <div class="text-right shrink-0">
                <div class="text-xs text-slate-500">{{ $t("auctions.detail.currentBestUnit") }}</div>
                <div class="font-mono text-lg font-bold text-emerald-600">
                  {{ item.currentBestAmount != null ? formatMoney(item.currentBestAmount, auction.currencyCode) : "—" }}
                </div>
                <div v-if="item.currentBestAmount != null" class="text-[11px] text-slate-400">
                  {{ $t("auctions.evaluation.lineTotal") }}: {{ formatMoney(item.currentBestAmount * item.quantity, auction.currencyCode) }}
                </div>
              </div>
            </div>

            <!-- Taslakta kalem düzenlenir / silinir -->
            <form v-if="editingItemId === item.id" class="mt-3 grid grid-cols-1 gap-2 rounded-lg border border-slate-200 bg-slate-50 p-3 sm:grid-cols-4" data-testid="item-editor" @submit.prevent="saveItem">
              <input v-model="itemForm.description" required :aria-label="$t('auctions.detail.form.description')" :placeholder="$t('auctions.detail.form.description')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm sm:col-span-4" />
              <input v-model.number="itemForm.quantity" type="number" min="0.01" step="0.01" required :aria-label="$t('auctions.detail.form.quantity')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
              <select v-model="itemForm.unitOfMeasure" :aria-label="$t('auctions.edit.unit')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
                <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ unitLabel(u) }}</option>
              </select>
              <input v-model.number="itemForm.targetPrice" type="number" min="0" step="0.01" :aria-label="$t('auctions.evaluation.targetUnit')" :placeholder="$t('auctions.evaluation.targetUnit')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
              <input v-model="itemForm.requestedDeliveryDate" type="date" :aria-label="$t('auctions.terms.requestedDelivery')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
              <textarea v-model="itemForm.specification" rows="2" :aria-label="$t('auctions.terms.specification')" :placeholder="$t('auctions.terms.specification')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm sm:col-span-4" />
              <div class="flex justify-end gap-2 sm:col-span-4">
                <button type="button" class="rounded-md px-3 py-1.5 text-xs text-slate-600 hover:bg-slate-100" @click="editingItemId = null">{{ $t("common.cancel") }}</button>
                <AppButton type="submit" :loading="busyEdit">{{ $t("common.save") }}</AppButton>
              </div>
            </form>
            <div class="mt-3 flex flex-wrap items-center gap-4">
              <button
                class="flex items-center gap-1.5 text-xs font-medium text-indigo-600 hover:text-indigo-700"
                @click="loadComparison(item.id)"
              >
                <DocumentMagnifyingGlassIcon class="h-3.5 w-3.5" />
                {{ $t("auctions.detail.compareBids") }}
              </button>
              <template v-if="auction.status === 'Draft' && editingItemId !== item.id">
                <button type="button" class="flex items-center gap-1 text-xs font-medium text-slate-600 hover:text-indigo-700" data-testid="edit-item" @click="startEditItem(item)">
                  <PencilSquareIcon class="h-3.5 w-3.5" />{{ $t("common.edit") }}
                </button>
                <button type="button" class="flex items-center gap-1 text-xs font-medium text-rose-600 hover:text-rose-700" data-testid="delete-item" @click="pendingDelete = item">
                  <TrashIcon class="h-3.5 w-3.5" />{{ $t("common.delete") }}
                </button>
              </template>
            </div>

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
                    <td class="py-1.5 font-mono">{{ formatMoney(b.amount, b.currencyCode) }}</td>
                    <td class="py-1.5 font-mono">{{ formatMoney(b.normalizedAmountInBaseCurrency) }}</td>
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
            class="rounded-xl border border-dashed border-slate-300 bg-surface p-5 shadow-sm"
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
                <span class="flex shrink-0 items-center gap-2">
                  <AppBadge tone="slate">{{ $t(`enums.invitationStatus.${inv.status}`) }}</AppBadge>
                  <button
                    v-if="inv.status === 'Invited' && ['Draft', 'Scheduled'].includes(auction.status)"
                    type="button"
                    class="text-xs font-medium text-rose-600 hover:underline"
                    data-testid="revoke-invitation"
                    @click="pendingRevoke = { id: inv.id, companyName: inv.companyName }"
                  >{{ $t("auctions.edit.revoke") }}</button>
                </span>
              </li>
            </ul>
            <EmptyState v-else class="mt-3">{{ $t("auctions.detail.emptyInvites") }}</EmptyState>

            <div v-if="['Draft', 'Scheduled'].includes(auction.status)" class="mt-4 flex gap-2">
              <LookupField
                v-model="inviteCompanyId"
                class="min-w-0 flex-1"
                :load="loadSupplierRows"
                :title="$t('auctions.detail.selectCompany')"
                :display="inviteCompanyName"
                :placeholder="$t('auctions.detail.selectCompany')"
                data-testid="invite-company"
                @select="(r) => (inviteCompanyName = r ? `${r.code} · ${r.name}` : null)"
              />
              <AppButton :disabled="!inviteCompanyId" @click="invite">
                <UserPlusIcon class="h-4 w-4" />
                {{ $t("auctions.detail.invite") }}
              </AppButton>
            </div>
          </AppCard>

          <InviteLinksPanel :auction-id="auctionId" :status="auction.status" @created="load" />

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

    <!-- Davetsiz tedarikçilerin katılım talepleri: cari onayı → ön teklif kabulü / red -->
    <!-- Durum değişince (ör. canlıya geçişte ön teklifler teklife dönüşür) panel yeniden yüklenir. -->
    <div v-if="auction.status !== 'Draft'" id="participation" class="scroll-mt-28">
      <ParticipationRequestsPanel :key="auction.status" :auction-id="auctionId" @changed="load" />
    </div>

    <div id="terms" class="scroll-mt-28">
      <AuctionTermsCard :auction="auction" internal @changed="load" />

      <ErpStatusCard v-if="auction.status !== 'Draft'" :auction-id="auctionId" />
    </div>

    <div id="qa-section" class="scroll-mt-28">
      <ProcessQaPanel v-if="auction.status !== 'Draft'" kind="auction" :process-id="auctionId" internal />
    </div>

    <ConfirmDialog
      :open="!!pendingDelete"
      :title="$t('auctions.edit.deleteItemTitle')"
      :confirm-label="$t('common.delete')"
      tone="danger"
      :loading="busyEdit"
      @confirm="confirmDeleteItem"
      @cancel="pendingDelete = null"
    >{{ $t("auctions.edit.deleteItemBody", { item: pendingDelete?.description ?? "" }) }}</ConfirmDialog>
    <ConfirmDialog
      :open="!!pendingRevoke"
      :title="$t('auctions.edit.revokeTitle')"
      :confirm-label="$t('auctions.edit.revoke')"
      tone="danger"
      :loading="busyEdit"
      @confirm="confirmRevoke"
      @cancel="pendingRevoke = null"
    >{{ $t("auctions.edit.revokeBody", { company: pendingRevoke?.companyName ?? "" }) }}</ConfirmDialog>
  </div>
</template>
