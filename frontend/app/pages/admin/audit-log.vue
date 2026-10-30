<script setup lang="ts">
import { ArrowDownTrayIcon, XMarkIcon } from "@heroicons/vue/24/outline";
import type { AuditLogDto, PagedResult } from "~/types/api";

/**
 * Denetim kaydı: kayıt türü, kullanıcı, işlem ve tarih aralığına göre süzülür; değişen alanlar satırda görünür,
 * eski/yeni değerler satıra tıklayınca açılır. Excel'e aktarma geçerli süzgeçle çalışır.
 */
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const logs = ref<AuditLogDto[]>([]);
const loading = ref(true);
const expandedId = ref<string | null>(null);
const facets = ref<{ entities: string[]; users: string[] }>({ entities: [], users: [] });
const filters = reactive({ entityName: "", user: "", action: "", from: "", to: "" });
const actions = ["Create", "Update", "Delete"];

const actionTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = { Create: "emerald", Update: "amber", Delete: "rose" };
const hasFilter = computed(() => Object.values(filters).some((v) => v));

const { page, total, pageSize, resetAndLoad } = usePagedList(() => load(), 30);

const query = () => ({
  entityName: filters.entityName || undefined,
  user: filters.user || undefined,
  action: filters.action || undefined,
  from: filters.from || undefined,
  to: filters.to || undefined,
});

let loadSeq = 0;
async function load() {
  const seq = ++loadSeq;
  loading.value = true;
  try {
    const result = await api.get<PagedResult<AuditLogDto>>("/api/audit-logs", { ...query(), page: page.value, pageSize });
    if (seq !== loadSeq) return;
    logs.value = result.items;
    total.value = result.totalCount;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auditLog.loadError")));
  } finally {
    if (seq === loadSeq) loading.value = false;
  }
}

function clearFilters() {
  Object.assign(filters, { entityName: "", user: "", action: "", from: "", to: "" });
}

function prettyJson(raw: string | null): string {
  if (!raw) return "-";
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

function changedColumns(raw: string | null): string {
  if (!raw) return "";
  try {
    const cols = JSON.parse(raw) as string[];
    return cols.length > 4 ? `${cols.slice(0, 4).join(", ")} +${cols.length - 4}` : cols.join(", ");
  } catch {
    return raw;
  }
}

// Geçerli süzgeçle en fazla 5.000 kayıt Excel'e aktarılır (sayfa sayfa çekilir).
const { exportRows } = useExcelExport();
const exporting = ref(false);
async function exportLogs() {
  exporting.value = true;
  try {
    const all: AuditLogDto[] = [];
    for (let p = 1; p <= 25; p++) {
      const r = await api.get<PagedResult<AuditLogDto>>("/api/audit-logs", { ...query(), page: p, pageSize: 200 });
      all.push(...r.items);
      if (all.length >= r.totalCount || r.items.length === 0) break;
    }
    exportRows("denetim-kayitlari", t("auditLog.title"), [
      { header: t("auditLog.export.time"), value: (l: AuditLogDto) => formatDateTime(l.changedAt), width: 18 },
      { header: t("auditLog.export.user"), value: (l) => l.changedByUserName ?? "", width: 26 },
      { header: t("auditLog.export.action"), value: (l) => t(`enums.auditAction.${l.action}`) },
      { header: t("auditLog.export.entity"), value: (l) => l.entityName, width: 22 },
      { header: t("auditLog.export.entityId"), value: (l) => l.entityId, width: 38 },
      { header: t("auditLog.export.columns"), value: (l) => l.affectedColumns ?? "", width: 40 },
    ], all);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auditLog.loadError")));
  } finally {
    exporting.value = false;
  }
}

watch(filters, () => resetAndLoad(), { deep: true });
onMounted(async () => {
  load();
  try {
    facets.value = await api.get("/api/audit-logs/facets");
  } catch {
    // süzgeç seçenekleri yüklenemezse alanlar boş kalır
  }
});
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("auditLog.title") }}</h1>
        <p class="mt-0.5 text-sm text-slate-500">{{ $t("auditLog.subtitle") }}</p>
      </div>
      <AppButton variant="secondary" :loading="exporting" data-testid="export-audit" @click="exportLogs">
        <ArrowDownTrayIcon class="h-4 w-4" />{{ $t("auctions.export.button") }}
      </AppButton>
    </div>

    <AppCard>
      <div class="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5" data-testid="audit-filters">
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("auditLog.f.entity") }}
          <select v-model="filters.entityName" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" data-testid="audit-entity">
            <option value="">{{ $t("auditLog.f.all") }}</option>
            <option v-for="e in facets.entities" :key="e" :value="e">{{ e }}</option>
          </select>
        </label>
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("auditLog.f.user") }}
          <input v-model.lazy="filters.user" list="audit-users" :placeholder="$t('auditLog.f.userPlaceholder')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" data-testid="audit-user" />
          <datalist id="audit-users"><option v-for="u in facets.users" :key="u" :value="u" /></datalist>
        </label>
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("auditLog.f.action") }}
          <select v-model="filters.action" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" data-testid="audit-action">
            <option value="">{{ $t("auditLog.f.all") }}</option>
            <option v-for="a in actions" :key="a" :value="a">{{ $t(`enums.auditAction.${a}`) }}</option>
          </select>
        </label>
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("auditLog.f.from") }}
          <input v-model="filters.from" type="date" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" />
        </label>
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("auditLog.f.to") }}
          <input v-model="filters.to" type="date" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm font-normal" />
        </label>
      </div>
      <button v-if="hasFilter" type="button" class="mb-3 inline-flex items-center gap-1 text-xs font-medium text-indigo-600 hover:underline" @click="clearFilters">
        <XMarkIcon class="h-3.5 w-3.5" />{{ $t("auditLog.f.clear") }}
      </button>

      <LoadingSpinner v-if="loading" />
      <template v-else>
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
                <th class="pb-2">{{ $t("auditLog.table.time") }}</th>
                <th class="pb-2">{{ $t("auditLog.table.entity") }}</th>
                <th class="pb-2">{{ $t("auditLog.table.action") }}</th>
                <th class="hidden pb-2 md:table-cell">{{ $t("auditLog.export.columns") }}</th>
                <th class="pb-2">{{ $t("auditLog.table.user") }}</th>
              </tr>
            </thead>
            <tbody>
              <template v-for="log in logs" :key="log.id">
                <tr
                  class="cursor-pointer border-b border-slate-50 hover:bg-slate-50"
                  tabindex="0"
                  :aria-expanded="expandedId === log.id"
                  @click="expandedId = expandedId === log.id ? null : log.id"
                  @keydown.enter="expandedId = expandedId === log.id ? null : log.id"
                >
                  <td class="whitespace-nowrap py-2 text-slate-500">{{ formatDateTime(log.changedAt) }}</td>
                  <td class="py-2 font-medium text-slate-700">
                    {{ log.entityName }}
                    <span class="font-mono text-xs text-slate-400">{{ log.entityId.slice(0, 8) }}</span>
                  </td>
                  <td class="py-2"><AppBadge :tone="actionTone[log.action] ?? 'slate'">{{ $t(`enums.auditAction.${log.action}`) }}</AppBadge></td>
                  <td class="hidden max-w-xs truncate py-2 text-xs text-slate-500 md:table-cell">{{ changedColumns(log.affectedColumns) }}</td>
                  <td class="py-2 text-slate-500">{{ log.changedByUserName ?? $t("auditLog.system") }}</td>
                </tr>
                <tr v-if="expandedId === log.id" class="border-b border-slate-50 bg-slate-50">
                  <td colspan="5" class="p-3">
                    <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
                      <div>
                        <div class="mb-1 text-xs font-semibold uppercase text-slate-400">{{ $t("auditLog.oldValues") }}</div>
                        <pre class="overflow-x-auto rounded bg-surface p-2 text-xs text-slate-600">{{ prettyJson(log.oldValues) }}</pre>
                      </div>
                      <div>
                        <div class="mb-1 text-xs font-semibold uppercase text-slate-400">{{ $t("auditLog.newValues") }}</div>
                        <pre class="overflow-x-auto rounded bg-surface p-2 text-xs text-slate-600">{{ prettyJson(log.newValues) }}</pre>
                      </div>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
          <EmptyState v-if="logs.length === 0">{{ $t("auditLog.empty") }}</EmptyState>
        </div>
        <ListPager v-model:page="page" :total="total" :page-size="pageSize" />
      </template>
    </AppCard>
  </div>
</template>
