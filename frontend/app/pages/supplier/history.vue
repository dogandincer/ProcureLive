<script setup lang="ts">
import type { AuctionDto, PagedResult } from "~/types/api";

const api = useApi();
const auctions = ref<AuctionDto[]>([]);
const errorMessage = ref("");

const statusBadge: Record<string, string> = {
  Ended: "bg-slate-100 text-slate-600",
  Awarded: "bg-indigo-100 text-indigo-700",
  Cancelled: "bg-rose-100 text-rose-700",
};

onMounted(async () => {
  try {
    const page = await api.get<PagedResult<AuctionDto>>("/api/auctions/history/mine", { page: 1, pageSize: 50 });
    auctions.value = page.items;
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Kayıtlar yüklenemedi.");
  }
});
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">İhale Geçmişim</h1>

    <p v-if="errorMessage" class="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{{ errorMessage }}</p>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Başlık</th>
            <th class="pb-2">Alıcı</th>
            <th class="pb-2">Durum</th>
            <th class="pb-2">Bitiş</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50">
            <td class="py-2 font-medium text-slate-700">{{ a.title }}</td>
            <td class="py-2 text-slate-500">{{ a.ownerCompanyName }}</td>
            <td class="py-2"><span class="rounded px-2 py-0.5 text-xs font-medium" :class="statusBadge[a.status]">{{ a.status }}</span></td>
            <td class="py-2 text-slate-500">{{ new Date(a.endAt).toLocaleString("tr-TR") }}</td>
            <td class="py-2 text-right"><NuxtLink :to="`/supplier/auctions/${a.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
          </tr>
          <tr v-if="auctions.length === 0"><td colspan="5" class="py-6 text-center text-slate-400">Tamamlanmış ihale yok</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
