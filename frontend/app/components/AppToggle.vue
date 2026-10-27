<script setup lang="ts">
withDefaults(defineProps<{ label: string; description?: string; disabled?: boolean }>(), { disabled: false });

const model = defineModel<boolean>({ required: true });
</script>

<template>
  <label class="flex cursor-pointer items-start justify-between gap-4" :class="{ 'cursor-not-allowed opacity-60': disabled }">
    <span>
      <span class="block text-sm font-medium text-slate-700">{{ label }}</span>
      <span v-if="description" class="mt-0.5 block text-xs text-slate-500">{{ description }}</span>
    </span>
    <button
      type="button"
      role="switch"
      :aria-checked="model"
      :disabled="disabled"
      class="relative mt-0.5 inline-flex h-6 w-11 shrink-0 rounded-full border-2 border-transparent transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500"
      :class="model ? 'bg-indigo-600' : 'bg-slate-300'"
      @click="model = !model"
    >
      <span
        class="pointer-events-none inline-block h-5 w-5 rounded-full bg-white shadow transition-transform"
        :class="model ? 'translate-x-5' : 'translate-x-0'"
      />
    </button>
  </label>
</template>
