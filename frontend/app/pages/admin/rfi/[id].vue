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
const answerText = (r: RfiResponseDto) =>
  r.selectedOptionText ?? (r.answerBool !== null ? (r.answerBool ? t("common.yes") : t("common.no")) : (r.answerNumber ?? r.answerText ?? r.attachmentUrl ?? ""));

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
      <div class="flex gap-2">
        <AppButton v-if="!['Closed', 'Evaluated', 'Cancelled'].includes(process.status)" variant="secondary" @click="close">
          {{ $t("rfi.detail.closeProcess") }}
        </AppButton>
        <AppButton v-if="cancellable" variant="danger-outline" data-testid="cancel-process" @click="showCancel = true; cancelReason = ''">
          {{ $t("rfx.edit.cancel") }}
        </AppButton>
      </div>
    </div>

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
        <div class="mt-3 flex gap-2">
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
                <a v-if="r.attachmentUrl" :href="assetUrl(r.attachmentUrl) ?? r.attachmentUrl" target="_blank" rel="noopener" class="text-indigo-600 hover:underline">{{ $t("rfi.detail.openAttachment") }}</a>
                <template v-else>{{ answerText(r) }}</template>
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
        <select v-model="evalForm.companyId" :aria-label="$t('rfi.detail.selectCompany')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
          <option value="" disabled>{{ $t("rfi.detail.selectCompany") }}</option>
          <option v-for="inv in process.invitations" :key="inv.companyId" :value="inv.companyId">{{ inv.companyName }}</option>
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
