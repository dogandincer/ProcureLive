<script setup lang="ts">
import type { Component } from "vue";
import {
  CheckBadgeIcon,
  TrophyIcon,
  UserPlusIcon,
  ChatBubbleLeftRightIcon,
  DocumentCheckIcon,
  ShoppingCartIcon,
  DocumentTextIcon,
  BuildingOfficeIcon,
  EnvelopeOpenIcon,
  QuestionMarkCircleIcon,
  ChevronRightIcon,
} from "@heroicons/vue/24/outline";
import type { DashboardTaskDto } from "~/types/api";

/**
 * Panodaki "Yapılacaklar": sırada bekleyen işler (role ve veri kapsamına göre). Tek kayıt varsa doğrudan ona,
 * birden çok varsa süzülmüş listeye gidilir. Hiç iş yoksa kısa bir "her şey yolunda" mesajı gösterilir.
 */
const props = defineProps<{ internal: boolean }>();
const api = useApi();

const tasks = ref<DashboardTaskDto[]>([]);
const loaded = ref(false);

const meta: Record<string, { icon: Component; tone: string; list: string }> = {
  publishApprovals: { icon: CheckBadgeIcon, tone: "amber", list: "/admin/auctions?status=Draft" },
  pendingSuppliers: { icon: UserPlusIcon, tone: "amber", list: "/admin/companies?status=PendingApproval" },
  awaitingAward: { icon: TrophyIcon, tone: "indigo", list: "/admin/auctions?status=Ended" },
  participationRequests: { icon: UserPlusIcon, tone: "indigo", list: "/admin/auctions" },
  openQuestions: { icon: ChatBubbleLeftRightIcon, tone: "rose", list: "/admin/auctions" },
  rfqToAward: { icon: DocumentCheckIcon, tone: "indigo", list: "/admin/rfq?status=Closed" },
  draftOrders: { icon: ShoppingCartIcon, tone: "slate", list: "/admin/orders?status=Draft" },
  expiringDocuments: { icon: DocumentTextIcon, tone: "amber", list: "/admin/companies" },
  auctionInvitations: { icon: EnvelopeOpenIcon, tone: "indigo", list: "/supplier/auctions" },
  rfiInvitations: { icon: QuestionMarkCircleIcon, tone: "indigo", list: "/supplier/rfi" },
  rfqInvitations: { icon: DocumentTextIcon, tone: "indigo", list: "/supplier/rfq" },
  ordersToConfirm: { icon: ShoppingCartIcon, tone: "amber", list: "/supplier/orders?status=Sent" },
  pendingParticipation: { icon: UserPlusIcon, tone: "slate", list: "/supplier/auctions" },
  myExpiringDocuments: { icon: BuildingOfficeIcon, tone: "amber", list: "/supplier/settings#documents" },
};

const toneClass: Record<string, string> = {
  amber: "bg-amber-50 text-amber-600",
  indigo: "bg-indigo-50 text-indigo-600",
  rose: "bg-rose-50 text-rose-600",
  slate: "bg-slate-100 text-slate-600",
};

function link(task: DashboardTaskDto): string {
  const base = props.internal ? "/admin" : "/supplier";
  if (task.targetId) {
    switch (task.targetKind) {
      case "auction": return `${base}/auctions/${task.targetId}${task.key === "openQuestions" ? "#qa" : ""}`;
      case "rfi": return `${base}/rfi/${task.targetId}${task.key === "openQuestions" ? "#qa" : ""}`;
      case "rfq": return `${base}/rfq/${task.targetId}${task.key === "openQuestions" ? "#qa" : ""}`;
      case "order": return `${base}/orders/${task.targetId}`;
      case "company": return props.internal ? `/admin/companies/${task.targetId}${task.key === "expiringDocuments" ? "#documents" : ""}` : "/supplier/settings#documents";
    }
  }
  return meta[task.key]?.list ?? base;
}

onMounted(async () => {
  try {
    tasks.value = await api.get<DashboardTaskDto[]>("/api/dashboard/tasks");
  } catch {
    tasks.value = [];
  } finally {
    loaded.value = true;
  }
});
</script>

<template>
  <AppCard :title="$t('tasks.title')" :description="$t('tasks.hint')">
    <div data-testid="dashboard-tasks">
      <LoadingSpinner v-if="!loaded" />
      <p v-else-if="tasks.length === 0" class="py-2 text-sm text-slate-500">{{ $t("tasks.empty") }}</p>
      <ul v-else class="divide-y divide-slate-100">
        <li v-for="task in tasks" :key="task.key">
          <NuxtLink :to="link(task)" class="group flex items-center gap-3 rounded-md px-1 py-2.5 hover:bg-slate-50" data-testid="dashboard-task">
            <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg" :class="toneClass[meta[task.key]?.tone ?? 'slate']">
              <component :is="meta[task.key]?.icon ?? CheckBadgeIcon" class="h-5 w-5" />
            </span>
            <span class="min-w-0 flex-1 text-sm text-slate-700">{{ $t(`tasks.items.${task.key}`, { n: task.count }) }}</span>
            <span class="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-semibold tabular-nums text-slate-700">{{ task.count }}</span>
            <ChevronRightIcon class="h-4 w-4 text-slate-300 group-hover:text-slate-500" />
          </NuxtLink>
        </li>
      </ul>
    </div>
  </AppCard>
</template>
