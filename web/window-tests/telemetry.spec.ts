import { createHash } from 'node:crypto';
import { type Bravo, expect, test } from './bravo';
import { chooseOption, openOptions, showSection } from './options';
import type { TelemetryItem } from './test-ingestion';

// The telemetry of the page, sent to an endpoint of the test: the page gets it from the host, with the values of
// the host, and follows the consent of the user and the policy of an administrator.

// The SDK of the page sends what it tracked at most 15 seconds later, all at once
const sendSeconds = 15;

function namesOf(items: TelemetryItem[]) {
    return items.map(item => item.data.baseData.name);
}

function themesOf(items: TelemetryItem[]) {
    return items.filter(item => item.data.baseData.name === 'Theme').map(item => item.data.baseData.properties?.theme);
}

/** Turns telemetry on or off with its switch in the options dialog, and waits until the host has saved it. */
async function turnTelemetry(bravo: Bravo, on: boolean) {
    await showSection(bravo, 'telemetry');
    const control = bravo.page.locator('#dialog-options #body-telemetry #telemetryenabled');
    const saved = bravo.page.waitForResponse(response =>
        response.url() === `${bravo.address}/options` && response.request().method() === 'PUT');
    await control.locator('.slider').click();

    expect(await (await saved).json()).toMatchObject({ telemetryEnabled: on });
}

test.describe('telemetry on', () => {

    test.use({ telemetry: true });

    test('the page sends its start and its first page, as the user of the host, in the run of the host', async ({ bravo }) => {
        const { items } = bravo.ingestion;

        await expect.poll(() => namesOf(items), { timeout: 2 * sendSeconds * 1000 }).toEqual(expect.arrayContaining(['Start', 'Welcome']));

        // The identifier of 1.0.x: the hash of the computer and of the user, which names neither
        const userId = createHash('sha256').update(`${process.env.COMPUTERNAME}\\${process.env.USERNAME}`, 'utf8').digest('hex');
        const start = items.find(item => item.data.baseData.name === 'Start');
        expect(start.tags['ai.user.id']).toBe(userId);
        expect(start.tags['ai.session.id']).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/);
        expect(start.data.baseData.properties).toMatchObject({ ProductName: 'Bravo', PublishMode: 'None' });
        expect(items.map(item => item.data.baseType)).not.toContain('PageviewPerformanceData');
    });

    test('turned off, the page sends nothing more; turned on again, it sends from then', async ({ bravo }) => {
        const { items } = bravo.ingestion;
        await openOptions(bravo, 'telemetry');

        await turnTelemetry(bravo, false);
        await showSection(bravo, 'general');
        expect((await chooseOption(bravo, 'theme', 'Dark')).status()).toBe(200);

        await turnTelemetry(bravo, true);
        await showSection(bravo, 'general');
        expect((await chooseOption(bravo, 'theme', 'Light')).status()).toBe(200);

        // The change made while it was off would have gone with the one after, or before it
        await expect.poll(() => themesOf(items), { timeout: 2 * sendSeconds * 1000 }).toContain('Light');
        expect(themesOf(items)).toEqual(['Light']);
    });
});

test.describe('a policy that turns telemetry off', () => {

    test.use({ telemetry: true, settingsFile: JSON.stringify({ telemetryEnabled: true }), policies: { TelemetryEnabled: 0 } });

    test('stops the page from sending anything, whatever the user chose', async ({ bravo }) => {
        await openOptions(bravo, 'general');
        expect((await chooseOption(bravo, 'theme', 'Dark')).status()).toBe(200);

        // Nothing can tell that an item will not come: the test waits longer than the SDK waits to send
        await bravo.page.waitForTimeout((sendSeconds + 5) * 1000);

        expect(bravo.ingestion.items).toEqual([]);
        expect(bravo.requests.filter(request => request.url.startsWith(bravo.ingestion.origin))).toEqual([]);
    });
});
