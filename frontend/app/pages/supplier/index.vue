<script setup lang="ts">
import type { AuctionDto, PagedResult, RfiProcessDto, RfqProcessDto } from "~/types/api";

const api = useApi();
const { currentUser } = useAuth();
const toast = useToast();

const auctions = ref<AuctionDto[]>([]);
const rfiProcesses = ref<RfiProcessDto[]>([]);
const rfqProcesses = ref<RfqProcessDto[]>([]);
const loading = ref(true);

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
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Panel verileri yüklenemedi."));
  } finally {
    loading.value = false;
  }
}

const liveAuctions = computed(() => auctions.value.filter((a) => a.status === "Live"));

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
    <h1 class="text-xl font-bold text-slate-800">Merhaba, {{ currentUser?.firstName }} 👋</h1>

    <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <AppCard>
        <div class="text-sm text-slate-500">İhale Davetlerim</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ auctions.length }}</div>
        <NuxtLink to="/supplier/auctions" class="mt-2 inline-block text-xs text-indigo-600 hover:underline">Tümünü gör →</NuxtLink>
      </AppCard>
      <AppCard>
        <div class="text-sm text-slate-500">RFI Davetlerim</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ rfiProcesses.length }}</div>
        <NuxtLink to="/supplier/rfi" class="mt-2 inline-block text-xs text-indigo-600 hover:underline">Tümünü gör →</NuxtLink>
      </AppCard>
      <AppCard>
        <div class="text-sm text-slate-500">RFQ Davetlerim</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ rfqProcesses.length }}</div>
        <NuxtLink to="/supplier/rfq" class="mt-2 inline-block text-xs text-indigo-600 hover:underline">Tümünü gör →</NuxtLink>
      </AppCard>
    </div>

    <div v-if="liveAuctions.length > 0" class="rounded-xl border border-emerald-200 bg-emerald-50 p-5">
      <h2 class="text-sm font-semibold text-emerald-800">🔴 Şu Anda Canlı İhaleler</h2>
      <ul class="mt-3 space-y-2">
        <li v-for="a in liveAuctions" :key="a.id" class="flex items-center justify-between rounded-lg bg-white px-4 py-2">
          <span class="text-sm font-medium text-slate-700">{{ a.title }}</span>
          <NuxtLink :to="`/supplier/auctions/${a.id}`" class="rounded-md bg-emerald-600 px-3 py-1 text-xs font-semibold text-white">
            İhaleye Git
          </NuxtLink>
        </li>
      </ul>
    </div>

    <AppCard>
      <h2 class="text-sm font-semibold text-slate-700">Son İhale Davetlerim</h2>
      <LoadingSpinner v-if="loading" class="mt-3" />
      <div v-else class="mt-3 overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">Başlık</th>
              <th class="pb-2">Durum</th>
              <th class="pb-2">Bitiş</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">{{ a.title }}</td>
              <td class="py-2"><AppBadge :tone="statusTone[a.status]">{{ a.status }}</AppBadge></td>
              <td class="py-2 text-slate-500">{{ new Date(a.endAt).toLocaleString("tr-TR") }}</td>
              <td class="py-2 text-right"><NuxtLink :to="`/supplier/auctions/${a.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="auctions.length === 0">Davet yok</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
