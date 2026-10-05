<script setup lang="ts">
import type { AuctionItemDto, BidComparisonDto } from "~/types/api";
import { TrophyIcon, CheckCircleIcon } from "@heroicons/vue/24/solid";
import { CurrencyDollarIcon } from "@heroicons/vue/24/outline";

const props = defineProps<{
  item: AuctionItemDto;
  bids: BidComparisonDto[];
  auctionStatus: string;
  currencyCode: string;
  awardedBidId?: string | null;
}>();

const emit = defineEmits<{ award: [bidId: string] }>();
const { t } = useI18n();

// Sort ascending by normalized amount: lowest (best procurement offer) first
const ranked = computed(() =>
  [...props.bids].sort((a, b) => a.normalizedAmountInBaseCurrency - b.normalizedAmountInBaseCurrency),
);

const maxNormalized = computed(() => Math.max(...ranked.value.map((b) => b.normalizedAmountInBaseCurrency), 1));
const bestNormalized = computed(() => ranked.value[0]?.normalizedAmountInBaseCurrency ?? 1);

const canAward = computed(() => props.auctionStatus === "Ended");

function barWidth(bid: BidComparisonDto): string {
  return ((bid.normalizedAmountInBaseCurrency / maxNormalized.value) * 100).toFixed(1) + "%";
}

function deltaVsBest(bid: BidComparisonDto): string {
  const pct = ((bid.normalizedAmountInBaseCurrency - bestNormalized.value) / bestNormalized.value) * 100;
  if (pct < 0.05) return "—";
  return `+${pct.toFixed(1)}%`;
}

function deltaVsTarget(bid: BidComparisonDto): string | null {
  if (!props.item.targetPrice) return null;
  const pct = ((bid.amount - props.item.targetPrice) / props.item.targetPrice) * 100;
  return (pct >= 0 ? "+" : "") + pct.toFixed(1) + "%";
}

function barColor(rank: number): string {
  if (rank === 0) return "bg-emerald-500";
  if (rank === 1) return "bg-amber-400";
  if (rank === 2) return "bg-orange-400";
  return "bg-slate-300";
}

const rankMedal = ["🥇", "🥈", "🥉"];

function pricePerUnit(bid: BidComparisonDto): string {
  const unitPrice = bid.amount / props.item.quantity;
  return unitPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

const totalEstimated = computed(() => {
  const best = ranked.value[0];
  if (!best) return null;
  const winner = props.awardedBidId ? ranked.value.find((b) => b.bidId === props.awardedBidId) : best;
  return (winner ?? best).amount;
});
</script>

<template>
  <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
    <!-- Item header -->
    <div class="flex items-start justify-between gap-4 border-b border-slate-100 bg-slate-50 px-5 py-3">
      <div class="min-w-0">
        <div class="font-semibold text-slate-800 leading-snug">{{ item.description }}</div>
        <div class="mt-0.5 text-xs text-slate-500">
          {{ item.quantity }} {{ $t(`enums.unitOfMeasure.${item.unitOfMeasure}`) }}
          <template v-if="item.targetPrice">
            · {{ $t("auctions.evaluation.target") }}: {{ item.targetPrice.toLocaleString() }} {{ currencyCode }}
          </template>
        </div>
      </div>
      <div class="flex shrink-0 flex-col items-end gap-1">
        <div class="flex items-center gap-1.5 text-xs text-slate-500">
          <CurrencyDollarIcon class="h-3.5 w-3.5" />
          {{ bids.length }} {{ $t("auctions.evaluation.bids") }}
        </div>
        <div v-if="awardedBidId" class="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700">
          <CheckCircleIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.evaluation.awarded") }}
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-if="bids.length === 0" class="px-5 py-8 text-center text-sm text-slate-400">
      {{ $t("auctions.evaluation.noBids") }}
    </div>

    <!-- Bid rows -->
    <div v-else class="divide-y divide-slate-50">
      <div
        v-for="(bid, idx) in ranked"
        :key="bid.bidId"
        class="px-5 py-3"
        :class="bid.bidId === awardedBidId ? 'bg-emerald-50/60' : idx === 0 ? 'bg-white' : 'bg-white hover:bg-slate-50'"
      >
        <div class="flex items-center gap-3">
          <!-- Rank -->
          <div class="w-7 shrink-0 text-center text-base leading-none">
            <TrophyIcon v-if="idx === 0 && !awardedBidId" class="h-5 w-5 text-amber-400 mx-auto" />
            <CheckCircleIcon v-else-if="bid.bidId === awardedBidId" class="h-5 w-5 text-emerald-500 mx-auto" />
            <span v-else class="text-xs text-slate-400 font-medium">{{ idx + 1 }}</span>
          </div>

          <!-- Company + bar -->
          <div class="min-w-0 flex-1">
            <div class="flex items-center justify-between gap-2">
              <span
                class="text-sm font-semibold truncate"
                :class="bid.bidId === awardedBidId ? 'text-emerald-700' : idx === 0 ? 'text-slate-800' : 'text-slate-600'"
              >
                {{ bid.companyName }}
              </span>
              <div class="flex shrink-0 items-center gap-3 text-xs">
                <span class="font-mono font-semibold text-slate-800">
                  {{ bid.amount.toLocaleString() }} {{ bid.currencyCode }}
                </span>
                <span
                  v-if="deltaVsBest(bid) !== '—'"
                  class="rounded px-1.5 py-0.5 font-medium"
                  :class="idx === 0 ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'"
                >
                  {{ idx === 0 ? $t("auctions.evaluation.bestOffer") : deltaVsBest(bid) }}
                </span>
                <span
                  v-if="item.targetPrice"
                  class="rounded px-1.5 py-0.5 font-medium"
                  :class="(bid.amount - item.targetPrice) <= 0 ? 'bg-emerald-50 text-emerald-600' : 'bg-rose-50 text-rose-600'"
                >
                  {{ deltaVsTarget(bid) }}
                </span>
              </div>
            </div>
            <!-- Price bar -->
            <div class="mt-1.5 h-1.5 w-full rounded-full bg-slate-100">
              <div
                class="h-full rounded-full transition-all duration-500"
                :class="barColor(idx)"
                :style="{ width: barWidth(bid) }"
              />
            </div>
            <!-- Per unit price -->
            <div class="mt-1 text-[11px] text-slate-400">
              {{ $t("auctions.evaluation.unitPrice") }}: {{ pricePerUnit(bid) }} {{ bid.currencyCode }}
              <template v-if="bid.currencyCode !== currencyCode">
                · {{ $t("auctions.evaluation.normalized") }}: {{ bid.normalizedAmountInBaseCurrency.toFixed(2) }} {{ currencyCode }}
              </template>
            </div>
          </div>

          <!-- Award action -->
          <div class="shrink-0">
            <div v-if="bid.bidId === awardedBidId" class="text-xs font-semibold text-emerald-600">
              {{ $t("auctions.evaluation.winner") }}
            </div>
            <button
              v-else-if="canAward && !awardedBidId"
              class="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:border-indigo-300 hover:bg-indigo-50 hover:text-indigo-700 transition-colors"
              @click="emit('award', bid.bidId)"
            >
              {{ $t("auctions.evaluation.selectWinner") }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
