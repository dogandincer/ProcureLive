<script setup lang="ts">
import { useDebounceFn } from "@vueuse/core";
import { useVirtualizer } from "@tanstack/vue-virtual";
import type { PagedResult, ProductDto } from "~/types/api";
import type { ProductPoolItem } from "~/composables/useTenderForm";
import { MagnifyingGlassIcon, XMarkIcon, PlusIcon, CheckIcon } from "@heroicons/vue/24/outline";

const emit = defineEmits<{ (e: "close"): void }>();

const api = useApi();
const { t } = useI18n();
const { addLineFromProduct, hasProduct } = useTenderForm();

const term = ref("");
const results = ref<ProductDto[]>([]);
const loading = ref(false);

// Ardışık aramalarda eski cevabın yeniyi ezmesini engelle (race guard).
let reqId = 0;

async function runSearch(q: string) {
  const my = ++reqId;
  loading.value = true;
  try {
    // Server-side arama: binlerce ürün frontend'e inmez; sayfa başı 50.
    const page = await api.get<PagedResult<ProductDto>>("/api/products", {
      searchTerm: q || undefined,
      isActive: true,
      page: 1,
      pageSize: 50,
    });
    if (my === reqId) results.value = page.items; // yalnızca en son isteğin sonucu yazılır
  } catch (error: any) {
    if (my === reqId) results.value = [];
  } finally {
    if (my === reqId) loading.value = false;
  }
}

const debouncedSearch = useDebounceFn((q: string) => runSearch(q), 300);
watch(term, (q) => debouncedSearch(q));

const assetUrl = useAssetUrl();
function thumb(p: ProductDto): string | null {
  if (!p.images || p.images.length === 0) return null;
  return assetUrl((p.images.find((i) => i.isPrimary) ?? p.images[0]).url);
}

function add(p: ProductDto) {
  const item: ProductPoolItem = { id: p.id, sku: p.sku, name: p.name, thumbnailUrl: thumb(p) };
  addLineFromProduct(item);
}

// ---- Sonuç listesi de sanallaştırılır (500 ürün dönse bile DOM kasmaz) ----
const parentRef = ref<HTMLElement | null>(null);
const rowVirtualizer = useVirtualizer(
  computed(() => ({
    count: results.value.length,
    getScrollElement: () => parentRef.value,
    estimateSize: () => 64,
    overscan: 12,
  })),
);
const virtualRows = computed(() => rowVirtualizer.value.getVirtualItems());
const totalSize = computed(() => rowVirtualizer.value.getTotalSize());

// Esc ile kapanır (diğer pencerelerle aynı davranış).
function onKey(e: KeyboardEvent) {
  if (e.key === "Escape") emit("close");
}
onMounted(() => {
  runSearch("");
  document.addEventListener("keydown", onKey);
});
onBeforeUnmount(() => document.removeEventListener("keydown", onKey));
</script>

<template>
  <div class="fixed inset-0 z-50 flex justify-end">
    <div class="absolute inset-0 bg-black/40 backdrop-blur-sm" @click="emit('close')" />

    <aside class="relative flex h-full w-full max-w-md flex-col bg-surface shadow-xl">
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-slate-100 px-4 py-3">
        <h3 class="text-sm font-semibold text-slate-800">{{ $t("tender.pool.title") }}</h3>
        <button type="button" class="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700" :aria-label="$t('common.close')" @click="emit('close')">
          <XMarkIcon class="h-5 w-5" />
        </button>
      </div>

      <!-- Search -->
      <div class="border-b border-slate-100 p-3">
        <div class="relative">
          <MagnifyingGlassIcon class="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />
          <input
            v-model="term"
            :placeholder="$t('tender.pool.searchPlaceholder')"
            class="w-full rounded-lg border border-slate-300 py-2 pl-8 pr-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />
        </div>
        <p class="mt-1.5 text-[11px] text-slate-400">
          {{ loading ? $t("tender.pool.searching") : $t("tender.pool.resultCount", { n: results.length }) }}
        </p>
      </div>

      <!-- Virtualized results -->
      <div v-if="results.length === 0 && !loading" class="flex-1 p-8">
        <EmptyState>{{ $t("tender.pool.empty") }}</EmptyState>
      </div>
      <div v-else ref="parentRef" class="flex-1 overflow-auto">
        <div :style="{ height: `${totalSize}px`, position: 'relative' }">
          <div
            v-for="vr in virtualRows"
            :key="results[vr.index].id"
            class="absolute left-0 top-0 flex w-full items-center gap-3 border-b border-slate-50 px-3"
            :style="{ height: `${vr.size}px`, transform: `translateY(${vr.start}px)` }"
          >
            <img
              v-if="thumb(results[vr.index])"
              :src="thumb(results[vr.index])!"
              :alt="results[vr.index].name"
              class="h-11 w-11 shrink-0 rounded-md border border-slate-200 object-cover"
              loading="lazy"
            />
            <div v-else class="flex h-11 w-11 shrink-0 items-center justify-center rounded-md border border-dashed border-slate-200 text-[9px] text-slate-300">
              {{ $t("tender.pool.noImage") }}
            </div>

            <div class="min-w-0 flex-1">
              <div class="truncate text-sm font-medium text-slate-800">{{ results[vr.index].name }}</div>
              <div class="truncate text-[11px] text-slate-400">
                <span class="font-mono">{{ results[vr.index].sku }}</span>
                <span v-if="results[vr.index].categoryName"> · {{ results[vr.index].categoryName }}</span>
                · {{ $t(`enums.unitOfMeasure.${results[vr.index].unitOfMeasure}`) }}
              </div>
            </div>

            <button
              v-if="hasProduct(results[vr.index].id)"
              disabled
              class="flex shrink-0 items-center gap-1 rounded-md bg-emerald-50 px-2.5 py-1.5 text-xs font-semibold text-emerald-600"
            >
              <CheckIcon class="h-3.5 w-3.5" />{{ $t("tender.pool.added") }}
            </button>
            <button
              v-else
              class="flex shrink-0 items-center gap-1 rounded-md bg-indigo-600 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
              @click="add(results[vr.index])"
            >
              <PlusIcon class="h-3.5 w-3.5" />{{ $t("tender.pool.add") }}
            </button>
          </div>
        </div>
      </div>
    </aside>
  </div>
</template>
