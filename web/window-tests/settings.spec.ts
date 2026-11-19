import { expect, test } from './bravo';
import { chooseOption, closeOptions, openOptions } from './options';

// The file of the settings as 1.0.14 writes it, with the line ends of Windows: the dark theme, the diagnostic
// level Basic, and the update channel, a member that this version does not know.
const fileOf1014 = [
    '{',
    '  "telemetryEnabled": true,',
    '  "diagnosticLevel": 1,',
    '  "updateChannel": 0,',
    '  "updateCheckEnabled": true,',
    '  "theme": 2,',
    '  "proxy": null,',
    '  "useSystemBrowserForAuthentication": false,',
    '  "customTemplatesEnabled": true,',
    '  "customOptions": null',
    '}',
].join('\r\n');

const readFailure = 'Unable to read the settings: the default ones are used.';

test.describe('the file of 1.0.14', () => {

    test.use({ settingsFile: fileOf1014 });

    test('gives the page its settings at the start', async ({ bravo }) => {
        const { page } = bravo;

        await expect(page.locator('#diagnostics')).toBeVisible();
        await expect(page.locator('#notification-sidebar .item', { hasText: readFailure })).toHaveCount(0);

        // The options say the theme of the file, whatever the theme of the machine
        await openOptions(bravo, 'general');
        await expect(page.locator('#dialog-options #theme select.listener')).toHaveValue('Dark');
        await closeOptions(bravo);
    });

    test('is replaced at a change, as 1.0.14 reads it, with the member that this version does not know', async ({ bravo }) => {
        await openOptions(bravo, 'general');
        const saved = await chooseOption(bravo, 'theme', 'Light');
        expect(saved.status()).toBe(200);

        // The host answers once the file is replaced; an enum stays a number, which 1.0.14 reads
        const file = JSON.parse(await bravo.readSettingsFile());
        expect(file).toMatchObject({ theme: 1, diagnosticLevel: 1, updateChannel: 0, proxy: { type: 1 } });
    });
});

test.describe('a file that is not JSON', () => {

    test.use({ settingsFile: 'not JSON' });

    test('leaves the defaults, tells the user, and stays until a change', async ({ bravo }) => {
        const { page } = bravo;

        await expect(page.locator('#notification-sidebar .item', { hasText: readFailure })).toHaveCount(1);
        await expect(page.locator('#diagnostics')).toBeHidden();
        expect(await bravo.readSettingsFile()).toBe('not JSON');
    });
});
