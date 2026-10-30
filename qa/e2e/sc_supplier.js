// Tedarikçi senaryoları (T-01 … T-05, P-01 … P-04)
const { fx, api, token, notifications } = require("./lib");
const { go, toastSeen, mainText, bodyText } = require("./ui");

const A = fx.auctions, R = fx.rfi, Q = fx.rfq;

async function itemCard(p, text) {
  return p.locator('[data-testid="supplier-item"]').filter({ hasText: text }).first();
}

module.exports = () => [
  ["T-01", "Canlı teklif", async (t) => {
    const p = await t.login("teklif@egebilisim.example");
    await go(p, `/supplier/auctions/${A.A4}`);
    const card = await itemCard(p, "Dizüstü");
    const best = Number((await card.innerText()).match(/Güncel en iyi[^\d]*([\d.]+),/)?.[1].replace(/\./g, "") ?? 36850);
    await card.locator('input[type="number"]').fill(String(best - 100));
    await card.getByRole("button", { name: "Teklif Ver" }).click();
    await p.waitForTimeout(1500);
    const txt = await card.innerText();
    t.step("düşük teklif kabul edildi ve sıra güncellendi", /Lidersiniz|Sıranız: 1/.test(txt), txt.slice(0, 300));
    await card.locator('input[type="number"]').fill(String(best + 5000));
    await card.getByRole("button", { name: "Teklif Ver" }).click();
    await p.waitForTimeout(1200);
    const err = await bodyText(p);
    t.step("yüksek teklif açık hata mesajıyla reddedildi", /daha düşük olmalıdır|en iyi/i.test(err), err.match(/Teklif[^\n]{0,120}/)?.[0]);
    await t.shot(p, "bids");
  }],

  ["T-04", "RFQ teklif ve sonuç", async (t) => {
    const p = await t.login("teklif@egebilisim.example");
    await go(p, `/supplier/rfq/${Q.Q2}`);
    const acc = p.getByRole("button", { name: "Kabul Et" });
    if (await acc.count()) { await acc.first().click(); await t.settle(p, 800); }
    const prices = p.locator('input[type="number"]');
    const n = await prices.count();
    for (let i = 0; i < n; i++) await prices.nth(i).fill(i % 2 === 0 ? String(410 + i) : "7");
    await t.shot(p, "quote");
    await p.getByRole("button", { name: /Teklifi Gönder|Gönder/ }).last().click();
    t.step("teklif gönderildi", await toastSeen(p, /gönderildi/i));
    await t.settle(p);
    const after = await mainText(p);
    t.step("Standart üye revize edemez (bilgi gösteriliyor)", /Plus/.test(after));
    await go(p, `/supplier/rfq/${Q.Q5}`);
    const res = await mainText(p);
    t.step("kaybedilen RFQ'da sonuç anlaşılır", /kazan|sonuç|seçilmedi|kaybed/i.test(res), res.slice(0, 200));
    await t.shot(p, "lost");
  }],

  ["T-02", "Son katılım hakkı", async (t) => {
    const p = await t.login("teklif@egebilisim.example");
    await go(p, `/supplier/auctions/${A.A2}`);
    await p.getByRole("button", { name: "Daveti Kabul Et" }).click().catch(async () => p.getByRole("button", { name: /Kabul/ }).first().click());
    await t.settle(p, 1000);
    const sub = await api("GET", "/api/subscription/me", await token("teklif@egebilisim.example"));
    t.step("kalan hak 0", sub.data.remainingJoins === 0, JSON.stringify(sub.data).slice(0, 160));
    await go(p, "/supplier/auctions");
    const txt = await mainText(p);
    t.step("ihale listesinde Plus'a geç uyarısı", /Plus/.test(txt), txt.slice(0, 200));
    await t.shot(p, "quota");
  }],

  ["T-03", "Kota dolu → Plus", async (t) => {
    const p = await t.login("satis@anadoluofis.example");
    await go(p, `/supplier/auctions/${A.A13}`);
    t.step("kota dolu ihale kısıtlı görünüyor", /Plus/.test(await mainText(p)) && await p.locator(".blur-sm, .blur, [class*='blur']").count() > 0);
    await t.shot(p, "restricted");
    await go(p, `/supplier/rfi/${R.RFI2}`);
    t.step("RFI daveti kısıtlı", /Plus/.test(await mainText(p)));
    await go(p, "/supplier/pricing");
    await p.getByRole("button", { name: /Satın al|Bu pakete geç/ }).first().click();
    const pay = async (num) => {
      const dlg = p.locator('[role="dialog"], .fixed.inset-0').last();
      await dlg.locator("input").nth(0).fill("Hakan Uçar");
      await dlg.locator("input").nth(1).fill(num);
      await dlg.locator("input").nth(2).fill("12/29");
      await dlg.getByRole("button", { name: /öde/i }).click();
      await p.waitForTimeout(2000);
    };
    await pay("4000 0000 0000 0002");
    t.step("0002 kartı reddedildi", /reddedildi/i.test(await bodyText(p)));
    await t.shot(p, "declined");
    await pay("4242 4242 4242 4242");
    t.step("4242 ile Satın Alım Başarılı", /Satın Alım Başarılı/.test(await bodyText(p)));
    await t.shot(p, "success");
    await go(p, `/supplier/auctions/${A.A13}`);
    t.step("Plus sonrası kısıt kalktı", await p.locator('[data-testid="supplier-item"]').count() > 0 || /Daveti Kabul Et/.test(await mainText(p)));
  }],

  ["T-05", "Firma ayarları ve KVKK", async (t) => {
    const p = await t.login("teklif@egebilisim.example");
    await go(p, "/supplier/settings");
    const toggle = p.getByRole("switch", { name: /İletişim bilgilerim/ });
    if (await toggle.count()) {
      if ((await toggle.getAttribute("aria-checked")) === "true") await toggle.click();
    } else {
      await p.getByText("İletişim bilgilerim açık görünsün").click();
    }
    await p.getByRole("button", { name: "Kaydet" }).first().click();
    t.step("ayar kaydedildi", await toastSeen(p, /kaydedildi|güncellendi/i));
    const plus = await token("kurumsal@bogazicitek.example");
    const r = await api("GET", "/api/search?q=Ege%20Bili%C5%9Fim", plus);
    const c = (r.data?.companies ?? []).find((x) => x.name.includes("Ege Bilişim"));
    t.step("aramada e-posta/telefon maskeli", c && !/@egebilisim\.example/.test(JSON.stringify(c)) && /\*/.test(JSON.stringify(c)), JSON.stringify(c ?? r.data).slice(0, 200));
    await t.shot(p, "settings");
  }],

  ["P-01", "Plus arama", async (t) => {
    const p = await t.login("kurumsal@bogazicitek.example");
    await go(p, "/supplier/search");
    const box = p.getByPlaceholder(/Şirket adı, sektör/);
    for (const term of ["kablo", "Aliağa", "Toros"]) {
      await box.fill(term);
      await p.waitForTimeout(1200);
      const txt = await mainText(p);
      t.step(`“${term}” sonuç getiriyor`, !/Sonuç bulunamadı/.test(txt), txt.slice(0, 120));
    }
    await box.fill("Kartal");
    await p.waitForTimeout(1200);
    const ins = await mainText(p);
    t.step("Kuzey İnşaat ihaleleri listelenmiyor", !/Hazır Beton|Demir Fiyat/.test(ins));
    await box.fill("Kuzey");
    await p.waitForTimeout(1200);
    const ownerBtn = p.getByRole("button", { name: /açık ihale/ }).first();
    if (await ownerBtn.count()) {
      await ownerBtn.click();
      await p.waitForTimeout(1000);
      t.step("ihale sahibine göre süzülüyor", /Firma/.test(await mainText(p)));
    } else t.step("ihale sahibine göre süzme düğmesi", false);
    await t.shot(p, "search");
  }],

  ["P-02", "Katılım talebi + ön teklif", async (t) => {
    const p = await t.login("kurumsal@bogazicitek.example");
    await go(p, `/supplier/auctions/${A.A2}`);
    const cta = p.getByRole("button", { name: /^Katılım talebi$/ });
    if (await cta.count()) { await cta.first().click(); await p.waitForTimeout(500); }
    const inputs = p.locator('input[type="number"]');
    const n = Math.min(await inputs.count(), 2);
    for (let i = 0; i < n; i++) await inputs.nth(i).fill(i === 0 ? "395" : "28500");
    await p.locator("textarea").last().fill("Teslimat 3 hafta; montaj dahil.");
    await t.shot(p, "form");
    await p.getByRole("button", { name: "Talebi gönder" }).click();
    t.step("talep gönderildi", await toastSeen(p, "Katılım talebiniz gönderildi"));
    await t.settle(p);
    t.step("“Cari onayı bekleniyor” görünüyor", /Cari onayı bekleniyor/.test(await mainText(p)));
    const n2 = await notifications("selin.demir@kuzey.local");
    t.step("Selin Demir'e bildirim", n2.some((x) => x.relatedEntityId === A.A2 && /tale[pb]/i.test(x.title + x.body)), n2.slice(0, 3).map((x) => x.title).join(" | "));
  }],

  ["P-03", "RFI cevap ve revizyon", async (t) => {
    const p = await t.login("kurumsal@bogazicitek.example");
    await go(p, `/supplier/rfi/${R.RFI2}`);
    const acc = p.getByRole("button", { name: "Kabul Et" });
    if (await acc.count()) { await acc.first().click(); await t.settle(p, 800); }
    // Yalnızca cevap formu (sayfadaki soru-cevap paneli de bir form ve "Gönder" düğmesi içeriyor).
    const answerForm = () => p.locator("form").filter({ has: p.locator('[id^="q-"]') }).first();
    const fillAll = async (suffix) => {
      const f = answerForm();
      for (const inp of await f.locator('input:not([type="number"]):not([type="file"]):not([type="checkbox"])').all()) await inp.fill("Cevap " + suffix).catch(() => {});
      for (const inp of await f.locator('input[type="number"]').all()) await inp.fill("5").catch(() => {});
      for (const sel of await f.locator("select").all()) await sel.selectOption({ index: 1 }).catch(() => {});
    };
    await fillAll("ilk");
    await answerForm().locator('button[type="submit"]').click();
    t.step("cevaplar gönderildi", await toastSeen(p, /gönderildi|kaydedildi/i));
    await t.settle(p, 800);
    await fillAll("revize");
    const [rev] = await Promise.all([
      p.waitForResponse((r) => r.url().includes("/responses") && r.request().method() === "POST", { timeout: 10000 }),
      answerForm().getByRole("button", { name: /Yanıtı Güncelle/ }).click(),
    ]);
    t.step("Plus revize edebildi", rev.ok(), String(rev.status()));
    await go(p, `/supplier/rfi/${R.RFI2}`);
    const vals = await answerForm().locator('input:not([type="number"]):not([type="file"]):not([type="checkbox"])').evaluateAll((els) => els.map((e) => e.value));
    t.step("son cevaplar formda dolu", vals.some((v) => v === "Cevap revize"), vals.join(" | ").slice(0, 160));
    await t.shot(p, "rfi");
  }],

  ["P-04", "Ön teklifin ilk teklif olması", async (t) => {
    const p = await t.login("satis@kapadokyaenerji.example");
    await go(p, `/supplier/auctions/${A.A4}`);
    const txt = await mainText(p);
    t.step("dizüstü kaleminde ön teklif lider/teklif olarak görünür", /Sizin en iyi teklifiniz/.test(txt), txt.slice(0, 200));
    t.step("atlanan kalem için bilgi görünür", /atlandı|geçmedi/i.test(txt));
    if (!/atlandı|geçmedi/i.test(txt)) t.finding("low", "Tedarikçi, ön teklifinin hangi kalemde ilk teklif olduğunu, hangisinde en iyiyi geçemediği için atlandığını göremiyor (bilgi yalnızca satınalmacı panelinde).", `/supplier/auctions/${A.A4}`);
    await t.shot(p, "preoffer");
  }],
];
