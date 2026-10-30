<script setup lang="ts">
const { locale, locales, setLocale } = useI18n();

const available = computed(() => locales.value as { code: "tr" | "en"; name: string }[]);
</script>

<template>
  <div role="group" :aria-label="$t('nav.language')" class="flex items-center rounded-md border border-slate-200 bg-slate-50 p-0.5 text-xs font-semibold">
    <button
      v-for="l in available"
      :key="l.code"
      type="button"
      :lang="l.code"
      :aria-label="l.name"
      :aria-pressed="locale === l.code"
      class="rounded px-2 py-1 uppercase tracking-wide transition-colors"
      :class="locale === l.code ? 'bg-indigo-600 text-white' : 'text-slate-500 hover:text-slate-700'"
      @click="setLocale(l.code)"
    >
      {{ l.code }}
    </button>
  </div>
</template>
