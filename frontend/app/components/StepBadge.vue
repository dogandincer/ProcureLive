<script setup lang="ts">
defineProps<{ step: number; total: number; label: string }>();
</script>

<template>
  <div class="flex items-center gap-2">
    <span class="flex h-5 w-5 items-center justify-center rounded-full bg-indigo-600 text-[11px] font-bold text-white">
      {{ step }}
    </span>
    <span class="text-sm font-semibold text-slate-700">{{ label }}</span>
    <span class="text-xs text-slate-400">Adım {{ step }}/{{ total }}</span>
  </div>
</template>
