<script setup lang="ts">
import type { CompanyDto, PagedResult } from "~/types/api";

const api = useApi();
const companies = ref<CompanyDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const errorMessage = ref("");

const form = reactive({ name: "", taxNumber: "", taxOffice: "", companyType: "Supplier", website: "" });

async function load() {
  const page = await api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100 });
  companies.value = page.items;
}

async function create() {
  errorMessage.value = "";
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
    await load();
  } catch (error: any) {
    errorMessage.value = error?.data?.title ?? "Cari oluşturulamadı.";
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
      <button class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white" @click="showForm = !showForm">
        {{ showForm ? "Vazgeç" : "+ Yeni Cari" }}
      </button>
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
      <p v-if="errorMessage" class="text-sm text-rose-600 sm:col-span-2">{{ errorMessage }}</p>
      <button type="submit" :disabled="submitting" class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white sm:col-span-2 disabled:opacity-50">
        {{ submitting ? "Kaydediliyor..." : "Kaydet" }}
      </button>
    </form>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Ad</th>
            <th class="pb-2">Vergi No</th>
            <th class="pb-2">Tür</th>
            <th class="pb-2">Durum</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in companies" :key="c.id" class="border-b border-slate-50">
            <td class="py-2 font-medium text-slate-700">{{ c.name }}</td>
            <td class="py-2 text-slate-500">{{ c.taxNumber }}</td>
            <td class="py-2 text-slate-500">{{ c.companyType }}</td>
            <td class="py-2 text-slate-500">{{ c.status }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
