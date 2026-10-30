<script setup lang="ts">
import type { CompanyDetailDto, SectorDto, WorkspaceDto } from "~/types/api";
import { CheckIcon, BuildingStorefrontIcon, StarIcon, PencilIcon, TrashIcon } from "@heroicons/vue/24/outline";

const route = useRoute();
const router = useRouter();
const companyId = route.params.id as string;
const api = useApi();
const { ask } = useConfirm();
const accessState = ref<"forbidden" | "notFound" | null>(null);
const toast = useToast();
const { t } = useI18n();

const company = ref<CompanyDetailDto | null>(null);
const { can } = usePermissions();
const loading = ref(true);
const showContactForm = ref(false);
const showAddressForm = ref(false);

// Sector assignment
const allSectors = ref<SectorDto[]>([]);
const selectedSectorIds = ref<Set<string>>(new Set());
const savingSectors = ref(false);
const showSectorPanel = ref(false);

// Workspace assignment
const workspaces = ref<WorkspaceDto[]>([]);
const companyWorkspaceId = ref<string>("");
const savingWorkspace = ref(false);
const visibleWorkspaceIds = ref<string[]>([]);
const savingVisible = ref(false);
// Başlıktaki firmanın işyerleri + zaten bağlı olanlar (başka firmalardaki bağlar da görünür kalır).
const { selectedFirmId } = useFirm();
const visibleOptions = computed(() =>
  workspaces.value
    .filter((w) => w.id !== companyWorkspaceId.value)
    .filter((w) => !selectedFirmId.value || w.firmId === selectedFirmId.value || (company.value?.visibleWorkspaceIds ?? []).includes(w.id))
    .map((w) => ({ value: w.id, label: w.firmCode ? `${w.firmCode} · ${w.name}` : w.name })),
);
const visibleDirty = computed(() => {
  const saved = [...(company.value?.visibleWorkspaceIds ?? [])].sort().join();
  return saved !== [...visibleWorkspaceIds.value].sort().join();
});

// Evaluation
const showEvalForm = ref(false);
const savingEval = ref(false);
const evalForm = reactive({ quality: 4, delivery: 4, communication: 4, price: 4, compliance: 4, comments: "" });
const evalCriteria = [
  { key: "quality", model: "quality" as const },
  { key: "delivery", model: "delivery" as const },
  { key: "price", model: "price" as const },
  { key: "communication", model: "communication" as const },
  { key: "compliance", model: "compliance" as const },
];
const { formatDateTime } = useDateFormat();

async function submitEvaluation() {
  savingEval.value = true;
  try {
    await api.post(`/api/companies/${companyId}/evaluations`, {
      qualityScore: evalForm.quality,
      deliveryScore: evalForm.delivery,
      communicationScore: evalForm.communication,
      priceScore: evalForm.price,
      complianceScore: evalForm.compliance,
      comments: evalForm.comments || null,
      sourceType: "Manual",
      sourceProcessId: null,
    });
    toast.success(t("companies.detail.evaluation.saved"));
    Object.assign(evalForm, { quality: 4, delivery: 4, communication: 4, price: 4, compliance: 4, comments: "" });
    showEvalForm.value = false;
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.evaluation.saveError")));
  } finally {
    savingEval.value = false;
  }
}

function scoreColor(score: number): string {
  if (score >= 4) return "text-emerald-600";
  if (score >= 3) return "text-amber-600";
  return "text-rose-600";
}

const contactForm = reactive({ firstName: "", lastName: "", title: "", email: "", phone: "", isPrimary: false });
const addressTypes = ["HeadOffice", "Billing", "Shipping"];
const addressForm = reactive({ addressType: "HeadOffice", country: "Türkiye", city: "", district: "", postalCode: "", line1: "", line2: "" });

async function load() {
  loading.value = true;
  try {
    const [companyData, sectors, ws] = await Promise.all([
      api.get<CompanyDetailDto>(`/api/companies/${companyId}`),
      api.get<SectorDto[]>("/api/sectors"),
      api.get<WorkspaceDto[]>("/api/workspaces", { activeOnly: true }),
    ]);
    company.value = companyData;
    allSectors.value = sectors;
    workspaces.value = ws;
    companyWorkspaceId.value = companyData.workspaceId ?? "";
    visibleWorkspaceIds.value = [...companyData.visibleWorkspaceIds];
    selectedSectorIds.value = new Set(companyData.sectors.map((s) => s.id));
  } catch (error: any) {
    // Yetki yoksa / kayıt yoksa boş sayfa ve kaybolan bildirim yerine açıklayıcı durum gösterilir.
    const code = error?.response?.status ?? error?.statusCode;
    if (code === 403 || code === 404) accessState.value = code === 403 ? "forbidden" : "notFound";
    else toast.error(apiErrorMessage(error, t("companies.detail.loadError")));
  } finally {
    loading.value = false;
  }
}

function toggleSector(id: string) {
  if (selectedSectorIds.value.has(id)) {
    selectedSectorIds.value.delete(id);
  } else {
    selectedSectorIds.value.add(id);
  }
}

async function saveSectors() {
  savingSectors.value = true;
  try {
    await api.put(`/api/companies/${companyId}/sectors`, {
      sectorIds: Array.from(selectedSectorIds.value),
    });
    toast.success(t("companies.detail.sectorsSaved"));
    showSectorPanel.value = false;
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.sectorsError")));
  } finally {
    savingSectors.value = false;
  }
}

async function saveWorkspace() {
  savingWorkspace.value = true;
  try {
    await api.put(`/api/companies/${companyId}/workspace`, {
      workspaceId: companyWorkspaceId.value || null,
    });
    toast.success(t("companies.detail.workspaceSaved"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.workspaceError")));
  } finally {
    savingWorkspace.value = false;
  }
}

async function saveVisibleWorkspaces() {
  savingVisible.value = true;
  try {
    await api.put(`/api/companies/${companyId}/visible-workspaces`, { workspaceIds: visibleWorkspaceIds.value });
    toast.success(t("companies.detail.visibleSaved"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.visibleError")));
  } finally {
    savingVisible.value = false;
  }
}

async function addContact() {
  try {
    await api.post(`/api/companies/${companyId}/contacts`, {
      firstName: contactForm.firstName,
      lastName: contactForm.lastName,
      title: contactForm.title || null,
      email: contactForm.email,
      phone: contactForm.phone || null,
      isPrimary: contactForm.isPrimary,
    });
    Object.assign(contactForm, { firstName: "", lastName: "", title: "", email: "", phone: "", isPrimary: false });
    showContactForm.value = false;
    toast.success(t("toast.contactCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.contactAddError")));
  }
}

async function addAddress() {
  try {
    await api.post(`/api/companies/${companyId}/addresses`, {
      addressType: addressForm.addressType,
      country: addressForm.country,
      city: addressForm.city,
      district: addressForm.district || null,
      postalCode: addressForm.postalCode || null,
      line1: addressForm.line1,
      line2: addressForm.line2 || null,
    });
    Object.assign(addressForm, { addressType: "HeadOffice", country: "Türkiye", city: "", district: "", postalCode: "", line1: "", line2: "" });
    showAddressForm.value = false;
    toast.success(t("toast.addressCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.addressAddError")));
  }
}

// Edit base + detail fields
const showEditForm = ref(false);
const savingEdit = ref(false);
const statuses = ["PendingApproval", "Active", "Passive"];
const editForm = reactive({
  code: "", name: "", taxOffice: "", status: "Active", website: "",
  tradeName: "", mersisNo: "", phone: "", email: "", foundationYear: "", employeeCount: "", description: "",
});

function openEdit() {
  if (!company.value) return;
  const c = company.value;
  Object.assign(editForm, {
    code: c.code ?? "", name: c.name, taxOffice: c.taxOffice, status: c.status, website: c.website ?? "",
    tradeName: c.tradeName ?? "", mersisNo: c.mersisNo ?? "", phone: c.phone ?? "", email: c.email ?? "",
    foundationYear: c.foundationYear != null ? String(c.foundationYear) : "",
    employeeCount: c.employeeCount != null ? String(c.employeeCount) : "",
    description: c.description ?? "",
  });
  showEditForm.value = true;
}

// Kendi kaydıyla gelen (onay bekleyen) tedarikçiyi tek adımda aktifleştir; diğer alanlar korunur.
const approving = ref(false);
async function approvePending() {
  if (!company.value) return;
  approving.value = true;
  try {
    const c = company.value;
    await api.put(`/api/companies/${companyId}`, {
      code: c.code ?? null, name: c.name, taxOffice: c.taxOffice, status: "Active", website: c.website ?? null,
      tradeName: c.tradeName ?? null, mersisNo: c.mersisNo ?? null, phone: c.phone ?? null, email: c.email ?? null,
      foundationYear: c.foundationYear ?? null, employeeCount: c.employeeCount ?? null, description: c.description ?? null,
    });
    toast.success(t("companies.detail.approved"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.updateError")));
  } finally {
    approving.value = false;
  }
}

async function saveEdit() {
  savingEdit.value = true;
  try {
    await api.put(`/api/companies/${companyId}`, {
      code: company.value?.companyType === "Internal" ? null : editForm.code.trim() || null,
      name: editForm.name,
      taxOffice: editForm.taxOffice,
      status: editForm.status,
      website: editForm.website || null,
      tradeName: editForm.tradeName || null,
      mersisNo: editForm.mersisNo || null,
      phone: editForm.phone || null,
      email: editForm.email || null,
      foundationYear: editForm.foundationYear ? Number(editForm.foundationYear) : null,
      employeeCount: editForm.employeeCount ? Number(editForm.employeeCount) : null,
      description: editForm.description || null,
    });
    toast.success(t("companies.detail.updated"));
    showEditForm.value = false;
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.updateError")));
  } finally {
    savingEdit.value = false;
  }
}

const deletingCompany = ref(false);
async function deleteCompany() {
  if (!company.value || !(await ask({ title: t("companies.deleteConfirm", { name: company.value.name }), tone: "danger", confirmLabel: t("common.delete") }))) return;
  deletingCompany.value = true;
  try {
    await api.delete(`/api/companies/${companyId}`);
    toast.success(t("companies.deleted"));
    await router.push("/admin/companies");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.deleteError")));
    deletingCompany.value = false;
  }
}

onMounted(load);
</script>

<template>
  <AccessState v-if="accessState" :kind="accessState" back-to="/admin/companies" :back-label="$t('nav.companies')" />
  <LoadingSpinner v-else-if="loading" />

  <div v-else-if="company" class="space-y-6">
    <div>
      <NuxtLink to="/admin/companies" class="text-xs text-indigo-600 hover:underline">← {{ $t("companies.title") }}</NuxtLink>
      <div class="mt-1 flex flex-wrap items-start justify-between gap-2">
        <div>
          <h1 class="flex flex-wrap items-center gap-2 text-xl font-bold text-slate-800">
            <span><span v-if="company.code" class="mr-1 font-mono text-base font-semibold text-slate-400">{{ company.code }}</span>{{ company.name }}</span>
            <AppBadge :tone="company.status === 'Active' ? 'emerald' : company.status === 'PendingApproval' ? 'amber' : 'slate'">{{ $t(`enums.companyStatus.${company.status}`) }}</AppBadge>
          </h1>
          <p class="text-sm text-slate-500">
            {{ company.taxNumber }} · {{ company.taxOffice }} · {{ $t(`enums.companyType.${company.companyType}`) }} · {{ $t(`enums.companyStatus.${company.status}`) }}
          </p>
        </div>
        <div class="flex items-center gap-2">
          <button class="flex items-center gap-1 rounded-md border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50" @click="openEdit">
            <PencilIcon class="h-3.5 w-3.5" />{{ $t("common.edit") }}
          </button>
          <button class="flex items-center gap-1 rounded-md border border-red-200 px-2.5 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50" :disabled="deletingCompany" @click="deleteCompany">
            <TrashIcon class="h-3.5 w-3.5" />{{ $t("common.delete") }}
          </button>
        </div>
      </div>
      <div class="mt-2 flex items-center gap-2 text-sm">
        <BuildingStorefrontIcon class="h-4 w-4 shrink-0 text-slate-400" />
        <span class="text-slate-500">{{ $t("companies.detail.workspace") }}:</span>
        <select
          v-model="companyWorkspaceId"
          :disabled="savingWorkspace"
          class="rounded-md border border-slate-300 px-2 py-1 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:opacity-50"
          @change="saveWorkspace"
        >
          <option value="">{{ $t("workspaces.unassigned") }}</option>
          <option v-for="w in workspaces" :key="w.id" :value="w.id">{{ w.name }}</option>
        </select>
      </div>
      <div
        v-if="company.companyType !== 'Internal' && company.status === 'PendingApproval'"
        class="mt-3 flex flex-wrap items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
        data-testid="pending-company"
      >
        <p class="min-w-0 flex-1">{{ companyWorkspaceId ? $t("companies.detail.pendingHint") : $t("companies.detail.pendingNeedsWorkspace") }}</p>
        <AppButton variant="success" :loading="approving" :disabled="!companyWorkspaceId" data-testid="approve-company" @click="approvePending">
          {{ $t("companies.detail.approve") }}
        </AppButton>
      </div>
      <div v-if="company.companyType !== 'Internal'" class="mt-3 rounded-lg border border-slate-200 bg-surface p-3">
        <ChipMultiSelect
          v-model="visibleWorkspaceIds"
          :options="visibleOptions"
          :label="$t('companies.detail.visibleWorkspaces')"
          :empty-text="$t('companies.form.noOtherWorkspaces')"
        />
        <p class="mt-1 text-[11px] text-slate-400">{{ $t("companies.detail.visibleHint") }}</p>
        <button
          v-if="visibleDirty"
          class="mt-2 rounded-md bg-indigo-600 px-3 py-1 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
          :disabled="savingVisible"
          @click="saveVisibleWorkspaces"
        >
          {{ savingVisible ? $t("common.saving") : $t("common.save") }}
        </button>
      </div>
    </div>

    <!-- Edit form -->
    <AppCard v-if="showEditForm" :title="$t('companies.detail.editTitle')">
      <form class="grid grid-cols-1 gap-3 sm:grid-cols-2" @submit.prevent="saveEdit">
        <div v-if="company.companyType !== 'Internal'"><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.code") }}</label><input v-model="editForm.code" maxlength="30" :placeholder="$t('companies.form.codeAuto')" class="w-full rounded-md border border-slate-300 px-3 py-1.5 font-mono text-sm uppercase" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.name") }}</label><input v-model="editForm.name" required class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.tradeName") }}</label><input v-model="editForm.tradeName" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.taxOffice") }}</label><input v-model="editForm.taxOffice" required class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.mersisNo") }}</label><input v-model="editForm.mersisNo" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("common.status") }}</label>
          <select v-model="editForm.status" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm">
            <option v-for="s in statuses" :key="s" :value="s">{{ $t(`enums.companyStatus.${s}`) }}</option>
          </select>
        </div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.phone") }}</label><input v-model="editForm.phone" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.email") }}</label><input v-model="editForm.email" type="email" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.website") }}</label><input v-model="editForm.website" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.foundationYear") }}</label><input v-model="editForm.foundationYear" type="number" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.employeeCount") }}</label><input v-model="editForm.employeeCount" type="number" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div class="sm:col-span-2"><label class="mb-1 block text-xs text-slate-500">{{ $t("companies.form.description") }}</label><textarea v-model="editForm.description" rows="2" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
        <div class="flex justify-end gap-2 sm:col-span-2">
          <button type="button" class="rounded-md px-4 py-1.5 text-sm text-slate-600 hover:bg-slate-100" @click="showEditForm = false">{{ $t("common.cancel") }}</button>
          <button type="submit" :disabled="savingEdit" class="rounded-md bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">{{ savingEdit ? $t("common.saving") : $t("common.save") }}</button>
        </div>
      </form>
    </AppCard>

    <CompanySettingsCard :company-id="companyId" />

    <CompanyUsersCard v-if="company.companyType !== 'Internal'" :company-id="companyId" />

    <CompanyDocumentsCard v-if="company.companyType !== 'Internal'" :company-id="companyId" :editable="can('companies.manage')" />

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <AppCard>
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-slate-700">{{ $t("companies.detail.contacts") }}</h2>
          <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showContactForm = !showContactForm">
            {{ showContactForm ? $t("common.cancel") : `+ ${$t("common.new")}` }}
          </button>
        </div>

        <form v-if="showContactForm" class="mt-3 grid grid-cols-2 gap-2" @submit.prevent="addContact">
          <input v-model="contactForm.firstName" :placeholder="$t('companies.detail.form.firstName')" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="contactForm.lastName" :placeholder="$t('companies.detail.form.lastName')" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="contactForm.title" :placeholder="$t('companies.detail.form.title')" class="col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="contactForm.email" type="email" :placeholder="$t('companies.detail.form.email')" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="contactForm.phone" :placeholder="$t('companies.detail.form.phone')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <label class="col-span-2 flex items-center gap-2 text-xs text-slate-600">
            <input v-model="contactForm.isPrimary" type="checkbox" />
            {{ $t("companies.detail.primaryContactLabel") }}
          </label>
          <button type="submit" class="col-span-2 rounded-md bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white">{{ $t("common.save") }}</button>
        </form>

        <ul v-if="company.contactPersons.length > 0" class="mt-3 space-y-2 text-sm">
          <li v-for="c in company.contactPersons" :key="c.id" class="rounded-lg border border-slate-100 p-2">
            <div class="flex items-center justify-between">
              <span class="font-medium text-slate-700">{{ c.firstName }} {{ c.lastName }}</span>
              <AppBadge v-if="c.isPrimary" tone="indigo">{{ $t("companies.detail.primary") }}</AppBadge>
            </div>
            <div class="text-xs text-slate-500">{{ c.title ?? "-" }} · {{ c.email }} <span v-if="c.phone">· {{ c.phone }}</span></div>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">{{ $t("companies.detail.emptyContacts") }}</EmptyState>
      </AppCard>

      <AppCard>
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-slate-700">{{ $t("companies.detail.addresses") }}</h2>
          <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showAddressForm = !showAddressForm">
            {{ showAddressForm ? $t("common.cancel") : `+ ${$t("common.new")}` }}
          </button>
        </div>

        <form v-if="showAddressForm" class="mt-3 grid grid-cols-2 gap-2" @submit.prevent="addAddress">
          <select v-model="addressForm.addressType" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
            <option v-for="at in addressTypes" :key="at" :value="at">{{ $t(`enums.addressType.${at}`) }}</option>
          </select>
          <input v-model="addressForm.country" :placeholder="$t('companies.detail.form.country')" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.city" :placeholder="$t('companies.detail.form.city')" required class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.district" :placeholder="$t('companies.detail.form.district')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.postalCode" :placeholder="$t('companies.detail.form.postalCode')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.line1" :placeholder="$t('companies.detail.form.line1')" required class="col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <input v-model="addressForm.line2" :placeholder="$t('companies.detail.form.line2')" class="col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          <button type="submit" class="col-span-2 rounded-md bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white">{{ $t("common.save") }}</button>
        </form>

        <ul v-if="company.addresses.length > 0" class="mt-3 space-y-2 text-sm">
          <li v-for="a in company.addresses" :key="a.id" class="rounded-lg border border-slate-100 p-2">
            <div class="font-medium text-slate-700">{{ $t(`enums.addressType.${a.addressType}`) }}</div>
            <div class="text-xs text-slate-500">
              {{ a.line1 }}<span v-if="a.line2">, {{ a.line2 }}</span>, {{ a.district ? a.district + " / " : "" }}{{ a.city }}
              <span v-if="a.postalCode">({{ a.postalCode }})</span>, {{ a.country }}
            </div>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">{{ $t("companies.detail.emptyAddresses") }}</EmptyState>
      </AppCard>
    </div>

    <!-- Sectors card (full width) -->
    <AppCard>
      <div class="flex items-center justify-between">
        <h2 class="text-sm font-semibold text-slate-700">{{ $t("companies.detail.sectors") }}</h2>
        <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showSectorPanel = !showSectorPanel">
          {{ showSectorPanel ? $t("common.cancel") : $t("companies.detail.editSectors") }}
        </button>
      </div>

      <!-- Sector assignment panel -->
      <div v-if="showSectorPanel" class="mt-3">
        <div v-if="allSectors.length === 0" class="text-sm text-slate-400">{{ $t("sectors.empty") }}</div>
        <div v-else class="flex flex-wrap gap-2">
          <button
            v-for="sector in allSectors"
            :key="sector.id"
            type="button"
            :class="[
              'flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-medium transition-colors',
              selectedSectorIds.has(sector.id)
                ? 'border-indigo-500 bg-indigo-50 text-indigo-700'
                : 'border-slate-200 bg-surface text-slate-600 hover:border-indigo-300',
            ]"
            @click="toggleSector(sector.id)"
          >
            <CheckIcon v-if="selectedSectorIds.has(sector.id)" class="h-3 w-3" />
            {{ sector.name }}
          </button>
        </div>
        <button
          class="mt-3 rounded-md bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
          :disabled="savingSectors"
          @click="saveSectors"
        >
          {{ savingSectors ? $t("common.saving") : $t("common.save") }}
        </button>
      </div>

      <!-- Display assigned sectors -->
      <div v-else class="mt-3">
        <div v-if="company.sectors.length === 0" class="text-sm text-slate-400">{{ $t("companies.detail.noSectors") }}</div>
        <div v-else class="flex flex-wrap gap-2">
          <span
            v-for="sector in company.sectors"
            :key="sector.id"
            class="rounded-full bg-indigo-50 px-3 py-1 text-xs font-medium text-indigo-700"
          >
            {{ sector.name }}
          </span>
        </div>
      </div>
    </AppCard>

    <!-- Company info (detailed fields) -->
    <AppCard>
      <h2 class="text-sm font-semibold text-slate-700">{{ $t("companies.detail.info") }}</h2>
      <dl class="mt-3 grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3">
        <div><dt class="text-xs text-slate-400">{{ $t("companies.detail.fields.tradeName") }}</dt><dd class="text-slate-700">{{ company.tradeName || "—" }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("companies.detail.fields.mersisNo") }}</dt><dd class="font-mono text-slate-700">{{ company.mersisNo || "—" }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("companies.detail.fields.phone") }}</dt><dd class="text-slate-700">{{ company.phone || "—" }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("companies.detail.fields.email") }}</dt><dd class="truncate text-slate-700">{{ company.email || "—" }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("companies.detail.fields.foundationYear") }}</dt><dd class="text-slate-700">{{ company.foundationYear || "—" }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("companies.detail.fields.employeeCount") }}</dt><dd class="text-slate-700">{{ company.employeeCount || "—" }}</dd></div>
      </dl>
      <div v-if="company.description" class="mt-3 border-t border-slate-100 pt-3 text-sm text-slate-600">{{ company.description }}</div>
    </AppCard>

    <!-- Evaluation / scorecard -->
    <AppCard>
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <StarIcon class="h-5 w-5 text-amber-500" />
          <h2 class="text-sm font-semibold text-slate-700">{{ $t("companies.detail.evaluation.title") }}</h2>
        </div>
        <button class="text-xs font-medium text-indigo-600 hover:underline" @click="showEvalForm = !showEvalForm">
          {{ showEvalForm ? $t("common.cancel") : $t("companies.detail.evaluation.add") }}
        </button>
      </div>

      <!-- Aggregate score -->
      <div class="mt-3 flex items-center gap-4 rounded-xl border border-slate-100 bg-slate-50/60 p-4">
        <div class="text-center">
          <div class="flex items-end gap-1">
            <span class="text-3xl font-bold tabular-nums" :class="company.averageScore != null ? scoreColor(company.averageScore) : 'text-slate-300'">
              {{ company.averageScore != null ? company.averageScore.toFixed(2) : "—" }}
            </span>
            <span class="mb-1 text-sm text-slate-400">/ 5</span>
          </div>
          <div class="mt-0.5 text-[11px] text-slate-400">{{ company.evaluationCount }} {{ $t("companies.detail.evaluation.count") }}</div>
        </div>
        <div class="flex-1">
          <div class="flex gap-1">
            <div v-for="n in 5" :key="n" class="h-2 flex-1 rounded-full" :class="company.averageScore != null && company.averageScore >= n - 0.5 ? 'bg-amber-400' : 'bg-slate-200'" />
          </div>
          <p class="mt-2 text-xs text-slate-500">{{ $t("companies.detail.evaluation.hint") }}</p>
        </div>
      </div>

      <!-- Add evaluation form -->
      <form v-if="showEvalForm" class="mt-4 space-y-3 rounded-xl border border-indigo-100 bg-indigo-50/30 p-4" @submit.prevent="submitEvaluation">
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div v-for="crit in evalCriteria" :key="crit.key">
            <label class="mb-1 flex items-center justify-between text-xs font-medium text-slate-600">
              <span>{{ $t(`scoreboard.criteria.${crit.key}`) }}</span>
              <span class="font-bold text-indigo-600 tabular-nums">{{ evalForm[crit.model] }}/5</span>
            </label>
            <input v-model.number="evalForm[crit.model]" type="range" min="1" max="5" step="1" class="w-full accent-indigo-600" />
          </div>
        </div>
        <textarea v-model="evalForm.comments" rows="2" :placeholder="$t('companies.detail.evaluation.commentsPlaceholder')" maxlength="2000" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500" />
        <div class="flex justify-end">
          <button type="submit" :disabled="savingEval" class="rounded-md bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
            {{ savingEval ? $t("common.saving") : $t("companies.detail.evaluation.submit") }}
          </button>
        </div>
      </form>

      <!-- Evaluation history -->
      <div v-if="company.evaluations.length > 0" class="mt-4">
        <div class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-slate-400">{{ $t("companies.detail.evaluation.history") }}</div>
        <ul class="divide-y divide-slate-50">
          <li v-for="ev in [...company.evaluations].sort((a, b) => new Date(b.evaluatedAt).getTime() - new Date(a.evaluatedAt).getTime())" :key="ev.id" class="py-2.5">
            <div class="flex items-center justify-between">
              <span class="text-sm font-bold tabular-nums" :class="scoreColor(ev.overallScore)">{{ ev.overallScore.toFixed(2) }} <span class="text-xs font-normal text-slate-400">/5</span></span>
              <span class="text-[11px] text-slate-400">{{ formatDateTime(ev.evaluatedAt) }}</span>
            </div>
            <div class="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-[11px] text-slate-500">
              <span>{{ $t("scoreboard.criteria.quality") }}: {{ ev.qualityScore }}</span>
              <span>{{ $t("scoreboard.criteria.delivery") }}: {{ ev.deliveryScore }}</span>
              <span>{{ $t("scoreboard.criteria.price") }}: {{ ev.priceScore }}</span>
              <span>{{ $t("scoreboard.criteria.communication") }}: {{ ev.communicationScore }}</span>
              <span>{{ $t("scoreboard.criteria.compliance") }}: {{ ev.complianceScore }}</span>
            </div>
            <p v-if="ev.comments" class="mt-1 text-xs text-slate-500 italic">"{{ ev.comments }}"</p>
          </li>
        </ul>
      </div>
      <EmptyState v-else class="mt-4">{{ $t("companies.detail.evaluation.empty") }}</EmptyState>
    </AppCard>
  </div>
</template>
