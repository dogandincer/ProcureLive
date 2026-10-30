<script setup lang="ts">
/**
 * Kaydın açılacağı işyeri (cari, ürün, Excel aktarımı). Başlıkta işyeri seçiliyse o, değilse seçili ya da varsayılan
 * firmanın varsayılan işyeri önerilir. Başlıkta "Tüm firmalar" seçiliyken de kayıt açılabilsin diye seçim formdadır.
 */
const model = defineModel<string>({ required: true });
const props = withDefaults(defineProps<{ label?: string; hint?: string }>(), { label: undefined, hint: undefined });
const { firms, selectedFirmId } = useFirm();
const { workspaces, selectedWorkspaceId } = useWorkspace();

const active = computed(() => workspaces.value.filter((w) => w.isActive));
const groups = computed(() =>
  firms.value
    .map((f) => ({ id: f.id, label: `${f.code} · ${f.name}`, items: active.value.filter((w) => w.firmId === f.id) }))
    .filter((g) => g.items.length),
);

function suggest() {
  if (selectedWorkspaceId.value && active.value.some((w) => w.id === selectedWorkspaceId.value)) return selectedWorkspaceId.value;
  const firmId = selectedFirmId.value ?? firms.value.find((f) => f.isDefault)?.id;
  const inFirm = active.value.filter((w) => w.firmId === firmId);
  return inFirm.find((w) => w.isDefault)?.id ?? inFirm[0]?.id ?? active.value[0]?.id ?? "";
}

watch(
  () => [active.value.length, firms.value.length],
  () => {
    if (!model.value || !active.value.some((w) => w.id === model.value)) model.value = suggest();
  },
  { immediate: true },
);
// Form açıkken başlıktaki seçim değişirse öneri de onu izler.
watch([selectedWorkspaceId, selectedFirmId], () => (model.value = suggest()));
</script>

<template>
  <FormField :label="props.label ?? $t('processWorkspace.workspace')" :hint="props.hint" required>
    <select v-model="model" required class="rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-800" data-testid="workspace-field">
      <optgroup v-for="g in groups" :key="g.id" :label="g.label">
        <option v-for="w in g.items" :key="w.id" :value="w.id">{{ w.name }}</option>
      </optgroup>
    </select>
  </FormField>
</template>
