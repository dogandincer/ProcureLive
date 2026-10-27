<script setup lang="ts">
definePageMeta({ layout: false });

const { login, homePathForRole } = useAuth();
const toast = useToast();
const { t } = useI18n();
// Varsayılan firmanın markası: logo + "{Kısa Ad} e-İhale Yönetim Sistemi" (yoksa "Kuzey e-İhale Yönetimi").
const { branding, logoSrc, initials, load: loadBranding } = useBranding();
onMounted(() => loadBranding());

const email = ref("admin@procurelive.local");
const password = ref("");
const loading = ref(false);

async function submit() {
  loading.value = true;

  try {
    await login(email.value, password.value);
    await navigateTo(homePathForRole());
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("login.defaultError")));
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="relative flex min-h-screen items-center justify-center bg-slate-100">
    <LanguageSwitcher class="absolute right-4 top-4 sm:right-6 sm:top-6" />

    <form class="w-full max-w-sm rounded-xl border border-slate-200 bg-white p-8 shadow-sm" @submit.prevent="submit">
      <div class="flex items-center gap-3">
        <div class="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-indigo-600 text-base font-bold text-white">
          <img v-if="logoSrc" :src="logoSrc" alt="" class="h-full w-full bg-white object-contain" />
          <template v-else>{{ initials }}</template>
        </div>
        <div class="min-w-0">
          <h1 class="text-lg font-bold leading-tight text-indigo-700">{{ branding.appName }}</h1>
          <p class="mt-0.5 text-xs text-slate-500">{{ $t("app.tagline") }}</p>
        </div>
      </div>

      <div class="mt-6 space-y-4">
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("login.email") }}</label>
          <input
            v-model="email"
            type="email"
            required
            class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
          />
        </div>

        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("login.password") }}</label>
          <input
            v-model="password"
            type="password"
            required
            class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
          />
        </div>

        <AppButton type="submit" class="w-full" :loading="loading">
          {{ loading ? $t("login.submitting") : $t("login.submit") }}
        </AppButton>
      </div>
    </form>
  </div>
</template>
