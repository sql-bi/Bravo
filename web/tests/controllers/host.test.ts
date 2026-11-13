/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { Host } from '../../controllers/host';
import { AppError, AppErrorType } from '../../model/exceptions';

const address = "http://127.0.0.1:50000";

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

        it("is not taken for a request that was aborted", async () => {
            const error = await failureOf(host.getDateConfigurations());

            expect(error.requestAborted).toBe(false);
            expect(error.code).toBeUndefined();
        });
    });
});
