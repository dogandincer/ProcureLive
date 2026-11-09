<script setup lang="ts">
import type { CurrentUser } from "~/types/api";

/** Kullanıcının kendi hesabı: kimlik bilgileri, bildirim tercihi ve şifre değiştirme (mevcut şifre gerekir). */
const { currentUser } = useAuth();
const api = useApi();
const toast = useToast();
const { t } = useI18n();

const roleLabels: Record<string, string> = { Admin: "roles.admin", Buyer: "roles.buyer", Supplier: "roles.supplier" };
const form = reactive({ current: "", next: "", confirm: "" });
const saving = ref(false);
const errorMessage = ref("");

// Bildirimleri e-postayla da alma tercihi (sunucuda kullanıcıya kayıtlı; kapalıysa dağıtıcı bu kişiyi atlar).
const emailNotifications = ref(currentUser.value?.emailNotificationsEnabled ?? true);
const savingPreference = ref(false);
watch(() => currentUser.value?.emailNotificationsEnabled, (v) => {
  if (v !== undefined) emailNotifications.value = v;
});
watch(emailNotifications, async (value, previous) => {
  if (savingPreference.value || value === (currentUser.value?.emailNotificationsEnabled ?? true)) return;
  savingPreference.value = true;
  try {
    await api.put("/api/auth/me/preferences", { emailNotificationsEnabled: value });
    if (currentUser.value) currentUser.value.emailNotificationsEnabled = value;
    toast.success(t("profile.preferencesSaved"));
  } catch (error: any) {
    emailNotifications.value = previous;
    toast.error(apiErrorMessage(error, t("profile.preferencesError")));
  } finally {
    savingPreference.value = false;
  }
});

// Girişte oturum bilgisi tercihi taşımaz; ekranda sunucudaki güncel değer gösterilsin.
onMounted(async () => {
  try {
    const me = await api.get<CurrentUser>("/api/auth/me");
    if (currentUser.value) currentUser.value.emailNotificationsEnabled = me.emailNotificationsEnabled ?? true;
    emailNotifications.value = me.emailNotificationsEnabled ?? true;
  } catch {
    // Tercih okunamazsa varsayılan (açık) gösterilir; kaydetme yine sunucuya gider.
  }
});

async function changePassword() {
  errorMessage.value = "";
  if (form.next !== form.confirm) {
    errorMessage.value = t("passwordReset.mismatch");
    return;
  }
  saving.value = true;
  try {
    await api.post("/api/auth/change-password", { currentPassword: form.current, newPassword: form.next });
    Object.assign(form, { current: "", next: "", confirm: "" });
    toast.success(t("profile.passwordChanged"));
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, t("profile.passwordError"));
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-6">
    <h1 class="text-xl font-bold text-slate-800">{{ $t("profile.title") }}</h1>

    <AppCard :title="$t('profile.account')">
      <dl v-if="currentUser" class="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
        <div><dt class="text-xs text-slate-500">{{ $t("profile.name") }}</dt><dd class="font-medium text-slate-800">{{ currentUser.firstName }} {{ currentUser.lastName }}</dd></div>
        <div><dt class="text-xs text-slate-500">{{ $t("login.email") }}</dt><dd class="font-medium text-slate-800">{{ currentUser.email }}</dd></div>
        <div><dt class="text-xs text-slate-500">{{ $t("profile.role") }}</dt><dd class="font-medium text-slate-800">{{ currentUser.roles.map((r) => $t(roleLabels[r] ?? r)).join(", ") }}</dd></div>
      </dl>
      <p class="mt-3 text-xs text-slate-400">{{ $t("profile.nameHint") }}</p>
    </AppCard>

    <AppCard :title="$t('profile.notifications')">
      <AppToggle
        v-model="emailNotifications"
        :label="$t('profile.emailNotifications')"
        :description="$t('profile.emailNotificationsHint')"
        :disabled="savingPreference"
        data-testid="email-notifications-toggle"
      />
    </AppCard>

    <AppCard :title="$t('profile.changePassword')">
      <form class="grid grid-cols-1 gap-3 sm:max-w-sm" data-testid="change-password" @submit.prevent="changePassword">
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("profile.currentPassword") }}
          <input v-model="form.current" type="password" required autocomplete="current-password" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
        </label>
        <div class="flex flex-col gap-1">
          <label for="profile-new-password" class="text-xs font-medium text-slate-600">{{ $t("passwordReset.newPassword") }}</label>
          <input id="profile-new-password" v-model="form.next" type="password" required minlength="8" autocomplete="new-password" aria-describedby="profile-password-rules" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
          <span id="profile-password-rules" class="text-xs text-slate-400">{{ $t("passwordReset.rules") }}</span>
        </div>
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("passwordReset.confirm") }}
          <input v-model="form.confirm" type="password" required minlength="8" autocomplete="new-password" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
        </label>
        <p v-if="errorMessage" role="alert" class="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700" data-testid="password-error">{{ errorMessage }}</p>
        <div><AppButton type="submit" :loading="saving">{{ $t("profile.savePassword") }}</AppButton></div>
      </form>
    </AppCard>
  </div>
</template>
