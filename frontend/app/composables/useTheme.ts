import { applyTheme, DEFAULT_THEME, type ThemeSettings } from "~/utils/theme";

/**
 * Kişi başı görünüm tercihi. Tarayıcıda kullanıcıya özel anahtarla saklanır (aynı bilgisayarı kullanan kişiler kendi
 * temasını görür); giriş ekranı son kullanılan temayla açılır. Değişiklik anında uygulanır.
 */
const LAST_KEY = "pl_theme_last";
const keyFor = (userId: string | null | undefined) => (userId ? `pl_theme:${userId}` : LAST_KEY);

function read(key: string): ThemeSettings | null {
  try {
    const raw = localStorage.getItem(key);
    return raw ? { ...DEFAULT_THEME, ...JSON.parse(raw) } : null;
  } catch {
    return null;
  }
}

export function useTheme() {
  const theme = useState<ThemeSettings>("pl_theme", () => ({ ...DEFAULT_THEME }));
  const { currentUser } = useAuth();

  function save() {
    try {
      const json = JSON.stringify(theme.value);
      localStorage.setItem(keyFor(currentUser.value?.userId), json);
      localStorage.setItem(LAST_KEY, json);
    } catch {
      // gizli pencere vb.: tercih yalnızca bu oturumda geçerli kalır
    }
  }

  function update(patch: Partial<ThemeSettings>) {
    theme.value = { ...theme.value, ...patch };
    applyTheme(theme.value);
    save();
  }

  /** Kullanıcı değişince (giriş/çıkış) o kişinin kayıtlı temasını uygular. */
  function loadFor(userId: string | null | undefined) {
    theme.value = read(keyFor(userId)) ?? read(LAST_KEY) ?? { ...DEFAULT_THEME };
    applyTheme(theme.value);
  }

  function reset() {
    update({ ...DEFAULT_THEME });
  }

  return { theme, update, loadFor, reset };
}
