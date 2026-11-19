<script setup lang="ts">
import { ChevronRightIcon } from "@heroicons/vue/24/outline";

/**
 * RFI / RFQ detay başlığı (iç kullanıcı ve tedarikçi ortak): üst gezinti, başlık, durum rozeti ve künye satırı.
 * Künye öğeleri `meta` ile, sağdaki işlemler `actions` yuvasıyla, başlık altındaki ek içerik varsayılan yuvayla verilir.
 */
defineProps<{
  title: string;
  status: string;
  backTo: string;
  backLabel: string;
  meta?: (string | null | undefined | false)[];
}>();
</script>

<template>
  <div class="flex flex-wrap items-start justify-between gap-3" data-testid="process-header">
    <div class="min-w-0">
      <div class="mb-1 flex items-center gap-1 text-xs text-slate-400">
        <NuxtLink :to="backTo" class="hover:text-slate-600">{{ backLabel }}</NuxtLink>
        <ChevronRightIcon class="h-3 w-3 shrink-0" />
        <span class="truncate text-slate-500">{{ title }}</span>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <h1 class="text-xl font-bold text-slate-800">{{ title }}</h1>
        <AppBadge :tone="processStatusTone[status] ?? 'slate'" data-testid="process-status">{{ $t(`enums.processStatus.${status}`) }}</AppBadge>
      </div>
      <p v-if="meta?.some(Boolean)" class="mt-1 flex flex-wrap gap-x-2 text-sm text-slate-500">
        <template v-for="(m, i) in meta.filter(Boolean)" :key="i">
          <span v-if="i > 0" aria-hidden="true">·</span>
          <span>{{ m }}</span>
        </template>
      </p>
      <slot />
    </div>
    <div v-if="$slots.actions" class="flex flex-wrap gap-2">
      <slot name="actions" />
    </div>
  </div>
</template>
