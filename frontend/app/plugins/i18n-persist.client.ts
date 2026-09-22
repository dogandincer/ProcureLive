const STORAGE_KEY = "procurelive-locale";

// Runs in a Nuxt plugin (not a component's setup()), so it uses the global $i18n instance directly
// rather than the useI18n() composable, which requires an active component setup context.
export default defineNuxtPlugin((nuxtApp) => {
  const i18n = nuxtApp.$i18n as { locale: { value: string }; locales: { value: { code: string }[] } };

  const stored = localStorage.getItem(STORAGE_KEY);
  const validCodes = i18n.locales.value.map((l) => l.code);
  if (stored && validCodes.includes(stored)) {
    i18n.locale.value = stored;
  }

  watch(
    () => i18n.locale.value,
    (value) => localStorage.setItem(STORAGE_KEY, value),
  );
});
