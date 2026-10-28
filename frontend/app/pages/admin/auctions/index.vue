<script setup lang="ts">
import type { AuctionDto, CurrencyDto, PagedResult } from "~/types/api";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const auctions = ref<AuctionDto[]>([]);
const currencies = ref<CurrencyDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const { selectedFirmId } = useFirm();
const { selectedWorkspaceId } = useWorkspace();

const form = reactive({
  title: "",
  description: "",
  auctionType: "English",
  ownerCompanyId: "",
  workspaceId: "",
  currencyId: "",
  startAt: "",
  endAt: "",
  extensionRuleMinutes: 2,
  isListedForPlus: false,
});

const auctionTypes = ["English", "Dutch", "ReverseSealed", "SealedBid"];

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Scheduled: "amber",
  Live: "emerald",
  Paused: "amber",
  Ended: "slate",
  Awarded: "indigo",
  Cancelled: "rose",
};

// Başlıktaki firma/işyeri açılışta art arda kurulur ve her değişim yeniden yükler; geç dönen eski (ör. filtresiz)
// yanıt yenisinin üzerine yazmasın diye yalnızca son isteğin sonucu uygulanır.
let loadSeq = 0;
const { page: listPage, total: listTotal, search: listSearch, pageSize: listPageSize, params: listParams, resetAndLoad } = usePagedList(() => load());


async function load() {
  const seq = ++loadSeq;
  loading.value = true;
  try {
    const [auctionPage, currencyList] = await Promise.all([
      // Başlıkta işyeri seçiliyse yalnızca o işyerinin ihaleleri; "Tüm İşyerleri"nde firmanın tümü.
      api.get<PagedResult<AuctionDto>>("/api/auctions", {
        ...listParams(),
        firmId: selectedFirmId.value ?? undefined,
        workspaceId: selectedWorkspaceId.value ?? undefined,
      }),
      api.get<CurrencyDto[]>("/api/currencies"),
    ]);
    if (seq !== loadSeq) return;
    auctions.value = auctionPage.items;
    listTotal.value = auctionPage.totalCount;
    currencies.value = currencyList;

    if (!form.currencyId && currencies.value.length > 0) {
      form.currencyId = currencies.value.find((c) => c.isBaseCurrency)?.id ?? currencies.value[0].id;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.loadError")));
  } finally {
    if (seq === loadSeq) loading.value = false;
  }
}

async function createAuction() {
  submitting.value = true;
  try {
    const id = await api.post<string>("/api/auctions", {
      title: form.title,
      description: form.description || null,
      auctionType: form.auctionType,
      ownerCompanyId: form.ownerCompanyId,
      currencyId: form.currencyId,
      startAt: new Date(form.startAt).toISOString(),
      endAt: new Date(form.endAt).toISOString(),
      reservePrice: null,
      minDecrementAmount: null,
      minDecrementPercentage: null,
      extensionRuleMinutes: form.extensionRuleMinutes,
      isListedForPlus: form.isListedForPlus,
      workspaceId: form.workspaceId || null,
    });
    toast.success(t("auctions.createdToast"));
    await navigateTo(`/admin/auctions/${id}`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.createError")));
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
watch([selectedFirmId, selectedWorkspaceId], () => resetAndLoad());
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("auctions.title") }}</h1>
      <div class="flex items-center gap-2">
        <NuxtLink to="/admin/auctions/create" class="inline-flex items-center gap-1.5 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
          {{ $t("tender.wizard.title") }}
        </NuxtLink>
        <AppButton variant="secondary" @click="showForm = !showForm">
          {{ showForm ? $t("common.cancel") : $t("auctions.new") }}
        </AppButton>
      </div>
    </div>

    <form v-if="showForm" class="space-y-4 rounded-xl border border-slate-200 bg-surface p-5" @submit.prevent="createAuction">
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div class="sm:col-span-2">
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.form.title") }}</label>
          <input v-model="form.title" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div class="sm:col-span-2">
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.form.description") }}</label>
          <textarea v-model="form.description" rows="2" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.form.auctionType") }}</label>
          <select v-model="form.auctionType" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option v-for="at in auctionTypes" :key="at" :value="at">{{ $t(`enums.auctionType.${at}`) }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.form.extensionMinutes") }}</label>
          <input v-model.number="form.extensionRuleMinutes" type="number" min="0" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <!-- Sahip yalnızca firma olabilir (tedarikçi cari seçilemez); işyeri başlıktaki seçimle dolar -->
        <div class="grid grid-cols-1 gap-3 sm:col-span-2 sm:grid-cols-2">
          <FirmWorkspaceSelect v-model:owner="form.ownerCompanyId" v-model:workspace="form.workspaceId" />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.form.currency") }}</label>
          <select v-model="form.currencyId" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.form.startAt") }}</label>
          <input v-model="form.startAt" type="datetime-local" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.form.endAt") }}</label>
          <input v-model="form.endAt" type="datetime-local" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <label class="flex items-start gap-2 text-sm text-slate-700 sm:col-span-2">
          <input v-model="form.isListedForPlus" type="checkbox" class="mt-0.5 rounded" />
          <span>
            {{ $t("tender.wizard.f.listedForPlus") }}
            <span class="block text-xs text-slate-400">{{ $t("tender.wizard.f.listedForPlusHint") }}</span>
          </span>
        </label>
      </div>

      <AppButton type="submit" :loading="submitting">
        {{ submitting ? $t("auctions.form.creating") : $t("auctions.form.createDraft") }}
      </AppButton>
      <p class="text-xs text-slate-400">{{ $t("auctions.form.hint") }}</p>
    </form>

    <AppCard>
      <input v-model="listSearch" type="search" :placeholder="$t('pager.search.auctions')" :aria-label="$t('pager.search.auctions')" class="mb-3 w-full max-w-sm rounded-md border border-slate-300 px-3 py-1.5 text-sm" data-testid="list-search" />
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("auctions.table.title") }}</th>
              <th class="pb-2">{{ $t("auctions.table.owner") }}</th>
              <th class="pb-2">{{ $t("auctions.table.type") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2">{{ $t("auctions.table.items") }}</th>
              <th class="pb-2">{{ $t("auctions.table.invitations") }}</th>
              <th class="pb-2">{{ $t("auctions.table.endsAt") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">{{ a.title }}</td>
              <td class="py-2 text-slate-500">
                {{ a.ownerCompanyName }}
                <span v-if="a.workspaceName" class="block text-[11px] text-slate-400">{{ a.workspaceName }}</span>
              </td>
              <td class="py-2 text-slate-500">{{ $t(`enums.auctionType.${a.auctionType}`) }}</td>
              <td class="py-2">
                <AppBadge :tone="statusTone[a.status]">{{ $t(`enums.auctionStatus.${a.status}`) }}</AppBadge>
              </td>
              <td class="py-2 text-slate-500">{{ a.itemCount }}</td>
              <td class="py-2 text-slate-500">{{ a.invitationCount }}</td>
              <td class="py-2 text-slate-500">{{ formatDateTime(a.endAt) }}</td>
              <td class="py-2 text-right">
                <NuxtLink :to="`/admin/auctions/${a.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="auctions.length === 0">{{ $t("auctions.empty") }}</EmptyState>
        <ListPager v-model:page="listPage" :total="listTotal" :page-size="listPageSize" />
      </div>
    </AppCard>
  </div>
</template>
