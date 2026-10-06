<script setup lang="ts">
import type { SupplierScoreboardDto } from "~/types/api";
import { TrophyIcon, StarIcon, BuildingOffice2Icon, ArrowPathIcon, ChartBarSquareIcon } from "@heroicons/vue/24/outline";

const api = useApi();
const toast = useToast();
const { t } = useI18n();

const data = ref<SupplierScoreboardDto | null>(null);
const loading = ref(true);

const sectorChartRef = ref<HTMLCanvasElement | null>(null);
let sectorChart: import("chart.js").Chart | null = null;

async function load() {
  loading.value = true;
  try {
    data.value = await api.get<SupplierScoreboardDto>("/api/companies/scoreboard", { topCount: 10 });
    await nextTick();
    renderSectorChart();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("scoreboard.loadError")));
  } finally {
    loading.value = false;
  }
}

async function renderSectorChart() {
  if (!data.value || !sectorChartRef.value) return;
  const sectors = data.value.popularSectors.filter((s) => s.companyCount > 0).slice(0, 8);
  if (sectors.length === 0) return;

  const { Chart, BarController, BarElement, CategoryScale, LinearScale, Tooltip } = await import("chart.js");
  Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip);

  const css = getComputedStyle(document.documentElement);
  const accent = css.getPropertyValue("--chart-accent").trim() || "#6366f1";
  const grid = css.getPropertyValue("--chart-grid").trim() || "#e2e8f0";
  const fg = css.getPropertyValue("--chart-fg").trim() || "#64748b";

  if (sectorChart) sectorChart.destroy();
  sectorChart = new Chart(sectorChartRef.value, {
    type: "bar",
    data: {
      labels: sectors.map((s) => s.sectorName),
      datasets: [{ label: t("scoreboard.companyCount"), data: sectors.map((s) => s.companyCount), backgroundColor: accent, borderRadius: 6, maxBarThickness: 44 }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: { display: false }, tooltip: { callbacks: { afterLabel: (ctx) => {
        const s = sectors[ctx.dataIndex];
        return `${t("scoreboard.evaluated")}: ${s.evaluatedCompanyCount}` + (s.averageScore != null ? ` · ${t("scoreboard.avg")}: ${s.averageScore.toFixed(2)}` : "");
      } } } },
      scales: {
        x: { grid: { display: false }, ticks: { color: fg, font: { size: 11 } } },
        y: { beginAtZero: true, grid: { color: grid }, ticks: { color: fg, precision: 0, font: { size: 11 } } },
      },
    },
  });
}

function scoreTone(score: number): string {
  if (score >= 4) return "ok";
  if (score >= 3) return "warn";
  return "bad";
}

const rankMedals = ["🥇", "🥈", "🥉"];

onMounted(load);
onUnmounted(() => sectorChart?.destroy());
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("scoreboard.title") }}</h1>
        <p class="mt-0.5 text-sm text-slate-500">{{ $t("scoreboard.subtitle") }}</p>
      </div>
      <button
        :disabled="loading"
        class="flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-50 disabled:opacity-50"
        @click="load"
      >
        <ArrowPathIcon class="h-4 w-4" :class="{ 'animate-spin': loading }" />
        {{ $t("scoreboard.refresh") }}
      </button>
    </div>

    <LoadingSpinner v-if="loading" class="p-10" />

    <template v-else-if="data">
      <!-- KPIs -->
      <div class="grid grid-cols-2 gap-4 sm:grid-cols-3">
        <AppStat :label="$t('scoreboard.evaluatedSuppliers')" :value="data.totalEvaluatedSuppliers" tone="indigo">
          <template #icon><BuildingOffice2Icon class="h-5 w-5" /></template>
        </AppStat>
        <AppStat :label="$t('scoreboard.totalEvaluations')" :value="data.totalEvaluations" tone="emerald">
          <template #icon><ChartBarSquareIcon class="h-5 w-5" /></template>
        </AppStat>
        <AppStat :label="$t('scoreboard.topScore')" :value="data.topSuppliers.length ? data.topSuppliers[0].averageScore.toFixed(2) : '—'" tone="amber">
          <template #icon><TrophyIcon class="h-5 w-5" /></template>
        </AppStat>
      </div>

      <!-- Top suppliers ranking -->
      <AppCard :title="$t('scoreboard.topSuppliers')" :description="$t('scoreboard.topSuppliersHint')">
        <div v-if="data.topSuppliers.length === 0" class="py-6">
          <EmptyState>{{ $t("scoreboard.noEvaluations") }}</EmptyState>
        </div>
        <ol v-else class="space-y-3">
          <li
            v-for="(s, i) in data.topSuppliers"
            :key="s.companyId"
            class="rounded-xl border border-slate-100 p-3.5"
            :class="{ 'border-amber-200 bg-amber-50/40': i === 0 }"
          >
            <div class="flex items-center justify-between gap-3">
              <div class="flex items-center gap-2.5 min-w-0">
                <span class="w-7 shrink-0 text-center text-sm font-bold text-slate-400">
                  <span v-if="i < 3" class="text-base">{{ rankMedals[i] }}</span>
                  <span v-else>{{ i + 1 }}</span>
                </span>
                <div class="min-w-0">
                  <NuxtLink :to="`/admin/companies/${s.companyId}`" class="truncate font-semibold text-slate-800 hover:text-indigo-600">{{ s.companyName }}</NuxtLink>
                  <div class="mt-0.5 flex flex-wrap gap-1">
                    <span v-for="sec in s.sectors" :key="sec" class="rounded-full bg-indigo-50 px-1.5 py-0.5 text-[10px] font-medium text-indigo-600">{{ sec }}</span>
                  </div>
                </div>
              </div>
              <div class="shrink-0 text-right">
                <div class="flex items-center gap-1" :class="{ 'text-emerald-600': scoreTone(s.averageScore)==='ok', 'text-amber-600': scoreTone(s.averageScore)==='warn', 'text-rose-600': scoreTone(s.averageScore)==='bad' }">
                  <StarIcon class="h-4 w-4 fill-current" />
                  <span class="text-lg font-bold tabular-nums">{{ s.averageScore.toFixed(2) }}</span>
                  <span class="text-xs text-slate-400">/5</span>
                </div>
                <div class="text-[11px] text-slate-400">{{ s.evaluationCount }} {{ $t("scoreboard.evalShort") }}</div>
              </div>
            </div>
            <!-- criterion mini-bars -->
            <div class="mt-3 grid grid-cols-5 gap-2">
              <div v-for="crit in [
                { k: 'quality', v: s.avgQuality },
                { k: 'delivery', v: s.avgDelivery },
                { k: 'price', v: s.avgPrice },
                { k: 'communication', v: s.avgCommunication },
                { k: 'compliance', v: s.avgCompliance },
              ]" :key="crit.k">
                <div class="mb-0.5 flex justify-between text-[10px] text-slate-500">
                  <span class="truncate">{{ $t(`scoreboard.criteria.${crit.k}`) }}</span>
                  <span class="tabular-nums font-medium">{{ crit.v.toFixed(1) }}</span>
                </div>
                <div class="h-1.5 rounded-full bg-slate-100">
                  <div class="h-1.5 rounded-full bg-indigo-400" :style="{ width: `${(crit.v / 5) * 100}%` }" />
                </div>
              </div>
            </div>
          </li>
        </ol>
      </AppCard>

      <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <!-- Popular sectors -->
        <AppCard :title="$t('scoreboard.popularSectors')" :description="$t('scoreboard.popularSectorsHint')">
          <div v-if="data.popularSectors.filter((s) => s.companyCount > 0).length === 0" class="py-6">
            <EmptyState>{{ $t("scoreboard.noSectors") }}</EmptyState>
          </div>
          <div v-else class="h-56"><canvas ref="sectorChartRef" /></div>
        </AppCard>

        <!-- Top supplier per sector -->
        <AppCard :title="$t('scoreboard.topPerSector')" :description="$t('scoreboard.topPerSectorHint')">
          <div v-if="data.topSupplierPerSector.length === 0" class="py-6">
            <EmptyState>{{ $t("scoreboard.noSectors") }}</EmptyState>
          </div>
          <ul v-else class="divide-y divide-slate-50">
            <li v-for="row in data.topSupplierPerSector" :key="row.sectorId" class="flex items-center justify-between gap-3 py-2.5">
              <span class="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600">{{ row.sectorName }}</span>
              <template v-if="row.companyId">
                <div class="flex min-w-0 flex-1 items-center justify-end gap-2">
                  <NuxtLink :to="`/admin/companies/${row.companyId}`" class="truncate text-sm font-medium text-slate-700 hover:text-indigo-600">{{ row.companyName }}</NuxtLink>
                  <span class="shrink-0 rounded-md bg-emerald-50 px-2 py-0.5 text-xs font-bold text-emerald-700 tabular-nums">{{ row.averageScore?.toFixed(2) }}</span>
                </div>
              </template>
              <span v-else class="text-xs text-slate-300">{{ $t("scoreboard.notEvaluated") }}</span>
            </li>
          </ul>
        </AppCard>
      </div>
    </template>
  </div>
</template>
