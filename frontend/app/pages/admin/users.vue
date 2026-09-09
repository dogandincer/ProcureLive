<script setup lang="ts">
import type { CompanyDto, PagedResult, UserDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const api = useApi();
const toast = useToast();
const { isAdminRole } = useAuth();
const users = ref<UserDto[]>([]);
const companies = ref<CompanyDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);
const roleDrafts = reactive<Record<string, string>>({});
const companyDrafts = reactive<Record<string, string>>({});

// A Buyer may only create Supplier accounts - the backend enforces this too (RegisterCommandHandler),
// this just keeps a Buyer's own form from offering a role their submission would be rejected for.
const roles = isAdminRole() ? ["Admin", "Buyer", "Supplier"] : ["Supplier"];
const form = reactive({ email: "", password: "", firstName: "", lastName: "", role: roles[0], companyId: "" });

const companyName = (companyId: string | null) => companies.value.find((c) => c.id === companyId)?.name ?? "-";

const importColumns: ExcelColumn[] = [
  { key: "email", label: "E-posta", required: true },
  { key: "password", label: "Şifre", required: true, hint: "En az 8 karakter" },
  { key: "firstName", label: "Ad", required: true },
  { key: "lastName", label: "Soyad", required: true },
  { key: "role", label: "Rol", required: true, hint: roles.join("/") },
  { key: "company", label: "Cari", hint: "Rol Supplier ise zorunlu - mevcut bir Cari adı" },
];

async function importUserRow(row: Record<string, string | number | boolean | null>) {
  const companyNameValue = String(row.company ?? "").trim();
  let companyId: string | null = null;

  if (companyNameValue) {
    const match = companies.value.find((c) => c.name.toLocaleLowerCase("tr") === companyNameValue.toLocaleLowerCase("tr"));
    if (!match) {
      throw { data: { title: `Cari bulunamadı: "${companyNameValue}"` } };
    }
    companyId = match.id;
  }

  await api.post("/api/auth/register", {
    email: row.email,
    password: row.password,
    firstName: row.firstName,
    lastName: row.lastName,
    role: row.role,
    companyId,
  });
}

async function load() {
  loading.value = true;
  try {
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
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Kayıtlar yüklenemedi."));
  } finally {
    loading.value = false;
  }
}

async function create() {
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
    toast.success("Kullanıcı oluşturuldu.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Kullanıcı oluşturulamadı."));
  } finally {
    submitting.value = false;
  }
}

async function saveRole(user: UserDto) {
  try {
    if (roleDrafts[user.id] === "Supplier" && companyDrafts[user.id]) {
      await api.put(`/api/users/${user.id}`, {
        firstName: user.firstName,
        lastName: user.lastName,
        companyId: companyDrafts[user.id],
      });
    }
    await api.post(`/api/users/${user.id}/roles`, { roles: [roleDrafts[user.id]] });
    toast.success("Kullanıcı rolü güncellendi.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Rol güncellenemedi."));
  }
}

async function deactivate(user: UserDto) {
  try {
    await api.post(`/api/users/${user.id}/deactivate`);
    toast.success("Kullanıcı devre dışı bırakıldı.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Kullanıcı devre dışı bırakılamadı."));
  }
}

async function activate(user: UserDto) {
  try {
    await api.post(`/api/users/${user.id}/activate`);
    toast.success("Kullanıcı etkinleştirildi.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Kullanıcı etkinleştirilemedi."));
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">Kullanıcılar</h1>
      <div class="flex gap-2">
        <ExcelImportButton :columns="importColumns" template-filename="kullanicilar-sablon.xlsx" :import-row="importUserRow" @imported="load" />
        <AppButton @click="showForm = !showForm">
          {{ showForm ? "Vazgeç" : "+ Yeni Kullanıcı" }}
        </AppButton>
      </div>
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
      <AppButton type="submit" class="sm:col-span-2" :loading="submitting">
        {{ submitting ? "Kaydediliyor..." : "Kaydet" }}
      </AppButton>
    </form>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
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
                <AppBadge :tone="u.isActive ? 'emerald' : 'slate'">{{ u.isActive ? "Aktif" : "Pasif" }}</AppBadge>
              </td>
              <td class="py-2 text-right">
                <button v-if="isAdminRole() && u.isActive" class="text-xs font-medium text-rose-600 hover:underline" @click="deactivate(u)">
                  Devre Dışı Bırak
                </button>
                <button v-else-if="isAdminRole() && !u.isActive" class="text-xs font-medium text-emerald-600 hover:underline" @click="activate(u)">
                  Etkinleştir
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="users.length === 0">Henüz kullanıcı yok</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
