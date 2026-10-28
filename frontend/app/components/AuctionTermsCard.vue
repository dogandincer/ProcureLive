<script setup lang="ts">
import { PaperClipIcon, TrashIcon } from "@heroicons/vue/24/outline";
import type { AuctionDetailDto, UploadedFileDto } from "~/types/api";

/** İhalenin şartları ve ekleri — tedarikçi teklif vermeden önce teknik şartnameyi ve koşulları görür. */
const props = defineProps<{ auction: AuctionDetailDto; internal?: boolean }>();
const emit = defineEmits<{ changed: [] }>();
const { formatDateTime, formatMoney } = useDateFormat();
const assetUrl = useAssetUrl();
const api = useApi();
const toast = useToast();
const { t } = useI18n();

// Ekler taslak ve planlanmış ihalede (iç kullanıcı) eklenip çıkarılabilir; başlayan ihalede sabittir.
const canEditAttachments = computed(() => props.internal && ["Draft", "Scheduled"].includes(props.auction.status));
const removing = ref<string | null>(null);

async function addAttachment(f: UploadedFileDto) {
  try {
    await api.post(`/api/auctions/${props.auction.id}/attachments`, { fileName: f.fileName, url: f.url, contentType: f.contentType, sizeBytes: f.sizeBytes });
    toast.success(t("auctions.terms.attachmentAdded"));
    emit("changed");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.detail.actionError")));
  }
}

async function removeAttachment(id: string) {
  removing.value = id;
  try {
    await api.delete(`/api/auctions/${props.auction.id}/attachments/${id}`);
    emit("changed");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.detail.actionError")));
  } finally {
    removing.value = null;
  }
}

const sizeLabel = (bytes: number) =>
  bytes >= 1048576 ? `${(bytes / 1048576).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;

const rows = computed(() => {
  const a = props.auction;
  const out: { label: string; value: string }[] = [
    { label: "auctions.terms.startAt", value: formatDateTime(a.actualStartAt ?? a.startAt) },
    { label: "auctions.terms.endAt", value: formatDateTime(a.endAt) },
  ];
  return out;
});
</script>

<template>
  <AppCard :title="$t('auctions.terms.title')" data-testid="auction-terms">
    <dl class="grid grid-cols-1 gap-x-6 gap-y-3 text-sm sm:grid-cols-2">
      <div v-for="r in rows" :key="r.label">
        <dt class="text-xs text-slate-500">{{ $t(r.label) }}</dt>
        <dd class="font-medium text-slate-800">{{ r.value }}</dd>
      </div>
      <div>
        <dt class="text-xs text-slate-500">{{ $t("auctions.terms.paymentTerm") }}</dt>
        <dd class="font-medium text-slate-800">{{ auction.paymentTerm ? $t(`enums.paymentTerm.${auction.paymentTerm}`) : $t("auctions.terms.notSpecified") }}</dd>
      </div>
      <div>
        <dt class="text-xs text-slate-500">{{ $t("auctions.terms.deliveryTerm") }}</dt>
        <dd class="font-medium text-slate-800">{{ auction.deliveryTerm ? $t(`enums.deliveryTerm.${auction.deliveryTerm}`) : $t("auctions.terms.notSpecified") }}</dd>
      </div>
      <div>
        <dt class="text-xs text-slate-500">{{ $t("auctions.terms.partial") }}</dt>
        <dd class="font-medium text-slate-800">{{ auction.allowPartialBids ? $t("auctions.terms.partialYes") : $t("auctions.terms.partialNo") }}</dd>
      </div>
      <div>
        <dt class="text-xs text-slate-500">{{ $t("auctions.terms.alternatives") }}</dt>
        <dd class="font-medium text-slate-800">{{ auction.allowAlternativeProducts ? $t("common.yes") : $t("common.no") }}</dd>
      </div>
      <div v-if="internal && auction.reservePrice != null">
        <dt class="text-xs text-slate-500">{{ $t("auctions.terms.reserve") }}</dt>
        <dd class="font-medium text-slate-800">{{ formatMoney(auction.reservePrice, auction.currencyCode) }}</dd>
      </div>
    </dl>

    <div class="mt-4 border-t border-slate-100 pt-3">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <div class="text-xs font-semibold uppercase tracking-wide text-slate-500">{{ $t("auctions.terms.attachments") }}</div>
        <FileUploadButton v-if="canEditAttachments" purpose="auction-attachment" multiple :label="$t('auctions.terms.addAttachment')" @uploaded="addAttachment" />
      </div>
      <ul v-if="auction.attachments.length" class="mt-2 space-y-1.5">
        <li v-for="f in auction.attachments" :key="f.id" class="flex items-center gap-2 text-sm">
          <PaperClipIcon class="h-4 w-4 shrink-0 text-slate-400" />
          <a :href="assetUrl(f.url) ?? f.url" target="_blank" rel="noopener" class="min-w-0 truncate font-medium text-indigo-600 hover:underline">{{ f.fileName }}</a>
          <span class="shrink-0 text-xs text-slate-400">{{ sizeLabel(f.sizeBytes) }}</span>
          <button
            v-if="canEditAttachments"
            type="button"
            class="ml-auto shrink-0 rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
            :disabled="removing === f.id"
            :aria-label="$t('auctions.terms.removeAttachment', { name: f.fileName })"
            @click="removeAttachment(f.id)"
          >
            <TrashIcon class="h-4 w-4" />
          </button>
        </li>
      </ul>
      <p v-else class="mt-2 text-sm text-slate-400">{{ $t("auctions.terms.noAttachments") }}</p>
    </div>
  </AppCard>
</template>
