<script setup lang="ts">
import type { CompanyDto, CurrencyDto, PagedResult, RfqProcessDto } from "~/types/api";

const api = useApi();
const processes = ref<RfqProcessDto[]>([]);
const companies = ref<CompanyDto[]>([]);
const currencies = ref<CurrencyDto[]>([]);
const showForm = ref(false);

const form = reactive({ title: "", ownerCompanyId: "", currencyId: "", dueDate: "" });
const actionError = ref("");

const statusBadge: Record<string, string> = {
  Draft: "bg-slate-100 text-slate-600",
  Sent: "bg-amber-100 text-amber-700",
  InProgress: "bg-amber-100 text-amber-700",
  Closed: "bg-slate-100 text-slate-600",
  Awarded: "bg-indigo-100 text-indigo-700",
};

async function load() {
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
}

async function create() {
  actionError.value = "";
  try {
    const id = await api.post<string>("/api/rfq/processes", {
      title: form.title,
      ownerCompanyId: form.ownerCompanyId,
      currencyId: form.currencyId,
      dueDate: new Date(form.dueDate).toISOString(),
    });
    showForm.value = false;
    await navigateTo(`/admin/rfq/${id}`);
  } catch (error: any) {
    actionError.value = error?.data?.title ?? error?.data?.detail ?? "İşlem gerçekleştirilemedi.";
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">RFQ (Hızlı Teklif Toplama)</h1>
      <button class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white" @click="showForm = !showForm">
        {{ showForm ? "Vazgeç" : "+ Yeni RFQ (Adım 1/4)" }}
      </button>
    </div>

    <div v-if="actionError" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ actionError }}</span>
      <button class="font-semibold" @click="actionError = ''">Kapat</button>
    </div>

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.title" placeholder="Başlık" required class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
      <select v-model="form.ownerCompanyId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <select v-model="form.currencyId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
      </select>
      <input v-model="form.dueDate" type="datetime-local" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <button type="submit" class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white sm:col-span-2">
        Oluştur ve Devam Et (Kalem → Davet → Yayınla)
      </button>
    </form>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Başlık</th>
            <th class="pb-2">Alıcı</th>
            <th class="pb-2">Durum</th>
            <th class="pb-2">Kalem</th>
            <th class="pb-2">Teklif</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in processes" :key="p.id" class="border-b border-slate-50">
            <td class="py-2 font-medium text-slate-700">{{ p.title }}</td>
            <td class="py-2 text-slate-500">{{ p.ownerCompanyName }}</td>
            <td class="py-2"><span class="rounded px-2 py-0.5 text-xs font-medium" :class="statusBadge[p.status]">{{ p.status }}</span></td>
            <td class="py-2 text-slate-500">{{ p.lineItemCount }}</td>
            <td class="py-2 text-slate-500">{{ p.quoteCount }}</td>
            <td class="py-2 text-right"><NuxtLink :to="`/admin/rfq/${p.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
