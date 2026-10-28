<script setup lang="ts">
definePageMeta({ layout: false });

/** Şifre sıfırlama bağlantısı isteği. Hesap var/yok her durumda aynı mesaj gösterilir (hesap varlığı ele verilmez). */
const api = useApi();
const { t } = useI18n();
const { branding, logoSrc, initials, load: loadBranding } = useBranding();
onMounted(() => loadBranding());

const email = ref("");
const loading = ref(false);
const sent = ref(false);
const errorMessage = ref("");

async function submit() {
  loading.value = true;
  errorMessage.value = "";
  try {
    await api.post("/api/auth/forgot-password", { email: email.value });
    sent.value = true;
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, t("passwordReset.requestError"));
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
          <p class="mt-0.5 text-xs text-slate-500">{{ $t("passwordReset.forgotTitle") }}</p>
        </div>
      </div>

      <div v-if="sent" class="mt-6 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-3 text-sm text-emerald-800" role="status" data-testid="forgot-sent">
        {{ $t("passwordReset.sent") }}
      </div>
      <div v-else class="mt-6 space-y-4">
        <p class="text-sm text-slate-600">{{ $t("passwordReset.forgotIntro") }}</p>
        <div>
          <label for="forgot-email" class="mb-1 block text-sm font-medium text-slate-700">{{ $t("login.email") }}</label>
          <input id="forgot-email" v-model="email" type="email" required autocomplete="email" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
        </div>
        <p v-if="errorMessage" role="alert" class="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">{{ errorMessage }}</p>
        <AppButton type="submit" class="w-full" :loading="loading">{{ $t("passwordReset.sendLink") }}</AppButton>
      </div>
      <p class="mt-5 border-t border-slate-100 pt-4 text-center text-xs">
        <NuxtLink to="/login" class="font-semibold text-indigo-600 hover:underline">← {{ $t("passwordReset.backToLogin") }}</NuxtLink>
      </p>
    </form>
  </div>
</template>
