<script setup lang="ts">
import type { CategoryDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const api = useApi();
const tree = ref<CategoryDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const errorMessage = ref("");

const form = reactive({ name: "", code: "", parentCategoryId: "" });

const importColumns: ExcelColumn[] = [
  { key: "name", label: "Kategori Adı", required: true },
  { key: "code", label: "Kod", required: true },
  { key: "parentCategory", label: "Üst Kategori", hint: "Opsiyonel - mevcut veya bu dosyada daha önce geçen bir kategori adı" },
];

// Resolves parent-by-name across rows in one import session so a parent created earlier in the same
// file (e.g. "Elektronik" in row 2) can be referenced by later rows (e.g. "Kablolar" -> "Elektronik").
let importSessionNameToId: Map<string, string> | null = null;

async function importCategoryRow(row: Record<string, string | number | boolean | null>) {
  if (!importSessionNameToId) {
    importSessionNameToId = new Map(flatCategories.value.map((fc) => [fc.category.name.toLocaleLowerCase("tr"), fc.category.id]));
  }

  const parentName = String(row.parentCategory ?? "").trim();
  let parentCategoryId: string | null = null;

  if (parentName) {
    parentCategoryId = importSessionNameToId.get(parentName.toLocaleLowerCase("tr")) ?? null;
    if (!parentCategoryId) {
      throw { data: { title: `Üst kategori bulunamadı: "${parentName}"` } };
    }
  }

  const newId = await api.post<string>("/api/categories", {
    name: row.name,
    code: row.code,
    parentCategoryId,
  });
  importSessionNameToId.set(String(row.name).toLocaleLowerCase("tr"), newId);
}

interface FlatCategory {
  category: CategoryDto;
  depth: number;
}

function flatten(list: CategoryDto[], depth = 0): FlatCategory[] {
  return list.flatMap((c) => [{ category: c, depth }, ...flatten(c.children, depth + 1)]);
}

const flatCategories = computed(() => flatten(tree.value));

async function load() {
  importSessionNameToId = null;
  try {
    tree.value = await api.get<CategoryDto[]>("/api/categories");
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Kayıtlar yüklenemedi.");
  }
}

async function create() {
  errorMessage.value = "";
  submitting.value = true;
  try {
    await api.post("/api/categories", {
      name: form.name,
      code: form.code,
      parentCategoryId: form.parentCategoryId || null,
    });
    Object.assign(form, { name: "", code: "", parentCategoryId: "" });
    showForm.value = false;
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Kategori oluşturulamadı.");
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">Kategoriler</h1>
      <div class="flex gap-2">
        <ExcelImportButton :columns="importColumns" template-filename="kategoriler-sablon.xlsx" :import-row="importCategoryRow" @imported="load" />
        <button class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white" @click="showForm = !showForm">
          {{ showForm ? "Vazgeç" : "+ Yeni Kategori" }}
        </button>
      </div>
    </div>

    <p v-if="errorMessage && !showForm" class="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{{ errorMessage }}</p>

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.name" placeholder="Kategori Adı" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.code" placeholder="Kod (örn. ELEKTRONIK)" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <select v-model="form.parentCategoryId" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2">
        <option value="">Üst kategori yok (kök kategori)</option>
        <option v-for="fc in flatCategories" :key="fc.category.id" :value="fc.category.id">
          {{ "—".repeat(fc.depth) }} {{ fc.category.name }}
        </option>
      </select>
      <p v-if="errorMessage" class="text-sm text-rose-600 sm:col-span-2">{{ errorMessage }}</p>
      <button type="submit" :disabled="submitting" class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white sm:col-span-2 disabled:opacity-50">
        {{ submitting ? "Kaydediliyor..." : "Kaydet" }}
      </button>
    </form>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Ad</th>
            <th class="pb-2">Kod</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="fc in flatCategories" :key="fc.category.id" class="border-b border-slate-50">
            <td class="py-2 font-medium text-slate-700" :style="{ paddingLeft: `${fc.depth * 1.25}rem` }">
              {{ fc.category.name }}
            </td>
            <td class="py-2 font-mono text-xs text-slate-500">{{ fc.category.code }}</td>
          </tr>
          <tr v-if="flatCategories.length === 0"><td colspan="2" class="py-6 text-center text-slate-400">Henüz kategori yok</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
