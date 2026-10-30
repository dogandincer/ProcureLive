<script setup lang="ts">
/**
 * Süreç (RFI/RFQ) sahibi firma + işyeri. Firma listesi firmalardan gelir (tedarikçi cariler sahip seçilemez); varsayılanlar
 * başlıktaki seçimdir. İşyeri boş bırakılırsa sunucu firmanın varsayılan işyerini (satınalmacı için yetkili olanı) atar.
 */
const owner = defineModel<string>("owner", { required: true });
const workspace = defineModel<string>("workspace", { required: true });
const { firms, selectedFirmId } = useFirm();
const { workspaces, selectedWorkspaceId } = useWorkspace();

const ownerWorkspaces = computed(() => workspaces.value.filter((w) => w.isActive && w.firmId === owner.value));

watch(
  () => [firms.value.length, selectedFirmId.value],
  () => {
    if (!owner.value || !firms.value.some((f) => f.id === owner.value)) {
      owner.value = selectedFirmId.value ?? firms.value.find((f) => f.isDefault)?.id ?? firms.value[0]?.id ?? "";
    }
  },
  { immediate: true },
);

watch(
  () => [owner.value, workspaces.value.length],
  () => {
    if (workspace.value && ownerWorkspaces.value.some((w) => w.id === workspace.value)) return;
    const preferred = ownerWorkspaces.value.find((w) => w.id === selectedWorkspaceId.value)
      ?? ownerWorkspaces.value.find((w) => w.isDefault) ?? ownerWorkspaces.value[0];
    workspace.value = preferred?.id ?? "";
  },
  { immediate: true },
);
</script>

<template>
  <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">
    {{ $t("processWorkspace.owner") }}
    <select v-model="owner" required class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal text-slate-800">
      <option v-for="f in firms" :key="f.id" :value="f.id">{{ f.code }} · {{ f.name }}</option>
    </select>
  </label>
  <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">
    {{ $t("processWorkspace.workspace") }}
    <select v-model="workspace" :disabled="!ownerWorkspaces.length" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal text-slate-800 disabled:bg-slate-50">
      <option value="">{{ ownerWorkspaces.length ? $t("tender.wizard.f.workspaceDefault") : $t("tender.wizard.f.noWorkspace") }}</option>
      <option v-for="w in ownerWorkspaces" :key="w.id" :value="w.id">{{ w.name }}</option>
    </select>
  </label>
</template>
