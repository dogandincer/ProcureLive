<script setup lang="ts">
import type { Component } from "vue";
import { CheckIcon, ComputerDesktopIcon, MoonIcon, SunIcon } from "@heroicons/vue/24/outline";
import { ACCENT_PRESETS, SWATCHES, accentScale, isHex, scaleFromBase, whiteTextContrast, type SidebarStyle, type ThemeMode } from "~/utils/theme";

/** Kişisel görünüm ayarları: tema, vurgu paleti (hazır veya seçim paletinden özel renk) ve menü stili. Anında uygulanır. */
const { theme, update, reset } = useTheme();
const { t } = useI18n();
const toast = useToast();

const modes: { id: ThemeMode; icon: Component }[] = [
  { id: "light", icon: SunIcon },
  { id: "dark", icon: MoonIcon },
  { id: "system", icon: ComputerDesktopIcon },
];
const sidebars: SidebarStyle[] = ["dark", "light", "accent"];

const hexInput = ref(theme.value.customColor);
watch(() => theme.value.customColor, (v) => (hexInput.value = v));
// Geçerli tam bir HEX yazıldığında beklemeden uygulanır (Enter/odak kaybı gerekmez).
watch(hexInput, (v) => {
  const value = v.startsWith("#") ? v : `#${v}`;
  if (/^#[0-9a-fA-F]{6}$/.test(value) && value.toLowerCase() !== theme.value.customColor?.toLowerCase()) pickCustom(value);
});

const current = computed(() => accentScale(theme.value));
const contrast = computed(() => whiteTextContrast(current.value[600]));

function pickPreset(id: string) {
  update({ accent: id });
}
function pickCustom(hex: string) {
  const v = hex.startsWith("#") ? hex.toLowerCase() : `#${hex.toLowerCase()}`;
  if (!isHex(v)) return;
  update({ accent: "custom", customColor: v });
}
function commitHex() {
  if (isHex(hexInput.value)) pickCustom(hexInput.value);
  else hexInput.value = theme.value.customColor;
}
function resetAll() {
  reset();
  toast.success(t("settings.resetDone"));
}

// Önizleme ölçeğinden birkaç ton (kart üzerindeki küçük renk şeridi).
const stripe = (sc: Record<number, string>) => [100, 300, 500, 600, 800].map((k) => sc[k]);
</script>

<template>
  <div class="mx-auto max-w-4xl space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ $t("settings.title") }}</h1>
      <p class="mt-1 text-sm text-slate-500">{{ $t("settings.subtitle") }}</p>
    </div>

    <!-- Tema -->
    <AppCard :title="$t('settings.mode.title')">
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-3" role="radiogroup" :aria-label="$t('settings.mode.title')">
        <button
          v-for="m in modes"
          :key="m.id"
          type="button"
          role="radio"
          :aria-checked="theme.mode === m.id"
          class="flex items-center gap-3 rounded-xl border p-4 text-left transition-colors"
          :class="theme.mode === m.id ? 'border-indigo-500 bg-indigo-50 ring-1 ring-indigo-500' : 'border-slate-200 hover:border-slate-300'"
          :data-testid="`mode-${m.id}`"
          @click="update({ mode: m.id })"
        >
          <component :is="m.icon" class="h-6 w-6 shrink-0" :class="theme.mode === m.id ? 'text-indigo-600' : 'text-slate-500'" />
          <span class="min-w-0">
            <span class="block text-sm font-semibold text-slate-800">{{ $t(`settings.mode.${m.id}`) }}</span>
            <span class="block text-xs text-slate-500">{{ $t(`settings.mode.${m.id}Hint`) }}</span>
          </span>
        </button>
      </div>
    </AppCard>

    <!-- Renk paleti -->
    <AppCard :title="$t('settings.palette.title')" :description="$t('settings.palette.subtitle')">
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-4" role="radiogroup" :aria-label="$t('settings.palette.title')">
        <button
          v-for="p in ACCENT_PRESETS"
          :key="p.id"
          type="button"
          role="radio"
          :aria-checked="theme.accent === p.id"
          class="relative rounded-xl border p-3 text-left transition-colors"
          :class="theme.accent === p.id ? 'border-indigo-500 ring-1 ring-indigo-500' : 'border-slate-200 hover:border-slate-300'"
          :data-testid="`preset-${p.id}`"
          @click="pickPreset(p.id)"
        >
          <span class="flex h-6 overflow-hidden rounded-md">
            <span v-for="(c, i) in stripe(p.scale)" :key="i" class="flex-1" :style="{ backgroundColor: c }" />
          </span>
          <span class="mt-2 block text-sm font-medium text-slate-800">{{ $t(`settings.palette.presets.${p.id}`) }}</span>
          <CheckIcon v-if="theme.accent === p.id" class="absolute right-2 top-2 h-4 w-4 rounded-full bg-indigo-600 p-0.5 text-white" />
        </button>
      </div>

      <div class="mt-5 border-t border-slate-100 pt-4">
        <div class="flex flex-wrap items-baseline justify-between gap-2">
          <h3 class="text-sm font-semibold text-slate-700">{{ $t("settings.palette.custom") }}</h3>
          <span class="text-xs text-slate-500">{{ $t("settings.palette.customHint") }}</span>
        </div>
        <div class="mt-3 grid grid-cols-9 gap-2 sm:grid-cols-12" role="radiogroup" :aria-label="$t('settings.palette.custom')" data-testid="swatches">
          <button
            v-for="c in SWATCHES"
            :key="c"
            type="button"
            role="radio"
            :aria-checked="theme.accent === 'custom' && theme.customColor === c"
            :aria-label="c"
            :title="c"
            class="relative aspect-square w-full rounded-lg ring-offset-2 ring-offset-surface transition-transform hover:scale-110 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-400"
            :class="theme.accent === 'custom' && theme.customColor === c ? 'ring-2 ring-slate-800' : ''"
            :style="{ backgroundColor: c }"
            @click="pickCustom(c)"
          >
            <CheckIcon v-if="theme.accent === 'custom' && theme.customColor === c" class="absolute inset-0 m-auto h-4 w-4 text-white" />
          </button>
        </div>
        <div class="mt-4 flex flex-wrap items-center gap-3">
          <label class="flex items-center gap-2 text-sm text-slate-600">
            <input
              type="color"
              :value="theme.customColor"
              class="h-9 w-12 cursor-pointer rounded border border-slate-300 bg-surface p-0.5"
              :aria-label="$t('settings.palette.picker')"
              @input="pickCustom(($event.target as HTMLInputElement).value)"
            />
            {{ $t("settings.palette.picker") }}
          </label>
          <input
            v-model="hexInput"
            maxlength="7"
            :aria-label="$t('settings.palette.hex')"
            class="w-28 rounded-md border border-slate-300 px-2 py-1.5 font-mono text-sm uppercase"
            data-testid="hex-input"
            @keyup.enter="commitHex"
            @blur="commitHex"
          />
          <span v-if="theme.accent === 'custom'" class="flex h-6 overflow-hidden rounded-md" :title="$t('settings.palette.generated')">
            <span v-for="(c, i) in stripe(scaleFromBase(theme.customColor))" :key="i" class="w-6" :style="{ backgroundColor: c }" />
          </span>
        </div>
        <p v-if="contrast < 4.5" class="mt-3 rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800" data-testid="contrast-warning">
          {{ $t("settings.palette.lowContrast") }}
        </p>
      </div>
    </AppCard>

    <!-- Menü stili -->
    <AppCard :title="$t('settings.sidebar.title')">
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-3" role="radiogroup" :aria-label="$t('settings.sidebar.title')">
        <button
          v-for="sb in sidebars"
          :key="sb"
          type="button"
          role="radio"
          :aria-checked="theme.sidebar === sb"
          class="flex items-center gap-3 rounded-xl border p-3 text-left transition-colors"
          :class="theme.sidebar === sb ? 'border-indigo-500 ring-1 ring-indigo-500' : 'border-slate-200 hover:border-slate-300'"
          :data-testid="`sidebar-${sb}`"
          @click="update({ sidebar: sb })"
        >
          <span
            class="flex h-12 w-10 shrink-0 flex-col gap-1 rounded-md border border-slate-200 p-1.5"
            :style="{ backgroundColor: sb === 'dark' ? '#0f172a' : sb === 'light' ? '#ffffff' : current[950] }"
          >
            <span class="h-1.5 rounded-sm" :style="{ backgroundColor: current[600] }" />
            <span class="h-1 rounded-sm opacity-60" :style="{ backgroundColor: sb === 'light' ? '#94a3b8' : '#cbd5e1' }" />
            <span class="h-1 rounded-sm opacity-60" :style="{ backgroundColor: sb === 'light' ? '#94a3b8' : '#cbd5e1' }" />
          </span>
          <span class="text-sm font-medium text-slate-800">{{ $t(`settings.sidebar.${sb}`) }}</span>
        </button>
      </div>
    </AppCard>

    <!-- Önizleme -->
    <AppCard :title="$t('settings.preview.title')">
      <div class="space-y-4" data-testid="theme-preview">
        <div class="flex flex-wrap items-center gap-2">
          <AppButton>{{ $t("settings.preview.primary") }}</AppButton>
          <AppButton variant="secondary">{{ $t("settings.preview.secondary") }}</AppButton>
          <AppBadge tone="indigo">{{ $t("enums.auctionStatus.Awarded") }}</AppBadge>
          <AppBadge tone="emerald">{{ $t("enums.auctionStatus.Live") }}</AppBadge>
          <AppBadge tone="amber">{{ $t("enums.auctionStatus.Scheduled") }}</AppBadge>
          <AppBadge tone="rose">{{ $t("enums.auctionStatus.Cancelled") }}</AppBadge>
          <a href="#" class="text-sm font-medium text-indigo-600 hover:underline" @click.prevent>{{ $t("settings.preview.link") }}</a>
        </div>
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div class="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">{{ $t("settings.preview.success") }}</div>
          <div class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">{{ $t("settings.preview.warning") }}</div>
          <div class="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">{{ $t("settings.preview.error") }}</div>
        </div>
        <input :placeholder="$t('settings.preview.input')" :aria-label="$t('settings.preview.input')" class="w-full max-w-sm rounded-md border border-slate-300 bg-surface px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500" />
      </div>
    </AppCard>

    <div class="flex justify-end">
      <button type="button" class="rounded-md px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100" data-testid="theme-reset" @click="resetAll">
        {{ $t("settings.reset") }}
      </button>
    </div>
  </div>
</template>
