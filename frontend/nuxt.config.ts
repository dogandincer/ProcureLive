export default defineNuxtConfig({
  compatibilityDate: "2026-01-01",
  srcDir: "app/",
  devtools: { enabled: true },
  modules: ["@nuxtjs/tailwindcss"],
  css: ["~/assets/css/main.css"],
  ssr: true,
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
