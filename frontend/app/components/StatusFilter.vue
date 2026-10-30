<script setup lang="ts">
/**
 * Liste sayfalarında durum süzgeci (Tümü + durumlar). Seçim adres çubuğuna (?status=) yazılır; panodaki
 * "Yapılacaklar" bağlantıları listeyi doğrudan süzülmüş açabilir.
 */
const props = defineProps<{ options: { value: string; label: string }[]; modelValue: string }>();
const emit = defineEmits<{ "update:modelValue": [value: string] }>();
const route = useRoute();
const router = useRouter();

function select(value: string) {
  emit("update:modelValue", value);
  router.replace({ query: { ...route.query, status: value || undefined } });
}
</script>

<template>
  <div class="flex flex-wrap gap-1.5" role="group" :aria-label="$t('common.status')" data-testid="status-filter">
    <button
      v-for="o in [{ value: '', label: $t('statusFilter.all') }, ...props.options]"
      :key="o.value"
      type="button"
      class="rounded-full border px-3 py-1 text-xs font-medium transition-colors"
      :class="modelValue === o.value ? 'border-indigo-600 bg-indigo-600 text-white' : 'border-slate-200 bg-surface text-slate-600 hover:border-slate-300 hover:bg-slate-50'"
      :aria-pressed="modelValue === o.value"
      @click="select(o.value)"
    >
      {{ o.label }}
    </button>
  </div>
</template>
