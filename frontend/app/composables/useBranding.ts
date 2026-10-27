import type { BrandingDto } from "~/types/api";

const FALLBACK: BrandingDto = {
  appName: "Kuzey e-İhale Yönetimi",
  shortName: null,
  suffix: "e-İhale Yönetim Sistemi",
  logoUrl: null,
  isCustom: false,
};

/** Backend'in döndürdüğü göreli dosya adresini (/uploads/...) API kökünden çözer; harici URL'ler olduğu gibi kalır. */
export function useAssetUrl() {
  const config = useRuntimeConfig();
  return (url: string | null | undefined): string | null => {
    if (!url) return null;
    return url.startsWith("/") ? `${config.public.apiBaseUrl}${url}` : url;
  };
}

/**
 * Uygulama markası (varsayılan firmadan): ad = "{Kısa Ad} {Ek}" ya da "Kuzey e-İhale Yönetimi", logo = firma logosu.
 * Giriş ekranı, kenar çubuğu, sekme başlığı ve favicon bu state'i okur; firma kaydedilince refresh() çağrılır.
 */
export function useBranding() {
  const config = useRuntimeConfig();
  const assetUrl = useAssetUrl();
  const branding = useState<BrandingDto>("branding", () => FALLBACK);
  const loaded = useState<boolean>("brandingLoaded", () => false);

  async function load(force = false) {
    if (loaded.value && !force) return;
    try {
      branding.value = await $fetch<BrandingDto>("/api/branding", { baseURL: config.public.apiBaseUrl });
      loaded.value = true;
    } catch {
      // marka isteği düşerse temel ad kalır
    }
  }

  const logoSrc = computed(() => assetUrl(branding.value.logoUrl));
  const initials = computed(() =>
    (branding.value.shortName ?? branding.value.appName)
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((w) => w[0]!.toLocaleUpperCase("tr-TR"))
      .join(""),
  );

  return { branding, logoSrc, initials, load, refresh: () => load(true) };
}
