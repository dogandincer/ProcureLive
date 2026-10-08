<script setup lang="ts">
import type { AuctionDto, CompanyDto, CurrencyDto, PagedResult } from "~/types/api";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const auctions = ref<AuctionDto[]>([]);
const companies = ref<CompanyDto[]>([]);
const currencies = ref<CurrencyDto[]>([]);
const showForm = ref(false);
const submitting = ref(false);
const loading = ref(true);

const form = reactive({
  title: "",
  description: "",
  auctionType: "English",
  ownerCompanyId: "",
  currencyId: "",
  startAt: "",
  endAt: "",
  extensionRuleMinutes: 2,
});

const auctionTypes = ["English", "Dutch", "ReverseSealed", "SealedBid"];

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Scheduled: "amber",
  Live: "emerald",
  Ended: "slate",
  Awarded: "indigo",
  Cancelled: "rose",
};

async function load() {
  loading.value = true;
  try {
    const [auctionPage, companyPage, currencyList] = await Promise.all([
      api.get<PagedResult<AuctionDto>>("/api/auctions", { page: 1, pageSize: 50 }),
      api.get<PagedResult<CompanyDto>>("/api/companies", { page: 1, pageSize: 100 }),
      api.get<CurrencyDto[]>("/api/currencies"),
    ]);
    auctions.value = auctionPage.items;
    companies.value = companyPage.items;
    currencies.value = currencyList;

    if (!form.ownerCompanyId && companies.value.length > 0) {
      form.ownerCompanyId = companies.value.find((c) => c.companyType === "Internal")?.id ?? companies.value[0].id;
    }
    if (!form.currencyId && currencies.value.length > 0) {
      form.currencyId = currencies.value.find((c) => c.isBaseCurrency)?.id ?? currencies.value[0].id;
    }
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.loadError")));
  } finally {
    loading.value = false;
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

    <form v-if="showForm" class="space-y-4 rounded-xl border border-slate-200 bg-white p-5" @submit.prevent="createAuction">
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
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.form.ownerCompany") }}</label>
          <select v-model="form.ownerCompanyId" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
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
      </div>

      <AppButton type="submit" :loading="submitting">
        {{ submitting ? $t("auctions.form.creating") : $t("auctions.form.createDraft") }}
      </AppButton>
      <p class="text-xs text-slate-400">{{ $t("auctions.form.hint") }}</p>
    </form>

    <AppCard>
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
              <td class="py-2 text-slate-500">{{ a.ownerCompanyName }}</td>
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
      </div>
    </AppCard>
  </div>
</template>
