<script setup lang="ts">
/** Etiketli form alanı: alan varsayılan slot'a konur, etiket tıklanınca alana odaklanır; isteğe bağlı ipucu altta. */
defineProps<{ label: string; required?: boolean; hint?: string }>();
</script>

<template>
  <label class="flex flex-col gap-1">
    <span class="text-xs font-medium text-slate-600">
      {{ label }}<span v-if="required" class="ml-0.5 text-rose-600" aria-hidden="true">*</span>
    </span>
    <slot />
    <span v-if="hint" class="text-[11px] text-slate-400">{{ hint }}</span>
  </label>
</template>
