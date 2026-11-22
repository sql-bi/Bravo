import { expect, test } from './bravo';
import { chooseOption, closeOptions, openOptions, showSection } from './options';
import { feedOf } from './update-feed';

// A development build is installed by no package and carries no runtime: the file of its update is the
// archive without the runtime, in the folder of the package that the feed names.
const download = 'https://github.com/sql-bi/Bravo/releases/download/v99.0.0/Bravo.99.0.0.x64.msi';
const downloadOfThisCopy = 'https://github.com/sql-bi/Bravo/releases/download/v99.0.0/Bravo.99.0.0.x64-frameworkdependent-portable.zip';

test.describe('a newer release in the update feed', () => {

    test.use({ updateFeed: feedOf('99.0.0.1', download) });

    test('is told at the start, with the file of this copy', async ({ bravo }) => {
        const notification = bravo.page.locator('#notification-sidebar .item', { hasText: 'A new version of Bravo is available: 99.0.0' });

        await expect(notification.locator('.actions .link').first()).toHaveAttribute('href', downloadOfThisCopy);
    });

    test('is told in the options, and the browser downloads its file', async ({ bravo }) => {
        await openOptions(bravo, 'about');
        const status = bravo.page.locator('#dialog-options .update-status');
        await expect(status.locator('.pending-update')).toHaveText('New version available: 99.0.0');

        await bravo.answerBrowserRequests();
        await status.locator('.button').click();

        await expect.poll(() => bravo.openedInBrowser).toEqual([downloadOfThisCopy]);
    });
});

test.describe('no update feed', () => {

    test.use({ updateFeed: null });

    test('the options say that the check failed', async ({ bravo }) => {
        await openOptions(bravo, 'about');

        const notice = bravo.page.locator('#dialog-options .update-status .notice');
        await expect(notice).toHaveText('Unable to check for updates - see the diagnostics pane for details.');
    });

    // The page tells the code of the error alone: its cause is in the record that the host wrote under its trace
    test('the diagnostics pane shows the error that the host recorded, with its trace', async ({ bravo }) => {
        const { page } = bravo;
        await openOptions(bravo, 'about');
        await expect(page.locator('#dialog-options .update-status .notice')).toBeVisible();

        await showSection(bravo, 'telemetry');
        await chooseOption(bravo, 'diagnosticlevel', 'Basic');
        await closeOptions(bravo);

        const record = page.locator('#diagnostics .log .item.errorMessage', { hasText: '[HOST] BravoExceptionHandler.LogForeseenError' }).last();
        await record.locator('.json-formatter-toggler-link').click();
        await expect(record).toContainText('UpdateCheckFailed');
        await expect(record).toContainText(/[0-9a-f]{32}/);
    });
});
