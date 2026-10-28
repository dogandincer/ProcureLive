<script setup lang="ts">
import type { AuctionItemDto, BidComparisonDto } from "~/types/api";
import { TrophyIcon, CheckCircleIcon } from "@heroicons/vue/24/solid";
import { CurrencyDollarIcon } from "@heroicons/vue/24/outline";

/**
 * Bir kalemin teklif değerlendirmesi. Teklif tutarı birim fiyattır; toplam = birim × miktar.
 * Her firmanın en iyi teklifi bir satırdır; İngiliz (artan) ihalede yüksek, diğerlerinde düşük teklif öndedir.
 * Kazanan bilgisi sunucudaki ödül kaydından (isAwarded) gelir — canlıdaki “Winning” durumu ödül değildir.
 */
const props = defineProps<{
  item: AuctionItemDto;
  bids: BidComparisonDto[];
  auctionStatus: string;
  auctionType: string;
  currencyCode: string;
}>();

const emit = defineEmits<{ award: [bid: BidComparisonDto] }>();
const { formatMoney, formatDateTime } = useDateFormat();

const higherIsBetter = computed(() => props.auctionType === "English");

// Firma başına en iyi teklif (ödüllü teklif varsa o firmanın satırı ödüllü teklifi gösterir).
const ranked = computed(() => {
  const byCompany = new Map<string, BidComparisonDto>();
  for (const b of props.bids) {
    const cur = byCompany.get(b.companyId);
    const better = !cur
      || b.isAwarded
      || (!cur.isAwarded && (higherIsBetter.value
        ? b.normalizedAmountInBaseCurrency > cur.normalizedAmountInBaseCurrency
        : b.normalizedAmountInBaseCurrency < cur.normalizedAmountInBaseCurrency));
    if (better) byCompany.set(b.companyId, b);
  }
  const dir = higherIsBetter.value ? -1 : 1;
  return [...byCompany.values()].sort((a, b) => dir * (a.normalizedAmountInBaseCurrency - b.normalizedAmountInBaseCurrency));
});

const awarded = computed(() => props.bids.find((b) => b.isAwarded) ?? null);
const canAward = computed(() => props.auctionStatus === "Ended" && !awarded.value);
const best = computed(() => ranked.value[0]?.normalizedAmountInBaseCurrency ?? 0);
const maxNormalized = computed(() => Math.max(...ranked.value.map((b) => b.normalizedAmountInBaseCurrency), 1));

function barWidth(bid: BidComparisonDto): string {
  return ((bid.normalizedAmountInBaseCurrency / maxNormalized.value) * 100).toFixed(1) + "%";
}

function deltaVsBest(bid: BidComparisonDto): string | null {
  if (!best.value) return null;
  const pct = ((bid.normalizedAmountInBaseCurrency - best.value) / best.value) * 100;
  if (Math.abs(pct) < 0.05) return null;
  return (pct > 0 ? "+" : "") + pct.toFixed(1) + "%";
}

function deltaVsTarget(bid: BidComparisonDto): { text: string; good: boolean } | null {
  if (!props.item.targetPrice) return null;
  const pct = ((bid.amount - props.item.targetPrice) / props.item.targetPrice) * 100;
  return { text: (pct >= 0 ? "+" : "") + pct.toFixed(1) + "%", good: higherIsBetter.value ? pct >= 0 : pct <= 0 };
}

function barColor(rank: number): string {
  if (rank === 0) return "bg-emerald-500";
  if (rank === 1) return "bg-amber-400";
  if (rank === 2) return "bg-orange-400";
  return "bg-slate-300";
}
</script>

<template>
  <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm" data-testid="bid-comparison">
    <div class="flex items-start justify-between gap-4 border-b border-slate-100 bg-slate-50 px-5 py-3">
      <div class="min-w-0">
        <div class="font-semibold leading-snug text-slate-800">{{ item.description }}</div>
        <div class="mt-0.5 text-xs text-slate-500">
          {{ item.quantity.toLocaleString() }} {{ $t(`enums.unitOfMeasure.${item.unitOfMeasure}`) }}
          <template v-if="item.targetPrice">
            · {{ $t("auctions.evaluation.targetUnit") }}: {{ formatMoney(item.targetPrice, currencyCode) }}
          </template>
        </div>
      </div>
      <div class="flex shrink-0 flex-col items-end gap-1">
        <div class="flex items-center gap-1.5 text-xs text-slate-500">
          <CurrencyDollarIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.evaluation.companiesBids", { companies: ranked.length, bids: bids.length }) }}
        </div>
        <div v-if="awarded" class="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700">
          <CheckCircleIcon class="h-3.5 w-3.5" />
          {{ $t("auctions.evaluation.awarded") }}
        </div>
      </div>
    </div>

    <div v-if="bids.length === 0" class="px-5 py-8 text-center text-sm text-slate-400">
      {{ $t("auctions.evaluation.noBids") }}
    </div>

    <div v-else class="divide-y divide-slate-50">
      <div
        v-for="(bid, idx) in ranked"
        :key="bid.bidId"
        class="px-5 py-3"
        :class="bid.isAwarded ? 'bg-emerald-50/60' : 'bg-white hover:bg-slate-50'"
      >
        <div class="flex items-center gap-3">
          <div class="w-7 shrink-0 text-center text-base leading-none">
            <CheckCircleIcon v-if="bid.isAwarded" class="mx-auto h-5 w-5 text-emerald-500" />
            <TrophyIcon v-else-if="idx === 0 && !awarded" class="mx-auto h-5 w-5 text-amber-400" />
            <span v-else class="text-xs font-medium text-slate-400">{{ idx + 1 }}</span>
          </div>

          <div class="min-w-0 flex-1">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <span class="truncate text-sm font-semibold" :class="bid.isAwarded ? 'text-emerald-700' : idx === 0 ? 'text-slate-800' : 'text-slate-600'">
                {{ bid.companyName }}
              </span>
              <div class="flex shrink-0 flex-wrap items-center gap-2 text-xs">
                <span class="font-mono font-semibold text-slate-800" :title="$t('auctions.evaluation.unitPrice')">
                  {{ formatMoney(bid.amount, bid.currencyCode) }}
                </span>
                <span v-if="idx === 0" class="rounded bg-emerald-100 px-1.5 py-0.5 font-medium text-emerald-700">{{ $t("auctions.evaluation.bestOffer") }}</span>
                <span v-else-if="deltaVsBest(bid)" class="rounded bg-slate-100 px-1.5 py-0.5 font-medium text-slate-500">{{ deltaVsBest(bid) }}</span>
                <span
                  v-if="deltaVsTarget(bid)"
                  class="rounded px-1.5 py-0.5 font-medium"
                  :class="deltaVsTarget(bid)!.good ? 'bg-emerald-50 text-emerald-600' : 'bg-rose-50 text-rose-600'"
                  :title="$t('auctions.evaluation.vsTarget')"
                >{{ deltaVsTarget(bid)!.text }}</span>
              </div>
            </div>
            <div class="mt-1.5 h-1.5 w-full rounded-full bg-slate-100">
              <div class="h-full rounded-full transition-all duration-500" :class="barColor(idx)" :style="{ width: barWidth(bid) }" />
            </div>
            <div class="mt-1 flex flex-wrap gap-x-3 text-[11px] text-slate-400">
              <span>{{ $t("auctions.evaluation.unitPrice") }}: {{ formatMoney(bid.amount, bid.currencyCode) }}</span>
              <span>{{ $t("auctions.evaluation.lineTotal") }}: {{ formatMoney(bid.amount * item.quantity, bid.currencyCode) }}</span>
              <span v-if="bid.currencyCode !== currencyCode">{{ $t("auctions.evaluation.normalized") }}: {{ formatMoney(bid.normalizedAmountInBaseCurrency) }}</span>
              <span>{{ formatDateTime(bid.bidTime) }}</span>
            </div>
          </div>

          <div class="shrink-0">
            <div v-if="bid.isAwarded" class="text-xs font-semibold text-emerald-600">{{ $t("auctions.evaluation.winner") }}</div>
            <button
              v-else-if="canAward"
              type="button"
              class="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 transition-colors hover:border-indigo-300 hover:bg-indigo-50 hover:text-indigo-700"
              @click="emit('award', bid)"
            >
              {{ $t("auctions.evaluation.selectWinner") }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
