import * as XLSX from "xlsx";
import * as cptable from "xlsx/dist/cpexcel.full.mjs";

// Loads the full codepage table set so non-UTF-8 CSV exports (e.g. Windows-1254, common for
// Turkish-locale Excel "CSV" saves) decode correctly instead of producing garbled text.
XLSX.set_cptable(cptable);

export interface ExcelColumn {
  /** Field name passed to the row object and to the importRow callback. */
  key: string;
  /** Column header shown in the template file and matched (case/space-insensitive) against the uploaded file's header row. */
  label: string;
  required?: boolean;
  type?: "string" | "number" | "boolean";
  /** Shown next to the column in the template hint row, e.g. "Supplier veya Internal". */
  hint?: string;
}

export interface ParsedExcelRow {
  rowNumber: number;
  values: Record<string, string | number | boolean | null>;
  errors: string[];
}

function normalizeHeader(header: string): string {
  return header
    .toLocaleLowerCase("tr")
    .replace(/\(.*?\)/g, "")
    .replace(/[^a-z0-9ğüşıöç]/gi, "")
    .trim();
}

function coerce(raw: unknown, type: ExcelColumn["type"]): string | number | boolean | null {
  if (raw === undefined || raw === null || raw === "") {
    return null;
  }

  if (type === "number") {
    const num = typeof raw === "number" ? raw : Number(String(raw).replace(",", "."));
    return Number.isFinite(num) ? num : null;
  }

  if (type === "boolean") {
    const text = String(raw).trim().toLocaleLowerCase("tr");
    return ["evet", "true", "1", "yes", "doğru"].includes(text);
  }

  return String(raw).trim();
}

export function useExcelImport() {
  function parseFile(file: File, columns: ExcelColumn[]): Promise<ParsedExcelRow[]> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onerror = () => reject(new Error("Dosya okunamadı."));

      reader.onload = () => {
        try {
          const data = new Uint8Array(reader.result as ArrayBuffer);
          // codepage 65001 = UTF-8. Needed for CSV specifically (no built-in encoding marker, unlike
          // .xlsx's internal XML which is always UTF-8) - without it, Turkish characters in headers
          // (ı, ü, ş, ğ, ö, ç) get misread and silently fail to match against the column schema.
          const workbook = XLSX.read(data, { type: "array", codepage: 65001 });
          const firstSheetName = workbook.SheetNames[0];

          if (!firstSheetName) {
            resolve([]);
            return;
          }

          const sheet = workbook.Sheets[firstSheetName];
          const rawRows = XLSX.utils.sheet_to_json<Record<string, unknown>>(sheet, { defval: null, raw: true });

          const headerByNormalized = new Map<string, string>();
          if (rawRows.length > 0) {
            for (const header of Object.keys(rawRows[0]!)) {
              headerByNormalized.set(normalizeHeader(header), header);
            }
          }

          const parsed: ParsedExcelRow[] = rawRows.map((rawRow, index) => {
            const values: Record<string, string | number | boolean | null> = {};
            const errors: string[] = [];

            for (const column of columns) {
              const matchedHeader =
                headerByNormalized.get(normalizeHeader(column.label)) ?? headerByNormalized.get(normalizeHeader(column.key));
              const rawValue = matchedHeader ? rawRow[matchedHeader] : null;
              const value = coerce(rawValue, column.type);

              if (column.required && (value === null || value === "")) {
                errors.push(`"${column.label}" zorunlu.`);
              }

              values[column.key] = value;
            }

            return { rowNumber: index + 2, values, errors }; // +2: header row is row 1, data starts at row 2
          });

          resolve(parsed);
        } catch {
          reject(new Error("Dosya okunamadı. Geçerli bir Excel (.xlsx) veya CSV dosyası seçin."));
        }
      };

      reader.readAsArrayBuffer(file);
    });
  }

  function downloadTemplate(columns: ExcelColumn[], filename: string) {
    const headerRow = columns.map((c) => c.label);
    const hintRow = columns.map((c) => c.hint ?? (c.required ? "(zorunlu)" : "(opsiyonel)"));
    const worksheet = XLSX.utils.aoa_to_sheet([headerRow, hintRow]);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "Şablon");
    XLSX.writeFile(workbook, filename);
  }

  return { parseFile, downloadTemplate };
}
