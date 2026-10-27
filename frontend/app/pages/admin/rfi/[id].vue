<script setup lang="ts">
import type { CompanyDto, PagedResult, RfiProcessDetailDto, RfiResponseDto, SupplierEvaluationDto } from "~/types/api";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const toast = useToast();
const { t } = useI18n();

const process = ref<RfiProcessDetailDto | null>(null);
const companies = ref<CompanyDto[]>([]);
const responses = ref<RfiResponseDto[]>([]);
const evaluations = ref<SupplierEvaluationDto[]>([]);
const inviteCompanyId = ref("");
const evalForm = reactive({ companyId: "", totalScore: 0, comments: "" });
const loading = ref(true);

async function runAction(action: () => Promise<void>) {
  try {
    await action();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.detail.actionError")));
  }
}

async function load() {
  loading.value = true;
  try {
    const [detail, companyPage, evalList] = await Promise.all([
      api.get<RfiProcessDetailDto>(`/api/rfi/processes/${processId}`),
      api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100, companyType: "Supplier" }),
      api.get<SupplierEvaluationDto[]>(`/api/rfi/processes/${processId}/evaluations`),
    ]);
    process.value = detail;
    companies.value = companyPage.items;
    evaluations.value = evalList;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.detail.loadError")));
  } finally {
    loading.value = false;
  }
}

async function loadResponses() {
  try {
    responses.value = await api.get<RfiResponseDto[]>(`/api/rfi/processes/${processId}/responses`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.detail.responsesLoadError")));
  }
}

async function invite() {
  if (!inviteCompanyId.value) return;
  await runAction(async () => {
    await api.post(`/api/rfi/processes/${processId}/invitations`, { companyIds: [inviteCompanyId.value] });
    inviteCompanyId.value = "";
    toast.success(t("rfi.detail.invitedToast"));
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
    toast.success(t("rfi.detail.evaluationSavedToast"));
    await load();
  });
}

async function close() {
  await runAction(async () => {
    await api.post(`/api/rfi/processes/${processId}/close`);
    toast.success(t("rfi.detail.closedToast"));
    await load();
  });
}

onMounted(async () => {
  await load();
  if (!process.value) return;
  await loadResponses();
});
</script>

<template>
  <LoadingSpinner v-if="loading" />

  <div v-else-if="process" class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
        <p class="text-sm text-slate-500">
          {{ $t("rfi.detail.templateLabel") }}: {{ process.templateName }} · {{ $t("rfi.detail.statusLabel") }}: {{ $t(`enums.processStatus.${process.status}`) }}
        </p>
        <ProcessWorkspace
          kind="rfi"
          :process-id="process.id"
          :owner-company-id="process.ownerCompanyId"
          :workspace-id="process.workspaceId"
          :workspace-name="process.workspaceName"
          :status="process.status"
          @changed="load"
        />
      </div>
      <AppButton v-if="!['Closed', 'Evaluated'].includes(process.status)" variant="secondary" @click="close">
        {{ $t("rfi.detail.closeProcess") }}
      </AppButton>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <AppCard>
        <h3 class="text-sm font-semibold text-slate-700">{{ $t("rfi.detail.invitees") }}</h3>
        <ul v-if="process.invitations.length > 0" class="mt-3 space-y-2 text-sm">
          <li v-for="inv in process.invitations" :key="inv.id" class="flex items-center justify-between">
            <span>{{ inv.companyName }}</span>
            <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ $t(`enums.invitationStatus.${inv.status}`) }}</span>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">{{ $t("rfi.detail.emptyInvites") }}</EmptyState>
        <div class="mt-3 flex gap-2">
          <select v-model="inviteCompanyId" class="flex-1 rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option value="" disabled>{{ $t("rfi.detail.selectCompany") }}</option>
            <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
          <AppButton @click="invite">{{ $t("rfi.detail.invite") }}</AppButton>
        </div>
      </AppCard>

      <AppCard>
        <h3 class="text-sm font-semibold text-slate-700">{{ $t("rfi.detail.questions") }}</h3>
        <ol class="mt-3 list-decimal space-y-1 pl-4 text-sm text-slate-600">
          <li v-for="q in process.questions" :key="q.id">
            {{ q.questionText }} <span class="text-xs text-slate-400">({{ $t(`enums.questionType.${q.questionType}`) }}, {{ $t("rfi.weightLabel") }} {{ q.weight }})</span>
          </li>
        </ol>
      </AppCard>
    </div>

    <AppCard>
      <h3 class="text-sm font-semibold text-slate-700">{{ $t("rfi.detail.incomingResponses") }}</h3>
      <div class="mt-3 overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("rfi.detail.responseTable.company") }}</th>
              <th class="pb-2">{{ $t("rfi.detail.responseTable.question") }}</th>
              <th class="pb-2">{{ $t("rfi.detail.responseTable.answer") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(r, i) in responses" :key="i" class="border-b border-slate-50">
              <td class="py-2 text-slate-700">{{ r.companyName }}</td>
              <td class="py-2 text-slate-500">{{ r.questionText }}</td>
              <td class="py-2 text-slate-700">
                {{ r.selectedOptionText ?? (r.answerBool !== null ? (r.answerBool ? $t("common.yes") : $t("common.no")) : (r.answerNumber ?? r.answerText)) }}
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="responses.length === 0">{{ $t("rfi.detail.emptyResponses") }}</EmptyState>
      </div>
    </AppCard>

    <AppCard>
      <h3 class="text-sm font-semibold text-slate-700">{{ $t("rfi.detail.scoredEvaluation") }}</h3>
      <div class="mt-3 flex flex-wrap items-end gap-3">
        <select v-model="evalForm.companyId" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
          <option value="" disabled>{{ $t("rfi.detail.selectCompany") }}</option>
          <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
        <input v-model.number="evalForm.totalScore" type="number" min="0" max="100" step="0.5" :placeholder="$t('rfi.detail.scorePlaceholder')" class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        <input v-model="evalForm.comments" :placeholder="$t('rfi.detail.commentPlaceholder')" class="flex-1 min-w-[200px] rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        <AppButton @click="submitEvaluation">{{ $t("rfi.detail.score") }}</AppButton>
      </div>

      <div class="mt-4 overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("rfi.detail.evalTable.company") }}</th>
              <th class="pb-2">{{ $t("rfi.detail.evalTable.score") }}</th>
              <th class="pb-2">{{ $t("rfi.detail.evalTable.comment") }}</th>
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
        <EmptyState v-if="evaluations.length === 0">{{ $t("rfi.detail.emptyEvaluations") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
