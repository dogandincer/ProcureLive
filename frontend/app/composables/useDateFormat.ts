export function useDateFormat() {
  const { locale } = useI18n();

  const localeTag = computed(() => (locale.value === "tr" ? "tr-TR" : "en-US"));

  function formatDateTime(value: string | Date): string {
    return new Date(value).toLocaleString(localeTag.value);
  }

  function formatDate(value: string | Date): string {
    return new Date(value).toLocaleDateString(localeTag.value);
  }

  return { formatDateTime, formatDate };
}
