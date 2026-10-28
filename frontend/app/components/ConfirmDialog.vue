<script setup lang="ts">
/**
 * Geri alınamaz işlemler için onay penceresi (kazanan seçme, süreç kapatma, iptal...).
 * Esc ve arka plan tıklaması vazgeçer; onay butonu yükleniyorken kilitlenir.
 */
const props = withDefaults(defineProps<{
  open: boolean;
  title: string;
  confirmLabel?: string;
  tone?: "primary" | "danger";
  loading?: boolean;
}>(), { confirmLabel: undefined, tone: "primary", loading: false });
const emit = defineEmits<{ confirm: []; cancel: [] }>();

function onKey(e: KeyboardEvent) {
  if (e.key === "Escape" && props.open && !props.loading) emit("cancel");
}
onMounted(() => document.addEventListener("keydown", onKey));
onBeforeUnmount(() => document.removeEventListener("keydown", onKey));
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" @click.self="!loading && emit('cancel')">
      <div role="dialog" aria-modal="true" :aria-label="title" class="w-full max-w-md rounded-xl bg-surface p-5 shadow-xl" data-testid="confirm-dialog">
        <h2 class="text-base font-semibold text-slate-800">{{ title }}</h2>
        <div class="mt-2 text-sm text-slate-600"><slot /></div>
        <div class="mt-5 flex justify-end gap-2">
          <button type="button" class="rounded-md px-3 py-2 text-sm text-slate-600 hover:bg-slate-100" :disabled="loading" @click="emit('cancel')">
            {{ $t("common.cancel") }}
          </button>
          <AppButton :variant="tone === 'danger' ? 'danger' : 'primary'" :loading="loading" @click="emit('confirm')">
            {{ confirmLabel ?? $t("common.confirm") }}
          </AppButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>
