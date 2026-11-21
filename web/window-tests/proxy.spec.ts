import { type Bravo, expect, test } from './bravo';
import { openOptions, showSection } from './options';

// The proxy options: the proxy server of the user, its credentials and the connection test. The proxy server
// is one of the test, on the loopback address, and the host reaches it as it reaches any other. No test shows
// the dialog of Windows that asks for the credentials, nor touches the credentials of the machine: a test of
// the credentials answers in place of the host, and has no proxy server that would ask for them.

const credentialsAsked = 'The proxy server of your network asks for a user name and a password. Enter them in the proxy options.';
const restartNotice = 'Changes to the proxy server, its address and its exclusions take effect the next time you start Bravo.';

const promptRequest = 'POST /network/proxy-credentials/prompt';
const deleteRequest = 'DELETE /network/proxy-credentials';

/** The switch of the custom credentials: its input, which tells its state, and its slider, which the user clicks. */
function customCredentials(bravo: Bravo) {
    const option = bravo.page.locator('#dialog-options #proxy-usedefaultcredentials');
    return { input: option.locator('input.listener'), slider: option.locator('.slider') };
}

/** Runs the connection test of the proxy options that are open; returns its one site: a development build asks its update feed alone. */
async function testConnection(bravo: Bravo) {
    await bravo.page.locator('#dialog-options #proxy-test .button').click();
    return bravo.page.locator('#dialog-options .connection-test-results .site');
}

test.describe('a proxy server that asks for credentials', () => {

    test.use({ proxyServer: 'asks' });

    test('is told at the start, by the connection test and by the update check of the options', async ({ bravo }) => {
        const { page } = bravo;
        const notification = page.locator('#notification-sidebar .item', { hasText: credentialsAsked });
        await expect(notification).toHaveCount(1);

        // The link of the notification opens the proxy options
        await page.locator('#ctrl-notifications').click();
        await notification.locator('.actions .link').click();
        await expect(page.locator('#dialog-options #body-proxy')).toBeVisible();

        const site = await testConnection(bravo);
        await expect(site.locator('.name')).toContainText('Bravo updates');
        await expect(site.locator('.result')).toHaveText(credentialsAsked);
        await expect(site.locator('.route')).toHaveText(`Through the proxy server ${bravo.proxy.address}`);

        await showSection(bravo, 'about');
        await expect(page.locator('#dialog-options .update-status .notice')).toHaveText(credentialsAsked);
    });
});

test.describe('a proxy server that answers', () => {

    test.use({ proxyServer: 'answers' });

    test('is the way of the connection test to the update feed, which is reached', async ({ bravo }) => {
        await openOptions(bravo, 'proxy');
        const site = await testConnection(bravo);

        await expect(site.locator('.result')).toHaveText('Reached');
        await expect(site.locator('.route')).toHaveText(`Through the proxy server ${bravo.proxy.address}`);
        expect(bravo.proxy.requests.length).toBeGreaterThan(0);
    });
});

test('with no proxy server the connection test reaches the update feed, and the options say when a change applies', async ({ bravo }) => {
    await openOptions(bravo, 'proxy');
    const site = await testConnection(bravo);

    await expect(site.locator('.result')).toHaveText('Reached');
    await expect(site.locator('.route')).toHaveText('No proxy server');
    await expect(bravo.page.locator('#dialog-options #body-proxy .option-notice')).toHaveText(restartNotice);
});

test('custom credentials are asked for when the user turns them on, and are on once the user has entered them', async ({ bravo }) => {
    const { input, slider } = customCredentials(bravo);
    await openOptions(bravo, 'proxy');

    // A user who closes the dialog of Windows with no credentials
    await bravo.answerProxyCredentialRequests(false);
    await slider.click();
    await expect.poll(() => bravo.hostRequests()).toContain(promptRequest);
    await expect(input).not.toBeChecked();

    // A user who enters them
    await bravo.answerProxyCredentialRequests(true);
    const saved = bravo.page.waitForResponse(response =>
        response.url() === `${bravo.address}/options` && response.request().method() === 'PUT');
    await slider.click();

    expect(await (await saved).json()).toMatchObject({ proxy: { useDefaultCredentials: false } });
    await expect(input).toBeChecked();
});

test.describe('custom credentials that the user chose before', () => {

    test.use({ settingsFile: JSON.stringify({ proxy: { type: 1, useDefaultCredentials: false, bypassOnLocal: true } }) });

    test('are asked for again by their link, stay until the user confirms, and are then forgotten', async ({ bravo }) => {
        const { input, slider } = customCredentials(bravo);
        const confirm = bravo.page.locator('#dialog-confirm-delete-proxy-cred');
        await bravo.answerProxyCredentialRequests(true);
        await openOptions(bravo, 'proxy');

        await bravo.page.locator('#dialog-options .edit-proxy-credentials').click();
        await expect.poll(() => bravo.hostRequests()).toContain(promptRequest);
        await expect(input).toBeChecked();

        await slider.click();
        await confirm.locator('.button[data-action="cancel"]').click();
        await expect(input).toBeChecked();

        await slider.click();
        await confirm.locator('.button[data-action="ok"]').click();
        await expect.poll(() => bravo.hostRequests()).toContain(deleteRequest);
        await expect(input).not.toBeChecked();
    });
});
