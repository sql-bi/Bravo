/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { beforeEach, describe, expect, it, vi } from 'vitest';
import { connectHost, hostFetch, HostProblem } from '../../controllers/host-fetch';

const address = "http://127.0.0.1:50000";
const token = "token-of-the-test";

function answer(status: number, body?: string, contentType = "application/json") {
    return new Response(body ?? null, { status: status, headers: (body === undefined ? {} : { "Content-Type": contentType }) });
}

describe("hostFetch", () => {

    const fetchMock = vi.fn<typeof fetch>();

    // The first request that the function under test sent
    function sentRequest() {
        const [url, init] = fetchMock.mock.calls[0];
        return { url: url, init: init ?? {} };
    }

    function failureOf(call: Promise<unknown>): Promise<HostProblem> {
        return call.then(() => { throw new Error("The call did not fail."); }, error => error);
    }

    beforeEach(() => {
        fetchMock.mockReset();
        vi.stubGlobal("fetch", fetchMock);
        connectHost({ address: address, token: token });
    });

    it("calls the address of the host with the token of the host", async () => {
        fetchMock.mockResolvedValue(answer(200, "{}"));

        await hostFetch("/options", { method: "GET" });

        const { url, init } = sentRequest();
        expect(url).toBe(`${address}/options`);
        expect(new Headers(init.headers).get("Authorization")).toBe(`Bearer ${token}`);
    });

    it("calls the host that was connected last", async () => {
        fetchMock.mockResolvedValue(answer(200, "{}"));
        connectHost({ address: "http://127.0.0.1:50001", token: "another-token" });

        await hostFetch("/options", {});

        const { url, init } = sentRequest();
        expect(url).toBe("http://127.0.0.1:50001/options");
        expect(new Headers(init.headers).get("Authorization")).toBe("Bearer another-token");
    });

    it("passes on what the caller set", async () => {
        fetchMock.mockResolvedValue(answer(200, "{}"));
        const controller = new AbortController();

        await hostFetch("/options", { method: "PUT", body: `{"theme":"Dark"}`, headers: { "Content-Type": "application/json" }, signal: controller.signal });

        const { init } = sentRequest();
        expect(init.method).toBe("PUT");
        expect(init.body).toBe(`{"theme":"Dark"}`);
        expect(init.signal).toBe(controller.signal);
        expect(new Headers(init.headers).get("Content-Type")).toBe("application/json");
    });

    it("returns the body of an answer as the value that its JSON describes", async () => {
        fetchMock.mockResolvedValue(answer(200, `{"version":"1.2.3","isDevelopmentBuild":true}`));

        const body = await hostFetch<{ version: string, isDevelopmentBuild: boolean }>("/application", {});

        expect(body).toEqual({ version: "1.2.3", isDevelopmentBuild: true });
    });

    it.each([
        ["no body", () => answer(204)],
        ["an empty body", () => answer(200, "")],
    ])("returns nothing for an answer with %s", async (_, createAnswer) => {
        fetchMock.mockResolvedValue(createAnswer());

        const body = await hostFetch("/options", {});

        expect(body).toBeUndefined();
    });

    it("throws the status and the problem details of an error answer", async () => {
        const problem = { type: "https://tools.ietf.org/html/rfc9110#section-15.5.10", title: "Conflict", status: 409, detail: "The model was changed by another program.", code: "ModelChanged", traceId: "00-abc-def-00" };
        fetchMock.mockResolvedValue(answer(409, JSON.stringify(problem), "application/problem+json"));

        const error = await failureOf(hostFetch("/options", {}));

        expect(error).toBeInstanceOf(HostProblem);
        expect(error.status).toBe(409);
        expect(error.problem).toEqual(problem);
        expect(error.message).toBe("Conflict");
    });

    it.each([
        ["is not JSON", () => answer(502, "<html><body>Bad Gateway</body></html>", "text/html")],
        ["is empty", () => answer(503)],
    ])("throws the status alone for an error answer whose body %s", async (_, createAnswer) => {
        const expected = createAnswer();
        fetchMock.mockResolvedValue(createAnswer());

        const error = await failureOf(hostFetch("/options", {}));

        expect(error).toBeInstanceOf(HostProblem);
        expect(error.status).toBe(expected.status);
        expect(error.problem).toBeUndefined();
        expect(error.message).toBe(`HTTP ${expected.status}`);
    });

    it("lets through the error of a request that got no answer", async () => {
        const failure = new TypeError("Failed to fetch");
        fetchMock.mockRejectedValue(failure);

        const error = await failureOf(hostFetch("/options", {}));

        expect(error).toBe(failure);
    });
});
