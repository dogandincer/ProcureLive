<script setup lang="ts">
import { ArrowUpTrayIcon } from "@heroicons/vue/24/outline";
import type { UploadedFileDto } from "~/types/api";
import type { UploadPurpose } from "~/composables/useFileUpload";

/** Dosya seç → sunucuya yükle → `uploaded` ile sonucu ver. Hata mesajı sunucudan (tür/boyut/içerik) gelir. */
const props = withDefaults(defineProps<{ purpose: UploadPurpose; accept?: string; multiple?: boolean; label?: string }>(), {
  accept: undefined,
  multiple: false,
  label: undefined,
});
const emit = defineEmits<{ uploaded: [file: UploadedFileDto] }>();
const { upload } = useFileUpload();
const toast = useToast();
const { t } = useI18n();
const busy = ref(false);
const input = ref<HTMLInputElement | null>(null);

async function onChange(e: Event) {
  const files = Array.from((e.target as HTMLInputElement).files ?? []);
  if (!files.length) return;
  busy.value = true;
  try {
    for (const f of files) {
      try {
        emit("uploaded", await upload(f, props.purpose));
      } catch (error: any) {
        toast.error(`${f.name}: ${apiErrorMessage(error, t("upload.error"))}`);
      }
    }
  } finally {
    busy.value = false;
    if (input.value) input.value.value = "";
  }
}
</script>

<template>
  <label
    class="inline-flex cursor-pointer items-center gap-1.5 rounded-md border border-slate-300 bg-surface px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
    :class="{ 'pointer-events-none opacity-60': busy }"
  >
    <ArrowUpTrayIcon class="h-4 w-4" />
    {{ busy ? $t("upload.uploading") : label ?? $t("upload.choose") }}
    <input ref="input" type="file" class="sr-only" :accept="accept" :multiple="multiple" data-testid="file-input" @change="onChange" />
  </label>
</template>
