/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { Logger } from '../../controllers/logger';
import { DiagnosticLevel } from '../../generated/bravo';
import { optionsController } from '../../main';

describe("Logger", () => {

    // The levels are names: which one shows more is said here, and not by the order of two numbers
    it.each([
        ["Basic", "Basic", true],
        ["Basic", "Verbose", true],
        ["Verbose", "Verbose", true],
        ["Verbose", "Basic", false],
        ["Basic", "None", false],
        ["Verbose", "None", false],
    ])("a message of the level %s, for a user with the level %s, is logged: %s", (level, levelOfTheUser, logged) => {
        optionsController.options.diagnosticLevel = <DiagnosticLevel>levelOfTheUser;

        expect(Logger.LevelMatch(<DiagnosticLevel>level)).toBe(logged);
    });
});
