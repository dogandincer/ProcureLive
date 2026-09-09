<script setup lang="ts">
import type { CompanyDto, PagedResult } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const api = useApi();
const toast = useToast();
const companies = ref<CompanyDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const form = reactive({ name: "", taxNumber: "", taxOffice: "", companyType: "Supplier", website: "" });
const statuses = ["PendingApproval", "Active", "Passive"];
const statusDrafts = reactive<Record<string, string>>({});

const importColumns: ExcelColumn[] = [
  { key: "name", label: "Firma Adı", required: true },
  { key: "taxNumber", label: "Vergi No", required: true },
  { key: "taxOffice", label: "Vergi Dairesi", required: true },
  { key: "companyType", label: "Tür", required: true, hint: "Supplier veya Internal" },
  { key: "website", label: "Web Sitesi" },
];

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
    const page = await api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100 });
    companies.value = page.items;
    for (const c of companies.value) {
      statusDrafts[c.id] = c.status;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Kayıtlar yüklenemedi."));
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
    toast.success("Cari durumu güncellendi.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Durum güncellenemedi."));
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
    });
    Object.assign(form, { name: "", taxNumber: "", taxOffice: "", website: "" });
    showForm.value = false;
    toast.success("Cari oluşturuldu.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Cari oluşturulamadı."));
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">Cariler</h1>
      <div class="flex gap-2">
        <ExcelImportButton :columns="importColumns" template-filename="cariler-sablon.xlsx" :import-row="importCompanyRow" @imported="load" />
        <AppButton @click="showForm = !showForm">
          {{ showForm ? "Vazgeç" : "+ Yeni Cari" }}
        </AppButton>
      </div>
    </div>

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.name" placeholder="Firma Adı" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.taxNumber" placeholder="Vergi No" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.taxOffice" placeholder="Vergi Dairesi" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <select v-model="form.companyType" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option value="Supplier">Supplier (Tedarikçi)</option>
        <option value="Internal">Internal (İç Alım)</option>
      </select>
      <input v-model="form.website" placeholder="Web sitesi (opsiyonel)" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
      <AppButton type="submit" class="sm:col-span-2" :loading="submitting">
        {{ submitting ? "Kaydediliyor..." : "Kaydet" }}
      </AppButton>
    </form>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">Ad</th>
              <th class="pb-2">Vergi No</th>
              <th class="pb-2">Tür</th>
              <th class="pb-2">Durum</th>
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
                    <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
                  </select>
                  <button
                    v-if="statusDrafts[c.id] !== c.status"
                    class="rounded bg-indigo-600 px-2 py-1 text-xs font-semibold text-white"
                    @click="saveStatus(c)"
                  >
                    Kaydet
                  </button>
                </div>
              </td>
              <td class="py-2 text-right"><NuxtLink :to="`/admin/companies/${c.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="companies.length === 0">Henüz cari yok</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
