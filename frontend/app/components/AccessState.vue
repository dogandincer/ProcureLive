<script setup lang="ts">
import { LockClosedIcon, QuestionMarkCircleIcon } from "@heroicons/vue/24/outline";

/** Detay sayfası açılamadığında boş ekran yerine: yetki yok (403) ya da kayıt bulunamadı (404) + geri dönüş. */
defineProps<{ kind: "forbidden" | "notFound"; backTo: string; backLabel: string }>();
</script>

<template>
  <div class="mx-auto mt-10 max-w-md rounded-xl border border-slate-200 bg-white p-6 text-center shadow-sm" data-testid="access-state">
    <div class="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-slate-100">
      <LockClosedIcon v-if="kind === 'forbidden'" class="h-5 w-5 text-slate-500" />
      <QuestionMarkCircleIcon v-else class="h-5 w-5 text-slate-500" />
    </div>
    <h1 class="mt-3 text-base font-semibold text-slate-800">{{ $t(`accessState.${kind}.title`) }}</h1>
    <p class="mt-1 text-sm text-slate-500">{{ $t(`accessState.${kind}.body`) }}</p>
    <NuxtLink :to="backTo" class="mt-4 inline-block text-sm font-medium text-indigo-600 hover:underline">← {{ backLabel }}</NuxtLink>
  </div>
</template>
