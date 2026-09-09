<script setup lang="ts">
import type { RfiProcessDetailDto } from "~/types/api";

const route = useRoute();
const processId = route.params.id as string;
const api = useApi();
const toast = useToast();
const { currentUser } = useAuth();

const process = ref<RfiProcessDetailDto | null>(null);
const loading = ref(true);
const declineReason = ref("");
const showDecline = ref(false);
const answers = reactive<Record<string, { text: string; number: number | null; bool: boolean | null; optionId: string | null }>>({});
const submitting = ref(false);
const accessDenied = ref(false);

const myInvitation = computed(() =>
  process.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId) ?? null,
);

const canAnswer = computed(() => myInvitation.value && ["Accepted", "Participating"].includes(myInvitation.value.status));

async function load() {
  loading.value = true;
  try {
    process.value = await api.get<RfiProcessDetailDto>(`/api/rfi/processes/${processId}`);
    for (const q of process.value.questions) {
      answers[q.id] ??= { text: "", number: null, bool: null, optionId: null };
    }
  } catch (error: any) {
    if (error?.response?.status === 403) {
      accessDenied.value = true;
    } else {
      toast.error(apiErrorMessage(error, "RFI süreci yüklenemedi."));
    }
  } finally {
    loading.value = false;
  }
}

async function accept() {
  try {
    await api.post(`/api/rfi/processes/${processId}/invitations/respond`, { accept: true, declineReason: null });
    toast.success("Davet kabul edildi.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "İşlem gerçekleştirilemedi."));
  }
}

async function decline() {
  try {
    await api.post(`/api/rfi/processes/${processId}/invitations/respond`, {
      accept: false,
      declineReason: declineReason.value || "Belirtilmedi",
    });
    showDecline.value = false;
    // Declining revokes read access to this process (server-side), so re-fetching would 403.
    const invitation = process.value?.invitations.find((i) => i.companyId === currentUser.value?.companyId);
    if (invitation) invitation.status = "Declined";
    toast.success("Davet reddedildi.");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "İşlem gerçekleştirilemedi."));
  }
}

async function submitAnswers() {
  if (!process.value) return;
  submitting.value = true;
  try {
    await api.post(`/api/rfi/processes/${processId}/responses`, {
      answers: process.value.questions.map((q) => ({
        rfiQuestionId: q.id,
        answerText: q.questionType === "Text" ? answers[q.id].text || null : null,
        answerNumber: q.questionType === "Number" ? answers[q.id].number : null,
        answerBool: q.questionType === "Boolean" ? answers[q.id].bool : null,
        selectedOptionId: q.questionType === "MultipleChoice" ? answers[q.id].optionId : null,
        attachmentUrl: null,
      })),
    });
    toast.success("Yanıtlarınız kaydedildi.");
    await load();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, "İşlem gerçekleştirilemedi."));
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div v-if="accessDenied" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
    <p class="text-sm font-medium text-slate-600">Bu RFI sürecine erişiminiz yok. Daveti reddetmiş olabilirsiniz.</p>
  </div>

  <LoadingSpinner v-else-if="loading" />

  <div v-else-if="process" class="space-y-6">
    <div>
      <h1 class="text-xl font-bold text-slate-800">{{ process.title }}</h1>
      <p class="text-sm text-slate-500">Son Tarih: {{ new Date(process.dueDate).toLocaleDateString("tr-TR") }}</p>
    </div>

    <div v-if="myInvitation?.status === 'Declined'" class="rounded-xl border border-slate-200 bg-slate-50 p-5">
      <p class="text-sm font-medium text-slate-600">Bu RFI davetini reddettiniz. Bu sürece artık erişiminiz yok.</p>
    </div>

    <div v-else-if="myInvitation?.status === 'Invited'" class="rounded-xl border border-amber-200 bg-amber-50 p-5">
      <p class="text-sm font-medium text-amber-800">Bu RFI sürecine davetlisiniz.</p>
      <div class="mt-3 flex gap-2">
        <AppButton variant="success" @click="accept">Kabul Et</AppButton>
        <AppButton variant="danger" @click="showDecline = true">Reddet</AppButton>
      </div>
      <div v-if="showDecline" class="mt-3 flex gap-2">
        <input v-model="declineReason" placeholder="Red gerekçesi" class="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <AppButton variant="danger" @click="decline">Onayla</AppButton>
      </div>
    </div>

    <form v-else-if="canAnswer" class="space-y-4 rounded-xl border border-slate-200 bg-white p-5" @submit.prevent="submitAnswers">
      <div v-for="q in process.questions" :key="q.id">
        <label class="mb-1 block text-sm font-medium text-slate-700">{{ q.questionText }}</label>

        <input v-if="q.questionType === 'Text'" v-model="answers[q.id].text" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <input v-else-if="q.questionType === 'Number'" v-model.number="answers[q.id].number" type="number" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
        <select v-else-if="q.questionType === 'Boolean'" v-model="answers[q.id].bool" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
          <option :value="true">Evet</option>
          <option :value="false">Hayır</option>
        </select>
        <select v-else-if="q.questionType === 'MultipleChoice'" v-model="answers[q.id].optionId" class="w-full rounded-md border border-slate-300 px-3 py-2 text-sm">
          <option v-for="o in q.options" :key="o.id" :value="o.id">{{ o.optionText }}</option>
        </select>
        <p v-else class="text-xs text-slate-400">Dosya yükleme bu ekranda henüz desteklenmiyor.</p>
      </div>

      <AppButton type="submit" :loading="submitting">
        {{ submitting ? "Gönderiliyor..." : "Yanıtları Gönder" }}
      </AppButton>
    </form>

    <div v-else class="rounded-xl border border-slate-200 bg-white p-5 text-sm text-slate-500">
      Bu süreçle etkileşiminiz bulunmuyor.
    </div>
  </div>
</template>
