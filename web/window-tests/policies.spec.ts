import { type Bravo, expect, test } from './bravo';
import { chooseOption, openOptions } from './options';

// The policies of 1.0.x, with the values that their templates write: the page reads them at its start and locks
// their options, which keep the choice of the user.

const policyNotice = 'This option is managed by your organization.';

// The file of the settings of a user who turned on the other switches that the policies lock
function settingsOf(updateCheckEnabled: boolean) {
    return JSON.stringify({
        telemetryEnabled: true,
        updateCheckEnabled,
        useSystemBrowserForAuthentication: true,
        customTemplatesEnabled: true
    });
}

/** The requests of a start of the page to the host, until it reads the events, after its own update check. */
async function requestsOfAStart(bravo: Bravo): Promise<string[]> {
    bravo.clear();
    await bravo.page.reload();
    await expect.poll(() => bravo.hostRequests()).toContain('GET /events');
    return bravo.hostRequests();
}

async function expectLocked(bravo: Bravo, section: 'general' | 'telemetry' | 'dev', option: string, on: boolean) {
    await bravo.page.locator(`#dialog-options #item-${section}`).click();
    const control = bravo.page.locator(`#dialog-options #body-${section} #${option}`);
    await expect(control.locator('input.listener')).toBeDisabled();
    await expect(control.locator('input.listener')).toBeChecked({ checked: on });
    await expect(control.locator('.policy-notice')).toHaveText(policyNotice);
}

test.describe('policies that turn the switches off', () => {

    test.use({
        settingsFile: settingsOf(true),
        policies: { TelemetryEnabled: 0, UpdateCheckEnabled: 0, UseSystemBrowserForAuthentication: 0, CustomTemplatesEnabled: 0 }
    });

    test('are read at the start, which checks for no update', async ({ bravo }) => {
        const requests = await requestsOfAStart(bravo);

        expect(requests).toContain('GET /policies');
        expect(requests).not.toContain('GET /releases/latest');
    });

    test('lock the options off and say why', async ({ bravo }) => {
        const { page } = bravo;
        await openOptions(bravo, 'telemetry');

        await expectLocked(bravo, 'telemetry', 'telemetryenabled', false);
        await expectLocked(bravo, 'general', 'usesystembrowserforauthentication', false);
        await expectLocked(bravo, 'dev', 'customtemplatesenabled', false);

        // The update check can be neither asked for nor turned on
        await page.locator('#dialog-options #item-about').click();
        const about = page.locator('#dialog-options #body-about');
        await expect(about.locator('.auto-check-option input')).toBeDisabled();
        await expect(about.locator('.auto-check-option input')).not.toBeChecked();
        await expect(about.locator('.policy-notice')).toHaveText(policyNotice);
        await expect(about.locator('.update-status')).toHaveCount(0);
        expect(bravo.hostRequests()).not.toContain('GET /releases/latest');
    });

    test('leave the choices of the user in the file of the settings', async ({ bravo }) => {
        await openOptions(bravo, 'general');
        const saved = await chooseOption(bravo, 'theme', 'Dark');
        expect(saved.status()).toBe(200);

        const file = JSON.parse(await bravo.readSettingsFile());
        expect(file).toMatchObject({ theme: 2, telemetryEnabled: true, updateCheckEnabled: true, useSystemBrowserForAuthentication: true, customTemplatesEnabled: true });
    });
});

test.describe('a policy that turns the update check on', () => {

    test.use({ settingsFile: settingsOf(false), policies: { UpdateCheckEnabled: 1 } });

    test('makes the start check for updates, which the user turned off', async ({ bravo }) => {
        expect(await requestsOfAStart(bravo)).toContain('GET /releases/latest');
    });

    test('locks the option on and leaves the other ones to the user', async ({ bravo }) => {
        const { page } = bravo;
        await openOptions(bravo, 'about');

        const about = page.locator('#dialog-options #body-about');
        await expect(about.locator('.auto-check-option input')).toBeDisabled();
        await expect(about.locator('.auto-check-option input')).toBeChecked();
        await expect(about.locator('.policy-notice')).toHaveText(policyNotice);
        await expect(about.locator('.update-status .up-to-date')).toBeVisible();

        await page.locator('#dialog-options #item-telemetry').click();
        const telemetry = page.locator('#dialog-options #body-telemetry #telemetryenabled');
        await expect(telemetry.locator('input.listener')).toBeEnabled();
        await expect(telemetry.locator('.policy-notice')).toHaveCount(0);
    });
});
