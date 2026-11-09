// Premium ERP (UyumERP) entegrasyonu senaryoları — sahte ERP'ye karşı (qa/erp-fake/fake_erp.py, port 8099).
// R-01 premium kapalıyken çalıştırılır (API Features__ErpIntegration=false), diğerleri açıkken.
// Kullanım: node e2e/erp.js <çıktı> [R-02,R-03,...]
const { createRun, api, token, WEB } = require("./lib");
const { execFileSync } = require("child_process");

// 127.0.0.1: Windows'ta "localhost" önce IPv6 denenir, sahte ERP yalnızca IPv4'te dinler (her çağrıya saniyeler eklenir).
const FAKE = "http://127.0.0.1:8099";
const sql = (q) => execFileSync("docker", ["exec", "procurelive-postgres", "psql", "-tA", "-U", "procurelive_user", "-d", "procurelive", "-c", q]).toString().trim();
const fake = async () => (await fetch(`${FAKE}/_requests`)).json();
const CONN = {
  getServiceUrl: `${FAKE}/WebService/ERP/UyumGetWebService.asmx`, saveServiceUrl: `${FAKE}/WebService/ERP/UyumSaveWebService.asmx`,
  username: "test.user", password: "test.pass", coCode: "01", branchCode: "0101", offerDocTraCode: "STK", orderDocTraCode: "SAS",
  docNoPrefix: "PL-", entityCodePrefix: "320", exportNewSuppliers: false, exportNewProducts: false,
};
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
async function until(fn, ms = 40000, step = 2000) {
  const end = Date.now() + ms;
  while (Date.now() < end) {
    const v = await fn();
    if (v) return v;
    await sleep(step);
  }
  return null;
}

(async () => {
  const run = createRun(process.argv[2] || "erp");
  const only = (process.argv[3] || "").split(",").filter(Boolean);
  const scenario = (id, title, fn) => (only.length && !only.includes(id) ? null : run.scenario(id, title, fn));
  await run.start();
  const admin = await token("admin@procurelive.local", "ChangeMe123!");
  // Canlı ve Ege'nin teklif verdiği bir ihale; kalemleri ürünlü.
  const liveAuction = sql(`select a."Id" from "Auctions" a join "AuctionInvitations" v on v."AuctionId" = a."Id" join "Companies" c on c."Id" = v."CompanyId"
    where a."Status" = 2 and c."Name" like 'Ege Bilişim%'
      and exists (select 1 from "Bids" b join "AuctionItems" bi on bi."Id" = b."AuctionItemId" where bi."AuctionId" = a."Id" and b."CompanyId" = c."Id")
      and exists (select 1 from "AuctionItems" i where i."AuctionId" = a."Id" and i."ProductId" is not null) order by a."Title" limit 1`);
  if (!liveAuction) throw new Error("Ege'nin katıldığı canlı ihale bulunamadı");

  await scenario("R-01", "Premium kapalı: ERP yalnızca tanıtım olarak görünür, uygulama ERP'siz çalışır", async (t) => {
    const p = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    const link = p.getByRole("link", { name: /ERP Entegrasyonu/ });
    t.step("Menüde ERP bağlantısı Premium rozetli", /Premium/i.test(await link.innerText()), await link.innerText());
    await link.click();
    await p.getByTestId("erp-premium").waitFor();
    t.step("Ekranda premium tanıtımı var, form yok", (await p.getByTestId("erp-form").count()) === 0);
    await t.shot(p, "premium");
    await p.goto(WEB + "/admin/companies");
    await t.settle(p);
    t.step("Carilerde ERP'den aktar düğmesi yok", (await p.getByTestId("erp-import-entities").count()) === 0);
    await p.goto(WEB + `/admin/auctions/${liveAuction}`);
    await t.settle(p, 1200);
    t.step("İhalede ERP kartı yok", (await p.getByTestId("erp-card").count()) === 0);
    t.step("API 403 PremiumRequired", (await api("GET", "/api/erp/settings", admin)).data?.code === "PremiumRequired");
  });

  await scenario("R-02", "Admin bağlantıyı test eder ve Simülasyon modunda kaydeder", async (t) => {
    await fetch(`${FAKE}/_reset`, { method: "POST" });
    await api("PUT", "/api/erp/settings", admin, { ...CONN, mode: "Off", getServiceUrl: "", username: "", password: null, coCode: "" });
    const p = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    await p.goto(WEB + "/admin/erp");
    await p.getByTestId("erp-form").waitFor();
    await t.settle(p);
    await p.getByTestId("erp-mode").locator('input[data-mode="Simulation"]').check();
    await p.getByTestId("erp-get-url").fill(CONN.getServiceUrl);
    await p.getByTestId("erp-save-url").fill(CONN.saveServiceUrl);
    await p.getByTestId("erp-username").fill(CONN.username);
    await p.getByTestId("erp-password").fill("yanlis");
    await p.getByTestId("erp-co-code").fill(CONN.coCode);
    await p.getByTestId("erp-branch-code").fill(CONN.branchCode);
    await p.getByTestId("erp-offer-doctra").fill(CONN.offerDocTraCode);
    await p.getByTestId("erp-order-doctra").fill(CONN.orderDocTraCode);
    await p.getByTestId("erp-test").click();
    await p.getByTestId("erp-test-result").waitFor();
    t.step("Yanlış şifreyle test anlaşılır hata verir", /Bağlanılamadı.*şifre/i.test(await p.getByTestId("erp-test-result").innerText()), await p.getByTestId("erp-test-result").innerText());
    await p.getByTestId("erp-password").fill(CONN.password);
    await p.getByTestId("erp-test").click();
    await p.getByText(/Bağlantı başarılı/).waitFor();
    t.step("Doğru şifreyle test başarılı ve sayıları gösterir", /\d+ cari, \d+ stok/.test(await p.getByTestId("erp-test-result").innerText()), await p.getByTestId("erp-test-result").innerText());
    const put = p.waitForResponse((r) => r.url().endsWith("/api/erp/settings") && r.request().method() === "PUT");
    await p.getByTestId("erp-save").click();
    t.step("Kayıt 204", (await put).status() === 204);
    await t.settle(p);
    await t.shot(p, "simulasyon");
    const dto = (await api("GET", "/api/erp/settings", admin)).data;
    t.step("Mod Simülasyon, şifre dönmüyor", dto.mode === "Simulation" && dto.hasPassword === true && !JSON.stringify(dto).includes("test.pass"));
    t.step("Veritabanında şifre düz metin değil", !sql(`select "PasswordProtected" from "ErpSettings"`).includes("test.pass"));
  });

  await scenario("R-03", "ERP'den cari ve ürün aktarımı (eşleme, kopya açmama)", async (t) => {
    const p = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    await p.goto(WEB + "/admin/companies");
    await p.getByTestId("erp-import-entities").click();
    const d = p.getByTestId("erp-import-dialog");
    await d.waitFor();
    await d.locator('input[data-code="320.01.001"]').waitFor();
    t.step("Önek süzgeci: yalnızca 320 carileri listelenir", (await d.locator("input[data-code^='120']").count()) === 0);
    await p.getByTestId("erp-import-search").fill("ege");
    await d.locator('input[data-code="320.01.003"]').waitFor();
    await d.locator('input[data-code="320.01.003"]').check();
    await p.getByTestId("erp-import-search").fill("");
    await d.locator('input[data-code="320.01.002"]').waitFor();
    await d.locator('input[data-code="320.01.002"]').check();
    await t.shot(p, "cari-aktar");
    await p.getByTestId("erp-import-run").click();
    await p.getByTestId("erp-import-result").waitFor();
    const res = await p.getByTestId("erp-import-result").innerText();
    t.step("1 yeni cari, 1 eşleme (Ege kopyalanmadı)", /1 yeni, 1 eşlendi/.test(res), res);
    t.step("Ege Bilişim tek kayıt ve ERP kodlu", sql(`select count(*) || '|' || max("ErpEntityCode") from "Companies" where "Name" like 'Ege Bilişim%'`) === "1|320.01.003");
    t.step("Listede eşleşmiş rozeti", !!(await until(async () => (await d.getByText("Eşleşmiş").count()) >= 2, 10000, 500)));
    await p.keyboard.press("Escape");
    await p.goto(WEB + "/admin/products");
    await p.getByTestId("erp-import-items").click();
    const di = p.getByTestId("erp-import-dialog");
    await di.locator('input[data-code="TRF-1000-KT"]').waitFor();
    await di.locator('input[data-code="TRF-1000-KT"]').check();
    await p.getByTestId("erp-import-run").click();
    await p.getByTestId("erp-import-result").waitFor();
    t.step("Ürün aktarıldı", /1 yeni/.test(await p.getByTestId("erp-import-result").innerText()));
    t.step("Ürün ERP kodu ve birimi doğru", sql(`select "ErpItemCode" || '|' || "UnitOfMeasure" from "Products" where "Sku" = 'TRF-1000-KT'`) === "TRF-1000-KT|0");
  });

  await scenario("R-04", "Simülasyon: yayınlanmış ihalede ERP teklifleri hazırlanır, gönderilmez", async (t) => {
    const ok = await until(async () => Number(sql(`select count(*) from "ErpOfferLinks" where "AuctionId" = '${liveAuction}'`)) > 0);
    t.step("İhale için ERP teklif bağlantıları oluştu", !!ok);
    const p = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    await p.goto(WEB + `/admin/auctions/${liveAuction}`);
    const card = p.getByTestId("erp-card");
    await card.waitFor({ timeout: 20000 });
    await card.scrollIntoViewIfNeeded();
    await t.settle(p, 800);
    const txt = await card.innerText();
    t.step("İhale ERP kartında tedarikçi teklifleri ve Simülasyon rozeti", /Simülasyon/.test(txt) && /PL-/.test(txt) && /Simüle edildi/.test(txt), txt.slice(0, 200));
    await t.shot(p, "ihale-kart");
    t.step("Sahte ERP'ye yazma isteği gitmedi", (await fake()).requests.every((r) => r.op === "GetDataTableWithSQL"));
    await p.goto(WEB + "/admin/erp?status=Simulated");
    await p.getByTestId("erp-jobs-table").waitFor();
    await t.settle(p, 800);
    const row = p.getByTestId("erp-job-row").filter({ hasText: "Teklif aç" }).first();
    await row.getByRole("button", { name: "Ayrıntı" }).click();
    const det = p.getByTestId("erp-job-detail");
    await det.waitFor();
    const xml = await det.innerText();
    t.step("XML: SaveOffer, hareket türü ve maskeli şifre", /SaveOffer/.test(xml) && /<tem:DocTraCode>STK</.test(xml) && /\*\*\*/.test(xml) && !/test\.pass/.test(xml));
    await t.shot(p, "xml");
  });

  await scenario("R-05", "Canlı: teklif açma, teklifle revizyon ve ERP siparişi", async (t) => {
    const p = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    await p.goto(WEB + "/admin/erp");
    await p.getByTestId("erp-form").waitFor();
    await t.settle(p);
    await p.getByTestId("erp-mode").locator('input[data-mode="Live"]').check();
    await p.getByRole("switch").first().click(); // tedarikçileri otomatik aç
    await p.getByRole("switch").nth(1).click(); // ürünleri otomatik aç
    await p.getByTestId("erp-save").click();
    await p.getByRole("button", { name: "Canlıya geç" }).click();
    await t.settle(p);
    t.step("Canlıya geçiş onay sordu ve kaydedildi", (await api("GET", "/api/erp/settings", admin)).data.mode === "Live");
    // Simülasyonda hazırlanan teklifler canlıda yeniden açılsın: bağlantıları sıfırla (gerçek kullanımda simülasyon ayrı ihalelerde denenir).
    sql(`delete from "ErpOfferLinks"; delete from "ErpSyncJobs" where "Status" = 1;`);
    const opened = await until(async () => Object.keys((await fake()).offers).length > 0 && Number(sql(`select count(*) from "ErpSyncJobs" where "Status" = 0`)) === 0, 120000, 4000);
    const f1 = await fake();
    t.step("ERP'de teklifler açıldı", Object.keys(f1.offers).length > 0, Object.keys(f1.offers).length);
    t.step("Kuyrukta başarısız iş yok", sql(`select count(*) from "ErpSyncJobs" where "Status" = 3`) === "0", sql(`select string_agg("Message", ' | ') from "ErpSyncJobs" where "Status" = 3`));

    const item = sql(`select i."Id" from "AuctionItems" i where i."AuctionId" = '${liveAuction}' order by i."SortOrder" limit 1`);
    const best = Number(sql(`select min(b."Amount") from "Bids" b where b."AuctionItemId" = '${item}'`));
    const cur = sql(`select "CurrencyId" from "Auctions" where "Id" = '${liveAuction}'`);
    const ege = await token("teklif@egebilisim.example", "Test1234!");
    const amount = Math.floor(best * 0.97);
    const bid = await api("POST", `/api/auctions/${liveAuction}/items/${item}/bids`, ege, { amount, currencyId: cur });
    t.step("Ege yeni teklif verdi", bid.status === 200, `${bid.status} ${JSON.stringify(bid.data).slice(0, 120)}`);
    const rev = await until(async () => (await fake()).revisions.some((r) => Object.values(r.prices).includes(String(amount))), 40000);
    t.step("Yeni teklif ERP teklifini revize etti", !!rev, amount);

    const po = sql(`select "Id" from "PurchaseOrders" where "Status" in (1,2) and "ErpOrderDocNo" is null order by "OrderNumber" limit 1`);
    await p.goto(WEB + `/admin/orders/${po}`);
    const card = p.getByTestId("erp-card");
    await card.waitFor({ timeout: 20000 });
    await card.getByTestId("erp-action").click();
    const code = await until(async () => /PL-PO-/.test(await card.getByTestId("erp-code").innerText().catch(() => "")), 40000);
    t.step("ERP'de sipariş oluştu ve belge no kartta", !!code, await card.innerText());
    t.step("Siparişte tekrar oluştur düğmesi yok", (await card.getByTestId("erp-action").count()) === 0);
    await t.shot(p, "siparis");
    t.step("Sahte ERP'de sipariş SAS hareket türüyle", Object.values((await fake()).orders).some((o) => o.docTra === "SAS"));
  });

  await scenario("R-06", "Yetki ve görünüm: satınalmacı, mobil, İngilizce", async (t) => {
    const b = await t.login("ahmet.yilmaz@kuzey.local");
    t.step("Satınalmacı menüsünde ERP yok", (await b.getByRole("link", { name: /ERP Entegrasyonu/ }).count()) === 0);
    await b.goto(WEB + "/admin/erp");
    await t.settle(b);
    t.step("Satınalmacı ERP ekranına yönlendirilir", !b.url().includes("/admin/erp"), b.url());
    await b.goto(WEB + "/admin/companies");
    await t.settle(b);
    t.step("Satınalmacıda ERP'den aktar düğmesi yok", (await b.getByTestId("erp-import-entities").count()) === 0);
    const buyer = await token("ahmet.yilmaz@kuzey.local", "Test1234!");
    t.step("Satınalmacı ERP ayar API 403", (await api("GET", "/api/erp/settings", buyer)).status === 403);
    const m = await t.login("admin@procurelive.local", { password: "ChangeMe123!", mobile: true });
    await m.goto(WEB + "/admin/erp");
    await m.getByTestId("erp-form").waitFor();
    await t.settle(m, 800);
    t.step("Mobilde yatay taşma yok", (await m.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)) <= 1);
    await t.shot(m, "mobil");
    const e = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    await e.evaluate(() => localStorage.setItem("procurelive-locale", "en"));
    await e.goto(WEB + "/admin/erp");
    await e.getByTestId("erp-form").waitFor();
    await t.settle(e, 800);
    const text = await e.locator("main").innerText();
    t.step("İngilizcede çevrilmemiş anahtar yok", !/erp\.[a-zA-Z]/.test(text), (text.match(/erp\.[\w.]+/) || [""])[0]);
    await t.shot(e, "ingilizce");
  });

  await run.stop();
  const okCount = run.results.filter((r) => r.ok).length;
  console.log(`\n${okCount}/${run.results.length} senaryo geçti; bulgu: ${run.results.reduce((n, r) => n + r.findings.length, 0)}`);
})();
