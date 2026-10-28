<script setup lang="ts">
import type { AuctionDetailDto } from "~/types/api";

/** Taslak ihalenin başlık, tarih ve şartlarını düzenler (yalnızca Taslak; sunucu da zorlar). */
const props = defineProps<{ auction: AuctionDetailDto; open: boolean }>();
const emit = defineEmits<{ close: []; saved: [] }>();
const api = useApi();
const toast = useToast();
const { t } = useI18n();

const paymentTerms = ["Advance", "Net15", "Net30", "Net60", "Net90"];
const deliveryTerms = ["EXW", "FOB", "CIF", "DAP", "DDP"];

// <input type="datetime-local"> yerel saat ister; sunucuya UTC ISO gider.
const toLocal = (iso: string) => {
  const d = new Date(iso);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
};

const form = reactive({
  title: "", description: "", startAt: "", endAt: "", paymentTerm: "" as string, deliveryTerm: "" as string,
  reservePrice: null as number | null, allowPartialBids: false, allowAlternativeProducts: false, isListedForPlus: false,
  extensionRuleMinutes: 0, extensionDurationMinutes: null as number | null, maxExtensionMinutes: null as number | null,
});
const saving = ref(false);

watch(() => props.open, (o) => {
  if (!o) return;
  const a = props.auction;
  Object.assign(form, {
    title: a.title, description: a.description ?? "", startAt: toLocal(a.startAt), endAt: toLocal(a.endAt),
    paymentTerm: a.paymentTerm ?? "", deliveryTerm: a.deliveryTerm ?? "", reservePrice: a.reservePrice,
    allowPartialBids: a.allowPartialBids, allowAlternativeProducts: a.allowAlternativeProducts, isListedForPlus: a.isListedForPlus,
    extensionRuleMinutes: a.extensionRuleMinutes, extensionDurationMinutes: a.extensionDurationMinutes, maxExtensionMinutes: a.maxExtensionMinutes,
  });
}, { immediate: true });

async function save() {
  saving.value = true;
  try {
    await api.put(`/api/auctions/${props.auction.id}`, {
      ...form,
      description: form.description || null,
      paymentTerm: form.paymentTerm || null,
      deliveryTerm: form.deliveryTerm || null,
      startAt: new Date(form.startAt).toISOString(),
      endAt: new Date(form.endAt).toISOString(),
    });
    toast.success(t("auctions.edit.saved"));
    emit("saved");
  } catch (error: any) {
    toast.error(apiErrorMessage(error, t("auctions.detail.actionError")));
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4" @click.self="emit('close')">
      <form class="my-8 w-full max-w-2xl rounded-xl bg-surface p-5 shadow-xl" role="dialog" aria-modal="true" :aria-label="$t('auctions.edit.title')" data-testid="draft-editor" @submit.prevent="save">
        <h2 class="text-base font-semibold text-slate-800">{{ $t("auctions.edit.title") }}</h2>
        <p class="mt-0.5 text-xs text-slate-500">{{ $t("auctions.edit.hint") }}</p>
        <div class="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600 sm:col-span-2">{{ $t("tender.wizard.f.title") }}
            <input v-model="form.title" required maxlength="300" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600 sm:col-span-2">{{ $t("auctions.edit.description") }}
            <textarea v-model="form.description" rows="2" maxlength="4000" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.startAt") }}
            <input v-model="form.startAt" type="datetime-local" required class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.endAt") }}
            <input v-model="form.endAt" type="datetime-local" required class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.paymentTerm") }}
            <select v-model="form.paymentTerm" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal">
              <option value="">{{ $t("tender.wizard.f.none") }}</option>
              <option v-for="p in paymentTerms" :key="p" :value="p">{{ $t(`enums.paymentTerm.${p}`) }}</option>
            </select>
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.deliveryTerm") }}
            <select v-model="form.deliveryTerm" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal">
              <option value="">{{ $t("tender.wizard.f.none") }}</option>
              <option v-for="d in deliveryTerms" :key="d" :value="d">{{ $t(`enums.deliveryTerm.${d}`) }}</option>
            </select>
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("tender.wizard.f.reserve") }}
            <input v-model.number="form.reservePrice" type="number" min="0" step="0.01" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
          </label>
          <label class="flex flex-col gap-1 text-xs font-medium text-slate-600">{{ $t("auctions.edit.extension") }}
            <input v-model.number="form.extensionRuleMinutes" type="number" min="0" max="120" class="rounded-md border border-slate-300 px-3 py-2 text-sm font-normal" />
          </label>
          <label class="flex items-center gap-2 text-sm text-slate-700 sm:col-span-2"><input v-model="form.allowPartialBids" type="checkbox" class="rounded" /> {{ $t("tender.wizard.f.allowPartial") }}</label>
          <label class="flex items-center gap-2 text-sm text-slate-700 sm:col-span-2"><input v-model="form.allowAlternativeProducts" type="checkbox" class="rounded" /> {{ $t("tender.wizard.f.allowAlternatives") }}</label>
          <label class="flex items-center gap-2 text-sm text-slate-700 sm:col-span-2"><input v-model="form.isListedForPlus" type="checkbox" class="rounded" /> {{ $t("tender.wizard.f.listedForPlus") }}</label>
        </div>
        <div class="mt-5 flex justify-end gap-2">
          <button type="button" class="rounded-md px-3 py-2 text-sm text-slate-600 hover:bg-slate-100" @click="emit('close')">{{ $t("common.cancel") }}</button>
          <AppButton type="submit" :loading="saving">{{ $t("common.save") }}</AppButton>
        </div>
      </form>
    </div>
  </Teleport>
</template>
