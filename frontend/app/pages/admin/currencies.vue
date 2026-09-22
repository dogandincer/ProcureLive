<script setup lang="ts">
import type { CurrencyDto, ExchangeRateDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const currencies = ref<CurrencyDto[]>([]);
const rates = ref<ExchangeRateDto[]>([]);
const showCurrencyForm = ref(false);
const showRateForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const currencyForm = reactive({ code: "", name: "", symbol: "", isBaseCurrency: false });
const rateForm = reactive({ currencyId: "", rateDate: new Date().toISOString().slice(0, 10), rateToBase: null as number | null, source: "" });

const nonBaseCurrencies = computed(() => currencies.value.filter((c) => !c.isBaseCurrency));

const importColumns = computed<ExcelColumn[]>(() => [
  { key: "code", label: t("currencies.import.code"), required: true, hint: t("currencies.import.codeHint") },
  { key: "name", label: t("currencies.import.name"), required: true },
  { key: "symbol", label: t("currencies.import.symbol"), required: true },
]);

async function importCurrencyRow(row: Record<string, string | number | boolean | null>) {
  await api.post("/api/currencies", {
    code: String(row.code).toUpperCase(),
    name: row.name,
    symbol: row.symbol,
    isBaseCurrency: false,
  });
}

async function load() {
  loading.value = true;
  try {
    const [currencyList, rateList] = await Promise.all([
      api.get<CurrencyDto[]>("/api/currencies"),
      api.get<ExchangeRateDto[]>("/api/currencies/exchange-rates"),
    ]);
    currencies.value = currencyList;
    rates.value = [...rateList].sort((a, b) => b.rateDate.localeCompare(a.rateDate));
    if (!rateForm.currencyId && nonBaseCurrencies.value.length > 0) {
      rateForm.currencyId = nonBaseCurrencies.value[0].id;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("currencies.loadError")));
  } finally {
    loading.value = false;
  }
}

async function createCurrency() {
  submitting.value = true;
  try {
    await api.post("/api/currencies", {
      code: currencyForm.code.toUpperCase(),
      name: currencyForm.name,
      symbol: currencyForm.symbol,
      isBaseCurrency: currencyForm.isBaseCurrency,
    });
    Object.assign(currencyForm, { code: "", name: "", symbol: "", isBaseCurrency: false });
    showCurrencyForm.value = false;
    toast.success(t("toast.currencyCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("currencies.createError")));
  } finally {
    submitting.value = false;
  }
}

async function createRate() {
  if (!rateForm.currencyId || rateForm.rateToBase === null) return;
  submitting.value = true;
  try {
    await api.post("/api/currencies/exchange-rates", {
      currencyId: rateForm.currencyId,
      rateDate: rateForm.rateDate,
      rateToBase: rateForm.rateToBase,
      source: rateForm.source || null,
    });
    rateForm.rateToBase = null;
    rateForm.source = "";
    showRateForm.value = false;
    toast.success(t("toast.rateCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("currencies.rateCreateError")));
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("currencies.title") }}</h1>
    </div>

    <LoadingSpinner v-if="loading" />
    <div v-else class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <AppCard>
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-slate-700">{{ $t("currencies.currenciesHeading") }}</h2>
          <div class="flex items-center gap-2">
            <ExcelImportButton :columns="importColumns" template-filename="para-birimleri-sablon.xlsx" :import-row="importCurrencyRow" @imported="load" />
            <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showCurrencyForm = !showCurrencyForm">
              {{ showCurrencyForm ? $t("common.cancel") : $t("currencies.newShort") }}
            </button>
          </div>
        </div>

        <form v-if="showCurrencyForm" class="mt-3 grid grid-cols-2 gap-2" @submit.prevent="createCurrency">
          <input v-model="currencyForm.code" :placeholder="$t('currencies.form.code')" maxlength="3" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="currencyForm.symbol" :placeholder="$t('currencies.form.symbol')" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="currencyForm.name" :placeholder="$t('currencies.form.name')" required class="col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <label class="col-span-2 flex items-center gap-2 text-xs text-slate-600">
            <input v-model="currencyForm.isBaseCurrency" type="checkbox" />
            {{ $t("currencies.form.makeBase") }}
          </label>
          <AppButton type="submit" variant="secondary" class="col-span-2" :loading="submitting">
            {{ submitting ? $t("common.saving") : $t("common.save") }}
          </AppButton>
        </form>

        <div class="overflow-x-auto">
          <table class="mt-3 w-full text-sm">
            <thead>
              <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
                <th class="pb-1">{{ $t("currencies.table.code") }}</th>
                <th class="pb-1">{{ $t("currencies.table.name") }}</th>
                <th class="pb-1">{{ $t("currencies.table.symbol") }}</th>
                <th class="pb-1"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in currencies" :key="c.id" class="border-b border-slate-50">
                <td class="py-1 font-mono font-semibold text-slate-700">{{ c.code }}</td>
                <td class="py-1 text-slate-600">{{ c.name }}</td>
                <td class="py-1 text-slate-500">{{ c.symbol }}</td>
                <td class="py-1 text-right">
                  <AppBadge v-if="c.isBaseCurrency" tone="indigo">{{ $t("currencies.base") }}</AppBadge>
                </td>
              </tr>
            </tbody>
          </table>
          <EmptyState v-if="currencies.length === 0">{{ $t("currencies.emptyCurrencies") }}</EmptyState>
        </div>
      </AppCard>

      <AppCard>
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-slate-700">{{ $t("currencies.ratesHeading") }}</h2>
          <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showRateForm = !showRateForm">
            {{ showRateForm ? $t("common.cancel") : $t("currencies.newShort") }}
          </button>
        </div>

        <form v-if="showRateForm" class="mt-3 grid grid-cols-2 gap-2" @submit.prevent="createRate">
          <select v-model="rateForm.currencyId" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="c in nonBaseCurrencies" :key="c.id" :value="c.id">{{ c.code }}</option>
          </select>
          <input v-model="rateForm.rateDate" type="date" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model.number="rateForm.rateToBase" type="number" step="0.000001" :placeholder="$t('currencies.form.rate')" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="rateForm.source" :placeholder="$t('currencies.form.source')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <AppButton type="submit" variant="secondary" class="col-span-2" :loading="submitting">
            {{ submitting ? $t("common.saving") : $t("common.save") }}
          </AppButton>
        </form>
        <p v-if="nonBaseCurrencies.length === 0" class="mt-2 text-xs text-slate-400">{{ $t("currencies.needNonBaseCurrency") }}</p>

        <div class="overflow-x-auto">
          <table class="mt-3 w-full text-sm">
            <thead>
              <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
                <th class="pb-1">{{ $t("currencies.table.currency") }}</th>
                <th class="pb-1">{{ $t("currencies.table.date") }}</th>
                <th class="pb-1">{{ $t("currencies.table.rate") }}</th>
                <th class="pb-1">{{ $t("currencies.table.source") }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in rates" :key="r.id" class="border-b border-slate-50">
                <td class="py-1 font-mono font-semibold text-slate-700">{{ r.currencyCode }}</td>
                <td class="py-1 text-slate-500">{{ r.rateDate }}</td>
                <td class="py-1 font-mono text-slate-700">{{ r.rateToBase }}</td>
                <td class="py-1 text-slate-400">{{ r.source ?? "-" }}</td>
              </tr>
            </tbody>
          </table>
          <EmptyState v-if="rates.length === 0">{{ $t("currencies.emptyRates") }}</EmptyState>
        </div>
      </AppCard>
    </div>
  </div>
</template>
