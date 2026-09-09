<script setup lang="ts">
import type { CompanyDto, PagedResult, RfiProcessDto, RfiTemplateDto } from "~/types/api";

const api = useApi();
const toast = useToast();
const processes = ref<RfiProcessDto[]>([]);
const templates = ref<RfiTemplateDto[]>([]);
const companies = ref<CompanyDto[]>([]);
const loading = ref(true);

const showTemplateForm = ref(false);
const showProcessForm = ref(false);
const templateForm = reactive({ name: "", description: "" });
const processForm = reactive({ rfiTemplateId: "", title: "", ownerCompanyId: "", dueDate: "" });
const expandedTemplateId = ref<string | null>(null);
const questionTypes = ["Text", "Number", "Boolean", "MultipleChoice", "FileUpload"];
const questionForm = reactive({ questionText: "", questionType: "Text", isRequired: true, weight: 1, optionsText: "" });

async function runAction(action: () => Promise<void>) {
  try {
    await action();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "İşlem gerçekleştirilemedi."));
  }
}

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Sent: "amber",
  InProgress: "amber",
  Closed: "slate",
  Evaluated: "indigo",
};

async function load() {
  loading.value = true;
  try {
    const [processPage, templateList, companyPage] = await Promise.all([
      api.get<PagedResult<RfiProcessDto>>("/api/rfi/processes", { page: 1, pageSize: 50 }),
      api.get<RfiTemplateDto[]>("/api/rfi/templates"),
      api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100 }),
    ]);
    processes.value = processPage.items;
    templates.value = templateList;
    companies.value = companyPage.items;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Kayıtlar yüklenemedi."));
    return;
  } finally {
    loading.value = false;
  }

  if (!processForm.rfiTemplateId && templates.value.length > 0) processForm.rfiTemplateId = templates.value[0].id;
  if (!processForm.ownerCompanyId && companies.value.length > 0) {
    processForm.ownerCompanyId = companies.value.find((c) => c.companyType === "Internal")?.id ?? companies.value[0].id;
  }
}

async function createTemplate() {
  await runAction(async () => {
    await api.post("/api/rfi/templates", { name: templateForm.name, description: templateForm.description || null, categoryId: null });
    Object.assign(templateForm, { name: "", description: "" });
    showTemplateForm.value = false;
    toast.success("Şablon oluşturuldu.");
    await load();
  });
}

function toggleTemplate(template: RfiTemplateDto) {
  expandedTemplateId.value = expandedTemplateId.value === template.id ? null : template.id;
  Object.assign(questionForm, { questionText: "", questionType: "Text", isRequired: true, weight: 1, optionsText: "" });
}

async function addQuestion(template: RfiTemplateDto) {
  await runAction(async () => {
    const options = questionForm.questionType === "MultipleChoice"
      ? questionForm.optionsText.split("\n").map((o) => o.trim()).filter(Boolean)
      : null;

    await api.post(`/api/rfi/templates/${template.id}/questions`, {
      questionText: questionForm.questionText,
      questionType: questionForm.questionType,
      isRequired: questionForm.isRequired,
      sortOrder: template.questions.length + 1,
      weight: questionForm.weight,
      options,
    });
    Object.assign(questionForm, { questionText: "", questionType: "Text", isRequired: true, weight: 1, optionsText: "" });
    toast.success("Soru eklendi.");
    await load();
  });
}

async function createProcess() {
  await runAction(async () => {
    const id = await api.post<string>("/api/rfi/processes", {
      rfiTemplateId: processForm.rfiTemplateId,
      title: processForm.title,
      ownerCompanyId: processForm.ownerCompanyId,
      dueDate: new Date(processForm.dueDate).toISOString(),
    });
    showProcessForm.value = false;
    toast.success("RFI süreci oluşturuldu.");
    await navigateTo(`/admin/rfi/${id}`);
  });
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">RFI (Bilgi Talebi)</h1>
      <div class="flex gap-2">
        <AppButton variant="secondary" @click="showTemplateForm = !showTemplateForm">
          + Şablon
        </AppButton>
        <AppButton @click="showProcessForm = !showProcessForm">
          + Yeni RFI Süreci
        </AppButton>
      </div>
    </div>

    <form v-if="showTemplateForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="createTemplate">
      <input v-model="templateForm.name" placeholder="Şablon Adı" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="templateForm.description" placeholder="Açıklama" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <AppButton type="submit" variant="secondary" class="sm:col-span-2">Şablonu Kaydet</AppButton>
      <p class="text-xs text-slate-400 sm:col-span-2">Şablonu kaydettikten sonra aşağıdaki listeden soru ekleyebilirsiniz.</p>
    </form>

    <AppCard>
      <h2 class="text-sm font-semibold text-slate-700">Şablonlar</h2>
      <LoadingSpinner v-if="loading" class="mt-3" />
      <template v-else>
        <ul v-if="templates.length > 0" class="mt-3 space-y-2">
          <li v-for="t in templates" :key="t.id" class="rounded-lg border border-slate-100 p-3">
            <button class="flex w-full items-center justify-between text-left" @click="toggleTemplate(t)">
              <span class="text-sm font-medium text-slate-700">{{ t.name }}</span>
              <span class="text-xs text-slate-400">{{ t.questions.length }} soru · {{ expandedTemplateId === t.id ? "Gizle" : "Detay" }}</span>
            </button>

            <div v-if="expandedTemplateId === t.id" class="mt-3 space-y-3 border-t border-slate-100 pt-3">
              <ol class="list-decimal space-y-1 pl-4 text-sm text-slate-600">
                <li v-for="q in t.questions" :key="q.id">
                  {{ q.questionText }}
                  <span class="text-xs text-slate-400">({{ q.questionType }}, ağırlık {{ q.weight }}{{ q.isRequired ? ", zorunlu" : "" }})</span>
                </li>
                <li v-if="t.questions.length === 0" class="text-slate-400">Henüz soru yok</li>
              </ol>

              <form class="grid grid-cols-1 gap-2 sm:grid-cols-2" @submit.prevent="addQuestion(t)">
                <input v-model="questionForm.questionText" placeholder="Soru metni" required class="sm:col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
                <select v-model="questionForm.questionType" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
                  <option v-for="qt in questionTypes" :key="qt" :value="qt">{{ qt }}</option>
                </select>
                <input v-model.number="questionForm.weight" type="number" min="0" step="0.5" placeholder="Ağırlık" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
                <textarea
                  v-if="questionForm.questionType === 'MultipleChoice'"
                  v-model="questionForm.optionsText"
                  placeholder="Her satıra bir seçenek"
                  rows="3"
                  class="sm:col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm"
                />
                <label class="flex items-center gap-2 text-xs text-slate-600">
                  <input v-model="questionForm.isRequired" type="checkbox" />
                  Zorunlu soru
                </label>
                <AppButton type="submit">Soru Ekle</AppButton>
              </form>
            </div>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">Henüz şablon yok</EmptyState>
      </template>
    </AppCard>

    <form v-if="showProcessForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="createProcess">
      <select v-model="processForm.rfiTemplateId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="t in templates" :key="t.id" :value="t.id">{{ t.name }} ({{ t.questions.length }} soru)</option>
      </select>
      <select v-model="processForm.ownerCompanyId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <input v-model="processForm.title" placeholder="Süreç Başlığı" required class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
      <input v-model="processForm.dueDate" type="datetime-local" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <AppButton type="submit">Oluştur</AppButton>
    </form>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
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
              <td class="py-2"><AppBadge :tone="statusTone[p.status]">{{ p.status }}</AppBadge></td>
              <td class="py-2 text-slate-500">{{ p.invitationCount }}</td>
              <td class="py-2 text-slate-500">{{ p.responseCount }}</td>
              <td class="py-2 text-right"><NuxtLink :to="`/admin/rfi/${p.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="processes.length === 0">Henüz RFI süreci yok</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
