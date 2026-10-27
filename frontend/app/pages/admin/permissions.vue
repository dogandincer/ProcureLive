<script setup lang="ts">
import type { FirmDto, PagedResult, UserDto, WorkspaceDto } from "~/types/api";
import { CheckIcon, MinusIcon, MagnifyingGlassIcon, BuildingOffice2Icon } from "@heroicons/vue/24/outline";

/**
 * Yetki ekranı (yalnızca Admin): rol matrisi + Satınalmacılara firma/işyeri yetkisi. Yetki işyeri bazında saklanır;
 * bir firmayı işaretlemek tüm işyerlerini verir. Sunucu kapsamı her istekte ayrıca uygular (ScopeAuthorizationBehavior).
 */
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const route = useRoute();

const buyers = ref<UserDto[]>([]);
const firms = ref<FirmDto[]>([]);
const workspaces = ref<WorkspaceDto[]>([]);
const selectedUserId = ref<string | null>(null);
const granted = ref<Set<string>>(new Set());
const original = ref<Set<string>>(new Set());
const term = ref("");
const loading = ref(true);
const saving = ref(false);

type Cell = "yes" | "no" | string;
const roleColumns = ["admin", "buyer", "supplier", "plus"] as const;
const matrix = computed<{ key: string; cells: Record<(typeof roleColumns)[number], Cell> }[]>(() => [
  { key: "defineFirms", cells: { admin: "yes", buyer: "no", supplier: "no", plus: "no" } },
  { key: "manageUsers", cells: { admin: "yes", buyer: "no", supplier: "no", plus: "no" } },
  { key: "scope", cells: { admin: t("permissions.cell.all"), buyer: t("permissions.cell.assigned"), supplier: "no", plus: "no" } },
  { key: "manageTenders", cells: { admin: "yes", buyer: t("permissions.cell.assignedFirms"), supplier: "no", plus: "no" } },
  { key: "changeStatus", cells: { admin: "yes", buyer: "no", supplier: "no", plus: "no" } },
  { key: "manageSuppliers", cells: { admin: "yes", buyer: t("permissions.cell.assignedWorkspaces"), supplier: "no", plus: "no" } },
  { key: "seeTenders", cells: { admin: t("permissions.cell.all"), buyer: t("permissions.cell.assignedFirms"), supplier: t("permissions.cell.invited"), plus: t("permissions.cell.invitedPlusListed") } },
  { key: "joinQuota", cells: { admin: "no", buyer: "no", supplier: t("permissions.cell.twoTenders"), plus: t("permissions.cell.unlimited") } },
  { key: "search", cells: { admin: "yes", buyer: "yes", supplier: "no", plus: "yes" } },
]);

const filteredBuyers = computed(() => {
  const q = term.value.trim().toLocaleLowerCase("tr-TR");
  return buyers.value.filter((u) => !q || `${u.firstName} ${u.lastName} ${u.email}`.toLocaleLowerCase("tr-TR").includes(q));
});
const selectedUser = computed(() => buyers.value.find((u) => u.id === selectedUserId.value) ?? null);
const tree = computed(() => firms.value.map((f) => ({ firm: f, workspaces: workspaces.value.filter((w) => w.firmId === f.id) })));
const dirty = computed(() => granted.value.size !== original.value.size || [...granted.value].some((id) => !original.value.has(id)));

function firmState(wsIds: string[]): "all" | "some" | "none" {
  const n = wsIds.filter((id) => granted.value.has(id)).length;
  return n === 0 ? "none" : n === wsIds.length ? "all" : "some";
}

function toggleFirm(wsIds: string[]) {
  const next = new Set(granted.value);
  if (firmState(wsIds) === "all") wsIds.forEach((id) => next.delete(id));
  else wsIds.forEach((id) => next.add(id));
  granted.value = next;
}

function toggleWorkspace(id: string) {
  const next = new Set(granted.value);
  if (next.has(id)) next.delete(id);
  else next.add(id);
  granted.value = next;
}

async function selectUser(id: string) {
  if (dirty.value && selectedUserId.value && !confirm(t("permissions.discardConfirm"))) return;
  selectedUserId.value = id;
  try {
    const access = await api.get<{ userId: string; workspaceIds: string[] }>(`/api/users/${id}/workspace-access`);
    granted.value = new Set(access.workspaceIds);
    original.value = new Set(access.workspaceIds);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("permissions.loadError")));
  }
}

async function save() {
  if (!selectedUserId.value) return;
  saving.value = true;
  try {
    await api.put(`/api/users/${selectedUserId.value}/workspace-access`, { workspaceIds: [...granted.value] });
    original.value = new Set(granted.value);
    toast.success(t("permissions.saved"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("permissions.saveError")));
  } finally {
    saving.value = false;
  }
}

onMounted(async () => {
  try {
    const [userPage, firmList, wsList] = await Promise.all([
      api.get<PagedResult<UserDto>>("/api/users", { kind: "internal", page: 1, pageSize: 200 }),
      api.get<FirmDto[]>("/api/firms"),
      api.get<WorkspaceDto[]>("/api/workspaces"),
    ]);
    buyers.value = userPage.items.filter((u) => u.roles.includes("Buyer") && !u.roles.includes("Admin"));
    firms.value = firmList;
    workspaces.value = wsList;
    const wanted = typeof route.query.user === "string" ? route.query.user : buyers.value[0]?.id;
    if (wanted && buyers.value.some((u) => u.id === wanted)) await selectUser(wanted);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("permissions.loadError")));
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ $t("permissions.title") }}</h1>
      <p class="text-sm text-slate-500">{{ $t("permissions.subtitle") }}</p>
    </div>

    <!-- Rol matrisi -->
    <AppCard :title="$t('permissions.matrixTitle')" no-padding>
      <div class="overflow-x-auto">
        <table class="w-full min-w-[40rem] text-sm">
          <thead class="border-b border-slate-100 bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th class="px-4 py-3">{{ $t("permissions.capability") }}</th>
              <th v-for="c in roleColumns" :key="c" class="px-4 py-3 text-center">{{ $t(`permissions.roles.${c}`) }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-50">
            <tr v-for="row in matrix" :key="row.key">
              <td class="px-4 py-2.5 font-medium text-slate-700">{{ $t(`permissions.rows.${row.key}`) }}</td>
              <td v-for="c in roleColumns" :key="c" class="px-4 py-2.5 text-center text-xs text-slate-600">
                <CheckIcon v-if="row.cells[c] === 'yes'" class="mx-auto h-4 w-4 text-emerald-500" />
                <MinusIcon v-else-if="row.cells[c] === 'no'" class="mx-auto h-4 w-4 text-slate-300" />
                <span v-else>{{ row.cells[c] }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </AppCard>

    <!-- Satınalmacı yetkileri -->
    <div class="grid gap-6 lg:grid-cols-3">
      <AppCard :title="$t('permissions.buyersTitle')" no-padding>
        <div class="border-b border-slate-100 p-3">
          <div class="relative">
            <MagnifyingGlassIcon class="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input v-model="term" type="search" :placeholder="$t('permissions.searchBuyer')" class="w-full rounded-md border border-slate-300 py-1.5 pl-8 pr-3 text-sm" />
          </div>
        </div>
        <LoadingSpinner v-if="loading" />
        <EmptyState v-else-if="filteredBuyers.length === 0">{{ $t("permissions.noBuyers") }}</EmptyState>
        <ul v-else class="max-h-[28rem] divide-y divide-slate-50 overflow-y-auto">
          <li v-for="u in filteredBuyers" :key="u.id">
            <button
              class="w-full px-4 py-2.5 text-left hover:bg-slate-50"
              :class="{ 'bg-indigo-50': u.id === selectedUserId }"
              @click="selectUser(u.id)"
            >
              <div class="text-sm font-medium" :class="u.id === selectedUserId ? 'text-indigo-700' : 'text-slate-800'">{{ u.firstName }} {{ u.lastName }}</div>
              <div class="text-xs text-slate-400">{{ u.email }}<span v-if="!u.isActive"> · {{ $t("common.passive") }}</span></div>
            </button>
          </li>
        </ul>
      </AppCard>

      <AppCard class="lg:col-span-2" :title="selectedUser ? $t('permissions.accessFor', { name: `${selectedUser.firstName} ${selectedUser.lastName}` }) : $t('permissions.accessTitle')">
        <template #action>
          <AppButton v-if="selectedUser" :loading="saving" :disabled="!dirty" @click="save">{{ $t("common.save") }}</AppButton>
        </template>
        <EmptyState v-if="!selectedUser">{{ $t("permissions.pickBuyer") }}</EmptyState>
        <div v-else class="space-y-3">
          <p class="text-xs text-slate-500">{{ $t("permissions.treeHint") }}</p>
          <div v-for="node in tree" :key="node.firm.id" class="rounded-lg border border-slate-200">
            <label class="flex cursor-pointer items-center gap-2 bg-slate-50 px-3 py-2">
              <input
                type="checkbox"
                class="rounded"
                :checked="firmState(node.workspaces.map((w) => w.id)) === 'all'"
                :indeterminate.prop="firmState(node.workspaces.map((w) => w.id)) === 'some'"
                :disabled="node.workspaces.length === 0"
                @change="toggleFirm(node.workspaces.map((w) => w.id))"
              />
              <BuildingOffice2Icon class="h-4 w-4 text-slate-400" />
              <span class="font-mono text-xs text-slate-400">{{ node.firm.code }}</span>
              <span class="text-sm font-medium text-slate-800">{{ node.firm.name }}</span>
              <span class="ml-auto text-xs text-slate-400">
                {{ node.workspaces.filter((w) => granted.has(w.id)).length }}/{{ node.workspaces.length }}
              </span>
            </label>
            <div v-if="node.workspaces.length > 0" class="grid gap-1 px-3 py-2 sm:grid-cols-2">
              <label v-for="w in node.workspaces" :key="w.id" class="flex cursor-pointer items-center gap-2 rounded px-2 py-1 text-sm text-slate-700 hover:bg-slate-50">
                <input type="checkbox" class="rounded" :checked="granted.has(w.id)" @change="toggleWorkspace(w.id)" />
                {{ w.name }}
              </label>
            </div>
            <p v-else class="px-3 py-2 text-xs text-slate-400">{{ $t("permissions.noWorkspaces") }}</p>
          </div>
        </div>
      </AppCard>
    </div>
  </div>
</template>
