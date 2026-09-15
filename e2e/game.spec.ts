import { expect, test, type Page } from "@playwright/test";
import { play } from "../src/tests/playthrough";
import { BALANCE as B } from "../src/data/balance";
import { defaultSave, LEGACY_SAVE_KEY, SAVE_KEY } from "../src/game/persistence";
import type { GameState } from "../src/game/types";
import { createGameState, placeStartingFoothold } from "../src/game/engine";

async function start(page: Page) {
  await page.goto("/");
  await page.getByTestId("setup-seed").fill("browser-playtest");
  await page.getByTestId("start-run").click();
  await page.getByTestId("tutorial-dismiss").click();
  await page.getByTestId("region-africa").click();
  await page.getByTestId("speed-pause").click();
}
async function resume(page: Page, state: GameState) {
  await page.addInitScript(({ key, save }) => { localStorage.setItem(key, JSON.stringify(save)); },
    { key: SAVE_KEY, save: { ...defaultSave(), state: { ...state, speed: 0 }, tutorialComplete: true } });
  await page.goto("/");
}
test("recruits, sacrifices, previews costs, and resumes a paused monthly run", async ({ page }) => {
  const errors: string[] = []; page.on("pageerror", e => errors.push(e.message));
  await start(page);
  await page.getByTestId("action-recruit").click();
  await expect(page.getByTestId("regional-followers")).toHaveText("135");
  await page.getByRole("button", { name: "Rituals", exact: true }).click();
  await page.getByTestId("action-sacrifice").click();
  await expect(page.getByTestId("regional-followers")).toHaveText("95");
  await expect(page.getByTestId("dread")).toHaveText("45");
  const before = await page.getByTestId("calendar").textContent();
  await page.waitForTimeout(2200); await expect(page.getByTestId("calendar")).toHaveText(before!);
  await page.reload(); await expect(page.getByTestId("regional-followers")).toHaveText("95");
  await expect(page.getByTestId("speed-pause")).toHaveAttribute("aria-pressed", "true");
  await page.getByTestId("debug-panel").locator("summary").click();
  await page.getByTestId("debug-year").click();
  await expect(page.getByTestId("calendar")).toHaveText("Year 2 · Month 1");
  expect(errors).toEqual([]);
});
test("finishes a legally prepared campaign through the visible alignment controls", async ({ page }) => {
  // This checkpoint is produced by ordinary engine commands up to the alignment, not state patching.
  await resume(page, play("hub", "browser-finale", B.preparationMonths));
  await page.getByTestId("debug-panel").locator("summary").click();
  await expect(page.getByTestId("phase")).toHaveText("alignment");
  await expect(page.getByTestId("site-count")).toHaveText("3 / 3 sites");
  await page.getByTestId("awakening-action").click();
  await expect(page.getByTestId("phase")).toHaveText("awakening");
  // The rite must be defended: occult threats strip preparation, so an unattended
  // alignment window no longer finishes. Counter what the chronicle offers, month by month.
  // Each threat card also holds a region-name button, so match the counter by its label.
  const affordable = page.locator('[data-testid^="threat-watch-"] button:not([disabled])').filter({ hasText: "Counter" });
  for (let month = 0; month < B.alignmentMonths; month++) {
    if (await page.getByTestId("phase").textContent() === "won") break;
    for (let guard = 0; guard < 8 && await affordable.count() > 0; guard++) await affordable.first().click();
    await page.getByTestId("debug-month").click();
  }
  await expect(page.getByTestId("phase")).toHaveText("won");
  await expect(page.getByRole("heading", { name: "Cthulhu awakens", exact: true })).toBeVisible();
  await expect(page.getByTestId("new-run")).toBeVisible();
});
test("misses the alignment deadline without a prepared cult", async ({ page }) => {
  await start(page); await page.getByTestId("debug-panel").locator("summary").click();
  for (let i = 0; i < Math.ceil(B.preparationMonths / 120); i++) await page.getByTestId("debug-decade").click();
  await expect(page.getByTestId("phase")).toHaveText("alignment");
  await expect(page.getByTestId("awakening-action")).toBeDisabled();
  await page.getByTestId("debug-year").click(); await page.getByTestId("debug-year").click();
  await expect(page.getByTestId("phase")).toHaveText("lost");
  await expect(page.getByRole("heading", { name: "The alignment has passed" })).toBeVisible();
});
test("preserves a legacy save when a revised run starts", async ({ page }) => {
  const legacy = JSON.stringify({ version: 1, state: { old: true }, settings: { muted: true, volume: 25 } });
  await page.addInitScript(({ key, value }) => localStorage.setItem(key, value), { key: LEGACY_SAVE_KEY, value: legacy });
  await page.goto("/"); await expect(page.getByText("Your earlier run is preserved.", { exact: false })).toBeVisible();
  await page.getByTestId("start-run").click();
  expect(await page.evaluate(key => localStorage.getItem(key), LEGACY_SAVE_KEY)).toBe(legacy);
});
test("shows public cults, regional threats and the three-resource HUD without overflow", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 });
  await resume(page, play("early", "visual", 150));
  await expect(page.getByTestId("followers")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Belief is not loyalty" })).toBeVisible();
  await expect(page.locator(".earth-map [data-country]")).toHaveCount(177);
  await page.locator('.earth-map [data-country="Australia"]').click();
  await expect(page.getByRole("heading", { name: "Oceania", exact: true })).toBeVisible();
  await page.locator('.earth-map [data-country="United Republic of Tanzania"]').click();
  await expect(page.getByRole("heading", { name: "Africa", exact: true })).toBeVisible();
  await page.screenshot({ path: "test-results/awakening-desktop.png", fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole("combobox", { name: "Choose a region" })).toBeVisible();
  await page.getByRole("combobox", { name: "Choose a region" }).selectOption("europe");
  await expect(page.getByRole("heading", { name: "Europe", exact: true })).toBeVisible();
  await page.screenshot({ path: "test-results/awakening-mobile.png", fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
test("runs at monthly playback speed and pauses when the guide opens", async ({ page }) => {
  await start(page);
  await page.getByTestId("speed-2x").click();
  await expect(page.getByTestId("calendar")).not.toHaveText("Year 1 · Month 1", { timeout: 2500 });
  await page.getByRole("button", { name: "Guide", exact: true }).click();
  const before = await page.getByTestId("calendar").textContent();
  await page.waitForTimeout(1300); await expect(page.getByTestId("calendar")).toHaveText(before!);
});

test("keeps chronicle and projected threat consequences on-screen at 2K and laptop sizes", async ({ page }) => {
  const state = placeStartingFoothold(createGameState({ seed: "feedback-ui", difficulty: "normal" }), "africa").state;
  state.regions.africa.threat = { kind: "occult", severity: 3, dueMonth: 2 };
  state.regions.africa.site = { invested: true, progress: 24 };
  await page.setViewportSize({ width: 2560, height: 1440 });
  await resume(page, state);
  const dock = page.getByTestId("chronicle-dock");
  await expect(dock).toBeInViewport({ ratio: 1 });
  // Losses are a share of the cell now, so the projection is derived rather than pinned.
  const projected = Math.ceil(Math.max(3 * B.threatLossFloor, state.regions.africa.population.followers * 3 * B.threatLossShare));
  await expect(page.getByTestId("threat-watch-africa")).toContainText("Lose " + projected + " followers");
  await expect(page.getByTestId("threat-watch-africa")).toContainText("ritual −6 preparation months");
  await expect(page.getByTestId("threat-marker-africa")).toBeVisible();
  const marker = await page.getByTestId("threat-marker-africa").getAttribute("transform");
  await page.getByTestId("threat-marker-africa").click();
  await expect(page.getByRole("heading", { name: "Africa", exact: true })).toBeVisible();
  await page.screenshot({ path: "test-results/feedback-2k.png" });
  await page.setViewportSize({ width: 1280, height: 720 });
  await expect(dock).toBeInViewport({ ratio: 1 });
  await page.screenshot({ path: "test-results/feedback-laptop.png" });
  await page.reload();
  await expect(page.getByTestId("threat-marker-africa")).toHaveAttribute("transform", marker!);
});
