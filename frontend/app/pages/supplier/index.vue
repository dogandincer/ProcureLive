<script setup lang="ts">
import type { AuctionDto, CompanyDetailDto, PagedResult, RfiProcessDto, RfqProcessDto } from "~/types/api";
import { ClockIcon } from "@heroicons/vue/24/outline";

const api = useApi();
const { currentUser } = useAuth();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const auctions = ref<AuctionDto[]>([]);
const rfiProcesses = ref<RfiProcessDto[]>([]);
const rfqProcesses = ref<RfqProcessDto[]>([]);
const loading = ref(true);
const counts = reactive({ auctions: 0, rfi: 0, rfq: 0 });
// Kendi carimin durumu: onay bekleyen / pasif cariye ne olacağını panel söyler.
const company = ref<CompanyDetailDto | null>(null);
const { isPlus, refresh: refreshSubscription } = useSubscription();

async function load() {
  loading.value = true;
  try {
    const [auctionPage, rfiPage, rfqPage] = await Promise.all([
      api.get<PagedResult<AuctionDto>>("/api/auctions", { page: 1, pageSize: 10 }),
      api.get<PagedResult<RfiProcessDto>>("/api/rfi/processes", { page: 1, pageSize: 10 }),
      api.get<PagedResult<RfqProcessDto>>("/api/rfq/processes", { page: 1, pageSize: 10 }),
    ]);
    auctions.value = auctionPage.items;
    rfiProcesses.value = rfiPage.items;
    rfqProcesses.value = rfqPage.items;
    counts.auctions = auctionPage.totalCount;
    counts.rfi = rfiPage.totalCount;
    counts.rfq = rfqPage.totalCount;
    if (currentUser.value?.companyId) {
      company.value = await api.get<CompanyDetailDto>(`/api/companies/${currentUser.value.companyId}`).catch(() => null);
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("dashboardSupplier.loadError")));
  } finally {
    loading.value = false;
  }
}

const liveAuctions = computed(() => auctions.value.filter((a) => a.status === "Live"));

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Scheduled: "amber",
  Live: "emerald",
  Paused: "amber",
  Ended: "slate",
  Awarded: "indigo",
  Cancelled: "rose",
};

onMounted(() => Promise.all([load(), refreshSubscription()]));
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">{{ $t("dashboardSupplier.greeting", { name: currentUser?.firstName }) }} 👋</h1>

    <!-- Kendi kaydıyla gelen ve henüz onaylanmamış cari: ne bekleniyor, beklerken ne yapılabilir -->
    <div v-if="company && company.status !== 'Active'" class="rounded-xl border border-amber-200 bg-amber-50 p-5" data-testid="pending-approval">
      <div class="flex items-start gap-3">
        <ClockIcon class="mt-0.5 h-5 w-5 shrink-0 text-amber-600" />
        <div class="min-w-0 space-y-2 text-sm text-amber-900">
          <p class="font-semibold">{{ $t(company.status === 'Passive' ? 'dashboardSupplier.pending.passiveTitle' : 'dashboardSupplier.pending.title') }}</p>
          <p>{{ $t(company.status === 'Passive' ? 'dashboardSupplier.pending.passiveBody' : 'dashboardSupplier.pending.body') }}</p>
          <ul v-if="company.status !== 'Passive'" class="list-disc space-y-1 pl-5 text-amber-800">
            <li>{{ $t("dashboardSupplier.pending.stepSettings") }}</li>
            <li>{{ isPlus ? $t("dashboardSupplier.pending.stepSearchPlus") : $t("dashboardSupplier.pending.stepPlus") }}</li>
            <li>{{ $t("dashboardSupplier.pending.stepInvite") }}</li>
          </ul>
          <div class="flex flex-wrap gap-2 pt-1">
            <NuxtLink to="/supplier/settings" class="rounded-md bg-surface px-3 py-1.5 text-xs font-semibold text-amber-800 ring-1 ring-amber-300 hover:bg-amber-100">{{ $t("nav.companySettings") }}</NuxtLink>
            <NuxtLink :to="isPlus ? '/supplier/search' : '/supplier/pricing'" class="rounded-md bg-amber-500 px-3 py-1.5 text-xs font-semibold text-white hover:bg-amber-600">
              {{ isPlus ? $t("nav.plusSearch") : $t("nav.pricing") }}
            </NuxtLink>
          </div>
        </div>
      </div>
    </div>

    <!-- Sayaçlar kartın tamamıyla tıklanır; mobilde üçü tek satırda kalır. -->
    <div class="grid grid-cols-3 gap-3 sm:gap-4">
      <NuxtLink
        v-for="s in [
          { to: '/supplier/auctions', label: $t('dashboardSupplier.auctionInvites'), count: counts.auctions },
          { to: '/supplier/rfi', label: $t('dashboardSupplier.rfiInvites'), count: counts.rfi },
          { to: '/supplier/rfq', label: $t('dashboardSupplier.rfqInvites'), count: counts.rfq },
        ]"
        :key="s.to"
        :to="s.to"
        class="rounded-xl border border-slate-200 bg-surface p-3 shadow-sm transition-colors hover:border-indigo-300 sm:p-5"
        data-testid="supplier-stat"
      >
        <div class="text-xs text-slate-500 sm:text-sm">{{ s.label }}</div>
        <div class="mt-1 text-xl font-bold text-slate-800 sm:text-2xl">{{ s.count }}</div>
        <span class="mt-2 hidden text-xs text-indigo-600 sm:inline-block">{{ $t("dashboardSupplier.viewAll") }}</span>
      </NuxtLink>
    </div>

    <!-- Yapılacaklar: yanıt bekleyen davetler, onay bekleyen siparişler, süresi dolan belgeler -->
    <DashboardTasks :internal="false" />

    <div v-if="liveAuctions.length > 0" class="rounded-xl border border-emerald-200 bg-emerald-50 p-5">
      <h2 class="text-sm font-semibold text-emerald-800">🔴 {{ $t("dashboardSupplier.liveNow") }}</h2>
      <ul class="mt-3 space-y-2">
        <li v-for="a in liveAuctions" :key="a.id" class="flex items-center justify-between rounded-lg bg-surface px-4 py-2">
          <span class="text-sm font-medium text-slate-700">{{ a.title }}</span>
          <NuxtLink :to="`/supplier/auctions/${a.id}`" class="rounded-md bg-emerald-600 px-3 py-1 text-xs font-semibold text-white">
            {{ $t("dashboardSupplier.goToAuction") }}
          </NuxtLink>
        </li>
      </ul>
    </div>

    <AppCard>
      <h2 class="text-sm font-semibold text-slate-700">{{ $t("dashboardSupplier.recentInvites") }}</h2>
      <LoadingSpinner v-if="loading" class="mt-3" />
      <div v-else class="mt-3 overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("dashboardSupplier.table.title") }}</th>
              <th class="pb-2">{{ $t("dashboardSupplier.table.status") }}</th>
              <th class="pb-2">{{ $t("dashboardSupplier.table.endsAt") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">{{ a.title }}</td>
              <td class="py-2"><AppBadge :tone="statusTone[a.status]">{{ $t(`enums.auctionStatus.${a.status}`) }}</AppBadge></td>
              <td class="py-2 text-slate-500">{{ formatDateTime(a.endAt) }}</td>
              <td class="py-2 text-right"><NuxtLink :to="`/supplier/auctions/${a.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="auctions.length === 0">{{ $t("dashboardSupplier.emptyInvites") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
