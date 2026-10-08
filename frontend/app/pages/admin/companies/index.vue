<script setup lang="ts">
import type { CompanyDto, CompanyDetailDto, CompanyLookupResultDto, PagedResult } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";
import { MagnifyingGlassIcon, StarIcon } from "@heroicons/vue/24/outline";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { selectedWorkspaceId } = useWorkspace();
const companies = ref<CompanyDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const form = reactive({
  name: "", taxNumber: "", taxOffice: "", companyType: "Supplier", website: "",
  tradeName: "", mersisNo: "", phone: "", email: "", foundationYear: "", employeeCount: "", description: "",
});
const statuses = ["PendingApproval", "Active", "Passive"];
const statusDrafts = reactive<Record<string, string>>({});

// TCKN/VKN lookup (simulated auto-fill)
const lookupValue = ref("");
const lookingUp = ref(false);

async function runLookup() {
  if (!lookupValue.value.trim()) return;
  lookingUp.value = true;
  try {
    const r = await api.get<CompanyLookupResultDto>("/api/companies/lookup", { identifier: lookupValue.value.trim() });
    if (!r.isValid) {
      toast.error(r.message || t("companies.lookup.invalid"));
      return;
    }
    form.name = r.name ?? form.name;
    form.taxNumber = r.identifierType === "VKN" ? r.identifier : form.taxNumber;
    form.taxOffice = r.taxOffice ?? form.taxOffice;
    form.tradeName = r.tradeName ?? form.tradeName;
    form.mersisNo = r.mersisNo ?? form.mersisNo;
    form.phone = r.phone ?? form.phone;
    form.email = r.email ?? form.email;
    form.foundationYear = r.foundationYear ? String(r.foundationYear) : form.foundationYear;
    toast.success(t("companies.lookup.filled"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.lookup.error")));
  } finally {
    lookingUp.value = false;
  }
}

const importColumns = computed<ExcelColumn[]>(() => [
  { key: "name", label: t("companies.import.name"), required: true },
  { key: "taxNumber", label: t("companies.import.taxNumber"), required: true },
  { key: "taxOffice", label: t("companies.import.taxOffice"), required: true },
  { key: "companyType", label: t("companies.import.type"), required: true, hint: t("companies.import.typeHint") },
  { key: "website", label: t("companies.import.website") },
]);

async function importCompanyRow(row: Record<string, string | number | boolean | null>) {
  await api.post("/api/companies", {
    name: row.name,
    taxNumber: row.taxNumber,
    taxOffice: row.taxOffice,
    companyType: row.companyType || "Supplier",
    website: row.website || null,
  });
}

async function load() {
  loading.value = true;
  try {
    const params: Record<string, unknown> = { page: 1, pageSize: 100 };
    if (selectedWorkspaceId.value) params.workspaceId = selectedWorkspaceId.value;
    const page = await api.get<PagedResult<CompanyDto>>("/api/companies", params);
    companies.value = page.items;
    for (const c of companies.value) {
      statusDrafts[c.id] = c.status;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.loadError")));
  } finally {
    loading.value = false;
  }
}

async function saveStatus(company: CompanyDto) {
  try {
    // Update overwrites all fields, so load the full detail and preserve it while changing status.
    const d = await api.get<CompanyDetailDto>(`/api/companies/${company.id}`);
    await api.put(`/api/companies/${company.id}`, {
      name: d.name,
      taxOffice: d.taxOffice,
      status: statusDrafts[company.id],
      website: d.website,
      tradeName: d.tradeName,
      mersisNo: d.mersisNo,
      phone: d.phone,
      email: d.email,
      foundationYear: d.foundationYear,
      employeeCount: d.employeeCount,
      description: d.description,
    });
    toast.success(t("toast.companyStatusUpdated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.statusUpdateError")));
  }
}

async function create() {
  submitting.value = true;
  try {
    await api.post("/api/companies", {
      name: form.name,
      taxNumber: form.taxNumber,
      taxOffice: form.taxOffice,
      companyType: form.companyType,
      website: form.website || null,
      workspaceId: selectedWorkspaceId.value || null,
      tradeName: form.tradeName || null,
      mersisNo: form.mersisNo || null,
      phone: form.phone || null,
      email: form.email || null,
      foundationYear: form.foundationYear ? Number(form.foundationYear) : null,
      employeeCount: form.employeeCount ? Number(form.employeeCount) : null,
      description: form.description || null,
    });
    Object.assign(form, { name: "", taxNumber: "", taxOffice: "", website: "", tradeName: "", mersisNo: "", phone: "", email: "", foundationYear: "", employeeCount: "", description: "" });
    lookupValue.value = "";
    showForm.value = false;
    toast.success(t("toast.companyCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.createError")));
  } finally {
    submitting.value = false;
  }
}

watch(selectedWorkspaceId, load);
onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("companies.title") }}</h1>
      <div class="flex gap-2">
        <ExcelImportButton :columns="importColumns" template-filename="cariler-sablon.xlsx" :import-row="importCompanyRow" @imported="load" />
        <AppButton @click="showForm = !showForm">
          {{ showForm ? $t("common.cancel") : $t("companies.new") }}
        </AppButton>
      </div>
    </div>

    <form v-if="showForm" class="space-y-4 rounded-xl border border-slate-200 bg-white p-5" @submit.prevent="create">
      <!-- TCKN/VKN lookup bar -->
      <div class="rounded-lg border border-indigo-100 bg-indigo-50/40 p-3">
        <label class="mb-1 block text-xs font-medium text-indigo-800">{{ $t("companies.lookup.label") }}</label>
        <div class="flex gap-2">
          <input
            v-model="lookupValue"
            :placeholder="$t('companies.lookup.placeholder')"
            inputmode="numeric"
            class="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            @keyup.enter.prevent="runLookup"
          />
          <button type="button" :disabled="lookingUp" class="flex items-center gap-1.5 rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50" @click="runLookup">
            <MagnifyingGlassIcon class="h-4 w-4" />
            {{ lookingUp ? $t("companies.lookup.querying") : $t("companies.lookup.query") }}
          </button>
        </div>
        <p class="mt-1 text-[11px] text-indigo-500">{{ $t("companies.lookup.hint") }}</p>
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <input v-model="form.name" :placeholder="$t('companies.form.name')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-model="form.tradeName" :placeholder="$t('companies.form.tradeName')" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-model="form.taxNumber" :placeholder="$t('companies.form.taxNumber')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-model="form.taxOffice" :placeholder="$t('companies.form.taxOffice')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-model="form.mersisNo" :placeholder="$t('companies.form.mersisNo')" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <select v-model="form.companyType" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
          <option value="Supplier">{{ $t("companies.form.typeSupplier") }}</option>
          <option value="Internal">{{ $t("companies.form.typeInternal") }}</option>
        </select>
        <input v-model="form.phone" :placeholder="$t('companies.form.phone')" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-model="form.email" type="email" :placeholder="$t('companies.form.email')" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-model="form.foundationYear" type="number" :placeholder="$t('companies.form.foundationYear')" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-model="form.employeeCount" type="number" :placeholder="$t('companies.form.employeeCount')" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-model="form.website" :placeholder="$t('companies.form.website')" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <textarea v-model="form.description" rows="2" :placeholder="$t('companies.form.description')" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
      </div>
      <AppButton type="submit" :loading="submitting">
        {{ submitting ? $t("common.saving") : $t("common.save") }}
      </AppButton>
    </form>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("companies.table.name") }}</th>
              <th class="pb-2">{{ $t("companies.table.taxNumber") }}</th>
              <th class="pb-2">{{ $t("companies.table.type") }}</th>
              <th class="pb-2">{{ $t("companies.table.rating") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in companies" :key="c.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">{{ c.name }}</td>
              <td class="py-2 text-slate-500">{{ c.taxNumber }}</td>
              <td class="py-2 text-slate-500">{{ c.companyType }}</td>
              <td class="py-2">
                <span v-if="c.averageScore != null" class="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold"
                  :class="c.averageScore >= 4 ? 'bg-emerald-50 text-emerald-700' : c.averageScore >= 3 ? 'bg-amber-50 text-amber-700' : 'bg-rose-50 text-rose-700'">
                  <StarIcon class="h-3 w-3 fill-current" />{{ c.averageScore.toFixed(1) }}
                  <span class="font-normal opacity-70">({{ c.evaluationCount }})</span>
                </span>
                <span v-else class="text-xs text-slate-300">—</span>
              </td>
              <td class="py-2">
                <div class="flex items-center gap-2">
                  <select v-model="statusDrafts[c.id]" class="rounded-md border border-slate-300 px-2 py-1 text-xs">
                    <option v-for="s in statuses" :key="s" :value="s">{{ $t(`enums.companyStatus.${s}`) }}</option>
                  </select>
                  <button
                    v-if="statusDrafts[c.id] !== c.status"
                    class="rounded bg-indigo-600 px-2 py-1 text-xs font-semibold text-white"
                    @click="saveStatus(c)"
                  >
                    {{ $t("common.save") }}
                  </button>
                </div>
              </td>
              <td class="py-2 text-right"><NuxtLink :to="`/admin/companies/${c.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="companies.length === 0">{{ $t("companies.empty") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
