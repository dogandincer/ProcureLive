<script setup lang="ts">
definePageMeta({ layout: false });

/** E-postadaki bağlantıdan gelinir (?email=&token=). Yeni şifre kaydedilince girişe yönlenir; kilitli hesap da açılır. */
const route = useRoute();
const api = useApi();
const { t } = useI18n();
const { branding, logoSrc, initials, load: loadBranding } = useBranding();
onMounted(() => loadBranding());

const email = computed(() => String(route.query.email ?? ""));
const token = computed(() => String(route.query.token ?? ""));
const password = ref("");
const confirm = ref("");
const loading = ref(false);
const done = ref(false);
const errorMessage = ref("");

async function submit() {
  errorMessage.value = "";
  if (password.value !== confirm.value) {
    errorMessage.value = t("passwordReset.mismatch");
    return;
  }
  loading.value = true;
  try {
    await api.post("/api/auth/reset-password", { email: email.value, token: token.value, newPassword: password.value });
    done.value = true;
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, t("passwordReset.resetError"));
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="relative flex min-h-screen items-center justify-center bg-slate-100 px-4">
    <LanguageSwitcher class="absolute right-4 top-4 sm:right-6 sm:top-6" />
    <form class="w-full max-w-sm rounded-xl border border-slate-200 bg-surface p-8 shadow-sm" @submit.prevent="submit">
      <div class="flex items-center gap-3">
        <div class="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-indigo-600 text-base font-bold text-white">
          <img v-if="logoSrc" :src="logoSrc" alt="" class="h-full w-full bg-white object-contain" />
          <template v-else>{{ initials }}</template>
        </div>
        <div class="min-w-0">
          <h1 class="text-lg font-bold leading-tight text-indigo-700">{{ branding.appName }}</h1>
          <p class="mt-0.5 text-xs text-slate-500">{{ $t("passwordReset.resetTitle") }}</p>
        </div>
      </div>

      <div v-if="!email || !token" class="mt-6 rounded-md border border-amber-200 bg-amber-50 px-3 py-3 text-sm text-amber-800">
        {{ $t("passwordReset.badLink") }}
      </div>
      <div v-else-if="done" class="mt-6 space-y-3" role="status" data-testid="reset-done">
        <p class="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-3 text-sm text-emerald-800">{{ $t("passwordReset.done") }}</p>
        <NuxtLink to="/login" class="block w-full rounded-md bg-indigo-600 px-4 py-2 text-center text-sm font-semibold text-white hover:bg-indigo-700">{{ $t("login.submit") }}</NuxtLink>
      </div>
      <div v-else class="mt-6 space-y-4">
        <p class="text-sm text-slate-600">{{ $t("passwordReset.resetIntro", { email }) }}</p>
        <div>
          <label for="reset-password" class="mb-1 block text-sm font-medium text-slate-700">{{ $t("passwordReset.newPassword") }}</label>
          <input id="reset-password" v-model="password" type="password" required minlength="8" autocomplete="new-password" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
          <p class="mt-1 text-[11px] text-slate-400">{{ $t("passwordReset.rules") }}</p>
        </div>
        <div>
          <label for="reset-confirm" class="mb-1 block text-sm font-medium text-slate-700">{{ $t("passwordReset.confirm") }}</label>
          <input id="reset-confirm" v-model="confirm" type="password" required minlength="8" autocomplete="new-password" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
        </div>
        <p v-if="errorMessage" role="alert" class="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700" data-testid="reset-error">{{ errorMessage }}</p>
        <AppButton type="submit" class="w-full" :loading="loading">{{ $t("passwordReset.save") }}</AppButton>
      </div>
      <p class="mt-5 border-t border-slate-100 pt-4 text-center text-xs">
        <NuxtLink to="/login" class="font-semibold text-indigo-600 hover:underline">← {{ $t("passwordReset.backToLogin") }}</NuxtLink>
      </p>
    </form>
  </div>
</template>
