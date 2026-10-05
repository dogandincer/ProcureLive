import type { WorkspaceDto } from "~/types/api";

const STORAGE_KEY = "pl_workspace_id";

const selectedWorkspaceId = ref<string | null>(null);
const workspaces = ref<WorkspaceDto[]>([]);

export function useWorkspace() {
  function init() {
    try {
      selectedWorkspaceId.value = localStorage.getItem(STORAGE_KEY);
    } catch {
      selectedWorkspaceId.value = null;
    }
  }

  function setWorkspace(id: string | null) {
    selectedWorkspaceId.value = id;
    try {
      if (id) {
        localStorage.setItem(STORAGE_KEY, id);
      } else {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      // localStorage unavailable (private window etc.)
    }
  }

  const selectedWorkspace = computed(() =>
    workspaces.value.find((w) => w.id === selectedWorkspaceId.value) ?? null,
  );

  return { selectedWorkspaceId: readonly(selectedWorkspaceId), selectedWorkspace, workspaces, setWorkspace, init };
}
