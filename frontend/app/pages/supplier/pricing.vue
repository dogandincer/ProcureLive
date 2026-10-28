<script setup lang="ts">
import type { PurchaseResultDto, SeatBand, SeatBandPriceDto, SubscriptionPlansDto } from "~/types/api";
import { CheckCircleIcon, CheckIcon, MinusIcon, SparklesIcon, XMarkIcon } from "@heroicons/vue/24/solid";

/**
 * Paketler / Upgrade. Ödeme DEMO'dur: kart numarası yalnızca bu bileşenin belleğinde durur;
 * sunucuya sadece kart sahibi + son 4 hane gider (mock ödeme servisi "0002" ile biten kartı reddeder).
 */
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();
const { subscription, isPlus, refresh: refreshSubscription } = useSubscription();

const plans = ref<SubscriptionPlansDto | null>(null);
const loading = ref(true);

const checkoutBand = ref<SeatBandPriceDto | null>(null);
const card = reactive({ holder: "", number: "4242 4242 4242 4242", expiry: "12/29" });
const paying = ref(false);
const paymentError = ref("");
const purchase = ref<PurchaseResultDto | null>(null);

const usd = (n: number) => new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(n);

const userCount = computed(() => subscription.value?.userCount ?? 0);
const recommendedBand = computed<SeatBand | null>(() => {
  const fit = plans.value?.bands.find((b) => b.maxUsers === null || userCount.value <= b.maxUsers);
  return fit?.band ?? null;
});

function bandLabel(b: SeatBandPriceDto) {
  return b.maxUsers === null ? t("pricing.bandOpen", { min: b.minUsers }) : t("pricing.bandRange", { min: b.minUsers, max: b.maxUsers });
}

function tooSmall(b: SeatBandPriceDto) {
  return b.maxUsers !== null && userCount.value > b.maxUsers;
}

function isCurrent(b: SeatBandPriceDto) {
  return isPlus.value && subscription.value?.seatBand === b.band;
}

const cardDigits = computed(() => card.number.replace(/\D/g, ""));
const cardValid = computed(() => cardDigits.value.length >= 12 && cardDigits.value.length <= 19 && /^\d{2}\/\d{2}$/.test(card.expiry) && card.holder.trim().length > 1);

function formatCardNumber() {
  card.number = cardDigits.value.slice(0, 19).replace(/(\d{4})(?=\d)/g, "$1 ");
}

function openCheckout(b: SeatBandPriceDto) {
  checkoutBand.value = b;
  paymentError.value = "";
  purchase.value = null;
}

function closeCheckout() {
  if (paying.value) return;
  checkoutBand.value = null;
}

async function pay() {
  if (!checkoutBand.value || !cardValid.value) return;
  paying.value = true;
  paymentError.value = "";
  try {
    const result = await api.post<PurchaseResultDto>("/api/subscription/purchase", {
      seatBand: checkoutBand.value.band,
      cardHolder: card.holder.trim(),
      cardLast4: cardDigits.value.slice(-4),
    });
    if (result.succeeded) {
      purchase.value = result;
      checkoutBand.value = null;
      await refreshSubscription();
    } else {
      paymentError.value = t("pricing.checkout.declined", { reason: result.failureReason ?? "" });
    }
  } catch (error: any) {
    paymentError.value = apiErrorMessage(error, t("pricing.checkout.error"));
  } finally {
    paying.value = false;
  }
}

onMounted(async () => {
  try {
    [plans.value] = await Promise.all([api.get<SubscriptionPlansDto>("/api/subscription/plans"), refreshSubscription()]);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("pricing.loadError")));
  } finally {
    loading.value = false;
  }
});

const features = computed(() => [
  { key: "visibility", standard: t("pricing.features.visibility.standard"), plus: t("pricing.features.visibility.plus") },
  { key: "quota", standard: t("pricing.features.quota.standard", { n: plans.value?.standardJoinLimit ?? 2 }), plus: t("pricing.features.quota.plus") },
  { key: "search", standard: null, plus: t("pricing.features.search.plus") },
  { key: "filters", standard: null, plus: t("pricing.features.filters.plus") },
  { key: "restricted", standard: t("pricing.features.restricted.standard"), plus: t("pricing.features.restricted.plus") },
]);
</script>

<template>
  <div class="space-y-8">
    <!-- Satın alım başarılı -->
    <div v-if="purchase" class="overflow-hidden rounded-2xl border border-emerald-200 bg-surface shadow-sm">
      <div class="bg-gradient-to-r from-emerald-500 to-teal-500 px-6 py-8 text-center text-white">
        <CheckCircleIcon class="mx-auto h-14 w-14" />
        <h1 class="mt-3 text-2xl font-bold">{{ $t("pricing.success.title") }}</h1>
        <p class="mt-1 text-sm text-emerald-50">{{ $t("pricing.success.body") }}</p>
      </div>
      <dl class="grid gap-4 px-6 py-6 text-sm sm:grid-cols-4">
        <div>
          <dt class="text-xs uppercase text-slate-400">{{ $t("pricing.success.plan") }}</dt>
          <dd class="mt-1 font-semibold text-slate-800">Plus · {{ $t(`pricing.bands.${purchase.seatBand}`) }}</dd>
        </div>
        <div>
          <dt class="text-xs uppercase text-slate-400">{{ $t("pricing.success.amount") }}</dt>
          <dd class="mt-1 font-semibold text-slate-800">{{ usd(purchase.priceUsd ?? 0) }}</dd>
        </div>
        <div>
          <dt class="text-xs uppercase text-slate-400">{{ $t("pricing.success.validUntil") }}</dt>
          <dd class="mt-1 font-semibold text-slate-800">{{ purchase.expiresAt ? formatDate(purchase.expiresAt) : "-" }}</dd>
        </div>
        <div>
          <dt class="text-xs uppercase text-slate-400">{{ $t("pricing.success.reference") }}</dt>
          <dd class="mt-1 font-mono text-xs font-semibold text-slate-800">{{ purchase.paymentReference }}</dd>
        </div>
      </dl>
      <div class="flex flex-wrap justify-center gap-3 border-t border-slate-100 px-6 py-5">
        <NuxtLink to="/supplier/search" class="inline-flex items-center gap-1.5 rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
          <SparklesIcon class="h-4 w-4" /> {{ $t("pricing.success.toSearch") }}
        </NuxtLink>
        <NuxtLink to="/supplier/auctions" class="inline-flex items-center rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">
          {{ $t("pricing.success.toAuctions") }}
        </NuxtLink>
      </div>
    </div>

    <template v-else>
      <div class="text-center">
        <h1 class="text-2xl font-bold text-slate-800">{{ $t("pricing.title") }}</h1>
        <p class="mx-auto mt-2 max-w-2xl text-sm text-slate-500">{{ $t("pricing.subtitle") }}</p>
      </div>

      <LoadingSpinner v-if="loading" />

      <template v-else-if="plans">
        <!-- Mevcut plan -->
        <div v-if="subscription" class="mx-auto flex max-w-3xl flex-wrap items-center justify-between gap-3 rounded-xl border border-slate-200 bg-surface p-4 text-sm">
          <div>
            <span class="text-slate-500">{{ $t("pricing.currentPlan") }}:</span>
            <span class="ml-1 font-semibold" :class="isPlus ? 'text-amber-600' : 'text-slate-800'">
              {{ isPlus ? `Plus · ${$t(`pricing.bands.${subscription.seatBand}`)}` : $t("pricing.standard") }}
            </span>
            <span v-if="isPlus && subscription.expiresAt" class="ml-2 text-xs text-slate-400">
              ({{ $t("pricing.validUntil", { date: formatDate(subscription.expiresAt) }) }})
            </span>
          </div>
          <div class="text-xs text-slate-500">
            {{ $t("pricing.usersInCompany", { n: subscription.userCount }) }}
            <template v-if="!isPlus"> · {{ $t("subscription.quotaUsage", { used: subscription.joinedCount, limit: subscription.quotaLimit }) }}</template>
          </div>
        </div>

        <!-- Standart vs Plus karşılaştırma -->
        <div class="mx-auto max-w-3xl overflow-x-auto rounded-xl border border-slate-200 bg-surface">
          <table class="w-full min-w-[34rem] text-sm">
            <thead>
              <tr class="border-b border-slate-100 bg-slate-50 text-left text-xs uppercase text-slate-500">
                <th class="px-4 py-3 font-semibold">{{ $t("pricing.compare.feature") }}</th>
                <th class="px-4 py-3 font-semibold">{{ $t("pricing.standard") }}</th>
                <th class="px-4 py-3 font-semibold text-amber-600">Plus</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="f in features" :key="f.key" class="border-b border-slate-50 last:border-0">
                <td class="px-4 py-3 font-medium text-slate-700">{{ $t(`pricing.features.${f.key}.label`) }}</td>
                <td class="px-4 py-3 text-slate-500">
                  <span v-if="f.standard">{{ f.standard }}</span>
                  <MinusIcon v-else class="h-4 w-4 text-slate-300" />
                </td>
                <td class="px-4 py-3 text-slate-700">
                  <span class="inline-flex items-start gap-1.5"><CheckIcon class="mt-0.5 h-4 w-4 shrink-0 text-emerald-500" />{{ f.plus }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Plus paketleri (kullanıcı sayısı bazlı) -->
        <div class="grid gap-5 md:grid-cols-3">
          <div
            v-for="b in plans.bands"
            :key="b.band"
            class="relative flex flex-col rounded-2xl border bg-surface p-6 shadow-sm transition-shadow hover:shadow-md"
            :class="b.band === recommendedBand && !isCurrent(b) ? 'border-amber-300 ring-2 ring-amber-200' : 'border-slate-200'"
          >
            <span
              v-if="b.band === recommendedBand && !isCurrent(b)"
              class="absolute -top-3 left-1/2 -translate-x-1/2 rounded-full bg-amber-500 px-3 py-0.5 text-[11px] font-semibold uppercase tracking-wide text-white"
            >
              {{ $t("pricing.recommended") }}
            </span>
            <div class="flex items-center gap-2 text-sm font-semibold text-amber-600">
              <SparklesIcon class="h-4 w-4" /> Plus
            </div>
            <div class="mt-2 text-lg font-bold text-slate-800">{{ bandLabel(b) }}</div>
            <div class="mt-4 flex items-baseline gap-1">
              <span class="text-4xl font-extrabold text-slate-900">{{ usd(b.priceUsd) }}</span>
              <span class="text-sm text-slate-400">/ {{ $t("pricing.perYear", { months: plans.durationMonths }) }}</span>
            </div>
            <ul class="mt-5 flex-1 space-y-2 text-sm text-slate-600">
              <li class="flex gap-2"><CheckIcon class="h-4 w-4 shrink-0 text-emerald-500" />{{ $t("pricing.features.quota.plus") }}</li>
              <li class="flex gap-2"><CheckIcon class="h-4 w-4 shrink-0 text-emerald-500" />{{ $t("pricing.features.search.plus") }}</li>
              <li class="flex gap-2"><CheckIcon class="h-4 w-4 shrink-0 text-emerald-500" />{{ $t("pricing.seats", { n: b.maxUsers ?? `${b.minUsers}+` }) }}</li>
            </ul>
            <AppButton
              class="mt-6 w-full"
              :variant="b.band === recommendedBand ? 'primary' : 'secondary'"
              :disabled="isCurrent(b) || tooSmall(b)"
              @click="openCheckout(b)"
            >
              {{ isCurrent(b) ? $t("pricing.currentBadge") : tooSmall(b) ? $t("pricing.tooSmall") : isPlus ? $t("pricing.switchCta") : $t("pricing.buyCta") }}
            </AppButton>
          </div>
        </div>

        <p class="text-center text-xs text-slate-400">{{ $t("pricing.demoNote") }}</p>
      </template>
    </template>

    <!-- Demo ödeme penceresi (body'ye taşınır: sayfa içi stacking context'i başlığın altında kalmasın) -->
    <Teleport to="body">
      <div v-if="checkoutBand" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm" @click.self="closeCheckout">
        <form class="w-full max-w-md overflow-hidden rounded-2xl bg-surface shadow-2xl" @submit.prevent="pay">
          <div class="flex items-center justify-between border-b border-slate-100 px-6 py-4">
            <div>
              <h2 class="text-base font-semibold text-slate-800">{{ $t("pricing.checkout.title") }}</h2>
              <p class="text-xs text-slate-500">Plus · {{ bandLabel(checkoutBand) }}</p>
            </div>
            <button type="button" class="rounded p-1 text-slate-400 hover:bg-slate-100" :disabled="paying" @click="closeCheckout">
              <XMarkIcon class="h-5 w-5" />
            </button>
          </div>

          <div class="space-y-4 px-6 py-5">
            <div class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800">
              {{ $t("pricing.checkout.demoWarning") }}
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("pricing.checkout.holder") }}</label>
              <input v-model="card.holder" required maxlength="100" autocomplete="off" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("pricing.checkout.number") }}</label>
              <input
                v-model="card.number"
                inputmode="numeric"
                autocomplete="off"
                class="w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-sm tracking-wider focus:border-indigo-500 focus:outline-none"
                @input="formatCardNumber"
              />
              <p class="mt-1 text-[11px] text-slate-400">{{ $t("pricing.checkout.testCards") }}</p>
            </div>
            <div class="w-28">
              <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("pricing.checkout.expiry") }}</label>
              <input v-model="card.expiry" placeholder="AA/YY" maxlength="5" autocomplete="off" class="w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-sm focus:border-indigo-500 focus:outline-none" />
            </div>

            <div class="flex items-center justify-between rounded-lg bg-slate-50 px-4 py-3">
              <span class="text-sm text-slate-600">{{ $t("pricing.checkout.total") }}</span>
              <span class="text-lg font-bold text-slate-900">{{ usd(checkoutBand.priceUsd) }}</span>
            </div>

            <p v-if="paymentError" class="rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">{{ paymentError }}</p>
          </div>

          <div class="flex justify-end gap-2 border-t border-slate-100 px-6 py-4">
            <AppButton variant="secondary" :disabled="paying" @click="closeCheckout">{{ $t("common.cancel") }}</AppButton>
            <AppButton type="submit" :loading="paying" :disabled="!cardValid">
              {{ paying ? $t("pricing.checkout.processing") : $t("pricing.checkout.pay", { amount: usd(checkoutBand.priceUsd) }) }}
            </AppButton>
          </div>
        </form>
      </div>
    </Teleport>
  </div>
</template>
