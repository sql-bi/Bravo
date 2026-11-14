import { expect, pageSize, test } from './bravo';

test('the page starts in the window, at the size of the tests', async ({ bravo }) => {
    await expect(bravo.page.locator('#ctrl-theme')).toBeVisible();
    expect(await bravo.page.evaluate(() => [window.innerWidth, window.innerHeight])).toEqual([pageSize.width, pageSize.height]);
});
