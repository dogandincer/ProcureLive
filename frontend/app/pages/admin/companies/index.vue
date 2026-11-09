<script setup lang="ts">
import type { CompanyDto, CompanyDetailDto, CompanyLookupResultDto, PagedResult, SectorDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";
import { MagnifyingGlassIcon, StarIcon, TrashIcon } from "@heroicons/vue/24/outline";

/** Cari ekranı: davetle veya kendi kaydıyla gelen tedarikçiler (Firma tanımı Firma Tanım ekranındadır). */
const api = useApi();
const { ask } = useConfirm();
const { can } = usePermissions();
const toast = useToast();
const { t } = useI18n();
const { selectedWorkspaceId, workspaces } = useWorkspace();
const { selectedFirmId } = useFirm();
const companies = ref<CompanyDto[]>([]);
const sectors = ref<SectorDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const emptyForm = () => ({
  code: "", name: "", taxNumber: "", taxOffice: "", website: "",
  tradeName: "", mersisNo: "", phone: "", email: "", foundationYear: "", employeeCount: "", description: "",
  sectorIds: [] as string[], visibleWorkspaceIds: [] as string[],
  userEmail: "", userPassword: "", userFirstName: "", userLastName: "",
});
const form = reactive(emptyForm());
const statuses = ["PendingApproval", "Active", "Passive"];
const statusDrafts = reactive<Record<string, string>>({});
const route = useRoute();
const statusFilter = ref(typeof route.query.status === "string" ? route.query.status : "");
const statusOptions = computed(() => statuses.map((s) => ({ value: s, label: t(`enums.companyStatus.${s}`) })));

// Birincil işyeri formda seçilir (başlıktaki seçim önerilir); ek görünür işyerleri aynı firmanın diğer aktif işyerleri.
const primaryWorkspaceId = ref("");
const importWorkspaceId = ref("");
function extraWorkspaceOptions(primaryId: string) {
  const firmId = workspaces.value.find((w) => w.id === primaryId)?.firmId;
  return workspaces.value
    .filter((w) => w.isActive && w.firmId === firmId && w.id !== primaryId)
    .map((w) => ({ value: w.id, label: w.name }));
}
const workspaceOptions = computed(() => extraWorkspaceOptions(primaryWorkspaceId.value));
watch(primaryWorkspaceId, () => {
  form.visibleWorkspaceIds = form.visibleWorkspaceIds.filter((id) => workspaceOptions.value.some((o) => o.value === id));
});
const sectorOptions = computed(() => sectors.value.filter((s) => s.isActive).map((s) => ({ value: s.id, label: s.name })));

// TCKN/VKN lookup (simulated auto-fill)
const lookupValue = ref("");
const lookingUp = ref(false);

async function runLookup() {
  if (!lookupValue.value.trim()) return;
  lookingUp.value = true;
  try {
    const r = await api.get<CompanyLookupResultDto>("/api/companies/lookup", { identifier: lookupValue.value.trim() });
    if (!r.isValid) {
      toast.error(r.message || t("companies.lookup.invalid"));
      return;
    }
    form.name = r.name ?? form.name;
    form.taxNumber = r.identifierType === "VKN" ? r.identifier : form.taxNumber;
    form.taxOffice = r.taxOffice ?? form.taxOffice;
    form.tradeName = r.tradeName ?? form.tradeName;
    form.mersisNo = r.mersisNo ?? form.mersisNo;
    form.phone = r.phone ?? form.phone;
    form.email = r.email ?? form.email;
    form.foundationYear = r.foundationYear ? String(r.foundationYear) : form.foundationYear;
    toast.success(t("companies.lookup.filled"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.lookup.error")));
  } finally {
    lookingUp.value = false;
  }
}

const importColumns = computed<ExcelColumn[]>(() => [
  { key: "code", label: t("companies.import.code"), hint: t("companies.import.codeHint") },
  { key: "name", label: t("companies.import.name"), required: true },
  { key: "taxNumber", label: t("companies.import.taxNumber"), required: true },
  { key: "taxOffice", label: t("companies.import.taxOffice"), required: true },
  { key: "sectors", label: t("companies.import.sectors"), hint: t("companies.import.listHint") },
  { key: "workspaces", label: t("companies.import.workspaces"), hint: t("companies.import.workspacesHint") },
  { key: "email", label: t("companies.import.email") },
  { key: "phone", label: t("companies.import.phone") },
  { key: "userEmail", label: t("companies.import.userEmail"), hint: t("companies.import.userEmailHint") },
  { key: "userPassword", label: t("companies.import.userPassword"), hint: t("companies.import.userPasswordHint") },
  { key: "userFirstName", label: t("companies.import.userFirstName") },
  { key: "userLastName", label: t("companies.import.userLastName") },
]);

const text = (v: string | number | boolean | null | undefined) => (v === null || v === undefined ? "" : String(v).trim());
const splitList = (v: string | number | boolean | null | undefined) =>
  text(v).split(/[;,]/).map((x) => x.trim().toLocaleLowerCase("tr")).filter(Boolean);

/** Excel adlarını ID'ye çevirir; bilinmeyen sektör/işyeri adı satırı hatalı sayar (sessizce atlanmaz). */
function resolveNames(names: string[], pool: { value: string; label: string }[], errorKey: string) {
  return names.map((n) => {
    const hit = pool.find((o) => o.label.toLocaleLowerCase("tr") === n);
    if (!hit) throw { data: { title: t(errorKey, { name: n }) } };
    return hit.value;
  });
}

async function importCompanyRow(row: Record<string, string | number | boolean | null>) {
  const userEmail = text(row.userEmail);
  const [first, ...rest] = text(row.name).split(" ");
  await api.post("/api/companies", {
    code: text(row.code) || null,
    name: text(row.name),
    taxNumber: text(row.taxNumber),
    taxOffice: text(row.taxOffice),
    companyType: "Supplier",
    website: null,
    workspaceId: importWorkspaceId.value || null,
    email: text(row.email) || null,
    phone: text(row.phone) || null,
    sectorIds: resolveNames(splitList(row.sectors), sectorOptions.value, "companies.import.unknownSector"),
    visibleWorkspaceIds: resolveNames(splitList(row.workspaces), extraWorkspaceOptions(importWorkspaceId.value), "companies.import.unknownWorkspace"),
    userEmail: userEmail || null,
    userPassword: userEmail ? text(row.userPassword) : null,
    userFirstName: userEmail ? text(row.userFirstName) || first || text(row.name) : null,
    userLastName: userEmail ? text(row.userLastName) || rest.join(" ") || t("companies.import.defaultLastName") : null,
  });
}

// Başlıktaki firma/işyeri açılışta art arda kurulur ve her değişim yeniden yükler; geç dönen eski (ör. filtresiz)
// yanıt yenisinin üzerine yazmasın diye yalnızca son isteğin sonucu uygulanır.
let loadSeq = 0;
const { page: listPage, total: listTotal, search: listSearch, pageSize: listPageSize, params: listParams, resetAndLoad } = usePagedList(() => load());


async function load() {
  const seq = ++loadSeq;
  loading.value = true;
  try {
    const params: Record<string, unknown> = { ...listParams("searchTerm"), companyType: "Supplier", status: statusFilter.value || undefined };
    // "Tüm İşyerleri": başlıktaki firmanın işyerleriyle sınırlı.
    if (selectedWorkspaceId.value) params.workspaceId = selectedWorkspaceId.value;
    else if (selectedFirmId.value) params.firmId = selectedFirmId.value;
    const page = await api.get<PagedResult<CompanyDto>>("/api/companies", params);
    if (seq !== loadSeq) return;
    companies.value = page.items;
    listTotal.value = page.totalCount;
    for (const c of companies.value) {
      statusDrafts[c.id] = c.status;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.loadError")));
  } finally {
    if (seq === loadSeq) loading.value = false;
  }
}

async function saveStatus(company: CompanyDto) {
  try {
    // Update overwrites all fields, so load the full detail and preserve it while changing status.
    const d = await api.get<CompanyDetailDto>(`/api/companies/${company.id}`);
    await api.put(`/api/companies/${company.id}`, {
      name: d.name,
      taxOffice: d.taxOffice,
      status: statusDrafts[company.id],
      website: d.website,
      tradeName: d.tradeName,
      mersisNo: d.mersisNo,
      phone: d.phone,
      email: d.email,
      foundationYear: d.foundationYear,
      employeeCount: d.employeeCount,
      description: d.description,
    });
    toast.success(t("toast.companyStatusUpdated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.statusUpdateError")));
  }
}

async function create() {
  submitting.value = true;
  try {
    await api.post("/api/companies", {
      code: form.code.trim() || null,
      name: form.name,
      taxNumber: form.taxNumber,
      taxOffice: form.taxOffice,
      companyType: "Supplier",
      website: form.website || null,
      workspaceId: primaryWorkspaceId.value || null,
      tradeName: form.tradeName || null,
      mersisNo: form.mersisNo || null,
      phone: form.phone || null,
      email: form.email || null,
      foundationYear: form.foundationYear ? Number(form.foundationYear) : null,
      employeeCount: form.employeeCount ? Number(form.employeeCount) : null,
      description: form.description || null,
      sectorIds: form.sectorIds,
      visibleWorkspaceIds: form.visibleWorkspaceIds,
      userEmail: form.userEmail.trim() || null,
      userPassword: form.userEmail.trim() ? form.userPassword : null,
      userFirstName: form.userEmail.trim() ? form.userFirstName : null,
      userLastName: form.userEmail.trim() ? form.userLastName : null,
    });
    Object.assign(form, emptyForm());
    lookupValue.value = "";
    showForm.value = false;
    toast.success(t("toast.companyCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.createError")));
  } finally {
    submitting.value = false;
  }
}

const deleting = ref<string | null>(null);
async function deleteCompany(c: CompanyDto) {
  if (!(await ask({ title: t("companies.deleteConfirm", { name: c.name }), tone: "danger", confirmLabel: t("common.delete") }))) return;
  deleting.value = c.id;
  try {
    await api.delete(`/api/companies/${c.id}`);
    toast.success(t("companies.deleted"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.deleteError")));
  } finally {
    deleting.value = null;
  }
}

async function loadSectors() {
  try {
    sectors.value = await api.get<SectorDto[]>("/api/sectors");
  } catch {
    sectors.value = [];
  }
}

watch([selectedWorkspaceId, selectedFirmId], () => resetAndLoad());
watch(statusFilter, () => resetAndLoad());
watch(() => route.query.status, (v) => { statusFilter.value = typeof v === "string" ? v : ""; });
onMounted(() => {
  load();
  loadSectors();
});
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("companies.title") }}</h1>
        <p class="text-sm text-slate-500">{{ $t("companies.subtitle") }}</p>
      </div>
      <div class="flex gap-2">
        <ErpImportButton kind="entities" @imported="load" />
        <ExcelImportButton :columns="importColumns" template-filename="cariler-sablon.xlsx" :import-row="importCompanyRow" @imported="load">
          <template #options><WorkspaceField v-model="importWorkspaceId" :label="$t('excelImport.targetWorkspace')" :hint="$t('companies.import.workspaceHint')" /></template>
        </ExcelImportButton>
        <AppButton v-if="can('companies.manage')" @click="showForm = !showForm">
          {{ showForm ? $t("common.cancel") : $t("companies.new") }}
        </AppButton>
      </div>
    </div>

    <form v-if="showForm" class="space-y-4 rounded-xl border border-slate-200 bg-surface p-5" @submit.prevent="create">
      <!-- TCKN/VKN lookup bar -->
      <div class="rounded-lg border border-indigo-100 bg-indigo-50/40 p-3">
        <label class="mb-1 block text-xs font-medium text-indigo-800">{{ $t("companies.lookup.label") }}</label>
        <div class="flex gap-2">
          <input
            v-model="lookupValue"
            :aria-label="$t('companies.lookup.label')"
            :placeholder="$t('companies.lookup.placeholder')"
            inputmode="numeric"
            class="min-w-0 flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            @keyup.enter.prevent="runLookup"
          />
          <button type="button" :disabled="lookingUp" class="flex items-center gap-1.5 rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50" @click="runLookup">
            <MagnifyingGlassIcon class="h-4 w-4" />
            {{ lookingUp ? $t("companies.lookup.querying") : $t("companies.lookup.query") }}
          </button>
        </div>
        <p class="mt-1 text-[11px] text-indigo-500">{{ $t("companies.lookup.hint") }}</p>
      </div>


      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <WorkspaceField v-model="primaryWorkspaceId" :label="$t('companies.form.primaryWorkspace')" class="sm:col-span-2" />
        <FormField :label="$t('companies.form.code')"><input v-model="form.code" maxlength="30" class="rounded-md border border-slate-300 px-3 py-2 text-sm uppercase" /></FormField>
        <FormField :label="$t('companies.form.name')" required><input v-model="form.name" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.tradeName')"><input v-model="form.tradeName" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.taxNumber')" required><input v-model="form.taxNumber" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.taxOffice')" required><input v-model="form.taxOffice" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.mersisNo')"><input v-model="form.mersisNo" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.phone')"><input v-model="form.phone" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.email')"><input v-model="form.email" type="email" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.foundationYear')"><input v-model="form.foundationYear" type="number" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.employeeCount')"><input v-model="form.employeeCount" type="number" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.website')" class="sm:col-span-2"><input v-model="form.website" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        <FormField :label="$t('companies.form.description')" class="sm:col-span-2"><textarea v-model="form.description" rows="2" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <ChipMultiSelect v-model="form.sectorIds" :options="sectorOptions" :label="$t('companies.form.sectors')" :empty-text="$t('companies.form.noSectors')" />
        <ChipMultiSelect
          v-model="form.visibleWorkspaceIds"
          :options="workspaceOptions"
          :label="$t('companies.form.visibleWorkspaces')"
          :empty-text="$t('companies.form.noOtherWorkspaces')"
        />
      </div>

      <fieldset class="rounded-lg border border-slate-200 p-3">
        <legend class="px-1 text-xs font-semibold text-slate-600">{{ $t("companies.form.userSection") }}</legend>
        <p class="mb-2 text-[11px] text-slate-400">{{ $t("companies.form.userHint") }}</p>
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormField :label="$t('companies.form.userEmail')"><input v-model="form.userEmail" type="email" autocomplete="off" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
          <FormField :label="$t('companies.form.userPassword')"><input v-model="form.userPassword" type="password" autocomplete="new-password" minlength="8" :required="!!form.userEmail" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
          <FormField :label="$t('companies.form.userFirstName')"><input v-model="form.userFirstName" :required="!!form.userEmail" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
          <FormField :label="$t('companies.form.userLastName')"><input v-model="form.userLastName" :required="!!form.userEmail" class="rounded-md border border-slate-300 px-3 py-2 text-sm" /></FormField>
        </div>
      </fieldset>

      <div class="flex justify-end">
        <AppButton type="submit" :loading="submitting">
          {{ submitting ? $t("common.saving") : $t("common.save") }}
        </AppButton>
      </div>
    </form>

    <AppCard>
      <div class="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <input v-model="listSearch" type="search" :placeholder="$t('pager.search.companies')" :aria-label="$t('pager.search.companies')" class="w-full max-w-sm rounded-md border border-slate-300 px-3 py-1.5 text-sm" data-testid="list-search" />
        <StatusFilter v-model="statusFilter" :options="statusOptions" />
      </div>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("companies.table.code") }}</th>
              <th class="pb-2">{{ $t("companies.table.name") }}</th>
              <th class="pb-2">{{ $t("companies.table.taxNumber") }}</th>
              <th class="pb-2">{{ $t("companies.table.workspaces") }}</th>
              <th class="pb-2">{{ $t("companies.table.rating") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in companies" :key="c.id" class="border-b border-slate-50">
              <td class="whitespace-nowrap py-2 font-mono text-xs text-slate-500">{{ c.code ?? "—" }}</td>
              <td class="py-2"><NuxtLink :to="`/admin/companies/${c.id}`" class="font-medium text-slate-800 hover:text-indigo-600 hover:underline">{{ c.name }}</NuxtLink></td>
              <td class="py-2 text-slate-500">{{ c.taxNumber }}</td>
              <td class="py-2 text-xs text-slate-500">
                <span v-if="c.workspaceName" class="font-medium text-slate-600">{{ c.workspaceName }}</span>
                <span v-else class="rounded bg-amber-50 px-1.5 py-0.5 text-amber-700">{{ $t("companies.table.unassigned") }}</span>
                <span v-if="c.visibleWorkspaceNames.length" class="text-slate-400"> + {{ c.visibleWorkspaceNames.join(", ") }}</span>
              </td>
              <td class="py-2">
                <span v-if="c.averageScore != null" class="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold"
                  :class="c.averageScore >= 4 ? 'bg-emerald-50 text-emerald-700' : c.averageScore >= 3 ? 'bg-amber-50 text-amber-700' : 'bg-rose-50 text-rose-700'">
                  <StarIcon class="h-3 w-3 fill-current" />{{ c.averageScore.toFixed(1) }}
                  <span class="font-normal opacity-70">({{ c.evaluationCount }})</span>
                </span>
                <span v-else class="text-xs text-slate-300">—</span>
              </td>
              <td class="py-2">
                <div class="flex items-center gap-2">
                  <select v-model="statusDrafts[c.id]" :aria-label="`${$t('common.status')} — ${c.name}`" class="rounded-md border border-slate-300 px-2 py-1 text-xs">
                    <option v-for="s in statuses" :key="s" :value="s">{{ $t(`enums.companyStatus.${s}`) }}</option>
                  </select>
                  <button
                    v-if="statusDrafts[c.id] !== c.status"
                    class="rounded bg-indigo-600 px-2 py-1 text-xs font-semibold text-white"
                    @click="saveStatus(c)"
                  >
                    {{ $t("common.save") }}
                  </button>
                </div>
              </td>
              <td class="py-2 text-right">
                <div class="flex items-center justify-end gap-2">
                  <NuxtLink :to="`/admin/companies/${c.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink>
                  <button
                    class="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-40"
                    :title="$t('common.delete')"
                    :disabled="deleting === c.id"
                    @click="deleteCompany(c)"
                  >
                    <TrashIcon class="h-4 w-4" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="companies.length === 0">{{ $t("companies.empty") }}</EmptyState>
        <ListPager v-model:page="listPage" :total="listTotal" :page-size="listPageSize" />
      </div>
    </AppCard>
  </div>
</template>
