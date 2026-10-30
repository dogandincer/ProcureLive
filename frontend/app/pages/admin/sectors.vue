<script setup lang="ts">
import type { SectorDto } from "~/types/api";
import { PlusIcon, PencilIcon, TrashIcon, CheckCircleIcon, XCircleIcon } from "@heroicons/vue/24/outline";

const api = useApi();
const { ask } = useConfirm();
const toast = useToast();
const { t } = useI18n();

const sectors = ref<SectorDto[]>([]);
const loading = ref(true);
const showForm = ref(false);
const editingId = ref<string | null>(null);
const saving = ref(false);
const deleting = ref<string | null>(null);

const form = reactive({ name: "", description: "", isActive: true });

async function load() {
  loading.value = true;
  try {
    sectors.value = await api.get<SectorDto[]>("/api/sectors");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("sectors.loadError")));
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  editingId.value = null;
  Object.assign(form, { name: "", description: "", isActive: true });
  showForm.value = true;
}

function openEdit(sector: SectorDto) {
  editingId.value = sector.id;
  Object.assign(form, { name: sector.name, description: sector.description ?? "", isActive: sector.isActive });
  showForm.value = true;
}

function cancelForm() {
  showForm.value = false;
  editingId.value = null;
}

async function save() {
  saving.value = true;
  try {
    if (editingId.value) {
      await api.put(`/api/sectors/${editingId.value}`, {
        name: form.name,
        description: form.description || null,
        isActive: form.isActive,
      });
      toast.success(t("sectors.updated"));
    } else {
      await api.post("/api/sectors", {
        name: form.name,
        description: form.description || null,
      });
      toast.success(t("sectors.created"));
    }
    cancelForm();
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("sectors.saveError")));
  } finally {
    saving.value = false;
  }
}

async function deleteSector(id: string) {
  if (!(await ask({ title: t("sectors.deleteConfirm"), tone: "danger", confirmLabel: t("common.delete") }))) return;
  deleting.value = id;
  try {
    await api.delete(`/api/sectors/${id}`);
    toast.success(t("sectors.deleted"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("sectors.deleteError")));
  } finally {
    deleting.value = null;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("sectors.title") }}</h1>
        <p class="text-sm text-slate-500">{{ $t("sectors.subtitle") }}</p>
      </div>
      <button
        class="flex items-center gap-1.5 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
        @click="openCreate"
      >
        <PlusIcon class="h-4 w-4" />
        {{ $t("sectors.newSector") }}
      </button>
    </div>

    <!-- Form panel -->
    <AppCard v-if="showForm" :title="editingId ? $t('sectors.editSector') : $t('sectors.newSector')">
      <form class="grid grid-cols-1 gap-3 sm:grid-cols-2" @submit.prevent="save">
        <div class="sm:col-span-2">
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("sectors.form.name") }} *</label>
          <input
            v-model="form.name"
            required
            maxlength="150"
            :placeholder="$t('sectors.form.namePlaceholder')"
            class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />
        </div>
        <div class="sm:col-span-2">
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("sectors.form.description") }}</label>
          <textarea
            v-model="form.description"
            rows="2"
            maxlength="500"
            :placeholder="$t('sectors.form.descriptionPlaceholder')"
            class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />
        </div>
        <div v-if="editingId" class="flex items-center gap-2">
          <input id="isActive" v-model="form.isActive" type="checkbox" class="rounded" />
          <label for="isActive" class="text-sm text-slate-600">{{ $t("sectors.form.isActive") }}</label>
        </div>
        <div class="flex items-center justify-end gap-2 sm:col-span-2">
          <button type="button" class="rounded-md px-4 py-1.5 text-sm text-slate-600 hover:bg-slate-100" @click="cancelForm">
            {{ $t("common.cancel") }}
          </button>
          <button
            type="submit"
            :disabled="saving"
            class="rounded-md bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            {{ saving ? $t("common.saving") : $t("common.save") }}
          </button>
        </div>
      </form>
    </AppCard>

    <!-- Table -->
    <AppCard noPadding>
      <LoadingSpinner v-if="loading" class="p-8" />
      <div v-else-if="sectors.length === 0" class="p-8">
        <EmptyState>{{ $t("sectors.empty") }}</EmptyState>
      </div>
      <table v-else class="w-full text-sm">
        <thead class="border-b border-slate-100 bg-slate-50">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("sectors.table.name") }}</th>
            <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("sectors.table.description") }}</th>
            <th class="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("sectors.table.companies") }}</th>
            <th class="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("sectors.table.status") }}</th>
            <th class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("common.actions") }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-50">
          <tr v-for="sector in sectors" :key="sector.id" class="hover:bg-slate-50">
            <td class="px-4 py-3 font-medium text-slate-800">{{ sector.name }}</td>
            <td class="px-4 py-3 text-slate-500">{{ sector.description || "—" }}</td>
            <td class="px-4 py-3 text-center">
              <span class="inline-flex items-center rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-700">
                {{ sector.companyCount }}
              </span>
            </td>
            <td class="px-4 py-3 text-center">
              <CheckCircleIcon v-if="sector.isActive" class="mx-auto h-4 w-4 text-emerald-500" />
              <XCircleIcon v-else class="mx-auto h-4 w-4 text-slate-300" />
            </td>
            <td class="px-4 py-3 text-right">
              <div class="flex items-center justify-end gap-1">
                <button
                  class="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-indigo-600"
                  :title="$t('common.edit')"
                  @click="openEdit(sector)"
                >
                  <PencilIcon class="h-4 w-4" />
                </button>
                <button
                  class="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600"
                  :title="$t('common.delete')"
                  :disabled="deleting === sector.id"
                  @click="deleteSector(sector.id)"
                >
                  <TrashIcon class="h-4 w-4" />
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </AppCard>
  </div>
</template>
