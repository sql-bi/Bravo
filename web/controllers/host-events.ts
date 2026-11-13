/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ActivationEvent, DiagnosticLevel, getEvents, HostEvent } from '../generated/bravo';
import { logger } from '../main';

interface EventCursor {
    address: string
    next: number
}

/**
 * Reads the events of the host, one request after the other: a request waits in the host until an event
 * arrives, or is answered with none after a while, and the next one asks for what follows.
 */
export class HostEvents {

    static StorageKey = "bravo-host-events";
    static FirstRetrySeconds = 1;
    static MaxRetrySeconds = 30;

    address: string;
    next: number;
    controller = new AbortController();
    started = false;

    /**
     * @param address The address of the host. The numbers of the events begin again with every host, so the
     * number to ask next is kept with the address it belongs to.
     */
    constructor(address: string) {
        this.address = address;
        this.next = this.loadNext();

        // A page that goes away leaves no request waiting in the host
        window.addEventListener("pagehide", () => this.stop());
    }

    start() {
        if (this.started) return;
        this.started = true;

        this.read();
    }

    stop() {
        this.controller.abort();
    }

    async read() {

        let retrySeconds = HostEvents.FirstRetrySeconds;
        const signal = this.controller.signal;

        while (!signal.aborted) {
            try {
                // Without a number the host answers with every event it still keeps: a page that starts
                // reads what happened before it was ready
                const batch = await getEvents(this.next ? { after: this.next } : {}, { signal: signal });

                batch.events.forEach(event => this.dispatch(event));

                // Kept only now: an event is never skipped, and a page loaded again does not read it again
                this.next = batch.next;
                this.saveNext();
                retrySeconds = HostEvents.FirstRetrySeconds;

            } catch (error) {
                if (signal.aborted) return;

                // The host may be busy or gone: the page asks again later, each time after a longer wait
                await this.wait(retrySeconds, signal);
                retrySeconds = Math.min(retrySeconds * 2, HostEvents.MaxRetrySeconds);
            }
        }
    }

    dispatch(event: HostEvent) {
        try {
            if (event.kind == "Activation" && event.activation)
                this.activated(event.activation);
        } catch (error) {
            // An event that the page cannot handle does not stop the reading of the next ones
            console.error(error);
        }
    }

    /**
     * A start of the application, the first one included. One without a server asks for nothing; one with a
     * server asks for a report, which the page cannot open until the host can find it.
     */
    activated(activation: ActivationEvent) {

        if (activation.errors.length) {
            logger.log("Activation: the command line has errors", { content: activation }, DiagnosticLevel.Basic, 0, "errorMessage", true);
        } else if (activation.serverName) {
            logger.log("Activation", { content: activation });
        }
    }

    wait(seconds: number, signal: AbortSignal) {
        return new Promise<void>(resolve => {
            const timeout = window.setTimeout(resolve, seconds * 1000);
            signal.addEventListener("abort", () => {
                window.clearTimeout(timeout);
                resolve();
            }, { once: true });
        });
    }

    // The session storage lasts as long as the page is open, across its reloads: what a page needs to go on
    // from where it was. A page of another host starts from the beginning.
    loadNext(): number {
        try {
            const cursor = <EventCursor>JSON.parse(sessionStorage.getItem(HostEvents.StorageKey));
            if (cursor && cursor.address == this.address && Number.isInteger(cursor.next) && cursor.next > 0)
                return cursor.next;
        } catch (ignore) {}

        return 0;
    }

    saveNext() {
        try {
            sessionStorage.setItem(HostEvents.StorageKey, JSON.stringify(<EventCursor>{ address: this.address, next: this.next }));
        } catch (ignore) {}
    }
}
