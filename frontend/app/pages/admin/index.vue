<script setup lang="ts">
import type { AuctionDto, AuctionModelRecommendationDto, PagedResult } from "~/types/api";

definePageMeta({ middleware: [] });

const api = useApi();

const auctions = ref<AuctionDto[]>([]);
const companyCount = ref(0);
const productCount = ref(0);
const loading = ref(true);
const errorMessage = ref("");

const recommendation = ref<AuctionModelRecommendationDto | null>(null);
const recommendationLoading = ref(false);
const recommendationError = ref("");
const estimatedParticipants = ref(5);
const estimatedItems = ref(1);

async function load() {
  loading.value = true;
  errorMessage.value = "";
  try {
    const [auctionPage, companyPage, productPage] = await Promise.all([
      api.get<PagedResult<AuctionDto>>("/api/auctions", { page: 1, pageSize: 10 }),
      api.get<PagedResult<unknown>>("/api/companies", { page: 1, pageSize: 1 }),
      api.get<PagedResult<unknown>>("/api/products", { page: 1, pageSize: 1 }),
    ]);
    auctions.value = auctionPage.items;
    companyCount.value = companyPage.totalCount;
    productCount.value = productPage.totalCount;
  } catch (error: any) {
    errorMessage.value = apiErrorMessage(error, "Panel verileri yüklenemedi.");
  } finally {
    loading.value = false;
  }
}

async function getRecommendation() {
  recommendationLoading.value = true;
  recommendationError.value = "";
  try {
    recommendation.value = await api.post<AuctionModelRecommendationDto>("/api/ai/auction-model-recommendation", {
      categoryId: null,
      candidateSupplierCompanyIds: null,
      estimatedParticipantCount: estimatedParticipants.value,
      estimatedItemCount: estimatedItems.value,
    });
  } catch (error: any) {
    recommendationError.value = apiErrorMessage(error, "Öneri alınamadı.");
  } finally {
    recommendationLoading.value = false;
  }
}

const statusBadge: Record<string, string> = {
  Draft: "bg-slate-100 text-slate-600",
  Scheduled: "bg-amber-100 text-amber-700",
  Live: "bg-emerald-100 text-emerald-700",
  Ended: "bg-slate-100 text-slate-600",
  Awarded: "bg-indigo-100 text-indigo-700",
  Cancelled: "bg-rose-100 text-rose-700",
};

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">Admin Paneli</h1>
      <NuxtLink
        to="/admin/auctions"
        class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
      >
        + Yeni İhale
      </NuxtLink>
    </div>

    <p v-if="errorMessage" class="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{{ errorMessage }}</p>

    <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">Kayıtlı Cari</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ companyCount }}</div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">Katalogdaki Ürün</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ productCount }}</div>
      </div>
      <div class="rounded-xl border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">Son İhaleler</div>
        <div class="mt-1 text-2xl font-bold text-slate-800">{{ auctions.length }}</div>
      </div>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <h2 class="text-sm font-semibold text-slate-700">Son İhaleler</h2>
      <table class="mt-3 w-full text-sm">
        <thead>
          <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
            <th class="pb-2">Başlık</th>
            <th class="pb-2">Tür</th>
            <th class="pb-2">Durum</th>
            <th class="pb-2">Bitiş</th>
            <th class="pb-2"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50">
            <td class="py-2 font-medium text-slate-700">{{ a.title }}</td>
            <td class="py-2 text-slate-500">{{ a.auctionType }}</td>
            <td class="py-2">
              <span class="rounded px-2 py-0.5 text-xs font-medium" :class="statusBadge[a.status]">{{ a.status }}</span>
            </td>
            <td class="py-2 text-slate-500">{{ new Date(a.endAt).toLocaleString("tr-TR") }}</td>
            <td class="py-2 text-right">
              <NuxtLink :to="`/admin/auctions/${a.id}`" class="text-indigo-600 hover:underline">Detay</NuxtLink>
            </td>
          </tr>
          <tr v-if="!loading && auctions.length === 0">
            <td colspan="5" class="py-6 text-center text-slate-400">Henüz ihale yok</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5">
      <h2 class="text-sm font-semibold text-slate-700">
        🤖 En Uygun İhale Modeli Önerisi
        <span class="ml-1 text-xs font-normal text-slate-400">(veri-hazır placeholder, Faz 4 AI altyapısı)</span>
      </h2>
      <div class="mt-3 flex flex-wrap items-end gap-3">
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-500">Tahmini Katılımcı</label>
          <input v-model.number="estimatedParticipants" type="number" min="1" class="w-28 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-slate-500">Tahmini Kalem Sayısı</label>
          <input v-model.number="estimatedItems" type="number" min="1" class="w-28 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
        </div>
        <button
          class="rounded-md bg-slate-800 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-900 disabled:opacity-50"
          :disabled="recommendationLoading"
          @click="getRecommendation"
        >
          {{ recommendationLoading ? "Hesaplanıyor..." : "Öner" }}
        </button>
      </div>

      <p v-if="recommendationError" class="mt-3 text-sm text-rose-600">{{ recommendationError }}</p>

      <div v-if="recommendation" class="mt-4 rounded-lg bg-indigo-50 p-4">
        <div class="text-sm font-semibold text-indigo-800">
          Önerilen: {{ recommendation.recommendedAuctionType }}
          <span class="ml-2 text-xs font-normal text-indigo-500">
            (güven skoru: {{ Math.round(recommendation.confidenceScore * 100) }}%)
          </span>
        </div>
        <p class="mt-1 text-sm text-indigo-700">{{ recommendation.rationale }}</p>
      </div>
    </div>
  </div>
</template>
