<script setup lang="ts">
import type { RfiProcessDetailDto } from "~/types/api";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDate } = useDateFormat();
const { currentUser } = useAuth();
const { isPlus, refresh: refreshSubscription } = useSubscription();

const process = ref<RfiProcessDetailDto | null>(null);
const loading = ref(true);
const declineReason = ref("");
const showDecline = ref(false);
const answers = reactive<Record<string, { text: string; number: number | null; bool: boolean | null; optionId: string | null; attachmentUrl: string }>>({});
const submitting = ref(false);
const accessDenied = ref(false);

const myInvitation = computed(() =>
  process.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId) ?? null,
);

const accepted = computed(() => !!myInvitation.value && ["Accepted", "Participating"].includes(myInvitation.value.status));
// Standart kota dolu ve davet kabul edilmemiş: sorular sunucuda düşürüldü → "Plus alınız".
const restricted = computed(() => !!process.value && process.value.accessLevel !== "Full");
// İlk yanıt herkese; gönderilmiş yanıtın revizyonu yalnızca Plus'a ve süreç açıkken (sunucu da zorlar).
const canSubmit = computed(() => !!process.value?.canSubmit);

async function load() {
  loading.value = true;
  try {
    process.value = await api.get<RfiProcessDetailDto>(`/api/rfi/processes/${processId}`);
    const mine = new Map((process.value.myAnswers ?? []).map((a) => [a.rfiQuestionId, a]));
    for (const q of process.value.questions) {
      const a = mine.get(q.id);
      answers[q.id] = {
        text: a?.answerText ?? "",
        number: a?.answerNumber ?? null,
        bool: a?.answerBool ?? null,
        optionId: a?.selectedOptionId ?? null,
        attachmentUrl: a?.attachmentUrl ?? "",
      };
    }
  } catch (error: any) {
    if (error?.response?.status === 403) {
      accessDenied.value = true;
    } else {
      toast.error(apiErrorMessage(error, t("rfi.answer.loadError")));
    }
  } finally {
    loading.value = false;
  }
}

async function accept() {
  try {
    await api.post(`/api/rfi/processes/${processId}/invitations/respond`, { accept: true, declineReason: null });
    toast.success(t("rfi.answer.acceptedToast"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.answer.actionError")));
    if (apiErrorCode(error) === "QuotaExceeded") await Promise.all([load(), refreshSubscription()]);
  }
}

async function decline() {
  try {
    await api.post(`/api/rfi/processes/${processId}/invitations/respond`, {
      accept: false,
      declineReason: declineReason.value || t("common.notSpecified"),
    });
    showDecline.value = false;
    // Declining revokes read access to this process (server-side), so re-fetching would 403.
    const invitation = process.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId);
    if (invitation) invitation.status = "Declined";
    toast.success(t("rfi.answer.declinedToast"));
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.answer.actionError")));
  }
}

// Zorunlu sorular boş geçilemez (sunucu da aynı kuralı uygular).
const missingRequired = ref<string[]>([]);
function isAnswered(q: { id: string; questionType: string }) {
  const a = answers[q.id];
  switch (q.questionType) {
    case "Number": return a.number !== null && a.number !== undefined && (a.number as any) !== "";
    case "Boolean": return a.bool === true || a.bool === false;
    case "MultipleChoice": return !!a.optionId;
    case "FileUpload": return !!a.attachmentUrl?.trim();
    default: return !!a.text?.trim();
  }
}

async function submitAnswers() {
  if (!process.value) return;
  missingRequired.value = process.value.questions.filter((q) => q.isRequired && !isAnswered(q)).map((q) => q.id);
  if (missingRequired.value.length) {
    toast.error(t("rfi.answer.requiredMissing", { n: missingRequired.value.length }));
    return;
  }
  submitting.value = true;
  try {
    await api.post(`/api/rfi/processes/${processId}/responses`, {
      answers: process.value.questions.map((q) => ({
        rfiQuestionId: q.id,
        answerText: q.questionType === "Text" ? answers[q.id].text || null : null,
        answerNumber: q.questionType === "Number" ? answers[q.id].number : null,
        answerBool: q.questionType === "Boolean" ? answers[q.id].bool : null,
        selectedOptionId: q.questionType === "MultipleChoice" ? answers[q.id].optionId : null,
        attachmentUrl: q.questionType === "FileUpload" ? answers[q.id].attachmentUrl?.trim() || null : null,
      })),
    });
    toast.success(t("rfi.answer.answersSavedToast"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.answer.actionError")));
  } finally {
    submitting.value = false;
  }
}

onMounted(() => Promise.all([load(), refreshSubscription()]));
</script>

<template>
  <div v-if="accessDenied" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
    <p class="text-sm font-medium text-slate-600">{{ $t("rfi.answer.accessDenied") }}</p>
  </div>

  <LoadingSpinner v-else-if="loading && !process" />

  <div v-else-if="process" class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
      <p class="text-sm text-slate-500">
        {{ process.ownerCompanyName }} · {{ $t("rfi.answer.dueDate") }}: {{ formatDate(process.dueDate) }}
      </p>
    </div>

    <RestrictedOverlay
      v-if="restricted"
      level="QuotaExceeded"
      :title="$t('restricted.process.title')"
      :body="$t('restricted.process.body')"
      :rows="3"
    >
      <template #extra>
        <button type="button" class="mt-3 block w-full text-xs text-slate-400 hover:text-slate-600" @click="showDecline = true">
          {{ $t("restricted.process.declineInstead") }}
        </button>
        <div v-if="showDecline" class="mt-2 flex gap-2">
          <input v-model="declineReason" :placeholder="$t('common.declineReasonPlaceholder')" class="min-w-0 flex-1 rounded-md border border-slate-300 px-2 py-1 text-xs" />
          <AppButton variant="danger" @click="decline">{{ $t("common.confirm") }}</AppButton>
        </div>
      </template>
    </RestrictedOverlay>

    <div v-else-if="myInvitation?.status === 'Declined'" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
      <p class="text-sm font-medium text-slate-600">{{ $t("rfi.answer.declinedBanner") }}</p>
    </div>

    <div v-else-if="myInvitation?.status === 'Invited'" class="rounded-xl border border-amber-200 bg-amber-50 p-5">
      <p class="text-sm font-medium text-amber-800">{{ $t("rfi.answer.invitedBanner") }}</p>
      <div class="mt-3 flex gap-2">
        <AppButton @click="accept">{{ $t("common.accept") }}</AppButton>
        <AppButton variant="danger" @click="showDecline = true">{{ $t("common.decline") }}</AppButton>
      </div>
      <div v-if="showDecline" class="mt-3 flex gap-2">
        <input v-model="declineReason" :placeholder="$t('common.declineReasonPlaceholder')" class="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <AppButton variant="danger" @click="decline">{{ $t("common.confirm") }}</AppButton>
      </div>
    </div>

    <form v-else-if="accepted" class="space-y-4 rounded-xl border border-slate-200 bg-white p-5" @submit.prevent="submitAnswers">
      <!-- Durum: kapalı süreç / gönderildi (Standart: revizyon Plus'a özel) / Plus: revize edilebilir -->
      <p v-if="!process.isOpen" class="rounded-md bg-slate-50 px-3 py-2 text-xs text-slate-600">{{ $t("rfx.closed") }}</p>
      <div v-else-if="process.hasSubmitted && !canSubmit" class="flex flex-wrap items-center justify-between gap-2 rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800">
        <span>{{ $t("rfx.submittedStandard") }}</span>
        <NuxtLink to="/supplier/pricing" class="rounded bg-amber-500 px-2 py-1 font-semibold text-white hover:bg-amber-600">{{ $t("restricted.upgradeCta") }}</NuxtLink>
      </div>
      <p v-else-if="process.hasSubmitted" class="rounded-md bg-indigo-50 px-3 py-2 text-xs text-indigo-700">{{ $t("rfx.submittedPlus") }}</p>

      <fieldset :disabled="!canSubmit" class="space-y-4 disabled:opacity-80">
        <div v-for="q in process.questions" :key="q.id">
          <label :for="`q-${q.id}`" class="mb-1 block text-sm font-medium text-slate-700">
            {{ q.questionText }}<span v-if="q.isRequired" class="ml-0.5 text-rose-600" :title="$t('rfi.answer.required')">*</span>
          </label>

          <input v-if="q.questionType === 'Text'" :id="`q-${q.id}`" v-model="answers[q.id].text" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50" />
          <input v-else-if="q.questionType === 'Number'" :id="`q-${q.id}`" v-model.number="answers[q.id].number" type="number" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50" />
          <select v-else-if="q.questionType === 'Boolean'" :id="`q-${q.id}`" v-model="answers[q.id].bool" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50">
            <option :value="true">{{ $t("common.yes") }}</option>
            <option :value="false">{{ $t("common.no") }}</option>
          </select>
          <select v-else-if="q.questionType === 'MultipleChoice'" :id="`q-${q.id}`" v-model="answers[q.id].optionId" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50">
            <option v-for="o in q.options" :key="o.id" :value="o.id">{{ o.optionText }}</option>
          </select>
          <input v-else :id="`q-${q.id}`" v-model="answers[q.id].attachmentUrl" type="url" :placeholder="$t('rfi.answer.fileUrlPlaceholder')" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50" />
          <p v-if="missingRequired.includes(q.id)" class="mt-1 text-xs text-rose-600">{{ $t("rfi.answer.requiredField") }}</p>
        </div>
        <p class="text-[11px] text-slate-400"><span class="text-rose-600">*</span> {{ $t("rfi.answer.requiredLegend") }}</p>
      </fieldset>

      <AppButton v-if="canSubmit" type="submit" :loading="submitting">
        {{ submitting ? $t("rfi.answer.submitting") : process.hasSubmitted ? $t("rfx.revise") : $t("rfi.answer.submit") }}
      </AppButton>
      <p v-if="canSubmit && !process.hasSubmitted && !isPlus" class="text-[11px] text-slate-400">{{ $t("rfx.standardOnce") }}</p>
    </form>

    <div v-else class="rounded-xl border border-slate-200 bg-white p-5 text-sm text-slate-500">
      {{ $t("rfi.answer.noInteraction") }}
    </div>
  </div>
</template>
