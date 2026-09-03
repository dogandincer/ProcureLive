<script setup lang="ts">
import type { CompanyDto, PagedResult, UserDto } from "~/types/api";

const api = useApi();
const { isAdminRole } = useAuth();
const users = ref<UserDto[]>([]);
const companies = ref<CompanyDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const errorMessage = ref("");
const roleDrafts = reactive<Record<string, string>>({});
const companyDrafts = reactive<Record<string, string>>({});

// A Buyer may only create Supplier accounts - the backend enforces this too (RegisterCommandHandler),
// this just keeps a Buyer's own form from offering a role their submission would be rejected for.
const roles = isAdminRole() ? ["Admin", "Buyer", "Supplier"] : ["Supplier"];
const form = reactive({ email: "", password: "", firstName: "", lastName: "", role: roles[0], companyId: "" });

const companyName = (companyId: string | null) => companies.value.find((c) => c.id === companyId)?.name ?? "-";

async function load() {
  const [userPage, companyPage] = await Promise.all([
    api.get<PagedResult<UserDto>>("/api/users", { page: 1, pageSize: 100 }),
    api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100 }),
  ]);
  users.value = userPage.items;
  companies.value = companyPage.items;
  for (const u of users.value) {
    roleDrafts[u.id] = u.roles[0] ?? "Supplier";
    companyDrafts[u.id] = u.companyId ?? "";
  }
}

async function create() {
  errorMessage.value = "";
  submitting.value = true;
  try {
    await api.post("/api/auth/register", {
      email: form.email,
      password: form.password,
      firstName: form.firstName,
      lastName: form.lastName,
      role: form.role,
      companyId: form.role === "Supplier" ? form.companyId || null : null,
    });
    Object.assign(form, { email: "", password: "", firstName: "", lastName: "", role: roles[0], companyId: "" });
    showForm.value = false;
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Kullanıcı oluşturulamadı.");
  } finally {
    submitting.value = false;
  }
}

async function saveRole(user: UserDto) {
  errorMessage.value = "";
  try {
    if (roleDrafts[user.id] === "Supplier" && companyDrafts[user.id]) {
      await api.put(`/api/users/${user.id}`, {
        firstName: user.firstName,
        lastName: user.lastName,
        companyId: companyDrafts[user.id],
      });
    }
    await api.post(`/api/users/${user.id}/roles`, { roles: [roleDrafts[user.id]] });
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Rol güncellenemedi.");
  }
}

async function deactivate(user: UserDto) {
  errorMessage.value = "";
  try {
    await api.post(`/api/users/${user.id}/deactivate`);
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Kullanıcı devre dışı bırakılamadı.");
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">Kullanıcılar</h1>
      <button class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white" @click="showForm = !showForm">
        {{ showForm ? "Vazgeç" : "+ Yeni Kullanıcı" }}
      </button>
    </div>

    <div v-if="errorMessage" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ errorMessage }}</span>
      <button class="font-semibold" @click="errorMessage = ''">Kapat</button>
    </div>

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.email" type="email" placeholder="E-posta" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.password" type="password" placeholder="Şifre" required minlength="8" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.firstName" placeholder="Ad" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.lastName" placeholder="Soyad" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <select v-model="form.role" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="r in roles" :key="r" :value="r">{{ r }}</option>
      </select>
      <select v-if="form.role === 'Supplier'" v-model="form.companyId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option value="" disabled>Cari seç...</option>
        <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <button type="submit" :disabled="submitting" class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white sm:col-span-2 disabled:opacity-50">
        {{ submitting ? "Kaydediliyor..." : "Kaydet" }}
      </button>
    </form>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">E-posta</th>
            <th class="pb-2">Ad Soyad</th>
            <th class="pb-2">Cari</th>
            <th class="pb-2">Rol</th>
            <th class="pb-2">Durum</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in users" :key="u.id" class="border-b border-slate-50">
            <td class="py-2 text-slate-700">{{ u.email }}</td>
            <td class="py-2 font-medium text-slate-700">{{ u.firstName }} {{ u.lastName }}</td>
            <td class="py-2 text-slate-500">{{ companyName(u.companyId) }}</td>
            <td class="py-2">
              <div v-if="isAdminRole()" class="flex flex-wrap items-center gap-2">
                <select v-model="roleDrafts[u.id]" class="rounded-md border border-slate-300 px-2 py-1 text-xs">
                  <option v-for="r in roles" :key="r" :value="r">{{ r }}</option>
                </select>
                <select
                  v-if="roleDrafts[u.id] === 'Supplier'"
                  v-model="companyDrafts[u.id]"
                  class="rounded-md border border-slate-300 px-2 py-1 text-xs"
                >
                  <option value="" disabled>Cari seç...</option>
                  <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
                </select>
                <button
                  v-if="roleDrafts[u.id] !== u.roles[0] || (roleDrafts[u.id] === 'Supplier' && companyDrafts[u.id] !== u.companyId)"
                  class="rounded bg-indigo-600 px-2 py-1 text-xs font-semibold text-white disabled:opacity-40"
                  :disabled="roleDrafts[u.id] === 'Supplier' && !companyDrafts[u.id]"
                  @click="saveRole(u)"
                >
                  Kaydet
                </button>
              </div>
              <span v-else class="text-xs text-slate-500">{{ u.roles.join(", ") }}</span>
            </td>
            <td class="py-2">
              <span class="rounded px-2 py-0.5 text-xs font-medium" :class="u.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'">
                {{ u.isActive ? "Aktif" : "Pasif" }}
              </span>
            </td>
            <td class="py-2 text-right">
              <button v-if="isAdminRole() && u.isActive" class="text-xs font-medium text-rose-600 hover:underline" @click="deactivate(u)">
                Devre Dışı Bırak
              </button>
            </td>
          </tr>
          <tr v-if="users.length === 0"><td colspan="6" class="py-6 text-center text-slate-400">Henüz kullanıcı yok</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
