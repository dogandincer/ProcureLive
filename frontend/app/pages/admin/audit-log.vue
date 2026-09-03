<script setup lang="ts">
import type { AuditLogDto, PagedResult } from "~/types/api";

const api = useApi();
const logs = ref<AuditLogDto[]>([]);
const totalCount = ref(0);
const page = ref(1);
const pageSize = 30;
const entityNameFilter = ref("");
const expandedId = ref<string | null>(null);
const loading = ref(true);
const errorMessage = ref("");

const actionBadge: Record<string, string> = {
  Create: "bg-emerald-100 text-emerald-700",
  Update: "bg-amber-100 text-amber-700",
  Delete: "bg-rose-100 text-rose-700",
};

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize)));

async function load() {
  loading.value = true;
  errorMessage.value = "";
  try {
    const result = await api.get<PagedResult<AuditLogDto>>("/api/audit-logs", {
      entityName: entityNameFilter.value || undefined,
      page: page.value,
      pageSize,
    });
    logs.value = result.items;
    totalCount.value = result.totalCount;
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Kayıtlar yüklenemedi.");
  } finally {
    loading.value = false;
  }
}

function toggleExpand(log: AuditLogDto) {
  expandedId.value = expandedId.value === log.id ? null : log.id;
}

function prettyJson(raw: string | null): string {
  if (!raw) return "-";
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

function applyFilter() {
  page.value = 1;
  load();
}

watch(page, load);
onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">Denetim Kaydı (Audit Log)</h1>
      <div class="flex gap-2">
        <input
          v-model="entityNameFilter"
          placeholder="Varlık adına göre filtrele (örn. Auction)"
          class="w-64 rounded-md border border-slate-300 px-3 py-1.5 text-sm"
          @keyup.enter="applyFilter"
        />
        <button class="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-semibold text-white" @click="applyFilter">Filtrele</button>
      </div>
    </div>

    <div v-if="errorMessage" class="flex items-center justify-between rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
      <span>{{ errorMessage }}</span>
      <button class="font-semibold" @click="errorMessage = ''">Kapat</button>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Zaman</th>
            <th class="pb-2">Varlık</th>
            <th class="pb-2">İşlem</th>
            <th class="pb-2">Kullanıcı</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="log in logs" :key="log.id">
            <tr class="cursor-pointer border-b border-slate-50 hover:bg-slate-50" @click="toggleExpand(log)">
              <td class="py-2 text-slate-500">{{ new Date(log.changedAt).toLocaleString("tr-TR") }}</td>
              <td class="py-2 font-medium text-slate-700">
                {{ log.entityName }}
                <span class="font-mono text-xs text-slate-400">{{ log.entityId.slice(0, 8) }}</span>
              </td>
              <td class="py-2">
                <span class="rounded px-2 py-0.5 text-xs font-medium" :class="actionBadge[log.action]">{{ log.action }}</span>
              </td>
              <td class="py-2 text-slate-500">{{ log.changedByUserName ?? "Sistem" }}</td>
              <td class="py-2 text-right text-xs text-indigo-600">{{ expandedId === log.id ? "Gizle" : "Detay" }}</td>
            </tr>
            <tr v-if="expandedId === log.id" class="border-b border-slate-50 bg-slate-50">
              <td colspan="5" class="p-3">
                <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <div>
                    <div class="mb-1 text-xs font-semibold uppercase text-slate-400">Eski Değerler</div>
                    <pre class="overflow-x-auto rounded bg-white p-2 text-xs text-slate-600">{{ prettyJson(log.oldValues) }}</pre>
                  </div>
                  <div>
                    <div class="mb-1 text-xs font-semibold uppercase text-slate-400">Yeni Değerler</div>
                    <pre class="overflow-x-auto rounded bg-white p-2 text-xs text-slate-600">{{ prettyJson(log.newValues) }}</pre>
                  </div>
                </div>
              </td>
            </tr>
          </template>
          <tr v-if="!loading && logs.length === 0"><td colspan="5" class="py-6 text-center text-slate-400">Kayıt bulunamadı</td></tr>
        </tbody>
      </table>

      <div v-if="totalPages > 1" class="mt-4 flex items-center justify-between text-xs text-slate-500">
        <span>Sayfa {{ page }} / {{ totalPages }} ({{ totalCount }} kayıt)</span>
        <div class="flex gap-2">
          <button :disabled="page <= 1" class="rounded border border-slate-300 px-2 py-1 disabled:opacity-40" @click="page = Math.max(1, page - 1)">Önceki</button>
          <button :disabled="page >= totalPages" class="rounded border border-slate-300 px-2 py-1 disabled:opacity-40" @click="page = Math.min(totalPages, page + 1)">Sonraki</button>
        </div>
      </div>
    </div>
  </div>
</template>
