/**
 * Sayfalı liste durumu: sayfa, toplam ve arama. Arama 300 ms bekleyip ilk sayfadan yeniden yükler;
 * sayfa değişince yükler. Yükleme fonksiyonu `page`, `pageSize` ve `search` değerlerini sorguya kendisi ekler.
 */
export function usePagedList(load: () => unknown, pageSize = 20) {
  const page = ref(1);
  const total = ref(0);
  const search = ref("");
  let timer: ReturnType<typeof setTimeout> | null = null;

  watch(page, () => load());
  watch(search, () => {
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => {
      if (page.value !== 1) page.value = 1; // sayfa watch'ı yükler
      else load();
    }, 300);
  });

  /** Başlıktaki firma/işyeri gibi dış filtre değişince: ilk sayfaya dön ve yükle. */
  function resetAndLoad() {
    if (page.value !== 1) page.value = 1;
    else load();
  }

  /** Sorgu parametreleri; cari/ürün/kullanıcı uçları aramayı `searchTerm` adıyla alır. */
  const params = (searchKey: "search" | "searchTerm" = "search") => ({
    page: page.value,
    pageSize,
    [searchKey]: search.value.trim() || undefined,
  });

  return { page, pageSize, total, search, params, resetAndLoad };
}
