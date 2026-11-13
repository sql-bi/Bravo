/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// The page as a whole is not started: the module gets a logger of the test, and a route of the test
vi.mock("../../main", () => ({ logger: { log: vi.fn() } }));
vi.mock("../../generated/bravo", async importOriginal => ({
    ...(await importOriginal<typeof import('../../generated/bravo')>()),
    getEvents: vi.fn()
}));

import { HostEvents } from '../../controllers/host-events';
import { EventBatch, getEvents, HostEvent } from '../../generated/bravo';
import { logger } from '../../main';

const address = "http://127.0.0.1:50000";

interface PendingRequest {
    after: number | undefined
    signal: AbortSignal
    answer: (batch: EventBatch) => void
    fail: (error: Error) => void
}

describe("HostEvents", () => {

    let requests: PendingRequest[];
    let hostEvents: HostEvents;

    // Each request of the page waits, as in the host, until the test answers it
    beforeEach(() => {
        vi.useFakeTimers();
        sessionStorage.clear();
        vi.mocked(logger.log).mockReset();

        requests = [];
        vi.mocked(getEvents).mockReset().mockImplementation((params, options) =>
            new Promise<EventBatch>((resolve, reject) => {
                requests.push({ after: params?.after, signal: <AbortSignal>options?.signal, answer: resolve, fail: reject });
            }));
    });

    afterEach(() => {
        hostEvents?.stop();
        vi.useRealTimers();
    });

    function start(hostAddress = address) {
        hostEvents = new HostEvents(hostAddress);
        hostEvents.start();
        return hostEvents;
    }

    // Lets the page go on after an answer, and moves the clock of its waits
    async function elapse(milliseconds = 0) {
        await vi.advanceTimersByTimeAsync(milliseconds);
    }

    function keptCursor() {
        return JSON.parse(sessionStorage.getItem(HostEvents.StorageKey) ?? "null");
    }

    function activation(number: number, serverName?: string, errors: string[] = []): HostEvent {
        return { number: number, kind: "Activation", activation: { serverName: serverName, errors: errors } };
    }

    describe("the number to ask next", () => {

        it("is absent in the first request, so that the host answers with every event it keeps", async () => {
            start();
            await elapse();

            expect(requests).toHaveLength(1);
            expect(requests[0].after).toBeUndefined();
        });

        it("is the one of the last answer", async () => {
            start();
            requests[0].answer({ events: [activation(1), activation(2)], next: 2 });
            await elapse();

            expect(requests).toHaveLength(2);
            expect(requests[1].after).toBe(2);
        });

        it("is kept with the address of the host once the events of an answer were handled", async () => {
            start();
            requests[0].answer({ events: [activation(1)], next: 1 });
            await elapse();

            expect(keptCursor()).toEqual({ address: address, next: 1 });
        });

        it("is not kept while no request was answered", async () => {
            start();
            await elapse();

            expect(sessionStorage.getItem(HostEvents.StorageKey)).toBeNull();
        });

        it("is found again by a page of the same host that is loaded again", async () => {
            sessionStorage.setItem(HostEvents.StorageKey, JSON.stringify({ address: address, next: 7 }));

            start();
            await elapse();

            expect(requests[0].after).toBe(7);
        });

        it("starts from the beginning for a page of another host", async () => {
            sessionStorage.setItem(HostEvents.StorageKey, JSON.stringify({ address: address, next: 7 }));

            start("http://127.0.0.1:50001");
            await elapse();

            expect(requests[0].after).toBeUndefined();
        });

        it.each([
            ["text that is not JSON", "{ not json"],
            ["a number below one", JSON.stringify({ address: address, next: 0 })],
            ["a number with decimals", JSON.stringify({ address: address, next: 1.5 })],
            ["no number", JSON.stringify({ address: address })],
            ["null", "null"],
        ])("starts from the beginning when what was kept is %s", async (_, kept) => {
            sessionStorage.setItem(HostEvents.StorageKey, kept);

            start();
            await elapse();

            expect(requests[0].after).toBeUndefined();
        });
    });

    describe("after an error", () => {

        it("waits one second, then twice as long each time, up to thirty seconds", async () => {
            const waits: number[] = [];
            start();

            for (let failure = 1; failure <= 7; failure++) {
                requests[failure - 1].fail(new Error("The host is busy."));
                await elapse();

                // No request until the wait is over, and one as soon as it is
                let seconds = 0;
                while (requests.length == failure && seconds < 60) {
                    await elapse(1000);
                    seconds++;
                }
                waits.push(seconds);
            }

            expect(waits).toEqual([1, 2, 4, 8, 16, 30, 30]);
        });

        it("asks again for the number it had: no event is skipped", async () => {
            start();
            requests[0].answer({ events: [], next: 5 });
            await elapse();

            requests[1].fail(new Error("The host is busy."));
            await elapse(1000);

            expect(requests[2].after).toBe(5);
        });

        it("waits one second again once a request was answered", async () => {
            start();
            requests[0].fail(new Error("The host is busy."));
            await elapse(1000);
            requests[1].fail(new Error("The host is busy."));
            await elapse(2000);
            requests[2].answer({ events: [], next: 1 });
            await elapse();

            requests[3].fail(new Error("The host is busy."));
            await elapse(999);
            expect(requests).toHaveLength(4);
            await elapse(1);

            expect(requests).toHaveLength(5);
        });
    });

    describe("a page that stops", () => {

        it("aborts the request that waits in the host, and asks for nothing more", async () => {
            start();
            await elapse();

            hostEvents.stop();
            requests[0].fail(new DOMException("The request was aborted.", "AbortError"));
            await elapse(60000);

            expect(requests[0].signal.aborted).toBe(true);
            expect(requests).toHaveLength(1);
        });

        it("ends the wait after an error, and asks for nothing more", async () => {
            start();
            requests[0].fail(new Error("The host is busy."));
            await elapse(500);

            hostEvents.stop();
            await elapse(60000);

            expect(requests).toHaveLength(1);
        });

        it("stops when the page goes away", async () => {
            start();
            await elapse();

            window.dispatchEvent(new Event("pagehide"));

            expect(requests[0].signal.aborted).toBe(true);
        });

        it("starts only once", async () => {
            start();
            hostEvents.start();
            await elapse();

            expect(requests).toHaveLength(1);
        });
    });

    describe("an activation", () => {

        it("without a server and without errors leaves nothing in the diagnostic log", async () => {
            start();
            requests[0].answer({ events: [activation(1)], next: 1 });
            await elapse();

            expect(logger.log).not.toHaveBeenCalled();
        });

        it("with a server is logged with what it asked for", async () => {
            const event = activation(1, "localhost:12345");
            start();
            requests[0].answer({ events: [event], next: 1 });
            await elapse();

            expect(logger.log).toHaveBeenCalledTimes(1);
            expect(vi.mocked(logger.log).mock.calls[0].slice(0, 2)).toEqual(["Activation", { content: event.activation }]);
        });

        it("with errors of the command line is logged as an error, whatever the diagnostic level", async () => {
            const event = activation(1, "localhost:12345", ["Unrecognized argument '--port'."]);
            start();
            requests[0].answer({ events: [event], next: 1 });
            await elapse();

            const [name, content, , , className, forceLogging] = vi.mocked(logger.log).mock.calls[0];
            expect(name).toBe("Activation: the command line has errors");
            expect(content).toEqual({ content: event.activation });
            expect([className, forceLogging]).toEqual(["errorMessage", true]);
        });

        it("that the page cannot handle does not stop the reading of the next events", async () => {
            vi.spyOn(console, "error").mockImplementation(() => {});
            vi.mocked(logger.log).mockImplementationOnce(() => { throw new Error("The log is broken."); });
            start();
            requests[0].answer({ events: [activation(1, "first"), activation(2, "second")], next: 2 });
            await elapse();

            expect(logger.log).toHaveBeenCalledTimes(2);
            expect(requests[1].after).toBe(2);
        });
    });
});
