<script setup lang="ts">
import type { CurrencyDto, ExchangeRateDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const api = useApi();
const currencies = ref<CurrencyDto[]>([]);
const rates = ref<ExchangeRateDto[]>([]);
const showCurrencyForm = ref(false);
const showRateForm = ref(false);
const submitting = ref(false);
const errorMessage = ref("");

const currencyForm = reactive({ code: "", name: "", symbol: "", isBaseCurrency: false });
const rateForm = reactive({ currencyId: "", rateDate: new Date().toISOString().slice(0, 10), rateToBase: null as number | null, source: "" });

const nonBaseCurrencies = computed(() => currencies.value.filter((c) => !c.isBaseCurrency));

const importColumns: ExcelColumn[] = [
  { key: "code", label: "Kod", required: true, hint: "3 harfli ISO kodu, örn. CHF" },
  { key: "name", label: "Ad", required: true },
  { key: "symbol", label: "Sembol", required: true },
];

async function importCurrencyRow(row: Record<string, string | number | boolean | null>) {
  await api.post("/api/currencies", {
    code: String(row.code).toUpperCase(),
    name: row.name,
    symbol: row.symbol,
    isBaseCurrency: false,
  });
}

async function load() {
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
    errorMessage.value = apiErrorMessage(error, "Kayıtlar yüklenemedi.");
  }
}

async function createCurrency() {
  errorMessage.value = "";
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
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Para birimi oluşturulamadı.");
  } finally {
    submitting.value = false;
  }
}

async function createRate() {
  if (!rateForm.currencyId || rateForm.rateToBase === null) return;
  errorMessage.value = "";
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
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Kur oluşturulamadı.");
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">Para Birimleri &amp; Kurlar</h1>
    </div>

    <div v-if="errorMessage" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ errorMessage }}</span>
      <button class="font-semibold" @click="errorMessage = ''">Kapat</button>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-slate-700">Para Birimleri</h2>
          <div class="flex items-center gap-2">
            <ExcelImportButton :columns="importColumns" template-filename="para-birimleri-sablon.xlsx" :import-row="importCurrencyRow" @imported="load" />
            <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showCurrencyForm = !showCurrencyForm">
              {{ showCurrencyForm ? "Vazgeç" : "+ Yeni" }}
            </button>
          </div>
        </div>

        <form v-if="showCurrencyForm" class="mt-3 grid grid-cols-2 gap-2" @submit.prevent="createCurrency">
          <input v-model="currencyForm.code" placeholder="Kod (örn. GBP)" maxlength="3" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="currencyForm.symbol" placeholder="Sembol (£)" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="currencyForm.name" placeholder="Ad (İngiliz Sterlini)" required class="col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <label class="col-span-2 flex items-center gap-2 text-xs text-slate-600">
            <input v-model="currencyForm.isBaseCurrency" type="checkbox" />
            Sistem baz para birimi yap
          </label>
          <button type="submit" :disabled="submitting" class="col-span-2 rounded-md bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50">
            {{ submitting ? "Kaydediliyor..." : "Kaydet" }}
          </button>
        </form>

        <table class="mt-3 w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-1">Kod</th>
              <th class="pb-1">Ad</th>
              <th class="pb-1">Sembol</th>
              <th class="pb-1"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in currencies" :key="c.id" class="border-b border-slate-50">
              <td class="py-1 font-mono font-semibold text-slate-700">{{ c.code }}</td>
              <td class="py-1 text-slate-600">{{ c.name }}</td>
              <td class="py-1 text-slate-500">{{ c.symbol }}</td>
              <td class="py-1 text-right">
                <span v-if="c.isBaseCurrency" class="rounded bg-indigo-100 px-2 py-0.5 text-xs font-medium text-indigo-700">Baz</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-slate-700">Döviz Kurları (baz para birimine göre)</h2>
          <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showRateForm = !showRateForm">
            {{ showRateForm ? "Vazgeç" : "+ Yeni" }}
          </button>
        </div>

        <form v-if="showRateForm" class="mt-3 grid grid-cols-2 gap-2" @submit.prevent="createRate">
          <select v-model="rateForm.currencyId" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="c in nonBaseCurrencies" :key="c.id" :value="c.id">{{ c.code }}</option>
          </select>
          <input v-model="rateForm.rateDate" type="date" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model.number="rateForm.rateToBase" type="number" step="0.000001" placeholder="Kur (örn. 34.50)" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="rateForm.source" placeholder="Kaynak (opsiyonel)" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <button type="submit" :disabled="submitting" class="col-span-2 rounded-md bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50">
            {{ submitting ? "Kaydediliyor..." : "Kaydet" }}
          </button>
        </form>
        <p v-if="nonBaseCurrencies.length === 0" class="mt-2 text-xs text-slate-400">Kur tanımlamak için önce baz olmayan bir para birimi ekleyin.</p>

        <table class="mt-3 w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-1">Para Birimi</th>
              <th class="pb-1">Tarih</th>
              <th class="pb-1">Kur</th>
              <th class="pb-1">Kaynak</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in rates" :key="r.id" class="border-b border-slate-50">
              <td class="py-1 font-mono font-semibold text-slate-700">{{ r.currencyCode }}</td>
              <td class="py-1 text-slate-500">{{ r.rateDate }}</td>
              <td class="py-1 font-mono text-slate-700">{{ r.rateToBase }}</td>
              <td class="py-1 text-slate-400">{{ r.source ?? "-" }}</td>
            </tr>
            <tr v-if="rates.length === 0"><td colspan="4" class="py-4 text-center text-slate-400">Henüz kur tanımlı değil</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
