<script setup lang="ts">
import { useDebounceFn } from "@vueuse/core";
import type { CompanyDto, CurrencyDto, PagedResult } from "~/types/api";
import {
  ChevronLeftIcon, ChevronRightIcon, CheckCircleIcon, XMarkIcon,
  CubeIcon, BuildingStorefrontIcon, DocumentArrowUpIcon, TrashIcon, RocketLaunchIcon,
} from "@heroicons/vue/24/outline";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const router = useRouter();
const { workspaces, selectedWorkspaceId } = useWorkspace();

const {
  master, lines, attachments, activeStep,
  isVendorSelected, toggleVendor, selectVendors, removeVendor, clearVendors, vendorChipList, vendorCount,
  lineCount, validationErrors, isValid, buildPayload, restoreDraft, resetForm,
} = useTenderForm();

const AUCTION_TYPES = ["English", "ReverseSealed", "SealedBid"] as const;
const PAYMENT_TERMS = ["Advance", "Net15", "Net30", "Net60", "Net90"] as const;
const DELIVERY_TERMS = ["EXW", "FOB", "CIF", "DAP", "DDP"] as const;

const steps = [
  { n: 1, key: "header" },
  { n: 2, key: "vendors" },
  { n: 3, key: "lines" },
  { n: 4, key: "settings" },
  { n: 5, key: "review" },
];

// ---- Reference data ----
const currencies = ref<CurrencyDto[]>([]);
const ownerCompanies = ref<CompanyDto[]>([]);

async function loadReference() {
  try {
    const [curr, owners] = await Promise.all([
      api.get<CurrencyDto[]>("/api/currencies"),
      api.get<PagedResult<CompanyDto>>("/api/companies", { companyType: "Internal", page: 1, pageSize: 100 }),
    ]);
    currencies.value = curr;
    ownerCompanies.value = owners.items;
    if (!master.currencyId && curr.length) master.currencyId = (curr.find((c) => c.isBaseCurrency) ?? curr[0]).id;
    if (!master.ownerCompanyId && owners.items.length === 1) master.ownerCompanyId = owners.items[0].id;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("tender.wizard.refError")));
  }
}

// İhalenin işyeri: sahip firmanın aktif işyerleri; başlıkta seçili işyeri bu firmadaysa varsayılan odur.
const ownerWorkspaces = computed(() => workspaces.value.filter((w) => w.isActive && w.firmId === master.ownerCompanyId));
watch(
  () => [master.ownerCompanyId, workspaces.value.length],
  () => {
    if (master.workspaceId && ownerWorkspaces.value.some((w) => w.id === master.workspaceId)) return;
    const preferred = ownerWorkspaces.value.find((w) => w.id === selectedWorkspaceId.value)
      ?? ownerWorkspaces.value.find((w) => w.isDefault) ?? ownerWorkspaces.value[0];
    master.workspaceId = preferred?.id ?? "";
  },
  { immediate: true },
);

// ---- Vendor search (debounced + race guard) ----
const vendorTerm = ref("");
const vendorResults = ref<CompanyDto[]>([]);
const vendorLoading = ref(false);
let vReq = 0;
async function searchVendors(q: string) {
  const my = ++vReq;
  vendorLoading.value = true;
  try {
    const page = await api.get<PagedResult<CompanyDto>>("/api/companies", {
      searchTerm: q || undefined, companyType: "Supplier", page: 1, pageSize: 100,
    });
    if (my === vReq) vendorResults.value = page.items;
  } catch {
    if (my === vReq) vendorResults.value = [];
  } finally {
    if (my === vReq) vendorLoading.value = false;
  }
}
const debouncedVendorSearch = useDebounceFn((q: string) => searchVendors(q), 300);
watch(vendorTerm, (q) => debouncedVendorSearch(q));

function vendorChipOf(c: CompanyDto) { return { id: c.id, name: c.name, taxNumber: c.taxNumber }; }
function selectAllShown() { selectVendors(vendorResults.value.map(vendorChipOf)); }

// ---- Product pool drawer + line spec editor ----
const showPool = ref(false);
const editingSpecTempId = ref<string | null>(null);
const specDraft = ref("");
function openSpec(tempId: string) {
  const l = lines.value.find((x) => x.tempId === tempId);
  editingSpecTempId.value = tempId;
  specDraft.value = l?.specification ?? "";
}
function saveSpec() {
  const l = lines.value.find((x) => x.tempId === editingSpecTempId.value);
  if (l) l.specification = specDraft.value.trim() || null;
  editingSpecTempId.value = null;
}

// ---- Ekler: sunucuya gerçek yükleme (tür/boyut/içerik sunucuda doğrulanır) ----
const { upload } = useFileUpload();
async function handleFiles(fileList: FileList | null | undefined) {
  if (!fileList) return;
  for (const f of Array.from(fileList)) {
    const draft = reactive({
      tempId: crypto.randomUUID(),
      fileName: f.name,
      url: "",
      contentType: f.type || "application/octet-stream",
      sizeBytes: f.size,
      status: "uploading" as const,
    });
    attachments.value.push(draft as any);
    try {
      const saved = await upload(f, "auction-attachment");
      const a = attachments.value.find((x) => x.tempId === draft.tempId);
      if (a) Object.assign(a, { url: saved.url, fileName: saved.fileName, contentType: saved.contentType, sizeBytes: saved.sizeBytes, status: "done" });
    } catch (error: any) {
      attachments.value = attachments.value.filter((x) => x.tempId !== draft.tempId);
      toast.error(`${f.name}: ${apiErrorMessage(error, t("upload.error"))}`);
    }
  }
}
const uploadingAttachments = computed(() => attachments.value.some((a) => a.status === "uploading"));
function removeAttachment(tempId: string) {
  attachments.value = attachments.value.filter((a) => a.tempId !== tempId);
}
const dragOver = ref(false);

// ---- Step validation + navigation ----
const stepValid = computed<Record<number, boolean>>(() => ({
  1: !!master.title.trim() && !!master.ownerCompanyId && !!master.currencyId && !!master.startAt && !!master.endAt
    && new Date(master.endAt) > new Date(master.startAt)
    && (!master.sniper.enabled || master.sniper.durationMinutes > 0),
  2: vendorCount.value > 0,
  3: lineCount.value > 0 && lines.value.every((l) => l.description.trim() && l.quantity > 0),
  4: true,
  5: isValid.value,
}));
function next() { if (stepValid.value[activeStep.value] && activeStep.value < 5) activeStep.value++; }
function prev() { if (activeStep.value > 1) activeStep.value--; }
function goTo(n: number) { if (n <= activeStep.value || stepValid.value[activeStep.value]) activeStep.value = n; }

// ---- Submit ----
const submitting = ref(false);
async function submit() {
  if (!isValid.value || submitting.value) return;
  submitting.value = true; // çift tıklamada mükerrer ihale açılmasın
  try {
    const id = await api.post<string>("/api/auctions/tender", buildPayload());
    toast.success(t("tender.wizard.created"));
    resetForm();
    await router.push(`/admin/auctions/${id}`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("tender.wizard.createError")));
    submitting.value = false; // başarıda true kalır (yönlendirme sürerken kilitli)
  }
}

onMounted(async () => {
  restoreDraft();
  await Promise.all([loadReference(), searchVendors("")]);
});
</script>

<template>
  <div class="mx-auto max-w-4xl space-y-5">
    <div>
      <NuxtLink to="/admin/auctions" class="text-xs text-indigo-600 hover:underline">← {{ $t("nav.auctions") }}</NuxtLink>
      <h1 class="mt-1 text-xl font-bold text-slate-800">{{ $t("tender.wizard.title") }}</h1>
    </div>

    <!-- Step indicator -->
    <div class="flex items-center gap-1 overflow-x-auto">
      <template v-for="(s, i) in steps" :key="s.n">
        <button
          type="button"
          class="flex shrink-0 items-center gap-2 rounded-lg px-3 py-1.5 text-xs font-medium transition-colors"
          :class="activeStep === s.n ? 'bg-indigo-600 text-white' : stepValid[s.n] && s.n < activeStep ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'"
          @click="goTo(s.n)"
        >
          <CheckCircleIcon v-if="s.n < activeStep && stepValid[s.n]" class="h-3.5 w-3.5" />
          <span v-else class="flex h-4 w-4 items-center justify-center rounded-full bg-surface/30 text-[10px]">{{ s.n }}</span>
          {{ $t(`tender.wizard.steps.${s.key}`) }}
        </button>
        <ChevronRightIcon v-if="i < steps.length - 1" class="h-3 w-3 shrink-0 text-slate-300" />
      </template>
    </div>

    <!-- STEP 1: Header -->
    <AppCard v-show="activeStep === 1" :title="$t('tender.wizard.steps.header')">
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div class="sm:col-span-2">
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.title") }} *</label>
          <input v-model="master.title" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" :placeholder="$t('tender.wizard.f.titlePlaceholder')" />
        </div>
        <div>
          <label class="mb-1 flex items-center justify-between text-xs font-medium text-slate-600">
            {{ $t("tender.wizard.f.referenceCode") }}
            <label class="flex items-center gap-1 font-normal text-slate-400"><input v-model="master.autoReference" type="checkbox" class="rounded" />{{ $t("tender.wizard.f.auto") }}</label>
          </label>
          <input v-model="master.referenceCode" :disabled="master.autoReference" :placeholder="master.autoReference ? $t('tender.wizard.f.autoHint') : 'IH-2026-0001'" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50 disabled:text-slate-400" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.auctionType") }}</label>
          <select v-model="master.auctionType" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option v-for="a in AUCTION_TYPES" :key="a" :value="a">{{ $t(`enums.auctionType.${a}`) }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.owner") }} *</label>
          <select v-model="master.ownerCompanyId" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option value="" disabled>{{ $t("tender.wizard.f.selectOwner") }}</option>
            <option v-for="c in ownerCompanies" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.workspace") }}</label>
          <select v-model="master.workspaceId" :disabled="!ownerWorkspaces.length" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50">
            <option value="">{{ ownerWorkspaces.length ? $t("tender.wizard.f.workspaceDefault") : $t("tender.wizard.f.noWorkspace") }}</option>
            <option v-for="w in ownerWorkspaces" :key="w.id" :value="w.id">{{ w.name }}</option>
          </select>
          <p class="mt-1 text-[11px] text-slate-400">{{ $t("tender.wizard.f.workspaceHint") }}</p>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.currency") }} *</label>
          <select v-model="master.currencyId" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.startAt") }} *</label>
          <input v-model="master.startAt" type="datetime-local" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.endAt") }} *</label>
          <input v-model="master.endAt" type="datetime-local" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.reserve") }}</label>
          <input v-model.number="master.reservePrice" type="number" min="0" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" :placeholder="$t('tender.grid.optional')" />
        </div>
      </div>

      <!-- Sniper protection -->
      <div class="mt-4 rounded-lg border border-slate-200 p-3">
        <label class="flex items-center gap-2 text-sm font-medium text-slate-700">
          <input v-model="master.sniper.enabled" type="checkbox" class="rounded" />
          {{ $t("tender.wizard.sniper.title") }}
        </label>
        <p class="mt-1 text-xs text-slate-400">{{ $t("tender.wizard.sniper.hint") }}</p>
        <div v-if="master.sniper.enabled" class="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div>
            <label class="mb-1 block text-xs text-slate-500">{{ $t("tender.wizard.sniper.trigger") }}</label>
            <input v-model.number="master.sniper.triggerMinutes" type="number" min="1" class="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          </div>
          <div>
            <label class="mb-1 block text-xs text-slate-500">{{ $t("tender.wizard.sniper.duration") }}</label>
            <input v-model.number="master.sniper.durationMinutes" type="number" min="1" class="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          </div>
          <div>
            <label class="mb-1 block text-xs text-slate-500">{{ $t("tender.wizard.sniper.max") }}</label>
            <input v-model.number="master.sniper.maxExtensionMinutes" type="number" min="0" class="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          </div>
        </div>
        <p v-if="master.sniper.enabled" class="mt-2 text-xs text-indigo-600">
          {{ $t("tender.wizard.sniper.summary", { x: master.sniper.triggerMinutes, y: master.sniper.durationMinutes, z: master.sniper.maxExtensionMinutes }) }}
        </p>
      </div>
    </AppCard>

    <!-- STEP 2: Vendors -->
    <AppCard v-show="activeStep === 2" :title="$t('tender.wizard.steps.vendors')">
      <div class="mb-3 flex flex-wrap items-center gap-2">
        <div class="relative min-w-[220px] flex-1">
          <BuildingStorefrontIcon class="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />
          <input v-model="vendorTerm" :placeholder="$t('tender.wizard.vendors.search')" class="w-full rounded-lg border border-slate-300 py-2 pl-8 pr-3 text-sm" />
        </div>
        <button type="button" class="rounded-md border border-slate-300 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50" @click="selectAllShown">{{ $t("tender.wizard.vendors.selectAll") }}</button>
        <button type="button" class="rounded-md border border-slate-300 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50" @click="clearVendors">{{ $t("tender.wizard.vendors.clear") }}</button>
      </div>

      <!-- Selected chips -->
      <div v-if="vendorChipList.length" class="mb-3 flex flex-wrap gap-1.5">
        <span v-for="v in vendorChipList" :key="v.id" class="inline-flex items-center gap-1 rounded-full bg-indigo-50 py-0.5 pl-2.5 pr-1 text-xs font-medium text-indigo-700">
          {{ v.name }}
          <button type="button" class="rounded-full p-0.5 hover:bg-indigo-100" @click="removeVendor(v.id)"><XMarkIcon class="h-3 w-3" /></button>
        </span>
      </div>

      <p class="mb-2 text-[11px] text-slate-400">{{ vendorLoading ? $t("tender.pool.searching") : $t("tender.wizard.vendors.count", { shown: vendorResults.length, selected: vendorCount }) }}</p>
      <div class="max-h-80 divide-y divide-slate-50 overflow-y-auto rounded-lg border border-slate-100">
        <label v-for="c in vendorResults" :key="c.id" class="flex cursor-pointer items-center gap-3 px-3 py-2 hover:bg-slate-50">
          <input type="checkbox" class="rounded" :checked="isVendorSelected(c.id)" @change="toggleVendor(vendorChipOf(c))" />
          <div class="min-w-0 flex-1">
            <div class="truncate text-sm font-medium text-slate-700">{{ c.name }}</div>
            <div class="truncate text-[11px] text-slate-400"><span class="font-mono">{{ c.taxNumber }}</span><span v-if="c.averageScore != null"> · ★ {{ c.averageScore.toFixed(1) }}</span></div>
          </div>
        </label>
        <EmptyState v-if="!vendorResults.length && !vendorLoading" class="py-6">{{ $t("tender.wizard.vendors.empty") }}</EmptyState>
      </div>
    </AppCard>

    <!-- STEP 3: Lines -->
    <div v-show="activeStep === 3" class="space-y-3">
      <div class="flex items-center justify-between">
        <h2 class="text-sm font-semibold text-slate-700">{{ $t("tender.wizard.steps.lines") }}</h2>
        <button type="button" class="flex items-center gap-1.5 rounded-lg bg-slate-800 px-3 py-1.5 text-sm font-semibold text-white hover:bg-slate-700" @click="showPool = true">
          <CubeIcon class="h-4 w-4" />{{ $t("tender.wizard.openPool") }}
        </button>
      </div>
      <TenderLineItemsDataGrid @edit-spec="openSpec" />
    </div>

    <!-- STEP 4: Settings -->
    <AppCard v-show="activeStep === 4" :title="$t('tender.wizard.steps.settings')">
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.paymentTerm") }}</label>
          <select v-model="master.paymentTerm" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option value="">{{ $t("tender.wizard.f.none") }}</option>
            <option v-for="p in PAYMENT_TERMS" :key="p" :value="p">{{ $t(`enums.paymentTerm.${p}`) }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.deliveryTerm") }}</label>
          <select v-model="master.deliveryTerm" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option value="">{{ $t("tender.wizard.f.none") }}</option>
            <option v-for="d in DELIVERY_TERMS" :key="d" :value="d">{{ $t(`enums.deliveryTerm.${d}`) }}</option>
          </select>
        </div>
      </div>

      <div class="mt-4 space-y-2">
        <label class="flex items-center gap-2 text-sm text-slate-700"><input v-model="master.allowAlternativeProducts" type="checkbox" class="rounded" />{{ $t("tender.wizard.f.allowAlternatives") }}</label>
        <label class="flex items-center gap-2 text-sm text-slate-700"><input v-model="master.allowPartialBids" type="checkbox" class="rounded" />{{ $t("tender.wizard.f.allowPartial") }}</label>
        <label class="flex items-start gap-2 text-sm text-slate-700">
          <input v-model="master.isListedForPlus" type="checkbox" class="mt-0.5 rounded" />
          <span>
            {{ $t("tender.wizard.f.listedForPlus") }}
            <span class="block text-xs text-slate-400">{{ $t("tender.wizard.f.listedForPlusHint") }}</span>
          </span>
        </label>
      </div>

      <!-- Attachments (simulated) -->
      <div class="mt-5">
        <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.attachments") }}</label>
        <div
          class="flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-6 text-center transition-colors"
          :class="dragOver ? 'border-indigo-400 bg-indigo-50/40' : 'border-slate-200'"
          @dragover.prevent="dragOver = true" @dragleave.prevent="dragOver = false"
          @drop.prevent="dragOver = false; handleFiles($event.dataTransfer?.files)"
        >
          <DocumentArrowUpIcon class="h-6 w-6 text-slate-400" />
          <p class="mt-1 text-xs text-slate-500">{{ $t("tender.wizard.f.dropHint") }}</p>
          <label class="mt-2 cursor-pointer rounded-md bg-slate-100 px-3 py-1 text-xs font-medium text-slate-600 hover:bg-slate-200">
            {{ $t("tender.wizard.f.chooseFiles") }}
            <input type="file" multiple class="hidden" @change="handleFiles(($event.target as HTMLInputElement).files)" />
          </label>
          <p class="mt-1 text-[10px] text-slate-400">{{ $t("upload.limits") }}</p>
        </div>
        <ul v-if="attachments.length" class="mt-2 space-y-1">
          <li v-for="a in attachments" :key="a.tempId" class="flex items-center justify-between rounded-md border border-slate-100 px-2.5 py-1.5 text-xs">
            <span class="truncate text-slate-600">{{ a.fileName }} <span class="text-slate-400">({{ Math.round(a.sizeBytes / 1024) }} KB)</span>
              <span v-if="a.status === 'uploading'" class="ml-1 text-indigo-600">{{ $t("upload.uploading") }}</span></span>
            <button type="button" class="rounded p-0.5 text-slate-400 hover:text-red-600" @click="removeAttachment(a.tempId)"><TrashIcon class="h-3.5 w-3.5" /></button>
          </li>
        </ul>
      </div>
    </AppCard>

    <!-- STEP 5: Review -->
    <AppCard v-show="activeStep === 5" :title="$t('tender.wizard.steps.review')">
      <dl class="grid grid-cols-2 gap-x-6 gap-y-2 text-sm sm:grid-cols-3">
        <div><dt class="text-xs text-slate-400">{{ $t("tender.wizard.f.title") }}</dt><dd class="font-medium text-slate-700">{{ master.title || "—" }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("tender.wizard.f.auctionType") }}</dt><dd class="text-slate-700">{{ $t(`enums.auctionType.${master.auctionType}`) }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("tender.wizard.steps.vendors") }}</dt><dd class="text-slate-700">{{ vendorCount }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("tender.grid.col.item") }}</dt><dd class="text-slate-700">{{ lineCount }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("tender.wizard.f.attachments") }}</dt><dd class="text-slate-700">{{ attachments.length }}</dd></div>
        <div><dt class="text-xs text-slate-400">{{ $t("tender.wizard.sniper.title") }}</dt><dd class="text-slate-700">{{ master.sniper.enabled ? `${master.sniper.triggerMinutes}/${master.sniper.durationMinutes}/${master.sniper.maxExtensionMinutes} dk` : $t("tender.wizard.f.none") }}</dd></div>
      </dl>
      <p v-if="!isValid" class="mt-3 rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-700">{{ $t("tender.wizard.fixErrors") }}</p>
      <button
        type="button" :disabled="!isValid || submitting || uploadingAttachments"
        class="mt-4 flex items-center gap-2 rounded-lg bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
        @click="submit"
      >
        <RocketLaunchIcon class="h-4 w-4" />
        {{ submitting ? $t("tender.wizard.creating") : $t("tender.wizard.submit") }}
      </button>
    </AppCard>

    <!-- Nav -->
    <div class="flex items-center justify-between">
      <button type="button" :disabled="activeStep === 1" class="flex items-center gap-1 rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 disabled:opacity-40" @click="prev">
        <ChevronLeftIcon class="h-4 w-4" />{{ $t("common.back") }}
      </button>
      <button v-if="activeStep < 5" type="button" :disabled="!stepValid[activeStep]" class="flex items-center gap-1 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50" @click="next">
        {{ $t("common.next") }}<ChevronRightIcon class="h-4 w-4" />
      </button>
    </div>

    <!-- Product pool drawer -->
    <TenderProductPoolDrawer v-if="showPool" @close="showPool = false" />

    <!-- Line spec editor -->
    <div v-if="editingSpecTempId" class="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div class="absolute inset-0 bg-black/40" @click="editingSpecTempId = null" />
      <div class="relative w-full max-w-lg rounded-xl bg-surface p-4 shadow-xl">
        <h3 class="mb-2 text-sm font-semibold text-slate-800">{{ $t("tender.grid.col.spec") }}</h3>
        <textarea v-model="specDraft" rows="6" maxlength="4000" :placeholder="$t('tender.wizard.specPlaceholder')" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500" />
        <div class="mt-3 flex justify-end gap-2">
          <button type="button" class="rounded-md px-4 py-1.5 text-sm text-slate-600 hover:bg-slate-100" @click="editingSpecTempId = null">{{ $t("common.cancel") }}</button>
          <button type="button" class="rounded-md bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-indigo-700" @click="saveSpec">{{ $t("common.save") }}</button>
        </div>
      </div>
    </div>
  </div>
</template>
