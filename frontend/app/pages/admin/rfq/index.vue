<script setup lang="ts">
import type { CompanyDto, CurrencyDto, PagedResult, RfqProcessDto } from "~/types/api";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const processes = ref<RfqProcessDto[]>([]);
const companies = ref<CompanyDto[]>([]);
const currencies = ref<CurrencyDto[]>([]);
const showForm = ref(false);
const loading = ref(true);

const form = reactive({ title: "", ownerCompanyId: "", currencyId: "", dueDate: "" });

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Sent: "amber",
  InProgress: "amber",
  Closed: "slate",
  Awarded: "indigo",
};

async function load() {
  loading.value = true;
  try {
    const [processPage, companyPage, currencyList] = await Promise.all([
      api.get<PagedResult<RfqProcessDto>>("/api/rfq/processes", { page: 1, pageSize: 50 }),
      api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100 }),
      api.get<CurrencyDto[]>("/api/currencies"),
    ]);
    processes.value = processPage.items;
    companies.value = companyPage.items;
    currencies.value = currencyList;

    if (!form.ownerCompanyId && companies.value.length > 0) {
      form.ownerCompanyId = companies.value.find((c) => c.companyType === "Internal")?.id ?? companies.value[0].id;
    }
    if (!form.currencyId && currencies.value.length > 0) {
      form.currencyId = currencies.value.find((c) => c.isBaseCurrency)?.id ?? currencies.value[0].id;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.loadError")));
  } finally {
    loading.value = false;
  }
}

async function create() {
  try {
    const id = await api.post<string>("/api/rfq/processes", {
      title: form.title,
      ownerCompanyId: form.ownerCompanyId,
      currencyId: form.currencyId,
      dueDate: new Date(form.dueDate).toISOString(),
    });
    showForm.value = false;
    toast.success(t("rfq.createdToast"));
    await navigateTo(`/admin/rfq/${id}`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfq.actionError")));
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("rfq.title") }}</h1>
      <AppButton @click="showForm = !showForm">
        {{ showForm ? $t("common.cancel") : $t("rfq.new") }}
      </AppButton>
    </div>

    <form v-if="showForm" class="space-y-4 rounded-xl border border-slate-200 bg-white p-5" @submit.prevent="create">
      <StepBadge :step="1" :total="4" :label="$t('rfq.stepInfo')" />
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <input v-model="form.title" :placeholder="$t('rfq.form.title')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
        <select v-model="form.ownerCompanyId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
          <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
        <select v-model="form.currencyId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
          <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
        </select>
        <input v-model="form.dueDate" type="datetime-local" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      </div>
      <AppButton type="submit" class="sm:col-span-2">
        {{ $t("rfq.createAndContinue") }}
      </AppButton>
    </form>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("rfq.table.title") }}</th>
              <th class="pb-2">{{ $t("rfq.table.owner") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2">{{ $t("rfq.table.items") }}</th>
              <th class="pb-2">{{ $t("rfq.table.quotes") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in processes" :key="p.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">{{ p.title }}</td>
              <td class="py-2 text-slate-500">{{ p.ownerCompanyName }}</td>
              <td class="py-2"><AppBadge :tone="statusTone[p.status]">{{ $t(`enums.processStatus.${p.status}`) }}</AppBadge></td>
              <td class="py-2 text-slate-500">{{ p.lineItemCount }}</td>
              <td class="py-2 text-slate-500">{{ p.quoteCount }}</td>
              <td class="py-2 text-right"><NuxtLink :to="`/admin/rfq/${p.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="processes.length === 0">{{ $t("rfq.empty") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
