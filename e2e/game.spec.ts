import { expect, test, type Page } from "@playwright/test";

async function fresh(page: Page): Promise<string[]> {
  const errors: string[] = [];
  page.on("console", (message) => { if (message.type() === "error") errors.push(message.text()); });
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  return errors;
}

async function start(page: Page, foothold = "africa") {
  await page.goto("/");
  await page.getByTestId("setup-seed").fill("playwright-seed");
  await page.getByTestId("start-run").click();
  await page.getByTestId("tutorial-dismiss").click();
  await page.getByTestId(`region-${foothold}`).click();
}

test("starts, places a foothold, and casts Whispered Dreams", async ({ page }) => {
  const errors = await fresh(page);
  await start(page);
  await page.getByTestId("speed-pause").click();
  await expect(page.getByTestId("region-africa")).toHaveAttribute("aria-label", /influence 8 percent/);
  await page.getByTestId("ability-whispered_dreams").click();
  await page.getByTestId("region-africa").click();
  await expect(page.getByTestId("region-africa")).toHaveAttribute("aria-label", /influence 14 percent/);
  await expect(page.locator(".resource").filter({ hasText: "Dread" }).locator("b")).toHaveText("10");
  await page.screenshot({ path: "test-results/cthulhu-1280x720.png" });
  expect(errors).toEqual([]);
});

test("unlocks an ability with Insight", async ({ page }) => {
  const errors = await fresh(page);
  await start(page);
  await page.getByTestId("speed-pause").click();
  await page.getByTestId("debug-panel").locator("summary").click();
  await page.getByTestId("debug-insight").click();
  await page.getByTestId("ability-impossible_omen").click();
  await expect(page.getByTestId("ability-impossible_omen")).toHaveAttribute("data-action", "cast");
  expect(errors).toEqual([]);
});

test("pause freezes simulation and reload resumes the saved run", async ({ page }) => {
  const errors = await fresh(page);
  await start(page, "oceania");
  await page.getByTestId("speed-pause").click();
  const clock = page.locator(".ticker > span");
  const before = await clock.textContent();
  await page.waitForTimeout(1100);
  await expect(clock).toHaveText(before ?? "0:00");
  await page.reload();
  await expect(page.locator(".phase")).toHaveText("playing");
  await expect(page.getByTestId("region-oceania")).toHaveAttribute("aria-label", /influence 8 percent/);
  expect(errors).toEqual([]);
});

test("reaches a scripted win through Awakening", async ({ page }) => {
  const errors = await fresh(page);
  await start(page);
  await page.getByTestId("debug-panel").locator("summary").click();
  await page.getByTestId("debug-win").click();
  await expect(page.getByRole("dialog")).toContainText("Cthulhu awakens");
  expect(errors).toEqual([]);
});

test("reaches a scripted suppression loss", async ({ page }) => {
  const errors = await fresh(page);
  await start(page);
  await page.getByTestId("debug-panel").locator("summary").click();
  await page.getByTestId("debug-loss").click();
  await expect(page.getByRole("dialog")).toContainText("The rite is broken");
  expect(errors).toEqual([]);
});
