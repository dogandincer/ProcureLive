<script setup lang="ts">
import { PencilSquareIcon } from "@heroicons/vue/24/outline";

/**
 * RFI / RFQ sürecinin işyeri: satınalmacı kapsamı bu işyerine göre uygulanır. Kapanmış süreçte değişmez (sunucu da zorlar).
 * Seçenekler sahip firmanın aktif işyerleridir (satınalmacıya başlık listesi zaten yetkili işyerleriyle sınırlı gelir).
 */
const props = defineProps<{
  kind: "rfi" | "rfq";
  processId: string;
  ownerCompanyId: string;
  workspaceId: string | null;
  workspaceName: string | null;
  status: string;
}>();
const emit = defineEmits<{ changed: [] }>();
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { workspaces } = useWorkspace();

const editing = ref(false);
const saving = ref(false);
const target = ref("");

const editable = computed(() => !["Closed", "Evaluated", "Awarded"].includes(props.status));
const options = computed(() => workspaces.value.filter((w) => w.isActive && w.firmId === props.ownerCompanyId));

function open() {
  target.value = props.workspaceId ?? options.value[0]?.id ?? "";
  editing.value = true;
}

async function save() {
  if (!target.value) return;
  saving.value = true;
  try {
    await api.put(`/api/${props.kind}/processes/${props.processId}/workspace`, { workspaceId: target.value });
    toast.success(t("processWorkspace.saved"));
    editing.value = false;
    emit("changed");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("processWorkspace.saveError")));
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <div class="mt-1 text-xs text-slate-500" data-testid="process-workspace">
    <div v-if="!editing" class="flex flex-wrap items-center gap-1">
      <span>{{ $t("processWorkspace.workspace") }}: <span class="font-medium text-slate-700">{{ workspaceName ?? "—" }}</span></span>
      <button v-if="editable && options.length" type="button" class="ml-1 inline-flex items-center gap-0.5 text-indigo-600 hover:underline" @click="open">
        <PencilSquareIcon class="h-3.5 w-3.5" />{{ $t("common.edit") }}
      </button>
    </div>
    <form v-else class="mt-1 flex flex-wrap items-center gap-2" @submit.prevent="save">
      <select v-model="target" required :aria-label="$t('processWorkspace.workspace')" class="rounded-md border border-slate-300 px-2 py-1 text-sm">
        <option v-for="w in options" :key="w.id" :value="w.id">{{ w.name }}</option>
      </select>
      <AppButton type="submit" :loading="saving">{{ $t("common.save") }}</AppButton>
      <button type="button" class="rounded-md px-2 py-1 text-sm text-slate-500 hover:bg-slate-100" @click="editing = false">{{ $t("common.cancel") }}</button>
    </form>
  </div>
</template>
