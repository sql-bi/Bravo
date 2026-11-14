import { expect, test } from './bravo';

test('a feature that has no route yet says so, and asks nothing of the host', async ({ bravo }) => {
    const { page } = bravo;
    bravo.clear();

    // Open a VPAX file, from the welcome page: the model is read by a call that has no route
    await page.locator('.quick-open-vpax').click();
    await page.locator('#dialog-connect input.file-browser').setInputFiles({
        name: 'Model.vpax',
        mimeType: 'application/octet-stream',
        buffer: Buffer.alloc(0)
    });

    await expect(page.locator('.error-scene .message')).toHaveText('This feature is not available yet.');

    // The page reads the events of the host all the time, on its own
    expect(bravo.hostRequests().filter(request => request !== 'GET /events')).toEqual([]);
});
