<script setup lang="ts">
import type { FirmDto } from "~/types/api";
import { PlusIcon, PencilIcon, TrashIcon, StarIcon, PhotoIcon, BuildingOffice2Icon } from "@heroicons/vue/24/outline";
import { StarIcon as StarSolid } from "@heroicons/vue/24/solid";

/** Firma Tanımı (yalnızca Admin). Firma, işyerlerinin üst ebeveynidir; varsayılan firmanın markası uygulama adını belirler. */
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const assetUrl = useAssetUrl();
const { refresh: refreshBranding } = useBranding();
const { firms: sharedFirms, ensureSelection } = useFirm();

const firms = ref<FirmDto[]>([]);
const loading = ref(true);
const editingId = ref<string | null>(null);
const showForm = ref(false);
const saving = ref(false);
const logoFile = ref<File | null>(null);
// Erişilemeyen (eski URL tabanlı) logolar kırık resim yerine varsayılan ikonla gösterilir.
const brokenLogos = reactive(new Set<string>());
const logoPreview = ref<string | null>(null);
const removeLogo = ref(false);

const emptyForm = () => ({
  code: "", name: "", tradeName: "", shortName: "",
  phone: "", phone2: "", mobilePhone: "", fax: "", email: "", email2: "", website: "",
  taxOffice: "", taxNumber: "",
  billingCountry: "Türkiye", billingCity: "", billingDistrict: "", billingPostalCode: "", billingAddressLine: "",
  isDefault: false, appTitleSuffix: "",
});
const form = reactive(emptyForm());

const editingFirm = computed(() => firms.value.find((f) => f.id === editingId.value) ?? null);
const currentLogo = computed(() => logoPreview.value ?? (removeLogo.value ? null : assetUrl(editingFirm.value?.logoUrl)));
const titlePreview = computed(() =>
  form.shortName.trim() ? `${form.shortName.trim()} ${form.appTitleSuffix.trim() || t("firms.defaultSuffix")}` : t("app.name"),
);

async function load() {
  loading.value = true;
  try {
    firms.value = await api.get<FirmDto[]>("/api/firms");
    sharedFirms.value = firms.value;
    ensureSelection();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("firms.loadError")));
  } finally {
    loading.value = false;
  }
}

function suggestCode() {
  const used = new Set(firms.value.map((f) => f.code));
  let n = firms.value.length + 1;
  while (used.has(`FIRMA-${String(n).padStart(3, "0")}`)) n++;
  return `FIRMA-${String(n).padStart(3, "0")}`;
}

function resetLogo() {
  logoFile.value = null;
  if (logoPreview.value) URL.revokeObjectURL(logoPreview.value);
  logoPreview.value = null;
  removeLogo.value = false;
}

function openCreate() {
  editingId.value = null;
  Object.assign(form, emptyForm(), { code: suggestCode(), isDefault: firms.value.length === 0 });
  resetLogo();
  showForm.value = true;
}

function openEdit(f: FirmDto) {
  editingId.value = f.id;
  Object.assign(form, emptyForm(), {
    code: f.code, name: f.name, tradeName: f.tradeName ?? "", shortName: f.shortName ?? "",
    phone: f.phone ?? "", phone2: f.phone2 ?? "", mobilePhone: f.mobilePhone ?? "", fax: f.fax ?? "",
    email: f.email ?? "", email2: f.email2 ?? "", website: f.website ?? "",
    taxOffice: f.taxOffice ?? "", taxNumber: f.taxNumber ?? "",
    billingCountry: f.billingCountry ?? "Türkiye", billingCity: f.billingCity ?? "", billingDistrict: f.billingDistrict ?? "",
    billingPostalCode: f.billingPostalCode ?? "", billingAddressLine: f.billingAddressLine ?? "",
    isDefault: f.isDefault, appTitleSuffix: f.appTitleSuffix ?? "",
  });
  resetLogo();
  showForm.value = true;
}

function onLogoSelected(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0];
  if (!file) return;
  if (file.size > 2 * 1024 * 1024) {
    toast.error(t("firms.logoTooLarge"));
    return;
  }
  resetLogo();
  logoFile.value = file;
  logoPreview.value = URL.createObjectURL(file);
}

const nullIfEmpty = (v: string) => (v.trim() ? v.trim() : null);

async function save() {
  saving.value = true;
  try {
    const body = Object.fromEntries(Object.entries(form).map(([k, v]) => [k, typeof v === "string" ? nullIfEmpty(v) : v]));
    let id = editingId.value;
    if (id) {
      await api.put(`/api/firms/${id}`, body);
    } else {
      id = await api.post<string>("/api/firms", body);
    }

    if (logoFile.value) {
      const data = new FormData();
      data.append("file", logoFile.value);
      await api.post(`/api/firms/${id}/logo`, data);
    } else if (removeLogo.value && editingFirm.value?.logoUrl) {
      await api.delete(`/api/firms/${id}/logo`);
    }

    toast.success(editingId.value ? t("firms.updated") : t("firms.created"));
    showForm.value = false;
    resetLogo();
    await Promise.all([load(), refreshBranding()]);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("firms.saveError")));
  } finally {
    saving.value = false;
  }
}

async function makeDefault(f: FirmDto) {
  try {
    await api.post(`/api/firms/${f.id}/default`);
    toast.success(t("firms.defaultSet", { name: f.name }));
    await Promise.all([load(), refreshBranding()]);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("firms.saveError")));
  }
}

async function remove(f: FirmDto) {
  if (!confirm(t("firms.deleteConfirm", { name: f.name }))) return;
  try {
    await api.delete(`/api/firms/${f.id}`);
    toast.success(t("firms.deleted"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("firms.deleteError")));
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("firms.title") }}</h1>
        <p class="text-sm text-slate-500">{{ $t("firms.subtitle") }}</p>
      </div>
      <AppButton @click="openCreate"><PlusIcon class="h-4 w-4" />{{ $t("firms.new") }}</AppButton>
    </div>

    <AppCard v-if="showForm" :title="editingId ? $t('firms.edit') : $t('firms.new')">
      <form class="space-y-6" @submit.prevent="save">
        <!-- Kimlik -->
        <section>
          <h3 class="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("firms.sections.identity") }}</h3>
          <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.code") }} *</label>
              <input v-model="form.code" required maxlength="30" class="w-full rounded-md border border-slate-300 px-3 py-1.5 font-mono text-sm uppercase focus:border-indigo-500 focus:outline-none" />
            </div>
            <div class="lg:col-span-2">
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.name") }} *</label>
              <input v-model="form.name" required maxlength="250" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.shortName") }}</label>
              <input v-model="form.shortName" maxlength="60" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div class="sm:col-span-2 lg:col-span-4">
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.tradeName") }}</label>
              <input v-model="form.tradeName" maxlength="300" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
          </div>
        </section>

        <!-- İletişim -->
        <section>
          <h3 class="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("firms.sections.contact") }}</h3>
          <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <div v-for="k in ['phone', 'phone2', 'mobilePhone', 'fax'] as const" :key="k">
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t(`firms.f.${k}`) }}</label>
              <input v-model="form[k]" type="tel" maxlength="30" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div v-for="k in ['email', 'email2'] as const" :key="k">
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t(`firms.f.${k}`) }}</label>
              <input v-model="form[k]" type="email" maxlength="200" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div class="sm:col-span-2">
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.website") }}</label>
              <input v-model="form.website" maxlength="250" placeholder="https://" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
          </div>
        </section>

        <!-- Fatura -->
        <section>
          <h3 class="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("firms.sections.billing") }}</h3>
          <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.taxOffice") }}</label>
              <input v-model="form.taxOffice" maxlength="150" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.taxNumber") }}</label>
              <input v-model="form.taxNumber" inputmode="numeric" maxlength="11" class="w-full rounded-md border border-slate-300 px-3 py-1.5 font-mono text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.billingCountry") }}</label>
              <input v-model="form.billingCountry" maxlength="100" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.billingCity") }}</label>
              <input v-model="form.billingCity" maxlength="100" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.billingDistrict") }}</label>
              <input v-model="form.billingDistrict" maxlength="100" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.billingPostalCode") }}</label>
              <input v-model="form.billingPostalCode" maxlength="20" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div class="sm:col-span-2">
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.billingAddressLine") }}</label>
              <input v-model="form.billingAddressLine" maxlength="300" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
          </div>
        </section>

        <!-- Marka -->
        <section>
          <h3 class="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("firms.sections.brand") }}</h3>
          <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div class="flex items-start gap-4">
              <div class="flex h-20 w-20 shrink-0 items-center justify-center overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
                <img v-if="currentLogo" :src="currentLogo" alt="" class="h-full w-full object-contain" />
                <PhotoIcon v-else class="h-8 w-8 text-slate-300" />
              </div>
              <div class="space-y-2">
                <label class="inline-flex cursor-pointer items-center gap-1.5 rounded-md border border-slate-300 bg-surface px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50">
                  <PhotoIcon class="h-4 w-4" /> {{ $t("firms.logoChoose") }}
                  <input type="file" accept="image/png,image/jpeg,image/webp,image/x-icon" class="hidden" @change="onLogoSelected" />
                </label>
                <button
                  v-if="currentLogo"
                  type="button"
                  class="block text-xs text-rose-600 hover:underline"
                  @click="resetLogo(); removeLogo = true"
                >
                  {{ $t("firms.logoRemove") }}
                </button>
                <p class="text-[11px] text-slate-400">{{ $t("firms.logoHint") }}</p>
              </div>
            </div>
            <div class="space-y-2">
              <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("firms.f.appTitleSuffix") }}</label>
              <input
                v-model="form.appTitleSuffix"
                maxlength="100"
                :placeholder="$t('firms.defaultSuffix')"
                class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none"
              />
              <p class="rounded-md bg-indigo-50 px-3 py-2 text-xs text-indigo-700">
                {{ $t("firms.titlePreview") }}: <strong>{{ titlePreview }}</strong>
              </p>
              <p class="text-[11px] text-slate-400">{{ $t("firms.titleHint") }}</p>
            </div>
          </div>
        </section>

        <label class="flex items-center gap-2 text-sm text-slate-700">
          <input v-model="form.isDefault" type="checkbox" class="rounded" :disabled="!!editingFirm?.isDefault" />
          {{ $t("firms.f.isDefault") }}
        </label>

        <div class="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <AppButton variant="secondary" :disabled="saving" @click="showForm = false; resetLogo()">{{ $t("common.cancel") }}</AppButton>
          <AppButton type="submit" :loading="saving">{{ $t("common.save") }}</AppButton>
        </div>
      </form>
    </AppCard>

    <AppCard no-padding>
      <LoadingSpinner v-if="loading" />
      <EmptyState v-else-if="firms.length === 0">{{ $t("firms.empty") }}</EmptyState>
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="border-b border-slate-100 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th class="px-4 py-3">{{ $t("firms.table.firm") }}</th>
              <th class="px-4 py-3">{{ $t("firms.table.contact") }}</th>
              <th class="px-4 py-3 text-center">{{ $t("firms.table.workspaces") }}</th>
              <th class="px-4 py-3 text-center">{{ $t("firms.table.default") }}</th>
              <th class="px-4 py-3 text-right">{{ $t("common.actions") }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-50">
            <tr v-for="f in firms" :key="f.id" class="hover:bg-slate-50">
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <div class="flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-slate-100 bg-slate-50">
                    <img
                      v-if="f.logoUrl && !brokenLogos.has(f.id)"
                      :src="assetUrl(f.logoUrl)!"
                      alt=""
                      class="h-full w-full object-contain"
                      @error="brokenLogos.add(f.id)"
                    />
                    <BuildingOffice2Icon v-else class="h-5 w-5 text-slate-300" />
                  </div>
                  <div class="min-w-0">
                    <div class="font-medium text-slate-800">{{ f.name }}</div>
                    <div class="text-xs text-slate-400"><span class="font-mono">{{ f.code }}</span><span v-if="f.shortName"> · {{ f.shortName }}</span></div>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3 text-xs text-slate-500">
                <div v-if="f.phone">{{ f.phone }}</div>
                <div v-if="f.email">{{ f.email }}</div>
                <div v-if="!f.phone && !f.email">—</div>
              </td>
              <td class="px-4 py-3 text-center">
                <span class="inline-flex rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-700">{{ f.workspaceCount }}</span>
              </td>
              <td class="px-4 py-3 text-center">
                <StarSolid v-if="f.isDefault" class="mx-auto h-4 w-4 text-amber-500" :title="$t('firms.default')" />
                <button v-else class="rounded p-1 text-slate-300 hover:text-amber-500" :title="$t('firms.makeDefault')" @click="makeDefault(f)">
                  <StarIcon class="h-4 w-4" />
                </button>
              </td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1">
                  <button class="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-indigo-600" :title="$t('common.edit')" @click="openEdit(f)">
                    <PencilIcon class="h-4 w-4" />
                  </button>
                  <button class="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600" :title="$t('common.delete')" @click="remove(f)">
                    <TrashIcon class="h-4 w-4" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </AppCard>
  </div>
</template>
