<script setup lang="ts">
import type { CategoryDto, PagedResult, ProductDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { selectedWorkspaceId } = useWorkspace();
const products = ref<ProductDto[]>([]);
const categories = ref<CategoryDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const form = reactive({ sku: "", name: "", categoryId: "", unitOfMeasure: "Piece", brand: "" });
const unitOfMeasures = ["Piece", "Kilogram", "Gram", "Liter", "Meter", "SquareMeter", "CubicMeter", "Box", "Pallet", "Hour", "Service"];
const unitLabel = (u: string) => t(`enums.unitOfMeasure.${u}`);

const importColumns = computed<ExcelColumn[]>(() => [
  { key: "sku", label: "SKU", required: true },
  { key: "name", label: t("products.import.name"), required: true },
  { key: "category", label: t("products.import.category"), required: true, hint: t("products.import.categoryHint") },
  { key: "unitOfMeasure", label: t("products.import.unit"), hint: unitOfMeasures.join("/") },
  { key: "brand", label: t("products.import.brand") },
]);

async function importProductRow(row: Record<string, string | number | boolean | null>) {
  const categoryName = String(row.category ?? "").trim();
  const category = categories.value.find((c) => c.name.toLocaleLowerCase("tr") === categoryName.toLocaleLowerCase("tr"));

  if (!category) {
    throw { data: { title: t("products.import.categoryNotFound", { name: categoryName }) } };
  }

  await api.post("/api/products", {
    sku: row.sku,
    name: row.name,
    description: null,
    categoryId: category.id,
    unitOfMeasure: unitOfMeasures.includes(String(row.unitOfMeasure)) ? row.unitOfMeasure : "Piece",
    brand: row.brand || null,
    defaultCurrencyId: null,
  });
}

function flattenCategories(list: CategoryDto[]): CategoryDto[] {
  return list.flatMap((c) => [c, ...flattenCategories(c.children)]);
}

async function load() {
  loading.value = true;
  try {
    const params: Record<string, unknown> = { page: 1, pageSize: 100 };
    if (selectedWorkspaceId.value) params.workspaceId = selectedWorkspaceId.value;
    const [productPage, categoryTree] = await Promise.all([
      api.get<PagedResult<ProductDto>>("/api/products", params),
      api.get<CategoryDto[]>("/api/categories"),
    ]);
    products.value = productPage.items;
    categories.value = flattenCategories(categoryTree);
    if (!form.categoryId && categories.value.length > 0) {
      form.categoryId = categories.value[0].id;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.loadError")));
  } finally {
    loading.value = false;
  }
}

async function create() {
  submitting.value = true;
  try {
    await api.post("/api/products", {
      sku: form.sku,
      name: form.name,
      description: null,
      categoryId: form.categoryId,
      unitOfMeasure: form.unitOfMeasure,
      brand: form.brand || null,
      defaultCurrencyId: null,
      workspaceId: selectedWorkspaceId.value || null,
    });
    Object.assign(form, { sku: "", name: "", brand: "" });
    showForm.value = false;
    toast.success(t("toast.productCreated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.createError")));
  } finally {
    submitting.value = false;
  }
}

watch(selectedWorkspaceId, load);
onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("products.title") }}</h1>
      <div class="flex gap-2">
        <ExcelImportButton :columns="importColumns" template-filename="urunler-sablon.xlsx" :import-row="importProductRow" @imported="load" />
        <AppButton @click="showForm = !showForm">
          {{ showForm ? $t("common.cancel") : $t("products.new") }}
        </AppButton>
      </div>
    </div>

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.sku" :placeholder="$t('products.form.sku')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.name" :placeholder="$t('products.form.name')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <select v-model="form.categoryId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <select v-model="form.unitOfMeasure" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ unitLabel(u) }}</option>
      </select>
      <input v-model="form.brand" :placeholder="$t('products.form.brand')" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
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
              <th class="pb-2">{{ $t("products.table.sku") }}</th>
              <th class="pb-2">{{ $t("products.table.name") }}</th>
              <th class="pb-2">{{ $t("products.table.category") }}</th>
              <th class="pb-2">{{ $t("products.table.unit") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in products" :key="p.id" class="border-b border-slate-50">
              <td class="py-2 font-mono text-xs text-slate-500">{{ p.sku }}</td>
              <td class="py-2 font-medium text-slate-700">{{ p.name }}</td>
              <td class="py-2 text-slate-500">{{ p.categoryName }}</td>
              <td class="py-2 text-slate-500">{{ unitLabel(p.unitOfMeasure) }}</td>
              <td class="py-2"><AppBadge :tone="p.isActive ? 'emerald' : 'slate'">{{ p.isActive ? $t("common.active") : $t("common.passive") }}</AppBadge></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="products.length === 0">{{ $t("products.empty") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
