<script setup lang="ts">
import type { UserDto } from "~/types/api";
import { UserPlusIcon } from "@heroicons/vue/24/outline";

/** Tedarikçi carinin giriş kullanıcıları (kullanıcı adı = e-posta). */
const props = defineProps<{ companyId: string }>();
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const users = ref<UserDto[]>([]);
const loading = ref(true);
const showForm = ref(false);
const saving = ref(false);
const form = reactive({ email: "", password: "", firstName: "", lastName: "" });

async function load() {
  loading.value = true;
  try {
    users.value = await api.get<UserDto[]>(`/api/companies/${props.companyId}/users`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.users.loadError")));
  } finally {
    loading.value = false;
  }
}

async function create() {
  saving.value = true;
  try {
    await api.post(`/api/companies/${props.companyId}/users`, { ...form });
    Object.assign(form, { email: "", password: "", firstName: "", lastName: "" });
    showForm.value = false;
    toast.success(t("companies.users.created"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.users.createError")));
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<template>
  <AppCard>
    <div class="flex items-center justify-between">
      <h2 class="text-sm font-semibold text-slate-700">{{ $t("companies.users.title") }}</h2>
      <button class="flex items-center gap-1 text-xs font-medium text-indigo-600 hover:underline" @click="showForm = !showForm">
        <UserPlusIcon v-if="!showForm" class="h-3.5 w-3.5" />{{ showForm ? $t("common.cancel") : $t("companies.users.add") }}
      </button>
    </div>

    <form v-if="showForm" class="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.email" type="email" required autocomplete="off" :placeholder="$t('companies.form.userEmail')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
      <input v-model="form.password" type="password" required minlength="8" autocomplete="new-password" :placeholder="$t('companies.form.userPassword')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
      <input v-model="form.firstName" required :placeholder="$t('companies.form.userFirstName')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
      <input v-model="form.lastName" required :placeholder="$t('companies.form.userLastName')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
      <button type="submit" :disabled="saving" class="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50 sm:col-span-2">
        {{ saving ? $t("common.saving") : $t("common.save") }}
      </button>
    </form>

    <LoadingSpinner v-if="loading" />
    <ul v-else-if="users.length" class="mt-3 divide-y divide-slate-100 text-sm">
      <li v-for="u in users" :key="u.id" class="flex flex-wrap items-center justify-between gap-2 py-2">
        <div class="min-w-0">
          <div class="font-medium text-slate-700">{{ u.firstName }} {{ u.lastName }}</div>
          <div class="truncate text-xs text-slate-500">{{ u.email }}</div>
        </div>
        <div class="flex items-center gap-2 text-xs text-slate-400">
          <span v-if="u.lastLoginAt">{{ $t("companies.users.lastLogin") }}: {{ formatDateTime(u.lastLoginAt) }}</span>
          <AppBadge :tone="u.isActive ? 'emerald' : 'slate'">{{ u.isActive ? $t("common.active") : $t("common.passive") }}</AppBadge>
        </div>
      </li>
    </ul>
    <EmptyState v-else class="mt-3">{{ $t("companies.users.empty") }}</EmptyState>
  </AppCard>
</template>
