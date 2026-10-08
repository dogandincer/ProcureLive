import { watchDebounced } from "@vueuse/core";

// ---- Enum kod tipleri (backend enum isimleriyle birebir) ----
export type AuctionTypeCode = "English" | "ReverseSealed" | "SealedBid";
export type PaymentTermCode = "Advance" | "Net15" | "Net30" | "Net60" | "Net90";
export type DeliveryTermCode = "EXW" | "FOB" | "CIF" | "DAP" | "DDP";

export const UNIT_OPTIONS = [
  "Piece", "Kilogram", "Gram", "Liter", "Meter", "SquareMeter", "CubicMeter", "Box", "Pallet", "Hour", "Service",
] as const;

// ---- Draft modelleri ----
export interface SniperProtection {
  enabled: boolean;
  triggerMinutes: number;      // X — son X dk'da teklif gelirse
  durationMinutes: number;     // Y — Y dk uzat
  maxExtensionMinutes: number; // Z — toplam uzatma tavanı
}

export interface TenderMaster {
  referenceCode: string;
  autoReference: boolean;
  title: string;
  description: string;
  auctionType: AuctionTypeCode;
  ownerCompanyId: string;
  currencyId: string;
  startAt: string;             // ISO (datetime-local'dan dönüştürülür)
  endAt: string;
  reservePrice: number | null;
  sniper: SniperProtection;
  paymentTerm: PaymentTermCode | "";
  deliveryTerm: DeliveryTermCode | "";
  allowAlternativeProducts: boolean;
  allowPartialBids: boolean;
}

export interface TenderLineDraft {
  tempId: string;              // stabil satır anahtarı (virtual grid + focus stabilitesi)
  productId: string | null;
  sku: string;                 // sadece gösterim — payload'a girmez
  name: string;
  thumbnailUrl: string | null;
  description: string;
  quantity: number;
  unitOfMeasure: string;
  targetPrice: number | null;
  requestedDeliveryDate: string | null;
  specification: string | null;
  sortOrder: number;
}

export interface VendorChip {
  id: string;
  name: string;
  taxNumber: string;
}

export interface ProductPoolItem {
  id: string;
  sku: string;
  name: string;
  thumbnailUrl: string | null;
}

export type AttachmentStatus = "uploading" | "done" | "error";

export interface TenderAttachmentDraft {
  tempId: string;
  fileName: string;
  url: string;
  contentType: string;
  sizeBytes: number;
  status: AttachmentStatus;
}

const STORAGE_KEY = "pl_tender_draft_v1";

function emptyMaster(): TenderMaster {
  return {
    referenceCode: "",
    autoReference: true,
    title: "",
    description: "",
    auctionType: "SealedBid",
    ownerCompanyId: "",
    currencyId: "",
    startAt: "",
    endAt: "",
    reservePrice: null,
    sniper: { enabled: false, triggerMinutes: 5, durationMinutes: 3, maxExtensionMinutes: 30 },
    paymentTerm: "",
    deliveryTerm: "",
    allowAlternativeProducts: false,
    allowPartialBids: false,
  };
}

// ---- Modül düzeyi tekil (singleton) state — sihirbaz adımları arası korunur ----
const master = reactive<TenderMaster>(emptyMaster());
const lines = ref<TenderLineDraft[]>([]);
const selectedVendorIds = reactive<Set<string>>(new Set());   // O(1) seçim/var-mı
const vendorChips = reactive<Map<string, VendorChip>>(new Map());
const attachments = ref<TenderAttachmentDraft[]>([]);
const activeStep = ref(1);
const restored = ref(false);

function reindexLines() {
  lines.value.forEach((l, i) => { l.sortOrder = i + 1; });
}

// ---- Taslak serileştirme (Set/Map düz diziye) ----
function snapshot() {
  return {
    master,
    lines: lines.value,
    vendors: [...vendorChips.values()],
    attachments: attachments.value,
    activeStep: activeStep.value,
  };
}

function saveDraft() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(snapshot()));
  } catch {
    // private window / kota — sessizce geç
  }
}

// Modül yüklenince bir kez: değişiklikleri debounce'lu otomatik kaydet (her tuşta değil)
if (import.meta.client) {
  watchDebounced(
    [() => master, lines, () => [...vendorChips.values()], attachments, activeStep],
    saveDraft,
    { debounce: 800, maxWait: 4000, deep: true },
  );
}

export function useTenderForm() {
  // ---------- Tedarikçi (cari) seçimi — hepsi O(1) ----------
  function isVendorSelected(id: string): boolean {
    return selectedVendorIds.has(id);
  }

  function toggleVendor(chip: VendorChip) {
    if (selectedVendorIds.has(chip.id)) {
      selectedVendorIds.delete(chip.id);
      vendorChips.delete(chip.id);
    } else {
      selectedVendorIds.add(chip.id);
      vendorChips.set(chip.id, chip);
    }
  }

  function selectVendors(chips: VendorChip[]) {
    for (const c of chips) {
      selectedVendorIds.add(c.id);
      vendorChips.set(c.id, c);
    }
  }

  function removeVendor(id: string) {
    selectedVendorIds.delete(id);
    vendorChips.delete(id);
  }

  function clearVendors() {
    selectedVendorIds.clear();
    vendorChips.clear();
  }

  // ---------- Kalemler ----------
  function hasProduct(productId: string): boolean {
    return lines.value.some((l) => l.productId === productId);
  }

  function addLineFromProduct(p: ProductPoolItem) {
    if (p.id && hasProduct(p.id)) return; // aynı ürünü iki kez ekleme
    lines.value.push({
      tempId: crypto.randomUUID(),
      productId: p.id,
      sku: p.sku,
      name: p.name,
      thumbnailUrl: p.thumbnailUrl,
      description: p.name,
      quantity: 1,
      unitOfMeasure: "Piece",
      targetPrice: null,
      requestedDeliveryDate: null,
      specification: null,
      sortOrder: lines.value.length + 1,
    });
  }

  function addBlankLine() {
    lines.value.push({
      tempId: crypto.randomUUID(),
      productId: null,
      sku: "",
      name: "",
      thumbnailUrl: null,
      description: "",
      quantity: 1,
      unitOfMeasure: "Piece",
      targetPrice: null,
      requestedDeliveryDate: null,
      specification: null,
      sortOrder: lines.value.length + 1,
    });
  }

  function removeLine(tempId: string) {
    const idx = lines.value.findIndex((l) => l.tempId === tempId);
    if (idx !== -1) {
      lines.value.splice(idx, 1);
      reindexLines();
    }
  }

  function moveLine(tempId: string, delta: number) {
    const idx = lines.value.findIndex((l) => l.tempId === tempId);
    const target = idx + delta;
    if (idx === -1 || target < 0 || target >= lines.value.length) return;
    const [moved] = lines.value.splice(idx, 1);
    lines.value.splice(target, 0, moved);
    reindexLines();
  }

  function applyDeliveryDateToAll(dateIso: string | null) {
    lines.value.forEach((l) => { l.requestedDeliveryDate = dateIso; });
  }

  // ---------- Türetilenler ----------
  const vendorChipList = computed(() => [...vendorChips.values()]);
  const vendorCount = computed(() => selectedVendorIds.size);
  const lineCount = computed(() => lines.value.length);

  const validationErrors = computed(() => {
    const e: string[] = [];
    if (!master.title.trim()) e.push("title");
    if (!master.ownerCompanyId) e.push("ownerCompany");
    if (!master.currencyId) e.push("currency");
    if (!master.startAt || !master.endAt) e.push("dates");
    else if (new Date(master.endAt) <= new Date(master.startAt)) e.push("endBeforeStart");
    if (lines.value.length === 0) e.push("noLines");
    if (lines.value.some((l) => !l.description.trim() || l.quantity <= 0)) e.push("invalidLine");
    if (selectedVendorIds.size === 0) e.push("noVendors");
    if (master.sniper.enabled && master.sniper.durationMinutes <= 0) e.push("sniperDuration");
    return e;
  });
  const isValid = computed(() => validationErrors.value.length === 0);

  // ---------- API payload (CreateTenderRequest kontratı) ----------
  function buildPayload() {
    return {
      master: {
        referenceCode: master.autoReference ? null : master.referenceCode || null,
        title: master.title.trim(),
        description: master.description || null,
        auctionType: master.auctionType,
        ownerCompanyId: master.ownerCompanyId,
        currencyId: master.currencyId,
        startAt: master.startAt,
        endAt: master.endAt,
        reservePrice: master.reservePrice,
        extensionTriggerMinutes: master.sniper.enabled ? master.sniper.triggerMinutes : 0,
        extensionDurationMinutes: master.sniper.enabled ? master.sniper.durationMinutes : null,
        maxExtensionMinutes: master.sniper.enabled ? master.sniper.maxExtensionMinutes : null,
        paymentTerm: master.paymentTerm || null,
        deliveryTerm: master.deliveryTerm || null,
        allowAlternativeProducts: master.allowAlternativeProducts,
        allowPartialBids: master.allowPartialBids,
      },
      lines: lines.value.map((l) => ({
        productId: l.productId,
        description: l.description.trim(),
        quantity: l.quantity,
        unitOfMeasure: l.unitOfMeasure,
        targetPrice: l.targetPrice,
        requestedDeliveryDate: l.requestedDeliveryDate,
        specification: l.specification,
        sortOrder: l.sortOrder,
      })),
      vendorIds: [...selectedVendorIds],
      attachments: attachments.value
        .filter((a) => a.status === "done")
        .map((a) => ({ fileName: a.fileName, url: a.url, contentType: a.contentType, sizeBytes: a.sizeBytes })),
    };
  }

  // ---------- Taslak yükle / temizle ----------
  function restoreDraft(): boolean {
    if (restored.value) return false;
    restored.value = true;
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return false;
      const d = JSON.parse(raw);
      Object.assign(master, d.master ?? {});
      lines.value = Array.isArray(d.lines) ? d.lines : [];
      selectedVendorIds.clear();
      vendorChips.clear();
      for (const v of d.vendors ?? []) {
        selectedVendorIds.add(v.id);
        vendorChips.set(v.id, v);
      }
      attachments.value = Array.isArray(d.attachments) ? d.attachments : [];
      activeStep.value = d.activeStep ?? 1;
      return (d.lines?.length ?? 0) > 0 || (d.vendors?.length ?? 0) > 0 || !!d.master?.title;
    } catch {
      return false;
    }
  }

  function resetForm() {
    Object.assign(master, emptyMaster());
    lines.value = [];
    selectedVendorIds.clear();
    vendorChips.clear();
    attachments.value = [];
    activeStep.value = 1;
    try { localStorage.removeItem(STORAGE_KEY); } catch { /* ignore */ }
  }

  return {
    // state
    master,
    lines,
    attachments,
    activeStep,
    // vendors
    isVendorSelected,
    toggleVendor,
    selectVendors,
    removeVendor,
    clearVendors,
    vendorChipList,
    vendorCount,
    // lines
    hasProduct,
    addLineFromProduct,
    addBlankLine,
    removeLine,
    moveLine,
    applyDeliveryDateToAll,
    lineCount,
    // derived + payload
    validationErrors,
    isValid,
    buildPayload,
    // draft
    saveDraft,
    restoreDraft,
    resetForm,
  };
}
