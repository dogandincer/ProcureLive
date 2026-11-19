<script setup lang="ts">
import type { RfiProcessDetailDto, RfiResponseDto, SupplierEvaluationDto } from "~/types/api";
import { ArrowDownTrayIcon } from "@heroicons/vue/24/outline";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const accessState = ref<"forbidden" | "notFound" | null>(null);
const toast = useToast();
const { ask } = useConfirm();
const { t } = useI18n();

const process = ref<RfiProcessDetailDto | null>(null);
const responses = ref<RfiResponseDto[]>([]);
const evaluations = ref<SupplierEvaluationDto[]>([]);
// Tedarikçi seçimi "sarı alan": tüm cariler içinde aranır; puanlama yalnızca davetliler arasından.
const inviteCompanyId = ref<string | null>(null);
const inviteCompanyName = ref<string | null>(null);
const { loadSupplierRows } = useSupplierLookup();
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
    const [detail, evalList] = await Promise.all([
      api.get<RfiProcessDetailDto>(`/api/rfi/processes/${processId}`),
      api.get<SupplierEvaluationDto[]>(`/api/rfi/processes/${processId}/evaluations`),
    ]);
    process.value = detail;
    evaluations.value = evalList;
  } catch (error: any) {
    // Yetki yoksa / kayıt yoksa boş sayfa ve kaybolan bildirim yerine açıklayıcı durum gösterilir.
    const code = error?.response?.status ?? error?.statusCode;
    if (code === 403 || code === 404) accessState.value = code === 403 ? "forbidden" : "notFound";
    else toast.error(apiErrorMessage(error, t("rfi.detail.loadError")));
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
    inviteCompanyId.value = null;
    inviteCompanyName.value = null;
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


// ---- İptal, davet geri alma, dışa aktarma ----
const showCancel = ref(false);
const cancelReason = ref("");
const pendingRevoke = ref<{ id: string; companyName: string } | null>(null);
const busyEdit = ref(false);
const { exportRows } = useExcelExport();
const cancellable = computed(() => !!process.value && ['Draft', 'Sent', 'InProgress'].includes(process.value.status));
const revocable = computed(() => !!process.value && ["Draft", "Sent", "InProgress"].includes(process.value.status));

async function confirmCancel() {
  busyEdit.value = true;
  await runAction(async () => {
    await api.post(`/api/rfi/processes/${processId}/cancel`, { reason: cancelReason.value.trim() || null });
    toast.success(t("rfx.edit.cancelled"));
    showCancel.value = false;
    await load();
  });
  busyEdit.value = false;
}

async function confirmRevoke() {
  const inv = pendingRevoke.value;
  if (!inv) return;
  busyEdit.value = true;
  await runAction(async () => {
    await api.delete(`/api/rfi/processes/${processId}/invitations/${inv.id}`);
    toast.success(t("auctions.edit.invitationRevoked"));
    await load();
  });
  pendingRevoke.value = null;
  busyEdit.value = false;
}

const assetUrl = useAssetUrl();
const { formatDate, formatNumber } = useDateFormat();
const answerText = (r: RfiResponseDto) =>
  r.selectedOptionText ?? (r.answerBool !== null ? (r.answerBool ? t("common.yes") : t("common.no")) : (r.answerNumber ?? r.answerText ?? r.attachmentUrl ?? ""));

/** Yanıtlar tedarikçi başına gruplanır; sorular şablondaki sırayla, yanıtsız sorular "—" ile gösterilir. */
const responsesBySupplier = computed(() => {
  const questions = process.value?.questions ?? [];
  const groups = new Map<string, { companyId: string; companyName: string; answers: Map<string, RfiResponseDto> }>();
  for (const r of responses.value) {
    if (!groups.has(r.companyId)) groups.set(r.companyId, { companyId: r.companyId, companyName: r.companyName, answers: new Map() });
    groups.get(r.companyId)!.answers.set(r.rfiQuestionId, r);
  }
  return [...groups.values()]
    .map((g) => ({
      ...g,
      rows: questions.map((q) => ({ question: q, response: g.answers.get(q.id) ?? null })),
      answered: questions.filter((q) => g.answers.has(q.id)).length,
      evaluation: evaluations.value.find((e) => e.companyId === g.companyId) ?? null,
    }))
    .sort((a, b) => a.companyName.localeCompare(b.companyName, "tr"));
});

const inviteOpen = computed(() => !!process.value && ["Draft", "Sent", "InProgress"].includes(process.value.status));
// Puanlanabilecekler: daveti reddetmemiş davetliler.
const evaluable = computed(() => (process.value?.invitations ?? []).filter((i) => i.status !== "Declined"));

async function exportResponses() {
  if (!process.value) return;
  if (responses.value.length === 0) await loadResponses();
  exportRows(`${process.value.title}-cevaplar`, t("rfi.detail.incomingResponses"), [
    { header: t("rfi.detail.responseTable.company"), value: (r: RfiResponseDto) => r.companyName, width: 34 },
    { header: t("rfi.detail.responseTable.question"), value: (r) => r.questionText, width: 44 },
    { header: t("rfi.detail.responseTable.answer"), value: (r) => answerText(r), width: 40 },
  ], responses.value);
}

async function close() {
  if (!(await ask({ title: t("rfi.detail.closeConfirmTitle"), body: t("rfi.detail.closeConfirmBody"), confirmLabel: t("rfi.detail.closeProcess") }))) return;
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
  <AccessState v-if="accessState" :kind="accessState" back-to="/admin/rfi" :back-label="$t('nav.rfi')" />
  <LoadingSpinner v-else-if="loading" />

  <div v-else-if="process" class="space-y-6">
    <ProcessHeader
      :title="process.title"
      :status="process.status"
      back-to="/admin/rfi"
      :back-label="$t('nav.rfi')"
      :meta="[process.ownerCompanyName, `${$t('rfi.detail.templateLabel')}: ${process.templateName}`, `${$t('rfi.answer.dueDate')}: ${formatDate(process.dueDate)}`]"
    >
      <ProcessWorkspace
        kind="rfi"
        :process-id="process.id"
        :owner-company-id="process.ownerCompanyId"
        :workspace-id="process.workspaceId"
        :workspace-name="process.workspaceName"
        :status="process.status"
        @changed="load"
      />
      <template #actions>
        <AppButton v-if="!['Closed', 'Evaluated', 'Cancelled'].includes(process.status)" variant="secondary" @click="close">
          {{ $t("rfi.detail.closeProcess") }}
        </AppButton>
        <AppButton v-if="cancellable" variant="danger-outline" data-testid="cancel-process" @click="showCancel = true; cancelReason = ''">
          {{ $t("rfx.edit.cancel") }}
        </AppButton>
      </template>
    </ProcessHeader>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <AppCard>
        <h3 class="text-sm font-semibold text-slate-700">{{ $t("rfi.detail.invitees") }}</h3>
        <ul v-if="process.invitations.length > 0" class="mt-3 space-y-2 text-sm">
          <li v-for="inv in process.invitations" :key="inv.id" class="flex items-center justify-between">
            <span>{{ inv.companyName }}</span>
            <span class="flex items-center gap-2">
              <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{{ $t(`enums.invitationStatus.${inv.status}`) }}</span>
              <button
              v-if="inv.status === 'Invited' && revocable"
              type="button"
              class="text-xs font-medium text-rose-600 hover:underline"
              data-testid="revoke-invitation"
              @click="pendingRevoke = { id: inv.id, companyName: inv.companyName }"
            >{{ $t("auctions.edit.revoke") }}</button>
            </span>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">{{ $t("rfi.detail.emptyInvites") }}</EmptyState>
        <div v-if="inviteOpen" class="mt-3 flex gap-2">
          <LookupField
            v-model="inviteCompanyId"
            class="min-w-0 flex-1"
            :load="loadSupplierRows"
            :title="$t('rfi.detail.selectCompany')"
            :display="inviteCompanyName"
            :placeholder="$t('rfi.detail.selectCompany')"
            data-testid="invite-company"
            @select="(r) => (inviteCompanyName = r ? `${r.code} · ${r.name}` : null)"
          />
          <AppButton :disabled="!inviteCompanyId" @click="invite">{{ $t("rfi.detail.invite") }}</AppButton>
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
      <h3 class="text-sm font-semibold text-slate-700">
        {{ $t("rfi.detail.incomingResponses") }}
        <button type="button" class="float-right inline-flex items-center gap-1 text-xs font-medium text-indigo-600 hover:underline" data-testid="export-responses" @click="exportResponses">
          <ArrowDownTrayIcon class="h-4 w-4" />{{ $t("auctions.export.button") }}
        </button>
      </h3>
      <div class="mt-3 space-y-3">
        <details
          v-for="g in responsesBySupplier"
          :key="g.companyId"
          :open="responsesBySupplier.length <= 3"
          class="group rounded-lg border border-slate-100"
          data-testid="rfi-supplier-responses"
        >
          <summary class="flex cursor-pointer list-none flex-wrap items-center justify-between gap-2 px-3 py-2">
            <span class="font-medium text-slate-700">{{ g.companyName }}</span>
            <span class="flex items-center gap-2 text-xs text-slate-500">
              <AppBadge v-if="g.evaluation" tone="indigo">{{ $t("rfi.detail.scoreBadge", { score: formatNumber(g.evaluation.totalScore, 1) }) }}</AppBadge>
              <span>{{ $t("rfi.detail.answeredCount", { n: g.answered, total: g.rows.length }) }}</span>
            </span>
          </summary>
          <dl class="divide-y divide-slate-50 border-t border-slate-100 px-3 text-sm">
            <div v-for="{ question: q, response: r } in g.rows" :key="q.id" class="grid gap-1 py-2 sm:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] sm:gap-4">
              <dt class="text-slate-500">{{ q.questionText }}</dt>
              <dd class="text-slate-700">
                <span v-if="!r" class="text-slate-300">—</span>
                <a v-else-if="r.attachmentUrl" :href="assetUrl(r.attachmentUrl) ?? r.attachmentUrl" target="_blank" rel="noopener" class="text-indigo-600 hover:underline">{{ $t("rfi.detail.openAttachment") }}</a>
                <template v-else>{{ answerText(r) }}</template>
              </dd>
            </div>
          </dl>
        </details>
        <EmptyState v-if="responses.length === 0">{{ $t("rfi.detail.emptyResponses") }}</EmptyState>
      </div>
    </AppCard>

    <AppCard>
      <h3 class="text-sm font-semibold text-slate-700">{{ $t("rfi.detail.scoredEvaluation") }}</h3>
      <form class="mt-3 flex flex-wrap items-end gap-3" data-testid="rfi-eval-form" @submit.prevent="submitEvaluation">
        <FormField :label="$t('rfi.detail.evalTable.company')" required class="min-w-[200px]">
          <select v-model="evalForm.companyId" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option value="" disabled>{{ $t("rfi.detail.selectCompany") }}</option>
            <option v-for="inv in evaluable" :key="inv.companyId" :value="inv.companyId">{{ inv.companyName }}</option>
          </select>
        </FormField>
        <FormField :label="$t('rfi.detail.scorePlaceholder')" required>
          <input v-model.number="evalForm.totalScore" type="number" min="0" max="100" step="0.5" required class="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        </FormField>
        <FormField :label="$t('rfi.detail.commentPlaceholder')" class="min-w-[200px] flex-1">
          <input v-model="evalForm.comments" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        </FormField>
        <AppButton type="submit" :disabled="!evalForm.companyId">{{ $t("rfi.detail.score") }}</AppButton>
      </form>

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
              <td class="py-2 font-mono font-semibold text-indigo-700">{{ formatNumber(e.totalScore, 1) }}</td>
              <td class="py-2 text-slate-500">{{ e.comments }}</td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="evaluations.length === 0">{{ $t("rfi.detail.emptyEvaluations") }}</EmptyState>
      </div>
    </AppCard>

    <ProcessQaPanel v-if="process.status !== 'Draft'" kind="rfi" :process-id="processId" internal />

      <ConfirmDialog :open="showCancel" :title="$t('rfx.edit.cancelTitle')" :confirm-label="$t('rfx.edit.cancel')" tone="danger" :loading="busyEdit" @confirm="confirmCancel" @cancel="showCancel = false">
      <p>{{ $t("rfx.edit.cancelBody") }}</p>
      <textarea v-model="cancelReason" rows="2" maxlength="500" :aria-label="$t('rfx.edit.reason')" :placeholder="$t('rfx.edit.reason')" class="mt-2 w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm" data-testid="cancel-reason" />
    </ConfirmDialog>
    <ConfirmDialog :open="!!pendingRevoke" :title="$t('auctions.edit.revokeTitle')" :confirm-label="$t('auctions.edit.revoke')" tone="danger" :loading="busyEdit" @confirm="confirmRevoke" @cancel="pendingRevoke = null">
      {{ $t("auctions.edit.revokeBody", { company: pendingRevoke?.companyName ?? "" }) }}
    </ConfirmDialog>
  </div>
</template>
