<script setup lang="ts">
import type { AuditLogDto, PagedResult } from "~/types/api";

const api = useApi();
const toast = useToast();
const logs = ref<AuditLogDto[]>([]);
const totalCount = ref(0);
const page = ref(1);
const pageSize = 30;
const entityNameFilter = ref("");
const expandedId = ref<string | null>(null);
const loading = ref(true);

const actionTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Create: "emerald",
  Update: "amber",
  Delete: "rose",
};

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize)));

async function load() {
  loading.value = true;
  try {
    const result = await api.get<PagedResult<AuditLogDto>>("/api/audit-logs", {
      entityName: entityNameFilter.value || undefined,
      page: page.value,
      pageSize,
    });
    logs.value = result.items;
    totalCount.value = result.totalCount;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "Kayıtlar yüklenemedi."));
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
        <AppButton @click="applyFilter">Filtrele</AppButton>
      </div>
    </div>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <template v-else>
        <div class="overflow-x-auto">
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
                    <AppBadge :tone="actionTone[log.action] ?? 'slate'">{{ log.action }}</AppBadge>
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
            </tbody>
          </table>
          <EmptyState v-if="logs.length === 0">Kayıt bulunamadı</EmptyState>
        </div>

        <div v-if="totalPages > 1" class="mt-4 flex items-center justify-between text-xs text-slate-500">
          <span>Sayfa {{ page }} / {{ totalPages }} ({{ totalCount }} kayıt)</span>
          <div class="flex gap-2">
            <button :disabled="page <= 1" class="rounded border border-slate-300 px-2 py-1 disabled:opacity-40" @click="page = Math.max(1, page - 1)">Önceki</button>
            <button :disabled="page >= totalPages" class="rounded border border-slate-300 px-2 py-1 disabled:opacity-40" @click="page = Math.min(totalPages, page + 1)">Sonraki</button>
          </div>
        </div>
      </template>
    </AppCard>
  </div>
</template>
