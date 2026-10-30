// Ortak test yardımcıları: tarayıcı, giriş, API, ekran görüntüsü ve senaryo/bulgu kaydı.
const { chromium } = require("playwright");
const fs = require("fs");
const path = require("path");

const WEB = "http://localhost:3000";
const API = "http://localhost:5228";
const QA = path.resolve(__dirname, "..");
const fx = JSON.parse(fs.readFileSync(path.join(QA, "data", "fixtures.json"), "utf8"));

const pw = (email) => (email === fx.admin.email ? fx.admin.password : fx.password);
const company = (name) => fx.companies[name].id;
const userByEmail = (email) => fx.users.find((u) => u.email === email);

async function api(method, p, token, body) {
  const res = await fetch(API + p, {
    method,
    headers: { "Content-Type": "application/json", "Accept-Language": "tr", ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  return { status: res.status, data };
}

async function token(email, password) {
  const r = await api("POST", "/api/auth/login", null, { email, password: password ?? pw(email) });
  if (r.status !== 200) throw new Error(`login ${email}: ${r.status} ${JSON.stringify(r.data)}`);
  return r.data.accessToken;
}

async function notifications(email, size = 40) {
  const r = await api("GET", `/api/notifications?page=1&pageSize=${size}`, await token(email));
  return r.data?.items ?? [];
}

function createRun(outName) {
  const OUT = path.join(QA, "out", outName);
  fs.rmSync(OUT, { recursive: true, force: true });
  fs.mkdirSync(path.join(OUT, "shots"), { recursive: true });
  let browser;
  let shotNo = 0;
  const results = [];

  async function start() { browser = await chromium.launch(); }
  async function stop() { await browser.close(); }

  /** Yeni bağlam + sayfa; konsol hataları ve başarısız API istekleri toplanır. */
  async function open({ mobile = false, locale = "tr-TR" } = {}) {
    const ctx = await browser.newContext(mobile
      ? { viewport: { width: 375, height: 812 }, isMobile: true, hasTouch: true, locale, timezoneId: "Europe/Istanbul", acceptDownloads: true }
      : { viewport: { width: 1440, height: 900 }, locale, timezoneId: "Europe/Istanbul", acceptDownloads: true });
    const page = await ctx.newPage();
    const issues = { console: [], api: [] };
    page.on("console", (m) => { if (m.type() === "error" && !/status of 40[0-9]/.test(m.text())) issues.console.push(m.text().slice(0, 240)); });
    page.on("pageerror", (e) => issues.console.push("[pageerror] " + e.message.slice(0, 240)));
    page.on("response", (r) => { if (r.url().startsWith(API) && r.status() >= 500) issues.api.push(`${r.status()} ${r.request().method()} ${r.url().replace(API, "")}`); });
    page.issues = issues;
    page.ctx = ctx;
    return page;
  }

  async function login(email, opts = {}) {
    const page = await open(opts);
    await page.goto(WEB + "/login");
    await page.fill('input[type="email"]', email);
    await page.fill('input[type="password"]', opts.password ?? pw(email));
    await page.click('button[type="submit"]');
    await page.waitForURL((u) => !u.pathname.startsWith("/login"), { timeout: 20000 });
    await settle(page);
    return page;
  }

  async function settle(page, ms = 400) {
    await page.waitForLoadState("networkidle", { timeout: 8000 }).catch(() => {});
    await page.waitForTimeout(ms);
  }

  async function shot(page, name, opts = {}) {
    const file = `${String(++shotNo).padStart(3, "0")}-${name.replace(/[^a-zA-Z0-9-]+/g, "-").slice(0, 60)}.jpg`;
    await page.screenshot({ path: path.join(OUT, "shots", file), fullPage: opts.full ?? true, type: "jpeg", quality: 70 }).catch(() => {});
    return file;
  }

  /** Senaryo: adımlar (geçti/kaldı), bulgular (önem + metin), görüntüler. Hata atarsa senaryo "hata" ile biter. */
  async function scenario(id, title, fn) {
    const r = { id, title, steps: [], findings: [], shots: [], ok: true, error: null, ms: 0 };
    const t0 = Date.now();
    const pages = [];
    const t = {
      step(name, cond, detail = "") {
        r.steps.push({ name, pass: !!cond, detail: String(detail).slice(0, 300) });
        if (!cond) r.ok = false;
        console.log(`   ${cond ? "✓" : "✗"} ${name}${cond ? "" : "  → " + String(detail).slice(0, 200)}`);
        return !!cond;
      },
      finding(sev, text, where = "") { r.findings.push({ sev, text, where }); console.log(`   ! [${sev}] ${text}`); },
      async shot(page, name, opts) { r.shots.push(await shot(page, `${id}-${name}`, opts)); },
      async login(email, opts) { const p = await login(email, opts); pages.push(p); return p; },
      async open(opts) { const p = await open(opts); pages.push(p); return p; },
      settle,
    };
    console.log(`\n▶ ${id} ${title}`);
    try {
      await fn(t);
    } catch (e) {
      r.ok = false;
      r.error = e.message.split("\n")[0].slice(0, 400);
      console.log(`   ✗ HATA: ${r.error}`);
      if (pages.length) await t.shot(pages[pages.length - 1], "error").catch(() => {});
    }
    for (const p of pages) {
      if (p.issues.console.length) r.findings.push({ sev: "low", text: "Konsol hatası: " + [...new Set(p.issues.console)].slice(0, 3).join(" | "), where: p.url() });
      if (p.issues.api.length) r.findings.push({ sev: "high", text: "Sunucu hatası: " + [...new Set(p.issues.api)].slice(0, 3).join(" | "), where: p.url() });
      await p.ctx.close().catch(() => {});
    }
    r.ms = Date.now() - t0;
    results.push(r);
    fs.writeFileSync(path.join(OUT, "results.json"), JSON.stringify(results, null, 1));
    return r;
  }

  return { OUT, ctx: {}, start, stop, open, login, settle, shot, scenario, results };
}

module.exports = { WEB, API, QA, fx, pw, company, userByEmail, api, token, notifications, createRun };
