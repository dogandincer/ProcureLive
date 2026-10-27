<script setup lang="ts">
import type { AuthResult } from "~/types/api";

/**
 * Davetsiz tedarikçi kaydı. Cari "Onay Bekliyor" olarak, hiçbir işyerine bağlı olmadan açılır; katılmak istenen
 * ihalenin firması/sorumlu satınalmacısı katılım talebi üzerinden cari onayı verir.
 */
definePageMeta({ layout: false });

const config = useRuntimeConfig();
const toast = useToast();
const { t, locale } = useI18n();
const { setSession } = useAuth();
const { branding, logoSrc, initials, load: loadBranding } = useBranding();
onMounted(() => loadBranding());

const form = reactive({
  companyName: "", taxNumber: "", taxOffice: "", phone: "",
  firstName: "", lastName: "", email: "", password: "", passwordConfirm: "", kvkk: false,
});
const busy = ref(false);
const formError = ref("");

async function submit() {
  formError.value = "";
  if (form.password !== form.passwordConfirm) {
    formError.value = t("invite.register.passwordMismatch");
    return;
  }
  if (!form.kvkk) {
    formError.value = t("invite.register.kvkkRequired");
    return;
  }
  busy.value = true;
  try {
    // Anonim uç: oturumsuz çağrıldığı için düz $fetch (401 yönlendirmesi yok).
    const result = await $fetch<AuthResult>("/api/auth/register-supplier", {
      baseURL: config.public.apiBaseUrl,
      method: "POST",
      headers: { "Accept-Language": locale.value },
      body: {
        companyName: form.companyName,
        taxNumber: form.taxNumber.trim(),
        taxOffice: form.taxOffice,
        phone: form.phone || null,
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        password: form.password,
        acceptTerms: form.kvkk,
      },
    });
    setSession(result);
    toast.success(t("register.successToast"));
    await navigateTo("/supplier");
  } catch (error: any) {
    formError.value = apiErrorMessage(error, t("register.error"));
  } finally {
    busy.value = false;
  }
}

const inputClass = "w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none";
</script>

<template>
  <div class="relative flex min-h-screen items-center justify-center bg-slate-100 px-4 py-10">
    <LanguageSwitcher class="absolute right-4 top-4 sm:right-6 sm:top-6" />

    <form class="w-full max-w-lg rounded-xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8" @submit.prevent="submit">
      <div class="flex items-center gap-3">
        <div class="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-indigo-600 text-base font-bold text-white">
          <img v-if="logoSrc" :src="logoSrc" alt="" class="h-full w-full bg-white object-contain" />
          <template v-else>{{ initials }}</template>
        </div>
        <div class="min-w-0">
          <h1 class="text-lg font-bold leading-tight text-indigo-700">{{ branding.appName }}</h1>
          <p class="mt-0.5 text-xs text-slate-500">{{ $t("register.title") }}</p>
        </div>
      </div>

      <p class="mt-4 rounded-lg bg-indigo-50 px-3 py-2 text-xs text-indigo-800">{{ $t("register.intro") }}</p>

      <h2 class="mt-5 text-xs font-semibold uppercase tracking-wide text-slate-400">{{ $t("register.companySection") }}</h2>
      <div class="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-2">
        <input v-model="form.companyName" required :placeholder="$t('register.companyName')" :class="[inputClass, 'sm:col-span-2']" />
        <input v-model="form.taxNumber" required inputmode="numeric" pattern="[0-9]{10,11}" :title="$t('register.taxNumberHint')" :placeholder="$t('register.taxNumber')" :class="inputClass" />
        <input v-model="form.taxOffice" required :placeholder="$t('register.taxOffice')" :class="inputClass" />
        <input v-model="form.phone" :placeholder="$t('register.phone')" :class="[inputClass, 'sm:col-span-2']" />
      </div>

      <h2 class="mt-5 text-xs font-semibold uppercase tracking-wide text-slate-400">{{ $t("register.userSection") }}</h2>
      <div class="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-2">
        <input v-model="form.firstName" required :placeholder="$t('invite.register.firstName')" :class="inputClass" />
        <input v-model="form.lastName" required :placeholder="$t('invite.register.lastName')" :class="inputClass" />
        <input v-model="form.email" type="email" required autocomplete="email" :placeholder="$t('login.email')" :class="[inputClass, 'sm:col-span-2']" />
        <input v-model="form.password" type="password" required minlength="8" autocomplete="new-password" :placeholder="$t('login.password')" :class="inputClass" />
        <input v-model="form.passwordConfirm" type="password" required minlength="8" autocomplete="new-password" :placeholder="$t('invite.register.passwordConfirm')" :class="inputClass" />
      </div>

      <label class="mt-4 flex items-start gap-2 text-xs text-slate-600">
        <input v-model="form.kvkk" type="checkbox" class="mt-0.5 rounded border-slate-300" />
        <span>{{ $t("invite.register.kvkk") }}</span>
      </label>

      <p v-if="formError" class="mt-3 rounded-md bg-rose-50 px-3 py-2 text-xs text-rose-700" role="alert">{{ formError }}</p>

      <AppButton type="submit" class="mt-4 w-full" :loading="busy">{{ $t("register.cta") }}</AppButton>

      <p class="mt-4 text-center text-xs text-slate-500">
        {{ $t("register.haveAccount") }}
        <NuxtLink to="/login" class="font-semibold text-indigo-600 hover:underline">{{ $t("login.submit") }}</NuxtLink>
      </p>
    </form>
  </div>
</template>
