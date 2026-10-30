<script setup lang="ts">
import type { CategoryDto, ProductDto, WorkspaceDto } from "~/types/api";
import {
  ArrowLeftIcon,
  CubeTransparentIcon,
  SparklesIcon,
  TrashIcon,
  PhotoIcon,
  PlusIcon,
  PencilIcon,
  StarIcon,
  BuildingStorefrontIcon,
} from "@heroicons/vue/24/outline";

const route = useRoute();
const router = useRouter();
const productId = route.params.id as string;
const api = useApi();
const { ask } = useConfirm();
const show3d = ref(false);
const assetUrl = useAssetUrl();
// Yüklenen görsel doğrudan ürüne eklenir (URL ile ekleme de sürer).
async function onImageUploaded(f: { url: string }) {
  imageForm.url = f.url;
  await addImage();
}
const accessState = ref<"forbidden" | "notFound" | null>(null);
const toast = useToast();
const { t } = useI18n();

const product = ref<ProductDto | null>(null);
const loading = ref(true);
const categories = ref<CategoryDto[]>([]);
const unitOfMeasures = ["Piece", "Kilogram", "Gram", "Liter", "Meter", "SquareMeter", "CubicMeter", "Box", "Pallet", "Hour", "Service"];

// Workspace assignment
const workspaces = ref<WorkspaceDto[]>([]);
const productWorkspaceId = ref<string>("");
const savingWorkspace = ref(false);

// Edit + delete
const showEditForm = ref(false);
const savingEdit = ref(false);
const deletingProduct = ref(false);
const editForm = reactive({ name: "", description: "", categoryId: "", unitOfMeasure: "Piece", brand: "", isActive: true });

function flattenCategories(list: CategoryDto[]): CategoryDto[] {
  return list.flatMap((c) => [c, ...flattenCategories(c.children)]);
}

function openEdit() {
  if (!product.value) return;
  const p = product.value;
  Object.assign(editForm, { name: p.name, description: p.description ?? "", categoryId: p.categoryId, unitOfMeasure: p.unitOfMeasure, brand: p.brand ?? "", isActive: p.isActive });
  showEditForm.value = true;
}

async function saveEdit() {
  savingEdit.value = true;
  try {
    await api.put(`/api/products/${productId}`, {
      name: editForm.name,
      description: editForm.description || null,
      categoryId: editForm.categoryId,
      unitOfMeasure: editForm.unitOfMeasure,
      brand: editForm.brand || null,
      defaultCurrencyId: null,
      isActive: editForm.isActive,
    });
    toast.success(t("products.detail.updated"));
    showEditForm.value = false;
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.detail.updateError")));
  } finally {
    savingEdit.value = false;
  }
}

async function deleteProduct() {
  if (!product.value || !(await ask({ title: t("products.deleteConfirm", { name: product.value.name }), tone: "danger", confirmLabel: t("common.delete") }))) return;
  deletingProduct.value = true;
  try {
    await api.delete(`/api/products/${productId}`);
    toast.success(t("products.deleted"));
    await router.push("/admin/products");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.deleteError")));
    deletingProduct.value = false;
  }
}

// Image management
const showImageForm = ref(false);
const imageForm = reactive({ url: "", isPrimary: false });
const savingImage = ref(false);

// 3D model management
const showModelForm = ref(false);
const modelUrlInput = ref("");
const savingModel = ref(false);
const generating = ref(false);
const showReplace = ref(false);

async function load() {
  loading.value = true;
  try {
    const [productData, ws, cats] = await Promise.all([
      api.get<ProductDto>(`/api/products/${productId}`),
      api.get<WorkspaceDto[]>("/api/workspaces", { activeOnly: true }),
      api.get<CategoryDto[]>("/api/categories"),
    ]);
    product.value = productData;
    workspaces.value = ws;
    categories.value = flattenCategories(cats);
    productWorkspaceId.value = productData.workspaceId ?? "";
  } catch (error: any) {
    // Yetki yoksa / kayıt yoksa boş sayfa ve kaybolan bildirim yerine açıklayıcı durum gösterilir.
    const code = error?.response?.status ?? error?.statusCode;
    if (code === 403 || code === 404) accessState.value = code === 403 ? "forbidden" : "notFound";
    else toast.error(apiErrorMessage(error, t("products.detail.loadError")));
  } finally {
    loading.value = false;
  }
}

async function saveWorkspace() {
  savingWorkspace.value = true;
  try {
    await api.put(`/api/products/${productId}/workspace`, {
      workspaceId: productWorkspaceId.value || null,
    });
    toast.success(t("products.detail.workspaceSaved"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.detail.workspaceError")));
  } finally {
    savingWorkspace.value = false;
  }
}

async function addImage() {
  savingImage.value = true;
  try {
    await api.post(`/api/products/${productId}/images`, {
      url: imageForm.url,
      isPrimary: imageForm.isPrimary,
      sortOrder: product.value?.images.length ?? 0,
    });
    imageForm.url = "";
    imageForm.isPrimary = false;
    showImageForm.value = false;
    toast.success(t("products.detail.imageAdded"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.detail.imageError")));
  } finally {
    savingImage.value = false;
  }
}

async function removeImage(imageId: string) {
  if (!(await ask({ title: t("products.detail.imageRemoveConfirm"), tone: "danger", confirmLabel: t("common.delete") }))) return;
  try {
    await api.delete(`/api/products/${productId}/images/${imageId}`);
    toast.success(t("products.detail.imageRemoved"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.detail.imageError")));
  }
}

async function saveModelUrl() {
  savingModel.value = true;
  try {
    await api.put(`/api/products/${productId}/model-3d`, { url: modelUrlInput.value });
    modelUrlInput.value = "";
    showModelForm.value = false;
    showReplace.value = false;
    toast.success(t("products.detail.model3d.saved"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.detail.model3d.saveError")));
  } finally {
    savingModel.value = false;
  }
}

async function generateModel() {
  generating.value = true;
  try {
    await api.post(`/api/products/${productId}/model-3d/generate`);
    showReplace.value = false;
    toast.success(t("products.detail.model3d.generated"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.detail.model3d.generateError")));
  } finally {
    generating.value = false;
  }
}

async function removeModel() {
  if (!(await ask({ title: t("products.detail.model3d.removeConfirm"), tone: "danger", confirmLabel: t("common.delete") }))) return;
  try {
    await api.delete(`/api/products/${productId}/model-3d`);
    toast.success(t("products.detail.model3d.removed"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("products.detail.model3d.saveError")));
  }
}

const hasImages = computed(() => (product.value?.images.length ?? 0) > 0);

onMounted(load);
</script>

<template>
  <AccessState v-if="accessState" :kind="accessState" back-to="/admin/products" :back-label="$t('nav.products')" />
  <LoadingSpinner v-else-if="loading" />

  <div v-else-if="product" class="space-y-6">
    <!-- Header -->
    <div>
      <NuxtLink to="/admin/products" class="inline-flex items-center gap-1 text-xs font-medium text-slate-400 hover:text-slate-600">
        <ArrowLeftIcon class="h-3.5 w-3.5" />
        {{ $t("products.detail.backToList") }}
      </NuxtLink>
      <div class="mt-1 flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 class="text-xl font-bold text-slate-800">{{ product.name }}</h1>
          <p class="text-sm text-slate-500">
            <span class="font-mono">{{ product.sku }}</span> · {{ product.categoryName }}
          </p>
        </div>
        <div class="flex items-center gap-2">
          <AppBadge :tone="product.isActive ? 'emerald' : 'slate'">
            {{ product.isActive ? $t("common.active") : $t("common.passive") }}
          </AppBadge>
          <button class="flex items-center gap-1 rounded-md border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50" @click="openEdit">
            <PencilIcon class="h-3.5 w-3.5" />{{ $t("common.edit") }}
          </button>
          <button class="flex items-center gap-1 rounded-md border border-red-200 px-2.5 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50" :disabled="deletingProduct" @click="deleteProduct">
            <TrashIcon class="h-3.5 w-3.5" />{{ $t("common.delete") }}
          </button>
        </div>
      </div>

      <!-- Edit form -->
      <AppCard v-if="showEditForm" class="mt-3" :title="$t('products.detail.editTitle')">
        <form class="grid grid-cols-1 gap-3 sm:grid-cols-2" @submit.prevent="saveEdit">
          <div><label class="mb-1 block text-xs text-slate-500">{{ $t("products.form.name") }}</label><input v-model="editForm.name" required class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
          <div><label class="mb-1 block text-xs text-slate-500">{{ $t("products.form.brand") }}</label><input v-model="editForm.brand" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
          <div><label class="mb-1 block text-xs text-slate-500">{{ $t("products.table.category") }}</label>
            <select v-model="editForm.categoryId" required class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm">
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
          <div><label class="mb-1 block text-xs text-slate-500">{{ $t("products.table.unit") }}</label>
            <select v-model="editForm.unitOfMeasure" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm">
              <option v-for="u in unitOfMeasures" :key="u" :value="u">{{ $t(`enums.unitOfMeasure.${u}`) }}</option>
            </select>
          </div>
          <div class="sm:col-span-2"><label class="mb-1 block text-xs text-slate-500">{{ $t("products.detail.descriptionLabel") }}</label><textarea v-model="editForm.description" rows="2" class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm" /></div>
          <label class="flex items-center gap-2 text-sm text-slate-600"><input v-model="editForm.isActive" type="checkbox" class="rounded" />{{ $t("common.active") }}</label>
          <div class="flex items-center justify-end gap-2">
            <button type="button" class="rounded-md px-4 py-1.5 text-sm text-slate-600 hover:bg-slate-100" @click="showEditForm = false">{{ $t("common.cancel") }}</button>
            <button type="submit" :disabled="savingEdit" class="rounded-md bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">{{ savingEdit ? $t("common.saving") : $t("common.save") }}</button>
          </div>
        </form>
      </AppCard>

      <div class="mt-2 flex items-center gap-2 text-sm">
        <BuildingStorefrontIcon class="h-4 w-4 shrink-0 text-slate-400" />
        <span class="text-slate-500">{{ $t("products.detail.workspace") }}:</span>
        <select
          v-model="productWorkspaceId"
          :disabled="savingWorkspace"
          class="rounded-md border border-slate-300 px-2 py-1 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:opacity-50"
          @change="saveWorkspace"
        >
          <option value="">{{ $t("workspaces.unassigned") }}</option>
          <option v-for="w in workspaces" :key="w.id" :value="w.id">{{ w.name }}</option>
        </select>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <!-- Images -->
      <AppCard>
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <PhotoIcon class="h-5 w-5 text-slate-400" />
            <h3 class="text-sm font-semibold text-slate-700">{{ $t("products.detail.images") }}</h3>
          </div>
          <button
            class="flex items-center gap-1 rounded-md bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600 hover:bg-slate-200"
            @click="showImageForm = !showImageForm"
          >
            <PlusIcon class="h-3.5 w-3.5" />
            {{ $t("products.detail.addImage") }}
          </button>
        </div>

        <form v-if="showImageForm" class="mt-3 space-y-2" @submit.prevent="addImage">
          <div class="flex flex-wrap items-center gap-2">
            <FileUploadButton purpose="product-image" accept="image/png,image/jpeg,image/webp" :label="$t('products.detail.uploadImage')" @uploaded="onImageUploaded" />
            <span class="text-xs text-slate-400">{{ $t("products.detail.orUrl") }}</span>
          </div>
          <input
            v-model="imageForm.url"
            type="url"
            required
            :placeholder="$t('products.detail.imageUrlPlaceholder')"
            class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />
          <div class="flex items-center justify-between">
            <label class="flex items-center gap-1.5 text-xs text-slate-600">
              <input v-model="imageForm.isPrimary" type="checkbox" class="rounded" />
              {{ $t("products.detail.setPrimary") }}
            </label>
            <button type="submit" :disabled="savingImage" class="rounded-md bg-indigo-600 px-3 py-1 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
              {{ savingImage ? $t("common.saving") : $t("common.add") }}
            </button>
          </div>
        </form>

        <div v-if="hasImages" class="mt-3 grid grid-cols-3 gap-2">
          <div
            v-for="img in product.images"
            :key="img.id"
            class="group relative aspect-square overflow-hidden rounded-lg border border-slate-200 bg-slate-50"
          >
            <img :src="assetUrl(img.url) ?? img.url" :alt="product.name" class="h-full w-full object-cover" loading="lazy" />
            <StarIcon v-if="img.isPrimary" class="absolute left-1 top-1 h-4 w-4 fill-amber-400 text-amber-400" />
            <button
              class="absolute right-1 top-1 rounded-full bg-surface/80 p-1 text-slate-400 opacity-0 transition-opacity hover:text-red-600 group-hover:opacity-100"
              @click="removeImage(img.id)"
            >
              <TrashIcon class="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
        <EmptyState v-else class="mt-3">{{ $t("products.detail.noImages") }}</EmptyState>
      </AppCard>

      <!-- 3D model -->
      <AppCard>
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <CubeTransparentIcon class="h-5 w-5 text-indigo-500" />
            <h3 class="text-sm font-semibold text-slate-700">{{ $t("products.detail.model3d.title") }}</h3>
          </div>
          <span
            v-if="product.model3dSource"
            class="rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide"
            :class="product.model3dSource === 'AiGenerated' ? 'bg-indigo-100 text-indigo-700' : 'bg-slate-100 text-slate-600'"
          >
            {{ product.model3dSource === 'AiGenerated' ? $t("products.detail.model3d.aiGenerated") : $t("products.detail.model3d.uploaded") }}
          </span>
        </div>

        <!-- Viewer -->
        <div v-if="product.model3dUrl" class="mt-3">
          <div class="overflow-hidden rounded-xl border border-slate-200 bg-gradient-to-b from-slate-50 to-slate-100">
            <!-- Model dosyası ancak istenince indirilir: sayfa 3D yüzünden yavaş açılmasın -->
            <button
              v-if="!show3d"
              type="button"
              class="flex h-[320px] w-full flex-col items-center justify-center gap-2 text-sm font-medium text-indigo-600 hover:bg-indigo-50/50"
              data-testid="show-3d"
              @click="show3d = true"
            >
              <CubeTransparentIcon class="h-8 w-8" />
              {{ $t("products.detail.model3d.show") }}
            </button>
            <model-viewer
              v-else
              :src="product.model3dUrl"
              camera-controls
              auto-rotate
              shadow-intensity="1"
              exposure="1"
              :alt="product.name"
              style="width: 100%; height: 320px; background-color: transparent;"
            />
          </div>
          <div class="mt-2 flex items-center justify-between">
            <p class="text-[11px] text-slate-400">{{ $t("products.detail.model3d.viewerHint") }}</p>
            <div class="flex items-center gap-1">
              <button
                class="flex items-center gap-1 rounded-md px-2 py-1 text-xs font-medium text-indigo-600 hover:bg-indigo-50"
                @click="showReplace = !showReplace"
              >
                <CubeTransparentIcon class="h-3.5 w-3.5" />
                {{ showReplace ? $t("common.cancel") : $t("products.detail.model3d.change") }}
              </button>
              <button
                class="flex items-center gap-1 rounded-md px-2 py-1 text-xs font-medium text-red-500 hover:bg-red-50"
                @click="removeModel"
              >
                <TrashIcon class="h-3.5 w-3.5" />
                {{ $t("products.detail.model3d.remove") }}
              </button>
            </div>
          </div>
        </div>

        <!-- No model (or replacing): actions -->
        <div v-if="!product.model3dUrl || showReplace" class="mt-3 space-y-3">
          <EmptyState v-if="!product.model3dUrl">{{ $t("products.detail.model3d.empty") }}</EmptyState>

          <!-- AI generation -->
          <div class="rounded-lg border border-indigo-100 bg-indigo-50/50 p-3">
            <div class="flex items-center gap-2">
              <SparklesIcon class="h-4 w-4 text-indigo-500" />
              <span class="text-sm font-medium text-indigo-800">{{ $t("products.detail.model3d.aiTitle") }}</span>
            </div>
            <p class="mt-1 text-xs text-indigo-600">{{ $t("products.detail.model3d.aiHint") }}</p>
            <button
              :disabled="!hasImages || generating"
              class="mt-2 flex items-center gap-1.5 rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
              @click="generateModel"
            >
              <SparklesIcon class="h-3.5 w-3.5" />
              {{ generating ? $t("products.detail.model3d.generating") : $t("products.detail.model3d.generate") }}
            </button>
            <p v-if="!hasImages" class="mt-1.5 text-[11px] text-amber-600">{{ $t("products.detail.model3d.needImages") }}</p>
          </div>

          <!-- Direct upload -->
          <div class="rounded-lg border border-slate-200 p-3">
            <button
              v-if="!showModelForm"
              class="flex items-center gap-1.5 text-sm font-medium text-slate-600 hover:text-slate-800"
              @click="showModelForm = true"
            >
              <CubeTransparentIcon class="h-4 w-4" />
              {{ $t("products.detail.model3d.uploadTitle") }}
            </button>
            <form v-else class="space-y-2" @submit.prevent="saveModelUrl">
              <label class="block text-xs font-medium text-slate-600">{{ $t("products.detail.model3d.uploadLabel") }}</label>
              <input
                v-model="modelUrlInput"
                type="url"
                required
                placeholder="https://.../model.glb"
                class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
              />
              <div class="flex items-center justify-end gap-2">
                <button type="button" class="rounded-md px-3 py-1 text-xs text-slate-500 hover:bg-slate-100" @click="showModelForm = false">
                  {{ $t("common.cancel") }}
                </button>
                <button type="submit" :disabled="savingModel" class="rounded-md bg-indigo-600 px-3 py-1 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                  {{ savingModel ? $t("common.saving") : $t("common.save") }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </AppCard>
    </div>
  </div>
</template>
