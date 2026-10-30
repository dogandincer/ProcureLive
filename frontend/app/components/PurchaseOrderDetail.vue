<script setup lang="ts">
import { PrinterIcon } from "@heroicons/vue/24/outline";
import type { PurchaseOrderDetailDto } from "~/types/api";

/**
 * Sipariş detayı. İç kullanıcı: taslakta not/teslim tarihi düzenler, gönderir, iptal eder (gönderilmişi de).
 * Tedarikçi: gönderilen siparişi isteğe bağlı notla onaylar. Kayıt yoksa / yetki yoksa erişim ekranı gösterilir.
 */
const props = defineProps<{ orderId: string; internal: boolean }>();
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatMoney, formatDate, formatDateTime, formatNumber } = useDateFormat();
const { can } = usePermissions();

const order = ref<PurchaseOrderDetailDto | null>(null);
const loading = ref(true);
const accessState = ref<"forbidden" | "notFound" | null>(null);
const form = reactive({ notes: "", deliveryDate: "" });
const saving = ref(false);
const dialog = ref<"send" | "cancel" | "confirm" | null>(null);
const reason = ref("");
const supplierNote = ref("");
const acting = ref(false);

const canManage = computed(() => props.internal && can("orders.manage"));
const dirty = computed(() => !!order.value && (form.notes !== (order.value.notes ?? "") || form.deliveryDate !== (order.value.deliveryDate?.slice(0, 10) ?? "")));
const listPath = computed(() => (props.internal ? "/admin/orders" : "/supplier/orders"));

async function load() {
  loading.value = true;
  try {
    order.value = await api.get<PurchaseOrderDetailDto>(`/api/purchase-orders/${props.orderId}`);
    form.notes = order.value.notes ?? "";
    form.deliveryDate = order.value.deliveryDate?.slice(0, 10) ?? "";
  } catch (error: any) {
    const code = error?.response?.status ?? error?.statusCode;
    if (code === 403 || code === 404) accessState.value = code === 403 ? "forbidden" : "notFound";
    else toast.error(apiErrorMessage(error, t("orders.loadError")));
  } finally {
    loading.value = false;
  }
}

async function saveDraft() {
  saving.value = true;
  try {
    await api.put(`/api/purchase-orders/${props.orderId}`, {
      notes: form.notes.trim() || null,
      deliveryDate: form.deliveryDate ? `${form.deliveryDate}T00:00:00Z` : null,
    });
    toast.success(t("orders.saved"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("orders.actionError")));
  } finally {
    saving.value = false;
  }
}

async function act() {
  const kind = dialog.value;
  if (!kind) return;
  acting.value = true;
  try {
    if (kind === "send") await api.post(`/api/purchase-orders/${props.orderId}/send`);
    if (kind === "cancel") await api.post(`/api/purchase-orders/${props.orderId}/cancel`, { reason: reason.value.trim() || null });
    if (kind === "confirm") await api.post(`/api/purchase-orders/${props.orderId}/confirm`, { note: supplierNote.value.trim() || null });
    toast.success(t(`orders.done.${kind}`));
    dialog.value = null;
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("orders.actionError")));
  } finally {
    acting.value = false;
  }
}

function print() {
  window.print();
}

onMounted(load);
</script>

<template>
  <AccessState v-if="accessState" :kind="accessState" :back-to="listPath" :back-label="internal ? $t('nav.orders') : $t('nav.myOrders')" />
  <LoadingSpinner v-else-if="loading && !order" />
  <div v-else-if="order" class="space-y-6" data-testid="order-detail">
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div>
        <NuxtLink :to="listPath" class="text-xs text-indigo-600 hover:underline print:hidden">← {{ internal ? $t("nav.orders") : $t("nav.myOrders") }}</NuxtLink>
        <h1 class="mt-1 flex flex-wrap items-center gap-2 text-xl font-bold text-slate-800">
          <span class="font-mono">{{ order.orderNumber }}</span>
          <AppBadge :tone="orderStatusTone[order.status]" data-testid="order-status">{{ $t(`orders.status.${order.status}`) }}</AppBadge>
        </h1>
        <p class="mt-1 text-sm text-slate-500">
          {{ $t(`orders.kind.${order.sourceKind}`) }}:
          <NuxtLink :to="orderSourcePath(order.sourceKind, order.sourceId, internal)" class="text-indigo-600 hover:underline">{{ order.sourceTitle }}</NuxtLink>
        </p>
      </div>
      <div class="flex flex-wrap gap-2 print:hidden">
        <AppButton variant="ghost" @click="print"><PrinterIcon class="h-4 w-4" />{{ $t("orders.print") }}</AppButton>
        <template v-if="canManage">
          <AppButton v-if="order.status === 'Draft'" data-testid="order-send" @click="dialog = 'send'">{{ $t("orders.send") }}</AppButton>
          <AppButton v-if="order.status === 'Draft' || order.status === 'Sent'" variant="danger-outline" data-testid="order-cancel" @click="dialog = 'cancel'">{{ $t("orders.cancel") }}</AppButton>
        </template>
        <AppButton v-if="!internal && order.status === 'Sent'" variant="success" data-testid="order-confirm" @click="dialog = 'confirm'">{{ $t("orders.confirm") }}</AppButton>
      </div>
    </div>

    <p v-if="!internal && order.status === 'Sent'" class="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800 print:hidden">{{ $t("orders.confirmHint") }}</p>

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-3">
      <AppCard :title="$t('orders.parties')">
        <dl class="space-y-2 text-sm">
          <div><dt class="text-xs text-slate-500">{{ $t("orders.buyer") }}</dt><dd class="font-medium text-slate-800">{{ order.ownerCompanyName }}</dd></div>
          <div><dt class="text-xs text-slate-500">{{ $t("orders.supplier") }}</dt><dd class="font-medium text-slate-800">{{ order.supplierCompanyName }}</dd></div>
        </dl>
      </AppCard>
      <AppCard :title="$t('orders.timeline')">
        <dl class="space-y-2 text-sm">
          <div class="flex justify-between gap-2"><dt class="text-slate-500">{{ $t("orders.createdAt") }}</dt><dd>{{ formatDateTime(order.createdAt) }}</dd></div>
          <div v-if="order.sentAt" class="flex justify-between gap-2"><dt class="text-slate-500">{{ $t("orders.sentAt") }}</dt><dd>{{ formatDateTime(order.sentAt) }}</dd></div>
          <div v-if="order.confirmedAt" class="flex justify-between gap-2"><dt class="text-slate-500">{{ $t("orders.confirmedAt") }}</dt><dd>{{ formatDateTime(order.confirmedAt) }}</dd></div>
          <div v-if="order.cancelledAt" class="flex justify-between gap-2"><dt class="text-slate-500">{{ $t("orders.cancelledAt") }}</dt><dd>{{ formatDateTime(order.cancelledAt) }}</dd></div>
        </dl>
      </AppCard>
      <AppCard :title="$t('orders.terms')">
        <form v-if="canManage && order.status === 'Draft'" class="space-y-3" data-testid="order-draft-form" @submit.prevent="saveDraft">
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("orders.deliveryDate") }}
            <input v-model="form.deliveryDate" type="date" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" />
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("orders.notes") }}
            <textarea v-model="form.notes" rows="3" maxlength="2000" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" />
          </label>
          <AppButton type="submit" variant="secondary" :loading="saving" :disabled="!dirty">{{ $t("common.save") }}</AppButton>
        </form>
        <dl v-else class="space-y-2 text-sm">
          <div><dt class="text-xs text-slate-500">{{ $t("orders.deliveryDate") }}</dt><dd>{{ order.deliveryDate ? formatDate(order.deliveryDate) : "—" }}</dd></div>
          <div><dt class="text-xs text-slate-500">{{ $t("orders.notes") }}</dt><dd class="whitespace-pre-line">{{ order.notes || "—" }}</dd></div>
          <div v-if="order.supplierNote"><dt class="text-xs text-slate-500">{{ $t("orders.supplierNote") }}</dt><dd class="whitespace-pre-line" data-testid="order-supplier-note">{{ order.supplierNote }}</dd></div>
        </dl>
      </AppCard>
    </div>

    <AppCard :title="$t('orders.lines')">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">#</th>
              <th class="pb-2">{{ $t("orders.item") }}</th>
              <th class="pb-2 text-right">{{ $t("orders.quantity") }}</th>
              <th class="pb-2 text-right">{{ $t("orders.unitPrice") }}</th>
              <th class="pb-2 text-right">{{ $t("orders.lineTotal") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(l, i) in order.lines" :key="l.id" class="border-b border-slate-50">
              <td class="py-2 text-slate-400">{{ i + 1 }}</td>
              <td class="py-2 text-slate-700">{{ l.description }}</td>
              <td class="py-2 text-right tabular-nums">{{ formatNumber(l.quantity) }} {{ $t(`enums.unitOfMeasure.${l.unitOfMeasure}`) }}</td>
              <td class="py-2 text-right tabular-nums">{{ formatMoney(l.unitPrice, order.currencyCode) }}</td>
              <td class="py-2 text-right font-medium tabular-nums">{{ formatMoney(l.lineTotal, order.currencyCode) }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr>
              <td colspan="4" class="pt-3 text-right text-sm font-semibold text-slate-600">{{ $t("orders.total") }}</td>
              <td class="pt-3 text-right text-base font-bold tabular-nums text-slate-800" data-testid="order-total">{{ formatMoney(order.totalAmount, order.currencyCode) }}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </AppCard>

    <ConfirmDialog
      :open="dialog !== null"
      :title="dialog ? $t(`orders.dialog.${dialog}Title`) : ''"
      :confirm-label="dialog ? $t(`orders.dialog.${dialog}Confirm`) : undefined"
      :tone="dialog === 'cancel' ? 'danger' : 'primary'"
      :loading="acting"
      @confirm="act"
      @cancel="dialog = null"
    >
      <p>{{ dialog ? $t(`orders.dialog.${dialog}Body`, { number: order.orderNumber, supplier: order.supplierCompanyName }) : "" }}</p>
      <label v-if="dialog === 'cancel' && order.status === 'Sent'" class="mt-3 flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("orders.cancelReason") }}
        <textarea v-model="reason" rows="2" maxlength="500" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" data-testid="order-cancel-reason" />
      </label>
      <label v-if="dialog === 'confirm'" class="mt-3 flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("orders.supplierNoteLabel") }}
        <textarea v-model="supplierNote" rows="3" maxlength="1000" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" data-testid="order-confirm-note" />
      </label>
    </ConfirmDialog>
  </div>
</template>
