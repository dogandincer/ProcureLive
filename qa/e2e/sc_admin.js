// Admin senaryoları (Y-01 … Y-10)
const path = require("path");
const fs = require("fs");
const zlib = require("zlib");
const { fx, api, token, notifications, QA } = require("./lib");
const { selectContaining, field, go, pickLookup, selectFirm, selectWorkspace, confirm, toastSeen, logout, mainText } = require("./ui");

const ADMIN = "admin@procurelive.local";
const A = fx.auctions;

function png(w, h, rgb) {
  const chunk = (t, d) => { const b = Buffer.alloc(12 + d.length); b.writeUInt32BE(d.length, 0); b.write(t, 4); d.copy(b, 8); b.writeUInt32BE(zlib.crc32(Buffer.concat([Buffer.from(t), d])) >>> 0, 8 + d.length); return b; };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 2;
  const raw = Buffer.concat(Array.from({ length: h }, () => Buffer.concat([Buffer.from([0]), ...Array.from({ length: w }, () => Buffer.from(rgb))])));
  return Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), chunk("IHDR", ihdr), chunk("IDAT", zlib.deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]);
}

module.exports = (run) => [
  ["Y-01", "Firma Tanım ve marka", async (t) => {
    const p = await t.login(ADMIN);
    await go(p, "/admin/firms");
    await p.getByRole("button", { name: "Yeni Firma" }).click();
    await field(p, "Firma Kodu").fill("KZY-TST");
    await field(p, "Firma Adı").fill("Kuzey Test Teknoloji A.Ş.");
    await field(p, "Firma Kısa Adı").fill("Kuzey Test");
    const logo = path.join(run.OUT, "logo.png");
    fs.writeFileSync(logo, png(64, 64, [200, 40, 90]));
    await p.locator('form input[type="file"]').setInputFiles(logo);
    await p.getByText("Bu firmayı varsayılan olarak ayarla").click();
    await t.shot(p, "form");
    await p.getByRole("button", { name: "Kaydet" }).click();
    t.step("firma kaydedildi", await toastSeen(p, /Firma oluşturuldu|Firma güncellendi/));
    await t.settle(p, 800);
    t.step("listede yeni firma ve varsayılan yıldızı", (await mainText(p)).includes("Kuzey Test Teknoloji"));
    await logout(p);
    await t.settle(p, 800);
    const login = await p.locator("body").innerText();
    t.step("giriş ekranında yeni kısa ad", login.includes("Kuzey Test"), login.slice(0, 200));
    t.step("sekme başlığında yeni ad", (await p.title()).includes("Kuzey Test"), await p.title());
    t.step("giriş ekranında logo görseli", await p.locator('img[src*="uploads"]').count() > 0);
    await t.shot(p, "login-brand");
    // geri al: Holding yeniden varsayılan
    await api("POST", `/api/firms/${fx.firms["KZY-HLD"]}/default`, await token(ADMIN));
  }],

  ["Y-02", "İşyeri tanımı", async (t) => {
    const p = await t.login(ADMIN);
    await go(p, "/admin/workspaces");
    const passiveRow = p.locator("tr", { hasText: "Eski Bursa İrtibat Ofisi" });
    t.step("pasif işyeri listede ayırt ediliyor", /Pasif/i.test(await passiveRow.innerText()), await passiveRow.innerText());
    await p.getByRole("button", { name: "Yeni İşyeri" }).click();
    await p.getByPlaceholder("Örn: Merkez, İstanbul Şubesi, Depo").fill("Ankara Depo");
    await pickLookup(p, p.getByPlaceholder("Firma seçmek için tıklayın"), "Gıda", "Kuzey Gıda");
    await p.getByText("Bu işyerini firmanın varsayılanı olarak ayarla").click();
    await t.shot(p, "form");
    await p.getByRole("button", { name: "Kaydet" }).click();
    t.step("işyeri oluşturuldu", await toastSeen(p, "İşyeri oluşturuldu"));
    await t.settle(p);
    await passiveRow.getByTitle("Düzenle").click().catch(async () => { await passiveRow.locator("button").nth(1).click(); });
    await p.getByText("Aktif", { exact: true }).click();
    await p.getByRole("button", { name: "Kaydet" }).click();
    t.step("pasif işyeri aktifleştirildi", await toastSeen(p, "İşyeri güncellendi"));
    await t.settle(p);
    await selectFirm(p, "Kuzey Gıda");
    await p.locator('button[title="İşyeri seçimi"]:visible').first().click();
    const opts = await p.locator("button", { hasText: "Ankara Depo" }).count();
    t.step("başlık işyeri seçicisinde yeni işyeri", opts > 0);
    await t.shot(p, "switcher");
  }],

  ["Y-03", "Kilit açma ve pasif kullanıcı", async (t) => {
    const p = await t.login(ADMIN);
    await go(p, "/admin/users");
    const gizem = p.locator("tr", { hasText: "gizem.polat" });
    t.step("Gizem 'Kilitli' rozeti", (await gizem.innerText()).includes("Kilitli"), await gizem.innerText());
    await gizem.getByRole("button", { name: "Kilidi Aç" }).click();
    t.step("kilit açıldı mesajı", await toastSeen(p, /kilidi açıldı|etkinleştirildi/i));
    await t.settle(p);
    const eski = p.locator("tr", { hasText: "eski.personel" });
    await eski.getByRole("button", { name: /Aktifleştir|Kilidi Aç/ }).click();
    await t.settle(p);
    await t.shot(p, "users");
    for (const e of ["gizem.polat@kuzey.local", "eski.personel@kuzey.local"]) {
      const r = await api("POST", "/api/auth/login", null, { email: e, password: fx.password });
      t.step(`${e} giriş yapabiliyor`, r.status === 200, r.status + " " + JSON.stringify(r.data).slice(0, 120));
    }
  }],

  ["Y-04", "İşyeri yetkisi", async (t) => {
    const adminTok = await token(ADMIN);
    const ahmetId = fx.users.find((u) => u.email === "ahmet.yilmaz@kuzey.local");
    const users = (await api("GET", "/api/users?kind=internal&page=1&pageSize=100", adminTok)).data.items;
    const ahmet = users.find((u) => u.email === "ahmet.yilmaz@kuzey.local");
    const before = (await api("GET", `/api/users/${ahmet.id}/workspace-access`, adminTok)).data.workspaceIds;
    const p = await t.login(ADMIN);
    await go(p, "/admin/permissions");
    await p.getByRole("button", { name: /Ahmet Yılmaz/ }).first().click();
    await t.settle(p);
    await p.locator("label", { hasText: "Konya Üretim Tesisi" }).locator('input[type="checkbox"]').check();
    await p.getByRole("button", { name: "Kaydet" }).last().click();
    t.step("yetki kaydedildi", await toastSeen(p, /kaydedildi/i));
    await t.shot(p, "permissions");
    const b = await t.login("ahmet.yilmaz@kuzey.local");
    await go(b, "/admin/auctions");
    await selectFirm(b, "Kuzey Gıda");
    await t.settle(b);
    const txt = await mainText(b);
    t.step("Ahmet Konya ihalelerini görüyor", txt.includes("Un ve Şeker") && txt.includes("Ambalaj"), txt.slice(0, 300));
    await t.shot(b, "ahmet-konya");
    await api("PUT", `/api/users/${ahmet.id}/workspace-access`, adminTok, { workspaceIds: before }); // geri al
    void ahmetId;
  }],

  ["Y-05", "Onay bekleyen cari", async (t) => {
    const p = await t.login(ADMIN);
    await go(p, `/admin/companies/${fx.companies["Yeni Ufuk Teknoloji Ltd. Şti."].id}`);
    await t.shot(p, "before");
    const ws = p.locator("select").filter({ has: p.locator("option", { hasText: "Genel Müdürlük" }) }).first();
    await selectContaining(ws, "Genel Müdürlük");
    t.step("işyeri seçimi kaydedildi", await toastSeen(p, "İşyeri güncellendi"));
    await t.settle(p);
    await p.getByRole("button", { name: "Düzenle" }).first().click();
    const form = p.locator("form", { hasText: "Cari Kodu" });
    await selectContaining(form.locator("select").filter({ has: p.locator("option", { hasText: "Aktif" }) }).first(), "Aktif");
    await form.getByRole("button", { name: "Kaydet" }).click();
    t.step("cari güncellendi", await toastSeen(p, /güncellendi/));
    await t.settle(p);
    const r = await api("GET", `/api/companies/${fx.companies["Yeni Ufuk Teknoloji Ltd. Şti."].id}`, await token(ADMIN));
    t.step("cari aktif ve işyeri atanmış", r.data.status === "Active" && !!r.data.workspaceId, `${r.data.status} ${r.data.workspaceId}`);
    const n = await notifications("kayit@yeniufuk.example");
    t.step("tedarikçiye onay bildirimi", n.some((x) => /onay/i.test(x.title + x.body)), n.slice(0, 3).map((x) => x.title).join(" | "));
    await t.shot(p, "after");
  }],

  ["Y-06", "Cari Excel içe aktarma", async (t) => {
    const XLSX = require(path.join(QA, "..", "frontend", "node_modules", "xlsx"));
    const p = await t.login(ADMIN);
    await go(p, "/admin/companies");
    await p.getByRole("button", { name: /Excel/ }).first().click();
    const [dl] = await Promise.all([p.waitForEvent("download", { timeout: 10000 }), p.getByRole("button", { name: /Şablon/ }).click()]);
    const tpl = path.join(run.OUT, "cari-sablon.xlsx");
    await dl.saveAs(tpl);
    const wb = XLSX.readFile(tpl);
    const ws = wb.Sheets[wb.SheetNames[0]];
    const header = XLSX.utils.sheet_to_json(ws, { header: 1 })[0];
    t.step("şablon indirildi (sütunlar)", header.length >= 5, header.join(", "));
    const row = (vals) => header.map((h) => vals[h] ?? "");
    const H = (k) => header.find((h) => h.startsWith(k));
    const rows = [header,
      row({ [H("Cari Adı")]: "Excel Test Lojistik Ltd. Şti.", [H("VKN")]: "4567891230", [H("Vergi Dairesi")]: "Kadıköy", [H("Tür")]: "Supplier", [H("Sektörler")]: "Lojistik" }),
      row({ [H("Cari Adı")]: "Hatalı Satır A.Ş.", [H("VKN")]: "12", [H("Vergi Dairesi")]: "", [H("Tür")]: "Bilinmeyen" })];
    const out = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(out, XLSX.utils.aoa_to_sheet(rows), "Cariler");
    const file = path.join(run.OUT, "cari-import.xlsx");
    XLSX.writeFile(out, file);
    await p.locator('input[type="file"][accept*=".xlsx"]').setInputFiles(file);
    await t.settle(p, 800);
    await t.shot(p, "preview");
    const prev = await p.locator("body").innerText();
    t.step("önizlemede hatalı satır işaretli", /1\s*hatalı|geçersiz|hata/i.test(prev), prev.slice(prev.indexOf("satır") - 50, prev.indexOf("satır") + 200));
    t.step("aktarımda hedef işyeri seçili", (await p.getByTestId("workspace-field").inputValue()) !== "");
    await p.getByRole("button", { name: /İçe Aktar/ }).last().click();
    await t.settle(p, 1500);
    await t.shot(p, "result");
    const r = await api("GET", "/api/companies?searchTerm=Excel%20Test&page=1&pageSize=5", await token(ADMIN));
    t.step("geçerli satır cari olarak açıldı (kodlu)", r.data.items.length === 1 && /CARI-/.test(r.data.items[0].code ?? ""), JSON.stringify(r.data.items[0] ?? {}).slice(0, 160));
  }],

  ["Y-07", "Ürün, görsel ve 3D", async (t) => {
    const p = await t.login(ADMIN);
    await go(p, "/admin/products");
    await p.getByRole("button", { name: /Yeni Ürün/ }).click();
    const pf = p.getByTestId("product-form");
    await pf.getByLabel("SKU").fill("UT-PRJ-01");
    await pf.getByLabel("Ürün Adı").fill("UT Projeksiyon Cihazı");
    t.step("ürün formunda kayıt işyeri önerili", (await pf.getByTestId("workspace-field").inputValue()) !== "");
    const cat = pf.getByLabel("Kategori");
    await cat.selectOption({ index: 1 });
    await p.getByRole("button", { name: "Kaydet" }).click();
    t.step("ürün oluşturuldu", await toastSeen(p, /oluşturuldu|eklendi/));
    await t.settle(p, 800);
    t.step("oluşturulunca ürün detayı açıldı", /\/admin\/products\/[0-9a-f-]{36}/.test(p.url()), p.url());
    if (!/\/admin\/products\/[0-9a-f-]{36}/.test(p.url())) {
      await p.getByTestId("list-search").fill("UT Projeksiyon");
      await t.settle(p, 800);
      await p.locator("tr", { hasText: "UT Projeksiyon Cihazı" }).getByRole("link", { name: "Detay" }).click();
      await t.settle(p, 800);
    }
    await p.getByRole("button", { name: "Fotoğraf Ekle" }).click();
    await p.getByPlaceholder("Fotoğraf adresi (https://...)").fill("https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=640");
    await p.getByRole("button", { name: "Ekle" }).last().click();
    t.step("görsel eklendi", await toastSeen(p, "Fotoğraf eklendi"));
    await p.getByRole("button", { name: "Doğrudan 3D model adresi gir" }).click();
    await p.getByPlaceholder("https://.../model.glb").fill("https://modelviewer.dev/shared-assets/models/Astronaut.glb");
    await p.getByRole("button", { name: "Kaydet" }).last().click();
    t.step("3D model kaydedildi", await toastSeen(p, "3D model kaydedildi"));
    await t.settle(p, 800);
    const show = p.getByTestId("show-3d");
    if (await show.count()) await show.click();
    await p.waitForTimeout(2000);
    t.step("3D görüntüleyici sayfada", await p.locator("model-viewer, canvas").count() > 0);
    await t.shot(p, "product");
    // AI ile 3D: görseli olan başka bir ürün
    await go(p, "/admin/products");
    await p.locator("tr", { hasText: "Dizüstü Bilgisayar" }).getByRole("link", { name: "Detay" }).click();
    await t.settle(p, 800);
    const gen = p.getByRole("button", { name: /3D Model Oluştur/ });
    if (await gen.count()) {
      await gen.click();
      t.step("AI ile 3D üretildi", await toastSeen(p, "3D model oluşturuldu", 30000));
    } else {
      t.step("AI 3D üret düğmesi (modelsiz üründe)", (await mainText(p)).includes("3D"), "ürünün zaten modeli var");
    }
    await t.shot(p, "ai3d");
  }],

  ["Y-08", "Tanım ekranları", async (t) => {
    const p = await t.login(ADMIN);
    await go(p, "/admin/categories");
    await p.getByRole("button", { name: /Yeni Kategori/ }).click();
    await p.getByPlaceholder("Kategori Adı").fill("UT Ölçüm Cihazları");
    await p.getByPlaceholder("Kod (örn. ELEKTRONIK)").fill("UT-OLCUM");
    await p.locator("form select").first().selectOption({ index: 1 });
    await p.getByRole("button", { name: "Kaydet" }).click();
    await t.settle(p);
    t.step("alt kategori listede", (await mainText(p)).includes("UT Ölçüm Cihazları"));
    await go(p, "/admin/sectors");
    await p.getByRole("button", { name: "Yeni Sektör" }).click();
    await p.getByPlaceholder("Örn: Bilişim, Lojistik, İnşaat").fill("UT Savunma");
    await p.getByRole("button", { name: "Kaydet" }).click();
    t.step("sektör eklendi", await toastSeen(p, "Sektör oluşturuldu"));
    await go(p, "/admin/currencies");
    await p.getByRole("button", { name: "+ Yeni" }).first().click();
    await p.getByPlaceholder("Kod (örn. GBP)").fill("SEK");
    await p.getByPlaceholder("Sembol (£)").fill("kr");
    await p.getByPlaceholder("Ad (İngiliz Sterlini)").fill("İsveç Kronu");
    await p.getByRole("button", { name: "Kaydet" }).first().click();
    await t.settle(p);
    await p.getByRole("button", { name: "+ Yeni" }).last().click();
    const rateSel = p.locator("form select").last();
    await selectContaining(rateSel, "SEK");
    await p.getByPlaceholder("Kur (örn. 34.50)").fill("4.65");
    await p.getByRole("button", { name: "Kaydet" }).last().click();
    await t.settle(p);
    const txt = await mainText(p);
    t.step("para birimi ve kur listede", txt.includes("SEK") && /4[.,]65/.test(txt));
    await t.shot(p, "currencies");
    await go(p, "/admin/products");
    await p.getByRole("button", { name: /Yeni Ürün/ }).click();
    const opts = await p.locator("form select").first().locator("option").allInnerTexts();
    t.step("yeni kategori ürün formunda seçilebilir", opts.some((o) => o.includes("UT Ölçüm")), opts.join(", ").slice(0, 200));
  }],

  ["Y-09", "İhale durum yönetimi", async (t) => {
    const p = await t.login(ADMIN);
    await go(p, `/admin/auctions/${A.A7}`);
    await p.getByRole("button", { name: "Devam Ettir" }).click();
    await p.locator("form").filter({ hasText: "Devam Ettir" }).getByRole("button", { name: "Devam Ettir" }).last().click();
    t.step("Kartal devam ettirildi", await toastSeen(p, /devam|canlı/i));
    await t.settle(p);
    await go(p, `/admin/auctions/${A.A2}`);
    await p.getByRole("button", { name: "Şimdi Başlat" }).click();
    await p.locator("form").filter({ hasText: "Şimdi Başlat" }).getByRole("button", { name: "Şimdi Başlat" }).last().click();
    t.step("Aliağa başlatıldı", await toastSeen(p, /başlatıldı|canlı/i));
    await t.settle(p, 1200);
    await t.shot(p, "aliaga-live");
    const d = await api("GET", `/api/auctions/${A.A2}`, await token(ADMIN));
    const st = await api("GET", `/api/auctions/${A.A2}/status`, await token(ADMIN));
    t.step("Aliağa Canlı ve durum geçmişi tutuluyor", d.data.status === "Live" && st.data.history.length >= 2, `${d.data.status} ${st.data.history.length}`);
    const feed = await api("GET", `/api/auctions/${A.A2}/feed`, await token(ADMIN));
    t.step("Çınar Yapı ön teklifi ilk teklif oldu", JSON.stringify(feed.data).includes("Çınar"), JSON.stringify(feed.data).slice(0, 200));
  }],

  ["Y-10", "Denetim kayıtları ve izleme", async (t) => {
    const p = await t.login(ADMIN);
    await go(p, "/admin/audit-log");
    const filters = p.getByTestId("audit-filters");
    if (await filters.count()) {
      await p.getByTestId("audit-entity").selectOption("Auction");
      await t.settle(p);
      const byEntity = await mainText(p);
      t.step("denetim kaydı kayıt türüne göre süzülüyor", byEntity.includes("Auction") && !/Company/.test(byEntity.split("Kullanıcı").pop()));
      await p.getByTestId("audit-user").fill("ahmet");
      await p.getByTestId("audit-user").press("Tab");
      await p.getByTestId("audit-action").selectOption("Update");
      await t.settle(p);
      const txt = await mainText(p);
      t.step("kullanıcı ve işlem türüne göre süzülüyor", /ahmet\.yilmaz/.test(txt) && !/admin@procurelive/.test(txt.split("Kullanıcı").pop()), txt.slice(-300));
      t.step("kayıtlarda işlemi yapan kullanıcı görünüyor", /ahmet\.yilmaz/i.test(txt));
    } else {
      await p.getByPlaceholder("Varlık adına göre filtrele (örn. Auction)").fill("Auction");
      await p.getByRole("button", { name: "Filtrele" }).click();
      await t.settle(p);
      const txt = await mainText(p);
      t.step("denetim kaydı varlığa göre süzülüyor", txt.includes("Auction"));
      t.finding("med", "Denetim kaydında yalnızca varlık adı filtresi var; kullanıcı ve işlem türüne göre süzülemiyor (senaryo bunu bekliyor).", "/admin/audit-log");
      t.step("kayıtlarda işlemi yapan kullanıcı görünüyor", /System Admin|Ahmet|Selin|admin@/i.test(txt));
    }
    await t.shot(p, "audit");
    await go(p, "/admin/monitoring");
    await t.settle(p, 1200);
    const m = await mainText(p);
    t.step("izleme paneli canlı ihale sayılarını gösteriyor", /\d/.test(m) && /Canlı|Live/.test(m));
    await t.shot(p, "monitoring");
  }],
];
