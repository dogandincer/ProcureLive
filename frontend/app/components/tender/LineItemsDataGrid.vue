<script setup lang="ts">
import { useVirtualizer } from "@tanstack/vue-virtual";
import { UNIT_OPTIONS } from "~/composables/useTenderForm";
import {
  PlusIcon, TrashIcon, ChevronUpIcon, ChevronDownIcon,
  DocumentTextIcon, CheckCircleIcon, CalendarDaysIcon,
} from "@heroicons/vue/24/outline";

// Grid, store'u tek kaynak olarak tüketir (props/emit yerine) — tasarım kararı.
const { lines, lineCount, addBlankLine, removeLine, moveLine, applyDeliveryDateToAll } = useTenderForm();
const { t } = useI18n();

// Satır bazlı şartname düzenlemesi üst bileşene (drawer) bırakılır.
const emit = defineEmits<{ (e: "edit-spec", tempId: string): void }>();

const bulkDeliveryDate = ref("");
function applyBulkDate() {
  if (bulkDeliveryDate.value) applyDeliveryDateToAll(bulkDeliveryDate.value);
}

// ---- Sanallaştırma: yalnızca görünür ~pencere kadar satır DOM'da ----
const parentRef = ref<HTMLElement | null>(null);
const ROW_HEIGHT = 56;
const rowVirtualizer = useVirtualizer(
  computed(() => ({
    count: lines.value.length,
    getScrollElement: () => parentRef.value,
    estimateSize: () => ROW_HEIGHT,
    overscan: 10,
  })),
);
const virtualRows = computed(() => rowVirtualizer.value.getVirtualItems());
const totalSize = computed(() => rowVirtualizer.value.getTotalSize());

// Header ile satırların aynı kolon şablonunu paylaşması için tek kaynak.
const GRID_COLS = "44px minmax(200px,1fr) 88px 120px 120px 150px 84px 92px";

function confirmRemove(tempId: string, name: string) {
  if (confirm(t("tender.grid.removeConfirm", { name: name || "—" }))) removeLine(tempId);
}
</script>

<template>
  <div class="rounded-xl border border-slate-200 bg-white">
    <!-- Toolbar -->
    <div class="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 px-3 py-2.5">
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="flex items-center gap-1 rounded-md bg-indigo-600 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
          @click="addBlankLine"
        >
          <PlusIcon class="h-3.5 w-3.5" />{{ $t("tender.grid.addLine") }}
        </button>
        <span class="text-xs text-slate-400">{{ $t("tender.grid.lineCount", { n: lineCount }) }}</span>
      </div>
      <div class="flex items-center gap-1.5">
        <CalendarDaysIcon class="h-4 w-4 text-slate-400" />
        <span class="text-xs text-slate-500">{{ $t("tender.grid.bulkDelivery") }}</span>
        <input v-model="bulkDeliveryDate" type="date" class="rounded-md border border-slate-300 px-2 py-1 text-xs" />
        <button
          type="button"
          :disabled="!bulkDeliveryDate"
          class="rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-600 hover:bg-slate-50 disabled:opacity-40"
          @click="applyBulkDate"
        >
          {{ $t("tender.grid.applyAll") }}
        </button>
      </div>
    </div>

    <!-- Header -->
    <div
      class="grid items-center gap-2 overflow-x-auto border-b border-slate-100 bg-slate-50 px-3 py-2 text-[11px] font-semibold uppercase tracking-wide text-slate-500"
      :style="{ gridTemplateColumns: GRID_COLS, minWidth: '900px' }"
    >
      <div>#</div>
      <div>{{ $t("tender.grid.col.item") }}</div>
      <div class="text-right">{{ $t("tender.grid.col.qty") }}</div>
      <div>{{ $t("tender.grid.col.unit") }}</div>
      <div class="text-right">{{ $t("tender.grid.col.target") }}</div>
      <div>{{ $t("tender.grid.col.delivery") }}</div>
      <div class="text-center">{{ $t("tender.grid.col.spec") }}</div>
      <div class="text-right">{{ $t("common.actions") }}</div>
    </div>

    <!-- Empty -->
    <div v-if="lineCount === 0" class="p-8">
      <EmptyState>{{ $t("tender.grid.empty") }}</EmptyState>
    </div>

    <!-- Virtualized viewport -->
    <div v-else ref="parentRef" class="overflow-auto" style="height: 440px;">
      <div :style="{ height: `${totalSize}px`, minWidth: '900px', position: 'relative' }">
        <div
          v-for="vr in virtualRows"
          :key="lines[vr.index].tempId"
          class="absolute left-0 top-0 grid items-center gap-2 border-b border-slate-50 px-3"
          :style="{
            gridTemplateColumns: GRID_COLS,
            width: '100%',
            height: `${vr.size}px`,
            transform: `translateY(${vr.start}px)`,
          }"
        >
          <!-- # + sıralama -->
          <div class="flex items-center gap-0.5 text-xs text-slate-400">
            <span class="w-4 tabular-nums">{{ lines[vr.index].sortOrder }}</span>
            <div class="flex flex-col">
              <button type="button" class="text-slate-300 hover:text-indigo-600 disabled:opacity-30" :disabled="vr.index === 0" @click="moveLine(lines[vr.index].tempId, -1)"><ChevronUpIcon class="h-3 w-3" /></button>
              <button type="button" class="text-slate-300 hover:text-indigo-600 disabled:opacity-30" :disabled="vr.index === lineCount - 1" @click="moveLine(lines[vr.index].tempId, 1)"><ChevronDownIcon class="h-3 w-3" /></button>
            </div>
          </div>

          <!-- Ürün / açıklama -->
          <div class="flex min-w-0 items-center gap-2">
            <img
              v-if="lines[vr.index].thumbnailUrl"
              :src="lines[vr.index].thumbnailUrl!"
              :alt="lines[vr.index].name"
              class="h-9 w-9 shrink-0 rounded border border-slate-200 object-cover"
              loading="lazy"
            />
            <div v-else class="flex h-9 w-9 shrink-0 items-center justify-center rounded border border-dashed border-slate-200 text-[9px] text-slate-300">SKU</div>
            <div class="min-w-0 flex-1">
              <input
                v-model="lines[vr.index].description"
                :placeholder="$t('tender.grid.descPlaceholder')"
                class="w-full rounded-md border border-slate-200 px-2 py-1 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
              />
              <span v-if="lines[vr.index].sku" class="mt-0.5 block truncate font-mono text-[10px] text-slate-400">{{ lines[vr.index].sku }}</span>
            </div>
          </div>

          <!-- Miktar -->
          <input
            v-model.number="lines[vr.index].quantity"
            type="number" min="0.001" step="any"
            class="w-full rounded-md border border-slate-200 px-2 py-1 text-right text-sm tabular-nums focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            :class="{ 'border-rose-300': lines[vr.index].quantity <= 0 }"
          />

          <!-- Birim -->
          <select v-model="lines[vr.index].unitOfMeasure" class="w-full rounded-md border border-slate-200 px-1.5 py-1 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500">
            <option v-for="u in UNIT_OPTIONS" :key="u" :value="u">{{ $t(`enums.unitOfMeasure.${u}`) }}</option>
          </select>

          <!-- Hedef fiyat -->
          <input
            v-model.number="lines[vr.index].targetPrice"
            type="number" min="0" step="any"
            :placeholder="$t('tender.grid.optional')"
            class="w-full rounded-md border border-slate-200 px-2 py-1 text-right text-sm tabular-nums focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />

          <!-- Teslim tarihi -->
          <input
            v-model="lines[vr.index].requestedDeliveryDate"
            type="date"
            class="w-full rounded-md border border-slate-200 px-2 py-1 text-xs focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />

          <!-- Şartname -->
          <div class="flex justify-center">
            <button
              type="button"
              class="flex items-center gap-1 rounded-md border px-2 py-1 text-xs font-medium transition-colors"
              :class="lines[vr.index].specification
                ? 'border-indigo-300 bg-indigo-50 text-indigo-700'
                : 'border-slate-200 text-slate-500 hover:bg-slate-50'"
              :title="$t('tender.grid.col.spec')"
              @click="emit('edit-spec', lines[vr.index].tempId)"
            >
              <CheckCircleIcon v-if="lines[vr.index].specification" class="h-3.5 w-3.5" />
              <DocumentTextIcon v-else class="h-3.5 w-3.5" />
            </button>
          </div>

          <!-- Aksiyon -->
          <div class="flex justify-end">
            <button
              type="button"
              class="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600"
              :title="$t('common.delete')"
              @click="confirmRemove(lines[vr.index].tempId, lines[vr.index].description)"
            >
              <TrashIcon class="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
