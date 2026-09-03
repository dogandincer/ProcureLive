<script setup lang="ts">
import type { CompanyDto, PagedResult, RfqComparisonRowDto, RfqProcessDetailDto } from "~/types/api";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();

const process = ref<RfqProcessDetailDto | null>(null);
const companies = ref<CompanyDto[]>([]);
const matrix = ref<RfqComparisonRowDto[]>([]);
const inviteCompanyId = ref("");
const newLine = reactive({ description: "", quantity: 1, unitOfMeasure: "Piece" });
const actionError = ref("");

async function runAction(action: () => Promise<void>) {
  actionError.value = "";
  try {
    await action();
  } catch (error: any) {
    actionError.value = apiErrorMessage(error, "İşlem gerçekleştirilemedi.");
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
  const [detail, companyPage] = await Promise.all([
    api.get<RfqProcessDetailDto>(`/api/rfq/processes/${processId}`),
    api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100, companyType: "Supplier" }),
  ]);
  process.value = detail;
  companies.value = companyPage.items;
}

async function loadMatrix() {
  matrix.value = await api.get<RfqComparisonRowDto[]>(`/api/rfq/processes/${processId}/comparison-matrix`);
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
    await load();
  });
}

async function invite() {
  if (!inviteCompanyId.value) return;
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/invitations`, { companyIds: [inviteCompanyId.value] });
    inviteCompanyId.value = "";
    await load();
  });
}

async function publish() {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/publish`);
    await load();
  });
}

async function close() {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/close`);
    await load();
  });
}

onMounted(async () => {
  await load();
  await loadMatrix();
});
</script>

<template>
  <div v-if="process" class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
        <p class="text-sm text-slate-500">Durum: {{ process.status }} · Para Birimi: {{ process.currencyCode }}</p>
      </div>
      <div class="flex gap-2">
        <button v-if="process.status === 'Sent'" class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white" @click="publish">
          Yayınla (Adım 4/4)
        </button>
        <button v-if="!['Closed', 'Awarded'].includes(process.status)" class="rounded-md bg-slate-700 px-4 py-2 text-sm font-semibold text-white" @click="close">
          Süreci Kapat
        </button>
      </div>
    </div>

    <div v-if="actionError" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ actionError }}</span>
      <button class="font-semibold" @click="actionError = ''">Kapat</button>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <h3 class="text-sm font-semibold text-slate-700">Kalemler (Adım 2/4)</h3>
        <ul class="mt-3 space-y-1 text-sm text-slate-600">
          <li v-for="li in process.lineItems" :key="li.id">{{ li.description }} — {{ li.quantity }} {{ li.unitOfMeasure }}</li>
          <li v-if="process.lineItems.length === 0" class="text-slate-400">Henüz kalem yok</li>
        </ul>

        <form v-if="process.status === 'Draft'" class="mt-3 flex flex-wrap gap-2" @submit.prevent="addLine">
          <input v-model="newLine.description" placeholder="Açıklama" required class="min-w-[160px] flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model.number="newLine.quantity" type="number" min="0.01" step="0.01" class="w-24 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <select v-model="newLine.unitOfMeasure" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ u }}</option>
          </select>
          <button type="submit" class="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white">Ekle</button>
        </form>
        <p v-else class="mt-2 text-xs text-slate-400">Kalemler yalnızca taslak (Draft) aşamasında eklenebilir.</p>
      </div>

      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <h3 class="text-sm font-semibold text-slate-700">Davetliler (Adım 3/4)</h3>
        <ul class="mt-3 space-y-2 text-sm">
          <li v-for="inv in process.invitations" :key="inv.id" class="flex items-center justify-between">
            <span>{{ inv.companyName }}</span>
            <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ inv.status }}</span>
          </li>
          <li v-if="process.invitations.length === 0" class="text-slate-400">Henüz davet yok</li>
        </ul>
        <div v-if="['Draft', 'Sent'].includes(process.status)" class="mt-3 flex gap-2">
          <select v-model="inviteCompanyId" class="flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option value="" disabled>Cari seç...</option>
            <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
          <button class="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-semibold text-white" @click="invite">Davet Et</button>
        </div>
      </div>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <h3 class="text-sm font-semibold text-slate-700">Teklif Karşılaştırma Matrisi <span class="text-xs font-normal text-slate-400">(çoklu para birimi normalize edilmiş)</span></h3>
      <div v-for="(rows, description) in groupedMatrix" :key="description" class="mt-4">
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
      <p v-if="matrix.length === 0" class="mt-2 text-sm text-slate-400">Henüz teklif gelmedi.</p>
    </div>
  </div>
</template>
