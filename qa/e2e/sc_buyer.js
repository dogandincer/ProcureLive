// Satınalmacı senaryoları (S-01 … S-11)
const path = require("path");
const fs = require("fs");
const { fx, api, token, notifications } = require("./lib");
const { selectContaining, field, go, pickLookup, selectWorkspace, confirm, toastSeen, mainText } = require("./ui");

const A = fx.auctions, Q = fx.rfq;
const co = (n) => fx.companies[n].id;
const pad = (n) => String(n).padStart(2, "0");
const localDT = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;

/** Katılım talebi kartını aç (ilk açık talep sayfa açılışında zaten açık gelir); kartın kapsayıcısını döndürür. */
async function openRequest(p, row) {
  const card = row.locator("xpath=..");
  const actions = card.getByRole("button", { name: /Cariyi onayla|Ön teklifi kabul et/ });
  const expanded = await card.locator("table").count();
  if (!expanded) { await row.click(); await p.waitForTimeout(400); }
  return card;
}

module.exports = (run) => [
  ["S-01", "Veri kapsamı", async (t) => {
    const p = await t.login("ahmet.yilmaz@kuzey.local");
    for (const [route, label] of [["/admin/auctions", "ihale"], ["/admin/rfi", "RFI"], ["/admin/rfq", "RFQ"]]) {
      await go(p, route);
      const txt = await mainText(p);
      const foreign = ["Konya", "Aliağa", "Kartal", "Gebze", "Manisa", "Mersin", "Eskişehir", "Bursa"].filter((w) => txt.includes(w));
      t.step(`${label} listesinde yalnızca Holding kayıtları`, foreign.length === 0, foreign.join(", "));
    }
    await go(p, "/admin/auctions");
    await selectWorkspace(p, "Tüm İşyerleri").catch(() => {});
    await t.settle(p);
    const all = await p.getByTestId("list-search").locator("xpath=ancestor::*[contains(@class,'rounded')][1]").locator("tbody tr").count();
    await selectWorkspace(p, "Ankara Bölge");
    await t.settle(p);
    const ank = await p.locator("main tbody tr").count();
    t.step("Ankara seçilince liste daralır", ank < all && ank >= 1, `${all} → ${ank}`);
    await t.shot(p, "ankara");
  }],

  ["S-02", "Sihirbazla ihale açma", async (t) => {
    const p = await t.login("ahmet.yilmaz@kuzey.local");
    await go(p, "/admin/auctions/create");
    await p.getByPlaceholder("Örn: 2026 Q1 Ofis Malzemeleri Alımı").fill("UT Toplantı Odası Ekipmanları");
    const sel = p.locator("select");
    await selectContaining(sel.nth(0), "Hollanda").catch(() => {});
    const owner = p.locator("label", { hasText: "Açan Birim" }).locator("xpath=following-sibling::select[1]");
    const prefilled = (await owner.inputValue()) !== "";
    if (!prefilled) t.finding("low", "Sihirbazda 'Açan Birim' başlıkta seçili firmayla dolu gelmiyor; kullanıcı her seferinde seçmek zorunda.", "/admin/auctions/create");
    await selectContaining(owner, "Kuzey Holding");
    const start = new Date(Date.now() + 3 * 86400e3), end = new Date(Date.now() + 4 * 86400e3);
    const dts = p.locator('input[type="datetime-local"]');
    await dts.nth(0).fill(localDT(start));
    await dts.nth(1).fill(localDT(end));
    await t.shot(p, "step1");
    await p.getByRole("button", { name: "İleri" }).click();
    await p.getByPlaceholder("Cari ara (unvan / vergi no)...").fill("Bilişim");
    await p.waitForTimeout(500);
    await p.locator("label", { hasText: "Ege Bilişim" }).first().click();
    await p.getByPlaceholder("Cari ara (unvan / vergi no)...").fill("Boğaziçi");
    await p.waitForTimeout(500);
    await p.locator("label", { hasText: "Boğaziçi" }).first().click();
    await t.shot(p, "step2");
    await p.getByRole("button", { name: "İleri" }).click();
    await p.getByRole("button", { name: "Ürün Havuzunu Aç" }).click();
    for (const q of ["Monitör", "Switch", "Yazıcı"]) {
      await p.getByPlaceholder("Ürün adı veya stok kodu ara...").fill(q);
      await p.waitForTimeout(700);
      await p.getByRole("button", { name: "Ekle", exact: true }).first().click();
    }
    await p.keyboard.press("Escape");
    await p.waitForTimeout(300);
    if (await p.getByText("Ürün Havuzu", { exact: true }).isVisible()) {
      t.finding("low", "Ürün havuzu çekmecesi Esc ile kapanmıyor; kapatma düğmesinin erişilebilir adı yok.", "/admin/auctions/create");
      await p.getByText("Ürün Havuzu", { exact: true }).locator("xpath=following-sibling::button[1]").click();
    }
    await t.settle(p);
    t.step("ürün havuzundan 3 kalem eklendi", /3 kalem/.test(await mainText(p)), (await mainText(p)).match(/\d+ kalem/)?.[0]);
    await t.shot(p, "step3");
    await p.getByRole("button", { name: "İleri" }).click();
    const pdf = path.join(run.OUT, "teknik-sartname.pdf");
    fs.writeFileSync(pdf, "%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF");
    await p.locator('input[type="file"]').first().setInputFiles(pdf);
    await p.waitForTimeout(1500);
    await p.getByText("Bu ihale Plus kullanıcıların aramalarında listelenebilsin").click();
    const sim = await p.getByText("Yükleme simüledir").count();
    if (sim) t.finding("low", "Sihirbazdaki “Yükleme simüledir (gerçek depolama bağlı değil)” notu artık yanlış: ekler gerçekten yükleniyor.", "/admin/auctions/create");
    await t.shot(p, "step4");
    await p.getByRole("button", { name: "İleri" }).click();
    await t.shot(p, "step5");
    await p.getByRole("button", { name: "İhaleyi Başlat" }).click();
    await p.waitForURL(/\/admin\/auctions\/[0-9a-f-]{36}/, { timeout: 15000 });
    await t.settle(p, 800);
    const id = p.url().split("/").pop();
    const d = await api("GET", `/api/auctions/${id}`, await token("ahmet.yilmaz@kuzey.local"));
    t.step("ihale oluştu (3 kalem, 2 davet, 1 ek, Plus listeli)", d.data.items.length === 3 && d.data.invitations.length === 2 && d.data.attachments.length >= 1 && d.data.isListedForPlus,
      `${d.data.items.length} kalem, ${d.data.invitations.length} davet, ${d.data.attachments.length} ek, plus=${d.data.isListedForPlus}`);
    // Holding firmasında yayın Admin onayına gider
    const req = p.getByTestId("approval-request");
    if (await req.count()) {
      await req.click();
      await p.locator("form").filter({ hasText: "Yayın onayı iste" }).getByRole("button", { name: "Yayın onayı iste" }).click();
      t.step("Holding'de yayın onaya gönderildi", await toastSeen(p, "Yayın onayı istendi"));
      await api("POST", `/api/auctions/${id}/publish-approval/approve`, await token("admin@procurelive.local"));
    }
    const d2 = await api("GET", `/api/auctions/${id}`, await token("ahmet.yilmaz@kuzey.local"));
    t.step("ihale Planlandı", d2.data.status === "Scheduled", d2.data.status);
    const n = await notifications("teklif@egebilisim.example");
    t.step("davetli tedarikçiye bildirim", n.some((x) => x.relatedEntityId === id), n.slice(0, 3).map((x) => x.title).join(" | "));
    run.ctx.s02 = id;
  }],

  ["S-03", "Katılım talepleri", async (t) => {
    const p = await t.login("selin.demir@kuzey.local");
    await go(p, `/admin/auctions/${A.A2}`);
    const panel = p.locator("section, div").filter({ has: p.getByText("Katılım talepleri", { exact: true }) }).last();
    await t.shot(p, "before");
    for (const name of ["Toros", "Pamukkale"]) {
      const row = p.getByRole("button", { name: new RegExp(name) }).first();
      if (!(await row.count())) { t.step(`${name} talebi listede`, false); continue; }
      let card = await openRequest(p, row);
      const approve = card.getByRole("button", { name: /Cariyi onayla/ });
      if (await approve.count() && await approve.first().isEnabled()) { await approve.first().click(); await t.settle(p, 1000); }
      card = await openRequest(p, row);
      await card.getByRole("button", { name: /Ön teklifi kabul et/ }).first().click();
      t.step(`${name} ihaleye dahil edildi`, await toastSeen(p, "ihaleye dahil edildi"));
      await t.settle(p, 800);
    }
    void panel;
    const reqs = await api("GET", `/api/auctions/${A.A2}/participation-requests`, await token("selin.demir@kuzey.local"));
    const st = (reqs.data ?? []).map((r) => `${r.companyName}:${r.status}`);
    t.step("talepler Kabul edildi", st.filter((s) => /Toros|Pamukkale/.test(s)).every((s) => s.endsWith("Accepted")), st.join(", "));
    const n = await notifications("teklif@torosmakine.example");
    t.step("tedarikçiye her adımda bildirim", n.filter((x) => x.relatedEntityId === A.A2).length >= 2, n.slice(0, 4).map((x) => x.title).join(" | "));
    await t.shot(p, "after");
  }],

  ["S-04", "Canlı ihaleyi izleme", async (t) => {
    const p = await t.login("ahmet.yilmaz@kuzey.local");
    await go(p, `/admin/auctions/${A.A4}`);
    await p.waitForTimeout(1500);
    const ege = await token("teklif@egebilisim.example");
    const det = await api("GET", `/api/auctions/${A.A4}`, ege);
    const item = det.data.items.find((i) => i.description.includes("Yazıcı"));
    const amount = Math.floor((item.currentBestAmount ?? 16000) - 150);
    const r = await api("POST", `/api/auctions/${A.A4}/items/${item.id}/bids`, ege, { amount, currencyId: det.data.currencyId });
    t.step("Ege Bilişim teklif verdi (API)", r.status === 200, r.status + " " + JSON.stringify(r.data).slice(0, 120));
    await p.waitForTimeout(2500);
    const txt = await mainText(p);
    const shown = amount.toLocaleString("tr-TR");
    t.step("yeni teklif sayfa yenilenmeden akışa düştü", txt.includes(shown), shown);
    t.step("en iyi teklif güncellendi", txt.includes(`${shown},00`), shown);
    await t.shot(p, "live");
  }],

  ["S-05", "Kazanan seçme", async (t) => {
    const p = await t.login("burak.celik@kuzey.local");
    await go(p, `/admin/auctions/${A.A8}`);
    await t.shot(p, "before");
    let n = 0;
    for (let i = 0; i < 4; i++) {
      const btn = p.getByRole("button", { name: "Kazanan Seç" }).first();
      if (!(await btn.count())) break;
      await btn.click();
      await confirm(p, "Kazanan Seç");
      await t.settle(p, 1200);
      n++;
    }
    t.step("her kalemde kazanan seçildi", n >= 2, `${n} seçim`);
    const d = await api("GET", `/api/auctions/${A.A8}`, await token("burak.celik@kuzey.local"));
    t.step("ihale Sonuçlandı", d.data.status === "Awarded", d.data.status);
    const win = await notifications("info@trakyaambalaj.example");
    t.step("kazanan/kaybeden tedarikçilere bildirim", win.some((x) => x.relatedEntityId === A.A8), win.slice(0, 3).map((x) => x.title).join(" | "));
    await t.shot(p, "after");
  }],

  ["S-06", "Canlıyken ön teklif kabulü", async (t) => {
    const p = await t.login("burak.celik@kuzey.local");
    await go(p, `/admin/auctions/${A.A5}`);
    const row = p.getByRole("button", { name: /Mavi Deniz/ }).first();
    let card = await openRequest(p, row);
    const approve = card.getByRole("button", { name: /Cariyi onayla/ }).first();
    if (await approve.count() && await approve.isEnabled()) { await approve.click(); await t.settle(p, 1000); }
    card = await openRequest(p, row);
    await card.getByRole("button", { name: /Ön teklifi kabul et/ }).first().click();
    t.step("Mavi Deniz ihaleye dahil edildi", await toastSeen(p, "ihaleye dahil edildi"));
    await t.settle(p, 1200);
    await openRequest(p, row);
    const txt = await mainText(p);
    t.step("ön teklif sonucu (ilk teklif oldu / atlandı) gösteriliyor", /İlk teklif oldu|Atlandı/.test(txt));
    await t.shot(p, "after");
  }],

  ["S-07", "RFQ değerlendirme", async (t) => {
    const p = await t.login("ece.koc@kuzey.local");
    await go(p, `/admin/rfq/${Q.Q4}`);
    t.step("karşılaştırma matrisi", (await mainText(p)).includes("Teklif Karşılaştırma"));
    await p.getByRole("button", { name: /AI Analizi Yap/ }).click();
    await p.waitForTimeout(3000);
    const txt = await mainText(p);
    t.step("AI önerisi gerekçesiyle", /öneri|gerekçe|önerilen|en uygun/i.test(txt.slice(txt.indexOf("AI Teklif"), txt.indexOf("AI Teklif") + 800)));
    await t.shot(p, "ai");
    await p.getByRole("button", { name: "Kazanan Seç" }).first().click();
    const dlg = await p.locator('[data-testid="confirm-dialog"]').count();
    if (dlg) await confirm(p, /Kazanan|Onayla/);
    else t.finding("med", "RFQ'da “Kazanan Seç” onay sormadan, tek tıkla sonucu kesinleştiriyor (ihale tarafında onay penceresi var).", `/admin/rfq/${Q.Q4}`);
    await t.settle(p, 1200);
    const d = await api("GET", `/api/rfq/processes/${Q.Q4}`, await token("ece.koc@kuzey.local"));
    t.step("süreç Sonuçlandı", d.data.status === "Awarded", d.data.status);
    await t.shot(p, "after");
  }],

  ["S-08", "RFI oluşturma", async (t) => {
    const p = await t.login("ece.koc@kuzey.local");
    await go(p, "/admin/rfi");
    await p.getByRole("button", { name: /Yeni RFI|Yeni Süreç/ }).click();
    const form = p.locator("form").filter({ has: p.getByPlaceholder("Süreç Başlığı") }).last();
    await selectContaining(form.locator("select").first(), "Lojistik Kapasite");
    await selectContaining(form.locator("select").nth(1), "Lojistik");
    await p.waitForTimeout(300);
    await selectContaining(form.locator("select").nth(2), "Mersin");
    await form.getByPlaceholder("Süreç Başlığı").fill("UT Mersin Depolama Kapasite Anketi");
    if (!(await form.getByText("Son yanıt tarihi").count()))
      t.finding("low", "Yeni RFI formunda şablon, başlık ve bitiş tarihi alanlarının etiketi yok (yalnızca yer tutucu); tarih kutusunun ne olduğu anlaşılmıyor.", "/admin/rfi");
    await form.locator('input[type="datetime-local"]').fill(localDT(new Date(Date.now() + 7 * 86400e3)));
    await t.shot(p, "form");
    await form.getByRole("button", { name: /Oluştur/ }).click();
    await p.waitForURL(/\/admin\/rfi\/[0-9a-f-]{36}/, { timeout: 15000 });
    await t.settle(p, 800);
    for (const name of ["Akdeniz Lojistik", "Toros"]) {
      await pickLookup(p, p.getByTestId("invite-company"), name.split(" ")[0], name);
      await p.getByRole("button", { name: "Davet Et" }).click();
      await t.settle(p, 800);
    }
    const id = p.url().split("/").pop();
    const d = await api("GET", `/api/rfi/processes/${id}`, await token("ece.koc@kuzey.local"));
    t.step("RFI Gönderildi ve 2 davet", d.data.status === "Sent" && d.data.invitations.length === 2, `${d.data.status} ${d.data.invitations.length}`);
    const l = await api("GET", "/api/rfi/processes?page=1&pageSize=20", await token("operasyon@akdenizlojistik.example"));
    t.step("tedarikçi RFI davetlerinde görüyor", l.data.items.some((x) => x.id === id));
    await t.shot(p, "detail");
  }],

  ["S-09", "RFQ kapatma", async (t) => {
    const p = await t.login("deniz.arslan@kuzey.local");
    await go(p, `/admin/rfq/${Q.Q6}`);
    const txt = await mainText(p);
    t.step("Toros'un revize teklifi son hâliyle görünür", txt.includes("24.7") || txt.includes("24,7"), (txt.match(/Toros[^\n]*\n[^\n]*/) || [""])[0]);
    await p.getByRole("button", { name: "Süreci Kapat" }).click().catch(async () => p.getByRole("button", { name: /Kapat/ }).first().click());
    const dlg = await p.locator('[data-testid="confirm-dialog"]').count();
    if (dlg) await confirm(p, /Kapat/);
    await t.settle(p, 1000);
    await p.getByRole("button", { name: "Kazanan Seç" }).first().click();
    if (await p.locator('[data-testid="confirm-dialog"]').count()) await confirm(p, /Kazanan|Onayla/);
    await t.settle(p, 1000);
    const d = await api("GET", `/api/rfq/processes/${Q.Q6}`, await token("deniz.arslan@kuzey.local"));
    t.step("RFQ sonuçlandı", d.data.status === "Awarded", d.data.status);
    await t.shot(p, "after");
  }],

  ["S-10", "Sorumlu değiştirme ve davet linki", async (t) => {
    const p = await t.login("ahmet.yilmaz@kuzey.local");
    await go(p, `/admin/auctions/${A.A13}`);
    const box = p.getByTestId("auction-assignment");
    await box.getByRole("button", { name: "Düzenle" }).click();
    const resp = p.locator("select").filter({ has: p.locator("option", { hasText: "Zeynep" }) }).first();
    await resp.waitFor({ timeout: 10000 });
    await selectContaining(resp, "Zeynep");
    await resp.locator("xpath=ancestor::form[1]").getByRole("button", { name: "Kaydet" }).click()
      .catch(() => p.getByRole("button", { name: "Kaydet" }).first().click());
    t.step("sorumlu kaydedildi", await toastSeen(p, /güncellendi|kaydedildi/));
    const n = await notifications("zeynep.sahin@kuzey.local");
    t.step("yeni sorumluya (Zeynep) sorumluluk bildirimi", n.some((x) => x.relatedEntityId === A.A13), n.slice(0, 3).map((x) => x.title).join(" | "));
    await pickLookup(p, p.getByTestId("invite-link-company"), "Kapadokya", "Kapadokya");
    await p.getByRole("button", { name: "Link Oluştur" }).click();
    t.step("davet linki oluşturuldu", await toastSeen(p, /oluşturuldu|kopyalandı|Link/));
    await t.settle(p);
    t.step("link listede kopyalanabilir", await p.getByRole("button", { name: "Linki kopyala" }).count() > 0 || await p.locator('[title="Linki kopyala"]').count() > 0);
    await t.shot(p, "after");
  }],

  ["S-11", "Yetkisiz kullanıcı deneyimi", async (t) => {
    const p = await t.login("yeni.alici@kuzey.local");
    const seen = [];
    for (const r of ["/admin", "/admin/auctions", "/admin/rfi", "/admin/rfq", "/admin/companies", "/admin/products", "/admin/orders", "/admin/monitoring"]) {
      await go(p, r);
      const txt = await mainText(p);
      const ok = /yetki|yönetici/i.test(txt);
      seen.push(`${r}:${ok ? "açıklama var" : "AÇIKLAMA YOK"}`);
    }
    const missing = seen.filter((s) => s.includes("YOK"));
    t.step("her ekranda yetki isteme açıklaması", missing.length === 0, missing.join(", "));
    if (missing.length) t.finding("low", `Hiç yetkisi olmayan satınalmacı bazı ekranlarda yalnızca boş liste görüyor, ne yapması gerektiği söylenmiyor: ${missing.map((s) => s.split(":")[0]).join(", ")}`, "yeni.alici");
    await t.shot(p, "orders");
  }],
];
