<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string;
    value: string | number;
    subtitle?: string;
    tone?: "indigo" | "emerald" | "amber" | "rose" | "slate";
  }>(),
  { tone: "indigo" },
);

const iconBg: Record<string, string> = {
  indigo: "bg-indigo-50 text-indigo-600",
  emerald: "bg-emerald-50 text-emerald-600",
  amber: "bg-amber-50 text-amber-600",
  rose: "bg-rose-50 text-rose-600",
  slate: "bg-slate-100 text-slate-600",
};
</script>

<template>
  <div class="rounded-xl border border-slate-200 bg-surface p-5 shadow-sm">
    <div class="flex items-start justify-between gap-3">
      <div class="min-w-0">
        <div class="text-xs font-medium text-slate-500 uppercase tracking-wide">{{ label }}</div>
        <div class="mt-1.5 text-2xl font-bold text-slate-800 tabular-nums leading-none">{{ value }}</div>
        <div v-if="subtitle" class="mt-1 text-xs text-slate-400">{{ subtitle }}</div>
      </div>
      <div v-if="$slots.icon" :class="['rounded-lg p-2.5 shrink-0', iconBg[tone]]">
        <slot name="icon" />
      </div>
    </div>
  </div>
</template>
