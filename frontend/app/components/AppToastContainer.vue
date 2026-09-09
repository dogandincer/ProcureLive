<script setup lang="ts">
const { toasts, dismiss } = useToast();

const styles: Record<string, string> = {
  success: "border-emerald-200 bg-emerald-50 text-emerald-800",
  error: "border-rose-200 bg-rose-50 text-rose-800",
  info: "border-indigo-200 bg-indigo-50 text-indigo-800",
};

const icons: Record<string, string> = {
  success: "✓",
  error: "✕",
  info: "ℹ",
};
</script>

<template>
  <div class="pointer-events-none fixed right-4 top-4 z-50 flex w-full max-w-sm flex-col gap-2 sm:right-6 sm:top-6">
    <TransitionGroup name="toast">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="pointer-events-auto flex items-start gap-2 rounded-lg border p-3 text-sm shadow-lg"
        :class="styles[toast.type]"
      >
        <span class="mt-0.5 font-bold">{{ icons[toast.type] }}</span>
        <p class="flex-1">{{ toast.text }}</p>
        <button class="text-current opacity-60 hover:opacity-100" @click="dismiss(toast.id)">✕</button>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-enter-active,
.toast-leave-active {
  transition: all 0.2s ease;
}
.toast-enter-from {
  opacity: 0;
  transform: translateY(-8px);
}
.toast-leave-to {
  opacity: 0;
  transform: translateX(16px);
}
</style>
