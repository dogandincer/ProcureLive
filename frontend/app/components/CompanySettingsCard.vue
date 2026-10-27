<script setup lang="ts">
import type { CompanySettingsDto } from "~/types/api";
import { SparklesIcon } from "@heroicons/vue/24/solid";

/** Admin: carinin gizlilik (KVKK iletişim izni), Plus listeleme ve logo ayarları. */
const props = defineProps<{ companyId: string }>();

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();

const settings = ref<CompanySettingsDto | null>(null);
const form = reactive({ logoUrl: "", showContactInfo: false, listTendersForPlus: false });
const saving = ref(false);

function fill(s: CompanySettingsDto) {
  settings.value = s;
  form.logoUrl = s.logoUrl ?? "";
  form.showContactInfo = s.showContactInfo;
  form.listTendersForPlus = s.listTendersForPlus;
}

const dirty = computed(
  () =>
    !!settings.value &&
    ((settings.value.logoUrl ?? "") !== form.logoUrl.trim() ||
      settings.value.showContactInfo !== form.showContactInfo ||
      settings.value.listTendersForPlus !== form.listTendersForPlus),
);

async function save() {
  saving.value = true;
  try {
    fill(
      await api.put<CompanySettingsDto>(`/api/companies/${props.companyId}/settings`, {
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
    fill(await api.get<CompanySettingsDto>(`/api/companies/${props.companyId}/settings`));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companySettings.loadError")));
  }
});
</script>

<template>
  <AppCard :title="$t('companySettings.adminTitle')" :description="$t('companySettings.privacyDescription')">
    <template #action>
      <span v-if="settings?.isPlus" class="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-700">
        <SparklesIcon class="h-3.5 w-3.5" /> Plus
        <span v-if="settings.plusExpiresAt" class="font-normal">· {{ formatDate(settings.plusExpiresAt) }}</span>
      </span>
      <AppBadge v-else-if="settings">{{ $t("pricing.standard") }}</AppBadge>
    </template>

    <div v-if="settings" class="grid gap-5 md:grid-cols-2">
      <div class="space-y-4">
        <AppToggle v-model="form.showContactInfo" :label="$t('companySettings.showContactInfo')" :description="$t('companySettings.showContactInfoHint')" />
        <AppToggle v-model="form.listTendersForPlus" :label="$t('companySettings.listTendersForPlus')" :description="$t('companySettings.listTendersForPlusHint')" />
      </div>
      <div>
        <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("companySettings.logoUrl") }}</label>
        <div class="flex items-center gap-3">
          <div class="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-slate-200 bg-slate-50">
            <img v-if="form.logoUrl" :src="form.logoUrl" alt="" class="h-full w-full object-contain" />
          </div>
          <input v-model="form.logoUrl" type="url" maxlength="1000" placeholder="https://..." class="flex-1 rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
        </div>
        <div class="mt-4 flex justify-end">
          <AppButton :loading="saving" :disabled="!dirty" @click="save">{{ $t("common.save") }}</AppButton>
        </div>
      </div>
    </div>
    <LoadingSpinner v-else />
  </AppCard>
</template>
