import type { Config } from "tailwindcss";

// Renk aileleri CSS değişkenlerine bağlı: tema (açık/koyu), vurgu paleti ve menü stili çalışma anında değişir
// (app/utils/theme.ts). "indigo" sınıfları vurgu rengidir; "side" kenar menüsü, "surface" kart zeminidir.
const SHADES = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950];
const scale = (name: string) =>
  Object.fromEntries(SHADES.map((s) => [s, `rgb(var(--c-${name}-${s}) / <alpha-value>)`]));

export default <Partial<Config>>{
  content: [
    "./app/components/**/*.{vue,js,ts}",
    "./app/layouts/**/*.vue",
    "./app/pages/**/*.vue",
    "./app/plugins/**/*.{js,ts}",
    "./app/app.vue",
    "./app/error.vue",
  ],
  theme: {
    extend: {
      colors: {
        slate: scale("slate"),
        indigo: scale("accent"),
        amber: scale("amber"),
        rose: scale("rose"),
        emerald: scale("emerald"),
        red: scale("red"),
        teal: scale("teal"),
        orange: scale("orange"),
        side: scale("side"),
        surface: "rgb(var(--c-surface) / <alpha-value>)",
      },
    },
  },
};
