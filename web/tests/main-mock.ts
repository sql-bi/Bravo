/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { vi } from 'vitest';

/**
 * What stands for main.ts in a test. main.ts starts the whole page when it is loaded, and the modules read
 * from it the parts of the page that they talk to: a test gives the module under test parts that do nothing,
 * and its own in place of the ones that it looks at.
 *
 *     vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());
 */
export function createMainMock(parts: Record<string, unknown> = {}) {
    return {
        application: { version: "1.2.3", isDevelopmentBuild: true, culture: { ietfLanguageTag: "en-US", twoLetterISOLanguageName: "en" } },
        optionsController: { options: { customOptions: { locale: "en" } }, on: vi.fn(), getOption: vi.fn(), update: vi.fn() },
        app: { currentVersion: { toString: () => "1.2.3" } },
        debug: { enabled: false, catchApiCall: (): null => null, catchTelemetryTracking: () => false },
        logger: { log: vi.fn(), updateLog: vi.fn(), logError: vi.fn() },
        telemetry: { enabled: false, track: vi.fn(), trackError: vi.fn() },
        auth: {},
        host: {},
        themeController: {},
        pbiDesktop: {},
        notificationCenter: {},
        hostEvents: {},
        ...parts
    };
}
