<script setup lang="ts">
import type { AuctionDto, PagedResult, RfiProcessDto, RfqProcessDto } from "~/types/api";

const api = useApi();
const { currentUser } = useAuth();

const auctions = ref<AuctionDto[]>([]);
const rfiProcesses = ref<RfiProcessDto[]>([]);
const rfqProcesses = ref<RfqProcessDto[]>([]);
const loading = ref(true);

async function load() {
  loading.value = true;
  const [auctionPage, rfiPage, rfqPage] = await Promise.all([
    api.get<PagedResult<AuctionDto>>("/api/auctions", { page: 1, pageSize: 10 }),
    api.get<PagedResult<RfiProcessDto>>("/api/rfi/processes", { page: 1, pageSize: 10 }),
    api.get<PagedResult<RfqProcessDto>>("/api/rfq/processes", { page: 1, pageSize: 10 }),
  ]);
  auctions.value = auctionPage.items;
  rfiProcesses.value = rfiPage.items;
  rfqProcesses.value = rfqPage.items;
  loading.value = false;
}

const liveAuctions = computed(() => auctions.value.filter((a) => a.status === "Live"));

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">Merhaba, {{ currentUser?.firstName }} 👋</h1>

    <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">İhale Davetlerim</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ auctions.length }}</div>
        <NuxtLink to="/supplier/auctions" class="mt-2 inline-block text-xs text-indigo-600 hover:underline">Tümünü gör →</NuxtLink>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">RFI Davetlerim</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ rfiProcesses.length }}</div>
        <NuxtLink to="/supplier/rfi" class="mt-2 inline-block text-xs text-indigo-600 hover:underline">Tümünü gör →</NuxtLink>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">RFQ Davetlerim</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ rfqProcesses.length }}</div>
        <NuxtLink to="/supplier/rfq" class="mt-2 inline-block text-xs text-indigo-600 hover:underline">Tümünü gör →</NuxtLink>
      </div>
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

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <h2 class="text-sm font-semibold text-slate-700">Son İhale Davetlerim</h2>
      <table class="mt-3 w-full text-sm">
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
            <td class="py-2 text-slate-500">{{ a.status }}</td>
            <td class="py-2 text-slate-500">{{ new Date(a.endAt).toLocaleString("tr-TR") }}</td>
            <td class="py-2 text-right"><NuxtLink :to="`/supplier/auctions/${a.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
          </tr>
          <tr v-if="!loading && auctions.length === 0"><td colspan="4" class="py-6 text-center text-slate-400">Davet yok</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
