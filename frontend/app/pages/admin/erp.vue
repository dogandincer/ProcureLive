<script setup lang="ts">
import type { ErpSettingsDto, ErpSettingsRequest, ErpSyncJobDetailDto, ErpSyncJobDto, ErpSyncSummaryDto, ErpTestResultDto, PagedResult } from "~/types/api";
import { ArrowPathIcon, CheckCircleIcon, ExclamationTriangleIcon, LockClosedIcon, SignalIcon, SparklesIcon, XMarkIcon } from "@heroicons/vue/24/outline";

/**
 * Premium ERP (UyumERP) entegrasyonu — yalnızca Admin. Lisans (Features:ErpIntegration) yoksa tanıtım gösterilir.
 * Mod: Kapalı (ERP'siz çalışır) / Simülasyon (ERP'den okunur, yazılacak XML'ler kayda geçer, gönderilmez) / Canlı.
 */
const api = useApi();
const toast = useToast();
const { ask } = useConfirm();
const { t } = useI18n();
const route = useRoute();
const { formatDateTime } = useDateFormat();
const { ensure, refresh, erpLicensed } = useFeatures();

type Mode = ErpSettingsRequest["mode"];
const modes: Mode[] = ["Off", "Simulation", "Live"];

const loading = ref(true);
const saving = ref(false);
const settings = ref<ErpSettingsDto | null>(null);
const form = reactive<ErpSettingsRequest>({
  mode: "Off", getServiceUrl: "", saveServiceUrl: "", username: "", password: null, coCode: "", branchCode: "",
  offerDocTraCode: "", orderDocTraCode: "", docNoPrefix: "PL-", entityCodePrefix: null, exportNewSuppliers: false, exportNewProducts: false,
});
const saved = ref("");
const snapshot = () => JSON.stringify({ ...form, password: form.password || null });
const dirty = computed(() => snapshot() !== saved.value);
const errorMessage = ref("");

function fill(dto: ErpSettingsDto) {
  settings.value = dto;
  Object.assign(form, { ...dto, password: null });
  delete (form as any).hasPassword;
  delete (form as any).updatedAt;
  delete (form as any).updatedBy;
  saved.value = snapshot();
}

async function load() {
  loading.value = true;
  try {
    fill(await api.get<ErpSettingsDto>("/api/erp/settings"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("erp.loadError")));
  } finally {
    loading.value = false;
  }
}

function payload(): ErpSettingsRequest {
  return { ...form, password: form.password || null, entityCodePrefix: form.entityCodePrefix?.trim() || null };
}

async function save() {
  errorMessage.value = "";
  if (form.mode === "Live" && settings.value?.mode !== "Live"
    && !(await ask({ title: t("erp.liveConfirm.title"), body: t("erp.liveConfirm.body"), confirmLabel: t("erp.liveConfirm.ok"), tone: "danger" }))) return;
  saving.value = true;
  try {
    await api.put("/api/erp/settings", payload());
    toast.success(t("erp.saved"));
    await Promise.all([load(), refresh()]);
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, t("erp.saveError"));
  } finally {
    saving.value = false;
  }
}

// ---------------------------------------------------------------- Bağlantı testi (salt okuma, formdaki değerlerle)
const testing = ref(false);
const testResult = ref<{ ok: boolean; text: string } | null>(null);
async function test() {
  testResult.value = null;
  testing.value = true;
  try {
    const r = await api.post<ErpTestResultDto>("/api/erp/settings/test", payload());
    testResult.value = r.succeeded
      ? { ok: true, text: t("erp.testOk", { entities: r.entityCount ?? 0, items: r.itemCount ?? 0 }) }
      : { ok: false, text: t("erp.testFailed", { error: r.error ?? "" }) };
  } catch (error: any) {
    testResult.value = { ok: false, text: apiErrorMessage(error, t("erp.saveError")) };
  } finally {
    testing.value = false;
  }
}

// ---------------------------------------------------------------- İş kayıtları
const statuses = ["Failed", "Pending", "Simulated", "Done", "Skipped"] as const;
const kinds = ["EntityExport", "ItemExport", "OfferCreate", "OfferRevise", "OrderCreate"] as const;
const tone: Record<string, "amber" | "emerald" | "rose" | "slate" | "indigo"> = { Pending: "amber", Simulated: "indigo", Done: "emerald", Failed: "rose", Skipped: "slate" };
const status = ref(typeof route.query.status === "string" ? route.query.status : "");
const kind = ref("");
const jobs = ref<ErpSyncJobDto[]>([]);
const summary = ref<ErpSyncSummaryDto | null>(null);
const jobsLoading = ref(false);
const { page, total, pageSize, search, params, resetAndLoad } = usePagedList(() => loadJobs(), 20);
let jobSeq = 0;

async function loadJobs() {
  if (!erpLicensed.value) return;
  const mySeq = ++jobSeq;
  jobsLoading.value = true;
  try {
    const [list, sum] = await Promise.all([
      api.get<PagedResult<ErpSyncJobDto>>("/api/erp/jobs", { ...params(), status: status.value || undefined, kind: kind.value || undefined }),
      api.get<ErpSyncSummaryDto>("/api/erp/jobs/summary"),
    ]);
    if (mySeq !== jobSeq) return;
    jobs.value = list.items;
    total.value = list.totalCount;
    summary.value = sum;
  } catch (error: any) {
    if (mySeq === jobSeq) toast.error(apiErrorMessage(error, t("erp.loadError")));
  } finally {
    if (mySeq === jobSeq) jobsLoading.value = false;
  }
}
watch([status, kind], () => resetAndLoad());
const statusOptions = computed(() =>
  statuses.map((s) => ({ value: s, label: `${t(`erp.jobStatus.${s}`)}${summary.value ? ` (${summary.value[(s.charAt(0).toLowerCase() + s.slice(1)) as keyof ErpSyncSummaryDto]})` : ""}` })),
);

const detail = ref<ErpSyncJobDetailDto | null>(null);
async function openDetail(job: ErpSyncJobDto) {
  try {
    detail.value = await api.get<ErpSyncJobDetailDto>(`/api/erp/jobs/${job.id}`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("erp.loadError")));
  }
}
async function retry(job: ErpSyncJobDto) {
  try {
    await api.post(`/api/erp/jobs/${job.id}/retry`);
    toast.success(t("erp.retried"));
    detail.value = null;
    await loadJobs();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("erp.retryError")));
  }
}

onBeforeRouteLeave(async () => {
  if (dirty.value && !(await ask({ title: t("emailSettings.unsaved"), confirmLabel: t("emailSettings.leave"), tone: "danger" }))) return false;
});

onMounted(async () => {
  await ensure();
  if (!erpLicensed.value) {
    loading.value = false;
    return;
  }
  await Promise.all([load(), loadJobs()]);
});
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center gap-2">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("erp.title") }}</h1>
      <span class="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-800"><SparklesIcon class="h-3.5 w-3.5" />{{ $t("erp.premium") }}</span>
    </div>

    <LoadingSpinner v-if="loading" />

    <!-- Lisans yok: tanıtım -->
    <AppCard v-else-if="!erpLicensed" data-testid="erp-premium">
      <div class="flex flex-col gap-4 sm:flex-row">
        <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-amber-100"><LockClosedIcon class="h-6 w-6 text-amber-700" /></div>
        <div class="space-y-3 text-sm text-slate-600">
          <p class="text-base font-semibold text-slate-800">{{ $t("erp.locked.title") }}</p>
          <p>{{ $t("erp.locked.body") }}</p>
          <ul class="list-disc space-y-1 pl-5">
            <li v-for="n in 5" :key="n">{{ $t(`erp.locked.f${n}`) }}</li>
          </ul>
          <p class="text-xs text-slate-500">{{ $t("erp.locked.how") }}</p>
        </div>
      </div>
    </AppCard>

    <template v-else>
      <p class="text-sm text-slate-500">{{ $t("erp.subtitle") }}</p>
      <div class="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <form class="space-y-6 xl:col-span-2" data-testid="erp-form" novalidate @submit.prevent="save">
          <AppCard :title="$t('erp.modeSection')">
            <div class="grid grid-cols-1 gap-2 sm:grid-cols-3" role="radiogroup" :aria-label="$t('erp.modeSection')" data-testid="erp-mode">
              <label
                v-for="m in modes"
                :key="m"
                class="flex cursor-pointer flex-col gap-1 rounded-lg border p-3 text-sm transition-colors"
                :class="form.mode === m ? 'border-indigo-500 bg-indigo-50 ring-1 ring-indigo-500' : 'border-slate-200 hover:border-slate-300'"
              >
                <span class="flex items-center gap-2 font-semibold text-slate-800">
                  <input v-model="form.mode" type="radio" name="erp-mode" :value="m" class="text-indigo-600" :data-mode="m" />{{ $t(`erp.mode.${m}`) }}
                </span>
                <span class="text-xs text-slate-500">{{ $t(`erp.modeHint.${m}`) }}</span>
              </label>
            </div>
          </AppCard>

          <AppCard :title="$t('erp.connectionSection')" :description="$t('erp.connectionHint')">
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField :label="$t('erp.getUrl')" :required="form.mode !== 'Off'" class="sm:col-span-2">
                <input v-model="form.getServiceUrl" type="url" spellcheck="false" placeholder="https://erp.firma.com/WebService/ERP/UyumGetWebService.asmx" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-get-url" />
              </FormField>
              <FormField :label="$t('erp.saveUrl')" :required="form.mode === 'Live'" class="sm:col-span-2">
                <input v-model="form.saveServiceUrl" type="url" spellcheck="false" placeholder="https://erp.firma.com/WebService/ERP/UyumSaveWebService.asmx" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-save-url" />
              </FormField>
              <FormField :label="$t('erp.username')" :required="form.mode !== 'Off'">
                <input v-model="form.username" type="text" autocomplete="off" spellcheck="false" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-username" />
              </FormField>
              <FormField :label="$t('erp.password')" :hint="$t('emailSettings.passwordHint')">
                <input v-model="form.password" type="password" autocomplete="new-password" :placeholder="settings?.hasPassword ? $t('emailSettings.passwordSaved') : ''" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-password" />
              </FormField>
              <FormField :label="$t('erp.coCode')" :required="form.mode !== 'Off'">
                <input v-model="form.coCode" type="text" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-co-code" />
              </FormField>
              <FormField :label="$t('erp.branchCode')" :required="form.mode === 'Live'">
                <input v-model="form.branchCode" type="text" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-branch-code" />
              </FormField>
            </div>
          </AppCard>

          <AppCard :title="$t('erp.documentsSection')" :description="$t('erp.documentsHint')">
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <FormField :label="$t('erp.offerDocTra')" :required="form.mode === 'Live'">
                <input v-model="form.offerDocTraCode" type="text" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-offer-doctra" />
              </FormField>
              <FormField :label="$t('erp.orderDocTra')" :required="form.mode === 'Live'">
                <input v-model="form.orderDocTraCode" type="text" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-order-doctra" />
              </FormField>
              <FormField :label="$t('erp.docNoPrefix')" :hint="$t('erp.docNoPrefixHint')">
                <input v-model="form.docNoPrefix" type="text" maxlength="10" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
              </FormField>
            </div>
          </AppCard>

          <AppCard :title="$t('erp.syncSection')">
            <div class="space-y-4">
              <FormField :label="$t('erp.entityPrefix')" :hint="$t('erp.entityPrefixHint')">
                <input v-model="form.entityCodePrefix" type="text" placeholder="320" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:max-w-xs" />
              </FormField>
              <AppToggle v-model="form.exportNewSuppliers" :label="$t('erp.exportSuppliers')" :description="$t('erp.exportSuppliersHint')" />
              <AppToggle v-model="form.exportNewProducts" :label="$t('erp.exportProducts')" :description="$t('erp.exportProductsHint')" />
            </div>
          </AppCard>

          <div class="sticky bottom-0 z-10 flex flex-wrap items-center gap-3 rounded-lg border border-slate-200 bg-surface/95 px-4 py-3 shadow-sm backdrop-blur">
            <AppButton type="submit" :loading="saving" :disabled="!dirty" data-testid="erp-save">{{ $t("common.save") }}</AppButton>
            <AppButton v-if="dirty" variant="ghost" @click="settings && fill(settings)">{{ $t("common.cancel") }}</AppButton>
            <span v-if="dirty" class="text-xs text-amber-700">{{ $t("emailSettings.unsaved") }}</span>
            <span v-else-if="settings?.updatedAt" class="text-xs text-slate-400">{{ $t("emailSettings.lastUpdated", { at: formatDateTime(settings.updatedAt), by: settings.updatedBy ?? "—" }) }}</span>
            <p v-if="errorMessage" role="alert" class="w-full rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700" data-testid="erp-save-error">{{ errorMessage }}</p>
          </div>
        </form>

        <div class="space-y-6">
          <AppCard :title="$t('erp.testSection')" :description="$t('erp.testHint')">
            <AppButton variant="secondary" :loading="testing" :disabled="!form.getServiceUrl.trim() || !form.username.trim()" data-testid="erp-test" @click="test">
              <SignalIcon class="h-4 w-4" />{{ $t("erp.test") }}
            </AppButton>
            <div
              v-if="testResult"
              role="status"
              class="mt-3 flex items-start gap-2 rounded-md border px-3 py-2 text-sm"
              :class="testResult.ok ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-rose-200 bg-rose-50 text-rose-700'"
              data-testid="erp-test-result"
            >
              <component :is="testResult.ok ? CheckCircleIcon : ExclamationTriangleIcon" class="mt-0.5 h-4 w-4 shrink-0" />
              <span class="break-words">{{ testResult.text }}</span>
            </div>
          </AppCard>
          <AppCard :title="$t('erp.howSection')">
            <ol class="list-decimal space-y-1.5 pl-5 text-xs text-slate-600">
              <li v-for="n in 5" :key="n">{{ $t(`erp.how.s${n}`) }}</li>
            </ol>
          </AppCard>
        </div>
      </div>

      <AppCard :title="$t('erp.jobsSection')" :description="$t('erp.jobsHint')">
        <template #action>
          <AppButton variant="ghost" :loading="jobsLoading" :aria-label="$t('erp.refresh')" @click="loadJobs">
            <ArrowPathIcon class="h-4 w-4" /><span class="hidden sm:inline">{{ $t("erp.refresh") }}</span>
          </AppButton>
        </template>
        <div class="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <StatusFilter v-model="status" :options="statusOptions" />
          <div class="flex flex-col gap-2 sm:flex-row">
            <select v-model="kind" class="rounded-md border border-slate-300 px-3 py-1.5 text-sm" :aria-label="$t('erp.kindFilter')" data-testid="erp-kind">
              <option value="">{{ $t("erp.allKinds") }}</option>
              <option v-for="k in kinds" :key="k" :value="k">{{ $t(`erp.kind.${k}`) }}</option>
            </select>
            <input v-model="search" type="search" :placeholder="$t('erp.jobSearch')" :aria-label="$t('erp.jobSearch')" class="rounded-md border border-slate-300 px-3 py-1.5 text-sm sm:w-56" />
          </div>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-sm" data-testid="erp-jobs-table">
            <thead>
              <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
                <th class="pb-2 pr-3">{{ $t("emailSettings.logCols.date") }}</th>
                <th class="pb-2 pr-3">{{ $t("erp.jobCol") }}</th>
                <th class="pb-2 pr-3">{{ $t("common.status") }}</th>
                <th class="hidden pb-2 pr-3 lg:table-cell">{{ $t("emailSettings.logCols.detail") }}</th>
                <th class="pb-2"><span class="sr-only">{{ $t("common.actions") }}</span></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="j in jobs" :key="j.id" class="border-b border-slate-50 align-top" data-testid="erp-job-row">
                <td class="whitespace-nowrap py-2 pr-3 text-slate-500">{{ formatDateTime(j.completedAt ?? j.createdAt) }}</td>
                <td class="py-2 pr-3">
                  <div class="text-slate-700">{{ j.summary }}</div>
                  <div class="text-xs text-slate-400">{{ $t(`erp.kind.${j.kind}`) }}</div>
                </td>
                <td class="py-2 pr-3">
                  <AppBadge :tone="tone[j.status] ?? 'slate'">{{ $t(`erp.jobStatus.${j.status}`) }}</AppBadge>
                  <div v-if="j.attemptCount > 1" class="mt-1 text-[11px] text-slate-400">{{ $t("emailSettings.attempts", { n: j.attemptCount }) }}</div>
                </td>
                <td class="hidden max-w-sm py-2 pr-3 text-xs text-slate-500 lg:table-cell">{{ j.message }}</td>
                <td class="whitespace-nowrap py-2 text-right">
                  <AppButton variant="ghost" @click="openDetail(j)">{{ $t("erp.viewXml") }}</AppButton>
                </td>
              </tr>
            </tbody>
          </table>
          <EmptyState v-if="!jobsLoading && jobs.length === 0">{{ $t("erp.noJobs") }}</EmptyState>
        </div>
        <ListPager v-model:page="page" :total="total" :page-size="pageSize" />
      </AppCard>

      <Teleport to="body">
        <div v-if="detail" class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4" @click.self="detail = null" @keydown.esc="detail = null">
          <div role="dialog" aria-modal="true" :aria-label="detail.job.summary" class="mt-10 w-full max-w-4xl rounded-xl bg-surface shadow-xl" data-testid="erp-job-detail">
            <div class="flex items-start justify-between border-b border-slate-100 px-5 py-4">
              <div>
                <h2 class="text-base font-semibold text-slate-800">{{ detail.job.summary }}</h2>
                <p class="mt-0.5 text-xs text-slate-500">{{ $t(`erp.kind.${detail.job.kind}`) }} · {{ $t(`erp.jobStatus.${detail.job.status}`) }}<template v-if="detail.job.message"> · {{ detail.job.message }}</template></p>
              </div>
              <button type="button" class="rounded p-1 text-slate-400 hover:bg-slate-100" :aria-label="$t('common.close')" @click="detail = null"><XMarkIcon class="h-5 w-5" /></button>
            </div>
            <div class="space-y-4 p-5">
              <div>
                <div class="mb-1 text-xs font-semibold uppercase text-slate-400">{{ $t("erp.request") }}</div>
                <pre class="max-h-80 overflow-auto whitespace-pre-wrap break-all rounded bg-slate-50 p-3 text-xs text-slate-700">{{ detail.requestXml ?? "—" }}</pre>
              </div>
              <div v-if="detail.responseText">
                <div class="mb-1 text-xs font-semibold uppercase text-slate-400">{{ $t("erp.response") }}</div>
                <pre class="max-h-60 overflow-auto whitespace-pre-wrap break-all rounded bg-slate-50 p-3 text-xs text-slate-700">{{ detail.responseText }}</pre>
              </div>
            </div>
            <div class="flex justify-end gap-2 border-t border-slate-100 px-5 py-3">
              <AppButton v-if="['Failed', 'Skipped', 'Simulated'].includes(detail.job.status)" variant="secondary" data-testid="erp-job-retry" @click="retry(detail.job)">{{ $t("erp.runAgain") }}</AppButton>
              <AppButton variant="ghost" @click="detail = null">{{ $t("common.close") }}</AppButton>
            </div>
          </div>
        </div>
      </Teleport>
    </template>
  </div>
</template>
