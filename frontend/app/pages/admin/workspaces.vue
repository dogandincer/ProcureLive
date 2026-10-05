<script setup lang="ts">
import type { WorkspaceDto } from "~/types/api";
import { PlusIcon, PencilIcon, TrashIcon, CheckCircleIcon, XCircleIcon } from "@heroicons/vue/24/outline";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { workspaces, init } = useWorkspace();

const loading = ref(true);
const showForm = ref(false);
const editingId = ref<string | null>(null);
const saving = ref(false);
const deleting = ref<string | null>(null);

const form = reactive({ name: "", description: "", isActive: true });

async function load() {
  loading.value = true;
  try {
    workspaces.value = await api.get<WorkspaceDto[]>("/api/workspaces");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("workspaces.loadError")));
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  editingId.value = null;
  Object.assign(form, { name: "", description: "", isActive: true });
  showForm.value = true;
}

function openEdit(ws: WorkspaceDto) {
  editingId.value = ws.id;
  Object.assign(form, { name: ws.name, description: ws.description ?? "", isActive: ws.isActive });
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
      await api.put(`/api/workspaces/${editingId.value}`, {
        name: form.name,
        description: form.description || null,
        isActive: form.isActive,
      });
      toast.success(t("workspaces.updated"));
    } else {
      await api.post("/api/workspaces", { name: form.name, description: form.description || null });
      toast.success(t("workspaces.created"));
    }
    cancelForm();
    await load();
    init();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("workspaces.saveError")));
  } finally {
    saving.value = false;
  }
}

async function deleteWs(id: string) {
  if (!confirm(t("workspaces.deleteConfirm"))) return;
  deleting.value = id;
  try {
    await api.delete(`/api/workspaces/${id}`);
    toast.success(t("workspaces.deleted"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("workspaces.deleteError")));
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
        <h1 class="text-xl font-bold text-slate-800">{{ $t("workspaces.title") }}</h1>
        <p class="text-sm text-slate-500">{{ $t("workspaces.subtitle") }}</p>
      </div>
      <button
        class="flex items-center gap-1.5 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
        @click="openCreate"
      >
        <PlusIcon class="h-4 w-4" />
        {{ $t("workspaces.newWorkspace") }}
      </button>
    </div>

    <AppCard v-if="showForm" :title="editingId ? $t('workspaces.editWorkspace') : $t('workspaces.newWorkspace')">
      <form class="grid grid-cols-1 gap-3 sm:grid-cols-2" @submit.prevent="save">
        <div class="sm:col-span-2">
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("workspaces.form.name") }} *</label>
          <input
            v-model="form.name"
            required
            maxlength="150"
            :placeholder="$t('workspaces.form.namePlaceholder')"
            class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />
        </div>
        <div class="sm:col-span-2">
          <label class="mb-1 block text-xs font-medium text-slate-600">{{ $t("workspaces.form.description") }}</label>
          <textarea
            v-model="form.description"
            rows="2"
            maxlength="500"
            :placeholder="$t('workspaces.form.descriptionPlaceholder')"
            class="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />
        </div>
        <div v-if="editingId" class="flex items-center gap-2">
          <input id="wsIsActive" v-model="form.isActive" type="checkbox" class="rounded" />
          <label for="wsIsActive" class="text-sm text-slate-600">{{ $t("workspaces.form.isActive") }}</label>
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

    <AppCard noPadding>
      <LoadingSpinner v-if="loading" class="p-8" />
      <div v-else-if="workspaces.length === 0" class="p-8">
        <EmptyState>{{ $t("workspaces.empty") }}</EmptyState>
      </div>
      <table v-else class="w-full text-sm">
        <thead class="border-b border-slate-100 bg-slate-50">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("workspaces.table.name") }}</th>
            <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("workspaces.table.description") }}</th>
            <th class="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("workspaces.table.companies") }}</th>
            <th class="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("workspaces.table.products") }}</th>
            <th class="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("workspaces.table.status") }}</th>
            <th class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("common.actions") }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-50">
          <tr v-for="ws in workspaces" :key="ws.id" class="hover:bg-slate-50">
            <td class="px-4 py-3 font-medium text-slate-800">{{ ws.name }}</td>
            <td class="px-4 py-3 text-slate-500">{{ ws.description || "—" }}</td>
            <td class="px-4 py-3 text-center">
              <span class="inline-flex items-center rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-700">
                {{ ws.companyCount }}
              </span>
            </td>
            <td class="px-4 py-3 text-center">
              <span class="inline-flex items-center rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700">
                {{ ws.productCount }}
              </span>
            </td>
            <td class="px-4 py-3 text-center">
              <CheckCircleIcon v-if="ws.isActive" class="mx-auto h-4 w-4 text-emerald-500" />
              <XCircleIcon v-else class="mx-auto h-4 w-4 text-slate-300" />
            </td>
            <td class="px-4 py-3 text-right">
              <div class="flex items-center justify-end gap-1">
                <button
                  class="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-indigo-600"
                  :title="$t('common.edit')"
                  @click="openEdit(ws)"
                >
                  <PencilIcon class="h-4 w-4" />
                </button>
                <button
                  class="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600"
                  :title="$t('common.delete')"
                  :disabled="deleting === ws.id"
                  @click="deleteWs(ws.id)"
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
