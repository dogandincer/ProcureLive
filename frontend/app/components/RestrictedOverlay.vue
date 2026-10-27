<script setup lang="ts">
import { LockClosedIcon, SparklesIcon } from "@heroicons/vue/24/solid";

/**
 * Blurred "restricted" view. Purely visual: the backend has already dropped the sensitive fields
 * (items, prices, participants, attachments...) — there is no real data underneath the blur, only
 * placeholder skeleton rows (or whatever non-sensitive teaser the page passes in the slot).
 */
const props = withDefaults(
  defineProps<{
    level?: "QuotaExceeded" | "NoAccess" | "PlusRequired";
    title?: string;
    body?: string;
    rows?: number;
    compact?: boolean;
  }>(),
  { level: "QuotaExceeded", rows: 4, compact: false },
);

const { t } = useI18n();

const heading = computed(() => props.title ?? t(`restricted.${props.level}.title`));
const message = computed(() => props.body ?? t(`restricted.${props.level}.body`));
</script>

<template>
  <div class="relative min-h-[22rem] overflow-hidden rounded-xl border border-slate-200 bg-white">
    <div class="pointer-events-none select-none blur-[6px]" aria-hidden="true">
      <slot>
        <div class="space-y-4 p-5">
          <div v-for="i in rows" :key="i" class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-4">
            <div class="flex-1 space-y-2">
              <div class="h-3 w-2/3 rounded bg-slate-300" />
              <div class="h-2.5 w-1/3 rounded bg-slate-200" />
            </div>
            <div class="h-6 w-24 rounded bg-emerald-200" />
          </div>
        </div>
      </slot>
    </div>

    <div class="absolute inset-0 flex items-center justify-center bg-gradient-to-b from-white/30 to-white/80 p-4">
      <div
        class="w-full max-w-md rounded-xl border border-amber-200 bg-white/95 text-center shadow-lg"
        :class="compact ? 'p-4' : 'p-6'"
      >
        <div class="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-amber-100">
          <LockClosedIcon class="h-5 w-5 text-amber-600" />
        </div>
        <h3 class="mt-3 text-base font-semibold text-slate-800">{{ heading }}</h3>
        <p class="mt-1 text-sm text-slate-500">{{ message }}</p>
        <NuxtLink
          to="/supplier/pricing"
          class="mt-4 inline-flex items-center gap-1.5 rounded-md bg-amber-500 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-amber-600"
        >
          <SparklesIcon class="h-4 w-4" />
          {{ $t("restricted.upgradeCta") }}
        </NuxtLink>
        <slot name="extra" />
      </div>
    </div>
  </div>
</template>
