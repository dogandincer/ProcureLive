<script setup lang="ts">
import type { RfqAiRecommendationDto, RfqComparisonRowDto, RfqProcessDetailDto, RfqQuoteDto } from "~/types/api";
import { SparklesIcon } from "@heroicons/vue/24/outline";
import type { ExcelColumn } from "~/composables/useExcelImport";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const accessState = ref<"forbidden" | "notFound" | null>(null);
const toast = useToast();
const { t } = useI18n();

const process = ref<RfqProcessDetailDto | null>(null);
const matrix = ref<RfqComparisonRowDto[]>([]);
const quotes = ref<RfqQuoteDto[]>([]);
const aiRecommendation = ref<RfqAiRecommendationDto | null>(null);
const aiLoading = ref(false);
// Tedarikçi seçimi "sarı alan": tüm cariler içinde aranır (100 kayıtlık açılır liste yerine).
const inviteCompanyId = ref<string | null>(null);
const inviteCompanyName = ref<string | null>(null);
const { loadSupplierRows } = useSupplierLookup();
const newLine = reactive({ description: "", quantity: 1, unitOfMeasure: "Piece" });
const loading = ref(true);

async function runAction(action: () => Promise<void>) {
  try {
    await action();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.detail.actionError")));
  }
}

const unitOfMeasures = ["Piece", "Kilogram", "Gram", "Liter", "Meter", "SquareMeter", "CubicMeter", "Box", "Pallet", "Hour", "Service"];
const unitLabel = (u: string) => t(`enums.unitOfMeasure.${u}`);

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
    process.value = await api.get<RfqProcessDetailDto>(`/api/rfq/processes/${processId}`);
  } catch (error: any) {
    // Yetki yoksa / kayıt yoksa boş sayfa ve kaybolan bildirim yerine açıklayıcı durum gösterilir.
    const code = error?.response?.status ?? error?.statusCode;
    if (code === 403 || code === 404) accessState.value = code === 403 ? "forbidden" : "notFound";
    else toast.error(apiErrorMessage(error, t("rfq.detail.loadError")));
  } finally {
    loading.value = false;
  }
}

async function loadMatrix() {
  try {
    matrix.value = await api.get<RfqComparisonRowDto[]>(`/api/rfq/processes/${processId}/comparison-matrix`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.detail.matrixLoadError")));
  }
}

async function loadQuotes() {
  try {
    quotes.value = await api.get<RfqQuoteDto[]>(`/api/rfq/processes/${processId}/quotes`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.detail.quotesLoadError")));
  }
}

async function award(quoteId: string) {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/award`, { winningQuoteId: quoteId, notes: null });
    toast.success(t("rfq.detail.winnerSelectedToast"));
    aiRecommendation.value = null;
    await load();
  });
}

async function getAiRecommendation() {
  aiLoading.value = true;
  try {
    aiRecommendation.value = await api.get<RfqAiRecommendationDto>(`/api/rfq/processes/${processId}/ai-recommendation`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.detail.aiRecommendation.error")));
  } finally {
    aiLoading.value = false;
  }
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
    toast.success(t("rfq.detail.itemAddedToast"));
    await load();
  });
}

const importLineColumns = computed<ExcelColumn[]>(() => [
  { key: "description", label: t("auctions.detail.form.description"), required: true },
  { key: "quantity", label: t("auctions.detail.form.quantity"), required: true, type: "number" },
  { key: "unitOfMeasure", label: t("products.import.unit"), hint: unitOfMeasures.join("/") },
]);

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
    inviteCompanyId.value = null;
    inviteCompanyName.value = null;
    toast.success(t("rfq.detail.invitedToast"));
    await load();
  });
}

async function publish() {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/publish`);
    toast.success(t("rfq.detail.publishedToast"));
    await load();
  });
}

async function close() {
  await runAction(async () => {
    await api.post(`/api/rfq/processes/${processId}/close`);
    toast.success(t("rfq.detail.closedToast"));
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
  <AccessState v-if="accessState" :kind="accessState" back-to="/admin/rfq" :back-label="$t('nav.rfq')" />
  <LoadingSpinner v-else-if="loading" />

  <div v-else-if="process" class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
        <p class="text-sm text-slate-500">
          {{ $t("rfq.detail.statusLabel") }}: {{ $t(`enums.processStatus.${process.status}`) }} · {{ $t("rfq.detail.currencyLabel") }}: {{ process.currencyCode }}
        </p>
        <ProcessWorkspace
          kind="rfq"
          :process-id="process.id"
          :owner-company-id="process.ownerCompanyId"
          :workspace-id="process.workspaceId"
          :workspace-name="process.workspaceName"
          :status="process.status"
          @changed="load"
        />
      </div>
      <div class="flex gap-2">
        <AppButton v-if="process.status === 'Sent'" @click="publish">
          {{ $t("rfq.detail.publishStep4") }}
        </AppButton>
        <AppButton v-if="!['Closed', 'Awarded'].includes(process.status)" variant="secondary" @click="close">
          {{ $t("rfq.detail.closeProcess") }}
        </AppButton>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <AppCard>
        <div class="flex items-center justify-between">
          <StepBadge :step="2" :total="4" :label="$t('rfq.detail.stepItems')" />
          <ExcelImportButton
            v-if="process.status === 'Draft'"
            :columns="importLineColumns"
            template-filename="rfq-kalemler-sablon.xlsx"
            :import-row="importLineRow"
            @imported="load"
          />
        </div>
        <ul v-if="process.lineItems.length > 0" class="mt-3 space-y-1 text-sm text-slate-600">
          <li v-for="li in process.lineItems" :key="li.id">{{ li.description }} — {{ li.quantity }} {{ unitLabel(li.unitOfMeasure) }}</li>
        </ul>
        <EmptyState v-else class="mt-3">{{ $t("rfq.detail.emptyItems") }}</EmptyState>

        <form v-if="process.status === 'Draft'" class="mt-3 flex flex-wrap gap-2" @submit.prevent="addLine">
          <input v-model="newLine.description" :placeholder="$t('auctions.detail.form.description')" required class="min-w-[160px] flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model.number="newLine.quantity" type="number" min="0.01" step="0.01" class="w-24 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <select v-model="newLine.unitOfMeasure" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ unitLabel(u) }}</option>
          </select>
          <AppButton type="submit" variant="secondary">{{ $t("rfq.detail.add") }}</AppButton>
        </form>
        <p v-else class="mt-2 text-xs text-slate-400">{{ $t("rfq.detail.itemsDraftOnly") }}</p>
      </AppCard>

      <AppCard>
        <StepBadge :step="3" :total="4" :label="$t('rfq.detail.stepInvitees')" />
        <ul v-if="process.invitations.length > 0" class="mt-3 space-y-2 text-sm">
          <li v-for="inv in process.invitations" :key="inv.id" class="flex items-center justify-between">
            <span>{{ inv.companyName }}</span>
            <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ $t(`enums.invitationStatus.${inv.status}`) }}</span>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">{{ $t("rfq.detail.emptyInvites") }}</EmptyState>
        <div v-if="['Draft', 'Sent'].includes(process.status)" class="mt-3 flex gap-2">
          <LookupField
            v-model="inviteCompanyId"
            class="min-w-0 flex-1"
            :load="loadSupplierRows"
            :title="$t('rfq.detail.selectCompany')"
            :display="inviteCompanyName"
            :placeholder="$t('rfq.detail.selectCompany')"
            data-testid="invite-company"
            @select="(r) => (inviteCompanyName = r ? `${r.code} · ${r.name}` : null)"
          />
          <AppButton :disabled="!inviteCompanyId" @click="invite">{{ $t("rfq.detail.invite") }}</AppButton>
        </div>
      </AppCard>
    </div>

    <AppCard>
      <h3 class="text-sm font-semibold text-slate-700">
        {{ $t("rfq.detail.comparisonMatrix") }} <span class="text-xs font-normal text-slate-400">{{ $t("rfq.detail.comparisonMatrixHint") }}</span>
      </h3>
      <div v-for="(rows, description) in groupedMatrix" :key="description" class="mt-4 overflow-x-auto">
        <div class="text-xs font-semibold uppercase text-slate-500">{{ description }}</div>
        <table class="mt-1 w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-1">{{ $t("rfq.detail.matrixTable.company") }}</th>
              <th class="pb-1">{{ $t("rfq.detail.matrixTable.unitPrice") }}</th>
              <th class="pb-1">{{ $t("rfq.detail.matrixTable.normalized") }}</th>
              <th class="pb-1">{{ $t("rfq.detail.matrixTable.leadTime") }}</th>
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
      <EmptyState v-if="matrix.length === 0">{{ $t("rfq.detail.emptyMatrix") }}</EmptyState>
    </AppCard>

    <!-- AI Recommendation panel — shown when Closed and there are submitted quotes -->
    <AppCard v-if="process.status === 'Closed' && quotes.filter((q) => q.status === 'Submitted').length > 0">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <SparklesIcon class="h-5 w-5 text-indigo-500" />
          <h3 class="text-sm font-semibold text-slate-700">{{ $t("rfq.detail.aiRecommendation.title") }}</h3>
        </div>
        <button
          :disabled="aiLoading"
          class="flex items-center gap-1.5 rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
          @click="getAiRecommendation"
        >
          <SparklesIcon class="h-3.5 w-3.5" />
          {{ aiLoading ? $t("rfq.detail.aiRecommendation.analyzing") : $t("rfq.detail.aiRecommendation.analyze") }}
        </button>
      </div>

      <div v-if="aiRecommendation" class="mt-4 space-y-4">
        <!-- Winner summary -->
        <div class="rounded-lg border border-indigo-200 bg-indigo-50 p-3">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-xs text-indigo-500 font-medium uppercase tracking-wide">{{ $t("rfq.detail.aiRecommendation.recommended") }}</p>
              <p class="mt-0.5 text-base font-bold text-indigo-800">{{ aiRecommendation.recommendedCompanyName }}</p>
            </div>
            <div class="text-right">
              <p class="text-xs text-indigo-500 font-medium uppercase tracking-wide">{{ $t("rfq.detail.aiRecommendation.confidence") }}</p>
              <p class="mt-0.5 text-base font-bold text-indigo-800">{{ aiRecommendation.confidenceScore.toFixed(0) }}%</p>
            </div>
          </div>
          <!-- Confidence bar -->
          <div class="mt-2 h-1.5 rounded-full bg-indigo-200">
            <div
              class="h-1.5 rounded-full bg-indigo-500 transition-all"
              :style="{ width: `${aiRecommendation.confidenceScore}%` }"
            />
          </div>
        </div>

        <!-- Rationale -->
        <p class="text-sm text-slate-600 leading-relaxed">{{ aiRecommendation.rationale }}</p>

        <!-- Score table -->
        <div class="overflow-x-auto">
          <table class="w-full text-xs">
            <thead>
              <tr class="border-b border-slate-100 text-left text-slate-400 uppercase tracking-wide">
                <th class="pb-1 pr-3">{{ $t("rfq.detail.aiRecommendation.company") }}</th>
                <th class="pb-1 pr-3 text-right">{{ $t("rfq.detail.aiRecommendation.totalScore") }}</th>
                <th class="pb-1 pr-3 text-right">{{ $t("rfq.detail.aiRecommendation.priceScore") }}</th>
                <th class="pb-1 pr-3 text-right">{{ $t("rfq.detail.aiRecommendation.leadScore") }}</th>
                <th class="pb-1 text-right">{{ $t("rfq.detail.aiRecommendation.totalCost") }}</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="s in aiRecommendation.scores"
                :key="s.quoteId"
                class="border-b border-slate-50"
                :class="{ 'bg-indigo-50/60 font-semibold': s.quoteId === aiRecommendation!.recommendedQuoteId }"
              >
                <td class="py-1.5 pr-3">
                  <span v-if="s.quoteId === aiRecommendation!.recommendedQuoteId" class="mr-1 text-indigo-500">★</span>
                  {{ s.companyName }}
                </td>
                <td class="py-1.5 pr-3 text-right font-mono text-indigo-700">{{ s.totalScore.toFixed(1) }}</td>
                <td class="py-1.5 pr-3 text-right font-mono text-slate-600">{{ s.priceScore.toFixed(1) }}</td>
                <td class="py-1.5 pr-3 text-right font-mono text-slate-600">{{ s.leadTimeScore.toFixed(1) }}</td>
                <td class="py-1.5 text-right font-mono text-slate-500">{{ s.totalNormalizedCost.toFixed(2) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p class="text-[11px] text-slate-400">{{ $t("rfq.detail.aiRecommendation.weightNote") }}</p>
      </div>

      <p v-else class="mt-2 text-xs text-slate-400">{{ $t("rfq.detail.aiRecommendation.hint") }}</p>
    </AppCard>

    <AppCard>
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-semibold text-slate-700">{{ $t("rfq.detail.quotes") }}</h3>
        <span v-if="process.status === 'Closed'" class="text-[11px] text-slate-400">
          {{ $t("rfq.detail.thankYouEmailNote") }}
        </span>
      </div>
      <p v-if="process.status === 'Awarded'" class="mt-1 text-xs text-emerald-700">
        {{ $t("rfq.detail.winner") }}: {{ quotes.find((q) => q.id === process!.winningQuoteId)?.companyName ?? "-" }}
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
            {{ $t("rfq.detail.selectWinner") }}
          </button>
          <AppBadge v-else-if="q.id === process.winningQuoteId" tone="emerald">{{ $t("rfq.detail.won") }}</AppBadge>
        </li>
      </ul>
      <EmptyState v-else class="mt-3">{{ $t("rfq.detail.emptyQuotes") }}</EmptyState>
      <p v-if="process.status !== 'Closed' && process.status !== 'Awarded'" class="mt-2 text-xs text-slate-400">
        {{ $t("rfq.detail.closeBeforeAward") }}
      </p>
    </AppCard>
  </div>
</template>
