<script setup lang="ts">
import type { AuctionDetailDto, ReserveStatus } from "~/types/api";
import { ShieldCheckIcon } from "@heroicons/vue/24/outline";

/**
 * Rezerv fiyat durumu (yalnızca iç kullanıcı): rezerv, ihale toplamı için alıcının iç sınırıdır. Sunucu, en iyi (ya da ödüllü)
 * tekliflerin birim fiyat × miktar toplamını rezervle karşılaştırır; karşılanmıyorsa kazanan seçimi ayrıca onay ister.
 */
const props = defineProps<{ auction: AuctionDetailDto }>();
const { formatMoney } = useDateFormat();

const status = computed<ReserveStatus | null>(() => props.auction.reserveStatus);
const visible = computed(() => props.auction.reservePrice != null && !!status.value && status.value !== "NotSet");

const tone: Record<string, "slate" | "amber" | "emerald" | "rose"> = { NoBids: "slate", Met: "emerald", NotMet: "rose", Incomplete: "amber" };
const frame: Record<string, string> = {
  NoBids: "border-slate-200 bg-surface",
  Met: "border-emerald-200 bg-emerald-50/50",
  NotMet: "border-rose-200 bg-rose-50/60",
  Incomplete: "border-amber-200 bg-amber-50/50",
};
</script>

<template>
  <div v-if="visible && status" class="rounded-xl border px-5 py-4 shadow-sm" :class="frame[status]" data-testid="reserve-note" :data-status="status">
    <div class="flex flex-wrap items-center gap-x-6 gap-y-2">
      <div class="flex items-center gap-2 text-sm font-semibold text-slate-800">
        <ShieldCheckIcon class="h-4 w-4 text-slate-500" />
        {{ $t("auctions.reserve.title") }}
        <AppBadge :tone="tone[status]" data-testid="reserve-status">{{ $t(`auctions.reserve.status.${status}`) }}</AppBadge>
      </div>
      <dl class="flex flex-wrap gap-x-6 gap-y-1 text-sm">
        <div class="flex items-baseline gap-1.5">
          <dt class="text-xs text-slate-500">{{ $t("auctions.reserve.reserveLabel") }}</dt>
          <dd class="font-mono font-semibold text-slate-800">{{ formatMoney(auction.reservePrice, auction.currencyCode) }}</dd>
        </div>
        <div v-if="auction.reserveTotal != null" class="flex items-baseline gap-1.5">
          <dt class="text-xs text-slate-500">{{ $t("auctions.reserve.total") }}</dt>
          <dd class="font-mono font-semibold text-slate-800" data-testid="reserve-total">{{ formatMoney(auction.reserveTotal, auction.currencyCode) }}</dd>
        </div>
      </dl>
    </div>
    <p class="mt-1.5 text-xs text-slate-600">
      {{ $t(`auctions.reserve.explain.${status}`) }}
      <span v-if="status !== 'NoBids'" class="text-slate-400">· {{ $t("auctions.reserve.coverage", { covered: auction.reserveCoveredItems, total: auction.items.length }) }}</span>
    </p>
  </div>
</template>
