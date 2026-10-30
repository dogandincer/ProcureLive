// Arayüz etkileşim yardımcıları (senaryolar ortak kullanır).
const WEB = "http://localhost:3000";

/** Etiketin hemen ardındaki alan (label + input/select/textarea kardeş yapısı) ya da label'a bağlı alan. */
function field(scope, labelText) {
  const label = scope.locator("label", { hasText: labelText }).first();
  return label.locator("xpath=following-sibling::*[self::input or self::select or self::textarea][1] | .//input | .//select | .//textarea").first();
}

async function go(page, route) {
  await page.goto(WEB + route);
  await page.waitForLoadState("networkidle", { timeout: 8000 }).catch(() => {});
  await page.waitForTimeout(500);
}

/** Sarı seçim alanı (LookupField): tetikleyiciye tıkla, pencerede ara, satırı seç. */
async function pickLookup(page, trigger, search, rowText = search) {
  await trigger.click();
  const modal = page.locator(".fixed.inset-0").filter({ has: page.locator('input[type="search"]') }).last();
  await modal.locator('input[type="search"]').fill(search);
  await page.waitForTimeout(500);
  await modal.locator("tr", { hasText: rowText }).first().click();
  await page.waitForTimeout(300);
}

async function selectFirm(page, nameOrCode) {
  await page.locator('button[title="Firma seçimi"]:visible').first().click();
  await page.locator("button:visible", { hasText: nameOrCode }).last().click();
  await page.waitForTimeout(800);
}

async function selectWorkspace(page, name) {
  await page.locator('button[title="İşyeri seçimi"]:visible').first().click();
  await page.locator("button:visible", { hasText: name }).last().click();
  await page.waitForTimeout(800);
}

/** Uygulamanın ConfirmDialog'u (veya rol=dialog) içinde düğmeye bas. */
async function confirm(page, buttonName) {
  const dlg = page.locator('[data-testid="confirm-dialog"], [role="dialog"]').last();
  await dlg.waitFor({ timeout: 8000 });
  await dlg.getByRole("button", { name: buttonName }).last().click();
}

async function openBell(page) {
  await page.getByRole("button", { name: /Bildirim/ }).first().click();
  await page.getByTestId("notification-panel").waitFor({ timeout: 8000 });
}

async function toastSeen(page, text, timeout = 8000) {
  try { await page.getByText(text).first().waitFor({ timeout }); return true; } catch { return false; }
}

async function logout(page) {
  await page.getByRole("button", { name: /Çıkış|Sign out|Log out/ }).first().click();
  await page.waitForURL("**/login", { timeout: 10000 });
}

/** Metni içeren seçeneği seç (etiket tam eşleşmesi gerekmez). */
async function selectContaining(select, text) {
  const value = await select.locator("option", { hasText: text }).first().getAttribute("value");
  await select.selectOption(value);
}

const bodyText = (page) => page.locator("body").innerText();
const mainText = (page) => page.locator("main").innerText();

module.exports = { WEB, selectContaining, field, go, pickLookup, selectFirm, selectWorkspace, confirm, openBell, toastSeen, logout, bodyText, mainText };
