<script setup lang="ts">
import type { PagedResult, UserDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";
import { LockClosedIcon, ShieldCheckIcon, UserPlusIcon } from "@heroicons/vue/24/outline";

/**
 * Kullanıcı Tanım (yalnızca Admin): şirket içi hesaplar — Admin ve Satınalmacı. Tedarikçi hesapları Cari ekranından
 * yönetilir. Art arda 3'ten fazla hatalı şifre giren hesap otomatik pasife alınır; Admin buradan yeniden açar.
 */
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();
const { currentUser } = useAuth();

const users = ref<UserDto[]>([]);
const loading = ref(true);
const showForm = ref(false);
const submitting = ref(false);
const roleDrafts = reactive<Record<string, string>>({});
const busy = reactive<Record<string, boolean>>({});

const roles = ["Buyer", "Admin"];
const roleLabelKeys: Record<string, string> = { Admin: "roles.admin", Buyer: "roles.buyer", Supplier: "roles.supplier" };
const roleLabel = (r: string) => t(roleLabelKeys[r] ?? r);
const emptyForm = () => ({ firstName: "", lastName: "", email: "", password: "", role: "Buyer" });
const form = reactive(emptyForm());

const importColumns = computed<ExcelColumn[]>(() => [
  { key: "email", label: t("users.import.email"), required: true },
  { key: "password", label: t("users.import.password"), required: true, hint: t("users.import.passwordHint") },
  { key: "firstName", label: t("users.import.firstName"), required: true },
  { key: "lastName", label: t("users.import.lastName"), required: true },
  { key: "role", label: t("users.import.role"), required: true, hint: roles.join("/") },
]);

async function importUserRow(row: Record<string, string | number | boolean | null>) {
  const role = String(row.role ?? "").trim();
  if (!roles.includes(role)) throw { data: { title: t("users.import.internalRoleOnly", { roles: roles.join("/") }) } };
  await api.post("/api/auth/register", {
    email: row.email, password: row.password, firstName: row.firstName, lastName: row.lastName, role, companyId: null,
  });
}

const { page: listPage, total: listTotal, search: listSearch, pageSize: listPageSize, params: listParams, resetAndLoad } = usePagedList(() => load());

async function load() {
  loading.value = true;
  try {
    const page = await api.get<PagedResult<UserDto>>("/api/users", { kind: "internal", ...listParams("searchTerm") });
    users.value = page.items;
    listTotal.value = page.totalCount;
    for (const u of users.value) roleDrafts[u.id] = u.roles.includes("Admin") ? "Admin" : "Buyer";
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("users.loadError")));
  } finally {
    loading.value = false;
  }
}

async function create() {
  submitting.value = true;
  try {
    await api.post("/api/auth/register", { ...form, companyId: null });
    Object.assign(form, emptyForm());
    showForm.value = false;
    toast.success(t("toast.userCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("users.createError")));
  } finally {
    submitting.value = false;
  }
}

async function run(u: UserDto, action: () => Promise<unknown>, success: string, failure: string) {
  busy[u.id] = true;
  try {
    await action();
    toast.success(success);
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, failure));
  } finally {
    busy[u.id] = false;
  }
}

const saveRole = (u: UserDto) =>
  run(u, () => api.post(`/api/users/${u.id}/roles`, { roles: [roleDrafts[u.id]] }), t("toast.userRoleUpdated"), t("users.roleUpdateError"));
const deactivate = (u: UserDto) =>
  run(u, () => api.post(`/api/users/${u.id}/deactivate`), t("toast.userDeactivated"), t("users.deactivateError"));
const activate = (u: UserDto) =>
  run(u, () => api.post(`/api/users/${u.id}/activate`), u.lockedOutAt ? t("users.unlocked") : t("toast.userActivated"), t("users.activateError"));

const isSelf = (u: UserDto) => u.id === currentUser.value?.userId;
const currentRole = (u: UserDto) => (u.roles.includes("Admin") ? "Admin" : "Buyer");

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("users.title") }}</h1>
        <p class="text-sm text-slate-500">{{ $t("users.subtitle") }}</p>
      </div>
      <div class="flex gap-2">
        <ExcelImportButton :columns="importColumns" template-filename="kullanici-tanim-sablon.xlsx" :import-row="importUserRow" @imported="load" />
        <AppButton @click="showForm = !showForm">
          <UserPlusIcon class="h-4 w-4" />{{ showForm ? $t("common.cancel") : $t("users.new") }}
        </AppButton>
      </div>
    </div>

    <AppCard v-if="showForm" :title="$t('users.new')">
      <form class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5" @submit.prevent="create">
        <div>
          <label for="user-firstName" class="mb-1 block text-xs font-medium text-slate-600">{{ $t("users.form.firstName") }} *</label>
          <input id="user-firstName" v-model="form.firstName" required maxlength="100" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" />
        </div>
        <div>
          <label for="user-lastName" class="mb-1 block text-xs font-medium text-slate-600">{{ $t("users.form.lastName") }} *</label>
          <input id="user-lastName" v-model="form.lastName" required maxlength="100" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" />
        </div>
        <div>
          <label for="user-email" class="mb-1 block text-xs font-medium text-slate-600">{{ $t("users.form.email") }} *</label>
          <input id="user-email" v-model="form.email" type="email" required autocomplete="off" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" />
        </div>
        <div>
          <label for="user-password" class="mb-1 block text-xs font-medium text-slate-600">{{ $t("users.form.password") }} *</label>
          <input id="user-password" v-model="form.password" type="password" required minlength="8" autocomplete="new-password" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" />
        </div>
        <div>
          <label for="user-role" class="mb-1 block text-xs font-medium text-slate-600">{{ $t("users.table.role") }} *</label>
          <select id="user-role" v-model="form.role" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm">
            <option v-for="r in roles" :key="r" :value="r">{{ roleLabel(r) }}</option>
          </select>
        </div>
        <p class="text-xs text-slate-400 sm:col-span-2 lg:col-span-4">{{ $t("users.form.buyerAccessHint") }}</p>
        <div class="flex justify-end">
          <AppButton type="submit" :loading="submitting">{{ $t("common.save") }}</AppButton>
        </div>
      </form>
    </AppCard>

    <input v-model="listSearch" type="search" :placeholder="$t('pager.search.users')" :aria-label="$t('pager.search.users')"
      class="w-full max-w-sm rounded-md border border-slate-300 px-3 py-1.5 text-sm" data-testid="list-search" />
    <AppCard no-padding>
      <LoadingSpinner v-if="loading" />
      <EmptyState v-else-if="users.length === 0">{{ $t("users.empty") }}</EmptyState>
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="border-b border-slate-100 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th class="px-4 py-3">{{ $t("users.table.name") }}</th>
              <th class="px-4 py-3">{{ $t("users.table.role") }}</th>
              <th class="px-4 py-3">{{ $t("common.status") }}</th>
              <th class="px-4 py-3">{{ $t("users.table.lastLogin") }}</th>
              <th class="px-4 py-3 text-right">{{ $t("common.actions") }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-50">
            <tr v-for="u in users" :key="u.id" class="hover:bg-slate-50">
              <td class="px-4 py-3">
                <div class="font-medium text-slate-800">{{ u.firstName }} {{ u.lastName }}</div>
                <div class="text-xs text-slate-400">{{ u.email }}</div>
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <select v-model="roleDrafts[u.id]" :aria-label="`${$t('users.table.role')} — ${u.email}`" :disabled="isSelf(u)" class="rounded-md border border-slate-300 px-2 py-1 text-xs disabled:bg-slate-50">
                    <option v-for="r in roles" :key="r" :value="r">{{ roleLabel(r) }}</option>
                  </select>
                  <button
                    v-if="roleDrafts[u.id] !== currentRole(u)"
                    class="text-xs font-semibold text-indigo-600 hover:underline"
                    :disabled="busy[u.id]"
                    @click="saveRole(u)"
                  >
                    {{ $t("common.save") }}
                  </button>
                </div>
              </td>
              <td class="px-4 py-3">
                <span v-if="u.lockedOutAt" class="inline-flex items-center gap-1 rounded-full bg-rose-100 px-2 py-0.5 text-xs font-medium text-rose-700" :title="$t('users.lockedHint', { date: formatDateTime(u.lockedOutAt) })">
                  <LockClosedIcon class="h-3 w-3" /> {{ $t("users.locked") }}
                </span>
                <AppBadge v-else :tone="u.isActive ? 'emerald' : 'slate'">{{ u.isActive ? $t("common.active") : $t("common.passive") }}</AppBadge>
                <div v-if="u.lockedOutAt" class="mt-0.5 text-[11px] text-rose-500">{{ formatDateTime(u.lockedOutAt) }}</div>
              </td>
              <td class="px-4 py-3 text-xs text-slate-500">{{ u.lastLoginAt ? formatDateTime(u.lastLoginAt) : "—" }}</td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-3 text-xs font-semibold">
                  <NuxtLink v-if="currentRole(u) === 'Buyer'" :to="`/admin/permissions?user=${u.id}`" class="inline-flex items-center gap-1 text-indigo-600 hover:underline">
                    <ShieldCheckIcon class="h-3.5 w-3.5" /> {{ $t("users.permissions") }}
                  </NuxtLink>
                  <button v-if="!u.isActive" class="text-emerald-600 hover:underline" :disabled="busy[u.id]" @click="activate(u)">
                    {{ u.lockedOutAt ? $t("users.unlock") : $t("users.activate") }}
                  </button>
                  <button v-else-if="!isSelf(u)" class="text-rose-600 hover:underline" :disabled="busy[u.id]" @click="deactivate(u)">
                    {{ $t("users.deactivate") }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </AppCard>
    <ListPager v-model:page="listPage" :total="listTotal" :page-size="listPageSize" />
  </div>
</template>
