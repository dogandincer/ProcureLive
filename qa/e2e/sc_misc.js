// Ziyaretçi/genel (K-01 … K-04) ve yeni özellik senaryoları (N-01 … N-12)
const path = require("path");
const fs = require("fs");
const { fx, api, token, notifications, QA } = require("./lib");
const { go, confirm, openBell, toastSeen, logout, mainText, bodyText } = require("./ui");

const A = fx.auctions;
const pad = (n) => String(n).padStart(2, "0");
const localDT = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
const order = (n) => fx.orders.find((o) => o.number === n).id;

module.exports = (run) => [
  ["K-01", "Kendi kaydı", async (t) => {
    const p = await t.open();
    await go(p, "/login");
    await p.getByRole("link", { name: "Tedarikçi olarak kaydolun" }).click();
    await p.waitForURL("**/register");
    await p.getByPlaceholder("Firma adı").fill("UT Kayıt Teknoloji Ltd. Şti.");
    await p.getByPlaceholder("VKN / TCKN").fill("9876543210");
    await p.getByPlaceholder("Vergi dairesi").fill("Beşiktaş");
    await p.getByPlaceholder("Ad", { exact: true }).fill("Deniz");
    await p.getByPlaceholder("Soyad", { exact: true }).fill("Kaya");
    await p.getByPlaceholder("E-posta", { exact: true }).fill("ut.kayit@utkayit.example");
    await p.getByPlaceholder("Şifre", { exact: true }).fill("Test1234!");
    await p.getByPlaceholder("Şifre (tekrar)").fill("Test1234!");
    await p.locator('input[type="checkbox"]').check();
    await t.shot(p, "form");
    await p.getByRole("button", { name: "Kayıt ol" }).click();
    await p.waitForURL("**/supplier**", { timeout: 15000 });
    await t.settle(p, 800);
    t.step("hesap açıldı, onay bekliyor bilgisi", /onay/i.test(await mainText(p)));
    await t.shot(p, "pending");
    const n = await notifications("admin@procurelive.local", 20);
    t.step("Admin'e bildirim", n.some((x) => /UT Kayıt/.test(x.body)), n.slice(0, 3).map((x) => x.title).join(" | "));
  }],

  ["K-02", "Davet linki ile kayıt", async (t) => {
    const link = fx.links.find((l) => l.company.startsWith("Ege Etiket"));
    const p = await t.open();
    await go(p, `/invite?token=${link.token}`);
    await t.shot(p, "invite");
    await p.getByRole("button", { name: "Yeni hesap oluştur" }).click().catch(() => {});
    const form = p.locator("form").last();
    const inputs = form.locator("input:not([type=checkbox])");
    await inputs.nth(0).fill("Selim");
    await inputs.nth(1).fill("Etiket");
    if (await inputs.nth(2).isEditable()) await inputs.nth(2).fill(link.email ?? "satis@egeetiket.example");
    await form.locator('input[type="password"]').nth(0).fill("Test1234!");
    await form.locator('input[type="password"]').nth(1).fill("Test1234!");
    await form.locator('input[type="checkbox"]').check();
    await form.getByRole("button", { name: /Hesap oluştur/ }).click();
    await p.waitForURL("**/supplier/auctions/**", { timeout: 15000 });
    await t.settle(p, 800);
    t.step("kayıt sonrası ilgili ihale açıldı", p.url().includes(A.A3), p.url());
    t.step("davet görünüyor", /Daveti Kabul Et|Davet/.test(await mainText(p)));
    await t.shot(p, "auction");
  }],

  ["K-03", "Hatalı şifre kilidi", async (t) => {
    const p = await t.open();
    await go(p, "/login");
    let msg = "";
    for (let i = 1; i <= 4; i++) {
      await p.fill('input[type="email"]', "gizem.polat@kuzey.local");
      await p.fill('input[type="password"]', "Yanlis" + i);
      await p.click('button[type="submit"]');
      await p.waitForTimeout(1200);
      msg = await p.getByTestId("login-error").innerText().catch(() => "");
    }
    t.step("4. denemede hesap kilitlendi ve mesaj söylüyor", /kilit|pasife/i.test(msg), msg);
    await t.shot(p, "locked");
    const a = await t.login("admin@procurelive.local");
    await go(a, "/admin/users");
    await a.locator("tr", { hasText: "gizem.polat" }).getByRole("button", { name: "Kilidi Aç" }).click();
    await t.settle(a);
    const r = await api("POST", "/api/auth/login", null, { email: "gizem.polat@kuzey.local", password: fx.password });
    t.step("Admin kilidi açtı, doğru şifreyle giriş", r.status === 200, r.status);
  }],

  ["K-04", "Dil ve mobil", async (t) => {
    const p = await t.login("teklif@egebilisim.example", { mobile: true });
    const drawerOpen = await p.locator("aside").first().evaluate((el) => { const r = el.getBoundingClientRect(); return r.right > 40 && r.width > 100; }).catch(() => false);
    if (drawerOpen) {
      t.finding("med", "Mobilde sayfa açılışında yan menü (çekmece) açık geliyor ve içeriği, dil seçimini örtüyor.", "mobil");
      await p.mouse.click(360, 600);
      await p.waitForTimeout(500);
    }
    await p.locator("header").getByRole("button", { name: "English" }).click();
    await p.waitForTimeout(1000);
    const overflow = [];
    const turkish = [];
    for (const r of ["/supplier", "/supplier/auctions", `/supplier/auctions/${A.A4}`]) {
      await go(p, r);
      if (await p.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1)) overflow.push(r);
      const txt = await p.locator("body").innerText();
      const hits = (txt.match(/\b(İhale|Teklif Ver|Kaydet|Davetlerim|Panelim|Birim fiyat|Güncel en iyi)\b/g) || []);
      if (hits.length) turkish.push(`${r}: ${[...new Set(hits)].join(",")}`);
    }
    t.step("mobilde yatay kaydırma yok", overflow.length === 0, overflow.join(", "));
    t.step("İngilizce modda Türkçe arayüz metni kalmadı", turkish.length === 0, turkish.join(" | "));
    await t.shot(p, "mobile-en");
  }],

  ["N-01", "Yayın onayı iste", async (t) => {
    const ahmet = await token("ahmet.yilmaz@kuzey.local");
    const create = await api("POST", "/api/auctions/tender", ahmet, {
      master: { title: "UT Onay Akışı Taslağı", description: null, auctionType: "Dutch", ownerCompanyId: fx.firms["KZY-HLD"], currencyId: (await api("GET", "/api/currencies", ahmet)).data.find((c) => c.isBaseCurrency).id,
        startAt: new Date(Date.now() + 5 * 86400e3).toISOString(), endAt: new Date(Date.now() + 6 * 86400e3).toISOString(), reservePrice: null, extensionTriggerMinutes: 5, extensionDurationMinutes: 3, maxExtensionMinutes: 30,
        paymentTerm: "Net30", deliveryTerm: "DAP", allowAlternativeProducts: true, allowPartialBids: true, isListedForPlus: false, workspaceId: fx.workspaces["Genel Müdürlük - İstanbul"], referenceCode: null },
      lines: [{ productId: null, description: "UT Kalem", quantity: 10, unitOfMeasure: "Piece", targetPrice: 100, requestedDeliveryDate: null, specification: null, sortOrder: 1 }],
      vendorIds: [fx.companies["Ege Bilişim Teknolojileri A.Ş."].id], attachments: [] });
    const id = typeof create.data === "string" ? create.data : create.data?.id;
    t.step("taslak oluşturuldu", !!id, JSON.stringify(create.data).slice(0, 120));
    run.ctx.n01 = id;
    const p = await t.login("ahmet.yilmaz@kuzey.local");
    await go(p, `/admin/auctions/${id}`);
    t.step("doğrudan “Yayınla” yok", await p.getByRole("button", { name: "Yayınla", exact: true }).count() === 0);
    await p.getByTestId("approval-request").click();
    await p.locator("form").filter({ hasText: "Yayın onayı iste" }).getByRole("button", { name: "Yayın onayı iste" }).click();
    await p.getByTestId("approval-pending").waitFor({ timeout: 8000 });
    t.step("panelde “Yayın onayı bekleniyor”", true);
    const n = await notifications("admin@procurelive.local", 20);
    t.step("Admin'e bildirim", n.some((x) => x.relatedEntityId === id), n.slice(0, 3).map((x) => x.title).join(" | "));
    await t.shot(p, "pending");
  }],

  ["N-02", "Yayın onayı ver / reddet", async (t) => {
    const p = await t.login("admin@procurelive.local");
    await go(p, "/admin");
    const card = p.getByTestId("pending-approvals");
    await card.getByRole("link", { name: "2027 Yılı Kırtasiye Çerçeve Alımı" }).click();
    await p.waitForURL(`**/admin/auctions/${A.A1}`);
    await t.settle(p);
    await p.getByTestId("approval-reject").click();
    await p.getByTestId("approval-reject-reason").fill("Teslim yeri ve şartname eki netleştirilsin.");
    await p.getByTestId("approval-reject-confirm").click();
    await p.getByTestId("approval-rejected").waitFor({ timeout: 8000 });
    t.step("ret gerekçesi taslakta görünüyor", true);
    const b = await t.login("ahmet.yilmaz@kuzey.local");
    await go(b, `/admin/auctions/${A.A1}`);
    t.step("satınalmacı gerekçeyi görüyor", /netleştirilsin/.test(await mainText(b)));
    await b.getByTestId("approval-request").click();
    const dts = b.locator('form input[type="datetime-local"]');
    await dts.nth(0).fill(localDT(new Date(Date.now() + 2 * 86400e3)));
    await dts.nth(1).fill(localDT(new Date(Date.now() + 2 * 86400e3 + 3 * 3600e3)));
    await b.locator("form").filter({ hasText: "Yayın onayı iste" }).getByRole("button", { name: "Yayın onayı iste" }).click();
    await b.getByTestId("approval-pending").waitFor({ timeout: 8000 });
    await p.reload();
    await p.getByTestId("approval-approve").click();
    t.step("onaylandı", await toastSeen(p, "Yayın onaylandı"));
    const d = await api("GET", `/api/auctions/${A.A1}`, await token("admin@procurelive.local"));
    t.step("ihale Planlandı", d.data.status === "Scheduled", d.data.status);
    const n = await notifications("ahmet.yilmaz@kuzey.local");
    t.step("satınalmacıya her adımda bildirim", n.some((x) => x.title === "Yayın Reddedildi") && n.some((x) => x.title === "Yayın Onaylandı"));
    await t.shot(p, "approved");
  }],

  ["N-03", "Kazanan seç ve sipariş aç", async (t) => {
    const p = await t.login("burak.celik@kuzey.local");
    await go(p, `/admin/auctions/${A.A8}`);
    for (let i = 0; i < 4; i++) {
      const btn = p.getByRole("button", { name: "Kazanan Seç" }).first();
      if (!(await btn.count())) break;
      await btn.click();
      await confirm(p, "Kazanan Seç");
      await t.settle(p, 1200);
    }
    await p.getByTestId("create-orders").click();
    t.step("sipariş taslakları oluşturuldu", await toastSeen(p, /sipariş taslağı oluşturuldu/));
    await t.settle(p, 1000);
    const r = await api("GET", `/api/purchase-orders?sourceId=${A.A8}`, await token("burak.celik@kuzey.local"));
    t.step("tedarikçi başına bir sipariş (PO-…)", r.data.items.length >= 1 && r.data.items.every((o) => o.orderNumber.startsWith("PO-")),
      r.data.items.map((o) => `${o.orderNumber} ${o.supplierCompanyName}`).join(", "));
    await t.shot(p, "orders");
  }],

  ["N-04", "Siparişi düzenle ve gönder", async (t) => {
    const p = await t.login("ahmet.yilmaz@kuzey.local");
    await go(p, "/admin/orders");
    await p.locator('[data-testid="order-row"]', { hasText: "PO-2026-0005" }).getByRole("link", { name: "Detay" }).click();
    await p.getByTestId("order-detail").waitFor();
    const form = p.getByTestId("order-draft-form");
    await form.locator('input[type="date"]').fill(new Date(Date.now() + 15 * 86400e3).toISOString().slice(0, 10));
    await form.locator("textarea").fill("Monitörler BT birimine teslim edilecek.");
    await form.getByRole("button", { name: "Kaydet" }).click();
    t.step("taslak kaydedildi", await toastSeen(p, "Sipariş taslağı kaydedildi"));
    await p.getByTestId("order-send").click();
    await confirm(p, "Gönder");
    t.step("gönderildi", await toastSeen(p, "Sipariş tedarikçiye gönderildi"));
    await t.settle(p);
    t.step("durum Gönderildi, form kilitli", /Gönderildi/.test(await p.getByTestId("order-status").innerText()) && await p.getByTestId("order-draft-form").count() === 0);
    const n = await notifications("kurumsal@bogazicitek.example");
    t.step("tedarikçiye “Yeni Sipariş” bildirimi", n.some((x) => x.title === "Yeni Sipariş"));
    await t.shot(p, "sent");
  }],

  ["N-05", "Siparişi onayla", async (t) => {
    const p = await t.login("satis@anadoluofis.example");
    await go(p, "/supplier/orders");
    const rows = await p.getByTestId("order-row").allInnerTexts();
    t.step("taslak siparişler listede yok", !rows.some((r) => /Taslak/.test(r)), rows.join(" | "));
    await p.getByTestId("order-row").filter({ hasText: "Gönderildi" }).first().getByRole("link", { name: "Detay" }).click();
    await p.getByTestId("order-confirm").click();
    await p.getByTestId("order-confirm-note").fill("Teslimat 10 iş günü içinde yapılacaktır.");
    await confirm(p, "Onayla");
    t.step("sipariş onaylandı", await toastSeen(p, "Sipariş onaylandı."));
    await t.settle(p);
    t.step("not görünüyor", /10 iş günü/.test(await p.getByTestId("order-supplier-note").innerText()));
    const n = await notifications("ahmet.yilmaz@kuzey.local");
    t.step("satınalmacıya bildirim", n.some((x) => x.title === "Sipariş Onaylandı"));
    await t.shot(p, "confirmed");
  }],

  ["N-06", "Süreçte soru sor", async (t) => {
    const p = await t.login("teklif@egebilisim.example");
    await go(p, `/supplier/auctions/${A.A4}`);
    const f = p.getByTestId("qa-ask-form");
    await f.locator("textarea").fill("Yazıcılar için toner dahil mi?");
    await f.getByRole("button", { name: "Soru gönder" }).click();
    t.step("soru gönderildi", await toastSeen(p, "Sorunuz satınalma ekibine iletildi."));
    await t.settle(p);
    t.step("“Cevap bekleniyor” olarak listelendi", /Cevap bekleniyor/.test(await p.getByTestId("qa-item").filter({ hasText: "toner" }).innerText()));
    const n = await notifications("ahmet.yilmaz@kuzey.local");
    t.step("satınalmacıya bildirim", n.some((x) => x.title === "Yeni Tedarikçi Sorusu" && x.relatedEntityId === A.A4));
    await t.shot(p, "asked");
  }],

  ["N-07", "Soruyu cevapla, duyuru yap", async (t) => {
    const p = await t.login("ahmet.yilmaz@kuzey.local");
    await openBell(p);
    await p.getByTestId("notification-item").filter({ hasText: "Yeni Tedarikçi Sorusu" }).first().click();
    await p.waitForURL(`**/admin/auctions/${A.A4}#qa`);
    await p.getByTestId("qa-panel").waitFor();
    await p.waitForTimeout(1500);
    t.step("bildirim soru-cevap bölümüne indi", await p.getByTestId("qa-panel").evaluate((el) => { const r = el.getBoundingClientRect(); return r.top < innerHeight && r.bottom > 0; }));
    const q = p.getByTestId("qa-item").filter({ hasText: "toner" });
    await q.getByTestId("qa-answer-open").click();
    await q.getByTestId("qa-answer-input").fill("Evet, başlangıç toneri dahildir.");
    await q.getByTestId("qa-answer-save").click();
    t.step("cevap kaydedildi", await toastSeen(p, "Cevap kaydedildi."));
    await p.getByTestId("qa-announce-form").locator("textarea").fill("Teklif son günü değişmemiştir.");
    await p.getByTestId("qa-announce-form").getByRole("button", { name: "Duyuru yayınla" }).click();
    t.step("duyuru yayınlandı", await toastSeen(p, /Duyuru yayınlandı/));
    const other = await t.login("kurumsal@bogazicitek.example");
    await go(other, `/supplier/auctions/${A.A4}`);
    const txt = await other.getByTestId("qa-panel").innerText();
    t.step("diğer tedarikçi cevabı ve duyuruyu görüyor", txt.includes("başlangıç toneri") && txt.includes("son günü"));
    t.step("soran firma adı görünmüyor", !txt.includes("Ege Bilişim"));
    const n = await notifications("kurumsal@bogazicitek.example");
    t.step("katılımcılara bildirim", n.some((x) => x.title === "Yeni Duyuru"));
    await t.shot(other, "supplier-view");
  }],

  ["N-08", "Belge yükle", async (t) => {
    const p = await t.login("teklif@egebilisim.example");
    await go(p, "/supplier/settings");
    await p.getByTestId("document-add").click();
    await p.getByTestId("document-type").selectOption("Iso27001");
    const pdf = path.join(run.OUT, "iso27001.pdf");
    fs.writeFileSync(pdf, "%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF");
    await p.getByTestId("document-form").getByTestId("file-input").setInputFiles(pdf);
    await p.getByTestId("document-file-name").waitFor();
    await p.getByTestId("document-valid-until").fill(new Date(Date.now() + 10 * 86400e3).toISOString().slice(0, 10));
    await p.getByTestId("document-save").click();
    t.step("belge eklendi", await toastSeen(p, "Belge eklendi."));
    await t.settle(p);
    const row = p.getByTestId("document-row").filter({ hasText: "ISO 27001" });
    t.step("“Yakında doluyor” rozeti", /Yakında doluyor/.test(await row.first().innerText()));
    const b = await t.login("ahmet.yilmaz@kuzey.local");
    await go(b, `/admin/companies/${fx.companies["Ege Bilişim Teknolojileri A.Ş."].id}`);
    await b.getByTestId("documents-card").waitFor();
    t.step("satınalmacı cari kartında görüyor", /ISO 27001/.test(await b.getByTestId("documents-card").innerText()));
    await t.shot(p, "docs");
  }],

  ["N-09", "Satınalmacı işlem yetkisi", async (t) => {
    const p = await t.login("admin@procurelive.local");
    await go(p, "/admin/permissions");
    await p.getByTestId("perm-products.manage").uncheck();
    await p.getByTestId("role-permissions-save").click();
    t.step("yetki kaldırıldı", await toastSeen(p, "Rol yetkileri kaydedildi."));
    const b = await t.login("selin.demir@kuzey.local");
    await go(b, "/admin/products");
    t.step("“Yeni Ürün” düğmesi görünmüyor", await b.getByRole("button", { name: /Yeni Ürün/ }).count() === 0);
    const selin = await token("selin.demir@kuzey.local");
    const cat = (await api("GET", "/api/categories", selin)).data[0].id;
    const r = await api("POST", "/api/products", selin, { sku: "UT-X1", name: "Yetki Deneme", description: null, categoryId: cat, unitOfMeasure: "Piece", brand: null, defaultCurrencyId: null, workspaceId: fx.workspaces["Aliağa Doğalgaz Santrali"] });
    t.step("API'den 403", r.status === 403, r.status + " " + JSON.stringify(r.data).slice(0, 120));
    const bad = await api("POST", "/api/products", selin, { sku: "", name: "", categoryId: cat, unitOfMeasure: "Piece" });
    if (bad.status === 400) t.finding("low", "Yetkisi olmayan kullanıcı eksik/hatalı istekte önce doğrulama hatası (400) alıyor; yetki kontrolü doğrulamadan önce yapılmalı (403).", "PermissionBehavior sırası");
    await p.getByTestId("perm-products.manage").check();
    await p.getByTestId("role-permissions-save").click();
    t.step("yetki geri verildi", await toastSeen(p, "Rol yetkileri kaydedildi."));
  }],

  ["N-10", "Bildirimden kayda git", async (t) => {
    const p = await t.login("teklif@egebilisim.example");
    await openBell(p);
    const item = p.getByTestId("notification-item").filter({ hasText: /Sipariş|İhale|Soru|Duyuru|Davet/ }).first();
    const title = await item.innerText();
    await item.click();
    await p.waitForTimeout(1500);
    t.step("ilgili kayıt açıldı", /\/supplier\/(auctions|orders|rfi|rfq)\//.test(p.url()), `${title.slice(0, 60)} → ${p.url()}`);
    await openBell(p);
    await p.mouse.click(700, 700);
    await p.waitForTimeout(400);
    t.step("dışarı tıklayınca panel kapandı", !(await p.getByTestId("notification-panel").isVisible().catch(() => false)));
    await openBell(p);
    await p.keyboard.press("Escape");
    await p.waitForTimeout(300);
    t.step("Esc ile panel kapandı", !(await p.getByTestId("notification-panel").isVisible().catch(() => false)));
  }],

  ["N-11", "Tema ve renk paleti", async (t) => {
    const p = await t.login("ece.koc@kuzey.local");
    await go(p, "/settings");
    await p.getByTestId("mode-dark").click();
    await p.getByTestId("preset-emerald").click();
    await p.getByTestId("swatches").locator("button").nth(5).click();
    await p.getByTestId("sidebar-light").click().catch(() => {});
    await p.waitForTimeout(600);
    const bg = await p.evaluate(() => getComputedStyle(document.body).backgroundColor);
    t.step("koyu mod uygulandı", /rgb\((\d+), (\d+), (\d+)\)/.test(bg) && Number(bg.match(/\d+/)[0]) < 60, bg);
    await t.shot(p, "theme");
    await logout(p);
    await p.fill('input[type="email"]', "ece.koc@kuzey.local");
    await p.fill('input[type="password"]', fx.password);
    await p.click('button[type="submit"]');
    await p.waitForURL((u) => !u.pathname.startsWith("/login"));
    await p.waitForTimeout(800);
    const bg2 = await p.evaluate(() => getComputedStyle(document.body).backgroundColor);
    t.step("çıkış/giriş sonrası tercih korundu", bg2 === bg, `${bg} / ${bg2}`);
    await go(p, "/settings");
    await p.getByTestId("hex-input").fill("#f5f5a0");
    await p.getByTestId("hex-input").press("Enter");
    await p.waitForTimeout(500);
    t.step("okunması zor renkte uyarı", await p.getByTestId("contrast-warning").isVisible());
    await p.getByTestId("theme-reset").click();
  }],

  ["N-12", "Şifremi unuttum", async (t) => {
    const p = await t.open();
    await go(p, "/login");
    await p.getByRole("link", { name: "Şifremi unuttum" }).click();
    await p.waitForURL("**/forgot-password");
    await t.settle(p);
    await p.getByLabel("E-posta").fill("deniz.arslan@kuzey.local");
    await p.getByRole("button", { name: "Bağlantı gönder" }).click();
    await p.getByTestId("forgot-sent").waitFor();
    t.step("nötr mesaj gösterildi", true);
    await p.waitForTimeout(1000);
    const log = fs.readFileSync(path.join(QA, "out", "api.log"), "utf8");
    const links = [...log.matchAll(/deniz\.arslan@kuzey\.local[^\n]*?(http\S+reset-password\?\S+)/g)];
    const link = links.length ? links[links.length - 1][1] : null;
    t.step("bağlantı e-posta simülasyonunda (API günlüğü)", !!link);
    if (!link) return;
    await p.goto(link);
    await p.locator('input[type="password"]').nth(0).fill("Yeni2026sifre");
    await p.locator('input[type="password"]').nth(1).fill("Yeni2026sifre");
    await p.getByRole("button", { name: "Şifreyi kaydet" }).click();
    await p.getByTestId("reset-done").waitFor();
    const ok = await api("POST", "/api/auth/login", null, { email: "deniz.arslan@kuzey.local", password: "Yeni2026sifre" });
    t.step("yeni şifreyle giriş", ok.status === 200);
    const b = await t.login("deniz.arslan@kuzey.local", { password: "Yeni2026sifre" });
    await go(b, "/profile");
    const f = b.getByTestId("change-password");
    await f.locator('input[type="password"]').nth(0).fill("Yeni2026sifre");
    await f.locator('input[type="password"]').nth(1).fill(fx.password);
    await f.locator('input[type="password"]').nth(2).fill(fx.password);
    await f.getByRole("button", { name: "Şifreyi değiştir" }).click();
    t.step("Profilim'den şifre değişti", await toastSeen(b, "Şifreniz değiştirildi."));
  }],
];
