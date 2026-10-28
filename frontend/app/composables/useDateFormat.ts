export function useDateFormat() {
  const { locale, t } = useI18n();

  const localeTag = computed(() => (locale.value === "tr" ? "tr-TR" : "en-US"));

  // Saniye gösterilmez: ekranlarda dakika hassasiyeti yeterli.
  function formatDateTime(value: string | Date): string {
    return new Date(value).toLocaleString(localeTag.value, { dateStyle: "short", timeStyle: "short" });
  }

  function formatDate(value: string | Date): string {
    return new Date(value).toLocaleDateString(localeTag.value);
  }

  /** Binlik ayırıcılı sayı; küsurat yalnızca gerektiğinde (en fazla 4 hane). */
  function formatNumber(value: number | null | undefined, maxFractionDigits = 4): string {
    if (value === null || value === undefined || Number.isNaN(value)) return "—";
    return value.toLocaleString(localeTag.value, { maximumFractionDigits: maxFractionDigits });
  }

  function formatMoney(value: number | null | undefined, currencyCode?: string | null): string {
    if (value === null || value === undefined || Number.isNaN(value)) return "—";
    const n = value.toLocaleString(localeTag.value, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return currencyCode ? `${n} ${currencyCode}` : n;
  }

  /** Kalan süre: "5 gün 23 sa 12 dk"; bir saatin altında "12 dk 05 sn". */
  function formatDuration(ms: number): string {
    if (ms <= 0) return t("time.expired");
    const totalSeconds = Math.floor(ms / 1000);
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    if (days > 0) return t("time.daysHoursMinutes", { d: days, h: hours, m: minutes });
    if (hours > 0) return t("time.hoursMinutes", { h: hours, m: minutes });
    return t("time.minutesSeconds", { m: minutes, s: String(seconds).padStart(2, "0") });
  }

  return { formatDateTime, formatDate, formatNumber, formatMoney, formatDuration };
}
