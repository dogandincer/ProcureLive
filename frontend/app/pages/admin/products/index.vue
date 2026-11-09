<script setup lang="ts">
import type { CategoryDto, PagedResult, ProductDto } from "~/types/api";
import type { ExcelColumn } from "~/composables/useExcelImport";
import { CubeTransparentIcon, TrashIcon } from "@heroicons/vue/24/outline";

const api = useApi();
const { ask } = useConfirm();
const { can } = usePermissions();
const toast = useToast();
const { t } = useI18n();
const { selectedWorkspaceId } = useWorkspace();
const { selectedFirmId } = useFirm();
const products = ref<ProductDto[]>([]);
const categories = ref<CategoryDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const form = reactive({ sku: "", name: "", categoryId: "", unitOfMeasure: "Piece", brand: "", workspaceId: "" });
// Excel aktarımındaki satırların açılacağı işyeri (aktarım penceresinde seçilir).
const importWorkspaceId = ref("");
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
    workspaceId: importWorkspaceId.value || null,
  });
}

function flattenCategories(list: CategoryDto[]): CategoryDto[] {
  return list.flatMap((c) => [c, ...flattenCategories(c.children)]);
}

// Başlıktaki firma/işyeri açılışta art arda kurulur ve her değişim yeniden yükler; geç dönen eski (ör. filtresiz)
// yanıt yenisinin üzerine yazmasın diye yalnızca son isteğin sonucu uygulanır.
let loadSeq = 0;
const { page: listPage, total: listTotal, search: listSearch, pageSize: listPageSize, params: listParams, resetAndLoad } = usePagedList(() => load());


async function load() {
  const seq = ++loadSeq;
  loading.value = true;
  try {
    const params: Record<string, unknown> = { ...listParams("searchTerm") };
    // "Tüm İşyerleri": başlıktaki firmanın işyerleriyle sınırlı.
    if (selectedWorkspaceId.value) params.workspaceId = selectedWorkspaceId.value;
    else if (selectedFirmId.value) params.firmId = selectedFirmId.value;
    const [productPage, categoryTree] = await Promise.all([
      api.get<PagedResult<ProductDto>>("/api/products", params),
      api.get<CategoryDto[]>("/api/categories"),
    ]);
    if (seq !== loadSeq) return;
    products.value = productPage.items;
    listTotal.value = productPage.totalCount;
    categories.value = flattenCategories(categoryTree);
    if (!form.categoryId && categories.value.length > 0) {
      form.categoryId = categories.value[0].id;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.loadError")));
  } finally {
    if (seq === loadSeq) loading.value = false;
  }
}

async function create() {
  submitting.value = true;
  try {
    const id = await api.post<string>("/api/products", {
      sku: form.sku,
      name: form.name,
      description: null,
      categoryId: form.categoryId,
      unitOfMeasure: form.unitOfMeasure,
      brand: form.brand || null,
      defaultCurrencyId: null,
      workspaceId: form.workspaceId || null,
    });
    Object.assign(form, { sku: "", name: "", brand: "" });
    showForm.value = false;
    // Görsel, 3D model ve teknik bilgiler detayda eklenir; kullanıcı yeni kaydı listede aramak zorunda kalmasın.
    toast.success(t("products.createdGoDetail"));
    await navigateTo(`/admin/products/${id}`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.createError")));
  } finally {
    submitting.value = false;
  }
}

const deleting = ref<string | null>(null);
async function deleteProduct(p: ProductDto) {
  if (!(await ask({ title: t("products.deleteConfirm", { name: p.name }), tone: "danger", confirmLabel: t("common.delete") }))) return;
  deleting.value = p.id;
  try {
    await api.delete(`/api/products/${p.id}`);
    toast.success(t("products.deleted"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.deleteError")));
  } finally {
    deleting.value = null;
  }
}

watch([selectedWorkspaceId, selectedFirmId], () => resetAndLoad());
onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("products.title") }}</h1>
      <div class="flex gap-2">
        <ErpImportButton kind="items" @imported="load" />
        <ExcelImportButton :columns="importColumns" template-filename="urunler-sablon.xlsx" :import-row="importProductRow" @imported="load">
          <template #options><WorkspaceField v-model="importWorkspaceId" :label="$t('excelImport.targetWorkspace')" /></template>
        </ExcelImportButton>
        <AppButton v-if="can('products.manage')" @click="showForm = !showForm">
          {{ showForm ? $t("common.cancel") : $t("products.new") }}
        </AppButton>
      </div>
    </div>

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-surface p-5 sm:grid-cols-2" data-testid="product-form" @submit.prevent="create">
      <FormField :label="$t('products.form.sku')" required>
        <input v-model="form.sku" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      </FormField>
      <FormField :label="$t('products.form.name')" required>
        <input v-model="form.name" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      </FormField>
      <FormField :label="$t('products.form.category')" required>
        <select v-model="form.categoryId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
          <option value="" disabled>{{ $t("products.form.categoryPick") }}</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </FormField>
      <FormField :label="$t('products.form.unit')">
        <select v-model="form.unitOfMeasure" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
          <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ unitLabel(u) }}</option>
        </select>
      </FormField>
      <FormField :label="$t('products.form.brand')">
        <input v-model="form.brand" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      </FormField>
      <WorkspaceField v-model="form.workspaceId" />
      <div class="flex justify-end sm:col-span-2">
        <AppButton type="submit" :loading="submitting">
          {{ submitting ? $t("common.saving") : $t("common.save") }}
        </AppButton>
      </div>
    </form>

    <AppCard>
      <input v-model="listSearch" type="search" :placeholder="$t('pager.search.products')" :aria-label="$t('pager.search.products')" class="mb-3 w-full max-w-sm rounded-md border border-slate-300 px-3 py-1.5 text-sm" data-testid="list-search" />
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
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in products" :key="p.id" class="border-b border-slate-50">
              <td class="py-2 font-mono text-xs text-slate-500">{{ p.sku }}</td>
              <td class="py-2 font-medium text-slate-700">
                <NuxtLink :to="`/admin/products/${p.id}`" class="text-slate-800 hover:text-indigo-600 hover:underline">{{ p.name }}</NuxtLink>
                <CubeTransparentIcon v-if="p.model3dUrl" class="ml-1 inline h-3.5 w-3.5 text-indigo-400" :title="$t('products.detail.model3d.title')" />
              </td>
              <td class="py-2 text-slate-500">{{ p.categoryName }}</td>
              <td class="py-2 text-slate-500">{{ unitLabel(p.unitOfMeasure) }}</td>
              <td class="py-2"><AppBadge :tone="p.isActive ? 'emerald' : 'slate'">{{ p.isActive ? $t("common.active") : $t("common.passive") }}</AppBadge></td>
              <td class="py-2 text-right">
                <div class="flex items-center justify-end gap-2">
                  <NuxtLink :to="`/admin/products/${p.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink>
                  <button
                    class="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-40"
                    :title="$t('common.delete')"
                    :disabled="deleting === p.id"
                    @click="deleteProduct(p)"
                  >
                    <TrashIcon class="h-4 w-4" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="products.length === 0">{{ $t("products.empty") }}</EmptyState>
        <ListPager v-model:page="listPage" :total="listTotal" :page-size="listPageSize" />
      </div>
    </AppCard>
  </div>
</template>
