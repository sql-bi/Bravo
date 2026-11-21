/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { Host } from '../../controllers/host';
import { Options } from '../../controllers/options';
import { logger, notificationCenter } from '../../main';
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

        it("that the host cannot save are not saved, and the user is told with the text of the error", async () => {
            const problem = { status: 409, code: "SettingsSaveFailed", detail: "The settings could not be saved." };
            fetchMock.mockResolvedValue(answer(409, problem, "application/problem+json"));
            vi.mocked(notificationCenter.add).mockClear();

            const saved = await host.updateOptions(<Options><unknown>optionsOfTheHost);

            expect(saved).toBe(false);
            expect(notificationCenter.add).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({ message: en.strings[strings.errorUserSettingsSaveError], temporary: true }));
        });

        it("that the host refuses tell the user nothing: the options dialog shows the value that it refused", async () => {
            const problem = { status: 400, code: "ValidationFailed", errors: { "proxy.address": ["The address is not the address of a server."] } };
            fetchMock.mockResolvedValue(answer(400, problem, "application/problem+json"));
            vi.mocked(notificationCenter.add).mockClear();

            await host.updateOptions(<Options><unknown>optionsOfTheHost);

            expect(notificationCenter.add).not.toHaveBeenCalled();
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

    describe("the latest release", () => {

        const release = {
            version: "1.1.0",
            isNewer: true,
            downloadAddress: "https://github.com/sql-bi/Bravo/releases/download/v1.1.0/Bravo.1.1.0.x64-portable.zip",
            releaseNotesAddress: "https://github.com/sql-bi/Bravo/releases/tag/v1.1.0"
        };

        it("is read as the host wrote it, with the names of the host", async () => {
            fetchMock.mockResolvedValue(answer(200, release));

            const latest = await host.getLatestRelease();

            const [url, init] = fetchMock.mock.calls[0];
            expect([url, init?.method]).toEqual([`${address}/releases/latest`, "GET"]);
            expect(latest).toEqual(release);
        });

        it("that the host cannot read fails with the text of the update check", async () => {
            const problem = { status: 409, code: "UpdateCheckFailed", detail: "The update feed cannot be read." };
            fetchMock.mockResolvedValue(answer(409, problem, "application/problem+json"));

            const error = await failureOf(host.getLatestRelease());

            expect(error.type).toBe(AppErrorType.Managed);
            expect(error.message).toBe(en.strings[strings.errorCheckForUpdates]);
        });
    });

    describe("a page opened in the browser", () => {

        const page = "https://docs.sqlbi.com/bravo/";

        it("is asked of the host with its address", async () => {
            fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

            await host.navigateTo(page);

            const [url, init] = fetchMock.mock.calls[0];
            expect([url, init?.method]).toEqual([`${address}/browser/open`, "POST"]);
            expect(JSON.parse(<string>init?.body)).toEqual({ address: page });
        });

        // Its callers do not wait for it: a failure that reached them would be an error that nobody handles
        it("that the host refuses is logged, and the call does not fail", async () => {
            const problem = { status: 400, code: "ValidationFailed", errors: { address: ["The address is not the one of a page on a site that the application trusts."] } };
            fetchMock.mockResolvedValue(answer(400, problem, "application/problem+json"));
            vi.mocked(logger.logError).mockClear();

            await host.navigateTo("https://attacker.test/");

            expect(logger.logError).toHaveBeenCalledOnce();
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

    describe("the credentials of the proxy server", () => {

        it("are asked for by the host, which tells whether the user entered them", async () => {
            fetchMock.mockResolvedValue(answer(200, { entered: true }));

            const entered = await host.promptProxyCredentials();

            const [url, init] = fetchMock.mock.calls[0];
            expect([url, init?.method]).toEqual([`${address}/network/proxy-credentials/prompt`, "POST"]);
            expect(entered).toBe(true);
        });

        it("are forgotten by the host", async () => {
            fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

            await host.deleteProxyCredentials();

            const [url, init] = fetchMock.mock.calls[0];
            expect([url, init?.method]).toEqual([`${address}/network/proxy-credentials`, "DELETE"]);
        });
    });
});
