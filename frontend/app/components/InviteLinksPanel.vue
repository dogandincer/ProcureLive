<script setup lang="ts">
import type { InviteLinkDto } from "~/types/api";
import { LinkIcon, ClipboardDocumentIcon, CheckIcon } from "@heroicons/vue/24/outline";

/**
 * Admin: bir cari için benzersiz, süreli davet linki (/invite?token=...). Link oluşturulunca cari
 * ihaleye "Davetli" olarak da eklenir; davetli kayıt olur ya da mevcut hesabıyla giriş yapıp sahiplenir.
 */
const props = defineProps<{ auctionId: string; status: string }>();
const emit = defineEmits<{ created: [] }>();

const api = useApi();
const toast = useToast();
const { t } = useI18n();
const { formatDateTime } = useDateFormat();

const links = ref<InviteLinkDto[]>([]);
const form = reactive({ companyId: null as string | null, email: "", validDays: 7 });
const companyName = ref<string | null>(null);
const { loadSupplierRows } = useSupplierLookup();
const creating = ref(false);
const copiedId = ref<string | null>(null);

const canCreate = computed(() => !["Ended", "Cancelled", "Awarded"].includes(props.status));
const fullUrl = (l: InviteLinkDto) => `${window.location.origin}${l.path}`;

async function load() {
  try {
    links.value = await api.get<InviteLinkDto[]>(`/api/auctions/${props.auctionId}/invite-links`);
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("inviteLinks.loadError")));
  }
}

async function create() {
  if (!form.companyId) return;
  creating.value = true;
  try {
    const link = await api.post<InviteLinkDto>(`/api/auctions/${props.auctionId}/invite-links`, {
      companyId: form.companyId,
      email: form.email.trim() || null,
      validDays: form.validDays,
    });
    links.value = [link, ...links.value];
    form.companyId = null;
    companyName.value = null;
    form.email = "";
    await copy(link);
    emit("created");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("inviteLinks.createError")));
  } finally {
    creating.value = false;
  }
}

async function copy(l: InviteLinkDto) {
  try {
    await navigator.clipboard.writeText(fullUrl(l));
    copiedId.value = l.id;
    toast.success(t("inviteLinks.copiedToast"));
    setTimeout(() => (copiedId.value = copiedId.value === l.id ? null : copiedId.value), 2000);
  } catch {
    // Pano izni yoksa link listede görünür kalır; kullanıcı elle kopyalayabilir.
  }
}

function state(l: InviteLinkDto): { key: string; tone: "emerald" | "slate" | "indigo" } {
  if (l.isExpired) return { key: "expired", tone: "slate" };
  if (l.usedAt) return { key: "registered", tone: "indigo" };
  return { key: "active", tone: "emerald" };
}

onMounted(load);
</script>

<template>
  <AppCard :title="$t('inviteLinks.title')" :description="$t('inviteLinks.description')">
    <form v-if="canCreate" class="space-y-2" @submit.prevent="create">
      <LookupField
        v-model="form.companyId"
        :load="loadSupplierRows"
        :title="$t('auctions.detail.selectCompany')"
        :display="companyName"
        :placeholder="$t('auctions.detail.selectCompany')"
        data-testid="invite-link-company"
        required
        @select="(r) => (companyName = r ? `${r.code} · ${r.name}` : null)"
      />
      <input
        v-model="form.email"
        type="email"
        maxlength="200"
        :placeholder="$t('inviteLinks.emailPlaceholder')"
        class="w-full rounded-lg border border-slate-300 px-2.5 py-1.5 text-sm focus:border-indigo-400 focus:outline-none"
      />
      <div class="flex items-center gap-2">
        <label class="text-xs text-slate-500">{{ $t("inviteLinks.validDays") }}</label>
        <input v-model.number="form.validDays" type="number" min="1" max="30" class="w-16 rounded-lg border border-slate-300 px-2 py-1.5 text-sm" />
        <AppButton type="submit" class="ml-auto" :loading="creating" :disabled="!form.companyId">
          <LinkIcon class="h-4 w-4" />
          {{ $t("inviteLinks.create") }}
        </AppButton>
      </div>
    </form>
    <p v-else class="text-xs text-slate-400">{{ $t("inviteLinks.closed") }}</p>

    <ul v-if="links.length > 0" class="-mx-5 -mb-5 mt-4 divide-y divide-slate-50 border-t border-slate-100">
      <li v-for="l in links" :key="l.id" class="px-5 py-3">
        <div class="flex items-center justify-between gap-2">
          <span class="truncate text-sm font-medium text-slate-700">{{ l.companyName }}</span>
          <AppBadge :tone="state(l).tone">{{ $t(`inviteLinks.state.${state(l).key}`) }}</AppBadge>
        </div>
        <div class="mt-1 flex items-center gap-2">
          <code class="flex-1 truncate rounded bg-slate-50 px-2 py-1 text-[11px] text-slate-500">{{ fullUrl(l) }}</code>
          <button
            type="button"
            class="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-indigo-600"
            :title="$t('inviteLinks.copy')"
            @click="copy(l)"
          >
            <CheckIcon v-if="copiedId === l.id" class="h-4 w-4 text-emerald-500" />
            <ClipboardDocumentIcon v-else class="h-4 w-4" />
          </button>
        </div>
        <div class="mt-1 text-[11px] text-slate-400">
          <span v-if="l.email">{{ l.email }} · </span>{{ $t("inviteLinks.expires", { date: formatDateTime(l.expiresAt) }) }}
        </div>
      </li>
    </ul>
  </AppCard>
</template>
