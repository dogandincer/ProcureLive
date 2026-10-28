<script setup lang="ts">
import type { AuctionDetailDto } from "~/types/api";
import { CheckCircleIcon, ClockIcon, XCircleIcon } from "@heroicons/vue/24/solid";

/**
 * Tedarikçi tarafı katılım talebi: ihalenin firmasının onaylı carisi değilse kalem bazında ön teklif + not ile talep
 * gönderilir; ardından adımlar (talep → cari onayı → ön teklif kabulü) izlenir. Red sonrası yeniden talep açılabilir.
 */
const props = defineProps<{ auction: AuctionDetailDto }>();
const emit = defineEmits<{ changed: [] }>();
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const request = computed(() => props.auction.myParticipationRequest);
const active = computed(() => request.value?.status === "Pending" || request.value?.status === "CompanyApproved");
const showForm = computed(() => props.auction.requiresApproval);

const prices = reactive<Record<string, number | null>>({});
const lineNotes = reactive<Record<string, string>>({});
const note = ref("");
const submitting = ref(false);
const formError = ref("");

const total = computed(() =>
  props.auction.items.reduce((sum, i) => sum + (prices[i.id] ? Number(prices[i.id]) * i.quantity : 0), 0),
);
const money = (n: number) => n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const steps = computed(() => {
  const s = request.value?.status;
  return [
    { key: "sent", done: !!request.value },
    { key: "companyApproved", done: s === "CompanyApproved" || s === "Accepted" },
    { key: "accepted", done: s === "Accepted" },
  ];
});

async function submit() {
  formError.value = "";
  const lines = props.auction.items
    .filter((i) => prices[i.id] && Number(prices[i.id]) > 0)
    .map((i) => ({ auctionItemId: i.id, unitPrice: Number(prices[i.id]), note: lineNotes[i.id]?.trim() || null }));
  if (lines.length === 0) {
    formError.value = t("participation.form.noLines");
    return;
  }
  if (!props.auction.allowPartialBids && lines.length !== props.auction.items.length) {
    formError.value = t("participation.form.allItemsRequired");
    return;
  }
  submitting.value = true;
  try {
    await api.post(`/api/auctions/${props.auction.id}/participation-requests`, { note: note.value.trim() || null, lines });
    toast.success(t("participation.sentToast"));
    emit("changed");
  } catch (error: any) {
    formError.value = apiErrorMessage(error, t("participation.form.error"));
  } finally {
    submitting.value = false;
  }
}

// Bildirimden gelinirse forma kaydır.
onMounted(() => {
  if (location.hash === "#participation") document.getElementById("participation")?.scrollIntoView({ behavior: "smooth" });
});
</script>

<template>
  <div id="participation" class="space-y-4 rounded-xl border border-indigo-200 bg-surface p-5">
    <div>
      <h2 class="text-sm font-semibold text-slate-800">{{ $t("participation.title") }}</h2>
      <p class="mt-0.5 text-xs text-slate-500">{{ $t("participation.intro", { owner: auction.ownerCompanyName }) }}</p>
    </div>

    <!-- Durum: adımlar -->
    <div v-if="request" class="rounded-lg border border-slate-100 bg-slate-50 p-3">
      <ol class="flex flex-wrap items-center gap-x-4 gap-y-2 text-xs">
        <li v-for="(step, i) in steps" :key="step.key" class="flex items-center gap-1.5">
          <CheckCircleIcon v-if="step.done" class="h-4 w-4 text-emerald-500" />
          <XCircleIcon v-else-if="request.status === 'Rejected'" class="h-4 w-4 text-rose-400" />
          <ClockIcon v-else class="h-4 w-4 text-amber-400" />
          <span :class="step.done ? 'font-medium text-slate-700' : 'text-slate-500'">{{ i + 1 }}. {{ $t(`participation.steps.${step.key}`) }}</span>
        </li>
      </ol>
      <p class="mt-2 text-xs text-slate-500">
        {{ $t("participation.sentAt", { date: formatDateTime(request.createdAt) }) }} ·
        <span class="font-semibold">{{ $t(`enums.participationStatus.${request.status}`) }}</span>
      </p>
      <p v-if="request.status === 'Rejected'" class="mt-2 rounded-md bg-rose-50 px-2 py-1.5 text-xs text-rose-700">
        {{ $t("participation.rejected") }}<span v-if="request.rejectReason"> — {{ request.rejectReason }}</span>
      </p>
      <p v-else-if="active" class="mt-2 text-xs text-indigo-700">{{ $t(`participation.waiting.${request.status}`) }}</p>

      <table v-if="active" class="mt-3 w-full text-xs">
        <thead>
          <tr class="text-left text-[11px] uppercase text-slate-400">
            <th class="pb-1">{{ $t("participation.form.item") }}</th>
            <th class="pb-1 text-right">{{ $t("participation.form.unitPrice") }}</th>
            <th class="pb-1 text-right">{{ $t("participation.form.lineTotal") }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="l in request.lines" :key="l.auctionItemId" class="border-t border-slate-100">
            <td class="py-1 text-slate-600">{{ l.itemDescription }} <span class="text-slate-400">× {{ l.quantity }}</span></td>
            <td class="py-1 text-right font-mono">{{ money(l.unitPrice) }}</td>
            <td class="py-1 text-right font-mono">{{ money(l.lineTotal) }}</td>
          </tr>
          <tr class="border-t border-slate-200 font-semibold">
            <td class="py-1" colspan="2">{{ $t("participation.form.total") }}</td>
            <td class="py-1 text-right font-mono">{{ money(request.totalAmount) }} {{ request.currencyCode }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Talep formu (ilk talep ya da red sonrası yeniden) -->
    <form v-if="showForm" class="space-y-3" @submit.prevent="submit">
      <p class="text-xs text-slate-500">
        {{ auction.allowPartialBids ? $t("participation.form.partialHint") : $t("participation.form.allHint") }}
      </p>
      <!-- Dar ekranda miktar kalem altına iner, not sütunu gizlenir (genel not alanı kalır). -->
      <div>
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("participation.form.item") }}</th>
              <th class="hidden pb-2 sm:table-cell">{{ $t("participation.form.quantity") }}</th>
              <th class="pb-2">{{ $t("participation.form.unitPrice") }} ({{ auction.currencyCode }})</th>
              <th class="hidden pb-2 sm:table-cell">{{ $t("participation.form.lineNote") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in auction.items" :key="item.id" class="border-b border-slate-50">
              <td class="py-2 pr-2 font-medium text-slate-700">
                {{ item.description }}
                <span class="block text-xs font-normal text-slate-400 sm:hidden">{{ item.quantity }} {{ $t(`enums.unitOfMeasure.${item.unitOfMeasure}`) }}</span>
              </td>
              <td class="hidden py-2 text-slate-500 sm:table-cell">{{ item.quantity }} {{ $t(`enums.unitOfMeasure.${item.unitOfMeasure}`) }}</td>
              <td class="py-2">
                <input
                  v-model.number="prices[item.id]"
                  type="number"
                  min="0"
                  step="0.01"
                  :required="!auction.allowPartialBids"
                  :aria-label="$t('participation.form.unitPrice') + ' — ' + item.description"
                  class="w-28 rounded-md border border-slate-300 px-2 py-1 text-sm sm:w-32"
                />
              </td>
              <td class="hidden py-2 sm:table-cell">
                <input v-model="lineNotes[item.id]" maxlength="500" class="w-full rounded-md border border-slate-300 px-2 py-1 text-sm" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <textarea v-model="note" rows="2" maxlength="1000" :placeholder="$t('participation.form.note')" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <div class="flex flex-wrap items-center justify-between gap-3">
        <span class="text-sm text-slate-600">
          {{ $t("participation.form.total") }}: <span class="font-mono font-semibold">{{ money(total) }} {{ auction.currencyCode }}</span>
        </span>
        <AppButton type="submit" :loading="submitting">{{ $t("participation.form.submit") }}</AppButton>
      </div>
      <p v-if="formError" class="rounded-md bg-rose-50 px-3 py-2 text-xs text-rose-700" role="alert">{{ formError }}</p>
    </form>
  </div>
</template>
