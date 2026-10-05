<script setup lang="ts">
import type { CompanyDto, PagedResult } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { selectedWorkspaceId } = useWorkspace();
const companies = ref<CompanyDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const form = reactive({ name: "", taxNumber: "", taxOffice: "", companyType: "Supplier", website: "" });
const statuses = ["PendingApproval", "Active", "Passive"];
const statusDrafts = reactive<Record<string, string>>({});

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
    await api.put(`/api/companies/${company.id}`, {
      name: company.name,
      taxOffice: company.taxOffice,
      status: statusDrafts[company.id],
      website: company.website,
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
    });
    Object.assign(form, { name: "", taxNumber: "", taxOffice: "", website: "" });
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

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.name" :placeholder="$t('companies.form.name')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.taxNumber" :placeholder="$t('companies.form.taxNumber')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.taxOffice" :placeholder="$t('companies.form.taxOffice')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <select v-model="form.companyType" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option value="Supplier">{{ $t("companies.form.typeSupplier") }}</option>
        <option value="Internal">{{ $t("companies.form.typeInternal") }}</option>
      </select>
      <input v-model="form.website" :placeholder="$t('companies.form.website')" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
      <AppButton type="submit" class="sm:col-span-2" :loading="submitting">
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
