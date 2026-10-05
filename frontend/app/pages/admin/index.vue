<script setup lang="ts">
import type { AuctionDto, AuctionModelRecommendationDto, PagedResult } from "~/types/api";
import {
  BuildingOfficeIcon,
  CubeIcon,
  ClipboardDocumentListIcon,
  BoltIcon,
  PlusIcon,
  SparklesIcon,
} from "@heroicons/vue/24/outline";

definePageMeta({ middleware: [] });

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const auctions = ref<AuctionDto[]>([]);
const companyCount = ref(0);
const productCount = ref(0);
const activeAuctionCount = ref(0);
const loading = ref(true);

const recommendation = ref<AuctionModelRecommendationDto | null>(null);
const recommendationLoading = ref(false);
const estimatedParticipants = ref(5);
const estimatedItems = ref(1);

async function load() {
  loading.value = true;
  try {
    const [auctionPage, companyPage, productPage, activePage] = await Promise.all([
      api.get<PagedResult<AuctionDto>>("/api/auctions", { page: 1, pageSize: 10 }),
      api.get<PagedResult<unknown>>("/api/companies", { page: 1, pageSize: 1 }),
      api.get<PagedResult<unknown>>("/api/products", { page: 1, pageSize: 1 }),
      api.get<PagedResult<AuctionDto>>("/api/auctions", { page: 1, pageSize: 1, status: "Live" }),
    ]);
    auctions.value = auctionPage.items;
    companyCount.value = companyPage.totalCount;
    productCount.value = productPage.totalCount;
    activeAuctionCount.value = activePage.totalCount;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("dashboardAdmin.loadError")));
  } finally {
    loading.value = false;
  }
}

async function getRecommendation() {
  recommendationLoading.value = true;
  try {
    recommendation.value = await api.post<AuctionModelRecommendationDto>("/api/ai/auction-model-recommendation", {
      categoryId: null,
      candidateSupplierCompanyIds: null,
      estimatedParticipantCount: estimatedParticipants.value,
      estimatedItemCount: estimatedItems.value,
    });
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("dashboardAdmin.recommendationError")));
  } finally {
    recommendationLoading.value = false;
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

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <!-- Page header -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("dashboardAdmin.title") }}</h1>
        <p class="mt-0.5 text-sm text-slate-500">{{ $t("dashboardAdmin.subtitle") }}</p>
      </div>
      <NuxtLink
        to="/admin/auctions"
        class="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 transition-colors"
      >
        <PlusIcon class="h-4 w-4" />
        {{ $t("dashboardAdmin.newAuction") }}
      </NuxtLink>
    </div>

    <!-- KPI stats -->
    <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
      <AppStat
        :label="$t('dashboardAdmin.stat.companies')"
        :value="loading ? '…' : companyCount"
        :subtitle="$t('dashboardAdmin.stat.companiesSub')"
        tone="indigo"
      >
        <template #icon><BuildingOfficeIcon class="h-5 w-5" /></template>
      </AppStat>
      <AppStat
        :label="$t('dashboardAdmin.stat.products')"
        :value="loading ? '…' : productCount"
        :subtitle="$t('dashboardAdmin.stat.productsSub')"
        tone="slate"
      >
        <template #icon><CubeIcon class="h-5 w-5" /></template>
      </AppStat>
      <AppStat
        :label="$t('dashboardAdmin.stat.totalAuctions')"
        :value="loading ? '…' : auctions.length > 0 ? `${auctions.length}+` : '0'"
        :subtitle="$t('dashboardAdmin.stat.recentSub')"
        tone="amber"
      >
        <template #icon><ClipboardDocumentListIcon class="h-5 w-5" /></template>
      </AppStat>
      <AppStat
        :label="$t('dashboardAdmin.stat.liveAuctions')"
        :value="loading ? '…' : activeAuctionCount"
        :subtitle="$t('dashboardAdmin.stat.liveSub')"
        tone="emerald"
      >
        <template #icon><BoltIcon class="h-5 w-5" /></template>
      </AppStat>
    </div>

    <!-- Recent auctions table -->
    <AppCard :title="$t('dashboardAdmin.recentAuctions')" :description="$t('dashboardAdmin.recentAuctionsSub')">
      <template #action>
        <NuxtLink to="/admin/auctions" class="text-xs font-medium text-indigo-600 hover:text-indigo-700">
          {{ $t("common.detail") }} →
        </NuxtLink>
      </template>

      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto -mx-5 -mb-5">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">
              <th class="pb-2.5 pt-1 px-5">{{ $t("dashboardAdmin.table.title") }}</th>
              <th class="pb-2.5 pt-1 px-5">{{ $t("dashboardAdmin.table.type") }}</th>
              <th class="pb-2.5 pt-1 px-5">{{ $t("dashboardAdmin.table.status") }}</th>
              <th class="pb-2.5 pt-1 px-5">{{ $t("dashboardAdmin.table.endsAt") }}</th>
              <th class="pb-2.5 pt-1 px-5"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="a in auctions"
              :key="a.id"
              class="border-b border-slate-50 hover:bg-slate-50 transition-colors"
            >
              <td class="px-5 py-3 font-medium text-slate-800">{{ a.title }}</td>
              <td class="px-5 py-3 text-slate-500">{{ $t(`enums.auctionType.${a.auctionType}`) }}</td>
              <td class="px-5 py-3">
                <AppBadge :tone="statusTone[a.status]">{{ $t(`enums.auctionStatus.${a.status}`) }}</AppBadge>
              </td>
              <td class="px-5 py-3 text-slate-500">{{ formatDateTime(a.endAt) }}</td>
              <td class="px-5 py-3 text-right">
                <NuxtLink :to="`/admin/auctions/${a.id}`" class="text-xs font-medium text-indigo-600 hover:text-indigo-700">
                  {{ $t("common.detail") }}
                </NuxtLink>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="auctions.length === 0" class="py-10">{{ $t("dashboardAdmin.emptyAuctions") }}</EmptyState>
      </div>
    </AppCard>

    <!-- AI recommendation -->
    <AppCard :title="`✨ ${$t('dashboardAdmin.recommendation.heading')}`" :description="$t('dashboardAdmin.recommendation.hint')">
      <div class="flex flex-wrap items-end gap-3">
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("dashboardAdmin.recommendation.estimatedParticipants") }}</label>
          <input v-model.number="estimatedParticipants" type="number" min="1" class="w-28 rounded-lg border border-slate-300 px-2.5 py-1.5 text-sm focus:border-indigo-400 focus:outline-none focus:ring-1 focus:ring-indigo-400" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("dashboardAdmin.recommendation.estimatedItems") }}</label>
          <input v-model.number="estimatedItems" type="number" min="1" class="w-28 rounded-lg border border-slate-300 px-2.5 py-1.5 text-sm focus:border-indigo-400 focus:outline-none focus:ring-1 focus:ring-indigo-400" />
        </div>
        <AppButton variant="secondary" :loading="recommendationLoading" @click="getRecommendation">
          <SparklesIcon class="h-4 w-4" />
          {{ recommendationLoading ? $t("dashboardAdmin.recommendation.calculating") : $t("dashboardAdmin.recommendation.suggest") }}
        </AppButton>
      </div>

      <div v-if="recommendation" class="mt-4 rounded-xl bg-indigo-50 border border-indigo-100 p-4">
        <div class="text-sm font-semibold text-indigo-800">
          {{ $t("dashboardAdmin.recommendation.recommended") }}: {{ recommendation.recommendedAuctionType }}
          <span class="ml-2 rounded-full bg-indigo-100 px-2 py-0.5 text-xs font-normal text-indigo-600">
            {{ $t("dashboardAdmin.recommendation.confidence") }}: {{ Math.round(recommendation.confidenceScore * 100) }}%
          </span>
        </div>
        <p class="mt-1.5 text-sm text-indigo-700">{{ recommendation.rationale }}</p>
      </div>
    </AppCard>
  </div>
</template>
