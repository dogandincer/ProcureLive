<script setup lang="ts">
import type { PagedResult, RfqProcessDto } from "~/types/api";
import { LockClosedIcon } from "@heroicons/vue/24/solid";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();
const processes = ref<RfqProcessDto[]>([]);
const loading = ref(true);

onMounted(async () => {
  loading.value = true;
  try {
    const page = await api.get<PagedResult<RfqProcessDto>>("/api/rfq/processes", { page: 1, pageSize: 50 });
    processes.value = page.items;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.supplierList.loadError")));
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">{{ $t("rfq.supplierList.title") }}</h1>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("rfq.supplierList.table.title") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2">{{ $t("rfq.supplierList.table.dueDate") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in processes" :key="p.id" class="border-b border-slate-50" :class="{ 'bg-slate-50/60': p.accessLevel !== 'Full' }">
              <td class="py-2 font-medium text-slate-700">
                <div class="flex items-center gap-2">
                  <LockClosedIcon v-if="p.accessLevel !== 'Full'" class="h-3.5 w-3.5 shrink-0 text-amber-500" />
                  {{ p.title }}
                </div>
                <span v-if="p.myInvitationStatus" class="text-[11px] font-normal text-slate-400">{{ $t(`enums.invitationStatus.${p.myInvitationStatus}`) }}</span>
              </td>
              <td class="py-2 text-slate-500">{{ $t(`enums.processStatus.${p.status}`) }}</td>
              <td class="py-2 text-slate-500">{{ formatDate(p.dueDate) }}</td>
              <td class="py-2 text-right">
                <!-- Standart kota dolu, davet kabul edilmemiş: içerik sunucuda düşürüldü → Plus alınız -->
                <div class="flex items-center justify-end gap-2">
                  <template v-if="p.accessLevel !== 'Full'">
                    <span class="text-xs text-slate-400">{{ $t("restricted.QuotaExceeded.short") }}</span>
                    <NuxtLink to="/supplier/pricing" class="rounded bg-amber-500 px-2 py-1 text-xs font-semibold text-white hover:bg-amber-600">Plus</NuxtLink>
                  </template>
                  <NuxtLink :to="`/supplier/rfq/${p.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="processes.length === 0">{{ $t("rfq.supplierList.empty") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
