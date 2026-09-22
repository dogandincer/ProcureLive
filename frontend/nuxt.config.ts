export default defineNuxtConfig({
  compatibilityDate: "2026-01-01",
  srcDir: "app/",
  devtools: { enabled: true },
  modules: ["@nuxtjs/tailwindcss", "@nuxtjs/i18n"],
  css: ["~/assets/css/main.css"],
  i18n: {
    restructureDir: false,
    langDir: "locales",
    strategy: "no_prefix",
    defaultLocale: "tr",
    locales: [
      { code: "tr", name: "Türkçe", file: "tr.json" },
      { code: "en", name: "English", file: "en.json" },
    ],
    // No URL prefixing (internal tool, not SEO) and no browser auto-detection - the user picks
    // explicitly via LanguageSwitcher.vue, and that choice should never be silently overridden.
    detectBrowserLanguage: false,
  },
  // Authenticated internal tool, not a public/SEO site: SSR has no benefit here, and it caused a
  // hydration mismatch because currentUser is only known client-side (JWT lives in a cookie, but
  // the "who am I" API call runs in a client-only plugin) - the server always rendered a logged-out
  // header. Disabling SSR removes the mismatch entirely instead of papering over it with <ClientOnly>.
  ssr: false,
  runtimeConfig: {
    public: {
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || "http://localhost:5228",
    },
  },
  app: {
    head: {
      title: "ProcureLive",
      meta: [{ name: "viewport", content: "width=device-width, initial-scale=1" }],
    },
  },
});
