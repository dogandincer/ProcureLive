<script setup lang="ts">
import type { CategoryDto, PagedResult, ProductDto } from "~/types/api";

const api = useApi();
const products = ref<ProductDto[]>([]);
const categories = ref<CategoryDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const errorMessage = ref("");

const form = reactive({ sku: "", name: "", categoryId: "", unitOfMeasure: "Piece", brand: "" });
const unitOfMeasures = ["Piece", "Kilogram", "Gram", "Liter", "Meter", "SquareMeter", "CubicMeter", "Box", "Pallet", "Hour", "Service"];

function flattenCategories(list: CategoryDto[]): CategoryDto[] {
  return list.flatMap((c) => [c, ...flattenCategories(c.children)]);
}

async function load() {
  const [productPage, categoryTree] = await Promise.all([
    api.get<PagedResult<ProductDto>>("/api/products", { page: 1, pageSize: 100 }),
    api.get<CategoryDto[]>("/api/categories"),
  ]);
  products.value = productPage.items;
  categories.value = flattenCategories(categoryTree);
  if (!form.categoryId && categories.value.length > 0) {
    form.categoryId = categories.value[0].id;
  }
}

async function create() {
  errorMessage.value = "";
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
    });
    Object.assign(form, { sku: "", name: "", brand: "" });
    showForm.value = false;
    await load();
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Ürün oluşturulamadı.");
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">Stok / Ürünler</h1>
      <button class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white" @click="showForm = !showForm">
        {{ showForm ? "Vazgeç" : "+ Yeni Ürün" }}
      </button>
    </div>

    <form v-if="showForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2" @submit.prevent="create">
      <input v-model="form.sku" placeholder="SKU" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="form.name" placeholder="Ürün Adı" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <select v-model="form.categoryId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <select v-model="form.unitOfMeasure" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ u }}</option>
      </select>
      <input v-model="form.brand" placeholder="Marka (opsiyonel)" class="rounded-md border border-slate-300 px-3 py-2 text-sm sm:col-span-2" />
      <p v-if="errorMessage" class="text-sm text-rose-600 sm:col-span-2">{{ errorMessage }}</p>
      <button type="submit" :disabled="submitting" class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white sm:col-span-2 disabled:opacity-50">
        {{ submitting ? "Kaydediliyor..." : "Kaydet" }}
      </button>
    </form>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">SKU</th>
            <th class="pb-2">Ad</th>
            <th class="pb-2">Kategori</th>
            <th class="pb-2">Birim</th>
            <th class="pb-2">Durum</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in products" :key="p.id" class="border-b border-slate-50">
            <td class="py-2 font-mono text-xs text-slate-500">{{ p.sku }}</td>
            <td class="py-2 font-medium text-slate-700">{{ p.name }}</td>
            <td class="py-2 text-slate-500">{{ p.categoryName }}</td>
            <td class="py-2 text-slate-500">{{ p.unitOfMeasure }}</td>
            <td class="py-2 text-slate-500">{{ p.isActive ? "Aktif" : "Pasif" }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
