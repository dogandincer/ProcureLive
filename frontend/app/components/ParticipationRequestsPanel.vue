<script setup lang="ts">
import type { ParticipationRequestDto } from "~/types/api";
import { ChevronDownIcon, ChevronRightIcon } from "@heroicons/vue/24/outline";

/**
 * Satınalmacı tarafı: davetsiz tedarikçilerin katılım talepleri. Adımlar: ① cariyi onayla (cari aktif + ihalenin
 * işyerinde görünür) ② ön teklifi kabul et (ihaleye dahil) — ya da gerekçeyle reddet. Her adımda iki tarafa bildirim gider.
 */
const props = defineProps<{ auctionId: string }>();
const emit = defineEmits<{ changed: [] }>();
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const requests = ref<ParticipationRequestDto[]>([]);
const loading = ref(true);
const busy = ref<string | null>(null);
const expanded = ref<string | null>(null);
const rejectFor = ref<string | null>(null);
const rejectReason = ref("");

const tone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Pending: "amber",
  CompanyApproved: "indigo",
  Accepted: "emerald",
  Rejected: "rose",
};
const openCount = computed(() => requests.value.filter((r) => r.status === "Pending" || r.status === "CompanyApproved").length);
const money = (n: number) => n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

async function load() {
  loading.value = true;
  try {
    requests.value = await api.get<ParticipationRequestDto[]>(`/api/auctions/${props.auctionId}/participation-requests`);
    if (!expanded.value) expanded.value = requests.value.find((r) => r.status === "Pending" || r.status === "CompanyApproved")?.id ?? null;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("participation.panel.loadError")));
  } finally {
    loading.value = false;
  }
}

async function act(r: ParticipationRequestDto, action: "approve-company" | "accept" | "reject") {
  busy.value = r.id;
  try {
    const body = action === "reject" ? { reason: rejectReason.value.trim() || null } : undefined;
    await api.post(`/api/auctions/${props.auctionId}/participation-requests/${r.id}/${action}`, body);
    toast.success(t(`participation.panel.done.${action}`, { company: r.companyName }));
    rejectFor.value = null;
    rejectReason.value = "";
    await load();
    emit("changed");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("participation.panel.actionError")));
  } finally {
    busy.value = null;
  }
}

// Tedarikçi yeni talep gönderdiğinde (sorumlu satınalmacıya bildirim düşer) panel kendini yeniler.
const lastNotification = useLastNotification();
watch(lastNotification, (n) => {
  if (n?.type === "ParticipationRequest" && n.relatedEntityId === props.auctionId) load();
});

onMounted(load);
defineExpose({ load });
</script>

<template>
  <AppCard>
    <div class="flex items-center justify-between">
      <h2 class="text-sm font-semibold text-slate-700">{{ $t("participation.panel.title") }}</h2>
      <AppBadge v-if="openCount" tone="amber">{{ $t("participation.panel.open", { n: openCount }) }}</AppBadge>
    </div>
    <p class="mt-0.5 text-xs text-slate-500">{{ $t("participation.panel.hint") }}</p>

    <LoadingSpinner v-if="loading" />
    <EmptyState v-else-if="requests.length === 0" class="mt-3">{{ $t("participation.panel.empty") }}</EmptyState>
    <ul v-else class="mt-3 space-y-2">
      <li v-for="r in requests" :key="r.id" class="rounded-lg border border-slate-200">
        <button
          type="button"
          class="flex w-full items-center justify-between gap-2 px-3 py-2 text-left"
          :aria-expanded="expanded === r.id"
          @click="expanded = expanded === r.id ? null : r.id"
        >
          <span class="flex min-w-0 items-center gap-1.5">
            <component :is="expanded === r.id ? ChevronDownIcon : ChevronRightIcon" class="h-3.5 w-3.5 shrink-0 text-slate-400" />
            <span class="min-w-0">
              <span class="block truncate text-sm font-medium text-slate-700">{{ r.companyName }}</span>
              <span class="block text-[11px] text-slate-400">
                {{ r.companyCode ?? "—" }} · {{ r.taxNumber }} · {{ formatDateTime(r.createdAt) }}
              </span>
            </span>
          </span>
          <span class="flex shrink-0 items-center gap-2">
            <span class="font-mono text-xs text-slate-600">{{ money(r.totalAmount) }} {{ r.currencyCode }}</span>
            <AppBadge :tone="tone[r.status]">{{ $t(`enums.participationStatus.${r.status}`) }}</AppBadge>
          </span>
        </button>

        <div v-if="expanded === r.id" class="space-y-3 border-t border-slate-100 px-3 py-3">
          <div class="flex flex-wrap gap-2 text-[11px]">
            <span class="rounded bg-slate-100 px-1.5 py-0.5 text-slate-600">{{ $t("participation.panel.companyStatus") }}: {{ $t(`enums.companyStatus.${r.companyStatus}`) }}</span>
            <span class="rounded px-1.5 py-0.5" :class="r.isCompanyApproved ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'">
              {{ r.isCompanyApproved ? $t("participation.panel.approvedInWorkspace") : $t("participation.panel.notApproved") }}
            </span>
            <NuxtLink :to="`/admin/companies/${r.companyId}`" class="text-indigo-600 hover:underline">{{ $t("participation.panel.viewCompany") }}</NuxtLink>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full min-w-[420px] text-xs">
              <thead>
                <tr class="text-left text-[11px] uppercase text-slate-400">
                  <th class="pb-1">{{ $t("participation.form.item") }}</th>
                  <th class="pb-1 text-right">{{ $t("participation.form.quantity") }}</th>
                  <th class="pb-1 text-right">{{ $t("participation.form.unitPrice") }}</th>
                  <th class="pb-1 text-right">{{ $t("participation.form.lineTotal") }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="l in r.lines" :key="l.auctionItemId" class="border-t border-slate-100 align-top">
                  <td class="py-1 text-slate-700">
                    {{ l.itemDescription }}
                    <span v-if="l.note" class="block text-[11px] text-slate-400">{{ l.note }}</span>
                    <span
                      v-if="l.appliedAsBid !== null"
                      class="mt-0.5 inline-block rounded px-1.5 py-0.5 text-[10px] font-medium"
                      :class="l.appliedAsBid ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'"
                    >
                      {{ l.appliedAsBid ? $t("participation.panel.applied") : $t("participation.panel.skipped") }}
                    </span>
                  </td>
                  <td class="py-1 text-right text-slate-500">{{ l.quantity }} {{ $t(`enums.unitOfMeasure.${l.unitOfMeasure}`) }}</td>
                  <td class="py-1 text-right font-mono">{{ money(l.unitPrice) }}</td>
                  <td class="py-1 text-right font-mono">{{ money(l.lineTotal) }}</td>
                </tr>
                <tr class="border-t border-slate-200 font-semibold">
                  <td class="py-1" colspan="3">{{ $t("participation.form.total") }}</td>
                  <td class="py-1 text-right font-mono">{{ money(r.totalAmount) }} {{ r.currencyCode }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <p v-if="r.note" class="rounded bg-slate-50 px-2 py-1.5 text-xs text-slate-600">“{{ r.note }}”</p>
          <p v-if="r.status === 'Accepted'" class="text-xs" :class="r.preOfferAppliedAt ? 'text-emerald-700' : 'text-indigo-700'">
            {{ r.preOfferAppliedAt ? $t("participation.panel.appliedAt", { date: formatDateTime(r.preOfferAppliedAt) }) : $t("participation.panel.willApply") }}
          </p>
          <p v-if="r.status === 'Rejected' && r.rejectReason" class="text-xs text-rose-600">{{ $t("participation.panel.rejectReason") }}: {{ r.rejectReason }}</p>

          <div v-if="r.status === 'Pending' || r.status === 'CompanyApproved'" class="flex flex-wrap gap-2">
            <AppButton v-if="r.status === 'Pending'" :loading="busy === r.id" @click="act(r, 'approve-company')">
              1 · {{ $t("participation.panel.approveCompany") }}
            </AppButton>
            <AppButton v-else :loading="busy === r.id" @click="act(r, 'accept')">
              2 · {{ $t("participation.panel.accept") }}
            </AppButton>
            <AppButton variant="danger-outline" :disabled="busy === r.id" @click="rejectFor = rejectFor === r.id ? null : r.id">
              {{ $t("participation.panel.reject") }}
            </AppButton>
          </div>
          <div v-if="rejectFor === r.id" class="flex flex-wrap gap-2">
            <input v-model="rejectReason" maxlength="500" :placeholder="$t('participation.panel.rejectPlaceholder')" class="min-w-0 flex-1 rounded-md border border-slate-300 px-2 py-1 text-sm" />
            <AppButton variant="danger" :loading="busy === r.id" @click="act(r, 'reject')">{{ $t("common.confirm") }}</AppButton>
          </div>
        </div>
      </li>
    </ul>
  </AppCard>
</template>
