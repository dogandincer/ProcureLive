<script setup lang="ts">
import type { PagedResult, RfiProcessDto } from "~/types/api";

const api = useApi();
const processes = ref<RfiProcessDto[]>([]);
const errorMessage = ref("");

async function load() {
  try {
    const page = await api.get<PagedResult<RfiProcessDto>>("/api/rfi/processes", { page: 1, pageSize: 50 });
    processes.value = page.items;
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Kayıtlar yüklenemedi.");
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">RFI Davetlerim</h1>

    <p v-if="errorMessage" class="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{{ errorMessage }}</p>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Başlık</th>
            <th class="pb-2">Durum</th>
            <th class="pb-2">Son Tarih</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in processes" :key="p.id" class="border-b border-slate-50">
            <td class="py-2 font-medium text-slate-700">{{ p.title }}</td>
            <td class="py-2 text-slate-500">{{ p.status }}</td>
            <td class="py-2 text-slate-500">{{ new Date(p.dueDate).toLocaleDateString("tr-TR") }}</td>
            <td class="py-2 text-right"><NuxtLink :to="`/supplier/rfi/${p.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
          </tr>
          <tr v-if="processes.length === 0"><td colspan="4" class="py-6 text-center text-slate-400">Davet yok</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
