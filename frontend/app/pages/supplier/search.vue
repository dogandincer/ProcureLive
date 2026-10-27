<script setup lang="ts">
import type { CompanySearchItemDto, ProductSearchItemDto, SearchResultDto } from "~/types/api";
import { useDebounceFn } from "@vueuse/core";
import {
  MagnifyingGlassIcon,
  MapPinIcon,
  EnvelopeIcon,
  PhoneIcon,
  GlobeAltIcon,
  UsersIcon,
  CubeIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import { LockClosedIcon, StarIcon } from "@heroicons/vue/24/solid";

type Tab = "all" | "companies" | "tenders" | "products";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();
const { subscription, isPlus, refresh: refreshSubscription } = useSubscription();

const term = ref("");
const tab = ref<Tab>("all");
const auctionType = ref("");
const minParticipants = ref<number | "" | null>(null);
const maxParticipants = ref<number | "" | null>(null);
const sort = ref("endingSoon");
const productFilter = ref<{ id: string; name: string } | null>(null);
const ownerFilter = ref<{ id: string; name: string } | null>(null);

const result = ref<SearchResultDto>({ companies: [], tenders: [], products: [] });
const searching = ref(false);
const gated = ref(false);
const ready = ref(false);

const auctionTypes = ["English", "Dutch", "ReverseSealed", "SealedBid"];
const sorts = ["endingSoon", "newest", "participantsDesc", "participantsAsc"];

let reqId = 0;

// v-model.number boş kutuyu "" bırakır; yalnızca gerçek sayıları gönder.
const num = (v: unknown) => (typeof v === "number" && Number.isFinite(v) ? v : undefined);

async function runSearch() {
  const id = ++reqId;
  searching.value = true;
  try {
    const data = await api.get<SearchResultDto>("/api/search", {
      q: term.value.trim() || undefined,
      scope: tab.value,
      auctionType: auctionType.value || undefined,
      minParticipants: num(minParticipants.value),
      maxParticipants: num(maxParticipants.value),
      productId: productFilter.value?.id,
      ownerCompanyId: ownerFilter.value?.id,
      sort: sort.value,
      take: tab.value === "all" ? 6 : 50,
    });
    // Yalnızca en son isteğin cevabı ekrana yazılır (yazarken gelen eski cevaplar yarışı bozmasın).
    if (id !== reqId) return;
    result.value = data;
    gated.value = false;
  } catch (error: any) {
    if (id !== reqId) return;
    if (apiErrorCode(error) === "PlusRequired") {
      gated.value = true;
    } else {
      toast.error(apiErrorMessage(error, t("search.loadError")));
    }
  } finally {
    if (id === reqId) searching.value = false;
  }
}

const debouncedSearch = useDebounceFn(runSearch, 300);

watch(term, () => debouncedSearch());
watch([tab, auctionType, minParticipants, maxParticipants, sort, productFilter, ownerFilter], () => runSearch());

function showTendersForProduct(p: ProductSearchItemDto) {
  productFilter.value = { id: p.id, name: p.name };
  ownerFilter.value = null;
  tab.value = "tenders";
}

function showTendersForCompany(c: CompanySearchItemDto) {
  ownerFilter.value = { id: c.id, name: c.name };
  productFilter.value = null;
  tab.value = "tenders";
}

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0]!.toLocaleUpperCase("tr-TR"))
    .join("");
}

const statusTone: Record<string, "amber" | "emerald" | "slate"> = { Scheduled: "amber", Live: "emerald" };

const tabs = computed<{ key: Tab; count: number | null }[]>(() => [
  { key: "all", count: null },
  { key: "companies", count: tab.value === "all" || tab.value === "companies" ? result.value.companies.length : null },
  { key: "tenders", count: tab.value === "all" || tab.value === "tenders" ? result.value.tenders.length : null },
  { key: "products", count: tab.value === "all" || tab.value === "products" ? result.value.products.length : null },
]);

const show = (section: Exclude<Tab, "all">) => tab.value === "all" || tab.value === section;
const nothingFound = computed(
  () => !searching.value && result.value.companies.length + result.value.tenders.length + result.value.products.length === 0,
);

onMounted(async () => {
  await refreshSubscription();
  // Plus değilse sunucu zaten 403 (PlusRequired) döner; isteği atmadan kapıyı göster.
  if (subscription.value && !isPlus.value) {
    gated.value = true;
  } else {
    await runSearch();
  }
  ready.value = true;
});
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("search.title") }}</h1>
        <p class="mt-0.5 text-sm text-slate-500">{{ $t("search.subtitle") }}</p>
      </div>
      <AppBadge tone="amber">Plus</AppBadge>
    </div>

    <!-- Plus kapısı: bulanık örnek sonuçlar + yükseltme -->
    <RestrictedOverlay v-if="gated" level="PlusRequired" :rows="5" />

    <template v-else-if="ready">
      <!-- Arama çubuğu -->
      <div class="relative">
        <MagnifyingGlassIcon class="pointer-events-none absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400" />
        <input
          v-model="term"
          type="search"
          :placeholder="$t('search.placeholder')"
          class="w-full rounded-xl border border-slate-300 bg-white py-3 pl-10 pr-4 text-sm shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-100"
        />
        <svg v-if="searching" class="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-slate-400" viewBox="0 0 24 24" fill="none">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
        </svg>
      </div>

      <!-- Sekmeler -->
      <div class="flex flex-wrap gap-1 border-b border-slate-200">
        <button
          v-for="x in tabs"
          :key="x.key"
          class="-mb-px border-b-2 px-4 py-2 text-sm font-medium transition-colors"
          :class="tab === x.key ? 'border-indigo-600 text-indigo-700' : 'border-transparent text-slate-500 hover:text-slate-700'"
          @click="tab = x.key"
        >
          {{ $t(`search.tabs.${x.key}`) }}
          <span v-if="x.count !== null" class="ml-1 rounded-full bg-slate-100 px-1.5 text-xs text-slate-500">{{ x.count }}</span>
        </button>
      </div>

      <!-- İhale filtreleri -->
      <div v-if="show('tenders')" class="flex flex-wrap items-end gap-3 rounded-xl border border-slate-200 bg-white p-4">
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-500">{{ $t("search.filters.auctionType") }}</label>
          <select v-model="auctionType" class="rounded-md border border-slate-300 px-3 py-1.5 text-sm">
            <option value="">{{ $t("search.filters.allTypes") }}</option>
            <option v-for="x in auctionTypes" :key="x" :value="x">{{ $t(`enums.auctionType.${x}`) }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-500">{{ $t("search.filters.participants") }}</label>
          <div class="flex items-center gap-1.5">
            <input v-model.number="minParticipants" type="number" min="0" :placeholder="$t('search.filters.min')" class="w-20 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
            <span class="text-slate-400">–</span>
            <input v-model.number="maxParticipants" type="number" min="0" :placeholder="$t('search.filters.max')" class="w-20 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
          </div>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-500">{{ $t("search.filters.sort") }}</label>
          <select v-model="sort" class="rounded-md border border-slate-300 px-3 py-1.5 text-sm">
            <option v-for="x in sorts" :key="x" :value="x">{{ $t(`search.sorts.${x}`) }}</option>
          </select>
        </div>
        <div class="flex flex-wrap gap-2">
          <span v-if="productFilter" class="inline-flex items-center gap-1 rounded-full bg-indigo-50 px-3 py-1 text-xs font-medium text-indigo-700">
            {{ $t("search.filters.product") }}: {{ productFilter.name }}
            <button class="rounded-full hover:bg-indigo-100" @click="productFilter = null"><XMarkIcon class="h-3.5 w-3.5" /></button>
          </span>
          <span v-if="ownerFilter" class="inline-flex items-center gap-1 rounded-full bg-indigo-50 px-3 py-1 text-xs font-medium text-indigo-700">
            {{ $t("search.filters.owner") }}: {{ ownerFilter.name }}
            <button class="rounded-full hover:bg-indigo-100" @click="ownerFilter = null"><XMarkIcon class="h-3.5 w-3.5" /></button>
          </span>
        </div>
      </div>

      <EmptyState v-if="nothingFound" icon="🔎">{{ $t("search.empty") }}</EmptyState>

      <!-- Şirketler -->
      <section v-if="show('companies') && result.companies.length > 0" class="space-y-3">
        <h2 v-if="tab === 'all'" class="text-sm font-semibold uppercase tracking-wide text-slate-500">{{ $t("search.tabs.companies") }}</h2>
        <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          <div v-for="c in result.companies" :key="c.id" class="flex flex-col rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <div class="flex items-start gap-3">
              <img v-if="c.logoUrl" :src="c.logoUrl" :alt="c.name" class="h-12 w-12 shrink-0 rounded-lg border border-slate-100 object-contain" />
              <div v-else class="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-indigo-100 text-sm font-bold text-indigo-700">
                {{ initials(c.name) }}
              </div>
              <div class="min-w-0 flex-1">
                <div class="truncate font-semibold text-slate-800">{{ c.name }}</div>
                <div v-if="c.tradeName" class="truncate text-xs text-slate-400">{{ c.tradeName }}</div>
                <div class="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-slate-500">
                  <span v-if="c.shortAddress" class="inline-flex items-center gap-1"><MapPinIcon class="h-3.5 w-3.5" />{{ c.shortAddress }}</span>
                  <span v-if="c.averageScore !== null" class="inline-flex items-center gap-1"><StarIcon class="h-3.5 w-3.5 text-amber-400" />{{ c.averageScore.toFixed(2) }}</span>
                  <AppBadge :tone="c.companyType === 'Internal' ? 'indigo' : 'slate'">{{ $t(`search.companyType.${c.companyType}`) }}</AppBadge>
                </div>
              </div>
            </div>

            <div v-if="c.sectors.length" class="mt-3 flex flex-wrap gap-1">
              <span v-for="s in c.sectors.slice(0, 4)" :key="s" class="rounded bg-slate-100 px-2 py-0.5 text-[11px] text-slate-600">{{ s }}</span>
            </div>

            <!-- KVKK: maskeli değerler sunucudan böyle gelir; ham veri istemciye ulaşmaz -->
            <ul class="mt-4 space-y-1.5 text-xs text-slate-600">
              <li v-if="c.email" class="flex items-center gap-2"><EnvelopeIcon class="h-3.5 w-3.5 text-slate-400" /><span class="font-mono">{{ c.email }}</span></li>
              <li v-if="c.phone" class="flex items-center gap-2"><PhoneIcon class="h-3.5 w-3.5 text-slate-400" /><span class="font-mono">{{ c.phone }}</span></li>
              <li v-if="c.website" class="flex items-center gap-2">
                <GlobeAltIcon class="h-3.5 w-3.5 text-slate-400" />
                <a :href="c.website" target="_blank" rel="noopener noreferrer" class="truncate text-indigo-600 hover:underline">{{ c.website }}</a>
              </li>
              <li v-if="c.contactMasked" class="flex items-center gap-1.5 text-[11px] text-amber-700" :title="$t('search.maskedTooltip')">
                <LockClosedIcon class="h-3 w-3" />{{ $t("search.maskedNote") }}
              </li>
            </ul>

            <button
              class="mt-4 self-start text-xs font-semibold text-indigo-600 hover:underline disabled:cursor-default disabled:text-slate-400 disabled:no-underline"
              :disabled="c.openTenderCount === 0"
              @click="showTendersForCompany(c)"
            >
              {{ $t("search.openTenders", { n: c.openTenderCount }) }} →
            </button>
          </div>
        </div>
      </section>

      <!-- İhaleler -->
      <section v-if="show('tenders') && result.tenders.length > 0" class="space-y-3">
        <h2 v-if="tab === 'all'" class="text-sm font-semibold uppercase tracking-wide text-slate-500">{{ $t("search.tabs.tenders") }}</h2>
        <div class="overflow-hidden rounded-xl border border-slate-200 bg-white">
          <NuxtLink
            v-for="x in result.tenders"
            :key="x.id"
            :to="`/supplier/auctions/${x.id}`"
            class="flex flex-wrap items-center gap-4 border-b border-slate-50 px-5 py-4 last:border-0 hover:bg-slate-50"
          >
            <img v-if="x.ownerLogoUrl" :src="x.ownerLogoUrl" :alt="x.ownerCompanyName" class="h-10 w-10 shrink-0 rounded-lg border border-slate-100 object-contain" />
            <div v-else class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-xs font-bold text-slate-600">{{ initials(x.ownerCompanyName) }}</div>
            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-semibold text-slate-800">{{ x.title }}</span>
                <AppBadge :tone="statusTone[x.status] ?? 'slate'">{{ $t(`enums.auctionStatus.${x.status}`) }}</AppBadge>
                <AppBadge v-if="x.isJoined" tone="emerald">{{ $t("auctions.supplierList.joined") }}</AppBadge>
              </div>
              <div class="mt-0.5 text-xs text-slate-500">
                {{ x.ownerCompanyName }}<span v-if="x.referenceCode" class="font-mono"> · {{ x.referenceCode }}</span>
                · {{ $t(`enums.auctionType.${x.auctionType}`) }}
              </div>
            </div>
            <div class="flex items-center gap-5 text-xs text-slate-500">
              <span class="inline-flex items-center gap-1" :title="$t('search.itemCount')"><CubeIcon class="h-4 w-4" />{{ x.itemCount }}</span>
              <span class="inline-flex items-center gap-1" :title="$t('search.participantCount')"><UsersIcon class="h-4 w-4" />{{ x.participantCount }}</span>
              <span class="w-36 text-right">{{ $t("search.endsAt") }}: {{ formatDateTime(x.endAt) }}</span>
            </div>
          </NuxtLink>
        </div>
      </section>

      <!-- Ürünler -->
      <section v-if="show('products') && result.products.length > 0" class="space-y-3">
        <h2 v-if="tab === 'all'" class="text-sm font-semibold uppercase tracking-wide text-slate-500">{{ $t("search.tabs.products") }}</h2>
        <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          <div v-for="p in result.products" :key="p.id" class="flex items-center gap-3 rounded-xl border border-slate-200 bg-white p-4">
            <img v-if="p.thumbnailUrl" :src="p.thumbnailUrl" :alt="p.name" class="h-14 w-14 shrink-0 rounded-lg border border-slate-100 object-cover" />
            <div v-else class="flex h-14 w-14 shrink-0 items-center justify-center rounded-lg bg-slate-100"><CubeIcon class="h-6 w-6 text-slate-400" /></div>
            <div class="min-w-0 flex-1">
              <div class="truncate font-medium text-slate-800">{{ p.name }}</div>
              <div class="truncate text-xs text-slate-500"><span class="font-mono">{{ p.sku }}</span> · {{ p.categoryName }} · {{ $t(`enums.unitOfMeasure.${p.unitOfMeasure}`) }}</div>
              <button
                class="mt-1 text-xs font-semibold text-indigo-600 hover:underline disabled:cursor-default disabled:text-slate-400 disabled:no-underline"
                :disabled="p.openTenderCount === 0"
                @click="showTendersForProduct(p)"
              >
                {{ $t("search.openTenders", { n: p.openTenderCount }) }} →
              </button>
            </div>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>
