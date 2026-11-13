/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { Host } from '../../controllers/host';
import { Options } from '../../controllers/options';
import { AppError, AppErrorType } from '../../model/exceptions';
import en from '../../model/i18n/en';
import { strings } from '../../model/strings';

const address = "http://127.0.0.1:50000";

// The options as the host writes them: every enum is the name of its value
const optionsOfTheHost = {
    theme: "Dark",
    telemetryEnabled: true,
    customTemplatesEnabled: true,
    updateCheckEnabled: false,
    useSystemBrowserForAuthentication: false,
    diagnosticLevel: "Verbose",
    proxy: { type: "Custom", useDefaultCredentials: true, address: "proxy.example:8080", bypassOnLocal: true },
    customOptions: { sidebarCollapsed: true, formatting: { daxFormatter: { lineStyle: 1 } } }
};

function answer(status: number, body: unknown, contentType = "application/json") {
    return new Response(JSON.stringify(body), { status: status, headers: { "Content-Type": contentType } });
}

function failureOf(call: Promise<unknown>): Promise<AppError> {
    return call.then(() => { throw new Error("The call did not fail."); }, error => error);
}

describe("Host", () => {

    const fetchMock = vi.fn<typeof fetch>();
    let host: Host;

    beforeEach(() => {
        fetchMock.mockReset();
        vi.stubGlobal("fetch", fetchMock);
        host = new Host(address, "token-of-the-test");
    });

    describe("the options", () => {

        it("are read as the host wrote them, with the names of the host", async () => {
            fetchMock.mockResolvedValue(answer(200, optionsOfTheHost));

            const options = await host.getOptions();

            expect(options).toEqual(optionsOfTheHost);
        });

        it("are sent as the user interface has them, with the same names", async () => {
            fetchMock.mockResolvedValue(answer(200, optionsOfTheHost));

            const saved = await host.updateOptions(<Options><unknown>optionsOfTheHost);

            const [url, init] = fetchMock.mock.calls[0];
            expect(saved).toBe(true);
            expect([url, init?.method]).toEqual([`${address}/options`, "PUT"]);
            expect(JSON.parse(<string>init?.body)).toEqual(optionsOfTheHost);
        });

        it("leave as it is a number that only the user interface reads", async () => {
            fetchMock.mockResolvedValue(answer(200, optionsOfTheHost));

            const options = await host.getOptions();

            expect(options.customOptions.formatting.daxFormatter.lineStyle).toBe(1);
        });

        it("are not saved when the host refuses a value", async () => {
            const problem = { status: 400, code: "ValidationFailed", errors: { "proxy.address": ["The address is not the address of a server."] } };
            fetchMock.mockResolvedValue(answer(400, problem, "application/problem+json"));

            const saved = await host.updateOptions(<Options><unknown>optionsOfTheHost);

            expect(saved).toBe(false);
        });
    });

    describe("a call of the generated client", () => {

        it("fails with the error of the code of the answer", async () => {
            fetchMock.mockResolvedValue(answer(500, { status: 500, code: "Unexpected", traceId: "00-abc-def-00" }, "application/problem+json"));

            const error = await failureOf(host.getOptions());

            expect(error).toBeInstanceOf(AppError);
            expect([error.code, error.traceId]).toEqual([500, "00-abc-def-00"]);
            expect(error.message).toBe(en.strings[strings.errorUnhandled]);
        });

        it("is aborted by the name of its operation", async () => {
            fetchMock.mockImplementation((_, init) => new Promise((_, reject) => {
                init?.signal?.addEventListener("abort", () => reject(new DOMException("The request was aborted.", "AbortError")));
            }));
            const call = failureOf(host.getOptions());

            host.apiAbortByAction("GetOptions");

            const error = await call;
            expect(error.type).toBe(AppErrorType.Abort);
            expect(error.requestAborted).toBe(true);
        });

        it("is forgotten once it is over, so that a later abort finds nothing", async () => {
            fetchMock.mockResolvedValue(answer(200, optionsOfTheHost));

            await host.getOptions();

            expect(Object.keys(host.requests)).toEqual([]);
        });
    });

    describe("a call that has no route in the host yet", () => {

        it("sends nothing", async () => {
            await failureOf(host.listDatasets());

            expect(fetchMock).not.toHaveBeenCalled();
        });

        it("fails at once with an error that says that the feature is not available yet", async () => {
            const error = await failureOf(host.listDatasets());

            expect(error).toBeInstanceOf(AppError);
            expect(error.type).toBe(AppErrorType.Generic);
            expect(error.message).toBe("This feature is not available yet.");
        });

        // A view may use what it built only after the task in which it asked: so it could with a request
        it("fails in a later task, as the answer to a request arrives", async () => {
            vi.useFakeTimers();
            try {
                let failed = false;
                host.listReports().catch(() => { failed = true; });

                await Promise.resolve();
                await Promise.resolve();
                expect(failed).toBe(false);

                await vi.advanceTimersByTimeAsync(0);
                expect(failed).toBe(true);
            } finally {
                vi.useRealTimers();
            }
        });

        it("is not taken for a request that was aborted", async () => {
            const error = await failureOf(host.getDateConfigurations());

            expect(error.requestAborted).toBe(false);
            expect(error.code).toBeUndefined();
        });
    });
});
