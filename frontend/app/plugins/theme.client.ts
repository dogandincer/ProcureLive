// Tema ilk boyamadan önce uygulanır (son kullanılan tema), oturum açan kullanıcının kendi teması ardından yüklenir.
// "Sistem" modunda işletim sistemi açık/koyu değişince sayfa da izler.
import { applyTheme } from "~/utils/theme";

// Dosya adı sırası auth.client.ts'den sonra çalışmasını sağlar (oturum bilgisi hazır).
export default defineNuxtPlugin({
  setup() {
    const { theme, loadFor } = useTheme();
    const { currentUser } = useAuth();

    loadFor(currentUser.value?.userId);
    watch(() => currentUser.value?.userId, (id) => loadFor(id));

    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
      if (theme.value.mode === "system") applyTheme(theme.value);
    });
  },
});
