<script setup lang="ts">
import { PlusIcon } from "@heroicons/vue/24/outline";
import type { AuctionDto, PagedResult } from "~/types/api";

/**
 * İhale listesi. Yeni ihale tek yoldan (sihirbaz) açılır. Durum süzgeci adres çubuğuyla senkron (?status=),
 * panodaki "Yapılacaklar" bağlantıları listeyi süzülmüş açar. Başlıkta firma/işyeri seçimine uyar.
 */
const api = useApi();
const { can } = usePermissions();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();
const route = useRoute();

const auctions = ref<AuctionDto[]>([]);
const loading = ref(true);
const status = ref(typeof route.query.status === "string" ? route.query.status : "");

const { selectedFirmId } = useFirm();
const { selectedWorkspaceId } = useWorkspace();

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Scheduled: "amber",
  Live: "emerald",
  Paused: "amber",
  Ended: "slate",
  Awarded: "indigo",
  Cancelled: "rose",
};
const statusOptions = computed(() =>
  ["Draft", "Scheduled", "Live", "Paused", "Ended", "Awarded", "Cancelled"].map((s) => ({ value: s, label: t(`enums.auctionStatus.${s}`) })),
);

// Başlıktaki firma/işyeri açılışta art arda kurulur ve her değişim yeniden yükler; geç dönen eski (ör. filtresiz)
// yanıt yenisinin üzerine yazmasın diye yalnızca son isteğin sonucu uygulanır.
let loadSeq = 0;
const { page: listPage, total: listTotal, search: listSearch, pageSize: listPageSize, params: listParams, resetAndLoad } = usePagedList(() => load());

async function load() {
  const seq = ++loadSeq;
  loading.value = true;
  try {
    // Başlıkta işyeri seçiliyse yalnızca o işyerinin ihaleleri; "Tüm İşyerleri"nde firmanın (ya da tüm firmaların) tümü.
    const result = await api.get<PagedResult<AuctionDto>>("/api/auctions", {
      ...listParams(),
      status: status.value || undefined,
      firmId: selectedFirmId.value ?? undefined,
      workspaceId: selectedWorkspaceId.value ?? undefined,
    });
    if (seq !== loadSeq) return;
    auctions.value = result.items;
    listTotal.value = result.totalCount;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.loadError")));
  } finally {
    if (seq === loadSeq) loading.value = false;
  }
}

onMounted(load);
watch([selectedFirmId, selectedWorkspaceId, status], () => resetAndLoad());
watch(() => route.query.status, (v) => { status.value = typeof v === "string" ? v : ""; });
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-slate-800">{{ $t("auctions.title") }}</h1>
        <p class="mt-0.5 text-sm text-slate-500">{{ $t("auctions.subtitle") }}</p>
      </div>
      <NuxtLink
        v-if="can('auctions.manage')"
        to="/admin/auctions/create"
        class="inline-flex items-center gap-1.5 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
        data-testid="new-auction"
      >
        <PlusIcon class="h-4 w-4" />{{ $t("auctions.new") }}
      </NuxtLink>
    </div>

    <AppCard>
      <div class="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <input v-model="listSearch" type="search" :placeholder="$t('pager.search.auctions')" :aria-label="$t('pager.search.auctions')" class="w-full max-w-sm rounded-md border border-slate-300 px-3 py-1.5 text-sm" data-testid="list-search" />
        <StatusFilter v-model="status" :options="statusOptions" />
      </div>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("auctions.table.title") }}</th>
              <th class="hidden pb-2 md:table-cell">{{ $t("auctions.table.owner") }}</th>
              <th class="hidden pb-2 lg:table-cell">{{ $t("auctions.table.type") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="hidden pb-2 text-right lg:table-cell">{{ $t("auctions.table.items") }}</th>
              <th class="hidden pb-2 text-right lg:table-cell">{{ $t("auctions.table.invitations") }}</th>
              <th class="hidden pb-2 sm:table-cell">{{ $t("auctions.table.endsAt") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50 hover:bg-slate-50/60">
              <td class="py-2.5 pr-3">
                <NuxtLink :to="`/admin/auctions/${a.id}`" class="font-medium text-slate-800 hover:text-indigo-600 hover:underline">{{ a.title }}</NuxtLink>
                <span class="block text-[11px] text-slate-400 md:hidden">{{ a.ownerCompanyName }}</span>
              </td>
              <td class="hidden py-2.5 text-slate-500 md:table-cell">
                {{ a.ownerCompanyName }}
                <span v-if="a.workspaceName" class="block text-[11px] text-slate-400">{{ a.workspaceName }}</span>
              </td>
              <td class="hidden py-2.5 text-slate-500 lg:table-cell">{{ $t(`enums.auctionType.${a.auctionType}`) }}</td>
              <td class="py-2.5">
                <AppBadge :tone="statusTone[a.status]">{{ $t(`enums.auctionStatus.${a.status}`) }}</AppBadge>
              </td>
              <td class="hidden py-2.5 text-right tabular-nums text-slate-500 lg:table-cell">{{ a.itemCount }}</td>
              <td class="hidden py-2.5 text-right tabular-nums text-slate-500 lg:table-cell">{{ a.invitationCount }}</td>
              <td class="hidden py-2.5 text-slate-500 sm:table-cell">{{ formatDateTime(a.endAt) }}</td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="auctions.length === 0">{{ status ? $t("auctions.emptyFiltered") : $t("auctions.empty") }}</EmptyState>
        <ListPager v-model:page="listPage" :total="listTotal" :page-size="listPageSize" />
      </div>
    </AppCard>
  </div>
</template>
