// Süreç testleri: rapordaki 46 senaryoyu arayüz üzerinden koşar. Veritabanı taze tohumda olmalı (scripts/restore.sh).
// Kullanım: node e2e/scenarios.js [çıktı adı] [senaryo öneki/kimlikleri, virgülle]   ör. node e2e/scenarios.js run1 Y-,S-01
const { createRun } = require("./lib");

const outName = process.argv[2] || "scenarios";
const filter = (process.argv[3] || "").split(",").filter(Boolean);
const want = (id) => !filter.length || filter.some((f) => id === f || (f.endsWith("-") && id.startsWith(f)));

(async () => {
  const run = createRun(outName);
  const groups = ["sc_admin", "sc_buyer", "sc_supplier", "sc_misc"].flatMap((g) => {
    try { return require("./" + g)(run); } catch (e) { if (e.code !== "MODULE_NOT_FOUND") throw e; return []; }
  });
  await run.start();
  // Dev sunucusu ilk istekte sayfaları derler; ilk senaryonun girişi zaman aşımına düşmesin diye ısıt.
  for (const r of ["/login", "/admin", "/supplier"]) await fetch("http://localhost:3000" + r).catch(() => {});
  for (const [id, title, fn] of groups) if (want(id)) await run.scenario(id, title, fn);
  await run.stop();
  const ok = run.results.filter((r) => r.ok).length;
  console.log(`\n${ok}/${run.results.length} senaryo geçti; bulgu: ${run.results.reduce((n, r) => n + r.findings.length, 0)}`);
})();
