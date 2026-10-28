<script setup lang="ts">
import { BuildingOffice2Icon, ChevronDownIcon, StarIcon } from "@heroicons/vue/24/outline";

// block: mobil menüde tam genişlik (başlıkta yer kalmadığında kenar çubuğuna taşınır).
const props = withDefaults(defineProps<{ block?: boolean }>(), { block: false });

// Firma seçimi — "Tüm firmalar" seçeneği bilinçli olarak yok; listeler her zaman tek firma bağlamında çalışır.
const { firms, selectedFirmId, selectedFirm, setFirm } = useFirm();
const { reconcile } = useWorkspace();
const open = ref(false);
const el = ref<HTMLElement | null>(null);

function select(id: string) {
  setFirm(id);
  reconcile(id);
  open.value = false;
}

function handleClickOutside(event: MouseEvent) {
  if (el.value && !el.value.contains(event.target as Node)) {
    open.value = false;
  }
}

onMounted(() => document.addEventListener("click", handleClickOutside));
onBeforeUnmount(() => document.removeEventListener("click", handleClickOutside));
</script>

<template>
  <div v-if="selectedFirm" ref="el" class="relative" :class="{ 'w-full': props.block }">
    <button
      class="flex items-center gap-1.5 rounded-lg border border-indigo-200 bg-indigo-50 px-2.5 py-1.5 text-xs font-medium text-indigo-700 hover:bg-indigo-100 transition-colors"
      :class="{ 'w-full': props.block }"
      :title="$t('firms.switcherTitle')"
      @click="open = !open"
    >
      <BuildingOffice2Icon class="h-3.5 w-3.5 shrink-0" />
      <span class="max-w-[140px] truncate">{{ selectedFirm.shortName || selectedFirm.name }}</span>
      <ChevronDownIcon class="h-3 w-3 shrink-0" :class="{ 'rotate-180': open }" />
    </button>

    <div
      v-if="open"
      class="absolute z-30 mt-1.5 min-w-[220px] rounded-xl border border-slate-200 bg-surface py-1 shadow-lg"
      :class="props.block ? 'left-0 right-0' : 'right-0'"
    >
      <button
        v-for="f in firms"
        :key="f.id"
        class="flex w-full items-center gap-2 px-3 py-2 text-left text-xs hover:bg-slate-50"
        :class="selectedFirmId === f.id ? 'font-semibold text-indigo-600' : 'text-slate-600'"
        @click="select(f.id)"
      >
        <StarIcon v-if="f.isDefault" class="h-3 w-3 shrink-0 text-amber-500" :title="$t('firms.default')" />
        <span class="shrink-0 font-mono text-[10px] text-slate-400">{{ f.code }}</span>
        <span class="flex-1 truncate">{{ f.name }}</span>
      </button>
    </div>
  </div>
</template>
