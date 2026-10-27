import type { FirmDto } from "~/types/api";

const STORAGE_KEY = "pl_firm_id";

const selectedFirmId = ref<string | null>(null);
const firms = ref<FirmDto[]>([]);

/**
 * Başlıktaki firma seçimi. "Tüm firmalar" seçeneği yoktur: her zaman bir firma seçilidir (ilk açılışta varsayılan
 * firma). Listeler, işyeri seçilmemişse ("Tüm İşyerleri") bu firmanın işyerleriyle sınırlanır.
 */
export function useFirm() {
  function stored(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }

  function setFirm(id: string | null) {
    selectedFirmId.value = id;
    try {
      if (id) localStorage.setItem(STORAGE_KEY, id);
      else localStorage.removeItem(STORAGE_KEY);
    } catch {
      // localStorage kullanılamıyor (gizli pencere vb.)
    }
  }

  /** Kayıtlı seçim listede yoksa (silindi / yetkisiz) varsayılan firmaya, o da yoksa ilk firmaya düşer. */
  function ensureSelection() {
    const candidate = selectedFirmId.value ?? stored();
    if (candidate && firms.value.some((f) => f.id === candidate)) {
      setFirm(candidate);
      return;
    }
    setFirm(firms.value.find((f) => f.isDefault)?.id ?? firms.value[0]?.id ?? null);
  }

  const selectedFirm = computed(() => firms.value.find((f) => f.id === selectedFirmId.value) ?? null);

  return { firms, selectedFirmId: readonly(selectedFirmId), selectedFirm, setFirm, ensureSelection };
}
