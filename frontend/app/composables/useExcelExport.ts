import * as XLSX from "xlsx";

export interface ExportColumn<T> {
  header: string;
  value: (row: T) => string | number | null | undefined;
  width?: number;
}

/**
 * Tablo verisini .xlsx olarak indirir (tarayıcıda üretilir; sunucuya veri gitmez).
 * Dosya adı Türkçe karakterleri korur; sayı alanları Excel'de sayı olarak kalır.
 */
export function useExcelExport() {
  function exportRows<T>(fileName: string, sheetName: string, columns: ExportColumn<T>[], rows: T[]) {
    const aoa = [columns.map((c) => c.header), ...rows.map((r) => columns.map((c) => c.value(r) ?? ""))];
    const sheet = XLSX.utils.aoa_to_sheet(aoa);
    sheet["!cols"] = columns.map((c) => ({ wch: c.width ?? Math.max(12, c.header.length + 2) }));
    const book = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(book, sheet, sheetName.slice(0, 31));
    const stamp = new Date().toISOString().slice(0, 10);
    XLSX.writeFile(book, `${fileName.replace(/[\/:*?"<>|]+/g, "-")}-${stamp}.xlsx`);
  }

  return { exportRows };
}
