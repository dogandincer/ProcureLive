<script setup lang="ts">
import type { PagedResult, RfiProcessDto, RfiTemplateDto } from "~/types/api";

const api = useApi();
const { can } = usePermissions();
const toast = useToast();
const { t } = useI18n();
const processes = ref<RfiProcessDto[]>([]);
const templates = ref<RfiTemplateDto[]>([]);
const loading = ref(true);
const route = useRoute();
const status = ref(typeof route.query.status === "string" ? route.query.status : "");
const statusOptions = computed(() =>
  ["Draft", "Sent", "InProgress", "Closed", "Evaluated", "Awarded", "Cancelled"].map((s) => ({ value: s, label: t(`enums.processStatus.${s}`) })),
);

const showTemplateForm = ref(false);
const showProcessForm = ref(false);
const templateForm = reactive({ name: "", description: "" });
const processForm = reactive({ rfiTemplateId: "", title: "", ownerCompanyId: "", workspaceId: "", dueDate: "" });
const { selectedFirmId } = useFirm();
const { selectedWorkspaceId } = useWorkspace();
const expandedTemplateId = ref<string | null>(null);
const questionTypes = ["Text", "Number", "Boolean", "MultipleChoice", "FileUpload"];
const questionForm = reactive({ questionText: "", questionType: "Text", isRequired: true, weight: 1, optionsText: "" });

async function runAction(action: () => Promise<void>) {
  try {
    await action();
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.actionError")));
  }
}

const statusTone: Record<string, "slate" | "amber" | "emerald" | "indigo" | "rose"> = {
  Draft: "slate",
  Sent: "amber",
  InProgress: "amber",
  Closed: "slate",
  Evaluated: "indigo",
};

// Başlıktaki firma/işyeri art arda kurulur; geç dönen eski yanıt yenisinin üzerine yazmasın (yalnızca son istek uygulanır).
let loadSeq = 0;
const { page: listPage, total: listTotal, search: listSearch, pageSize: listPageSize, params: listParams, resetAndLoad } = usePagedList(() => load());


async function load() {
  const seq = ++loadSeq;
  loading.value = true;
  try {
    const [processPage, templateList] = await Promise.all([
      api.get<PagedResult<RfiProcessDto>>("/api/rfi/processes", {
        ...listParams(),
        status: status.value || undefined,
        firmId: selectedFirmId.value ?? undefined,
        workspaceId: selectedWorkspaceId.value ?? undefined,
      }),
      api.get<RfiTemplateDto[]>("/api/rfi/templates"),
    ]);
    if (seq !== loadSeq) return;
    processes.value = processPage.items;
    listTotal.value = processPage.totalCount;
    templates.value = templateList;
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("rfi.loadError")));
    return;
  } finally {
    if (seq === loadSeq) loading.value = false;
  }

  if (!processForm.rfiTemplateId && templates.value.length > 0) processForm.rfiTemplateId = templates.value[0].id;
}

async function createTemplate() {
  await runAction(async () => {
    await api.post("/api/rfi/templates", { name: templateForm.name, description: templateForm.description || null, categoryId: null });
    Object.assign(templateForm, { name: "", description: "" });
    showTemplateForm.value = false;
    toast.success(t("rfi.templateCreatedToast"));
    await load();
  });
}

function toggleTemplate(template: RfiTemplateDto) {
  expandedTemplateId.value = expandedTemplateId.value === template.id ? null : template.id;
  Object.assign(questionForm, { questionText: "", questionType: "Text", isRequired: true, weight: 1, optionsText: "" });
}

async function addQuestion(template: RfiTemplateDto) {
  await runAction(async () => {
    const options = questionForm.questionType === "MultipleChoice"
      ? questionForm.optionsText.split("\n").map((o) => o.trim()).filter(Boolean)
      : null;

    await api.post(`/api/rfi/templates/${template.id}/questions`, {
      questionText: questionForm.questionText,
      questionType: questionForm.questionType,
      isRequired: questionForm.isRequired,
      sortOrder: template.questions.length + 1,
      weight: questionForm.weight,
      options,
    });
    Object.assign(questionForm, { questionText: "", questionType: "Text", isRequired: true, weight: 1, optionsText: "" });
    toast.success(t("rfi.questionAddedToast"));
    await load();
  });
}

async function createProcess() {
  await runAction(async () => {
    const id = await api.post<string>("/api/rfi/processes", {
      rfiTemplateId: processForm.rfiTemplateId,
      title: processForm.title,
      ownerCompanyId: processForm.ownerCompanyId,
      workspaceId: processForm.workspaceId || null,
      dueDate: new Date(processForm.dueDate).toISOString(),
    });
    showProcessForm.value = false;
    toast.success(t("rfi.processCreatedToast"));
    await navigateTo(`/admin/rfi/${id}`);
  });
}

onMounted(load);
watch([selectedFirmId, selectedWorkspaceId, status], () => resetAndLoad());
watch(() => route.query.status, (v) => { status.value = typeof v === "string" ? v : ""; });
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-slate-800">{{ $t("rfi.title") }}</h1>
      <div class="flex gap-2">
        <AppButton v-if="can('rfi.manage')" variant="secondary" @click="showTemplateForm = !showTemplateForm">
          {{ $t("rfi.newTemplate") }}
        </AppButton>
        <AppButton v-if="can('rfi.manage')" @click="showProcessForm = !showProcessForm">
          {{ $t("rfi.newProcess") }}
        </AppButton>
      </div>
    </div>

    <form v-if="showTemplateForm" class="grid grid-cols-1 gap-4 rounded-xl border border-slate-200 bg-surface p-5 sm:grid-cols-2" @submit.prevent="createTemplate">
      <input v-model="templateForm.name" :placeholder="$t('rfi.templateForm.name')" :aria-label="$t('rfi.templateForm.name')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <input v-model="templateForm.description" :placeholder="$t('rfi.templateForm.description')" :aria-label="$t('rfi.templateForm.description')" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <AppButton type="submit" variant="secondary" class="sm:col-span-2">{{ $t("rfi.templateForm.save") }}</AppButton>
      <p class="text-xs text-slate-400 sm:col-span-2">{{ $t("rfi.templateForm.hint") }}</p>
    </form>

    <AppCard>
      <h2 class="text-sm font-semibold text-slate-700">{{ $t("rfi.templatesHeading") }}</h2>
      <LoadingSpinner v-if="loading" class="mt-3" />
      <template v-else>
        <ul v-if="templates.length > 0" class="mt-3 space-y-2">
          <li v-for="tpl in templates" :key="tpl.id" class="rounded-lg border border-slate-100 p-3">
            <button class="flex w-full items-center justify-between text-left" @click="toggleTemplate(tpl)">
              <span class="text-sm font-medium text-slate-700">{{ tpl.name }}</span>
              <span class="text-xs text-slate-400">{{ tpl.questions.length }} {{ $t("rfi.questionCount") }} · {{ expandedTemplateId === tpl.id ? $t("rfi.hide") : $t("common.detail") }}</span>
            </button>

            <div v-if="expandedTemplateId === tpl.id" class="mt-3 space-y-3 border-t border-slate-100 pt-3">
              <ol class="list-decimal space-y-1 pl-4 text-sm text-slate-600">
                <li v-for="q in tpl.questions" :key="q.id">
                  {{ q.questionText }}
                  <span class="text-xs text-slate-400">
                    ({{ $t(`enums.questionType.${q.questionType}`) }}, {{ $t("rfi.weightLabel") }} {{ q.weight }}{{ q.isRequired ? $t("rfi.requiredSuffix") : "" }})
                  </span>
                </li>
                <li v-if="tpl.questions.length === 0" class="text-slate-400">{{ $t("rfi.emptyQuestions") }}</li>
              </ol>

              <form class="grid grid-cols-1 gap-2 sm:grid-cols-2" @submit.prevent="addQuestion(tpl)">
                <input v-model="questionForm.questionText" :placeholder="$t('rfi.questionForm.text')" :aria-label="$t('rfi.questionForm.text')" required class="sm:col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
                <select v-model="questionForm.questionType" :aria-label="$t('rfi.questionForm.type')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm">
                  <option v-for="qt in questionTypes" :key="qt" :value="qt">{{ $t(`enums.questionType.${qt}`) }}</option>
                </select>
                <input v-model.number="questionForm.weight" type="number" min="0" step="0.5" :placeholder="$t('rfi.questionForm.weight')" :aria-label="$t('rfi.questionForm.weight')" class="rounded-md border border-slate-300 px-2 py-1.5 text-sm" />
                <textarea
                  v-if="questionForm.questionType === 'MultipleChoice'"
                  v-model="questionForm.optionsText"
                  :placeholder="$t('rfi.questionForm.optionsPlaceholder')"
                  rows="3"
                  class="sm:col-span-2 rounded-md border border-slate-300 px-2 py-1.5 text-sm"
                />
                <label class="flex items-center gap-2 text-xs text-slate-600">
                  <input v-model="questionForm.isRequired" type="checkbox" />
                  {{ $t("rfi.questionForm.requiredLabel") }}
                </label>
                <AppButton type="submit">{{ $t("rfi.questionForm.add") }}</AppButton>
              </form>
            </div>
          </li>
        </ul>
        <EmptyState v-else class="mt-3">{{ $t("rfi.emptyTemplates") }}</EmptyState>
      </template>
    </AppCard>

    <form v-if="showProcessForm" class="space-y-4 rounded-xl border border-slate-200 bg-surface p-5" data-testid="rfi-process-form" @submit.prevent="createProcess">
      <h2 class="text-sm font-semibold text-slate-800">{{ $t("rfi.newProcess") }}</h2>
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600 sm:col-span-2">{{ $t("rfi.processForm.template") }}
          <select v-model="processForm.rfiTemplateId" required class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal text-slate-800">
            <option v-for="tpl in templates" :key="tpl.id" :value="tpl.id">{{ tpl.name }} ({{ tpl.questions.length }} {{ $t("rfi.questionCount") }})</option>
          </select>
        </label>
        <FirmWorkspaceSelect v-model:owner="processForm.ownerCompanyId" v-model:workspace="processForm.workspaceId" />
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600 sm:col-span-2">{{ $t("rfi.processForm.title") }}
          <input v-model="processForm.title" :placeholder="$t('rfi.processForm.titlePlaceholder')" required class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
        </label>
        <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("rfi.processForm.dueDate") }}
          <input v-model="processForm.dueDate" type="datetime-local" required class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
          <span class="font-normal text-slate-400">{{ $t("rfi.processForm.dueDateHint") }}</span>
        </label>
      </div>
      <div class="flex gap-2">
        <AppButton type="submit">{{ $t("rfi.processForm.create") }}</AppButton>
        <AppButton variant="ghost" @click="showProcessForm = false">{{ $t("common.cancel") }}</AppButton>
      </div>
    </form>

    <AppCard>
      <div class="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <input v-model="listSearch" type="search" :placeholder="$t('pager.search.rfi')" :aria-label="$t('pager.search.rfi')" class="w-full max-w-sm rounded-md border border-slate-300 px-3 py-1.5 text-sm" data-testid="list-search" />
        <StatusFilter v-model="status" :options="statusOptions" />
      </div>
      <LoadingSpinner v-if="loading" />
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-100 text-left text-xs uppercase text-slate-400">
              <th class="pb-2">{{ $t("rfi.table.title") }}</th>
              <th class="pb-2">{{ $t("rfi.table.template") }}</th>
              <th class="pb-2">{{ $t("common.status") }}</th>
              <th class="pb-2">{{ $t("rfi.table.invitations") }}</th>
              <th class="pb-2">{{ $t("rfi.table.responses") }}</th>
              <th class="pb-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in processes" :key="p.id" class="border-b border-slate-50">
              <td class="py-2 font-medium text-slate-700">
                <NuxtLink :to="`/admin/rfi/${p.id}`" class="text-slate-800 hover:text-indigo-600 hover:underline">{{ p.title }}</NuxtLink>
                <span v-if="p.workspaceName" class="block text-[11px] font-normal text-slate-400">{{ p.ownerCompanyName }} · {{ p.workspaceName }}</span>
              </td>
              <td class="py-2 text-slate-500">{{ p.templateName }}</td>
              <td class="py-2"><AppBadge :tone="statusTone[p.status]">{{ $t(`enums.processStatus.${p.status}`) }}</AppBadge></td>
              <td class="py-2 text-slate-500">{{ p.invitationCount }}</td>
              <td class="py-2 text-slate-500">{{ p.responseCount }}</td>
              <td class="py-2 text-right"><NuxtLink :to="`/admin/rfi/${p.id}`" class="text-indigo-600 hover:underline">{{ $t("common.detail") }}</NuxtLink></td>
            </tr>
          </tbody>
        </table>
        <EmptyState v-if="processes.length === 0">{{ $t("rfi.empty") }}</EmptyState>
        <ListPager v-model:page="listPage" :total="listTotal" :page-size="listPageSize" />
      </div>
    </AppCard>
  </div>
</template>
