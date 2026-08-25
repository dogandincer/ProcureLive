<script setup lang="ts">
import type { CurrencyDto, RfqProcessDetailDto } from "~/types/api";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const { currentUser } = useAuth();

const process = ref<RfqProcessDetailDto | null>(null);
const currencies = ref<CurrencyDto[]>([]);
const declineReason = ref("");
const showDecline = ref(false);
const lines = reactive<Record<string, { unitPrice: number | null; currencyId: string; leadTimeDays: number | null }>>({});
const submitting = ref(false);
const submitted = ref(false);
const actionError = ref("");
const accessDenied = ref(false);

const myInvitation = computed(() =>
  process.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId) ?? null,
);

const canQuote = computed(() => myInvitation.value && ["Accepted", "Participating"].includes(myInvitation.value.status));

async function load() {
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
      throw error;
    }
  }
}

async function accept() {
  actionError.value = "";
  try {
    await api.post(`/api/rfq/processes/${processId}/invitations/respond`, { accept: true, declineReason: null });
    await load();
  } catch (error: any) {
    actionError.value = error?.data?.title ?? "İşlem gerçekleştirilemedi.";
  }
}

async function decline() {
  actionError.value = "";
  try {
    await api.post(`/api/rfq/processes/${processId}/invitations/respond`, {
      accept: false,
      declineReason: declineReason.value || "Belirtilmedi",
    });
    showDecline.value = false;
    // Declining revokes read access to this process (server-side), so re-fetching would 403.
    const invitation = process.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId);
    if (invitation) invitation.status = "Declined";
  } catch (error: any) {
    actionError.value = error?.data?.title ?? "İşlem gerçekleştirilemedi.";
  }
}

async function submitQuote() {
  if (!process.value) return;
  submitting.value = true;
  actionError.value = "";
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
    submitted.value = true;
    await load();
  } catch (error: any) {
    actionError.value = error?.data?.title ?? "İşlem gerçekleştirilemedi.";
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div v-if="accessDenied" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
    <p class="text-sm font-medium text-slate-600">Bu RFQ sürecine erişiminiz yok. Daveti reddetmiş olabilirsiniz.</p>
  </div>

  <div v-else-if="process" class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
      <p class="text-sm text-slate-500">Son Tarih: {{ new Date(process.dueDate).toLocaleDateString("tr-TR") }}</p>
    </div>

    <div v-if="actionError" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ actionError }}</span>
      <button class="font-semibold" @click="actionError = ''">Kapat</button>
    </div>

    <div v-if="myInvitation?.status === 'Declined'" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
      <p class="text-sm font-medium text-slate-600">Bu RFQ davetini reddettiniz. Bu sürece artık erişiminiz yok.</p>
    </div>

    <div v-else-if="myInvitation?.status === 'Invited'" class="rounded-xl border border-amber-200 bg-amber-50 p-5">
      <p class="text-sm font-medium text-amber-800">Bu RFQ sürecine davetlisiniz.</p>
      <div class="mt-3 flex gap-2">
        <button class="rounded-md bg-emerald-600 px-4 py-2 text-sm font-semibold text-white" @click="accept">Kabul Et</button>
        <button class="rounded-md bg-rose-600 px-4 py-2 text-sm font-semibold text-white" @click="showDecline = true">Reddet</button>
      </div>
      <div v-if="showDecline" class="mt-3 flex gap-2">
        <input v-model="declineReason" placeholder="Red gerekçesi" class="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <button class="rounded-md bg-rose-600 px-4 py-2 text-sm font-semibold text-white" @click="decline">Onayla</button>
      </div>
    </div>

    <form v-else-if="canQuote" class="space-y-4 rounded-xl border border-slate-200 bg-white p-5" @submit.prevent="submitQuote">
      <p v-if="submitted" class="rounded-md bg-emerald-50 px-3 py-2 text-sm text-emerald-700">Teklifiniz kaydedildi.</p>
      <p class="text-xs text-slate-400">Her kalem için farklı para biriminde teklif verebilirsiniz.</p>

      <div v-for="li in process.lineItems" :key="li.id" class="rounded-lg border border-slate-100 p-3">
        <div class="text-sm font-medium text-slate-700">{{ li.description }}</div>
        <div class="text-xs text-slate-500">{{ li.quantity }} {{ li.unitOfMeasure }}</div>
        <div class="mt-2 flex flex-wrap gap-2">
          <input v-model.number="lines[li.id].unitPrice" type="number" step="0.01" placeholder="Birim Fiyat" required class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <select v-model="lines[li.id].currencyId" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
          </select>
          <input v-model.number="lines[li.id].leadTimeDays" type="number" placeholder="Termin (gün)" class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        </div>
      </div>

      <button type="submit" :disabled="submitting" class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50">
        {{ submitting ? "Gönderiliyor..." : "Teklifi Gönder" }}
      </button>
    </form>

    <div v-else class="rounded-xl border border-slate-200 bg-white p-5 text-sm text-slate-500">
      Bu süreçle etkileşiminiz bulunmuyor.
    </div>
  </div>
</template>
