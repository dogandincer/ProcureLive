<script setup lang="ts">
withDefaults(defineProps<{ title?: string; description?: string; noPadding?: boolean }>(), {});
</script>

<template>
  <div class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
    <div v-if="title || $slots.header" class="flex items-center justify-between border-b border-slate-100 px-5 py-3">
      <div>
        <slot name="header">
          <h3 class="text-sm font-semibold text-slate-800">{{ title }}</h3>
          <p v-if="description" class="text-xs text-slate-400 mt-0.5">{{ description }}</p>
        </slot>
      </div>
      <div v-if="$slots.action" class="ml-4 shrink-0">
        <slot name="action" />
      </div>
    </div>
    <div :class="noPadding ? '' : 'p-5'">
      <slot />
    </div>
  </div>
</template>
