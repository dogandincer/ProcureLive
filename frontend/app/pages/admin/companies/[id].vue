<script setup lang="ts">
import type { CompanyDetailDto, SectorDto, WorkspaceDto } from "~/types/api";
import { CheckIcon, BuildingStorefrontIcon } from "@heroicons/vue/24/outline";

const route = useRoute();
const companyId = route.params.id as string;
const api = useApi();
const toast = useToast();
const { t } = useI18n();

const company = ref<CompanyDetailDto | null>(null);
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
    selectedSectorIds.value = new Set(companyData.sectors.map((s) => s.id));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("companies.detail.loadError")));
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

onMounted(load);
</script>

<template>
  <LoadingSpinner v-if="loading" />

  <div v-else-if="company" class="space-y-6">
    <div>
      <NuxtLink to="/admin/companies" class="text-xs text-indigo-600 hover:underline">← {{ $t("companies.title") }}</NuxtLink>
      <h1 class="mt-1 text-xl font-bold text-slate-800">{{ company.name }}</h1>
      <p class="text-sm text-slate-500">
        {{ company.taxNumber }} · {{ company.taxOffice }} · {{ company.companyType }} · {{ $t(`enums.companyStatus.${company.status}`) }}
      </p>
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
    </div>

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
                : 'border-slate-200 bg-white text-slate-600 hover:border-indigo-300',
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
  </div>
</template>
