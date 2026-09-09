<script setup lang="ts">
// `tone` stays semantic (not the raw status string) so each page keeps its own status->tone mapping
// (e.g. { Draft: "slate", Live: "emerald", Cancelled: "rose" }) - this component only owns the colors.
withDefaults(defineProps<{ tone?: "slate" | "amber" | "emerald" | "indigo" | "rose" }>(), { tone: "slate" });

const toneClasses: Record<string, string> = {
  slate: "bg-slate-100 text-slate-600",
  amber: "bg-amber-100 text-amber-700",
  emerald: "bg-emerald-100 text-emerald-700",
  indigo: "bg-indigo-100 text-indigo-700",
  rose: "bg-rose-100 text-rose-700",
};
</script>

<template>
  <span class="rounded px-2 py-0.5 text-xs font-medium" :class="toneClasses[tone]">
    <slot />
  </span>
</template>
