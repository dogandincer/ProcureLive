<script setup lang="ts">
import type { AuctionDetailDto, ResponsibleCandidateDto } from "~/types/api";
import { PencilSquareIcon } from "@heroicons/vue/24/outline";

/**
 * İhalenin işyeri ve sorumlu satınalmacısı (katılım talepleri sorumluya düşer). İşyeri yalnızca taslak/planlanmış
 * ihalede değişir; sorumlu adayları seçilen işyerine yetkili aktif satınalmacılar + Admin'lerdir (sunucu da zorlar).
 */
const props = defineProps<{ auction: AuctionDetailDto }>();
const emit = defineEmits<{ changed: [] }>();
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { workspaces } = useWorkspace();

const editing = ref(false);
const saving = ref(false);
const workspaceId = ref<string>("");
const responsibleUserId = ref<string>("");
const candidates = ref<ResponsibleCandidateDto[]>([]);
const loadingCandidates = ref(false);

const workspaceEditable = computed(() => ["Draft", "Scheduled"].includes(props.auction.status));
// Başlık listesi satınalmacı için zaten yetkili işyerleriyle sınırlı gelir.
const firmWorkspaces = computed(() =>
  workspaces.value.filter((w) => w.firmId === props.auction.ownerCompanyId && (w.isActive || w.id === props.auction.workspaceId)),
);

async function loadCandidates() {
  loadingCandidates.value = true;
  try {
    candidates.value = await api.get<ResponsibleCandidateDto[]>(`/api/auctions/${props.auction.id}/responsible-candidates`, {
      workspaceId: workspaceId.value || undefined,
    });
    if (!candidates.value.some((c) => c.id === responsibleUserId.value)) responsibleUserId.value = "";
  } catch (error: any) {
    candidates.value = [];
    toast.error(apiErrorMessage(error, t("auctions.assignment.candidatesError")));
  } finally {
    loadingCandidates.value = false;
  }
}

function open() {
  workspaceId.value = props.auction.workspaceId ?? "";
  responsibleUserId.value = props.auction.responsibleUserId ?? "";
  editing.value = true;
  loadCandidates();
}

watch(workspaceId, () => {
  if (editing.value) loadCandidates();
});

async function save() {
  if (!responsibleUserId.value) return;
  saving.value = true;
  try {
    await api.put(`/api/auctions/${props.auction.id}/assignment`, {
      workspaceId: workspaceId.value || null,
      responsibleUserId: responsibleUserId.value,
    });
    toast.success(t("auctions.assignment.saved"));
    editing.value = false;
    emit("changed");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.assignment.saveError")));
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <div class="mt-1 text-xs text-slate-500">
    <div v-if="!editing" class="flex flex-wrap items-center gap-x-1 gap-y-0.5" data-testid="auction-assignment">
      <span>{{ $t("auctions.detail.workspace") }}: <span class="font-medium text-slate-700">{{ auction.workspaceName ?? "—" }}</span></span>
      <span>·</span>
      <span>{{ $t("auctions.detail.responsible") }}: <span class="font-medium text-slate-700">{{ auction.responsibleUserName ?? "—" }}</span></span>
      <button type="button" class="ml-1 inline-flex items-center gap-0.5 text-indigo-600 hover:underline" @click="open">
        <PencilSquareIcon class="h-3.5 w-3.5" />{{ $t("common.edit") }}
      </button>
    </div>

    <form v-else class="mt-1 flex flex-wrap items-end gap-2 rounded-lg border border-slate-200 bg-white p-2" @submit.prevent="save">
      <label class="flex flex-col gap-0.5">
        <span>{{ $t("auctions.detail.workspace") }}</span>
        <select v-model="workspaceId" :disabled="!workspaceEditable" class="rounded-md border border-slate-300 px-2 py-1 text-sm disabled:bg-slate-50">
          <option v-for="w in firmWorkspaces" :key="w.id" :value="w.id">{{ w.name }}</option>
        </select>
      </label>
      <label class="flex flex-col gap-0.5">
        <span>{{ $t("auctions.detail.responsible") }}</span>
        <select v-model="responsibleUserId" required :disabled="loadingCandidates" class="min-w-[12rem] rounded-md border border-slate-300 px-2 py-1 text-sm">
          <option value="" disabled>{{ loadingCandidates ? $t("common.loading") : $t("auctions.assignment.selectResponsible") }}</option>
          <option v-for="c in candidates" :key="c.id" :value="c.id">{{ c.fullName }}{{ c.isAdmin ? " (Admin)" : "" }}</option>
        </select>
      </label>
      <AppButton type="submit" :loading="saving" :disabled="!responsibleUserId">{{ $t("common.save") }}</AppButton>
      <button type="button" class="rounded-md px-2 py-1 text-sm text-slate-500 hover:bg-slate-100" @click="editing = false">{{ $t("common.cancel") }}</button>
      <p v-if="!workspaceEditable" class="basis-full text-[11px] text-slate-400">{{ $t("auctions.assignment.workspaceLocked") }}</p>
    </form>
  </div>
</template>
