/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { HostProblem } from '../../controllers/host-fetch';
import { ProblemCode, ProblemDetails } from '../../generated/bravo';
import { telemetry } from '../../main';
import { AppError, AppErrorType, AppProblem } from '../../model/exceptions';
import en from '../../model/i18n/en';
import { problemTexts } from '../../model/problem-texts';
import { strings } from '../../model/strings';

const english = (text: strings) => en.strings[text];

function problem(status: number, code: string, more: Partial<ProblemDetails> = {}) {
    return new HostProblem(status, <ProblemDetails>{ status: status, title: "A title of the standard", code: code, ...more });
}

describe("AppError", () => {

    beforeEach(() => {
        telemetry.enabled = false;
        vi.mocked(telemetry.trackError).mockReset();
    });

    describe("of an error answer of the host", () => {

        it.each(Object.values(ProblemCode))("has the text of the code %s, and not the title of the answer", code => {
            const error = AppError.InitFromHostProblem(problem(400, code));

            expect(error.message).toBe(english(problemTexts[code]));
        });

        it("has the status as its code, the detail and the identifier of the request", () => {
            const error = AppError.InitFromHostProblem(problem(500, "Unexpected", { detail: "What the host says.", traceId: "00-abc-def-00" }));

            expect(error.type).toBe(AppErrorType.Response);
            expect(error.code).toBe(500);
            expect([error.details, error.traceId]).toEqual(["What the host says.", "00-abc-def-00"]);
        });

        it("is not a missing sign-in when the token of the API was refused", () => {
            const error = AppError.InitFromHostProblem(problem(401, "Unauthorized"));

            expect(error.type).toBe(AppErrorType.Response);
            expect(error.message).toBe(english(strings.errorNotAuthorized));
        });

        // No use case reports an error yet: any code of the list stands for the first one
        it("is shown by its code when a use case foresaw it", () => {
            const error = AppError.InitFromHostProblem(problem(409, "NotFound", { detail: "The model was changed by another program." }));

            expect(error.type).toBe(AppErrorType.Managed);
            expect(error.code).toBe("NotFound");
            expect(error.details).toBe("The model was changed by another program.");
        });

        it("has a text also for a code that the page does not know", () => {
            const error = AppError.InitFromHostProblem(problem(409, "CodeOfAnotherVersion", { traceId: "00-abc-def-00" }));

            expect(error.type).toBe(AppErrorType.Response);
            expect(error.code).toBe(409);
            expect([error.message, error.traceId]).toEqual([english(strings.errorUnspecified), "00-abc-def-00"]);
        });

        it("has a text also when the answer was not a problem of the host", () => {
            const error = AppError.InitFromHostProblem(new HostProblem(502));

            expect(error.type).toBe(AppErrorType.Response);
            expect(error.code).toBe(502);
            expect(error.message).toBe(english(strings.errorUnspecified));
        });
    });

    describe("of a call that failed", () => {

        it("is the one of the error answer", () => {
            const error = AppError.InitFromCallError(problem(404, "NotFound"));

            expect(error.code).toBe(404);
            expect(error.message).toBe(english(strings.errorNotFound));
        });

        it("says that the request was aborted", () => {
            const error = AppError.InitFromCallError(new DOMException("The user aborted a request.", "AbortError"));

            expect(error.type).toBe(AppErrorType.Abort);
            expect(error.requestAborted).toBe(true);
            expect(error.message).toBe(english(strings.errorAborted));
        });

        it("keeps what the browser says when the host gave no answer", () => {
            const error = AppError.InitFromCallError(new TypeError("Failed to fetch"));

            expect(error.type).toBe(AppErrorType.Response);
            expect(error.requestAborted).toBe(false);
            expect(error.message).toBe("Failed to fetch.");
        });
    });

    describe("that the user interface raises itself", () => {

        it("has the text of its code", () => {
            const error = AppError.InitFromProblemCode(AppProblem.ConnectionUnsupported, null, "The details.");

            expect(error.type).toBe(AppErrorType.Managed);
            expect(error.code).toBe(AppProblem.ConnectionUnsupported);
            expect([error.message, error.details]).toEqual([english(strings.errorConnectionUnsupported), "The details."]);
        });

        it("has the message of its caller when there is one", () => {
            const error = AppError.InitFromProblemCode(AppProblem.TOMDatabaseUpdateErrorMeasure, "Two measures contain errors.");

            expect(error.message).toBe("Two measures contain errors.");
        });

        it("has no code when it says that a feature is not available yet", () => {
            const error = AppError.InitFromNotAvailable();

            expect(error.type).toBe(AppErrorType.Generic);
            expect(error.code).toBeUndefined();
            expect(error.message).toBe(english(strings.errorNotAvailable));
        });

        it.each([
            [401, AppErrorType.Auth, strings.errorNotAuthorized],
            [418, AppErrorType.Abort, strings.errorAborted],
            [408, AppErrorType.Abort, strings.errorTimeout],
        ])("has the type and the text of the status %i", (status, type, text) => {
            const error = AppError.InitFromResponseStatus(status);

            expect(error.type).toBe(type);
            expect(error.code).toBe(status);
            expect(error.message).toBe(english(text));
        });

        it("tells an aborted request from one that took too long", () => {
            const aborted = AppError.InitFromResponseStatus(418);
            const late = AppError.InitFromResponseStatus(408);

            expect([aborted.requestAborted, aborted.requestTimedout]).toEqual([true, false]);
            expect([late.requestAborted, late.requestTimedout]).toEqual([false, true]);
        });

        it("ends the message of an error with a full stop, and has a message without one", () => {
            expect(AppError.InitFromString("Path not found").message).toBe("Path not found.");
            expect(AppError.InitFromError(new Error("It broke.")).message).toBe("It broke.");
            expect(AppError.InitFromResponseStatus(500).message).toBe(english(strings.errorUnspecified));
        });

        it("is told to the telemetry, with an identifier, when it is a defect and the telemetry is on", () => {
            telemetry.enabled = true;

            const error = AppError.InitFromResponseStatus(500, "It broke.");

            expect(error.traceId).toBeTruthy();
            expect(telemetry.trackError).toHaveBeenCalledWith({ status: 500, title: "It broke.", traceId: error.traceId });
        });

        it.each([
            ["the telemetry is off", false, 500, true],
            ["the caller asks for no tracking", true, 500, false],
            ["the status is not a defect", true, 418, true],
        ])("is not told to the telemetry when %s", (_, enabled, status, track) => {
            telemetry.enabled = enabled;

            const error = AppError.InitFromResponseStatus(status, null, track);

            expect(error.traceId).toBeUndefined();
            expect(telemetry.trackError).not.toHaveBeenCalled();
        });
    });

    describe("as text", () => {

        it("names the status of an answer and the code of a foreseen error", () => {
            const answer = AppError.InitFromHostProblem(problem(500, "Unexpected"));
            const foreseen = AppError.InitFromHostProblem(problem(409, "NotFound"));

            expect(answer.toString(false, false, false)).toBe(`Error HTTP/500: ${english(strings.errorUnhandled)}`);
            expect(foreseen.toString(false, false, false)).toBe(`Error NotFound: ${english(strings.errorNotFound)}`);
        });

        it("has the detail, the identifier of the request and the version of the application", () => {
            const error = AppError.InitFromHostProblem(problem(500, "Unexpected", { detail: "What the host says.", traceId: "00-abc-def-00" }));

            expect(error.toString().split("\n").slice(1)).toEqual(["What the host says.", "Trace Id: 00-abc-def-00", "Version: 1.2.3"]);
        });

        it("has no code when the error has none", () => {
            expect(AppError.InitFromNotAvailable().toString(false, false, false)).toBe(`Error: ${english(strings.errorNotAvailable)}`);
        });
    });
});
