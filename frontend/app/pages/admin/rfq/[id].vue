<script setup lang="ts">
import type { CompanyDto, PagedResult, RfqComparisonRowDto, RfqProcessDetailDto, RfqQuoteDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const toast = useToast();

const process = ref<RfqProcessDetailDto | null>(null);
const companies = ref<CompanyDto[]>([]);
const matrix = ref<RfqComparisonRowDto[]>([]);
const quotes = ref<RfqQuoteDto[]>([]);
const inviteCompanyId = ref("");
const newLine = reactive({ description: "", quantity: 1, unitOfMeasure: "Piece" });
const loading = ref(true);

async function runAction(action: () => Promise<void>) {
  try {
    await action();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "İşlem gerçekleştirilemedi."));
  }
}

const unitOfMeasures = ["Piece", "Kilogram", "Gram", "Liter", "Meter", "SquareMeter", "CubicMeter", "Box", "Pallet", "Hour", "Service"];

const groupedMatrix = computed(() => {
  const groups: Record<string, RfqComparisonRowDto[]> = {};
  for (const row of matrix.value) {
    groups[row.lineItemDescription] ??= [];
    groups[row.lineItemDescription].push(row);
  }
  return groups;
});

async function load() {
  importSortOrderCounter = null;
  loading.value = true;
  try {
    const [detail, companyPage] = await Promise.all([
      api.get<RfqProcessDetailDto>(`/api/rfq/processes/${processId}`),
      api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100, companyType: "Supplier" }),
    ]);
    process.value = detail;
    companies.value = companyPage.items;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "RFQ süreci yüklenemedi."));
  } finally {
    loading.value = false;
  }
}

async function loadMatrix() {
  try {
    matrix.value = await api.get<RfqComparisonRowDto[]>(`/api/rfq/processes/${processId}/comparison-matrix`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Karşılaştırma matrisi yüklenemedi."));
  }
}

async function loadQuotes() {
  try {
    quotes.value = await api.get<RfqQuoteDto[]>(`/api/rfq/processes/${processId}/quotes`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Teklifler yüklenemedi."));
  }
}

async function award(quoteId: string) {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/award`, { winningQuoteId: quoteId, notes: null });
    toast.success("Kazanan seçildi.");
    await load();
  });
}

async function addLine() {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/line-items`, {
      productId: null,
      description: newLine.description,
      quantity: newLine.quantity,
      unitOfMeasure: newLine.unitOfMeasure,
      sortOrder: (process.value?.lineItems.length ?? 0) + 1,
    });
    newLine.description = "";
    toast.success("Kalem eklendi.");
    await load();
  });
}

const importLineColumns: ExcelColumn[] = [
  { key: "description", label: "Açıklama", required: true },
  { key: "quantity", label: "Miktar", required: true, type: "number" },
  { key: "unitOfMeasure", label: "Birim", hint: unitOfMeasures.join("/") },
];

// process.value only refreshes via load(), which we don't call per-row during a bulk import - so
// lineItems.length would stay frozen and every imported row would collide on the same sortOrder.
// Track a local counter across the whole import session instead.
let importSortOrderCounter: number | null = null;

async function importLineRow(row: Record<string, string | number | boolean | null>) {
  if (importSortOrderCounter === null) {
    importSortOrderCounter = process.value?.lineItems.length ?? 0;
  }
  importSortOrderCounter += 1;

  await api.post(`/api/rfq/processes/${processId}/line-items`, {
    productId: null,
    description: row.description,
    quantity: row.quantity,
    unitOfMeasure: unitOfMeasures.includes(String(row.unitOfMeasure)) ? row.unitOfMeasure : "Piece",
    sortOrder: importSortOrderCounter,
  });
}

async function invite() {
  if (!inviteCompanyId.value) return;
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/invitations`, { companyIds: [inviteCompanyId.value] });
    inviteCompanyId.value = "";
    toast.success("Cari davet edildi.");
    await load();
  });
}

async function publish() {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/publish`);
    toast.success("RFQ yayınlandı.");
    await load();
  });
}

async function close() {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/close`);
    toast.success("RFQ süreci kapatıldı.");
    await load();
  });
}

onMounted(async () => {
  await load();
  if (!process.value) return;
  await loadMatrix();
  await loadQuotes();
});
</script>

<template>
  <LoadingSpinner v-if="loading" />

  <div v-else-if="process" class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
        <p class="text-sm text-slate-500">Durum: {{ process.status }} · Para Birimi: {{ process.currencyCode }}</p>
      </div>
      <div class="flex gap-2">
        <AppButton v-if="process.status === 'Sent'" @click="publish">
          Yayınla (Adım 4/4)
        </AppButton>
        <AppButton v-if="!['Closed', 'Awarded'].includes(process.status)" variant="secondary" @click="close">
          Süreci Kapat
        </AppButton>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <AppCard>
        <div class="flex items-center justify-between">
          <StepBadge :step="2" :total="4" label="Kalemler" />
          <ExcelImportButton
            v-if="process.status === 'Draft'"
            :columns="importLineColumns"
            template-filename="rfq-kalemler-sablon.xlsx"
            :import-row="importLineRow"
            label="Excel'den Aktar"
            @imported="load"
          />
        </div>
        <ul v-if="process.lineItems.length > 0" class="mt-3 space-y-1 text-sm text-slate-600">
          <li v-for="li in process.lineItems" :key="li.id">{{ li.description }} — {{ li.quantity }} {{ li.unitOfMeasure }}</li>
        </ul>
        <EmptyState v-else class="mt-3">Henüz kalem yok</EmptyState>

        <form v-if="process.status === 'Draft'" class="mt-3 flex flex-wrap gap-2" @submit.prevent="addLine">
          <input v-model="newLine.description" placeholder="Açıklama" required class="min-w-[160px] flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model.number="newLine.quantity" type="number" min="0.01" step="0.01" class="w-24 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <select v-model="newLine.unitOfMeasure" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ u }}</option>
          </select>
          <AppButton type="submit" variant="secondary">Ekle</AppButton>
        </form>
        <p v-else class="mt-2 text-xs text-slate-400">Kalemler yalnızca taslak (Draft) aşamasında eklenebilir.</p>
      </AppCard>

      <AppCard>
        <StepBadge :step="3" :total="4" label="Davetliler" />
        <ul v-if="process.invitations.length > 0" class="mt-3 space-y-2 text-sm">
          <li v-for="inv in process.invitations" :key="inv.id" class="flex items-center justify-between">
            <span>{{ inv.companyName }}</span>
            <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ inv.status }}</span>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">Henüz davet yok</EmptyState>
        <div v-if="['Draft', 'Sent'].includes(process.status)" class="mt-3 flex gap-2">
          <select v-model="inviteCompanyId" class="flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option value="" disabled>Cari seç...</option>
            <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
          <AppButton @click="invite">Davet Et</AppButton>
        </div>
      </AppCard>
    </div>

    <AppCard>
      <h3 class="text-sm font-semibold text-slate-700">Teklif Karşılaştırma Matrisi <span class="text-xs font-normal text-slate-400">(çoklu para birimi normalize edilmiş)</span></h3>
      <div v-for="(rows, description) in groupedMatrix" :key="description" class="mt-4 overflow-x-auto">
        <div class="text-xs font-semibold uppercase text-slate-500">{{ description }}</div>
        <table class="mt-1 w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-1">Cari</th>
              <th class="pb-1">Birim Fiyat</th>
              <th class="pb-1">Normalize</th>
              <th class="pb-1">Termin (gün)</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in rows.sort((a, b) => a.normalizedUnitPriceInBaseCurrency - b.normalizedUnitPriceInBaseCurrency)" :key="row.companyId" class="border-b border-slate-50">
              <td class="py-1">{{ row.companyName }}</td>
              <td class="py-1 font-mono">{{ row.unitPrice }} {{ row.currencyCode }}</td>
              <td class="py-1 font-mono font-semibold text-emerald-700">{{ row.normalizedUnitPriceInBaseCurrency.toFixed(2) }}</td>
              <td class="py-1">{{ row.leadTimeDays ?? "-" }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <EmptyState v-if="matrix.length === 0">Henüz teklif gelmedi.</EmptyState>
    </AppCard>

    <AppCard>
      <h3 class="text-sm font-semibold text-slate-700">Teklifler</h3>
      <p v-if="process.status === 'Awarded'" class="mt-1 text-xs text-emerald-700">
        Kazanan: {{ quotes.find((q) => q.id === process!.winningQuoteId)?.companyName ?? "-" }}
      </p>
      <ul v-if="quotes.filter((q) => q.status === 'Submitted').length > 0" class="mt-3 space-y-2 text-sm">
        <li
          v-for="q in quotes.filter((q) => q.status === 'Submitted')"
          :key="q.id"
          class="flex items-center justify-between rounded-lg border border-slate-100 px-3 py-2"
          :class="{ 'border-emerald-300 bg-emerald-50': q.id === process.winningQuoteId }"
        >
          <span>{{ q.companyName }}</span>
          <button
            v-if="process.status === 'Closed'"
            class="rounded bg-indigo-600 px-2 py-1 text-xs font-semibold text-white"
            @click="award(q.id)"
          >
            Kazanan Seç
          </button>
          <AppBadge v-else-if="q.id === process.winningQuoteId" tone="emerald">Kazandı</AppBadge>
        </li>
      </ul>
      <EmptyState v-else class="mt-3">Henüz gönderilmiş teklif yok</EmptyState>
      <p v-if="process.status !== 'Closed' && process.status !== 'Awarded'" class="mt-2 text-xs text-slate-400">
        Kazanan seçmeden önce süreci kapatmanız gerekir.
      </p>
    </AppCard>
  </div>
</template>
