/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// The page as a whole is not started: the module gets a host of the test, which answers with the records
// that the test gives it
vi.mock("../../main", async () => (await import("../main-mock")).createMainMock({ host: { getLog: vi.fn() } }));

import { Logger, LogMessage } from '../../controllers/logger';
import { DiagnosticLevel, LogBatch, LogRecord } from '../../generated/bravo';
import { host, optionsController } from '../../main';

const traceId = "4bf92f3577b34da6a3ce929d0e0e4736";

function record(number: number, parts: Partial<LogRecord> = {}): LogRecord {
    return {
        number: number,
        time: "2026-11-22T09:30:15.123+00:00",
        level: "Information",
        category: "Bravo.Core.Updates.UpdateCheck",
        eventId: 1,
        message: `Record ${ number }.`,
        ...parts
    };
}

function records(first: number, last: number) {
    return Array.from({ length: last - first + 1 }, (_, i) => record(first + i));
}

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

    describe("the records of the host", () => {

        let logger: Logger;

        beforeEach(() => {
            vi.useFakeTimers();
            optionsController.options.diagnosticLevel = DiagnosticLevel.Basic;
            logger = new Logger(false);
            logger.enabled = true;
        });

        afterEach(() => {
            logger.enabled = false;
            vi.useRealTimers();
        });

        function hostAnswers(batch: LogBatch) {
            vi.mocked(host.getLog).mockReset().mockResolvedValue(batch);
        }

        function shown(): LogMessage[] {
            return Object.values(logger.logs);
        }

        it("show a warning as a problem, by its category and its event, with its trace", async () => {
            hostAnswers({ records: [record(1, { level: "Warning", eventName: "FeedUnreachable", traceId: traceId })], next: 1 });

            await logger.check();

            const [message] = shown();
            expect([message.name, message.className]).toEqual(["[HOST] UpdateCheck.FeedUnreachable", "errorMessage"]);
            expect(message.objs).toEqual([{ Message: "Record 1.", TraceId: traceId }]);
        });

        it("show information only to a user with the level Verbose", async () => {
            hostAnswers({ records: [record(1)], next: 1 });

            await logger.check();

            expect(shown()).toEqual([]);
        });

        it("are asked for after the last one read", async () => {
            hostAnswers({ records: [record(1), record(2)], next: 2 });
            await logger.check();

            await logger.check();

            expect(vi.mocked(host.getLog).mock.calls).toEqual([[0], [2]]);
        });

        it("that the host no longer keeps are counted", async () => {
            hostAnswers({ records: [record(41, { level: "Error" })], next: 41 });

            await logger.check();

            expect(shown().map(message => message.name)).toEqual([
                "[HOST] 40 earlier messages are not shown",
                "[HOST] UpdateCheck"
            ]);
        });

        it("beyond what the pane keeps are counted, and the newest ones are shown", async () => {
            optionsController.options.diagnosticLevel = DiagnosticLevel.Verbose;
            hostAnswers({ records: records(1, Logger.MaxMessages + 10), next: Logger.MaxMessages + 10 });

            await logger.check();

            const messages = shown();
            expect(messages[0].name).toBe("[HOST] 10 earlier messages are not shown");
            expect(messages[1].objs).toEqual([{ Message: "Record 11." }]);
            expect(messages[messages.length - 1].objs).toEqual([{ Message: `Record ${ Logger.MaxMessages + 10 }.` }]);
        });

        it("that the level does not show are not counted", async () => {
            hostAnswers({ records: records(1, Logger.MaxMessages + 10), next: Logger.MaxMessages + 10 });

            await logger.check();

            expect(shown()).toEqual([]);
        });
    });

    // Shown at every level, as an error that a call returns
    describe("the errors of the page", () => {

        let logger: Logger;

        beforeEach(() => {
            optionsController.options.diagnosticLevel = DiagnosticLevel.None;
            logger = new Logger(false);
        });

        it("that nothing catches are shown with their stack", () => {
            const error = new Error("It failed.");

            window.dispatchEvent(new ErrorEvent("error", { error: error, message: error.message }));

            const [message] = Object.values(logger.logs);
            expect(message.name).toContain("It failed.");
            expect(message.className).toBe("errorMessage");
            expect(message.objs).toEqual([{ Message: "It failed.", Stack: error.stack }]);
        });

        it("of a promise that nothing awaits are shown with their reason", () => {
            window.dispatchEvent(Object.assign(new Event("unhandledrejection"), { reason: "Rejected." }));

            const [message] = Object.values(logger.logs);
            expect(message.objs).toEqual([{ Message: "Rejected." }]);
        });
    });
});
