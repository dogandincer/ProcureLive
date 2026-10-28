/**
 * Tema motoru: Tailwind renk aileleri CSS değişkenlerine bağlıdır (tailwind.config.ts). Bu modül seçilen moda
 * (açık/koyu), vurgu paletine ve menü stiline göre değişkenleri <html> üzerinde günceller. Varsayılan (açık + İndigo)
 * değerler main.css'te de durur; JS çalışmadan önce sayfa doğru renklerle açılır.
 */
export const SHADES = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950] as const;
export type Shade = (typeof SHADES)[number];
export type Scale = Record<Shade, string>;

const s = (hex: string[]): Scale => Object.fromEntries(SHADES.map((k, i) => [k, hex[i]])) as Scale;

// Tailwind v3 varsayılan paletleri (açık tema değerleri).
export const BASE: Record<string, Scale> = {
  slate: s(["#f8fafc", "#f1f5f9", "#e2e8f0", "#cbd5e1", "#94a3b8", "#64748b", "#475569", "#334155", "#1e293b", "#0f172a", "#020617"]),
  amber: s(["#fffbeb", "#fef3c7", "#fde68a", "#fcd34d", "#fbbf24", "#f59e0b", "#d97706", "#b45309", "#92400e", "#78350f", "#451a03"]),
  rose: s(["#fff1f2", "#ffe4e6", "#fecdd3", "#fda4af", "#fb7185", "#f43f5e", "#e11d48", "#be123c", "#9f1239", "#881337", "#4c0519"]),
  emerald: s(["#ecfdf5", "#d1fae5", "#a7f3d0", "#6ee7b7", "#34d399", "#10b981", "#059669", "#047857", "#065f46", "#064e3b", "#022c22"]),
  red: s(["#fef2f2", "#fee2e2", "#fecaca", "#fca5a5", "#f87171", "#ef4444", "#dc2626", "#b91c1c", "#991b1b", "#7f1d1d", "#450a0a"]),
  teal: s(["#f0fdfa", "#ccfbf1", "#99f6e4", "#5eead4", "#2dd4bf", "#14b8a6", "#0d9488", "#0f766e", "#115e59", "#134e4a", "#042f2e"]),
  orange: s(["#fff7ed", "#ffedd5", "#fed7aa", "#fdba74", "#fb923c", "#f97316", "#ea580c", "#c2410c", "#9a3412", "#7c2d12", "#431407"]),
};

export interface AccentPreset { id: string; scale: Scale }

// Hazır vurgu paletleri (Tailwind ölçekleri; buton beyaz yazısıyla okunur kalacak koyulukta).
export const ACCENT_PRESETS: AccentPreset[] = [
  { id: "indigo", scale: s(["#eef2ff", "#e0e7ff", "#c7d2fe", "#a5b4fc", "#818cf8", "#6366f1", "#4f46e5", "#4338ca", "#3730a3", "#312e81", "#1e1b4b"]) },
  { id: "ocean", scale: s(["#f0f9ff", "#e0f2fe", "#bae6fd", "#7dd3fc", "#38bdf8", "#0ea5e9", "#0284c7", "#0369a1", "#075985", "#0c4a6e", "#082f49"]) },
  { id: "emerald", scale: s(["#ecfdf5", "#d1fae5", "#a7f3d0", "#6ee7b7", "#34d399", "#10b981", "#059669", "#047857", "#065f46", "#064e3b", "#022c22"]) },
  { id: "teal", scale: s(["#f0fdfa", "#ccfbf1", "#99f6e4", "#5eead4", "#2dd4bf", "#14b8a6", "#0d9488", "#0f766e", "#115e59", "#134e4a", "#042f2e"]) },
  { id: "violet", scale: s(["#f5f3ff", "#ede9fe", "#ddd6fe", "#c4b5fd", "#a78bfa", "#8b5cf6", "#7c3aed", "#6d28d9", "#5b21b6", "#4c1d95", "#2e1065"]) },
  { id: "rose", scale: s(["#fff1f2", "#ffe4e6", "#fecdd3", "#fda4af", "#fb7185", "#f43f5e", "#e11d48", "#be123c", "#9f1239", "#881337", "#4c0519"]) },
  { id: "sunset", scale: s(["#fff7ed", "#ffedd5", "#fed7aa", "#fdba74", "#fb923c", "#f97316", "#ea580c", "#c2410c", "#9a3412", "#7c2d12", "#431407"]) },
  { id: "graphite", scale: s(["#f8fafc", "#f1f5f9", "#e2e8f0", "#cbd5e1", "#94a3b8", "#64748b", "#475569", "#334155", "#1e293b", "#0f172a", "#020617"]) },
];

// Özel renk için seçim paleti (her biri ana ton olarak 600'e yerleşir).
export const SWATCHES = [
  "#b91c1c", "#dc2626", "#e11d48", "#db2777", "#c026d3", "#9333ea",
  "#7c3aed", "#6d28d9", "#4f46e5", "#4338ca", "#2563eb", "#1d4ed8",
  "#0284c7", "#0369a1", "#0891b2", "#0e7490", "#0d9488", "#0f766e",
  "#059669", "#047857", "#16a34a", "#15803d", "#65a30d", "#4d7c0f",
  "#ca8a04", "#a16207", "#d97706", "#b45309", "#ea580c", "#c2410c",
  "#78716c", "#57534e", "#64748b", "#475569", "#334155", "#1e293b",
];

export type ThemeMode = "light" | "dark" | "system";
export type SidebarStyle = "dark" | "light" | "accent";
export interface ThemeSettings { mode: ThemeMode; accent: string; customColor: string; sidebar: SidebarStyle }
export const DEFAULT_THEME: ThemeSettings = { mode: "light", accent: "indigo", customColor: "#4f46e5", sidebar: "dark" };

function hexToRgb(hex: string): [number, number, number] {
  const h = hex.replace("#", "");
  const n = parseInt(h.length === 3 ? h.split("").map((c) => c + c).join("") : h, 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
}
const toHex = (rgb: number[]) => "#" + rgb.map((v) => Math.round(Math.max(0, Math.min(255, v))).toString(16).padStart(2, "0")).join("");
const mix = (a: string, b: string, t: number) => {
  const x = hexToRgb(a), y = hexToRgb(b);
  return toHex(x.map((v, i) => v + (y[i] - v) * t));
};
export const isHex = (v: string) => /^#?[0-9a-fA-F]{6}$/.test(v.trim());

/** Seçilen tek renkten 50–950 ölçeği: renk 600'e oturur, açık tonlar beyazla, koyular siyahla karışır. */
export function scaleFromBase(base: string): Scale {
  const b = base.startsWith("#") ? base : "#" + base;
  const w = "#ffffff", k = "#0b0b0f";
  return {
    50: mix(b, w, 0.94), 100: mix(b, w, 0.87), 200: mix(b, w, 0.74), 300: mix(b, w, 0.56), 400: mix(b, w, 0.34),
    500: mix(b, w, 0.14), 600: b, 700: mix(b, k, 0.16), 800: mix(b, k, 0.32), 900: mix(b, k, 0.46), 950: mix(b, k, 0.64),
  };
}

/** Basit kontrast kontrolü: beyaz yazı bu zemin üzerinde okunur mu (WCAG ~4.5:1)? */
export function whiteTextContrast(hex: string): number {
  const lum = (c: number) => { const v = c / 255; return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4; };
  const [r, g, b] = hexToRgb(hex);
  const L = 0.2126 * lum(r) + 0.7152 * lum(g) + 0.0722 * lum(b);
  return 1.05 / (L + 0.05);
}

// Koyu tema eşlemeleri: açık zemin tonları koyuya, koyu yazı tonları açığa döner.
const NEUTRAL_DARK: Record<Shade, Shade> = { 50: 950, 100: 800, 200: 700, 300: 600, 400: 500, 500: 400, 600: 300, 700: 200, 800: 100, 900: 50, 950: 50 };
const COLOR_DARK: Record<Shade, Shade> = { 50: 950, 100: 900, 200: 800, 300: 700, 400: 400, 500: 500, 600: 500, 700: 300, 800: 200, 900: 100, 950: 50 };
const remap = (sc: Scale, map: Record<Shade, Shade>): Scale => Object.fromEntries(SHADES.map((k) => [k, sc[map[k]]])) as Scale;

export function accentScale(t: ThemeSettings): Scale {
  if (t.accent === "custom" && isHex(t.customColor)) return scaleFromBase(t.customColor);
  return (ACCENT_PRESETS.find((p) => p.id === t.accent) ?? ACCENT_PRESETS[0]).scale;
}

function sidebarScale(style: SidebarStyle, accent: Scale): Scale {
  const sl = BASE.slate;
  if (style === "light") {
    return { 50: sl[900], 100: sl[800], 200: sl[700], 300: sl[700], 400: sl[500], 500: sl[500], 600: sl[300], 700: sl[200], 800: sl[100], 900: "#ffffff", 950: sl[50] };
  }
  if (style === "accent") {
    return { 50: "#ffffff", 100: accent[50], 200: accent[100], 300: accent[200], 400: accent[300], 500: accent[300], 600: accent[700], 700: accent[800], 800: accent[900], 900: accent[950], 950: accent[950] };
  }
  return sl;
}

export function resolveDark(mode: ThemeMode): boolean {
  if (mode === "system") return typeof window !== "undefined" && window.matchMedia("(prefers-color-scheme: dark)").matches;
  return mode === "dark";
}

const triplet = (hex: string) => hexToRgb(hex).join(" ");

/** Değişkenleri <html> üzerine yazar; koyu temada "dark" sınıfı ve tarayıcı denetimleri için color-scheme. */
export function applyTheme(t: ThemeSettings) {
  if (typeof document === "undefined") return;
  const root = document.documentElement;
  const dark = resolveDark(t.mode);
  const accent = accentScale(t);
  const set = (name: string, sc: Scale) => SHADES.forEach((k) => root.style.setProperty(`--c-${name}-${k}`, triplet(sc[k])));

  set("slate", dark ? remap(BASE.slate, NEUTRAL_DARK) : BASE.slate);
  for (const name of ["amber", "rose", "emerald", "red", "teal", "orange"]) set(name, dark ? remap(BASE[name], COLOR_DARK) : BASE[name]);
  set("accent", dark ? remap(accent, COLOR_DARK) : accent);
  set("side", sidebarScale(t.sidebar, accent));
  root.style.setProperty("--c-surface", triplet(dark ? BASE.slate[900] : "#ffffff"));
  root.classList.toggle("dark", dark);
  root.style.colorScheme = dark ? "dark" : "light";
}
