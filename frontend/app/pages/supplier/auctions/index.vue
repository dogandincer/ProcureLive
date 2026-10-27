<script setup lang="ts">
import type { AuctionDto, PagedResult } from "~/types/api";
import { LockClosedIcon, SparklesIcon } from "@heroicons/vue/24/solid";

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();
const { subscription, isPlus, quotaReached, refresh: refreshSubscription } = useSubscription();

const auctions = ref<AuctionDto[]>([]);
const declineReason = reactive<Record<string, string>>({});
const showDeclineFor = ref<string | null>(null);
const joining = reactive<Record<string, boolean>>({});
const loading = ref(true);

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Scheduled: "amber",
  Live: "emerald",
  Paused: "amber",
  Ended: "slate",
  Awarded: "indigo",
  Cancelled: "rose",
};

const isOpen = (a: AuctionDto) => a.status === "Scheduled" || a.status === "Live";
const isRestricted = (a: AuctionDto) => a.accessLevel !== "Full";
// Davetsiz görünen açık ihale (davet eden firmanın diğer ihaleleri / Plus vitrini) → doğrudan katılım.
const canJoin = (a: AuctionDto) => !isRestricted(a) && !a.isJoined && !a.myInvitationStatus && isOpen(a);

async function load() {
  loading.value = true;
  try {
    // Davet durumum artık liste satırında geliyor (myInvitationStatus) — satır başına detay isteği yok.
    const page = await api.get<PagedResult<AuctionDto>>("/api/auctions", { page: 1, pageSize: 50 });
    auctions.value = page.items;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.supplierList.loadError")));
  } finally {
    loading.value = false;
  }
}

function handleQuotaError(error: any): boolean {
  if (apiErrorCode(error) !== "QuotaExceeded") return false;
  toast.error(t("subscription.quotaReachedToast"));
  refreshSubscription();
  load();
  return true;
}

async function accept(auctionId: string) {
  try {
    await api.post(`/api/auctions/${auctionId}/invitations/respond`, { accept: true, declineReason: null });
    toast.success(t("auctions.supplierList.acceptedToast"));
    await Promise.all([load(), refreshSubscription()]);
  } catch (error: any) {
    if (!handleQuotaError(error)) toast.error(apiErrorMessage(error, t("auctions.supplierList.actionError")));
  }
}

async function join(auctionId: string) {
  joining[auctionId] = true;
  try {
    await api.post(`/api/auctions/${auctionId}/join`);
    toast.success(t("auctions.supplierList.joinedToast"));
    await Promise.all([load(), refreshSubscription()]);
  } catch (error: any) {
    if (!handleQuotaError(error)) toast.error(apiErrorMessage(error, t("auctions.supplierList.actionError")));
  } finally {
    joining[auctionId] = false;
  }
}

async function decline(auctionId: string) {
  try {
    await api.post(`/api/auctions/${auctionId}/invitations/respond`, {
      accept: false,
      declineReason: declineReason[auctionId] || t("common.notSpecified"),
    });
    showDeclineFor.value = null;
    // Declining revokes list visibility for this auction (server-side); update locally instead of
    // reloading, so the row doesn't just vanish without explanation.
    const row = auctions.value.find((a) => a.id === auctionId);
    if (row) row.myInvitationStatus = "Declined";
    toast.success(t("auctions.supplierList.declinedToast"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.supplierList.actionError")));
  }
}

onMounted(() => Promise.all([load(), refreshSubscription()]));
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("auctions.supplierList.title") }}</h1>
      <NuxtLink v-if="isPlus" to="/supplier/search" class="text-sm font-semibold text-indigo-600 hover:underline">
        {{ $t("auctions.supplierList.discoverPlus") }} →
      </NuxtLink>
    </div>

    <!-- Kota bandı (Standart) / Plus rozeti -->
    <div
      v-if="subscription"
      class="flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4"
      :class="isPlus ? 'border-amber-200 bg-amber-50' : quotaReached ? 'border-rose-200 bg-rose-50' : 'border-indigo-100 bg-indigo-50'"
    >
      <div class="flex items-center gap-3">
        <SparklesIcon v-if="isPlus" class="h-5 w-5 text-amber-500" />
        <div>
          <div class="text-sm font-semibold" :class="isPlus ? 'text-amber-800' : quotaReached ? 'text-rose-800' : 'text-indigo-800'">
            {{ isPlus ? $t("subscription.plusActive") : quotaReached ? $t("subscription.quotaFullTitle") : $t("subscription.standardTitle") }}
          </div>
          <div class="text-xs" :class="isPlus ? 'text-amber-700' : quotaReached ? 'text-rose-700' : 'text-indigo-700'">
            <template v-if="isPlus">{{ $t("subscription.plusUnlimited") }}</template>
            <template v-else>
              {{ $t("subscription.quotaUsage", { used: subscription.joinedCount, limit: subscription.quotaLimit }) }}
              · {{ quotaReached ? $t("subscription.quotaFullBody") : $t("subscription.quotaRemaining", { n: subscription.remainingJoins }) }}
            </template>
          </div>
        </div>
      </div>
      <div v-if="!isPlus" class="flex items-center gap-3">
        <div class="hidden h-2 w-32 overflow-hidden rounded-full bg-white sm:block">
          <div
            class="h-full rounded-full"
            :class="quotaReached ? 'bg-rose-500' : 'bg-indigo-500'"
            :style="{ width: `${Math.min(100, (subscription.joinedCount / subscription.quotaLimit) * 100)}%` }"
          />
        </div>
        <NuxtLink to="/supplier/pricing" class="inline-flex items-center gap-1 rounded-md bg-amber-500 px-3 py-1.5 text-xs font-semibold text-white hover:bg-amber-600">
          <SparklesIcon class="h-3.5 w-3.5" /> {{ $t("restricted.upgradeCta") }}
        </NuxtLink>
      </div>
    </div>

    <AppCard>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("auctions.supplierList.table.title") }}</th>
              <th class="pb-2">{{ $t("auctions.supplierList.table.owner") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2">{{ $t("auctions.supplierList.table.myInviteStatus") }}</th>
              <th class="pb-2">{{ $t("auctions.supplierList.table.endsAt") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in auctions" :key="a.id" class="border-b border-slate-50" :class="{ 'bg-slate-50/60': isRestricted(a) }">
              <td class="py-2 font-medium text-slate-700">
                <div class="flex items-center gap-2">
                  <LockClosedIcon v-if="isRestricted(a)" class="h-3.5 w-3.5 shrink-0 text-amber-500" />
                  <span :class="{ 'select-none blur-[3px]': a.accessLevel === 'NoAccess' }">{{ a.title }}</span>
                  <AppBadge v-if="a.isListedForPlus && !a.myInvitationStatus" tone="amber">Plus</AppBadge>
                </div>
              </td>
              <td class="py-2 text-slate-500">{{ a.ownerCompanyName }}</td>
              <td class="py-2"><AppBadge :tone="statusTone[a.status]">{{ $t(`enums.auctionStatus.${a.status}`) }}</AppBadge></td>
              <td class="py-2">
                <span class="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">
                  {{ a.myInvitationStatus ? $t(`enums.invitationStatus.${a.myInvitationStatus}`) : a.isJoined ? $t("auctions.supplierList.joined") : "-" }}
                </span>
              </td>
              <td class="py-2 text-slate-500">{{ formatDateTime(a.endAt) }}</td>
              <td class="py-2 text-right">
                <!-- Kota dolu: sunucu bu satırda açıklama/rezerv/katılımcı bilgisini zaten göndermedi -->
                <div v-if="isRestricted(a)" class="flex items-center justify-end gap-2">
                  <span class="text-xs text-slate-400">{{ $t(`restricted.${a.accessLevel}.short`) }}</span>
                  <NuxtLink to="/supplier/pricing" class="rounded bg-amber-500 px-2 py-1 text-xs font-semibold text-white hover:bg-amber-600">Plus</NuxtLink>
                  <NuxtLink :to="`/supplier/auctions/${a.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink>
                </div>

                <template v-else>
                  <div v-if="a.myInvitationStatus === 'Invited'" class="flex justify-end gap-2">
                    <button class="rounded bg-emerald-600 px-2 py-1 text-xs font-semibold text-white" @click="accept(a.id)">{{ $t("common.accept") }}</button>
                    <button class="rounded bg-rose-600 px-2 py-1 text-xs font-semibold text-white" @click="showDeclineFor = a.id">{{ $t("common.decline") }}</button>
                  </div>
                  <button
                    v-else-if="canJoin(a)"
                    class="rounded bg-indigo-600 px-2 py-1 text-xs font-semibold text-white disabled:opacity-50"
                    :disabled="joining[a.id]"
                    @click="join(a.id)"
                  >
                    {{ $t("auctions.supplierList.join") }}
                  </button>
                  <span v-if="a.myInvitationStatus === 'Declined'" class="text-xs text-slate-400">{{ $t("auctions.supplierList.declined") }}</span>
                  <NuxtLink v-else :to="`/supplier/auctions/${a.id}`" class="ml-2 text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink>

                  <div v-if="showDeclineFor === a.id" class="mt-2 flex justify-end gap-2">
                    <input v-model="declineReason[a.id]" :placeholder="$t('common.declineReasonPlaceholder')" class="w-40 rounded-md border border-slate-300 px-2 py-1 text-xs" />
                    <button class="rounded bg-rose-600 px-2 py-1 text-xs font-semibold text-white" @click="decline(a.id)">{{ $t("common.confirm") }}</button>
                  </div>
                </template>
              </td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="auctions.length === 0">{{ $t("auctions.supplierList.empty") }}</EmptyState>
      </div>
    </AppCard>
  </div>
</template>
