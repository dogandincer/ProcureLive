// E-posta altyapısı senaryoları: Admin ayar ekranı (Mailpit ile test/kayıt), hata yolları, kullanıcı tercihi,
// bildirim e-postasının uçtan uca gidişi, yeniden deneme, yetki ve mobil görünüm.
// Önkoşul: docker compose up -d mailpit (SMTP 1025, API 8025). Kullanım: node e2e/email.js [çıktı adı]
const { execFileSync } = require("child_process");
const { createRun, api, token, fx, WEB } = require("./lib");

const MAILPIT = "http://localhost:8025/api/v1";
const mail = {
  async clear() { await fetch(`${MAILPIT}/messages`, { method: "DELETE" }); },
  async list() { return (await (await fetch(`${MAILPIT}/messages`)).json()).messages; },
  async waitFor(pred, ms = 25000) {
    const until = Date.now() + ms;
    while (Date.now() < until) {
      const hit = (await this.list()).filter(pred);
      if (hit.length) return hit;
      await new Promise((r) => setTimeout(r, 1000));
    }
    return [];
  },
};
const to = (m) => m.To.map((x) => x.Address);
const sql = (q) => execFileSync("docker", ["exec", "procurelive-postgres", "psql", "-tA", "-U", "procurelive_user", "-d", "procurelive", "-c", q]).toString().trim();

const GOOD = { enabled: true, host: "localhost", port: 1025, security: "None", username: null, password: null, clearPassword: false,
  fromAddress: "ihale@kuzey.local", fromName: "Kuzey e-İhale", replyTo: null, disabledNotificationTypes: [] };

(async () => {
  const run = createRun(process.argv[2] || "email");
  await run.start();
  const only = (process.argv[3] || "").split(",").filter(Boolean);
  const scenario = (id, title, fn) => (only.length && !only.includes(id) ? null : run.scenario(id, title, fn));
  const adminToken = await token("admin@procurelive.local", "ChangeMe123!");
  // Başlangıç: M-01 koşacaksa boş/kapalı ayar (ekranın ilk kurulum hali), yoksa Mailpit'e gönderen çalışan ayar.
  const fresh = !only.length || only.includes("M-01");
  await api("PUT", "/api/settings/email", adminToken, fresh ? { ...GOOD, enabled: false, host: "", fromAddress: "", fromName: "" } : GOOD);

  await scenario("M-01", "Admin SMTP ayarını hazır ayarla doldurur, test eder, açar ve kaydeder", async (t) => {
    await mail.clear();
    const p = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    await p.getByRole("link", { name: "E-posta Ayarları" }).first().click();
    await p.waitForURL(/\/admin\/email-settings/);
    await p.getByTestId("email-settings-form").waitFor();
    t.step("Menüden ekran açılır", true);
    t.step("Kapalıyken uyarı görünür", await p.getByTestId("email-disabled-note").isVisible());
    await t.shot(p, "bos");

    await p.getByTestId("email-preset").selectOption("mailpit");
    t.step("Hazır ayar sunucu/port/güvenliği doldurur",
      (await p.getByTestId("email-host").inputValue()) === "localhost" && (await p.getByTestId("email-port").inputValue()) === "1025"
      && (await p.getByTestId("email-security").inputValue()) === "None");
    await p.getByTestId("email-from-address").fill("ihale@kuzey.local");
    await p.getByTestId("email-from-name").fill("Kuzey e-İhale");
    await p.getByTestId("email-reply-to").fill("satinalma@kuzey.local");
    await p.getByTestId("email-types").locator('input[data-type="Outbid"]').uncheck();

    await p.getByTestId("email-test-to").fill("test-alici@kuzey.local");
    await p.getByRole("button", { name: "Test e-postası gönder" }).click();
    const result = p.getByTestId("email-test-result");
    await result.waitFor();
    t.step("Test sonucu başarılı (kaydetmeden)", /gönderildi/.test(await result.innerText()), await result.innerText());
    const got = await mail.waitFor((m) => to(m).includes("test-alici@kuzey.local"), 8000);
    t.step("Test e-postası Mailpit'e ulaştı", got.length === 1 && got[0].From.Name === "Kuzey e-İhale", JSON.stringify(got[0]?.From));

    await p.getByTestId("email-enabled").getByRole("switch").click();
    await t.shot(p, "dolu");
    const put = p.waitForResponse((r) => r.url().endsWith("/api/settings/email") && r.request().method() === "PUT");
    await p.getByTestId("email-save").click();
    t.step("Kaydet 204", (await put).status() === 204);
    await t.settle(p);
    await p.reload();
    await p.getByTestId("email-settings-form").waitFor();
    await t.settle(p);
    t.step("Yeniden yüklemede değerler korunur",
      (await p.getByTestId("email-host").inputValue()) === "localhost"
      && (await p.getByTestId("email-enabled").getByRole("switch").getAttribute("aria-checked")) === "true"
      && !(await p.getByTestId("email-types").locator('input[data-type="Outbid"]').isChecked())
      && (await p.getByTestId("email-types").locator('input[data-type="ProcessQuestion"]').isChecked()));
    t.step("Kaydet düğmesi değişiklik yokken pasif", await p.getByTestId("email-save").isDisabled());
    t.step("Son değişiklik bilgisi görünür", /Son değişiklik/.test(await p.getByTestId("email-settings-form").innerText()));
  });

  await scenario("M-02", "Hata yolları: yanlış port test sonucu, eksik alanla kayıt, şifre maskesi", async (t) => {
    const p = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    await p.goto(WEB + "/admin/email-settings");
    await p.getByTestId("email-settings-form").waitFor();
    await t.settle(p);
    await p.getByTestId("email-port").fill("2999");
    await p.getByRole("button", { name: "Test e-postası gönder" }).click();
    const result = p.getByTestId("email-test-result");
    await result.waitFor({ timeout: 40000 });
    t.step("Bağlanamayan sunucu anlaşılır hata verir", /Gönderilemedi/.test(await result.innerText()), await result.innerText());
    await t.shot(p, "test-hata");

    await p.getByTestId("email-port").fill("1025");
    await p.getByTestId("email-host").fill("");
    await p.getByTestId("email-save").click();
    const err = p.getByTestId("email-save-error");
    await err.waitFor();
    t.step("Açıkken sunucu boşsa kayıt reddedilir", /SMTP sunucusu boş olamaz/.test(await err.innerText()), await err.innerText());

    await p.getByTestId("email-host").fill("localhost");
    await p.getByTestId("email-password").fill("gizli-sifre-123");
    const put = p.waitForResponse((r) => r.url().endsWith("/api/settings/email") && r.request().method() === "PUT");
    await p.getByTestId("email-save").click();
    t.step("Şifreyle kayıt 204", (await put).status() === 204);
    await t.settle(p);
    const dto = await api("GET", "/api/settings/email", adminToken);
    t.step("API şifreyi döndürmez, yalnızca kayıtlı olduğunu", dto.data.hasPassword === true && !JSON.stringify(dto.data).includes("gizli-sifre-123"));
    t.step("Veritabanında şifre düz metin değil", !sql(`select "SmtpPasswordProtected" from "EmailSettings"`).includes("gizli-sifre-123"));
    const audit = sql(`select count(*) filter (where v = '***') || '/' || count(*) filter (where v <> '***') from (select "NewValues"->>'SmtpPasswordProtected' v from "AuditLogs" where "EntityName" = 'EmailSettings' union all select "OldValues"->>'SmtpPasswordProtected' from "AuditLogs" where "EntityName" = 'EmailSettings') x`);
    t.step("Denetim kaydında şifre maskeli", /^[1-9]\d*\/0$/.test(audit), `maskeli/açık: ${audit}`);
    t.step("Şifre alanı kayıtlı şifre ipucunu gösterir", /Kayıtlı şifre var/.test(await p.getByTestId("email-password").getAttribute("placeholder")));

    // Mailpit kimlik doğrulaması istemez: kayıtlı şifreyi sil, sonraki senaryolar temiz ayarla çalışsın.
    await p.getByTestId("email-clear-password").check();
    t.step("Şifreyi sil seçilince alan pasif", await p.getByTestId("email-password").isDisabled());
    await p.getByTestId("email-save").click();
    await t.settle(p, 800);
    t.step("Kayıtlı şifre silindi", (await api("GET", "/api/settings/email", adminToken)).data.hasPassword === false);
  });

  await scenario("M-03", "Bildirim e-postası uçtan uca: soru → alıcılar, cevap → tedarikçi; tercih kapalı kullanıcı atlanır", async (t) => {
    await mail.clear();
    sql(`update "AspNetUsers" set "EmailNotificationsEnabled" = true where "Email" like '%@egebilisim.example'`);
    // Tedarikçi kullanıcısı kendi tercihini Profil'den kapatır.
    const s = await t.login("destek@egebilisim.example");
    await s.goto(WEB + "/profile");
    const toggle = s.getByTestId("email-notifications-toggle").getByRole("switch");
    await toggle.waitFor();
    await t.settle(s);
    t.step("Profilde tercih varsayılan açık", (await toggle.getAttribute("aria-checked")) === "true");
    const pref = s.waitForResponse((r) => r.url().endsWith("/api/auth/me/preferences"));
    await toggle.click();
    t.step("Tercih kaydı 204", (await pref).status() === 204);
    await s.reload();
    await s.getByTestId("email-notifications-toggle").waitFor();
    await t.settle(s, 800);
    t.step("Yeniden yüklemede kapalı kalır", (await s.getByTestId("email-notifications-toggle").getByRole("switch").getAttribute("aria-checked")) === "false");
    await t.shot(s, "profil");

    const rfq = fx.rfq.Q1 ?? Object.values(fx.rfq)[0];
    const rfqId = sql(`select p."Id" from "RfqInvitations" i join "RfqProcesses" p on p."Id"=i."RfqProcessId" join "Companies" c on c."Id"=i."CompanyId" where c."Name" like 'Ege Bilişim%' and p."Status" = 1 limit 1`);
    t.step("Ege'nin açık RFQ'su var", !!rfqId, rfq);
    const ege = await token("teklif@egebilisim.example", "Test1234!");
    const ask = await api("POST", `/api/processes/rfq/${rfqId}/questions`, ege, { question: "Fatura hangi firmaya kesilecek? (e-posta testi)" });
    t.step("Soru gönderildi", ask.status === 200 || ask.status === 201, ask.status);
    const buyers = await mail.waitFor((m) => m.Subject === "Yeni Tedarikçi Sorusu");
    t.step("Sorumlu alıcılara e-posta gitti", buyers.length >= 1, buyers.map(to).join(","));
    t.step("İç kullanıcı e-postası yönetim ekranına bağlanır",
      buyers.length > 0 && (await (await fetch(`${MAILPIT}/message/${buyers[0].ID}`)).json()).HTML.includes(`/admin/rfq/${rfqId}#qa`));

    const answer = await api("PUT", `/api/process-questions/${String(ask.data).replace(/"/g, "")}/answer`, adminToken, { answer: "Kuzey Enerji A.Ş.'ye kesilecektir.", isPublic: false });
    t.step("Cevap verildi", answer.status === 204, answer.status);
    const supplier = await mail.waitFor((m) => m.Subject === "Sorunuz Cevaplandı");
    const all = supplier.flatMap(to);
    t.step("Tedarikçinin tercihi açık kullanıcısına gitti", all.includes("teklif@egebilisim.example"), all.join(","));
    t.step("Tercihi kapalı kullanıcıya gitmedi", !all.includes("destek@egebilisim.example"), all.join(","));
    const detail = await (await fetch(`${MAILPIT}/message/${supplier[0].ID}`)).json();
    t.step("Tedarikçi e-postası kendi ekranına bağlanır", detail.HTML.includes(`/supplier/rfq/${rfqId}#qa`));
    t.step("Düz metin sürümü de var", /Kaydı aç: http/.test(detail.Text));

    // Tercihi geri aç (sonraki koşular için).
    await s.goto(WEB + "/profile");
    await s.getByTestId("email-notifications-toggle").waitFor();
    await t.settle(s, 800);
    await s.getByTestId("email-notifications-toggle").getByRole("switch").click();
    await t.settle(s, 800);
  });

  await scenario("M-04", "Gönderim kayıtları: süzgeç, başarısız e-postayı yeniden deneme", async (t) => {
    await mail.clear();
    const id = sql(`select "Id" from "NotificationChannelLogs" where "Channel" = 1 and "Status" = 1 and "Recipient" = 'teklif@egebilisim.example' order by "SentAt" desc limit 1`);
    sql(`update "NotificationChannelLogs" set "Status" = 2, "AttemptCount" = 3, "ErrorMessage" = '5.7.1 Simüle edilmiş ret' where "Id" = '${id}'`);
    const p = await t.login("admin@procurelive.local", { password: "ChangeMe123!" });
    await p.goto(WEB + "/admin/email-settings?status=Failed");
    await p.getByTestId("email-logs").waitFor();
    await t.settle(p, 800);
    const rows = p.getByTestId("email-log-row");
    t.step("Başarısız süzgeci yalnızca başarısızı gösterir", (await rows.count()) === 1 && /Başarısız/.test(await rows.first().innerText()), await rows.count());
    t.step("Hata ayrıntısı ve deneme sayısı görünür", /Simüle edilmiş ret/.test(await rows.first().innerText()) && /3 deneme/.test(await rows.first().innerText()));
    await t.shot(p, "basarisiz");
    await rows.first().getByRole("button", { name: "Yeniden dene" }).click();
    await t.settle(p);
    t.step("Yeniden dene sonrası başarısız listesi boşalır", (await rows.count()) === 0);
    const got = await mail.waitFor((m) => to(m).includes("teklif@egebilisim.example"));
    t.step("Yeniden kuyruğa alınan e-posta gönderildi", got.length === 1);
    t.step("Kayıt Gönderildi oldu", sql(`select "Status" from "NotificationChannelLogs" where "Id" = '${id}'`) === "1");

    await p.getByTestId("status-filter").getByRole("button", { name: /^Gönderildi/ }).click();
    await t.settle(p);
    t.step("Gönderildi süzgeci sayaçlı ve dolu", (await rows.count()) > 0);
    await p.getByPlaceholder("Alıcı veya konu ara").fill("teklif@egebilisim");
    await t.settle(p, 900);
    const texts = await rows.allInnerTexts();
    t.step("Arama alıcıya göre süzer", texts.length > 0 && texts.every((x) => x.includes("teklif@egebilisim.example")), texts.length);
  });

  await scenario("M-05", "Yetki: satınalmacı ekranı ve API'yi kullanamaz", async (t) => {
    const p = await t.login("ahmet.yilmaz@kuzey.local");
    t.step("Menüde E-posta Ayarları yok", (await p.getByRole("link", { name: "E-posta Ayarları" }).count()) === 0);
    await p.goto(WEB + "/admin/email-settings");
    await t.settle(p);
    t.step("Doğrudan adres yönlendirilir", !p.url().includes("/admin/email-settings"), p.url());
    const buyer = await token("ahmet.yilmaz@kuzey.local", "Test1234!");
    t.step("API 403", (await api("GET", "/api/settings/email", buyer)).status === 403);
    t.step("Test ucu 403", (await api("POST", "/api/settings/email/test", buyer, { to: "x@y.z", settings: GOOD })).status === 403);
  });

  await scenario("M-06", "Görünüm: mobil ve İngilizce", async (t) => {
    const m = await t.login("admin@procurelive.local", { password: "ChangeMe123!", mobile: true });
    await m.goto(WEB + "/admin/email-settings");
    await m.getByTestId("email-settings-form").waitFor();
    await t.settle(m, 800);
    const overflow = await m.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    t.step("Mobilde yatay taşma yok", overflow <= 1, overflow);
    await t.shot(m, "mobil");
    const e = await t.login("admin@procurelive.local", { password: "ChangeMe123!", locale: "en-US" });
    await e.evaluate(() => localStorage.setItem("procurelive-locale", "en"));
    await e.goto(WEB + "/admin/email-settings");
    await e.getByTestId("email-settings-form").waitFor();
    await t.settle(e, 800);
    const text = await e.locator("main").innerText();
    t.step("Ekranda çevrilmemiş anahtar yok", !/emailSettings\./.test(text), (text.match(/emailSettings\.[\w.]+/) || [""])[0]);
    await t.shot(e, "ingilizce");
  });

  await run.stop();
  const ok = run.results.filter((r) => r.ok).length;
  console.log(`\n${ok}/${run.results.length} senaryo geçti; bulgu: ${run.results.reduce((n, r) => n + r.findings.length, 0)}`);
})();
