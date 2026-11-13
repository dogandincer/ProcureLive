// Kılavuz yazılırken bulunan üç konunun senaryoları: sihirbazda "Oluştur ve Yayınla" / "Taslak Olarak Kaydet",
// rezerv fiyatın kazanan seçimine etkisi, RFQ formundaki etiketler ve kapalı zarfta "güncel en iyi" teklif.
// Veritabanı taze tohumda olmalı (scripts/restore.sh). Kullanım: node e2e/followup.js <çıktı> [F-01,F-03,...]
const { createRun, api, token, notifications, fx } = require("./lib");
const { go, selectContaining, mainText } = require("./ui");
const { execFileSync } = require("child_process");

const sql = (q) => execFileSync("docker", ["exec", "procurelive-postgres", "psql", "-tA", "-U", "procurelive_user", "-d", "procurelive", "-c", q]).toString().trim();
const pad = (n) => String(n).padStart(2, "0");
const localDT = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
const days = (n) => new Date(Date.now() + n * 86400e3);
const A = fx.auctions;
const SUPPLIER = "teklif@egebilisim.example";

/** Sihirbazı özet adımına kadar doldurur (1 tedarikçi, 1 kalem). */
async function fillWizard(p, t, { title, start, end, reserve }) {
  await go(p, "/admin/auctions/create");
  await p.getByPlaceholder("Örn: 2026 Q1 Ofis Malzemeleri Alımı").fill(title);
  const owner = p.locator("label", { hasText: "Açan Birim" }).locator("xpath=following-sibling::select[1]");
  await selectContaining(owner, "Kuzey Holding");
  const dts = p.locator('input[type="datetime-local"]');
  await dts.nth(0).fill(localDT(start));
  await dts.nth(1).fill(localDT(end));
  if (reserve != null) await p.getByTestId("tender-reserve").fill(String(reserve));
  await p.getByRole("button", { name: "İleri" }).click();
  await p.getByPlaceholder("Cari ara (unvan / vergi no)...").fill("Bilişim");
  await p.waitForTimeout(500);
  await p.locator("label", { hasText: "Ege Bilişim" }).first().click();
  await p.getByRole("button", { name: "İleri" }).click();
  await p.getByRole("button", { name: "Ürün Havuzunu Aç" }).click();
  await p.getByPlaceholder("Ürün adı veya stok kodu ara...").fill("Monitör");
  await p.waitForTimeout(700);
  await p.getByRole("button", { name: "Ekle", exact: true }).first().click();
  await p.keyboard.press("Escape");
  await p.waitForTimeout(300);
  await p.getByRole("button", { name: "İleri" }).click();
  await p.getByRole("button", { name: "İleri" }).click();
  await t.settle(p);
}

const created = async (p) => {
  await p.waitForURL(/\/admin\/auctions\/[0-9a-f-]{36}/, { timeout: 15000 });
  return p.url().split("/").pop().split("#")[0];
};

(async () => {
  const run = createRun(process.argv[2] || "followup");
  const only = (process.argv[3] || "").split(",").filter(Boolean);
  const scenario = (id, title, fn) => (only.length && !only.includes(id) ? null : run.scenario(id, title, fn));
  await run.start();
  for (const r of ["/login", "/admin", "/admin/auctions/create"]) await fetch("http://localhost:3000" + r).catch(() => {});
  const admin = await token("admin@procurelive.local");

  await scenario("F-01", "Sihirbaz: Taslak Olarak Kaydet yalnızca taslak oluşturur, tedarikçi görmez", async (t) => {
    const p = await t.login("admin@procurelive.local");
    await fillWizard(p, t, { title: "FT Taslak İhale", start: days(3), end: days(4) });
    const text = await mainText(p);
    t.step("özet adımı iki seçeneği ve ne yaptıklarını açıklıyor",
      /Oluştur ve Yayınla/.test(text) && /Taslak Olarak Kaydet/.test(text) && /tedarikçiler görmez/.test(text) && !/İhaleyi Başlat/.test(text));
    await t.shot(p, "review");
    await p.getByTestId("tender-create-draft").click();
    const id = await created(p);
    await t.settle(p, 800);
    const d = await api("GET", `/api/auctions/${id}`, admin);
    t.step("ihale Taslak durumunda", d.data.status === "Draft", d.data.status);
    t.step("taslak olduğu söylendi", /taslağı oluşturuldu/.test(await p.locator("body").innerText()));
    const n = await notifications(SUPPLIER);
    t.step("tedarikçiye bildirim gitmedi", !n.some((x) => x.relatedEntityId === id));
    const s = await api("GET", `/api/auctions/${id}`, await token(SUPPLIER));
    t.step("tedarikçi taslağı göremiyor (404)", s.status === 404, s.status);
    await t.shot(p, "draft");
  });

  await scenario("F-02", "Sihirbaz: Oluştur ve Yayınla ihaleyi planlar, davet bildirimi gider; geçmiş başlangıçta yayın kapalı", async (t) => {
    const p = await t.login("admin@procurelive.local");
    const start = days(2), end = days(3);
    await fillWizard(p, t, { title: "FT Yayınlanan İhale", start: new Date(Date.now() - 3600e3), end, reserve: 250000 });
    t.step("başlangıç geçmişteyse uyarı çıkar ve yayın düğmesi kapalıdır",
      (await p.getByTestId("tender-start-past").count()) === 1 && (await p.getByTestId("tender-create-publish").isDisabled()) && (await p.getByTestId("tender-create-draft").isEnabled()));
    await t.shot(p, "past-start");
    await p.getByRole("button", { name: "Genel Bilgiler" }).click();
    await p.locator('input[type="datetime-local"]').nth(0).fill(localDT(start));
    await p.getByRole("button", { name: "Özet & Oluştur" }).click();
    t.step("ileri bir başlangıçla yayın düğmesi açılır", await p.getByTestId("tender-create-publish").isEnabled());
    await p.getByTestId("tender-create-publish").click();
    const id = await created(p);
    await t.settle(p, 800);
    const d = await api("GET", `/api/auctions/${id}`, admin);
    t.step("ihale Planlandı", d.data.status === "Scheduled", d.data.status);
    t.step("başlangıç sihirbazdaki zaman", Math.abs(new Date(d.data.startAt) - new Date(localDT(start))) < 60e3, d.data.startAt);
    t.step("yayınlandığı söylendi", /oluşturuldu ve yayınlandı/.test(await p.locator("body").innerText()));
    const n = await notifications(SUPPLIER);
    t.step("davetli tedarikçiye davet bildirimi gitti", n.some((x) => x.relatedEntityId === id), n.slice(0, 3).map((x) => x.title).join(" | "));
    t.step("rezerv fiyat kaydedildi, teklif yokken durumu 'Henüz teklif yok'", d.data.reservePrice === 250000 && d.data.reserveStatus === "NoBids", `${d.data.reservePrice} ${d.data.reserveStatus}`);
    t.step("ihale sayfasında rezerv notu var", (await p.getByTestId("reserve-note").getAttribute("data-status")) === "NoBids");
    const s = await api("GET", `/api/auctions/${id}`, await token(SUPPLIER));
    t.step("tedarikçiye rezerv fiyat ve durumu dönmez", s.status === 200 && s.data.reservePrice == null && s.data.reserveStatus == null && s.data.reserveTotal == null,
      JSON.stringify({ r: s.data.reservePrice, s: s.data.reserveStatus, t: s.data.reserveTotal }));
    await t.shot(p, "scheduled");
  });

  await scenario("F-03", "Rezerv fiyat karşılanmıyorsa kazanan seçimi ayrıca onay ister", async (t) => {
    const id = A.A8;
    const before = (await api("GET", `/api/auctions/${id}`, admin)).data;
    if (before.status !== "Ended") throw new Error(`A8 Ended değil: ${before.status} (taze tohum gerekir)`);
    const lowerWins = before.auctionType !== "English";
    const bidItems = before.items.filter((i) => i.bidCount > 0);
    const total = bidItems.reduce((s, i) => s + i.currentBestAmount * i.quantity, 0);
    // Karşılanmayan rezerv: düşük teklif kazanıyorsa toplamın altında, artan ihalede toplamın üstünde.
    const unmet = Math.round(lowerWins ? total * 0.5 : total * 2);
    sql(`update "Auctions" set "ReservePrice" = ${unmet} where "Id" = '${id}'`);

    const d = (await api("GET", `/api/auctions/${id}`, admin)).data;
    t.step("sunucu toplamı hesaplar ve rezervin karşılanmadığını söyler", d.reserveStatus === "NotMet" && Math.abs(d.reserveTotal - total) < 0.01 && d.reserveCoveredItems === bidItems.length,
      `${d.reserveStatus} toplam=${d.reserveTotal} beklenen=${total} kalem=${d.reserveCoveredItems}/${bidItems.length}`);

    const p = await t.login("admin@procurelive.local");
    await go(p, `/admin/auctions/${id}`);
    await p.getByTestId("reserve-note").waitFor({ timeout: 10000 });
    t.step("ihale sayfasında 'Karşılanmadı' görünür", (await p.getByTestId("reserve-note").getAttribute("data-status")) === "NotMet" && /Karşılanmadı/.test(await p.getByTestId("reserve-status").innerText()));
    await t.shot(p, "not-met");

    const panel = p.getByTestId("bid-comparison").filter({ has: p.getByRole("button", { name: "Kazanan Seç" }) }).first();
    await panel.getByRole("button", { name: "Kazanan Seç" }).first().click();
    const dlg = p.getByTestId("confirm-dialog");
    await dlg.getByRole("button", { name: "Kazanan Seç" }).click();
    await p.getByTestId("reserve-warning").waitFor({ timeout: 8000 });
    t.step("ilk onayda seçim yapılmaz, rezerv uyarısı çıkar", sql(`select count(*) from "AuctionAwards" where "AuctionId" = '${id}'`) === "0" && /Yine de Kazanan Seç/.test(await dlg.innerText()));
    await t.shot(p, "warning", { full: false });
    await dlg.getByRole("button", { name: "Yine de Kazanan Seç" }).click();
    await dlg.waitFor({ state: "detached", timeout: 10000 });
    await t.settle(p, 800);
    const note = sql(`select coalesce("Notes", '') from "AuctionAwards" where "AuctionId" = '${id}'`);
    t.step("ikinci onayla kazanan seçilir ve kayıt not düşer", /Rezerv fiyat karşılanmadan seçildi/.test(note), note);

    const rest = (await api("GET", `/api/auctions/${id}`, admin)).data;
    const awardedItems = sql(`select "AuctionItemId" from "AuctionAwards" where "AuctionId" = '${id}'`).split("\n");
    const next = rest.items.find((i) => i.bidCount > 0 && !awardedItems.includes(i.id));
    if (!next) { t.step("ikinci kalem yok; API reddi ve karşılanan rezerv adımları atlandı", true); return; }
    const bids = (await api("GET", `/api/auctions/${id}/items/${next.id}/bids/compare`, admin)).data;
    const refused = await api("POST", `/api/auctions/${id}/items/${next.id}/award`, admin, { winningBidId: bids[0].bidId, notes: null });
    t.step("API onaysız seçimi 409 ReserveNotMet ile reddeder", refused.status === 409 && refused.data?.code === "ReserveNotMet", `${refused.status} ${refused.data?.code}`);

    // Rezerv karşılanınca uyarı çıkmaz.
    sql(`update "Auctions" set "ReservePrice" = ${Math.round(lowerWins ? total * 10 : 1)} where "Id" = '${id}'`);
    await p.reload();
    await p.getByTestId("reserve-note").waitFor({ timeout: 10000 });
    const status = await p.getByTestId("reserve-note").getAttribute("data-status");
    t.step("rezerv karşılanınca durum 'Karşılandı' (teklifsiz kalem varsa 'Kesin değil')", ["Met", "Incomplete"].includes(status), status);
    const panel2 = p.getByTestId("bid-comparison").filter({ has: p.getByRole("button", { name: "Kazanan Seç" }) }).first();
    await panel2.getByRole("button", { name: "Kazanan Seç" }).first().click();
    await dlg.getByRole("button", { name: "Kazanan Seç" }).click();
    await dlg.waitFor({ state: "detached", timeout: 10000 });
    t.step("uyarısız seçildi", sql(`select count(*) from "AuctionAwards" where "AuctionId" = '${id}' and "Notes" is null`) === "1");
    await t.shot(p, "met");
  });

  await scenario("F-04", "Yeni RFQ formunda başlık, para birimi ve son teklif tarihi etiketli", async (t) => {
    const p = await t.login("selin.demir@kuzey.local");
    await go(p, "/admin/rfq");
    await p.getByRole("button", { name: /Yeni RFQ/ }).click();
    const form = p.locator("form").filter({ has: p.getByTestId("rfq-title") });
    for (const label of ["Başlık", "Para birimi", "Son teklif tarihi"]) {
      const n = await form.locator("label", { hasText: label }).locator("input, select").count();
      t.step(`"${label}" etiketi alanına bağlı`, n === 1, n);
    }
    t.step("son tarih ipucu var", /Tedarikçiler bu tarihe kadar teklif verebilir/.test(await form.innerText()));
    await t.shot(p, "form");
    const due = days(7);
    await form.getByLabel("Başlık").fill("FT Etiketli RFQ");
    await form.getByLabel("Son teklif tarihi").fill(localDT(due));
    await form.getByRole("button", { name: /Oluştur ve Devam Et/ }).click();
    await p.waitForURL(/\/admin\/rfq\/[0-9a-f-]{36}/, { timeout: 15000 });
    const id = p.url().split("/").pop();
    const d = await api("GET", `/api/rfq/processes/${id}`, await token("selin.demir@kuzey.local"));
    t.step("RFQ etiketli alanlarla oluştu", d.data.title === "FT Etiketli RFQ" && Math.abs(new Date(d.data.dueDate) - new Date(localDT(due))) < 60e3, `${d.data.title} ${d.data.dueDate}`);
  });

  await scenario("F-05", "Kapalı zarfta 'güncel en iyi' en düşük tekliftir", async (t) => {
    // Kapalı zarfta teklifler birbirini geçmek zorunda değildir: 100 → 80 → 120 sırasıyla verilen tekliflerin en iyisi 80'dir.
    const id = A.A11;
    const supplierEmail = "operasyon@akdenizlojistik.example";
    const before = (await api("GET", `/api/auctions/${id}`, admin)).data;
    if (before.auctionType !== "SealedBid" || before.status !== "Scheduled") throw new Error(`A11 beklenen durumda değil: ${before.auctionType} ${before.status}`);
    const supplier = await token(supplierEmail);
    const accepted = await api("POST", `/api/auctions/${id}/invitations/respond`, supplier, { accept: true, declineReason: null });
    t.step("tedarikçi daveti kabul etti", accepted.status < 300, `${accepted.status} ${JSON.stringify(accepted.data)}`);
    const live = await api("POST", `/api/auctions/${id}/status`, admin, { status: "Live", reason: null, newStartAt: null, newEndAt: days(1).toISOString() });
    t.step("ihale canlıya alındı", live.status < 300, `${live.status} ${JSON.stringify(live.data)}`);
    const item = before.items[0];
    for (const amount of [100, 80, 120]) {
      const r = await api("POST", `/api/auctions/${id}/items/${item.id}/bids`, supplier, { amount, currencyId: before.currencyId });
      t.step(`${amount} teklifi kabul edildi`, r.status < 300, `${r.status} ${JSON.stringify(r.data)}`);
    }
    const d = (await api("GET", `/api/auctions/${id}`, admin)).data;
    const now = d.items.find((i) => i.id === item.id);
    t.step("iç kullanıcıya güncel en iyi = 80", now.currentBestAmount === 80 && now.bidCount === 3, `en iyi=${now.currentBestAmount} teklif=${now.bidCount}`);
    const p = await t.login("admin@procurelive.local");
    await go(p, `/admin/auctions/${id}`);
    t.step("ihale sayfasında kalemin güncel en iyisi 80", /80,00/.test(await p.locator("main").innerText()));
    await t.shot(p, "sealed-best");
    await go(p, "/admin/monitoring");
    await t.settle(p, 800);
    const row = p.locator("tr", { hasText: before.title }).first();
    const expected = (80 * item.quantity).toLocaleString("tr-TR", { minimumFractionDigits: 2 });
    t.step("izleme ekranındaki toplam en düşük teklife göre", (await row.count()) === 1 && (await row.innerText()).includes(expected), `beklenen ${expected}: ${(await row.count()) ? (await row.innerText()).replace(/\s+/g, " ") : "satır yok"}`);
    await t.shot(p, "monitoring");
  });

  await run.stop();
  const ok = run.results.filter((r) => r.ok).length;
  console.log(`\n${ok}/${run.results.length} senaryo geçti; bulgu: ${run.results.reduce((n, r) => n + r.findings.length, 0)}`);
})();
