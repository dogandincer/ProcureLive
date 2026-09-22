<script setup lang="ts">
import type { AuctionDto, PagedResult } from "~/types/api";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();
const auctions = ref<AuctionDto[]>([]);
const loading = ref(true);

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Ended: "slate",
  Awarded: "indigo",
  Cancelled: "rose",
};

onMounted(async () => {
  loading.value = true;
  try {
    const page = await api.get<PagedResult<AuctionDto>>("/api/auctions/history/mine", { page: 1, pageSize: 50 });
    auctions.value = page.items;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.history.loadError")));
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-xl font-bold text-slate-800">{{ $t("auctions.history.title") }}</h1>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("auctions.history.table.title") }}</th>
              <th class="pb-2">{{ $t("auctions.history.table.owner") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2">{{ $t("auctions.history.table.endsAt") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">{{ a.title }}</td>
              <td class="py-2 text-slate-500">{{ a.ownerCompanyName }}</td>
              <td class="py-2"><AppBadge :tone="statusTone[a.status] ?? 'slate'">{{ $t(`enums.auctionStatus.${a.status}`) }}</AppBadge></td>
              <td class="py-2 text-slate-500">{{ formatDateTime(a.endAt) }}</td>
              <td class="py-2 text-right"><NuxtLink :to="`/supplier/auctions/${a.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="auctions.length === 0">{{ $t("auctions.history.empty") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
