import type { WorkspaceDto } from "~/types/api";

const STORAGE_KEY = "pl_workspace_id";
/** Kullanıcının bilinçli "Tüm İşyerleri" seçimi (hiç seçim yapılmamış olmasından ayırt etmek için). */
const ALL = "__all__";

const selectedWorkspaceId = ref<string | null>(null);
const workspaces = ref<WorkspaceDto[]>([]);
let hasStoredChoice = false;

export function useWorkspace() {
  function init() {
    try {
      const value = localStorage.getItem(STORAGE_KEY);
      hasStoredChoice = value !== null;
      selectedWorkspaceId.value = value === null || value === ALL ? null : value;
    } catch {
      selectedWorkspaceId.value = null;
    }
  }

  function setWorkspace(id: string | null) {
    selectedWorkspaceId.value = id;
    hasStoredChoice = true;
    try {
      localStorage.setItem(STORAGE_KEY, id ?? ALL);
    } catch {
      // localStorage unavailable (private window etc.)
    }
  }

  /**
   * Seçili işyeri, seçili firmaya ait değilse firmanın varsayılan işyerine geçilir (yoksa "Tüm İşyerleri").
   * Kullanıcı bilinçli olarak "Tüm İşyerleri" seçtiyse o korunur.
   */
  function reconcile(firmId: string | null) {
    const inFirm = workspaces.value.filter((w) => w.firmId === firmId && w.isActive);
    const current = selectedWorkspaceId.value;
    if (current && inFirm.some((w) => w.id === current)) return;
    if (current === null && hasStoredChoice) return;
    selectedWorkspaceId.value = inFirm.find((w) => w.isDefault)?.id ?? null;
  }

  const selectedWorkspace = computed(() =>
    workspaces.value.find((w) => w.id === selectedWorkspaceId.value) ?? null,
  );

  return { selectedWorkspaceId: readonly(selectedWorkspaceId), selectedWorkspace, workspaces, setWorkspace, reconcile, init };
}
