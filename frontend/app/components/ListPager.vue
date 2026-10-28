<script setup lang="ts">
import { ChevronLeftIcon, ChevronRightIcon } from "@heroicons/vue/24/outline";

/** Liste sayfalaması: "21–40 / 135" + önceki/sonraki ve yakın sayfa numaraları. Tek sayfada da toplam görünür. */
const page = defineModel<number>("page", { required: true });
const props = defineProps<{ total: number; pageSize: number }>();

const pageCount = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)));
const from = computed(() => (props.total === 0 ? 0 : (page.value - 1) * props.pageSize + 1));
const to = computed(() => Math.min(page.value * props.pageSize, props.total));
const pages = computed(() => {
  const n = pageCount.value;
  const cur = page.value;
  const set = new Set([1, n, cur - 1, cur, cur + 1].filter((p) => p >= 1 && p <= n));
  const sorted = [...set].sort((a, b) => a - b);
  const out: (number | "…")[] = [];
  sorted.forEach((p, i) => {
    if (i > 0 && p - sorted[i - 1] > 1) out.push("…");
    out.push(p);
  });
  return out;
});

function go(p: number) {
  if (p >= 1 && p <= pageCount.value && p !== page.value) page.value = p;
}
</script>

<template>
  <nav v-if="total > 0" class="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm" :aria-label="$t('pager.label')" data-testid="pager">
    <span class="text-slate-500">{{ $t("pager.range", { from, to, total }) }}</span>
    <div v-if="pageCount > 1" class="flex items-center gap-1">
      <button type="button" class="rounded-md p-1.5 text-slate-600 hover:bg-slate-100 disabled:opacity-40" :disabled="page <= 1" :aria-label="$t('pager.prev')" @click="go(page - 1)">
        <ChevronLeftIcon class="h-4 w-4" />
      </button>
      <template v-for="(p, i) in pages" :key="i">
        <span v-if="p === '…'" class="px-1 text-slate-400">…</span>
        <button
          v-else
          type="button"
          class="min-w-8 rounded-md px-2 py-1 font-medium"
          :class="p === page ? 'bg-indigo-600 text-white' : 'text-slate-600 hover:bg-slate-100'"
          :aria-current="p === page ? 'page' : undefined"
          @click="go(p)"
        >{{ p }}</button>
      </template>
      <button type="button" class="rounded-md p-1.5 text-slate-600 hover:bg-slate-100 disabled:opacity-40" :disabled="page >= pageCount" :aria-label="$t('pager.next')" @click="go(page + 1)">
        <ChevronRightIcon class="h-4 w-4" />
      </button>
    </div>
  </nav>
</template>
