<script setup lang="ts">
import type { AuctionDto, CompanyDto, CurrencyDto, PagedResult } from "~/types/api";

const api = useApi();
const toast = useToast();

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
    toast.error(apiErrorMessage(error, "Kayıtlar yüklenemedi."));
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
    toast.success("İhale taslağı oluşturuldu.");
    await navigateTo(`/admin/auctions/${id}`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "İhale oluşturulamadı."));
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">İhaleler</h1>
      <AppButton @click="showForm = !showForm">
        {{ showForm ? "Vazgeç" : "+ Yeni İhale" }}
      </AppButton>
    </div>

    <form v-if="showForm" class="space-y-4 rounded-xl border border-slate-200 bg-white p-5" @submit.prevent="createAuction">
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div class="sm:col-span-2">
          <label class="mb-1 block text-sm font-medium text-slate-700">Başlık</label>
          <input v-model="form.title" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div class="sm:col-span-2">
          <label class="mb-1 block text-sm font-medium text-slate-700">Açıklama</label>
          <textarea v-model="form.description" rows="2" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">İhale Türü</label>
          <select v-model="form.auctionType" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option v-for="t in auctionTypes" :key="t" :value="t">{{ t }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">Süre Uzatma (dk)</label>
          <input v-model.number="form.extensionRuleMinutes" type="number" min="0" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">Alıcı Cari</label>
          <select v-model="form.ownerCompanyId" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option v-for="c in companies" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">Para Birimi</label>
          <select v-model="form.currencyId" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
            <option v-for="c in currencies" :key="c.id" :value="c.id">{{ c.code }}</option>
          </select>
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">Başlangıç</label>
          <input v-model="form.startAt" type="datetime-local" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium text-slate-700">Bitiş</label>
          <input v-model="form.endAt" type="datetime-local" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        </div>
      </div>

      <AppButton type="submit" :loading="submitting">
        {{ submitting ? "Oluşturuluyor..." : "Taslak Oluştur" }}
      </AppButton>
      <p class="text-xs text-slate-400">Oluşturulduktan sonra detay sayfasından kalem ekleyip Cari davet edebilirsiniz.</p>
    </form>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">Başlık</th>
              <th class="pb-2">Alıcı</th>
              <th class="pb-2">Tür</th>
              <th class="pb-2">Durum</th>
              <th class="pb-2">Kalem</th>
              <th class="pb-2">Davet</th>
              <th class="pb-2">Bitiş</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">{{ a.title }}</td>
              <td class="py-2 text-slate-500">{{ a.ownerCompanyName }}</td>
              <td class="py-2 text-slate-500">{{ a.auctionType }}</td>
              <td class="py-2">
                <AppBadge :tone="statusTone[a.status]">{{ a.status }}</AppBadge>
              </td>
              <td class="py-2 text-slate-500">{{ a.itemCount }}</td>
              <td class="py-2 text-slate-500">{{ a.invitationCount }}</td>
              <td class="py-2 text-slate-500">{{ new Date(a.endAt).toLocaleString("tr-TR") }}</td>
              <td class="py-2 text-right">
                <NuxtLink :to="`/admin/auctions/${a.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="auctions.length === 0">Henüz ihale yok</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
