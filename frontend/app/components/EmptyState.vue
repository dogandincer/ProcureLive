<script setup lang="ts">
withDefaults(defineProps<{ icon?: string }>(), { icon: "📭" });
</script>

<template>
  <div class="flex flex-col items-center gap-2 py-8 text-center text-slate-400">
    <span class="text-2xl">{{ icon }}</span>
    <p class="text-sm">
      <slot />
    </p>
  </div>
</template>
