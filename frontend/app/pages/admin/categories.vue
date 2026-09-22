<script setup lang="ts">
import type { CategoryDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const tree = ref<CategoryDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const form = reactive({ name: "", code: "", parentCategoryId: "" });

const importColumns = computed<ExcelColumn[]>(() => [
  { key: "name", label: t("categories.import.name"), required: true },
  { key: "code", label: t("categories.import.code"), required: true },
  { key: "parentCategory", label: t("categories.import.parentCategory"), hint: t("categories.import.parentCategoryHint") },
]);

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
      throw { data: { title: t("categories.import.parentNotFound", { name: parentName }) } };
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
  loading.value = true;
  try {
    tree.value = await api.get<CategoryDto[]>("/api/categories");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("categories.loadError")));
  } finally {
    loading.value = false;
  }
}

async function create() {
  submitting.value = true;
  try {
    await api.post("/api/categories", {
      name: form.name,
      code: form.code,
      parentCategoryId: form.parentCategoryId || null,
    });
    Object.assign(form, { name: "", code: "", parentCategoryId: "" });
    showForm.value = false;
    toast.success(t("toast.categoryCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("categories.createError")));
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("categories.title") }}</h1>
      <div class="flex gap-2">
        <ExcelImportButton :columns="importColumns" template-filename="kategoriler-sablon.xlsx" :import-row="importCategoryRow" @imported="load" />
        <AppButton @click="showForm = !showForm">
          {{ showForm ? $t("common.cancel") : $t("categories.new") }}
        </AppButton>
      </div>
    </div>

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.name" :placeholder="$t('categories.form.name')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.code" :placeholder="$t('categories.form.code')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <select v-model="form.parentCategoryId" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2">
        <option value="">{{ $t("categories.form.noParent") }}</option>
        <option v-for="fc in flatCategories" :key="fc.category.id" :value="fc.category.id">
          {{ "—".repeat(fc.depth) }} {{ fc.category.name }}
        </option>
      </select>
      <AppButton type="submit" class="sm:col-span-2" :loading="submitting">
        {{ submitting ? $t("common.saving") : $t("common.save") }}
      </AppButton>
    </form>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("categories.table.name") }}</th>
              <th class="pb-2">{{ $t("categories.table.code") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="fc in flatCategories" :key="fc.category.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700" :style="{ paddingLeft: `${fc.depth * 1.25}rem` }">
                {{ fc.category.name }}
              </td>
              <td class="py-2 font-mono text-xs text-slate-500">{{ fc.category.code }}</td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="flatCategories.length === 0">{{ $t("categories.empty") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
