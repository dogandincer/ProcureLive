<script setup lang="ts">
import { ChatBubbleLeftRightIcon, MegaphoneIcon } from "@heroicons/vue/24/outline";
import type { ProcessQuestionDto } from "~/types/api";

/**
 * Süreç (ihale/RFI/RFQ) soru-cevabı ve duyurular. Tedarikçi soru sorar; iç kullanıcı cevaplar (herkese açık veya
 * yalnızca soran firmaya) ve tüm katılımcılara duyuru yapar. Görünürlük sunucuda süzülür: tedarikçi başka firmanın
 * sorusunu yalnızca herkese açık cevaplandıysa ve firma adı olmadan görür.
 */
const props = withDefaults(defineProps<{ kind: "auction" | "rfi" | "rfq"; processId: string; internal: boolean; canAsk?: boolean }>(), {
  canAsk: false,
});
const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();
const { can } = usePermissions();

const items = ref<ProcessQuestionDto[]>([]);
const loading = ref(true);
const question = ref("");
const announcement = ref("");
const posting = ref(false);
const answering = ref<string | null>(null);
const answerText = ref("");
const answerPublic = ref(true);

const canAnswer = computed(() => props.internal && can("qa.answer"));
const openCount = computed(() => items.value.filter((q) => !q.isAnnouncement && !q.answer).length);

async function load() {
  try {
    items.value = await api.get<ProcessQuestionDto[]>(`/api/processes/${props.kind}/${props.processId}/questions`);
  } catch {
    items.value = [];
  } finally {
    loading.value = false;
  }
}

async function ask() {
  if (!question.value.trim()) return;
  posting.value = true;
  try {
    await api.post(`/api/processes/${props.kind}/${props.processId}/questions`, { question: question.value.trim() });
    question.value = "";
    toast.success(t("qa.asked"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("qa.error")));
  } finally {
    posting.value = false;
  }
}

async function announce() {
  if (!announcement.value.trim()) return;
  posting.value = true;
  try {
    await api.post(`/api/processes/${props.kind}/${props.processId}/announcements`, { text: announcement.value.trim() });
    announcement.value = "";
    toast.success(t("qa.announced"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("qa.error")));
  } finally {
    posting.value = false;
  }
}

function startAnswer(q: ProcessQuestionDto) {
  answering.value = q.id;
  answerText.value = q.answer ?? "";
  answerPublic.value = q.answer ? q.isPublic : true;
}

async function saveAnswer(id: string) {
  if (!answerText.value.trim()) return;
  posting.value = true;
  try {
    await api.put(`/api/process-questions/${id}/answer`, { answer: answerText.value.trim(), isPublic: answerPublic.value });
    answering.value = null;
    toast.success(t("qa.answered"));
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("qa.error")));
  } finally {
    posting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <AppCard :title="$t('qa.title')" :description="internal ? $t('qa.internalHint') : $t('qa.supplierHint')">
    <div id="qa" class="space-y-4" data-testid="qa-panel">
      <p v-if="internal && openCount > 0" class="rounded-md bg-amber-50 px-3 py-2 text-xs font-medium text-amber-800" data-testid="qa-open-count">
        {{ $t("qa.openCount", { n: openCount }) }}
      </p>

      <form v-if="canAnswer" class="space-y-2" data-testid="qa-announce-form" @submit.prevent="announce">
        <label for="qa-announcement" class="text-xs font-medium text-slate-600">{{ $t("qa.announcementLabel") }}</label>
        <textarea id="qa-announcement" v-model="announcement" rows="2" maxlength="2000" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" :placeholder="$t('qa.announcementPlaceholder')" />
        <AppButton type="submit" variant="secondary" :loading="posting" :disabled="!announcement.trim()"><MegaphoneIcon class="h-4 w-4" />{{ $t("qa.announce") }}</AppButton>
      </form>

      <form v-if="!internal && canAsk" class="space-y-2" data-testid="qa-ask-form" @submit.prevent="ask">
        <label for="qa-question" class="text-xs font-medium text-slate-600">{{ $t("qa.askLabel") }}</label>
        <textarea id="qa-question" v-model="question" rows="2" maxlength="2000" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" :placeholder="$t('qa.askPlaceholder')" />
        <AppButton type="submit" :loading="posting" :disabled="!question.trim()"><ChatBubbleLeftRightIcon class="h-4 w-4" />{{ $t("qa.ask") }}</AppButton>
      </form>

      <LoadingSpinner v-if="loading" />
      <EmptyState v-else-if="items.length === 0" icon="💬">{{ $t("qa.empty") }}</EmptyState>
      <ul v-else class="space-y-3">
        <li v-for="q in items" :key="q.id" class="rounded-lg border p-3" :class="q.isAnnouncement ? 'border-indigo-200 bg-indigo-50/60' : 'border-slate-200'" data-testid="qa-item">
          <div class="flex flex-wrap items-center gap-2 text-xs text-slate-500">
            <AppBadge v-if="q.isAnnouncement" tone="indigo"><MegaphoneIcon class="mr-1 inline h-3 w-3" />{{ $t("qa.announcement") }}</AppBadge>
            <template v-else>
              <span v-if="q.companyName" class="font-medium text-slate-700">{{ q.companyName }}</span>
              <span v-else-if="q.isMine" class="font-medium text-slate-700">{{ $t("qa.mine") }}</span>
              <AppBadge v-if="q.answer" :tone="q.isPublic ? 'emerald' : 'slate'">{{ q.isPublic ? $t("qa.public") : $t("qa.private") }}</AppBadge>
              <AppBadge v-else tone="amber">{{ $t("qa.waiting") }}</AppBadge>
            </template>
            <span>{{ formatDateTime(q.createdAt) }}</span>
          </div>
          <p class="mt-1 whitespace-pre-line text-sm text-slate-800">{{ q.question }}</p>
          <div v-if="q.answer && answering !== q.id" class="mt-2 border-l-2 border-emerald-300 pl-3 text-sm text-slate-700" data-testid="qa-answer">
            <span class="block text-[11px] font-semibold uppercase text-emerald-700">{{ $t("qa.answer") }}</span>
            <span class="whitespace-pre-line">{{ q.answer }}</span>
          </div>
          <template v-if="canAnswer && !q.isAnnouncement">
            <form v-if="answering === q.id" class="mt-2 space-y-2" @submit.prevent="saveAnswer(q.id)">
              <textarea v-model="answerText" rows="3" maxlength="4000" :aria-label="$t('qa.answer')" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" data-testid="qa-answer-input" />
              <label class="flex items-center gap-2 text-xs text-slate-600">
                <input v-model="answerPublic" type="checkbox" class="rounded border-slate-300" data-testid="qa-answer-public" />
                {{ $t("qa.makePublic") }}
              </label>
              <div class="flex gap-2">
                <AppButton type="submit" :loading="posting" :disabled="!answerText.trim()" data-testid="qa-answer-save">{{ $t("qa.saveAnswer") }}</AppButton>
                <AppButton variant="ghost" @click="answering = null">{{ $t("common.cancel") }}</AppButton>
              </div>
            </form>
            <button v-else type="button" class="mt-2 text-xs font-medium text-indigo-600 hover:underline" data-testid="qa-answer-open" @click="startAnswer(q)">
              {{ q.answer ? $t("qa.editAnswer") : $t("qa.reply") }}
            </button>
          </template>
        </li>
      </ul>
    </div>
  </AppCard>
</template>
