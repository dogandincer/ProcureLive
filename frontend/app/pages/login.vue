<script setup lang="ts">
definePageMeta({ layout: false });

const { login, homePathForRole } = useAuth();

const email = ref("admin@procurelive.local");
const password = ref("");
const errorMessage = ref("");
const loading = ref(false);

async function submit() {
  errorMessage.value = "";
  loading.value = true;

  try {
    await login(email.value, password.value);
    await navigateTo(homePathForRole());
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Giriş başarısız. E-posta veya şifre hatalı.");
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-slate-100">
    <form class="w-full max-w-sm rounded-xl border border-slate-200 bg-white p-8 shadow-sm" @submit.prevent="submit">
      <h1 class="text-xl font-bold text-indigo-700">ProcureLive</h1>
      <p class="mt-1 text-sm text-slate-500">B2B E-Tedarik ve Anlık İhale Sistemi</p>

      <div class="mt-6 space-y-4">
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">E-posta</label>
          <input
            v-model="email"
            type="email"
            required
            class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
          />
        </div>

        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">Şifre</label>
          <input
            v-model="password"
            type="password"
            required
            class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
          />
        </div>

        <p v-if="errorMessage" class="text-sm text-rose-600">{{ errorMessage }}</p>

        <button
          type="submit"
          :disabled="loading"
          class="w-full rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          {{ loading ? "Giriş yapılıyor..." : "Giriş Yap" }}
        </button>
      </div>
    </form>
  </div>
</template>
