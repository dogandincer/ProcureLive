/**
 * Oturumdaki kullanıcının işlem yetkileri (Admin: tümü, Satınalmacı: rolüne verilenler). Yalnızca görünüm içindir:
 * düğmeleri gizler/kilitler; sunucu her işlemde yetkiyi ayrıca denetler (PermissionBehavior).
 */
let pendingFor: string | null = null;

export function usePermissions() {
  const api = useApi();
  const { currentUser } = useAuth();
  const state = useState<{ userId: string; codes: string[] } | null>("myPermissions", () => null);

  async function refresh() {
    const userId = currentUser.value?.userId;
    if (!userId) return;
    try {
      state.value = { userId, codes: await api.get<string[]>("/api/permissions/me") };
    } catch {
      // yüklenemezse düğmeler açık kalır; sunucu yine reddeder
    }
  }

  // Kullanıcı değişince (çıkış/giriş) eski kullanıcının yetkileri kullanılmaz. Aynı anda açılan bileşenler tek istek paylaşır.
  if (import.meta.client && currentUser.value && state.value?.userId !== currentUser.value.userId && pendingFor !== currentUser.value.userId) {
    pendingFor = currentUser.value.userId;
    refresh().finally(() => (pendingFor = null));
  }

  /** Yetki listesi henüz gelmediyse true: düğme kısa süre görünür, sunucu kararı geçerlidir. */
  const can = (code: string) => !state.value || state.value.userId !== currentUser.value?.userId || state.value.codes.includes(code);

  return { can, refresh };
}
