<script setup lang="ts">
import type { PagedResult, PurchaseOrderListItemDto } from "~/types/api";

/**
 * Sipariş listesi. İç kullanıcıda başlıktaki firma/işyeri seçimine uyar; `sourceId` verilirse yalnızca o sürecin
 * siparişlerini gösterir (ihale/RFQ detayındaki gömülü liste). Tedarikçi yalnızca kendisine gönderilenleri görür.
 */
const props = withDefaults(defineProps<{ internal: boolean; sourceId?: string; compact?: boolean }>(), { sourceId: undefined, compact: false });
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatMoney, formatDate } = useDateFormat();
const { selectedFirmId } = useFirm();
const { selectedWorkspaceId } = useWorkspace();

const orders = ref<PurchaseOrderListItemDto[]>([]);
const loading = ref(true);
const route = useRoute();
// Panodaki "Yapılacaklar" bağlantısı listeyi süzülmüş açabilir (?status=).
const status = ref(!props.compact && typeof route.query.status === "string" ? route.query.status : "");
const basePath = computed(() => (props.internal ? "/admin/orders" : "/supplier/orders"));
const statuses = computed(() => (props.internal ? ["Draft", "Sent", "Confirmed", "Cancelled"] : ["Sent", "Confirmed", "Cancelled"]));

let loadSeq = 0;
const { page, total, search, pageSize, params, resetAndLoad } = usePagedList(() => load(), props.compact ? 50 : 20);

async function load() {
  const seq = ++loadSeq;
  loading.value = true;
  try {
    const result = await api.get<PagedResult<PurchaseOrderListItemDto>>("/api/purchase-orders", {
      ...params(),
      status: status.value || undefined,
      sourceId: props.sourceId,
      firmId: props.internal && !props.sourceId ? selectedFirmId.value ?? undefined : undefined,
      workspaceId: props.internal && !props.sourceId ? selectedWorkspaceId.value ?? undefined : undefined,
    });
    if (seq !== loadSeq) return;
    orders.value = result.items;
    total.value = result.totalCount;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("orders.loadError")));
  } finally {
    if (seq === loadSeq) loading.value = false;
  }
}

onMounted(load);
watch(status, () => resetAndLoad());
if (props.internal && !props.sourceId) watch([selectedFirmId, selectedWorkspaceId], () => resetAndLoad());

defineExpose({ reload: load, count: computed(() => total.value) });
</script>

<template>
  <div data-testid="order-list">
    <div v-if="!compact" class="mb-3 flex flex-wrap items-center gap-2">
      <input v-model="search" type="search" :placeholder="$t('orders.search')" :aria-label="$t('orders.search')" class="w-full max-w-sm rounded-md border border-slate-300 px-3 py-1.5 text-sm" data-testid="list-search" />
      <select v-model="status" :aria-label="$t('common.status')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" data-testid="order-status-filter">
        <option value="">{{ $t("orders.allStatuses") }}</option>
        <option v-for="s in statuses" :key="s" :value="s">{{ $t(`orders.status.${s}`) }}</option>
      </select>
    </div>
    <LoadingSpinner v-if="loading" />
    <div v-else class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">{{ $t("orders.number") }}</th>
            <th v-if="!compact" class="pb-2">{{ $t("orders.source") }}</th>
            <th class="pb-2">{{ internal ? $t("orders.supplier") : $t("orders.buyer") }}</th>
            <th class="pb-2 text-right">{{ $t("orders.total") }}</th>
            <th class="pb-2">{{ $t("common.status") }}</th>
            <th class="pb-2">{{ $t("orders.date") }}</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="o in orders" :key="o.id" class="border-b border-slate-50" data-testid="order-row">
            <td class="py-2"><NuxtLink :to="`${basePath}/${o.id}`" class="font-mono text-xs font-semibold text-slate-800 hover:text-indigo-600 hover:underline">{{ o.orderNumber }}</NuxtLink></td>
            <td v-if="!compact" class="py-2 text-slate-600">
              {{ o.sourceTitle }}
              <span class="block text-[11px] text-slate-400">{{ $t(`orders.kind.${o.sourceKind}`) }}</span>
            </td>
            <td class="py-2 text-slate-600">{{ internal ? o.supplierCompanyName : o.ownerCompanyName }}</td>
            <td class="py-2 text-right tabular-nums text-slate-700">{{ formatMoney(o.totalAmount, o.currencyCode) }}</td>
            <td class="py-2"><AppBadge :tone="orderStatusTone[o.status]">{{ $t(`orders.status.${o.status}`) }}</AppBadge></td>
            <td class="py-2 text-slate-500">{{ formatDate(o.sentAt ?? o.createdAt) }}</td>
            <td class="py-2 text-right"><NuxtLink :to="`${basePath}/${o.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink></td>
          </tr>
        </tbody>
      </table>
      <EmptyState v-if="orders.length === 0">{{ sourceId ? $t("orders.emptyForSource") : $t("orders.empty") }}</EmptyState>
      <ListPager v-if="!compact" v-model:page="page" :total="total" :page-size="pageSize" />
    </div>
  </div>
</template>
