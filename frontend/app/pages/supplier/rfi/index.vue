<script setup lang="ts">
import type { PagedResult, RfiProcessDto } from "~/types/api";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();
const processes = ref<RfiProcessDto[]>([]);
const loading = ref(true);

async function load() {
  loading.value = true;
  try {
    const page = await api.get<PagedResult<RfiProcessDto>>("/api/rfi/processes", { page: 1, pageSize: 50 });
    processes.value = page.items;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.supplierList.loadError")));
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">{{ $t("rfi.supplierList.title") }}</h1>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("rfi.supplierList.table.title") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2">{{ $t("rfi.supplierList.table.dueDate") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in processes" :key="p.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">{{ p.title }}</td>
              <td class="py-2 text-slate-500">{{ $t(`enums.processStatus.${p.status}`) }}</td>
              <td class="py-2 text-slate-500">{{ formatDate(p.dueDate) }}</td>
              <td class="py-2 text-right"><NuxtLink :to="`/supplier/rfi/${p.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="processes.length === 0">{{ $t("rfi.supplierList.empty") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
