<script setup lang="ts">
import type { CompanyDto, PagedResult, RfiProcessDetailDto, RfiResponseDto, SupplierEvaluationDto } from "~/types/api";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();

const process = ref<RfiProcessDetailDto | null>(null);
const companies = ref<CompanyDto[]>([]);
const responses = ref<RfiResponseDto[]>([]);
const evaluations = ref<SupplierEvaluationDto[]>([]);
const inviteCompanyId = ref("");
const evalForm = reactive({ companyId: "", totalScore: 0, comments: "" });
const actionError = ref("");

async function runAction(action: () => Promise<void>) {
  actionError.value = "";
  try {
    await action();
  } catch (error: any) {
    actionError.value = apiErrorMessage(error, "İşlem gerçekleştirilemedi.");
  }
}

async function load() {
  const [detail, companyPage, evalList] = await Promise.all([
    api.get<RfiProcessDetailDto>(`/api/rfi/processes/${processId}`),
    api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100, companyType: "Supplier" }),
    api.get<SupplierEvaluationDto[]>(`/api/rfi/processes/${processId}/evaluations`),
  ]);
  process.value = detail;
  companies.value = companyPage.items;
  evaluations.value = evalList;
}

async function loadResponses() {
  responses.value = await api.get<RfiResponseDto[]>(`/api/rfi/processes/${processId}/responses`);
}

async function invite() {
  if (!inviteCompanyId.value) return;
  await runAction(async () => {
    await api.post(`/api/rfi/processes/${processId}/invitations`, { companyIds: [inviteCompanyId.value] });
    inviteCompanyId.value = "";
    await load();
  });
}

async function submitEvaluation() {
  if (!evalForm.companyId) return;
  await runAction(async () => {
    await api.post(`/api/rfi/processes/${processId}/evaluations`, {
      companyId: evalForm.companyId,
      totalScore: evalForm.totalScore,
      comments: evalForm.comments || null,
    });
    await load();
  });
}

async function close() {
  await runAction(async () => {
    await api.post(`/api/rfi/processes/${processId}/close`);
    await load();
  });
}

onMounted(async () => {
  await load();
  await loadResponses();
});
</script>

<template>
  <div v-if="process" class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
        <p class="text-sm text-slate-500">Şablon: {{ process.templateName }} · Durum: {{ process.status }}</p>
      </div>
      <button v-if="!['Closed', 'Evaluated'].includes(process.status)" class="rounded-md bg-slate-700 px-4 py-2 text-sm font-semibold text-white" @click="close">
        Süreci Kapat
      </button>
    </div>

    <div v-if="actionError" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ actionError }}</span>
      <button class="font-semibold" @click="actionError = ''">Kapat</button>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <h3 class="text-sm font-semibold text-slate-700">Davetliler</h3>
        <ul class="mt-3 space-y-2 text-sm">
          <li v-for="inv in process.invitations" :key="inv.id" class="flex items-center justify-between">
            <span>{{ inv.companyName }}</span>
            <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ inv.status }}</span>
          </li>
        </ul>
        <div class="mt-3 flex gap-2">
          <select v-model="inviteCompanyId" class="flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option value="" disabled>Cari seç...</option>
            <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
          <button class="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-semibold text-white" @click="invite">Davet Et</button>
        </div>
      </div>

      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <h3 class="text-sm font-semibold text-slate-700">Sorular</h3>
        <ol class="mt-3 list-decimal space-y-1 pl-4 text-sm text-slate-600">
          <li v-for="q in process.questions" :key="q.id">{{ q.questionText }} <span class="text-xs text-slate-400">({{ q.questionType }}, ağırlık {{ q.weight }})</span></li>
        </ol>
      </div>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <h3 class="text-sm font-semibold text-slate-700">Gelen Yanıtlar</h3>
      <table class="mt-3 w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Cari</th>
            <th class="pb-2">Soru</th>
            <th class="pb-2">Yanıt</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(r, i) in responses" :key="i" class="border-b border-slate-50">
            <td class="py-2 text-slate-700">{{ r.companyName }}</td>
            <td class="py-2 text-slate-500">{{ r.questionText }}</td>
            <td class="py-2 text-slate-700">
              {{ r.selectedOptionText ?? (r.answerBool !== null ? (r.answerBool ? "Evet" : "Hayır") : (r.answerNumber ?? r.answerText)) }}
            </td>
          </tr>
          <tr v-if="responses.length === 0"><td colspan="3" class="py-4 text-center text-slate-400">Henüz yanıt yok</td></tr>
        </tbody>
      </table>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <h3 class="text-sm font-semibold text-slate-700">Puanlı Tedarikçi Değerlendirmesi</h3>
      <div class="mt-3 flex flex-wrap items-end gap-3">
        <select v-model="evalForm.companyId" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
          <option value="" disabled>Cari seç...</option>
          <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
        <input v-model.number="evalForm.totalScore" type="number" min="0" max="100" step="0.5" placeholder="Puan (0-100)" class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        <input v-model="evalForm.comments" placeholder="Yorum" class="flex-1 min-w-[200px] rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        <button class="rounded-md bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white" @click="submitEvaluation">Puanla</button>
      </div>

      <table class="mt-4 w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Cari</th>
            <th class="pb-2">Puan</th>
            <th class="pb-2">Yorum</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="e in evaluations" :key="e.id" class="border-b border-slate-50">
            <td class="py-2 text-slate-700">{{ e.companyName }}</td>
            <td class="py-2 font-mono font-semibold text-indigo-700">{{ e.totalScore }}</td>
            <td class="py-2 text-slate-500">{{ e.comments }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
