import { expect, test } from './bravo';
import { chooseOption, openOptions } from './options';

test('a change of the theme is saved, and the page and its color scheme follow it', async ({ bravo }) => {
    const { page } = bravo;
    const colorSchemeIsDark = () => page.evaluate(() => matchMedia('(prefers-color-scheme: dark)').matches);
    const pageIsDark = () => page.locator('body').evaluate(body => body.classList.contains('dark'));

    // The theme is Auto at the start: the page has the color scheme that the window gives it
    const wasDark = await colorSchemeIsDark();
    expect(await pageIsDark()).toBe(wasDark);

    // The other theme, whatever the settings of the machine
    const theme = wasDark ? 'Light' : 'Dark';
    await openOptions(bravo, 'general');
    const saved = await chooseOption(bravo, 'theme', theme);

    expect(saved.status()).toBe(200);
    expect(await saved.json()).toMatchObject({ theme: theme });
    expect(await pageIsDark()).toBe(!wasDark);

    // The window sets the color scheme of its webview from the options that the host keeps
    await expect.poll(colorSchemeIsDark).toBe(!wasDark);
});

test('the diagnostic level Basic shows the diagnostics pane', async ({ bravo }) => {
    const diagnostics = bravo.page.locator('#diagnostics');
    await expect(diagnostics).toBeHidden();

    await openOptions(bravo, 'telemetry');
    const saved = await chooseOption(bravo, 'diagnosticlevel', 'Basic');

    expect(saved.status()).toBe(200);
    expect(await saved.json()).toMatchObject({ diagnosticLevel: 'Basic' });
    await expect(diagnostics).toBeVisible();
});
