<script setup lang="ts">
import type { CompanyDto, PagedResult, RfiProcessDto, RfiTemplateDto } from "~/types/api";

const api = useApi();
const processes = ref<RfiProcessDto[]>([]);
const templates = ref<RfiTemplateDto[]>([]);
const companies = ref<CompanyDto[]>([]);

const showTemplateForm = ref(false);
const showProcessForm = ref(false);
const templateForm = reactive({ name: "", description: "" });
const processForm = reactive({ rfiTemplateId: "", title: "", ownerCompanyId: "", dueDate: "" });

const statusBadge: Record<string, string> = {
  Draft: "bg-slate-100 text-slate-600",
  Sent: "bg-amber-100 text-amber-700",
  InProgress: "bg-amber-100 text-amber-700",
  Closed: "bg-slate-100 text-slate-600",
  Evaluated: "bg-indigo-100 text-indigo-700",
};

async function load() {
  const [processPage, templateList, companyPage] = await Promise.all([
    api.get<PagedResult<RfiProcessDto>>("/api/rfi/processes", { page: 1, pageSize: 50 }),
    api.get<RfiTemplateDto[]>("/api/rfi/templates"),
    api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100 }),
  ]);
  processes.value = processPage.items;
  templates.value = templateList;
  companies.value = companyPage.items;

  if (!processForm.rfiTemplateId && templates.value.length > 0) processForm.rfiTemplateId = templates.value[0].id;
  if (!processForm.ownerCompanyId && companies.value.length > 0) {
    processForm.ownerCompanyId = companies.value.find((c) => c.companyType === "Internal")?.id ?? companies.value[0].id;
  }
}

async function createTemplate() {
  await api.post("/api/rfi/templates", { name: templateForm.name, description: templateForm.description || null, categoryId: null });
  Object.assign(templateForm, { name: "", description: "" });
  showTemplateForm.value = false;
  await load();
}

async function createProcess() {
  const id = await api.post<string>("/api/rfi/processes", {
    rfiTemplateId: processForm.rfiTemplateId,
    title: processForm.title,
    ownerCompanyId: processForm.ownerCompanyId,
    dueDate: new Date(processForm.dueDate).toISOString(),
  });
  showProcessForm.value = false;
  await navigateTo(`/admin/rfi/${id}`);
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">RFI (Bilgi Talebi)</h1>
      <div class="flex gap-2">
        <button class="rounded-md bg-slate-700 px-4 py-2 text-sm font-semibold text-white" @click="showTemplateForm = !showTemplateForm">
          + Şablon
        </button>
        <button class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white" @click="showProcessForm = !showProcessForm">
          + Yeni RFI Süreci
        </button>
      </div>
    </div>

    <form v-if="showTemplateForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="createTemplate">
      <input v-model="templateForm.name" placeholder="Şablon Adı" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="templateForm.description" placeholder="Açıklama" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <button type="submit" class="rounded-md bg-slate-800 px-4 py-2 text-sm font-semibold text-white sm:col-span-2">Şablonu Kaydet</button>
      <p class="text-xs text-slate-400 sm:col-span-2">Soruları şablon oluşturulduktan sonra ekleyebilirsiniz (API: POST /api/rfi/templates/{`{id}`}/questions).</p>
    </form>

    <form v-if="showProcessForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="createProcess">
      <select v-model="processForm.rfiTemplateId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="t in templates" :key="t.id" :value="t.id">{{ t.name }} ({{ t.questions.length }} soru)</option>
      </select>
      <select v-model="processForm.ownerCompanyId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <input v-model="processForm.title" placeholder="Süreç Başlığı" required class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
      <input v-model="processForm.dueDate" type="datetime-local" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <button type="submit" class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white">Oluştur</button>
    </form>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Başlık</th>
            <th class="pb-2">Şablon</th>
            <th class="pb-2">Durum</th>
            <th class="pb-2">Davet</th>
            <th class="pb-2">Yanıt</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in processes" :key="p.id" class="border-b border-slate-50">
            <td class="py-2 font-medium text-slate-700">{{ p.title }}</td>
            <td class="py-2 text-slate-500">{{ p.templateName }}</td>
            <td class="py-2"><span class="rounded px-2 py-0.5 text-xs font-medium" :class="statusBadge[p.status]">{{ p.status }}</span></td>
            <td class="py-2 text-slate-500">{{ p.invitationCount }}</td>
            <td class="py-2 text-slate-500">{{ p.responseCount }}</td>
            <td class="py-2 text-right"><NuxtLink :to="`/admin/rfi/${p.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
