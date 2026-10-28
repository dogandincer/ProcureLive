<script setup lang="ts">
import type { MonitoringStatsDto } from "~/types/api";
import {
  ArrowPathIcon,
  BoltIcon,
  ChartBarIcon,
  ClockIcon,
  CursorArrowRaysIcon,
} from "@heroicons/vue/24/outline";

const api = useApi();
const { formatMoney } = useDateFormat();
const toast = useToast();
const { t, locale } = useI18n();

const stats = ref<MonitoringStatsDto | null>(null);
const loading = ref(true);
const lastUpdated = ref<Date | null>(null);

// Chart refs
const auctionChartRef = ref<HTMLCanvasElement | null>(null);
const rfqChartRef = ref<HTMLCanvasElement | null>(null);
let auctionChart: import("chart.js").Chart | null = null;
let rfqChart: import("chart.js").Chart | null = null;

// Keys must match the backend AuctionStatus enum names exactly.
const AUCTION_STATUS_COLORS: Record<string, string> = {
  Draft: "#94a3b8",
  Scheduled: "#f59e0b",
  Live: "#10b981",
  Paused: "#fb923c",
  Ended: "#64748b",
  Cancelled: "#f43f5e",
  Awarded: "#6366f1",
};

// Keys must match the backend ProcessStatus enum names exactly (RFI/RFQ share it).
const RFQ_STATUS_COLORS: Record<string, string> = {
  Draft: "#94a3b8",
  Sent: "#f59e0b",
  InProgress: "#3b82f6",
  Closed: "#64748b",
  Evaluated: "#8b5cf6",
  Awarded: "#6366f1",
};

async function load() {
  loading.value = true;
  try {
    stats.value = await api.get<MonitoringStatsDto>("/api/monitoring/stats");
    lastUpdated.value = new Date();
    await nextTick();
    renderCharts();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("monitoring.loadError")));
  } finally {
    loading.value = false;
  }
}

async function renderCharts() {
  const { Chart, ArcElement, DoughnutController, Tooltip, Legend } = await import("chart.js");
  Chart.register(ArcElement, DoughnutController, Tooltip, Legend);

  if (stats.value && auctionChartRef.value) {
    const auctionData = stats.value.auctionCountByStatus;
    const keys = Object.keys(auctionData);
    const labels = keys.map((k) => t(`enums.auctionStatus.${k}`));
    const data = keys.map((k) => auctionData[k]);
    const colors = keys.map((k) => AUCTION_STATUS_COLORS[k] ?? "#cbd5e1");

    if (auctionChart) auctionChart.destroy();
    auctionChart = new Chart(auctionChartRef.value, {
      type: "doughnut",
      data: { labels, datasets: [{ data, backgroundColor: colors, borderWidth: 2, borderColor: "#fff" }] },
      options: {
        cutout: "65%",
        plugins: {
          legend: { position: "bottom", labels: { boxWidth: 10, font: { size: 11 } } },
          tooltip: { callbacks: { label: (ctx) => ` ${ctx.label}: ${ctx.parsed}` } },
        },
      },
    });
  }

  if (stats.value && rfqChartRef.value) {
    const rfqData = stats.value.rfqCountByStatus;
    const keys = Object.keys(rfqData);
    const labels = keys.map((k) => t(`enums.processStatus.${k}`));
    const data = keys.map((k) => rfqData[k]);
    const colors = keys.map((k) => RFQ_STATUS_COLORS[k] ?? "#cbd5e1");

    if (rfqChart) rfqChart.destroy();
    rfqChart = new Chart(rfqChartRef.value, {
      type: "doughnut",
      data: { labels, datasets: [{ data, backgroundColor: colors, borderWidth: 2, borderColor: "#fff" }] },
      options: {
        cutout: "65%",
        plugins: {
          legend: { position: "bottom", labels: { boxWidth: 10, font: { size: 11 } } },
          tooltip: { callbacks: { label: (ctx) => ` ${ctx.label}: ${ctx.parsed}` } },
        },
      },
    });
  }
}

function timeUntil(dateStr: string): string {
  const diff = new Date(dateStr).getTime() - Date.now();
  if (diff <= 0) return t("monitoring.ended");
  const hours = Math.floor(diff / 3600000);
  const minutes = Math.floor((diff % 3600000) / 60000);
  const h = t("monitoring.hoursShort");
  const m = t("monitoring.minutesShort");
  if (hours > 0) return `${hours}${h} ${minutes}${m}`;
  return `${minutes}${m}`;
}

function timeSince(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const minutes = Math.floor(diff / 60000);
  const m = t("monitoring.minutesShort");
  if (minutes < 1) return t("monitoring.justNow");
  if (minutes < 60) return `${minutes}${m} ${t("monitoring.ago")}`;
  const hours = Math.floor(minutes / 60);
  return `${hours}${t("monitoring.hoursShort")} ${t("monitoring.ago")}`;
}

const totalAuctions = computed(() => {
  if (!stats.value) return 0;
  return Object.values(stats.value.auctionCountByStatus).reduce((a, b) => a + b, 0);
});

const totalRfq = computed(() => {
  if (!stats.value) return 0;
  return Object.values(stats.value.rfqCountByStatus).reduce((a, b) => a + b, 0);
});

// Auto-refresh every 30s
let refreshTimer: ReturnType<typeof setInterval> | null = null;

// Re-render charts (labels are localized) when the language changes.
watch(locale, () => {
  if (stats.value) renderCharts();
});

onMounted(() => {
  load();
  refreshTimer = setInterval(load, 30000);
});

onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer);
  auctionChart?.destroy();
  rfqChart?.destroy();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("monitoring.title") }}</h1>
        <p class="mt-0.5 text-sm text-slate-500">
          {{ $t("monitoring.subtitle") }}
          <span v-if="lastUpdated" class="text-slate-400"> · {{ $t("monitoring.lastUpdated") }}: {{ lastUpdated.toLocaleTimeString() }}</span>
        </p>
      </div>
      <button
        :disabled="loading"
        class="flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-50 disabled:opacity-50 transition-colors"
        @click="load"
      >
        <ArrowPathIcon class="h-4 w-4" :class="{ 'animate-spin': loading }" />
        {{ $t("monitoring.refresh") }}
      </button>
    </div>

    <!-- KPI row -->
    <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
      <AppStat
        :label="$t('monitoring.stat.liveAuctions')"
        :value="loading ? '…' : stats?.liveAuctions.length ?? 0"
        :subtitle="$t('monitoring.stat.liveAuctionsSub')"
        tone="emerald"
      >
        <template #icon><BoltIcon class="h-5 w-5" /></template>
      </AppStat>
      <AppStat
        :label="$t('monitoring.stat.bidsToday')"
        :value="loading ? '…' : stats?.totalBidsToday ?? 0"
        :subtitle="$t('monitoring.stat.bidsTodaySub')"
        tone="indigo"
      >
        <template #icon><CursorArrowRaysIcon class="h-5 w-5" /></template>
      </AppStat>
      <AppStat
        :label="$t('monitoring.stat.bidsWeek')"
        :value="loading ? '…' : stats?.totalBidsThisWeek ?? 0"
        :subtitle="$t('monitoring.stat.bidsWeekSub')"
        tone="amber"
      >
        <template #icon><ChartBarIcon class="h-5 w-5" /></template>
      </AppStat>
      <AppStat
        :label="$t('monitoring.stat.totalProcesses')"
        :value="loading ? '…' : totalAuctions + totalRfq"
        :subtitle="$t('monitoring.stat.totalProcessesSub')"
        tone="slate"
      >
        <template #icon><ClockIcon class="h-5 w-5" /></template>
      </AppStat>
    </div>

    <!-- Charts row -->
    <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
      <AppCard :title="$t('monitoring.charts.auctionStatus')" :description="$t('monitoring.charts.total', { count: totalAuctions })">
        <div v-if="loading || !stats || Object.keys(stats.auctionCountByStatus).length === 0" class="flex h-48 items-center justify-center">
          <LoadingSpinner v-if="loading" />
          <EmptyState v-else>{{ $t("monitoring.charts.noData") }}</EmptyState>
        </div>
        <div v-else class="flex items-center justify-center py-2">
          <canvas ref="auctionChartRef" class="max-h-52 max-w-xs" />
        </div>
      </AppCard>

      <AppCard :title="$t('monitoring.charts.rfqStatus')" :description="$t('monitoring.charts.total', { count: totalRfq })">
        <div v-if="loading || !stats || Object.keys(stats.rfqCountByStatus).length === 0" class="flex h-48 items-center justify-center">
          <LoadingSpinner v-if="loading" />
          <EmptyState v-else>{{ $t("monitoring.charts.noData") }}</EmptyState>
        </div>
        <div v-else class="flex items-center justify-center py-2">
          <canvas ref="rfqChartRef" class="max-h-52 max-w-xs" />
        </div>
      </AppCard>
    </div>

    <!-- Live auctions -->
    <AppCard :title="$t('monitoring.liveAuctions.title')" noPadding>
      <template #action>
        <span class="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700">
          <span class="h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse" />
          {{ $t("monitoring.live") }}
        </span>
      </template>
      <LoadingSpinner v-if="loading" class="p-6" />
      <div v-else-if="!stats?.liveAuctions.length" class="p-8">
        <EmptyState>{{ $t("monitoring.liveAuctions.empty") }}</EmptyState>
      </div>
      <div v-else class="overflow-x-auto"><table class="w-full min-w-[560px] text-sm">
        <thead class="border-b border-slate-100 bg-slate-50">
          <tr>
            <th class="px-4 py-2.5 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.liveAuctions.table.title") }}</th>
            <th class="px-4 py-2.5 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.liveAuctions.table.bids") }}</th>
            <th class="px-4 py-2.5 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.liveAuctions.table.leading") }}</th>
            <th class="px-4 py-2.5 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.liveAuctions.table.bestBid") }}</th>
            <th class="px-4 py-2.5 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.liveAuctions.table.endsIn") }}</th>
            <th class="px-4 py-2.5"></th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-50">
          <tr v-for="a in stats!.liveAuctions" :key="a.auctionId" class="hover:bg-slate-50 transition-colors">
            <td class="px-4 py-3 font-medium text-slate-800">{{ a.title }}</td>
            <td class="px-4 py-3 text-center">
              <span class="inline-flex items-center rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-semibold text-indigo-700">
                {{ a.bidCount }}
              </span>
            </td>
            <td class="px-4 py-3 text-slate-600">{{ a.leadingCompanyName ?? "—" }}</td>
            <td class="px-4 py-3 text-right font-mono font-semibold text-emerald-700">
              {{ formatMoney(a.bestBidAmount, a.bestBidCurrencyCode) }}
            </td>
            <td class="px-4 py-3 text-right">
              <span class="rounded-md bg-amber-50 px-2 py-0.5 text-xs font-semibold text-amber-700">
                {{ timeUntil(a.endsAt) }}
              </span>
            </td>
            <td class="px-4 py-3 text-right">
              <NuxtLink :to="`/admin/auctions/${a.auctionId}`" class="text-xs font-medium text-indigo-600 hover:text-indigo-700">
                {{ $t("common.detail") }}
              </NuxtLink>
            </td>
          </tr>
        </tbody>
      </table></div>
    </AppCard>

    <!-- Recent bid activity -->
    <AppCard :title="$t('monitoring.recentBids.title')" :description="$t('monitoring.recentBids.subtitle')" noPadding>
      <LoadingSpinner v-if="loading" class="p-6" />
      <div v-else-if="!stats?.recentBids.length" class="p-8">
        <EmptyState>{{ $t("monitoring.recentBids.empty") }}</EmptyState>
      </div>
      <div v-else class="overflow-x-auto"><table class="w-full min-w-[560px] text-sm">
        <thead class="border-b border-slate-100 bg-slate-50">
          <tr>
            <th class="px-4 py-2.5 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.recentBids.table.company") }}</th>
            <th class="px-4 py-2.5 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.recentBids.table.auction") }}</th>
            <th class="px-4 py-2.5 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.recentBids.table.amount") }}</th>
            <th class="px-4 py-2.5 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("monitoring.recentBids.table.time") }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-50">
          <tr v-for="b in stats!.recentBids" :key="b.bidId" class="hover:bg-slate-50 transition-colors">
            <td class="px-4 py-2.5 font-medium text-slate-700">{{ b.companyName }}</td>
            <td class="px-4 py-2.5 text-slate-500 max-w-[180px] truncate">{{ b.auctionTitle }}</td>
            <td class="px-4 py-2.5 text-right font-mono font-semibold text-slate-800">
              {{ formatMoney(b.amount, b.currencyCode) }}
            </td>
            <td class="px-4 py-2.5 text-right text-slate-400 text-xs">{{ timeSince(b.bidTime) }}</td>
          </tr>
        </tbody>
      </table></div>
    </AppCard>
  </div>
</template>
