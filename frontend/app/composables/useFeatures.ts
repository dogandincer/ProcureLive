/**
 * Premium özellikler (sunucu yapılandırması): ERP entegrasyonu lisanslı mı ve hangi modda. Oturum başına bir kez yüklenir;
 * ERP ayarı kaydedilince `refresh()` çağrılır. Lisans yoksa ERP düğmeleri/kartları hiç gösterilmez, menüde yalnızca tanıtım kalır.
 */
export type ErpMode = "Off" | "Simulation" | "Live";

export function useFeatures() {
  const api = useApi();
  const state = useState<{ erpIntegration: boolean; erpMode: ErpMode } | null>("features", () => null);
  const loading = useState<Promise<void> | null>("features-loading", () => null);

  async function refresh() {
    try {
      state.value = await api.get<{ erpIntegration: boolean; erpMode: ErpMode }>("/api/features");
    } catch {
      state.value = { erpIntegration: false, erpMode: "Off" };
    }
  }

  function ensure() {
    if (state.value || !import.meta.client) return Promise.resolve();
    loading.value ??= refresh().finally(() => (loading.value = null));
    return loading.value;
  }

  const erpLicensed = computed(() => !!state.value?.erpIntegration);
  const erpMode = computed<ErpMode>(() => state.value?.erpMode ?? "Off");
  /** Lisanslı ve Admin entegrasyonu Simülasyon/Canlı moda almış: aktarım ve ERP kartları görünür. */
  const erpActive = computed(() => erpLicensed.value && erpMode.value !== "Off");

  return { ensure, refresh, erpLicensed, erpMode, erpActive };
}
