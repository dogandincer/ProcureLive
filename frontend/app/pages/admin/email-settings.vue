<script setup lang="ts">
import type {
  EmailDeliveryStatus,
  EmailLogDto,
  EmailLogSummaryDto,
  EmailSecurityMode,
  EmailSettingsDto,
  EmailSettingsRequest,
  PagedResult,
  TestEmailResultDto,
} from "~/types/api";
import { ArrowPathIcon, CheckCircleIcon, ExclamationTriangleIcon, PaperAirplaneIcon } from "@heroicons/vue/24/outline";

/**
 * E-posta ayarları (yalnızca Admin): SMTP sunucusu, gönderen, e-postası gönderilecek bildirim türleri, test gönderimi ve
 * bildirim e-postalarının alıcı bazında gönderim kayıtları. Şifre sunucudan hiç gelmez; boş bırakılırsa kayıtlı şifre korunur.
 */
const api = useApi();
const toast = useToast();
const { ask } = useConfirm();
const { t } = useI18n();
const route = useRoute();
const { formatDateTime } = useDateFormat();
const { currentUser } = useAuth();

const securityModes: EmailSecurityMode[] = ["StartTls", "SslOnConnect", "None", "Auto"];

type PresetKey = "custom" | "gmail" | "office365" | "yandex" | "mailpit";
const presets: Record<Exclude<PresetKey, "custom">, { host: string; port: number; security: EmailSecurityMode }> = {
  gmail: { host: "smtp.gmail.com", port: 587, security: "StartTls" },
  office365: { host: "smtp.office365.com", port: 587, security: "StartTls" },
  yandex: { host: "smtp.yandex.com", port: 465, security: "SslOnConnect" },
  mailpit: { host: "localhost", port: 1025, security: "None" },
};

const loading = ref(true);
const saving = ref(false);
const loadFailed = ref(false);
const settings = ref<EmailSettingsDto | null>(null);

function emptyForm(): EmailSettingsRequest {
  return {
    enabled: false, host: "", port: 587, security: "StartTls", username: null, password: null, clearPassword: false,
    fromAddress: "", fromName: "", replyTo: null, disabledNotificationTypes: [],
  };
}
const form = reactive<EmailSettingsRequest>(emptyForm());
const saved = ref("");
const snapshot = () => JSON.stringify({ ...form, password: form.password || null });
const dirty = computed(() => snapshot() !== saved.value);

const preset = computed<PresetKey>(() => {
  const hit = (Object.keys(presets) as (keyof typeof presets)[]).find(
    (k) => presets[k].host === form.host.trim() && presets[k].port === Number(form.port) && presets[k].security === form.security,
  );
  return hit ?? "custom";
});
function applyPreset(key: PresetKey) {
  if (key === "custom") return;
  Object.assign(form, presets[key]);
}

function fill(dto: EmailSettingsDto) {
  settings.value = dto;
  Object.assign(form, {
    enabled: dto.enabled, host: dto.host, port: dto.port, security: dto.security, username: dto.username, password: null,
    clearPassword: false, fromAddress: dto.fromAddress, fromName: dto.fromName, replyTo: dto.replyTo,
    disabledNotificationTypes: [...dto.disabledNotificationTypes],
  });
  saved.value = snapshot();
}

async function load() {
  loading.value = true;
  loadFailed.value = false;
  try {
    fill(await api.get<EmailSettingsDto>("/api/settings/email"));
  } catch (error: any) {
    loadFailed.value = true;
    toast.error(apiErrorMessage(error, t("emailSettings.loadError")));
  } finally {
    loading.value = false;
  }
}

// Bildirim türleri: işaretli = e-postası gönderilir (sunucu tersini, devre dışı listesini saklar).
const types = computed(() => settings.value?.notificationTypes ?? []);
function typeEnabled(type: string) {
  return !form.disabledNotificationTypes.includes(type);
}
function toggleType(type: string) {
  form.disabledNotificationTypes = typeEnabled(type)
    ? [...form.disabledNotificationTypes, type]
    : form.disabledNotificationTypes.filter((x) => x !== type);
}
function setAllTypes(on: boolean) {
  form.disabledNotificationTypes = on ? [] : [...types.value];
}

function payload(): EmailSettingsRequest {
  return {
    ...form,
    port: Number(form.port),
    username: form.username?.trim() || null,
    password: form.clearPassword ? null : form.password || null,
    replyTo: form.replyTo?.trim() || null,
  };
}

const errorMessage = ref("");
async function save() {
  errorMessage.value = "";
  saving.value = true;
  try {
    await api.put("/api/settings/email", payload());
    toast.success(t("emailSettings.saved"));
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, t("emailSettings.saveError"));
  } finally {
    saving.value = false;
  }
}

// ---------------------------------------------------------------- Test e-postası (formdaki, kaydedilmemiş ayarla)
const testTo = ref("");
const testing = ref(false);
const testResult = ref<{ ok: boolean; text: string } | null>(null);
async function sendTest() {
  testResult.value = null;
  testing.value = true;
  try {
    const r = await api.post<TestEmailResultDto>("/api/settings/email/test", { to: testTo.value.trim(), settings: payload() });
    testResult.value = r.succeeded
      ? { ok: true, text: t("emailSettings.testOk", { to: testTo.value.trim() }) }
      : { ok: false, text: t("emailSettings.testFailed", { error: r.error ?? "" }) };
  } catch (error: any) {
    testResult.value = { ok: false, text: apiErrorMessage(error, t("emailSettings.saveError")) };
  } finally {
    testing.value = false;
  }
}

// ---------------------------------------------------------------- Gönderim kayıtları
const statuses: EmailDeliveryStatus[] = ["Failed", "Pending", "Sent", "Skipped"];
const statusTone: Record<EmailDeliveryStatus, "amber" | "emerald" | "rose" | "slate"> = {
  Pending: "amber", Sent: "emerald", Failed: "rose", Skipped: "slate",
};
const status = ref(typeof route.query.status === "string" ? route.query.status : "");
const logs = ref<EmailLogDto[]>([]);
const summary = ref<EmailLogSummaryDto | null>(null);
const logsLoading = ref(false);
const { page, total, pageSize, search, params, resetAndLoad } = usePagedList(() => loadLogs(), 20);
let logSeq = 0;

async function loadLogs() {
  const seq = ++logSeq;
  logsLoading.value = true;
  try {
    const [list, sum] = await Promise.all([
      api.get<PagedResult<EmailLogDto>>("/api/settings/email/logs", { ...params(), status: status.value || undefined }),
      api.get<EmailLogSummaryDto>("/api/settings/email/logs/summary"),
    ]);
    if (seq !== logSeq) return;
    logs.value = list.items;
    total.value = list.totalCount;
    summary.value = sum;
  } catch (error: any) {
    if (seq === logSeq) toast.error(apiErrorMessage(error, t("emailSettings.loadError")));
  } finally {
    if (seq === logSeq) logsLoading.value = false;
  }
}
watch(status, () => resetAndLoad());

const statusOptions = computed(() =>
  statuses.map((s) => ({ value: s, label: `${t(`emailSettings.logStatus.${s}`)}${summary.value ? ` (${summary.value[s.toLowerCase() as keyof EmailLogSummaryDto]})` : ""}` })),
);

async function retry(log: EmailLogDto) {
  try {
    await api.post(`/api/settings/email/logs/${log.id}/retry`);
    toast.success(t("emailSettings.retried"));
    await loadLogs();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("emailSettings.retryError")));
  }
}

function typeLabel(type: string) {
  const key = `emailSettings.types.${type}`;
  const label = t(key);
  return label === key ? type : label;
}

onBeforeRouteLeave(async () => {
  if (dirty.value && !(await ask({ title: t("emailSettings.unsaved"), confirmLabel: t("emailSettings.leave"), tone: "danger" }))) return false;
});

onMounted(() => {
  testTo.value = currentUser.value?.email ?? "";
  load();
  loadLogs();
});
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ $t("emailSettings.title") }}</h1>
      <p class="mt-1 text-sm text-slate-500">{{ $t("emailSettings.subtitle") }}</p>
    </div>

    <LoadingSpinner v-if="loading && !settings" />
    <div v-else-if="loadFailed && !settings" class="rounded-xl border border-rose-200 bg-rose-50 p-5 text-sm text-rose-700">
      {{ $t("emailSettings.loadError") }}
      <AppButton variant="secondary" class="ml-3" @click="load">{{ $t("emailSettings.refresh") }}</AppButton>
    </div>

    <div v-else class="grid grid-cols-1 gap-6 xl:grid-cols-3">
      <form class="space-y-6 xl:col-span-2" data-testid="email-settings-form" novalidate @submit.prevent="save">
        <AppCard>
          <AppToggle v-model="form.enabled" :label="$t('emailSettings.enabled')" :description="$t('emailSettings.enabledHint')" data-testid="email-enabled" />
          <div
            v-if="settings && !settings.enabled && !form.enabled"
            class="mt-4 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800"
            data-testid="email-disabled-note"
          >
            {{ $t("emailSettings.disabledNote") }}
          </div>
        </AppCard>

        <AppCard :title="$t('emailSettings.serverSection')">
          <div class="grid grid-cols-1 gap-4 sm:grid-cols-6">
            <FormField :label="$t('emailSettings.preset')" :hint="$t('emailSettings.presetHint')" class="sm:col-span-6">
              <select
                :value="preset"
                class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:max-w-xs"
                data-testid="email-preset"
                @change="applyPreset(($event.target as HTMLSelectElement).value as PresetKey)"
              >
                <option v-for="k in ['custom', 'gmail', 'office365', 'yandex', 'mailpit']" :key="k" :value="k">{{ $t(`emailSettings.presets.${k}`) }}</option>
              </select>
            </FormField>
            <FormField :label="$t('emailSettings.host')" :required="form.enabled" class="sm:col-span-3">
              <input v-model="form.host" type="text" autocomplete="off" spellcheck="false" placeholder="smtp.firma.com" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="email-host" />
            </FormField>
            <FormField :label="$t('emailSettings.port')" required class="sm:col-span-1">
              <input v-model.number="form.port" type="number" min="1" max="65535" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="email-port" />
            </FormField>
            <FormField :label="$t('emailSettings.security')" class="sm:col-span-2">
              <select v-model="form.security" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="email-security">
                <option v-for="m in securityModes" :key="m" :value="m">{{ $t(`emailSettings.securityOptions.${m}`) }}</option>
              </select>
            </FormField>
            <FormField :label="$t('emailSettings.username')" class="sm:col-span-3">
              <input v-model="form.username" type="text" autocomplete="off" spellcheck="false" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="email-username" />
            </FormField>
            <FormField :label="$t('emailSettings.password')" :hint="$t('emailSettings.passwordHint')" class="sm:col-span-3">
              <input
                v-model="form.password"
                type="password"
                autocomplete="new-password"
                :disabled="form.clearPassword"
                :placeholder="settings?.hasPassword ? $t('emailSettings.passwordSaved') : ''"
                class="rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50"
                data-testid="email-password"
              />
            </FormField>
            <label v-if="settings?.hasPassword" class="flex items-center gap-2 text-xs text-slate-600 sm:col-span-6">
              <input v-model="form.clearPassword" type="checkbox" class="rounded border-slate-300" data-testid="email-clear-password" />
              {{ $t("emailSettings.clearPassword") }}
            </label>
          </div>
        </AppCard>

        <AppCard :title="$t('emailSettings.senderSection')">
          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <FormField :label="$t('emailSettings.fromAddress')" :required="form.enabled">
              <input v-model="form.fromAddress" type="email" autocomplete="off" placeholder="ihale@firma.com" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="email-from-address" />
            </FormField>
            <FormField :label="$t('emailSettings.fromName')" :hint="$t('emailSettings.fromNameHint')">
              <input v-model="form.fromName" type="text" maxlength="150" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="email-from-name" />
            </FormField>
            <FormField :label="$t('emailSettings.replyTo')" :hint="$t('emailSettings.replyToHint')" class="sm:col-span-2">
              <input v-model="form.replyTo" type="email" autocomplete="off" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:max-w-sm" data-testid="email-reply-to" />
            </FormField>
          </div>
        </AppCard>

        <AppCard :title="$t('emailSettings.typesSection')" :description="$t('emailSettings.typesHint')">
          <template #action>
            <div class="flex gap-1">
              <AppButton variant="ghost" @click="setAllTypes(true)">{{ $t("emailSettings.selectAll") }}</AppButton>
              <AppButton variant="ghost" @click="setAllTypes(false)">{{ $t("emailSettings.selectNone") }}</AppButton>
            </div>
          </template>
          <div class="grid grid-cols-1 gap-x-6 gap-y-2 sm:grid-cols-2" data-testid="email-types">
            <label v-for="type in types" :key="type" class="flex items-center gap-2 text-sm text-slate-700">
              <input type="checkbox" class="rounded border-slate-300" :checked="typeEnabled(type)" :data-type="type" @change="toggleType(type)" />
              {{ typeLabel(type) }}
            </label>
          </div>
        </AppCard>

        <div class="sticky bottom-0 z-10 -mx-1 flex flex-wrap items-center gap-3 rounded-lg border border-slate-200 bg-surface/95 px-4 py-3 shadow-sm backdrop-blur">
          <AppButton type="submit" :loading="saving" :disabled="!dirty" data-testid="email-save">{{ $t("common.save") }}</AppButton>
          <AppButton v-if="dirty" variant="ghost" @click="settings && fill(settings)">{{ $t("common.cancel") }}</AppButton>
          <span v-if="dirty" class="text-xs text-amber-700">{{ $t("emailSettings.unsaved") }}</span>
          <span v-else-if="settings?.updatedAt" class="text-xs text-slate-400">
            {{ $t("emailSettings.lastUpdated", { at: formatDateTime(settings.updatedAt), by: settings.updatedBy ?? "—" }) }}
          </span>
          <p v-if="errorMessage" role="alert" class="w-full rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700" data-testid="email-save-error">{{ errorMessage }}</p>
        </div>
      </form>

      <div class="space-y-6">
        <AppCard :title="$t('emailSettings.testSection')" :description="$t('emailSettings.testHint')">
          <form class="space-y-3" data-testid="email-test" @submit.prevent="sendTest">
            <FormField :label="$t('emailSettings.testTo')" required>
              <input v-model="testTo" type="email" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="email-test-to" />
            </FormField>
            <AppButton type="submit" variant="secondary" :loading="testing" :disabled="!testTo.trim() || !form.host.trim() || !form.fromAddress.trim()">
              <PaperAirplaneIcon class="h-4 w-4" />{{ $t("emailSettings.testSend") }}
            </AppButton>
            <div
              v-if="testResult"
              role="status"
              class="flex items-start gap-2 rounded-md border px-3 py-2 text-sm"
              :class="testResult.ok ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-rose-200 bg-rose-50 text-rose-700'"
              data-testid="email-test-result"
            >
              <component :is="testResult.ok ? CheckCircleIcon : ExclamationTriangleIcon" class="mt-0.5 h-4 w-4 shrink-0" />
              <span class="break-words">{{ testResult.text }}</span>
            </div>
          </form>
        </AppCard>
      </div>
    </div>

    <AppCard :title="$t('emailSettings.logSection')" :description="$t('emailSettings.logHint')">
      <template #action>
        <AppButton variant="ghost" :loading="logsLoading" :aria-label="$t('emailSettings.refresh')" @click="loadLogs">
          <ArrowPathIcon class="h-4 w-4" /><span class="hidden sm:inline">{{ $t("emailSettings.refresh") }}</span>
        </AppButton>
      </template>
      <div class="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <StatusFilter v-model="status" :options="statusOptions" />
        <input v-model="search" type="search" :placeholder="$t('emailSettings.logSearch')" :aria-label="$t('emailSettings.logSearch')" class="rounded-md border border-slate-300 px-3 py-1.5 text-sm sm:w-64" />
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm" data-testid="email-logs">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2 pr-3">{{ $t("emailSettings.logCols.date") }}</th>
              <th class="pb-2 pr-3">{{ $t("emailSettings.logCols.recipient") }}</th>
              <th class="hidden pb-2 pr-3 md:table-cell">{{ $t("emailSettings.logCols.subject") }}</th>
              <th class="pb-2 pr-3">{{ $t("emailSettings.logCols.status") }}</th>
              <th class="hidden pb-2 pr-3 lg:table-cell">{{ $t("emailSettings.logCols.detail") }}</th>
              <th class="pb-2"><span class="sr-only">{{ $t("common.actions") }}</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="log in logs" :key="log.id" class="border-b border-slate-50 align-top" data-testid="email-log-row">
              <td class="whitespace-nowrap py-2 pr-3 text-slate-500">{{ formatDateTime(log.sentAt ?? log.createdAt) }}</td>
              <td class="py-2 pr-3">
                <div class="break-all text-slate-700">{{ log.recipient ?? "—" }}</div>
                <div class="text-xs text-slate-400 md:hidden">{{ log.subject }}</div>
              </td>
              <td class="hidden py-2 pr-3 md:table-cell">
                <div class="text-slate-700">{{ log.subject }}</div>
                <div class="text-xs text-slate-400">{{ typeLabel(log.notificationType) }}</div>
              </td>
              <td class="py-2 pr-3">
                <AppBadge :tone="statusTone[log.status]">{{ $t(`emailSettings.logStatus.${log.status}`) }}</AppBadge>
                <div v-if="log.attemptCount > 1" class="mt-1 text-[11px] text-slate-400">{{ $t("emailSettings.attempts", { n: log.attemptCount }) }}</div>
              </td>
              <td class="hidden max-w-sm py-2 pr-3 text-xs text-slate-500 lg:table-cell">{{ log.errorMessage }}</td>
              <td class="py-2 text-right">
                <AppButton v-if="log.status === 'Failed' && log.recipient" variant="ghost" :title="log.errorMessage ?? ''" @click="retry(log)">
                  {{ $t("emailSettings.retry") }}
                </AppButton>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="!logsLoading && logs.length === 0">{{ $t("emailSettings.logEmpty") }}</EmptyState>
      </div>
      <ListPager v-model:page="page" :total="total" :page-size="pageSize" />
    </AppCard>
  </div>
</template>
