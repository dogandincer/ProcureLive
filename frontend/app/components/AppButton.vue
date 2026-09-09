<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: "primary" | "secondary" | "danger" | "ghost";
    type?: "button" | "submit";
    loading?: boolean;
    disabled?: boolean;
  }>(),
  { variant: "primary", type: "button", loading: false, disabled: false },
);

const variantClasses: Record<string, string> = {
  primary: "bg-indigo-600 text-white hover:bg-indigo-700 disabled:hover:bg-indigo-600",
  secondary: "border border-slate-300 bg-white text-slate-700 hover:bg-slate-50",
  danger: "bg-rose-600 text-white hover:bg-rose-700 disabled:hover:bg-rose-600",
  ghost: "text-indigo-600 hover:underline",
};
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    class="inline-flex items-center justify-center gap-1.5 rounded-md px-4 py-2 text-sm font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-50"
    :class="variantClasses[variant]"
  >
    <svg v-if="loading" class="h-3.5 w-3.5 animate-spin" viewBox="0 0 24 24" fill="none">
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
    <slot />
  </button>
</template>
