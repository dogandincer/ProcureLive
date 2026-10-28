<script setup lang="ts">
import type { CompanySettingsDto } from "~/types/api";
import { SparklesIcon } from "@heroicons/vue/24/solid";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();

const settings = ref<CompanySettingsDto | null>(null);
const form = reactive({ logoUrl: "", showContactInfo: false, listTendersForPlus: false });
const loading = ref(true);
const saving = ref(false);
const logoBroken = ref(false);

function fill(s: CompanySettingsDto) {
  settings.value = s;
  form.logoUrl = s.logoUrl ?? "";
  form.showContactInfo = s.showContactInfo;
  form.listTendersForPlus = s.listTendersForPlus;
  logoBroken.value = false;
}

const dirty = computed(
  () =>
    !!settings.value &&
    ((settings.value.logoUrl ?? "") !== form.logoUrl.trim() ||
      settings.value.showContactInfo !== form.showContactInfo ||
      settings.value.listTendersForPlus !== form.listTendersForPlus),
);

watch(() => form.logoUrl, () => (logoBroken.value = false));

async function save() {
  saving.value = true;
  try {
    fill(
      await api.put<CompanySettingsDto>("/api/companies/me/settings", {
        logoUrl: form.logoUrl.trim() || null,
        showContactInfo: form.showContactInfo,
        listTendersForPlus: form.listTendersForPlus,
      }),
    );
    toast.success(t("companySettings.savedToast"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companySettings.saveError")));
  } finally {
    saving.value = false;
  }
}

onMounted(async () => {
  try {
    fill(await api.get<CompanySettingsDto>("/api/companies/me/settings"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companySettings.loadError")));
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="mx-auto max-w-3xl space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ $t("companySettings.title") }}</h1>
      <p class="mt-0.5 text-sm text-slate-500">{{ settings?.companyName }}</p>
    </div>

    <LoadingSpinner v-if="loading" />

    <template v-else-if="settings">
      <!-- Plan -->
      <div class="flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4" :class="settings.isPlus ? 'border-amber-200 bg-amber-50' : 'border-slate-200 bg-surface'">
        <div class="flex items-center gap-2 text-sm">
          <SparklesIcon v-if="settings.isPlus" class="h-5 w-5 text-amber-500" />
          <span class="font-semibold text-slate-800">{{ settings.isPlus ? "Plus" : $t("pricing.standard") }}</span>
          <span v-if="settings.isPlus && settings.plusExpiresAt" class="text-xs text-slate-500">
            {{ $t("pricing.validUntil", { date: formatDate(settings.plusExpiresAt) }) }}
          </span>
        </div>
        <NuxtLink to="/supplier/pricing" class="text-sm font-semibold text-indigo-600 hover:underline">
          {{ settings.isPlus ? $t("companySettings.managePlan") : $t("restricted.upgradeCta") }} →
        </NuxtLink>
      </div>

      <AppCard :title="$t('companySettings.brandTitle')" :description="$t('companySettings.brandDescription')">
        <div class="flex items-start gap-4">
          <div class="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
            <img v-if="form.logoUrl && !logoBroken" :src="form.logoUrl" alt="" class="h-full w-full object-contain" @error="logoBroken = true" />
            <span v-else class="text-xs text-slate-400">{{ $t("companySettings.noLogo") }}</span>
          </div>
          <div class="flex-1">
            <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("companySettings.logoUrl") }}</label>
            <input v-model="form.logoUrl" type="url" maxlength="1000" placeholder="https://..." class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
            <p v-if="logoBroken" class="mt-1 text-xs text-rose-600">{{ $t("companySettings.logoBroken") }}</p>
          </div>
        </div>
      </AppCard>

      <AppCard :title="$t('companySettings.privacyTitle')" :description="$t('companySettings.privacyDescription')">
        <div class="space-y-5">
          <AppToggle
            v-model="form.showContactInfo"
            :label="$t('companySettings.showContactInfo')"
            :description="$t('companySettings.showContactInfoHint')"
          />
          <div class="rounded-lg bg-slate-50 px-3 py-2 text-xs text-slate-500">
            {{ $t("companySettings.preview") }}:
            <span class="font-mono text-slate-700">{{ form.showContactInfo ? "test@ornekfirma.com · +90 532 123 45 67" : "te**@***.com · +90*******" }}</span>
          </div>
          <AppToggle
            v-model="form.listTendersForPlus"
            :label="$t('companySettings.listTendersForPlus')"
            :description="$t('companySettings.listTendersForPlusHint')"
          />
        </div>
      </AppCard>

      <div class="flex justify-end">
        <AppButton :loading="saving" :disabled="!dirty" @click="save">{{ $t("common.save") }}</AppButton>
      </div>
    </template>
  </div>
</template>
