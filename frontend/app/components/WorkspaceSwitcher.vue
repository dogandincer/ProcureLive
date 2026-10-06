<script setup lang="ts">
import { BuildingStorefrontIcon, ChevronDownIcon } from "@heroicons/vue/24/outline";
import type { WorkspaceDto } from "~/types/api";

const api = useApi();
const { t } = useI18n();
const { selectedWorkspaceId, selectedWorkspace, workspaces, setWorkspace, init } = useWorkspace();
const open = ref(false);
const el = ref<HTMLElement | null>(null);

async function load() {
  try {
    workspaces.value = await api.get<WorkspaceDto[]>("/api/workspaces", { activeOnly: true });
  } catch {
    // non-critical; header should not crash over this
  }
}

function select(id: string | null) {
  setWorkspace(id);
  open.value = false;
}

function handleClickOutside(event: MouseEvent) {
  if (el.value && !el.value.contains(event.target as Node)) {
    open.value = false;
  }
}

onMounted(async () => {
  document.addEventListener("click", handleClickOutside);
  init();
  await load();
});

onBeforeUnmount(() => {
  document.removeEventListener("click", handleClickOutside);
});
</script>

<template>
  <div ref="el" class="relative">
    <button
      class="flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 hover:text-slate-800 transition-colors"
      :class="{ 'border-indigo-400 bg-indigo-50 text-indigo-700': selectedWorkspaceId }"
      @click="open = !open"
    >
      <BuildingStorefrontIcon class="h-3.5 w-3.5 shrink-0" />
      <span class="max-w-[120px] truncate">
        {{ selectedWorkspace?.name ?? $t("workspaces.allWorkspaces") }}
      </span>
      <ChevronDownIcon class="h-3 w-3 shrink-0" :class="{ 'rotate-180': open }" />
    </button>

    <div
      v-if="open"
      class="absolute right-0 z-30 mt-1.5 min-w-[180px] rounded-xl border border-slate-200 bg-white py-1 shadow-lg"
    >
      <button
        class="flex w-full items-center gap-2 px-3 py-2 text-left text-xs hover:bg-slate-50"
        :class="!selectedWorkspaceId ? 'font-semibold text-indigo-600' : 'text-slate-600'"
        @click="select(null)"
      >
        {{ $t("workspaces.allWorkspaces") }}
      </button>
      <div v-if="workspaces.length > 0" class="my-1 border-t border-slate-100" />
      <button
        v-for="ws in workspaces"
        :key="ws.id"
        class="flex w-full items-center gap-2 px-3 py-2 text-left text-xs hover:bg-slate-50"
        :class="selectedWorkspaceId === ws.id ? 'font-semibold text-indigo-600' : 'text-slate-600'"
        @click="select(ws.id)"
      >
        <span class="flex-1 truncate">{{ ws.name }}</span>
        <span class="shrink-0 text-[10px] text-slate-400">{{ ws.companyCount }}C / {{ ws.productCount }}P</span>
      </button>
      <div v-if="workspaces.length === 0" class="px-3 py-2 text-[11px] text-slate-400">
        {{ $t("workspaces.noWorkspaces") }}
      </div>
    </div>
  </div>
</template>
