<script setup lang="ts">
import type { ExcelColumn, ParsedExcelRow } from "~/composables/useExcelImport";

const props = withDefaults(
  defineProps<{
    columns: ExcelColumn[];
    importRow: (row: Record<string, string | number | boolean | null>) => Promise<void>;
    templateFilename?: string;
    label?: string;
  }>(),
  { templateFilename: "sablon.xlsx" },
);

const emit = defineEmits<{ imported: [successCount: number] }>();

const { t } = useI18n();
const { parseFile, downloadTemplate } = useExcelImport();
const displayLabel = computed(() => props.label ?? t("excelImport.defaultLabel"));

const open = ref(false);
const fileInputRef = ref<HTMLInputElement | null>(null);
const stage = ref<"idle" | "preview" | "done">("idle");
const parsing = ref(false);
const importing = ref(false);
const parsedRows = ref<ParsedExcelRow[]>([]);
const results = ref<{ rowNumber: number; status: "success" | "error"; message?: string }[]>([]);
const fileError = ref("");

const validRows = computed(() => parsedRows.value.filter((r) => r.errors.length === 0));
const invalidRows = computed(() => parsedRows.value.filter((r) => r.errors.length > 0));
const successCount = computed(() => results.value.filter((r) => r.status === "success").length);
const errorCount = computed(() => results.value.filter((r) => r.status === "error").length);

function openPanel() {
  open.value = true;
  stage.value = "idle";
  parsedRows.value = [];
  results.value = [];
  fileError.value = "";
}

function closePanel() {
  open.value = false;
  if (results.value.length > 0 && successCount.value > 0) {
    emit("imported", successCount.value);
  }
}

function triggerFilePicker() {
  fileInputRef.value?.click();
}

async function onFileSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;

  fileError.value = "";
  parsing.value = true;
  try {
    parsedRows.value = await parseFile(file, props.columns);
    stage.value = "preview";
  } catch (error: any) {
    fileError.value = error?.message ?? t("excelImport.fileReadError");
  } finally {
    parsing.value = false;
    input.value = "";
  }
}

async function confirmImport() {
  importing.value = true;
  results.value = [];

  for (const row of validRows.value) {
    try {
      await props.importRow(row.values);
      results.value.push({ rowNumber: row.rowNumber, status: "success" });
    } catch (error: any) {
      results.value.push({ rowNumber: row.rowNumber, status: "error", message: apiErrorMessage(error, t("excelImport.rowImportError")) });
    }
  }

  importing.value = false;
  stage.value = "done";
}
</script>

<template>
  <div class="relative inline-block">
    <button
      type="button"
      class="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-semibold text-slate-700 hover:bg-slate-50"
      @click="openPanel"
    >
      📥 {{ displayLabel }}
    </button>

    <div v-if="open" class="fixed inset-0 z-30 flex items-center justify-center bg-slate-900/30 p-4" @click.self="closePanel">
      <div class="max-h-[85vh] w-full max-w-2xl overflow-y-auto rounded-xl bg-white p-5 shadow-xl">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-slate-800">{{ displayLabel }}</h3>
          <button class="text-sm text-slate-400 hover:text-slate-600" @click="closePanel">✕</button>
        </div>

        <div v-if="stage === 'idle'" class="mt-4 space-y-3">
          <button
            type="button"
            class="text-xs font-medium text-indigo-600 hover:underline"
            @click="downloadTemplate(columns, templateFilename)"
          >
            📄 {{ $t("excelImport.downloadTemplate") }}
          </button>
          <p class="text-xs text-slate-500">
            {{ $t("excelImport.expectedColumns") }}
            {{ columns.map((c) => c.label + (c.required ? "*" : "")).join(", ") }}
          </p>
          <input ref="fileInputRef" type="file" accept=".xlsx,.xls,.csv" class="hidden" @change="onFileSelected" />
          <button
            type="button"
            :disabled="parsing"
            class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
            @click="triggerFilePicker"
          >
            {{ parsing ? $t("excelImport.reading") : $t("excelImport.chooseFile") }}
          </button>
          <p v-if="fileError" class="text-sm text-rose-600">{{ fileError }}</p>
        </div>

        <div v-else-if="stage === 'preview'" class="mt-4 space-y-3">
          <p class="text-sm text-slate-600">
            {{ $t("excelImport.rowsRead", { count: parsedRows.length }) }}
            <span class="text-emerald-600">{{ $t("excelImport.validCount", { count: validRows.length }) }}</span>,
            <span v-if="invalidRows.length > 0" class="text-rose-600">{{ $t("excelImport.invalidCount", { count: invalidRows.length }) }}</span>
          </p>

          <div class="max-h-64 overflow-y-auto rounded-lg border border-slate-100">
            <table class="w-full text-xs">
              <thead class="sticky top-0 bg-slate-50">
                <tr class="text-left text-slate-400">
                  <th class="px-2 py-1">{{ $t("excelImport.rowHeader") }}</th>
                  <th v-for="c in columns" :key="c.key" class="px-2 py-1">{{ c.label }}</th>
                  <th class="px-2 py-1">{{ $t("excelImport.statusHeader") }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in parsedRows" :key="row.rowNumber" class="border-t border-slate-50" :class="{ 'bg-rose-50': row.errors.length > 0 }">
                  <td class="px-2 py-1 text-slate-400">{{ row.rowNumber }}</td>
                  <td v-for="c in columns" :key="c.key" class="px-2 py-1 text-slate-700">{{ row.values[c.key] ?? "-" }}</td>
                  <td class="px-2 py-1">
                    <span v-if="row.errors.length === 0" class="text-emerald-600">{{ $t("excelImport.ready") }}</span>
                    <span v-else class="text-rose-600">{{ row.errors.join(" ") }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="flex gap-2">
            <button
              type="button"
              :disabled="validRows.length === 0 || importing"
              class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
              @click="confirmImport"
            >
              {{ importing ? $t("excelImport.importing") : $t("excelImport.importRows", { count: validRows.length }) }}
            </button>
            <button type="button" class="rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600" @click="openPanel">
              {{ $t("excelImport.cancel") }}
            </button>
          </div>
        </div>

        <div v-else-if="stage === 'done'" class="mt-4 space-y-3">
          <p class="text-sm font-medium text-slate-700">
            <span class="text-emerald-600">{{ $t("excelImport.successCount", { count: successCount }) }}</span>
            <span v-if="errorCount > 0">, <span class="text-rose-600">{{ $t("excelImport.errorCount", { count: errorCount }) }}</span></span>
          </p>
          <ul v-if="errorCount > 0" class="max-h-48 space-y-1 overflow-y-auto text-xs">
            <li v-for="r in results.filter((x) => x.status === 'error')" :key="r.rowNumber" class="text-rose-600">
              {{ $t("excelImport.rowLabel", { number: r.rowNumber }) }}: {{ r.message }}
            </li>
          </ul>
          <button type="button" class="rounded-md bg-slate-800 px-4 py-2 text-sm font-semibold text-white" @click="closePanel">
            {{ $t("excelImport.close") }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
