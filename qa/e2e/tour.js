// Arayüz turu: rollere göre ana ekranların masaüstü ve mobil görüntüsü (UI/UX değerlendirmesi için).
// Kullanım: node e2e/tour.js [çıktı klasörü adı]
const { createRun, fx } = require("./lib");

const A = fx.auctions, R = fx.rfi, Q = fx.rfq;
const C = (n) => fx.companies[n].id;
const order = (n) => fx.orders.find((o) => o.number === n).id;

const PLAN = [
  ["admin@procurelive.local", [
    ["/admin", "panel"], ["/admin/auctions", "ihaleler"], ["/admin/auctions/create", "sihirbaz"], [`/admin/auctions/${A.A4}`, "ihale-canli"],
    [`/admin/auctions/${A.A8}`, "ihale-sona-erdi"], [`/admin/auctions/${A.A1}`, "ihale-taslak-onay"], ["/admin/rfq", "rfq-liste"], [`/admin/rfq/${Q.Q4}`, "rfq-detay"],
    [`/admin/rfi/${R.RFI5}`, "rfi-detay"], ["/admin/companies", "cariler"], [`/admin/companies/${C("Ege Bilişim Teknolojileri A.Ş.")}`, "cari-kart"],
    ["/admin/products", "urunler"], ["/admin/users", "kullanicilar"], ["/admin/permissions", "yetkiler"], ["/admin/firms", "firmalar"],
    ["/admin/orders", "siparisler"], [`/admin/orders/${order("PO-2026-0005")}`, "siparis-taslak"], ["/admin/monitoring", "izleme"],
    ["/admin/audit-log", "denetim"], ["/settings", "ayarlar"]]],
  ["teklif@egebilisim.example", [
    ["/supplier", "t-panel"], ["/supplier/auctions", "t-ihaleler"], [`/supplier/auctions/${A.A4}`, "t-canli-ihale"], ["/supplier/rfq", "t-rfq"],
    [`/supplier/rfq/${Q.Q2}`, "t-rfq-davet"], ["/supplier/settings", "t-ayarlar"], ["/supplier/pricing", "t-paketler"]]],
  ["satis@anadoluofis.example", [[`/supplier/auctions/${A.A13}`, "t-kota-dolu"], ["/supplier/orders", "t-siparisler"]]],
];
const MOBILE = [
  ["ahmet.yilmaz@kuzey.local", [["/admin", "m-panel"], ["/admin/auctions", "m-ihaleler"], [`/admin/auctions/${A.A4}`, "m-ihale"], ["/admin/orders", "m-siparisler"]]],
  ["teklif@egebilisim.example", [["/supplier", "m-t-panel"], [`/supplier/auctions/${A.A4}`, "m-t-ihale"]]],
];

(async () => {
  const run = createRun(process.argv[2] || "tour");
  await run.start();
  for (const [mobile, plan] of [[false, PLAN], [true, MOBILE]]) {
    for (const [email, pages] of plan) {
      const page = await run.login(email, { mobile });
      for (const [route, name] of pages) {
        await page.goto("http://localhost:3000" + route);
        await run.settle(page, 900);
        const f = await run.shot(page, name);
        const over = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1);
        console.log(f, over ? "TAŞMA" : "", page.issues.console.slice(-1)[0] ?? "");
      }
      await page.ctx.close();
    }
  }
  await run.stop();
})();
