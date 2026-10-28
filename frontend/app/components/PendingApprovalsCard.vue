<script setup lang="ts">
import type { PendingPublishApprovalDto } from "~/types/api";

/** Admin panosu: satınalmacıların yayın onayına gönderdiği taslak ihaleler. Liste boşsa kart hiç görünmez. */
const api = useApi();
const { formatDateTime } = useDateFormat();
const items = ref<PendingPublishApprovalDto[]>([]);

onMounted(async () => {
  try {
    items.value = await api.get<PendingPublishApprovalDto[]>("/api/auctions/pending-approvals");
  } catch {
    items.value = [];
  }
});
</script>

<template>
  <AppCard v-if="items.length > 0" :title="$t('auctions.approval.dashboardTitle', { n: items.length })" :description="$t('auctions.approval.dashboardHint')">
    <ul class="divide-y divide-slate-100" data-testid="pending-approvals">
      <li v-for="a in items" :key="a.auctionId" class="flex flex-wrap items-center justify-between gap-2 py-2 text-sm">
        <div class="min-w-0">
          <NuxtLink :to="`/admin/auctions/${a.auctionId}`" class="font-medium text-indigo-600 hover:underline">{{ a.title }}</NuxtLink>
          <span class="block text-xs text-slate-500">{{ a.ownerCompanyName }} · {{ $t("auctions.approval.requestedBy", { name: a.requestedByName ?? "—", date: formatDateTime(a.requestedAt) }) }}</span>
        </div>
        <span class="text-xs text-slate-500">{{ $t("auctions.approval.plannedStart", { date: formatDateTime(a.startAt) }) }}</span>
      </li>
    </ul>
  </AppCard>
</template>
