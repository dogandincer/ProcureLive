<script setup lang="ts">
import type { ErpRecordStatusDto } from "~/types/api";
import { ArrowPathIcon, ArrowUpTrayIcon } from "@heroicons/vue/24/outline";

/**
 * Premium ERP kutusu: cari, ürün, sipariş veya ihale kaydının ERP eşlemesi ve son ERP işleri. Lisans yoksa ya da entegrasyon
 * kapalıysa hiç görünmez. Cari/ürün için "ERP'ye gönder", sipariş için "ERP'de sipariş oluştur", ihale için tedarikçi teklifleri.
 */
const props = defineProps<{
  companyId?: string;
  productId?: string;
  purchaseOrderId?: string;
  auctionId?: string;
  /** Sipariş: yalnızca gönderilmiş/onaylanmış siparişte düğme etkin. */
  orderStatus?: string;
}>();

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { can } = usePermissions();
const { isAdminRole } = useAuth();
const { ensure, erpActive } = useFeatures();
const { formatDateTime } = useDateFormat();

const status = ref<ErpRecordStatusDto | null>(null);
const busy = ref(false);
let timer: ReturnType<typeof setInterval> | null = null;

const query = computed(() => ({
  companyId: props.companyId, productId: props.productId, purchaseOrderId: props.purchaseOrderId, auctionId: props.auctionId,
}));
const kind = computed(() => (props.companyId ? "company" : props.productId ? "product" : props.purchaseOrderId ? "order" : "auction"));

async function load() {
  try {
    status.value = await api.get<ErpRecordStatusDto>("/api/erp/status", query.value);
  } catch {
    status.value = null;
  }
}

const hasPending = computed(() => status.value?.jobs.some((j) => j.status === "Pending") ?? false);
const statusTone: Record<string, "amber" | "emerald" | "rose" | "slate" | "indigo"> = {
  Pending: "amber", Simulated: "indigo", Done: "emerald", Failed: "rose", Skipped: "slate",
};

const canAct = computed(() => {
  if (kind.value === "order") return can("orders.manage") && !status.value?.erpCode && (props.orderStatus === "Sent" || props.orderStatus === "Confirmed");
  if (kind.value === "company" || kind.value === "product") return isAdminRole();
  return false;
});

async function act() {
  busy.value = true;
  try {
    const path = kind.value === "order" ? `/api/erp/purchase-orders/${props.purchaseOrderId}/order`
      : kind.value === "company" ? `/api/erp/companies/${props.companyId}/export`
      : `/api/erp/products/${props.productId}/export`;
    await api.post(path);
    toast.success(t("erp.card.queued"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("erp.card.actionError")));
  } finally {
    busy.value = false;
  }
}

// Bekleyen iş varken kısa aralıkla yenile (kuyruk her 10 sn'de işlenir).
watch(hasPending, (pending) => {
  if (timer) clearInterval(timer);
  timer = pending ? setInterval(load, 5000) : null;
});
onBeforeUnmount(() => timer && clearInterval(timer));

onMounted(async () => {
  await ensure();
  if (erpActive.value) await load();
});
</script>

<template>
  <AppCard v-if="erpActive && status?.enabled" data-testid="erp-card">
    <template #header>
      <div class="flex items-center gap-2">
        <h3 class="text-sm font-semibold text-slate-800">{{ $t("erp.card.title") }}</h3>
        <AppBadge :tone="status.mode === 'Live' ? 'emerald' : 'indigo'">{{ $t(`erp.mode.${status.mode}`) }}</AppBadge>
      </div>
    </template>
    <template #action>
      <div class="flex items-center gap-1">
        <AppButton variant="ghost" :aria-label="$t('erp.refresh')" @click="load"><ArrowPathIcon class="h-4 w-4" /></AppButton>
        <AppButton v-if="canAct" variant="secondary" :loading="busy" data-testid="erp-action" @click="act">
          <ArrowUpTrayIcon class="h-4 w-4" />{{ $t(`erp.card.action.${kind}`) }}
        </AppButton>
      </div>
    </template>

    <dl v-if="kind !== 'auction'" class="mb-3 text-sm">
      <dt class="text-xs text-slate-500">{{ $t(`erp.card.code.${kind}`) }}</dt>
      <dd class="font-mono text-slate-800" data-testid="erp-code">{{ status.erpCode ?? $t("erp.card.notLinked") }}</dd>
    </dl>
    <p v-if="kind === 'order' && !canAct && !status.erpCode && orderStatus !== 'Sent' && orderStatus !== 'Confirmed'" class="mb-3 text-xs text-slate-500">
      {{ $t("erp.card.orderHint") }}
    </p>

    <div v-if="kind === 'auction'" class="overflow-x-auto">
      <p class="mb-2 text-xs text-slate-500">{{ $t("erp.card.auctionHint") }}</p>
      <table class="w-full text-sm" data-testid="erp-offers">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2 pr-3">{{ $t("erp.card.supplier") }}</th>
            <th class="pb-2 pr-3">{{ $t("erp.card.docNo") }}</th>
            <th class="pb-2 pr-3">{{ $t("erp.card.lastRevision") }}</th>
            <th class="pb-2">{{ $t("common.status") }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="o in status.offers" :key="o.companyId" class="border-b border-slate-50 align-top">
            <td class="py-2 pr-3 text-slate-700">{{ o.companyName }}</td>
            <td class="py-2 pr-3 font-mono text-xs text-slate-600">{{ o.docNo }}<span v-if="o.erpOfferMId" class="block text-slate-400">#{{ o.erpOfferMId }}</span></td>
            <td class="whitespace-nowrap py-2 pr-3 text-slate-500">{{ o.lastRevisedBidAt ? formatDateTime(o.lastRevisedBidAt) : "—" }}</td>
            <td class="py-2">
              <AppBadge v-if="o.lastStatus" :tone="statusTone[o.lastStatus] ?? 'slate'">{{ $t(`erp.jobStatus.${o.lastStatus}`) }}</AppBadge>
              <span v-if="o.lastStatus === 'Failed' || o.lastStatus === 'Skipped'" class="mt-1 block max-w-xs text-xs text-rose-700">{{ o.lastMessage }}</span>
            </td>
          </tr>
        </tbody>
      </table>
      <EmptyState v-if="status.offers.length === 0">{{ $t("erp.card.noOffers") }}</EmptyState>
    </div>

    <ul v-else-if="status.jobs.length" class="space-y-2 text-sm" data-testid="erp-jobs">
      <li v-for="j in status.jobs" :key="j.id" class="flex flex-wrap items-start justify-between gap-2 border-t border-slate-100 pt-2 first:border-0 first:pt-0">
        <span class="text-slate-600">
          {{ formatDateTime(j.completedAt ?? j.createdAt) }}
          <span v-if="j.message" class="block text-xs text-slate-500">{{ j.message }}</span>
        </span>
        <AppBadge :tone="statusTone[j.status] ?? 'slate'">{{ $t(`erp.jobStatus.${j.status}`) }}</AppBadge>
      </li>
    </ul>
    <p v-else class="text-xs text-slate-400">{{ $t("erp.card.noJobs") }}</p>
  </AppCard>
</template>
