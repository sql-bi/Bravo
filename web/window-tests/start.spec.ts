import { expect, pageSize, test } from './bravo';

test('the page starts with a clean console, reads the host and saves no option', async ({ bravo }) => {
    const { page } = bravo;

    // The first start of the page is over before a test can attach to it: the test sees the next one, the
    // same start of the same page against the same host
    bravo.clear();
    await page.reload();
    await expect(page.locator('#ctrl-theme')).toBeVisible();

    // The one wait for a time: to say that nothing else happens, it lasts more than the 10 seconds after which
    // the page asks for the list of the local reports, a call that has no route
    await page.waitForTimeout(12_000);

    expect(bravo.pageErrors).toEqual([]);
    expect(bravo.console.filter(entry => entry.type === 'error' || entry.type === 'warning')).toEqual([]);

    expect([...new Set(bravo.hostRequests())].sort()).toEqual(['GET /application', 'GET /events', 'GET /options']);
    expect(bravo.requests.filter(request =>
        !request.url.startsWith('https://bravo.invalid/') && !request.url.startsWith(`${bravo.address}/`))).toEqual([]);

    // The page has the size of the tests, and a screenshot has it too
    expect(await page.evaluate(() => [window.innerWidth, window.innerHeight])).toEqual([pageSize.width, pageSize.height]);
    const screenshot = await page.screenshot();
    expect([screenshot.readUInt32BE(16), screenshot.readUInt32BE(20)]).toEqual([pageSize.width, pageSize.height]);
});
