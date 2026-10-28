<script setup lang="ts">
import type { LookupRow } from "~/types/api";
import { EllipsisHorizontalIcon, MagnifyingGlassIcon, XMarkIcon } from "@heroicons/vue/24/outline";

/**
 * "Sarı alan": serbest metin girilemeyen, yanındaki üç noktayla açılan pencereden kayıt seçilen alan.
 * Seçimde kaydın ID'si döner (v-model); ekranda Kodu/Adı gösterilir. Satırları sağlayan `load` fonksiyonu
 * her ekrana özgüdür (firmalar, cariler, ürünler...), bileşen geneldir.
 */
const props = withDefaults(
  defineProps<{
    modelValue: string | null;
    load: () => Promise<LookupRow[]>;
    title: string;
    display?: string | null;
    required?: boolean;
    invalid?: boolean;
    disabled?: boolean;
    placeholder?: string;
  }>(),
  { display: null, required: false, invalid: false, disabled: false, placeholder: "" },
);

const emit = defineEmits<{ "update:modelValue": [value: string | null]; select: [row: LookupRow | null] }>();

const open = ref(false);
const loading = ref(false);
const rows = ref<LookupRow[]>([]);
const term = ref("");
const searchInput = ref<HTMLInputElement | null>(null);

const norm = (s: string) => s.toLocaleLowerCase("tr-TR");
const filtered = computed(() => {
  const q = norm(term.value.trim());
  if (!q) return rows.value;
  return rows.value.filter((r) => norm(r.code).includes(q) || norm(r.name).includes(q) || (r.detail && norm(r.detail).includes(q)));
});

const text = computed(() => {
  if (props.display) return props.display;
  const row = rows.value.find((r) => r.id === props.modelValue);
  return row ? `${row.code} · ${row.name}` : "";
});

async function openPicker() {
  if (props.disabled) return;
  open.value = true;
  term.value = "";
  loading.value = true;
  try {
    rows.value = await props.load();
  } finally {
    loading.value = false;
  }
  await nextTick();
  searchInput.value?.focus();
}

function choose(row: LookupRow) {
  emit("update:modelValue", row.id);
  emit("select", row);
  open.value = false;
}

function clear() {
  emit("update:modelValue", null);
  emit("select", null);
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === "Escape") open.value = false;
  if (e.key === "Enter" && filtered.value.length > 0) {
    e.preventDefault();
    choose(filtered.value[0]!);
  }
}
</script>

<template>
  <div>
    <div class="flex" :class="{ 'opacity-60': disabled }">
      <input
        :value="text"
        readonly
        :placeholder="placeholder"
        :aria-required="required"
        class="min-w-0 flex-1 cursor-pointer rounded-l-md border bg-amber-50 px-3 py-1.5 text-sm text-slate-800 focus:outline-none focus:ring-1"
        :class="invalid ? 'border-rose-400 focus:ring-rose-400' : 'border-amber-300 focus:ring-amber-400'"
        @click="openPicker"
        @keydown.enter.prevent="openPicker"
        @keydown.space.prevent="openPicker"
      />
      <button
        v-if="!required && modelValue && !disabled"
        type="button"
        class="border-y border-amber-300 bg-amber-50 px-1.5 text-slate-400 hover:text-rose-600"
        :title="$t('lookup.clear')"
        @click="clear"
      >
        <XMarkIcon class="h-4 w-4" />
      </button>
      <button
        type="button"
        class="rounded-r-md border border-l-0 px-2.5 text-amber-700 hover:bg-amber-200"
        :class="invalid ? 'border-rose-400 bg-rose-50' : 'border-amber-300 bg-amber-100'"
        :disabled="disabled"
        :title="$t('lookup.open')"
        @click="openPicker"
      >
        <EllipsisHorizontalIcon class="h-4 w-4" />
      </button>
    </div>

    <Teleport to="body">
      <div v-if="open" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm" @click.self="open = false">
        <div class="flex max-h-[80vh] w-full max-w-lg flex-col overflow-hidden rounded-2xl bg-surface shadow-2xl" @keydown="onKeydown">
          <div class="flex items-center justify-between border-b border-slate-100 px-5 py-3">
            <h2 class="text-sm font-semibold text-slate-800">{{ title }}</h2>
            <button type="button" class="rounded p-1 text-slate-400 hover:bg-slate-100" @click="open = false">
              <XMarkIcon class="h-5 w-5" />
            </button>
          </div>
          <div class="border-b border-slate-100 p-3">
            <div class="relative">
              <MagnifyingGlassIcon class="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                ref="searchInput"
                v-model="term"
                type="search"
                :placeholder="$t('lookup.search')"
                class="w-full rounded-md border border-slate-300 py-1.5 pl-8 pr-3 text-sm focus:border-indigo-500 focus:outline-none"
              />
            </div>
          </div>
          <div class="flex-1 overflow-y-auto">
            <LoadingSpinner v-if="loading" />
            <table v-else-if="filtered.length > 0" class="w-full text-sm">
              <thead class="sticky top-0 bg-slate-50 text-left text-xs uppercase text-slate-500">
                <tr>
                  <th class="px-4 py-2">{{ $t("lookup.code") }}</th>
                  <th class="px-4 py-2">{{ $t("lookup.name") }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-50">
                <tr
                  v-for="r in filtered"
                  :key="r.id"
                  class="cursor-pointer hover:bg-amber-50"
                  :class="{ 'bg-amber-50 font-medium': r.id === modelValue }"
                  @click="choose(r)"
                >
                  <td class="px-4 py-2 font-mono text-xs text-slate-600">{{ r.code }}</td>
                  <td class="px-4 py-2 text-slate-800">
                    {{ r.name }}
                    <span v-if="r.detail" class="block text-xs text-slate-400">{{ r.detail }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
            <EmptyState v-else>{{ $t("lookup.empty") }}</EmptyState>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
