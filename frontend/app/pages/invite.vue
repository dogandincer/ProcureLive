<script setup lang="ts">
import type { AuthResult, ClaimInviteResultDto, InviteInfoDto } from "~/types/api";
import { BuildingOffice2Icon, CalendarDaysIcon, ClockIcon, EnvelopeIcon } from "@heroicons/vue/24/outline";

/**
 * Davet linki karşılama ekranı (/invite?token=...). Oturum yoksa: hesabı olan giriş yapar, olmayan
 * token'a bağlı cariye kayıt olur. Oturum varsa: aynı carinin kullanıcısı daveti sahiplenir.
 * Her durumda sonunda davet edilen ihalenin detayına gidilir.
 */
definePageMeta({ layout: false });

const route = useRoute();
const config = useRuntimeConfig();
const toast = useToast();
const { t, locale } = useI18n();
const { formatDateTime } = useDateFormat();
const api = useApi();
const { accessToken, currentUser, login, setSession, clearSession, isSupplierRole, fetchMe, homePathForRole } = useAuth();

const token = computed(() => (typeof route.query.token === "string" ? route.query.token : ""));

const info = ref<InviteInfoDto | null>(null);
const loading = ref(true);
const mode = ref<"login" | "register">("register");
const busy = ref(false);
const formError = ref("");

const loginForm = reactive({ email: "", password: "" });
const registerForm = reactive({ firstName: "", lastName: "", email: "", password: "", passwordConfirm: "", kvkk: false });

const isLoggedIn = computed(() => !!accessToken.value && !!currentUser.value);
const sameCompany = computed(() => !!info.value?.companyId && currentUser.value?.companyId === info.value.companyId);
const emailLocked = computed(() => !!info.value?.email);

async function loadInfo() {
  loading.value = true;
  try {
    if (!token.value) {
      info.value = null;
      return;
    }
    // Anonymous endpoint: plain $fetch so a stale session can never bounce this page to /login on 401.
    info.value = await $fetch<InviteInfoDto>(`/api/invite/${encodeURIComponent(token.value)}`, {
      baseURL: config.public.apiBaseUrl,
      headers: { "Accept-Language": locale.value },
    });
    if (info.value.email) {
      registerForm.email = info.value.email;
      loginForm.email = info.value.email;
    }
    mode.value = info.value.registrationUsed ? "login" : "register";
  } catch {
    info.value = null;
  } finally {
    loading.value = false;
  }
}

async function claimAndGo() {
  busy.value = true;
  formError.value = "";
  try {
    const result = await api.post<ClaimInviteResultDto>(`/api/invite/${encodeURIComponent(token.value)}/claim`);
    toast.success(t("invite.claimedToast"));
    await navigateTo(`/supplier/auctions/${result.auctionId}`);
  } catch (error: any) {
    formError.value = apiErrorMessage(error, t("invite.genericError"));
  } finally {
    busy.value = false;
  }
}

async function submitLogin() {
  busy.value = true;
  formError.value = "";
  try {
    await login(loginForm.email, loginForm.password);
  } catch (error: any) {
    formError.value = apiErrorMessage(error, t("login.defaultError"));
    busy.value = false;
    return;
  }
  busy.value = false;

  // Logged in: claim only when this account belongs to the invited company; otherwise the
  // mismatch panel below explains what to do (no silent redirect to someone else's panel).
  if (isSupplierRole() && sameCompany.value) {
    await claimAndGo();
  }
}

async function submitRegister() {
  formError.value = "";
  if (registerForm.password !== registerForm.passwordConfirm) {
    formError.value = t("invite.register.passwordMismatch");
    return;
  }
  if (!registerForm.kvkk) {
    formError.value = t("invite.register.kvkkRequired");
    return;
  }

  busy.value = true;
  try {
    const result = await $fetch<AuthResult>(`/api/invite/${encodeURIComponent(token.value)}/register`, {
      baseURL: config.public.apiBaseUrl,
      method: "POST",
      headers: { "Accept-Language": locale.value },
      body: {
        email: registerForm.email,
        password: registerForm.password,
        firstName: registerForm.firstName,
        lastName: registerForm.lastName,
      },
    });
    setSession(result);
    toast.success(t("invite.register.successToast"));
    await navigateTo(`/supplier/auctions/${info.value!.auctionId}`);
  } catch (error: any) {
    const code = apiErrorCode(error);
    if (code === "AccountExists" || code === "InviteUsed") {
      loginForm.email = registerForm.email;
      mode.value = "login";
    }
    formError.value = apiErrorMessage(error, t("invite.genericError"));
  } finally {
    busy.value = false;
  }
}

function switchAccount() {
  clearSession();
  formError.value = "";
  mode.value = "login";
}

onMounted(async () => {
  if (accessToken.value && !currentUser.value) await fetchMe();
  await loadInfo();
});
</script>

<template>
  <div class="relative flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-100 via-white to-indigo-50 px-4 pb-10 pt-16 sm:py-10">
    <LanguageSwitcher class="absolute right-4 top-4 sm:right-6 sm:top-6" />

    <LoadingSpinner v-if="loading" />

    <!-- Geçersiz / süresi dolmuş / kapanmış ihale -->
    <div v-else-if="!info || !info.isValid" class="w-full max-w-md rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm">
      <h1 class="text-xl font-bold text-indigo-700">{{ $t("app.name") }}</h1>
      <div class="mx-auto mt-6 flex h-12 w-12 items-center justify-center rounded-full bg-rose-100 text-2xl">⚠️</div>
      <h2 class="mt-3 text-lg font-semibold text-slate-800">{{ $t(`invite.invalid.${info?.invalidReason ?? "InviteInvalid"}.title`) }}</h2>
      <p class="mt-1 text-sm text-slate-500">{{ $t(`invite.invalid.${info?.invalidReason ?? "InviteInvalid"}.body`) }}</p>
      <NuxtLink :to="isLoggedIn ? homePathForRole() : '/login'" class="mt-6 inline-block text-sm font-semibold text-indigo-600 hover:underline">
        {{ isLoggedIn ? $t("invite.goToPanel") : $t("invite.goToLogin") }}
      </NuxtLink>
    </div>

    <div v-else class="grid w-full max-w-4xl overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl md:grid-cols-5">
      <!-- Sol: davet özeti -->
      <section class="bg-gradient-to-b from-indigo-700 to-indigo-900 p-8 text-white md:col-span-2">
        <div class="flex items-center gap-2.5">
          <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-white/15 text-sm font-bold">PL</div>
          <div class="text-sm font-semibold">{{ $t("app.name") }}</div>
        </div>

        <h1 class="mt-8 text-2xl font-bold leading-tight">{{ $t("invite.welcomeTitle") }}</h1>
        <p class="mt-2 text-sm text-indigo-100">
          {{ $t("invite.welcomeBody", { owner: info.ownerCompanyName }) }}
        </p>

        <div class="mt-6 rounded-xl bg-white/10 p-4">
          <div class="text-xs uppercase tracking-wide text-indigo-200">{{ $t("invite.tender") }}</div>
          <div class="mt-1 text-base font-semibold">{{ info.auctionTitle }}</div>
          <div v-if="info.referenceCode" class="mt-0.5 font-mono text-xs text-indigo-200">{{ info.referenceCode }}</div>
        </div>

        <ul class="mt-6 space-y-3 text-sm text-indigo-100">
          <li class="flex items-center gap-2">
            <BuildingOffice2Icon class="h-4 w-4 shrink-0" />
            <span>{{ $t("invite.invitedCompany") }}: <strong class="text-white">{{ info.companyName }}</strong></span>
          </li>
          <li v-if="info.auctionEndAt" class="flex items-center gap-2">
            <CalendarDaysIcon class="h-4 w-4 shrink-0" />
            <span>{{ $t("invite.tenderEnds") }}: {{ formatDateTime(info.auctionEndAt) }}</span>
          </li>
          <li v-if="info.expiresAt" class="flex items-center gap-2">
            <ClockIcon class="h-4 w-4 shrink-0" />
            <span>{{ $t("invite.linkExpires") }}: {{ formatDateTime(info.expiresAt) }}</span>
          </li>
          <li v-if="info.email" class="flex items-center gap-2">
            <EnvelopeIcon class="h-4 w-4 shrink-0" />
            <span>{{ $t("invite.lockedTo") }}: {{ info.email }}</span>
          </li>
        </ul>
      </section>

      <!-- Sağ: giriş / kayıt / sahiplenme -->
      <section class="p-8 md:col-span-3">
        <!-- Oturum açık -->
        <template v-if="isLoggedIn">
          <template v-if="isSupplierRole() && sameCompany">
            <h2 class="text-lg font-semibold text-slate-800">{{ $t("invite.claim.title", { name: currentUser?.firstName }) }}</h2>
            <p class="mt-1 text-sm text-slate-500">{{ $t("invite.claim.body") }}</p>
            <AppButton class="mt-6 w-full" :loading="busy" @click="claimAndGo">{{ $t("invite.claim.cta") }}</AppButton>
          </template>
          <template v-else>
            <h2 class="text-lg font-semibold text-slate-800">{{ $t("invite.mismatch.title") }}</h2>
            <p class="mt-1 text-sm text-slate-500">
              {{ $t("invite.mismatch.body", { email: currentUser?.email, company: info.companyName }) }}
            </p>
            <div class="mt-6 flex flex-wrap gap-2">
              <AppButton @click="switchAccount">{{ $t("invite.mismatch.switch") }}</AppButton>
              <NuxtLink :to="homePathForRole()" class="inline-flex items-center rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">
                {{ $t("invite.goToPanel") }}
              </NuxtLink>
            </div>
          </template>
        </template>

        <!-- Oturum yok -->
        <template v-else>
          <div class="flex rounded-lg bg-slate-100 p-1 text-sm font-medium">
            <button
              type="button"
              class="flex-1 rounded-md px-3 py-1.5 transition-colors"
              :class="mode === 'register' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'"
              :disabled="info.registrationUsed"
              @click="mode = 'register'; formError = ''"
            >
              {{ $t("invite.tabs.register") }}
            </button>
            <button
              type="button"
              class="flex-1 rounded-md px-3 py-1.5 transition-colors"
              :class="mode === 'login' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'"
              @click="mode = 'login'; formError = ''"
            >
              {{ $t("invite.tabs.login") }}
            </button>
          </div>
          <p v-if="info.registrationUsed" class="mt-3 text-xs text-amber-700">{{ $t("invite.registrationUsed") }}</p>

          <form v-if="mode === 'login'" class="mt-6 space-y-4" @submit.prevent="submitLogin">
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("login.email") }}</label>
              <input v-model="loginForm.email" type="email" required autocomplete="email" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("login.password") }}</label>
              <input v-model="loginForm.password" type="password" required autocomplete="current-password" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <p v-if="formError" class="rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">{{ formError }}</p>
            <AppButton type="submit" class="w-full" :loading="busy">{{ $t("invite.loginCta") }}</AppButton>
          </form>

          <form v-else class="mt-6 space-y-4" @submit.prevent="submitRegister">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("invite.register.firstName") }}</label>
                <input v-model="registerForm.firstName" required maxlength="100" autocomplete="given-name" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("invite.register.lastName") }}</label>
                <input v-model="registerForm.lastName" required maxlength="100" autocomplete="family-name" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
              </div>
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("login.email") }}</label>
              <input
                v-model="registerForm.email"
                type="email"
                required
                autocomplete="email"
                :readonly="emailLocked"
                class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none read-only:bg-slate-50 read-only:text-slate-500"
              />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("login.password") }}</label>
                <input v-model="registerForm.password" type="password" required minlength="8" autocomplete="new-password" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("invite.register.passwordConfirm") }}</label>
                <input v-model="registerForm.passwordConfirm" type="password" required minlength="8" autocomplete="new-password" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
              </div>
            </div>
            <p class="text-xs text-slate-400">{{ $t("invite.register.companyNote", { company: info.companyName }) }}</p>
            <label class="flex items-start gap-2 text-xs text-slate-600">
              <input v-model="registerForm.kvkk" type="checkbox" class="mt-0.5 rounded border-slate-300" />
              <span>{{ $t("invite.register.kvkk") }}</span>
            </label>
            <p v-if="formError" class="rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">{{ formError }}</p>
            <AppButton type="submit" class="w-full" :loading="busy">{{ $t("invite.register.cta") }}</AppButton>
          </form>
        </template>
      </section>
    </div>
  </div>
</template>
