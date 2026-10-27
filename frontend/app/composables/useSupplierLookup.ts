import type { CompanyDto, LookupRow, PagedResult } from "~/types/api";

/**
 * Tedarikçi cari seçimi için "sarı alan" satırları. Liste seçim penceresi açıldığında yüklenir ve pencerede aranır;
 * sabit 100 kayıtlık açılır liste yerine kullanılır (alfabetik ilk 100'den sonraki cariler seçilemiyordu).
 * Satınalmacıya sunucu kendi kapsamındaki carileri döndürür.
 */
export function useSupplierLookup() {
  const api = useApi();

  async function loadSupplierRows(): Promise<LookupRow[]> {
    const page = await api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 1000, companyType: "Supplier" });
    return page.items.map((c) => ({
      id: c.id,
      code: c.code ?? "—",
      name: c.name,
      detail: [c.taxNumber, c.workspaceName].filter(Boolean).join(" · ") || null,
    }));
  }

  return { loadSupplierRows };
}
