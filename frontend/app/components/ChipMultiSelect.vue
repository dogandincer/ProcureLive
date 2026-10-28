<script setup lang="ts">
import { CheckIcon } from "@heroicons/vue/20/solid";

/** Küçük çoklu seçim: seçenekler çip olarak listelenir, tıklayınca seçilir/kaldırılır (8'den fazla seçenekte filtre kutusu). */
const props = defineProps<{
  options: { value: string; label: string; hint?: string | null; disabled?: boolean }[];
  label?: string;
  emptyText?: string;
}>();
const model = defineModel<string[]>({ required: true });
const filter = ref("");

const visible = computed(() => {
  const q = filter.value.trim().toLocaleLowerCase("tr");
  return q ? props.options.filter((o) => o.label.toLocaleLowerCase("tr").includes(q)) : props.options;
});

function toggle(value: string) {
  model.value = model.value.includes(value) ? model.value.filter((v) => v !== value) : [...model.value, value];
}
</script>

<template>
  <div>
    <div v-if="label" class="mb-1 flex items-center justify-between">
      <span class="text-xs font-medium text-slate-600">{{ label }}</span>
      <span v-if="model.length" class="text-[11px] text-slate-400">{{ $t("common.selectedCount", { n: model.length }) }}</span>
    </div>
    <input
      v-if="options.length > 8"
      v-model="filter"
      :placeholder="$t('common.filter')"
      class="mb-2 w-full rounded-md border border-slate-200 px-2 py-1 text-xs"
    />
    <div class="flex flex-wrap gap-1.5">
      <button
        v-for="o in visible"
        :key="o.value"
        type="button"
        :disabled="o.disabled"
        :title="o.hint ?? undefined"
        class="inline-flex items-center gap-1 rounded-full border px-2.5 py-1 text-xs transition disabled:cursor-not-allowed disabled:opacity-50"
        :class="model.includes(o.value)
          ? 'border-indigo-500 bg-indigo-50 font-medium text-indigo-700'
          : 'border-slate-200 bg-surface text-slate-600 hover:border-slate-300'"
        :aria-pressed="model.includes(o.value)"
        @click="toggle(o.value)"
      >
        <CheckIcon v-if="model.includes(o.value)" class="h-3 w-3" />
        {{ o.label }}
      </button>
      <span v-if="options.length === 0" class="text-xs text-slate-400">{{ emptyText ?? "—" }}</span>
    </div>
  </div>
</template>
