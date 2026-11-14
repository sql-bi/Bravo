import { expect, test } from './bravo';
import { chooseOption, closeOptions, openOptions } from './options';

test('a second start with a server is handed over, and the page logs it as an activation', async ({ bravo }) => {
    const { page } = bravo;

    // The page logs an activation with a server in the diagnostics pane
    await openOptions(bravo, 'telemetry');
    await chooseOption(bravo, 'diagnosticlevel', 'Basic');
    await closeOptions(bravo);
    const log = page.locator('#diagnostics .log .item .name');
    await expect(page.locator('#diagnostics')).toBeVisible();
    await expect(log.getByText('Activation', { exact: true })).toHaveCount(0);

    // As the registration of an external tool in Power BI Desktop starts the application
    expect(await bravo.startAgain(['--server=localhost:12345', '--database=db'])).toBe(0);

    await expect(log.getByText('Activation', { exact: true })).toHaveCount(1);
});
