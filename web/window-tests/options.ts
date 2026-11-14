import type { Response } from '@playwright/test';
import type { Bravo } from './bravo';

// The options dialog, as a user reaches it from the sidebar.

/** Opens the options dialog on one of its sections. */
export async function openOptions(bravo: Bravo, section: 'general' | 'telemetry') {
    await bravo.page.locator('#ctrl-options').click();
    await bravo.page.locator(`#dialog-options #item-${section}`).click();
}

/** Chooses the value of an option of the dialog; returns the answer of the host to the request that saves it. */
export async function chooseOption(bravo: Bravo, option: string, value: string): Promise<Response> {
    const saved = bravo.page.waitForResponse(response =>
        response.url() === `${bravo.address}/options` && response.request().method() === 'PUT');
    await bravo.page.locator(`#dialog-options #${option} select.listener`).selectOption(value);
    return await saved;
}

export async function closeOptions(bravo: Bravo) {
    await bravo.page.locator('#dialog-options footer .button[data-action="ok"]').click();
    await bravo.page.locator('#dialog-options').waitFor({ state: 'detached' });
}
