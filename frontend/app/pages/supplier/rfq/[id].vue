<script setup lang="ts">
import type { CurrencyDto, RfqProcessDetailDto, RfqQuoteDto } from "~/types/api";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();
const { currentUser } = useAuth();
const { isPlus, refresh: refreshSubscription } = useSubscription();

const process = ref<RfqProcessDetailDto | null>(null);
const currencies = ref<CurrencyDto[]>([]);
const loading = ref(true);
const declineReason = ref("");
const showDecline = ref(false);
const lines = reactive<Record<string, { unitPrice: number | null; currencyId: string; leadTimeDays: number | null }>>({});
const submitting = ref(false);
const accessDenied = ref(false);

const myInvitation = computed(() =>
  process.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId) ?? null,
);

const accepted = computed(() => !!myInvitation.value && ["Accepted", "Participating"].includes(myInvitation.value.status));
// Standart kota dolu ve davet kabul edilmemiş: kalemler sunucuda düşürüldü → "Plus alınız".
const restricted = computed(() => !!process.value && process.value.accessLevel !== "Full");
const showQa = computed(() => !!process.value && !restricted.value && !!myInvitation.value && myInvitation.value.status !== "Declined");
const qaOpen = computed(() => !!process.value && ["Sent", "InProgress"].includes(process.value.status));
// İlk teklif herkese; gönderilmiş teklifin revizyonu yalnızca Plus'a ve süreç açıkken (sunucu da zorlar).
const canSubmit = computed(() => !!process.value?.canSubmit);

async function load() {
  loading.value = true;
  try {
    const [detail, currencyList] = await Promise.all([
      api.get<RfqProcessDetailDto>(`/api/rfq/processes/${processId}`),
      api.get<CurrencyDto[]>("/api/currencies"),
    ]);
    // Gönderilmiş teklif (tedarikçiye yalnızca kendi teklifi döner) formu doldurur. Form durumu, sayfa
    // `process` ile render edilmeden önce hazırlanır (aradaki await sırasında satırlar tanımsız kalmasın).
    const quotes = detail.hasSubmitted ? await api.get<RfqQuoteDto[]>(`/api/rfq/processes/${processId}/quotes`) : [];
    const mine = new Map((quotes[0]?.lineItems ?? []).map((l) => [l.rfqLineItemId, l]));
    for (const li of detail.lineItems) {
      const q = mine.get(li.id);
      lines[li.id] = { unitPrice: q?.unitPrice ?? null, currencyId: q?.currencyId ?? detail.currencyId, leadTimeDays: q?.leadTimeDays ?? null };
    }
    currencies.value = currencyList;
    process.value = detail;
  } catch (error: any) {
    if (error?.response?.status === 403) {
      accessDenied.value = true;
    } else {
      toast.error(apiErrorMessage(error, t("rfq.quote.loadError")));
    }
  } finally {
    loading.value = false;
  }
}

async function accept() {
  try {
    await api.post(`/api/rfq/processes/${processId}/invitations/respond`, { accept: true, declineReason: null });
    toast.success(t("rfq.quote.acceptedToast"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.quote.actionError")));
    if (apiErrorCode(error) === "QuotaExceeded") await Promise.all([load(), refreshSubscription()]);
  }
}

async function decline() {
  try {
    await api.post(`/api/rfq/processes/${processId}/invitations/respond`, {
      accept: false,
      declineReason: declineReason.value || t("common.notSpecified"),
    });
    showDecline.value = false;
    // Declining revokes read access to this process (server-side), so re-fetching would 403.
    const invitation = process.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId);
    if (invitation) invitation.status = "Declined";
    toast.success(t("rfq.quote.declinedToast"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.quote.actionError")));
  }
}

async function submitQuote() {
  if (!process.value) return;
  submitting.value = true;
  try {
    await api.post(`/api/rfq/processes/${processId}/quotes`, {
      validUntil: null,
      notes: null,
      lineItems: process.value.lineItems.map((li) => ({
        rfqLineItemId: li.id,
        unitPrice: lines[li.id].unitPrice,
        currencyId: lines[li.id].currencyId,
        leadTimeDays: lines[li.id].leadTimeDays,
        notes: null,
      })),
    });
    toast.success(t("rfq.quote.quoteSavedToast"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.quote.actionError")));
  } finally {
    submitting.value = false;
  }
}

onMounted(() => Promise.all([load(), refreshSubscription()]));
</script>

<template>
  <div v-if="accessDenied" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
    <p class="text-sm font-medium text-slate-600">{{ $t("rfq.quote.accessDenied") }}</p>
  </div>

  <LoadingSpinner v-else-if="loading && !process" />

  <div v-else-if="process" class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
      <p class="text-sm text-slate-500">
        {{ process.ownerCompanyName }} · {{ $t("rfq.quote.dueDate") }}: {{ formatDate(process.dueDate) }}
      </p>
    </div>

    <RestrictedOverlay
      v-if="restricted"
      level="QuotaExceeded"
      :title="$t('restricted.process.title')"
      :body="$t('restricted.process.body')"
      :rows="3"
    >
      <template #extra>
        <button type="button" class="mt-3 block w-full text-xs text-slate-400 hover:text-slate-600" @click="showDecline = true">
          {{ $t("restricted.process.declineInstead") }}
        </button>
        <div v-if="showDecline" class="mt-2 flex gap-2">
          <input v-model="declineReason" :placeholder="$t('common.declineReasonPlaceholder')" class="min-w-0 flex-1 rounded-md border border-slate-300 px-2 py-1 text-xs" />
          <AppButton variant="danger" @click="decline">{{ $t("common.confirm") }}</AppButton>
        </div>
      </template>
    </RestrictedOverlay>

    <div v-else-if="myInvitation?.status === 'Declined'" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
      <p class="text-sm font-medium text-slate-600">{{ $t("rfq.quote.declinedBanner") }}</p>
    </div>

    <div v-else-if="myInvitation?.status === 'Invited'" class="rounded-xl border border-amber-200 bg-amber-50 p-5">
      <p class="text-sm font-medium text-amber-800">{{ $t("rfq.quote.invitedBanner") }}</p>
      <div class="mt-3 flex gap-2">
        <AppButton @click="accept">{{ $t("common.accept") }}</AppButton>
        <AppButton variant="danger" @click="showDecline = true">{{ $t("common.decline") }}</AppButton>
      </div>
      <div v-if="showDecline" class="mt-3 flex gap-2">
        <input v-model="declineReason" :placeholder="$t('common.declineReasonPlaceholder')" class="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <AppButton variant="danger" @click="decline">{{ $t("common.confirm") }}</AppButton>
      </div>
    </div>

    <form v-else-if="accepted" class="space-y-4 rounded-xl border border-slate-200 bg-surface p-5" @submit.prevent="submitQuote">
      <!-- Durum: kapalı süreç / gönderildi (Standart: revizyon Plus'a özel) / Plus: revize edilebilir -->
      <p v-if="!process.isOpen" class="rounded-md bg-slate-50 px-3 py-2 text-xs text-slate-600">{{ $t("rfx.closed") }}</p>
      <div v-else-if="process.hasSubmitted && !canSubmit" class="flex flex-wrap items-center justify-between gap-2 rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800">
        <span>{{ $t("rfx.submittedStandardQuote") }}</span>
        <NuxtLink to="/supplier/pricing" class="rounded bg-amber-500 px-2 py-1 font-semibold text-white hover:bg-amber-600">{{ $t("restricted.upgradeCta") }}</NuxtLink>
      </div>
      <p v-else-if="process.hasSubmitted" class="rounded-md bg-indigo-50 px-3 py-2 text-xs text-indigo-700">{{ $t("rfx.submittedPlusQuote") }}</p>

      <p v-if="canSubmit" class="text-xs text-slate-400">{{ $t("rfq.quote.multiCurrencyHint") }}</p>

      <fieldset :disabled="!canSubmit" class="space-y-4 disabled:opacity-80">
        <div v-for="li in process.lineItems" :key="li.id" class="rounded-lg border border-slate-100 p-3">
          <div class="text-sm font-medium text-slate-700">{{ li.description }}</div>
          <div class="text-xs text-slate-500">{{ li.quantity }} {{ $t(`enums.unitOfMeasure.${li.unitOfMeasure}`) }}</div>
          <div class="mt-2 flex flex-wrap gap-2">
            <input
              v-model.number="lines[li.id].unitPrice"
              type="number"
              step="0.01"
              :placeholder="$t('rfq.quote.unitPricePlaceholder')"
              :aria-label="`${$t('rfq.quote.unitPricePlaceholder')} — ${li.description}`"
              required
              class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm disabled:bg-slate-50"
            />
            <select v-model="lines[li.id].currencyId" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm disabled:bg-slate-50">
              <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
            </select>
            <input v-model.number="lines[li.id].leadTimeDays" type="number" :placeholder="$t('rfq.quote.leadTimePlaceholder')" class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm disabled:bg-slate-50" />
          </div>
        </div>
      </fieldset>

      <AppButton v-if="canSubmit" type="submit" :loading="submitting">
        {{ submitting ? $t("rfq.quote.submitting") : process.hasSubmitted ? $t("rfx.reviseQuote") : $t("rfq.quote.submit") }}
      </AppButton>
      <p v-if="canSubmit && !process.hasSubmitted && !isPlus" class="text-[11px] text-slate-400">{{ $t("rfx.standardOnce") }}</p>
    </form>

    <div v-else class="rounded-xl border border-slate-200 bg-surface p-5 text-sm text-slate-500">
      {{ $t("rfq.quote.noInteraction") }}
    </div>

    <ProcessQaPanel v-if="showQa" kind="rfq" :process-id="processId" :internal="false" :can-ask="qaOpen" />
  </div>
</template>
