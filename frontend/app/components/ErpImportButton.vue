<script setup lang="ts">
import type { CategoryDto, ErpEntityDto, ErpImportResultDto, ErpItemDto } from "~/types/api";
import { ArrowDownTrayIcon, MagnifyingGlassIcon, XMarkIcon } from "@heroicons/vue/24/outline";

/**
 * Premium: "ERP'den Cari Aktar" / "ERP'den Ürün Aktar". ERP'de arar, seçilenleri aktarır; eşleşen kayıt (ERP kodu, vergi no ya da
 * unvan / stok kodu) güncellenir, yoksa oluşturulur. Yalnızca Admin ve entegrasyon Simülasyon/Canlı moddayken görünür.
 */
const props = defineProps<{ kind: "entities" | "items" }>();
const emit = defineEmits<{ imported: [] }>();

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { isAdminRole } = useAuth();
const { ensure, erpActive } = useFeatures();

type Row = ErpEntityDto | ErpItemDto;
const open = ref(false);
const search = ref("");
const rows = ref<Row[]>([]);
const selected = ref<Set<string>>(new Set());
const loading = ref(false);
const importing = ref(false);
const error = ref("");
const result = ref<ErpImportResultDto | null>(null);
const workspaceId = ref("");
const categories = ref<CategoryDto[]>([]);
const categoryId = ref("");
let seq = 0;
let timer: ReturnType<typeof setTimeout> | null = null;

const flatten = (list: CategoryDto[]): CategoryDto[] => list.flatMap((c) => [c, ...flatten(c.children)]);
const linkedName = (r: Row) => ("linkedCompanyName" in r ? r.linkedCompanyName : r.linkedProductName);
const detail = (r: Row) => ("taxNo" in r ? [r.taxNo, r.phone, r.email] : [r.unitCode, r.brand]).filter(Boolean).join(" · ");

async function find() {
  const mySeq = ++seq;
  loading.value = true;
  error.value = "";
  try {
    const list = await api.get<Row[]>(`/api/erp/${props.kind}`, { search: search.value.trim() || undefined });
    if (mySeq === seq) rows.value = list;
  } catch (e: any) {
    if (mySeq === seq) {
      rows.value = [];
      error.value = apiErrorMessage(e, t("erp.import.searchError"));
    }
  } finally {
    if (mySeq === seq) loading.value = false;
  }
}
watch(search, () => {
  if (timer) clearTimeout(timer);
  timer = setTimeout(find, 350);
});

function toggle(code: string) {
  const next = new Set(selected.value);
  if (next.has(code)) next.delete(code);
  else next.add(code);
  selected.value = next;
}

async function show() {
  open.value = true;
  result.value = null;
  selected.value = new Set();
  if (props.kind === "items" && categories.value.length === 0) {
    categories.value = flatten(await api.get<CategoryDto[]>("/api/categories"));
    categoryId.value ||= categories.value[0]?.id ?? "";
  }
  await find();
}

async function run() {
  importing.value = true;
  error.value = "";
  try {
    result.value = await api.post<ErpImportResultDto>(`/api/erp/${props.kind}/import`, {
      codes: [...selected.value],
      workspaceId: workspaceId.value || null,
      categoryId: props.kind === "items" ? categoryId.value || null : null,
    });
    toast.success(t("erp.import.done", { created: result.value.created, updated: result.value.updated }));
    selected.value = new Set();
    emit("imported");
    await find();
  } catch (e: any) {
    error.value = apiErrorMessage(e, t("erp.import.error"));
  } finally {
    importing.value = false;
  }
}

function onKey(e: KeyboardEvent) {
  if (e.key === "Escape" && open.value && !importing.value) open.value = false;
}
onMounted(() => {
  ensure();
  document.addEventListener("keydown", onKey);
});
onBeforeUnmount(() => document.removeEventListener("keydown", onKey));
</script>

<template>
  <template v-if="erpActive && isAdminRole()">
    <AppButton variant="secondary" :data-testid="`erp-import-${kind}`" @click="show">
      <ArrowDownTrayIcon class="h-4 w-4" />{{ $t(`erp.import.button.${kind}`) }}
    </AppButton>

    <Teleport to="body">
      <div v-if="open" class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4" @click.self="!importing && (open = false)">
        <div role="dialog" aria-modal="true" :aria-label="$t(`erp.import.title.${kind}`)" class="mt-10 w-full max-w-3xl rounded-xl bg-surface shadow-xl" data-testid="erp-import-dialog">
          <div class="flex items-start justify-between border-b border-slate-100 px-5 py-4">
            <div>
              <h2 class="text-base font-semibold text-slate-800">{{ $t(`erp.import.title.${kind}`) }}</h2>
              <p class="mt-0.5 text-xs text-slate-500">{{ $t(`erp.import.hint.${kind}`) }}</p>
            </div>
            <button type="button" class="rounded p-1 text-slate-400 hover:bg-slate-100" :aria-label="$t('common.close')" @click="open = false">
              <XMarkIcon class="h-5 w-5" />
            </button>
          </div>

          <div class="space-y-4 p-5">
            <label class="relative block">
              <MagnifyingGlassIcon class="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <input v-model="search" type="search" :placeholder="$t(`erp.import.search.${kind}`)" class="w-full rounded-md border border-slate-300 py-2 pl-9 pr-3 text-sm" data-testid="erp-import-search" />
            </label>

            <p v-if="error" role="alert" class="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700" data-testid="erp-import-error">{{ error }}</p>

            <div class="max-h-80 overflow-y-auto rounded-md border border-slate-200">
              <LoadingSpinner v-if="loading && rows.length === 0" />
              <ul v-else class="divide-y divide-slate-100">
                <li v-for="r in rows" :key="r.code">
                  <label class="flex cursor-pointer items-start gap-3 px-3 py-2 hover:bg-slate-50">
                    <input type="checkbox" class="mt-1 rounded border-slate-300" :checked="selected.has(r.code)" :data-code="r.code" @change="toggle(r.code)" />
                    <span class="min-w-0 flex-1 text-sm">
                      <span class="font-mono text-xs text-slate-500">{{ r.code }}</span>
                      <span class="block font-medium text-slate-800">{{ r.name }}</span>
                      <span v-if="detail(r)" class="block text-xs text-slate-500">{{ detail(r) }}</span>
                    </span>
                    <AppBadge v-if="linkedName(r)" tone="emerald" :title="linkedName(r) ?? ''">{{ $t("erp.import.linked") }}</AppBadge>
                  </label>
                </li>
              </ul>
              <EmptyState v-if="!loading && rows.length === 0 && !error">{{ $t("erp.import.empty") }}</EmptyState>
            </div>

            <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <FormField v-if="kind === 'items'" :label="$t('erp.import.category')" required>
                <select v-model="categoryId" class="rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="erp-import-category">
                  <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
                </select>
              </FormField>
              <WorkspaceField v-model="workspaceId" :hint="$t('erp.import.workspaceHint')" />
            </div>

            <div v-if="result" role="status" class="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800" data-testid="erp-import-result">
              {{ $t("erp.import.done", { created: result.created, updated: result.updated }) }}
              <ul v-if="result.errors.length" class="mt-1 list-disc pl-5 text-rose-700">
                <li v-for="e in result.errors" :key="e">{{ e }}</li>
              </ul>
            </div>
          </div>

          <div class="flex flex-wrap items-center justify-end gap-2 border-t border-slate-100 px-5 py-3">
            <span class="mr-auto text-xs text-slate-500">{{ $t("common.selectedCount", { n: selected.size }) }}</span>
            <AppButton variant="ghost" :disabled="importing" @click="open = false">{{ $t("common.close") }}</AppButton>
            <AppButton :loading="importing" :disabled="selected.size === 0 || (kind === 'items' && !categoryId)" data-testid="erp-import-run" @click="run">
              {{ $t("erp.import.run", { n: selected.size }) }}
            </AppButton>
          </div>
        </div>
      </div>
    </Teleport>
  </template>
</template>
