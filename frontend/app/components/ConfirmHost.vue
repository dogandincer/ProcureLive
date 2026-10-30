<script setup lang="ts">
/** useConfirm().ask(...) ile açılan tek, uygulama geneli onay penceresi. */
const { state, settle } = useConfirm();
</script>

<template>
  <ConfirmDialog
    :open="!!state?.open"
    :title="state?.title ?? ''"
    :confirm-label="state?.confirmLabel"
    :tone="state?.tone ?? 'primary'"
    @confirm="settle(true)"
    @cancel="settle(false)"
  >
    <p v-if="state?.body">{{ state.body }}</p>
  </ConfirmDialog>
</template>
