<script setup lang="ts">
import type { CurrencyDto, RfqProcessDetailDto } from "~/types/api";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();
const { currentUser } = useAuth();

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

const canQuote = computed(() => myInvitation.value && ["Accepted", "Participating"].includes(myInvitation.value.status));

async function load() {
  loading.value = true;
  try {
    const [detail, currencyList] = await Promise.all([
      api.get<RfqProcessDetailDto>(`/api/rfq/processes/${processId}`),
      api.get<CurrencyDto[]>("/api/currencies"),
    ]);
    process.value = detail;
    currencies.value = currencyList;

    for (const li of detail.lineItems) {
      lines[li.id] ??= { unitPrice: null, currencyId: detail.currencyId, leadTimeDays: null };
    }
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

onMounted(load);
</script>

<template>
  <div v-if="accessDenied" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
    <p class="text-sm font-medium text-slate-600">{{ $t("rfq.quote.accessDenied") }}</p>
  </div>

  <LoadingSpinner v-else-if="loading" />

  <div v-else-if="process" class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
      <p class="text-sm text-slate-500">{{ $t("rfq.quote.dueDate") }}: {{ formatDate(process.dueDate) }}</p>
    </div>

    <div v-if="myInvitation?.status === 'Declined'" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
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

    <form v-else-if="canQuote" class="space-y-4 rounded-xl border border-slate-200 bg-white p-5" @submit.prevent="submitQuote">
      <p class="text-xs text-slate-400">{{ $t("rfq.quote.multiCurrencyHint") }}</p>

      <div v-for="li in process.lineItems" :key="li.id" class="rounded-lg border border-slate-100 p-3">
        <div class="text-sm font-medium text-slate-700">{{ li.description }}</div>
        <div class="text-xs text-slate-500">{{ li.quantity }} {{ $t(`enums.unitOfMeasure.${li.unitOfMeasure}`) }}</div>
        <div class="mt-2 flex flex-wrap gap-2">
          <input v-model.number="lines[li.id].unitPrice" type="number" step="0.01" :placeholder="$t('rfq.quote.unitPricePlaceholder')" required class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <select v-model="lines[li.id].currencyId" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
          </select>
          <input v-model.number="lines[li.id].leadTimeDays" type="number" :placeholder="$t('rfq.quote.leadTimePlaceholder')" class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        </div>
      </div>

      <AppButton type="submit" :loading="submitting">
        {{ submitting ? $t("rfq.quote.submitting") : $t("rfq.quote.submit") }}
      </AppButton>
    </form>

    <div v-else class="rounded-xl border border-slate-200 bg-white p-5 text-sm text-slate-500">
      {{ $t("rfq.quote.noInteraction") }}
    </div>
  </div>
</template>
