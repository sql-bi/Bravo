import { mkdir } from 'node:fs/promises';
import path from 'node:path';
import { type Bravo, expect, test } from './bravo';
import { chooseOption, closeOptions } from './options';

// The screens of the window that two builds of the bundle are compared on (ADR 0019). Not a test of the window:
// "npm run screens -- take <name>" runs it, with its own configuration, playwright.screens.config.ts, and
// gives it the folder to write in. Each theme has an application of its own, which starts with the theme Auto.

const folder = process.env.BRAVO_SCREENS_FOLDER;

/** The sections of the options dialog, in the order of its menu. */
const sections = ['general', 'formatting', 'proxy', 'telemetry', 'dev', 'about'];

const themes = ['Dark', 'Light'];

/**
 * Writes a screen of the page, as `<theme>-<name>.png`, once the fonts of the page are loaded and the engine has
 * drawn two frames since the last change: a screen taken earlier can hold a piece of the frame before.
 */
async function take(bravo: Bravo, theme: string, name: string) {
    const { page } = bravo;
    await page.evaluate(async () => {
        await document.fonts.ready;
        await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
    });
    await page.screenshot({
        path: path.join(folder, `${theme.toLowerCase()}-${name}.png`),
        animations: 'disabled'
    });
}

test.beforeAll(async () => {
    if (!folder)
        throw new Error('The folder of the screens is not given: run "npm run screens -- take <name>".');
    await mkdir(folder, { recursive: true });
});

for (const theme of themes) {
    test(`the screens of the theme ${theme}`, async ({ bravo }) => {
        const { page } = bravo;
        const colorSchemeIsDark = () => page.evaluate(() => matchMedia('(prefers-color-scheme: dark)').matches);

        await page.locator('#ctrl-options').click();
        expect((await chooseOption(bravo, 'theme', theme)).status()).toBe(200);
        await closeOptions(bravo);
        await expect.poll(colorSchemeIsDark).toBe(theme === 'Dark');

        await take(bravo, theme, 'welcome');

        await page.locator('#ctrl-options').click();
        for (const section of sections) {
            await page.locator(`#dialog-options #item-${section}`).click();
            await expect(page.locator(`#dialog-options #body-${section}`)).toBeVisible();
            await take(bravo, theme, `options-${section}`);
        }
        await closeOptions(bravo);

        // A feature that has no route yet: a VPAX file opened from the welcome page
        await page.locator('.quick-open-vpax').click();
        await page.locator('#dialog-connect input.file-browser').setInputFiles({
            name: 'Model.vpax',
            mimeType: 'application/octet-stream',
            buffer: Buffer.alloc(0)
        });
        await expect(page.locator('.error-scene .message')).toHaveText('This feature is not available yet.');
        await take(bravo, theme, 'error');

        await page.locator('#ctrl-options').click();
        await page.locator('#dialog-options #item-telemetry').click();
        expect((await chooseOption(bravo, 'diagnosticlevel', 'Basic')).status()).toBe(200);
        await closeOptions(bravo);
        await expect(page.locator('#diagnostics')).toBeVisible();

        // The times of the log are those of the run: each is written as the same time, in the style of the pane
        await page.locator('#diagnostics .timestamp').evaluateAll(times => times.forEach(time => time.textContent = '00:00:00'));
        await take(bravo, theme, 'diagnostics');
    });
}
