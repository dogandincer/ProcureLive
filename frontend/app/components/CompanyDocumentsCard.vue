<script setup lang="ts">
import { DocumentTextIcon, TrashIcon } from "@heroicons/vue/24/outline";
import type { CompanyDocumentDto, UploadedFileDto } from "~/types/api";

/**
 * Cari belge kartı (vergi levhası, ISO sertifikaları, sigorta...). Son geçerlilik 30 gün içindeyse "yakında doluyor",
 * geçmişse "süresi doldu" görünür; sistem süre dolmadan bir kez tedarikçiye ve ilgili satınalmacılara bildirim gönderir.
 */
const props = defineProps<{ companyId: string; editable: boolean }>();
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();
const assetUrl = useAssetUrl();

const types = ["TaxCertificate", "TradeRegistry", "SignatureCircular", "Iso9001", "Iso14001", "Iso27001", "Iso45001", "Insurance", "Other"];
const stateTone: Record<string, "slate" | "amber" | "emerald" | "rose"> = { Valid: "emerald", ExpiringSoon: "amber", Expired: "rose", NoExpiry: "slate" };

const docs = ref<CompanyDocumentDto[]>([]);
const loading = ref(true);
const adding = ref(false);
const saving = ref(false);
const form = reactive({ documentType: "TaxCertificate", title: "", validUntil: "", file: null as UploadedFileDto | null });
const toDelete = ref<CompanyDocumentDto | null>(null);
const deleting = ref(false);

const warnings = computed(() => docs.value.filter((d) => d.expiryState === "Expired" || d.expiryState === "ExpiringSoon").length);

async function load() {
  try {
    docs.value = await api.get<CompanyDocumentDto[]>(`/api/companies/${props.companyId}/documents`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("documents.loadError")));
  } finally {
    loading.value = false;
  }
}

function onUploaded(f: UploadedFileDto) {
  form.file = f;
  if (!form.title.trim()) form.title = t(`documents.type.${form.documentType}`);
}

async function save() {
  if (!form.file) return;
  saving.value = true;
  try {
    await api.post(`/api/companies/${props.companyId}/documents`, {
      documentType: form.documentType,
      title: form.title.trim(),
      fileUrl: form.file.url,
      fileName: form.file.fileName,
      validUntil: form.validUntil ? `${form.validUntil}T00:00:00Z` : null,
    });
    Object.assign(form, { documentType: "TaxCertificate", title: "", validUntil: "", file: null });
    adding.value = false;
    toast.success(t("documents.added"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("documents.saveError")));
  } finally {
    saving.value = false;
  }
}

async function remove() {
  if (!toDelete.value) return;
  deleting.value = true;
  try {
    await api.delete(`/api/companies/${props.companyId}/documents/${toDelete.value.id}`);
    toDelete.value = null;
    toast.success(t("documents.deleted"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("documents.saveError")));
  } finally {
    deleting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <AppCard :title="$t('documents.title')" :description="$t('documents.hint')">
    <div id="documents" class="space-y-3" data-testid="documents-card">
      <p v-if="warnings > 0" class="rounded-md bg-amber-50 px-3 py-2 text-xs font-medium text-amber-800" data-testid="documents-warning">
        {{ $t("documents.warning", { n: warnings }) }}
      </p>
      <LoadingSpinner v-if="loading" />
      <EmptyState v-else-if="docs.length === 0" icon="📄">{{ $t("documents.empty") }}</EmptyState>
      <ul v-else class="divide-y divide-slate-100">
        <li v-for="d in docs" :key="d.id" class="flex flex-wrap items-center gap-3 py-2 text-sm" data-testid="document-row">
          <DocumentTextIcon class="h-5 w-5 shrink-0 text-slate-400" />
          <div class="min-w-0 flex-1">
            <a :href="assetUrl(d.fileUrl) ?? d.fileUrl" target="_blank" rel="noopener" class="block truncate font-medium text-indigo-600 hover:underline">{{ d.title }}</a>
            <span class="text-xs text-slate-500">{{ $t(`documents.type.${d.documentType}`) }} · {{ d.fileName }}</span>
          </div>
          <div class="text-right text-xs">
            <AppBadge :tone="stateTone[d.expiryState]" data-testid="document-state">{{ $t(`documents.state.${d.expiryState}`) }}</AppBadge>
            <span v-if="d.validUntil" class="mt-0.5 block text-slate-500">{{ $t("documents.validUntilShort", { date: formatDate(d.validUntil) }) }}</span>
          </div>
          <button v-if="editable" type="button" class="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-600" :aria-label="$t('documents.delete', { name: d.title })" @click="toDelete = d">
            <TrashIcon class="h-4 w-4" />
          </button>
        </li>
      </ul>

      <template v-if="editable">
        <AppButton v-if="!adding" variant="secondary" data-testid="document-add" @click="adding = true">{{ $t("documents.add") }}</AppButton>
        <form v-else class="grid grid-cols-1 gap-3 rounded-lg border border-slate-200 p-3 sm:grid-cols-2" data-testid="document-form" @submit.prevent="save">
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("documents.typeLabel") }}
            <select v-model="form.documentType" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" data-testid="document-type">
              <option v-for="ty in types" :key="ty" :value="ty">{{ $t(`documents.type.${ty}`) }}</option>
            </select>
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("documents.name") }}
            <input v-model="form.title" required maxlength="200" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" data-testid="document-title" />
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("documents.validUntil") }}
            <input v-model="form.validUntil" type="date" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" data-testid="document-valid-until" />
          </label>
          <div class="flex flex-col gap-1 text-xs font-medium text-slate-600">
            <span>{{ $t("documents.file") }}</span>
            <div class="flex flex-wrap items-center gap-2">
              <FileUploadButton purpose="company-document" accept=".pdf,.png,.jpg,.jpeg" @uploaded="onUploaded" />
              <span v-if="form.file" class="truncate text-xs font-normal text-emerald-700" data-testid="document-file-name">{{ form.file.fileName }}</span>
            </div>
            <span class="font-normal text-slate-400">{{ $t("documents.limits") }}</span>
          </div>
          <div class="flex gap-2 sm:col-span-2">
            <AppButton type="submit" :loading="saving" :disabled="!form.file || !form.title.trim()" data-testid="document-save">{{ $t("common.save") }}</AppButton>
            <AppButton variant="ghost" @click="adding = false">{{ $t("common.cancel") }}</AppButton>
          </div>
        </form>
      </template>

      <ConfirmDialog :open="!!toDelete" :title="$t('documents.deleteTitle')" tone="danger" :confirm-label="$t('common.delete')" :loading="deleting" @confirm="remove" @cancel="toDelete = null">
        {{ $t("documents.deleteBody", { name: toDelete?.title ?? "" }) }}
      </ConfirmDialog>
    </div>
  </AppCard>
</template>
