import type { FirmDto } from "~/types/api";

const STORAGE_KEY = "pl_firm_id";
/** Kullanıcının bilinçli "Tüm firmalar" seçimi. */
const ALL = "__all__";

const selectedFirmId = ref<string | null>(null);
const firms = ref<FirmDto[]>([]);

/**
 * Başlıktaki firma seçimi. Birden çok firmaya erişen kullanıcı (Admin, çok firmalı satınalmacı) "Tüm firmalar"
 * seçebilir (null) ve ilk açılışta bunu görür; tek firmalı kullanıcıda o firma seçilidir. Listeler, işyeri
 * seçilmemişse ("Tüm İşyerleri") seçili firmanın — firma da seçilmemişse kapsamdaki tüm firmaların — kayıtlarını gösterir.
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
      localStorage.setItem(STORAGE_KEY, id ?? ALL);
    } catch {
      // localStorage kullanılamıyor (gizli pencere vb.)
    }
  }

  const allowAll = computed(() => firms.value.length > 1);

  /**
   * Kayıtlı seçim geçerliyse korunur; "Tüm firmalar" yalnızca birden çok firma varken geçerlidir.
   * İlk açılışta (kayıt yok) çok firmalı kullanıcıda "Tüm firmalar", tek firmalıda o firma seçilir.
   */
  function ensureSelection() {
    const saved = stored();
    const candidate = selectedFirmId.value ?? (saved === ALL ? null : saved);
    if (candidate && firms.value.some((f) => f.id === candidate)) {
      setFirm(candidate);
      return;
    }
    if (allowAll.value && (saved === ALL || saved === null || candidate)) {
      setFirm(null);
      return;
    }
    setFirm(firms.value.find((f) => f.isDefault)?.id ?? firms.value[0]?.id ?? null);
  }

  const selectedFirm = computed(() => firms.value.find((f) => f.id === selectedFirmId.value) ?? null);

  return { firms, selectedFirmId: readonly(selectedFirmId), selectedFirm, allowAll, setFirm, ensureSelection };
}
