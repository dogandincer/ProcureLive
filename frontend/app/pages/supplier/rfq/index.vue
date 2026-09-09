<script setup lang="ts">
import type { PagedResult, RfqProcessDto } from "~/types/api";

const api = useApi();
const toast = useToast();
const processes = ref<RfqProcessDto[]>([]);
const loading = ref(true);

onMounted(async () => {
  loading.value = true;
  try {
    const page = await api.get<PagedResult<RfqProcessDto>>("/api/rfq/processes", { page: 1, pageSize: 50 });
    processes.value = page.items;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Kayıtlar yüklenemedi."));
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">RFQ Davetlerim</h1>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
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
              <td class="py-2 text-right"><NuxtLink :to="`/supplier/rfq/${p.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="processes.length === 0">Davet yok</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
