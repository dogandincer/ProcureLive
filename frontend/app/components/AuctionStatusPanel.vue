<script setup lang="ts">
import type { Component } from "vue";
import type { AuctionStatusPanelDto, StatusTransitionOptionDto } from "~/types/api";
import {
  RocketLaunchIcon,
  PlayIcon,
  PauseIcon,
  StopIcon,
  XCircleIcon,
  ArrowUturnLeftIcon,
  ArrowPathIcon,
  ArrowRightIcon,
  ClockIcon,
  CpuChipIcon,
  UserIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";

/**
 * İhale durum yönetimi: yaşam döngüsü, sunucunun izin verdiği geçişler (AuctionStatusMachine), onay penceresi ve
 * durum geçmişi. Geçiş kuralları istemcide tekrarlanmaz — butonlar yalnızca panel uç noktasının döndürdüğü geçişlerdir.
 */
const props = defineProps<{ auctionId: string; status: string; canManage: boolean }>();
const emit = defineEmits<{ changed: [] }>();

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const panel = ref<AuctionStatusPanelDto | null>(null);
const dialog = ref<StatusTransitionOptionDto | null>(null);
const form = reactive({ reason: "", startAt: "", endAt: "" });
const endDirty = ref(false);
const dialogError = ref("");
const saving = ref(false);
const extendMinutes = ref(5);
const extending = ref(false);
const showAllHistory = ref(false);

type ActionKey = "publish" | "startNow" | "resume" | "reopen" | "pause" | "endNow" | "unpublish" | "cancel";

const actionStyle: Record<ActionKey, { icon: Component; variant: "primary" | "secondary" | "danger" | "success" }> = {
  publish: { icon: RocketLaunchIcon, variant: "primary" },
  startNow: { icon: PlayIcon, variant: "success" },
  resume: { icon: PlayIcon, variant: "success" },
  reopen: { icon: ArrowPathIcon, variant: "secondary" },
  pause: { icon: PauseIcon, variant: "secondary" },
  endNow: { icon: StopIcon, variant: "secondary" },
  unpublish: { icon: ArrowUturnLeftIcon, variant: "secondary" },
  cancel: { icon: XCircleIcon, variant: "danger" },
};

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Scheduled: "amber",
  Live: "emerald",
  Paused: "amber",
  Ended: "slate",
  Awarded: "indigo",
  Cancelled: "rose",
};

// Ana yaşam döngüsü; Duraklatıldı "Canlı" adımında, İptal ayrı bir son durum olarak gösterilir.
const pipeline = ["Draft", "Scheduled", "Live", "Ended", "Awarded"];
const pipelineIndex = computed(() => {
  const s = panel.value?.status ?? props.status;
  return s === "Paused" ? 2 : pipeline.indexOf(s);
});

function actionKey(o: StatusTransitionOptionDto): ActionKey {
  const from = panel.value?.status;
  if (o.to === "Live") return from === "Scheduled" ? "startNow" : from === "Paused" ? "resume" : "reopen";
  const map: Record<string, ActionKey> = { Scheduled: "publish", Draft: "unpublish", Paused: "pause", Ended: "endNow", Cancelled: "cancel" };
  return map[o.to] ?? "publish";
}

const dialogKey = computed(() => (dialog.value ? actionKey(dialog.value) : null));

const pad = (n: number) => String(n).padStart(2, "0");
const toLocalInput = (ms: number) => {
  const d = new Date(ms);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
};
const toIso = (v: string) => (v ? new Date(v).toISOString() : null);

async function load() {
  try {
    panel.value = await api.get<AuctionStatusPanelDto>(`/api/auctions/${props.auctionId}/status`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.status.loadError")));
  }
}

function openDialog(o: StatusTransitionOptionDto) {
  const p = panel.value!;
  const now = Date.now();
  const key = actionKey(o);
  dialog.value = o;
  dialogError.value = "";
  form.reason = "";
  endDirty.value = false;

  const startMs = o.allowsStartAt ? Math.max(new Date(p.startAt).getTime(), now + 10 * 60_000) : now;
  form.startAt = o.allowsStartAt ? toLocalInput(startMs) : "";

  let endMs = new Date(p.endAt).getTime();
  if (key === "reopen") endMs = now + 30 * 60_000;
  else if (key === "resume" && p.pausedAt) endMs += now - new Date(p.pausedAt).getTime();
  else if (key === "publish" && endMs <= startMs) endMs = startMs + 60 * 60_000;
  else if (key === "startNow" && endMs <= now + 60_000) endMs = now + 60 * 60_000;
  form.endAt = o.allowsEndAt || o.requiresEndAt ? toLocalInput(endMs) : "";
}

const canConfirm = computed(() => {
  if (!dialog.value) return false;
  if (dialog.value.requiresReason && form.reason.trim().length < 3) return false;
  if (dialog.value.requiresEndAt && !form.endAt) return false;
  return true;
});

async function confirm() {
  if (!dialog.value || !canConfirm.value) return;
  const o = dialog.value;
  const key = actionKey(o);
  saving.value = true;
  dialogError.value = "";
  try {
    // Devam ettirmede varsayılan bitişi sunucu hesaplar (duraklama süresi eklenir); yalnızca elle değiştirildiyse gönder.
    const sendEnd = o.requiresEndAt || (o.allowsEndAt && (key !== "resume" || endDirty.value));
    await api.post(`/api/auctions/${props.auctionId}/status`, {
      status: o.to,
      reason: form.reason.trim() || null,
      newStartAt: o.allowsStartAt ? toIso(form.startAt) : null,
      newEndAt: sendEnd ? toIso(form.endAt) : null,
    });
    toast.success(t(`auctions.status.toast.${key}`));
    dialog.value = null;
    await load();
    emit("changed");
  } catch (error: any) {
    dialogError.value = apiErrorMessage(error, t("auctions.status.saveError"));
  } finally {
    saving.value = false;
  }
}

async function extend() {
  if (!panel.value || extendMinutes.value < 1) return;
  extending.value = true;
  try {
    const newEndAt = new Date(new Date(panel.value.endAt).getTime() + extendMinutes.value * 60_000).toISOString();
    await api.post(`/api/auctions/${props.auctionId}/extend`, {
      newEndAt,
      reason: t("auctions.detail.extendReason", { minutes: extendMinutes.value }),
    });
    toast.success(t("auctions.status.extendedToast", { minutes: extendMinutes.value }));
    await load();
    emit("changed");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.status.saveError")));
  } finally {
    extending.value = false;
  }
}

const visibleHistory = computed(() => {
  const h = panel.value?.history ?? [];
  return showAllHistory.value ? h : h.slice(0, 5);
});

// Durum dışarıdan değişirse (zamanlayıcı, başka admin, SignalR yenilemesi) panel de tazelenir.
watch(() => props.status, () => load());
onMounted(load);
</script>

<template>
  <AppCard :title="$t('auctions.status.title')" :description="$t('auctions.status.description')">
    <LoadingSpinner v-if="!panel" />

    <div v-else class="space-y-5">
      <!-- Yaşam döngüsü -->
      <div class="flex flex-wrap items-center gap-3">
        <ol class="flex flex-1 items-center">
          <li v-for="(s, i) in pipeline" :key="s" class="flex flex-1 items-center last:flex-none">
            <div class="flex flex-col items-center gap-1">
              <span
                class="flex h-7 w-7 items-center justify-center rounded-full border-2 text-xs font-bold"
                :class="
                  panel.status === 'Cancelled'
                    ? 'border-slate-200 bg-slate-50 text-slate-300'
                    : i < pipelineIndex
                      ? 'border-indigo-500 bg-indigo-500 text-white'
                      : i === pipelineIndex
                        ? panel.status === 'Paused'
                          ? 'border-amber-500 bg-amber-50 text-amber-600'
                          : 'border-indigo-500 bg-surface text-indigo-600 ring-4 ring-indigo-100'
                        : 'border-slate-200 bg-surface text-slate-300'
                "
              >
                <PauseIcon v-if="i === pipelineIndex && panel.status === 'Paused'" class="h-3.5 w-3.5" />
                <template v-else>{{ i + 1 }}</template>
              </span>
              <span class="whitespace-nowrap text-[11px] font-medium" :class="i === pipelineIndex ? 'text-slate-800' : 'text-slate-400'">
                {{ $t(`enums.auctionStatus.${i === pipelineIndex && panel.status === 'Paused' ? 'Paused' : s}`) }}
              </span>
            </div>
            <div v-if="i < pipeline.length - 1" class="mx-1 mb-5 h-0.5 flex-1" :class="i < pipelineIndex && panel.status !== 'Cancelled' ? 'bg-indigo-500' : 'bg-slate-200'" />
          </li>
        </ol>
        <AppBadge v-if="panel.status === 'Cancelled'" tone="rose">{{ $t("enums.auctionStatus.Cancelled") }}</AppBadge>
      </div>

      <!-- Zamanlar -->
      <dl class="grid grid-cols-2 gap-3 text-xs sm:grid-cols-4">
        <div class="rounded-lg bg-slate-50 px-3 py-2">
          <dt class="text-slate-400">{{ $t("auctions.status.plannedStart") }}</dt>
          <dd class="mt-0.5 font-medium text-slate-700">{{ formatDateTime(panel.startAt) }}</dd>
        </div>
        <div class="rounded-lg bg-slate-50 px-3 py-2">
          <dt class="text-slate-400">{{ $t("auctions.status.actualStart") }}</dt>
          <dd class="mt-0.5 font-medium text-slate-700">{{ panel.actualStartAt ? formatDateTime(panel.actualStartAt) : "—" }}</dd>
        </div>
        <div class="rounded-lg bg-slate-50 px-3 py-2">
          <dt class="text-slate-400">{{ panel.actualEndAt ? $t("auctions.status.actualEnd") : $t("auctions.status.end") }}</dt>
          <dd class="mt-0.5 font-medium text-slate-700">{{ formatDateTime(panel.actualEndAt ?? panel.endAt) }}</dd>
        </div>
        <div class="rounded-lg px-3 py-2" :class="panel.pausedAt ? 'bg-amber-50' : 'bg-slate-50'">
          <dt :class="panel.pausedAt ? 'text-amber-600' : 'text-slate-400'">{{ $t("auctions.status.pausedSince") }}</dt>
          <dd class="mt-0.5 font-medium" :class="panel.pausedAt ? 'text-amber-700' : 'text-slate-700'">{{ panel.pausedAt ? formatDateTime(panel.pausedAt) : "—" }}</dd>
        </div>
      </dl>

      <!-- Geçişler -->
      <div v-if="panel.status === 'Cancelled' || panel.status === 'Awarded'" class="rounded-lg px-4 py-3 text-sm" :class="panel.status === 'Cancelled' ? 'bg-rose-50 text-rose-700' : 'bg-indigo-50 text-indigo-700'">
        {{ panel.status === "Cancelled" ? $t("auctions.status.cancelledBanner") : $t("auctions.status.awardedBanner") }}
      </div>
      <div v-else-if="canManage" class="flex flex-wrap items-center gap-2">
        <AppButton
          v-for="o in panel.transitions"
          :key="o.to"
          :variant="actionStyle[actionKey(o)].variant"
          @click="openDialog(o)"
        >
          <component :is="actionStyle[actionKey(o)].icon" class="h-4 w-4" />
          {{ $t(`auctions.status.actions.${actionKey(o)}`) }}
        </AppButton>
        <span v-if="panel.transitions.length === 0" class="text-sm text-slate-400">{{ $t("auctions.status.noActions") }}</span>

        <div v-if="panel.status === 'Live'" class="ml-auto flex items-center gap-2 rounded-lg border border-slate-200 px-2 py-1">
          <ClockIcon class="h-4 w-4 text-slate-400" />
          <input v-model.number="extendMinutes" type="number" min="1" class="w-16 rounded-md border border-slate-300 px-2 py-1 text-sm" />
          <span class="text-xs text-slate-500">{{ $t("auctions.status.minutes") }}</span>
          <AppButton variant="secondary" :loading="extending" @click="extend">{{ $t("auctions.status.extend") }}</AppButton>
        </div>
      </div>
      <p v-else class="text-xs text-slate-400">{{ $t("auctions.status.adminOnly") }}</p>

      <!-- Durum geçmişi -->
      <div>
        <h4 class="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("auctions.status.history") }}</h4>
        <ol v-if="panel.history.length > 0" class="relative space-y-3 border-l border-slate-200 pl-4">
          <li v-for="h in visibleHistory" :key="h.id" class="relative">
            <span class="absolute -left-[21px] top-1.5 h-2.5 w-2.5 rounded-full border-2 border-surface" :class="h.isAutomatic ? 'bg-slate-400' : 'bg-indigo-500'" />
            <div class="flex flex-wrap items-center gap-1.5 text-sm">
              <AppBadge :tone="statusTone[h.fromStatus]">{{ $t(`enums.auctionStatus.${h.fromStatus}`) }}</AppBadge>
              <ArrowRightIcon class="h-3.5 w-3.5 text-slate-400" />
              <AppBadge :tone="statusTone[h.toStatus]">{{ $t(`enums.auctionStatus.${h.toStatus}`) }}</AppBadge>
              <span class="ml-auto text-xs text-slate-400">{{ formatDateTime(h.changedAt) }}</span>
            </div>
            <div class="mt-1 flex items-center gap-1 text-xs text-slate-500">
              <CpuChipIcon v-if="h.isAutomatic" class="h-3.5 w-3.5" />
              <UserIcon v-else class="h-3.5 w-3.5" />
              <span>{{ h.isAutomatic ? $t("auctions.status.system") : h.changedByName ?? "—" }}<template v-if="h.isAutomatic && h.changedByName"> · {{ h.changedByName }}</template></span>
            </div>
            <p v-if="h.reason" class="mt-1 rounded bg-slate-50 px-2 py-1 text-xs italic text-slate-600">“{{ h.reason }}”</p>
            <p v-else-if="h.isAutomatic" class="mt-1 text-xs text-slate-400">{{ $t(`auctions.status.auto.${h.toStatus}`) }}</p>
          </li>
        </ol>
        <p v-else class="text-xs text-slate-400">{{ $t("auctions.status.historyEmpty") }}</p>
        <button
          v-if="panel.history.length > 5"
          type="button"
          class="mt-2 text-xs font-medium text-indigo-600 hover:underline"
          @click="showAllHistory = !showAllHistory"
        >
          {{ showAllHistory ? $t("auctions.status.showLess") : $t("auctions.status.showAll", { n: panel.history.length }) }}
        </button>
      </div>
    </div>

    <!-- Onay penceresi -->
    <Teleport to="body">
      <div v-if="dialog && panel" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm" @click.self="dialog = null">
        <form class="w-full max-w-md overflow-hidden rounded-2xl bg-surface shadow-2xl" @submit.prevent="confirm">
          <div class="flex items-start justify-between border-b border-slate-100 px-6 py-4">
            <div>
              <h2 class="text-base font-semibold text-slate-800">{{ $t(`auctions.status.actions.${dialogKey}`) }}</h2>
              <div class="mt-1.5 flex items-center gap-1.5">
                <AppBadge :tone="statusTone[panel.status]">{{ $t(`enums.auctionStatus.${panel.status}`) }}</AppBadge>
                <ArrowRightIcon class="h-3.5 w-3.5 text-slate-400" />
                <AppBadge :tone="statusTone[dialog.to]">{{ $t(`enums.auctionStatus.${dialog.to}`) }}</AppBadge>
              </div>
            </div>
            <button type="button" class="rounded p-1 text-slate-400 hover:bg-slate-100" :disabled="saving" @click="dialog = null">
              <XMarkIcon class="h-5 w-5" />
            </button>
          </div>

          <div class="space-y-4 px-6 py-5">
            <p class="rounded-lg px-3 py-2 text-sm" :class="dialogKey === 'cancel' ? 'bg-rose-50 text-rose-700' : 'bg-slate-50 text-slate-600'">
              {{ $t(`auctions.status.effect.${dialogKey}`) }}
            </p>

            <div v-if="dialog.allowsStartAt">
              <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.status.dialog.startAt") }}</label>
              <input v-model="form.startAt" type="datetime-local" required class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none" />
            </div>
            <div v-if="dialog.allowsEndAt || dialog.requiresEndAt">
              <label class="mb-1 block text-sm font-medium text-slate-700">{{ $t("auctions.status.dialog.endAt") }}</label>
              <input
                v-model="form.endAt"
                type="datetime-local"
                :required="dialog.requiresEndAt"
                class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
                @input="endDirty = true"
              />
              <p v-if="dialogKey === 'resume'" class="mt-1 text-[11px] text-slate-400">{{ $t("auctions.status.dialog.endHintResume") }}</p>
            </div>
            <div>
              <label class="mb-1 block text-sm font-medium text-slate-700">
                {{ dialog.requiresReason ? $t("auctions.status.dialog.reasonRequired") : $t("auctions.status.dialog.reason") }}
              </label>
              <textarea
                v-model="form.reason"
                rows="3"
                maxlength="500"
                :placeholder="$t('auctions.status.dialog.reasonPlaceholder')"
                class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none"
              />
            </div>
            <p v-if="dialogError" class="rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">{{ dialogError }}</p>
          </div>

          <div class="flex justify-end gap-2 border-t border-slate-100 px-6 py-4">
            <AppButton variant="secondary" :disabled="saving" @click="dialog = null">{{ $t("common.cancel") }}</AppButton>
            <AppButton type="submit" :variant="dialogKey === 'cancel' ? 'danger' : 'primary'" :loading="saving" :disabled="!canConfirm">
              {{ $t(`auctions.status.actions.${dialogKey}`) }}
            </AppButton>
          </div>
        </form>
      </div>
    </Teleport>
  </AppCard>
</template>
