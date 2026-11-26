/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock("../../main", async () => (await import("../main-mock")).createMainMock());

import { Telemetry } from '../../controllers/telemetry';
import { TelemetryDetails } from '../../generated/bravo';
import { optionsController } from '../../main';

/** An item as the SDK puts it on the wire. */
interface Envelope {
    name: string
    tags: Record<string, string>
    data: { baseType: string, baseData: { name?: string, message?: string, properties?: Record<string, string> } }
}

/** What the SDK sends, with no request leaving the test: each body that it posts, and where. */
class RecordingXhr {
    static readonly posts: { url: string, envelopes: Envelope[] }[] = [];
    private url: string;

    open(_method: string, url: string) {
        this.url = url;
    }

    setRequestHeader() { }

    send(body: string) {
        RecordingXhr.posts.push({ url: this.url, envelopes: <Envelope[]>JSON.parse(body) });
    }
}

const details: TelemetryDetails = {
    connectionString: "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9",
    userId: "user-of-the-test",
    sessionId: "session-of-the-test",
    version: "1.2.3-test.4",
    properties: { ProductName: "Bravo", InstallScope: "PerUser" }
};

describe("Telemetry", () => {

    let consent: boolean;
    let telemetry: Telemetry;

    /** What the user chooses, or the policy that locks the option, as the page hears of a change. */
    function changeConsent(given: boolean) {
        consent = given;
        const [, listener] = vi.mocked(optionsController.on).mock.calls.find(([event]) => event === "telemetryEnabled.change");
        (<() => void>listener)();
    }

    /** The items that reached the wire, by the name of their data, after the page has sent what waits. */
    function sentItems(): Envelope[] {
        telemetry.appInsights?.flush(false);
        return RecordingXhr.posts.flatMap(post => post.envelopes);
    }

    function namesOf(items: Envelope[]) {
        return items.map(item => item.data.baseData.name);
    }

    /** The events among the items, without the messages of the SDK. */
    function eventsOf(items: Envelope[]) {
        return items.filter(item => item.data.baseType === "EventData");
    }

    /** The page loaded again: the SDK of the page that goes away ends, and a new page creates its own. */
    function reloadPage() {
        telemetry.appInsights?.unload(false);
        RecordingXhr.posts.length = 0;
        telemetry = new Telemetry(details);
    }

    beforeEach(() => {
        RecordingXhr.posts.length = 0;
        sessionStorage.clear();
        localStorage.clear();
        vi.stubGlobal("XMLHttpRequest", RecordingXhr);
        consent = true;
        vi.mocked(optionsController.getOption).mockImplementation((option: string) => option === "telemetryEnabled" ? consent : undefined);
        vi.mocked(optionsController.on).mockReset();
    });

    afterEach(() => {
        telemetry?.appInsights?.unload(false);
        telemetry = undefined;
        vi.useRealTimers();
    });

    it("sends its start to the endpoint of the host, with the user, the run, the version and the properties of the host", () => {
        telemetry = new Telemetry(details);

        const items = sentItems();

        expect(RecordingXhr.posts.map(post => post.url)).toEqual(["http://127.0.0.1:9/v2/track"]);
        expect(namesOf(items)).toEqual(["Start"]);
        expect(items[0].tags).toMatchObject({
            "ai.user.id": "user-of-the-test",
            "ai.session.id": "session-of-the-test",
            "ai.application.ver": "1.2.3-test.4"
        });
        expect(items[0].data.baseData.properties).toMatchObject({ ProductName: "Bravo", InstallScope: "PerUser" });
    });

    it("sends what the page tracks and no timing of a page view", () => {
        telemetry = new Telemetry(details);

        telemetry.trackPage("Welcome");
        telemetry.appInsights.trackPageViewPerformance({ name: "Welcome" });

        expect(sentItems().map(item => item.data.baseType)).toEqual(["EventData", "PageviewData"]);
    });

    it("sends the critical errors of the SDK, among them the user agent at the start and at each page, as 1.0.x", () => {
        vi.useFakeTimers();
        telemetry = new Telemetry(details);

        telemetry.trackPage("Welcome");
        telemetry.appInsights.appInsights.core.logger.throwInternal(1, 1, "A failure of the SDK", undefined, true);
        // The SDK sends its own errors every 10 seconds
        vi.advanceTimersByTime(10_000);

        const messages = sentItems().filter(item => item.data.baseType === "MessageData").map(item => item.data.baseData.message);
        expect(messages).toEqual([
            expect.stringContaining(navigator.userAgent),
            expect.stringContaining(navigator.userAgent),
            expect.stringContaining("A failure of the SDK")
        ]);
    });

    it("turned off, drops what waits and sends nothing more; turned on, sends again", () => {
        telemetry = new Telemetry(details);
        telemetry.track("Waiting");

        changeConsent(false);
        telemetry.track("Off");
        expect(sentItems()).toEqual([]);

        changeConsent(true);
        telemetry.track("On");
        expect(namesOf(sentItems())).toEqual(["On"]);
    });

    it("off at the start, creates no sender until the user turns it on, then sends its start", () => {
        consent = false;
        telemetry = new Telemetry(details);
        expect(telemetry.appInsights).toBeUndefined();

        changeConsent(true);

        expect(namesOf(sentItems())).toEqual(["Start"]);
    });

    it("without a connection string, never creates a sender, whatever the user chooses", () => {
        telemetry = new Telemetry({ ...details, connectionString: undefined });

        telemetry.track("Something");
        telemetry.destroy();

        expect(telemetry.enabled).toBe(false);
        expect(telemetry.appInsights).toBeUndefined();
        expect(optionsController.on).not.toHaveBeenCalled();
    });

    it("keeps no cookie and nothing in the local storage, and what waits in the session storage of the page", () => {
        telemetry = new Telemetry(details);
        telemetry.track("Waiting");

        expect(document.cookie).toBe("");
        expect(localStorage.length).toBe(0);
        expect(sessionStorage.getItem("AI_buffer")).toContain("Waiting");
    });

    it("sends again, when the page loads again, what it posted and got no answer for", () => {
        telemetry = new Telemetry(details);
        telemetry.track("Unanswered");
        expect(namesOf(sentItems())).toEqual(["Start", "Unanswered"]);

        reloadPage();

        expect(namesOf(eventsOf(sentItems()))).toEqual(["Start", "Unanswered", "Start"]);
    });

    it("turned off, drops what waits in the session storage too: the page loaded again sends none of it", () => {
        telemetry = new Telemetry(details);
        telemetry.track("Unanswered");
        sentItems();
        telemetry.track("Waiting");
        expect(sessionStorage.getItem("AI_sentBuffer")).toContain("Unanswered");
        expect(sessionStorage.getItem("AI_buffer")).toContain("Waiting");

        changeConsent(false);
        expect(sessionStorage.getItem("AI_buffer")).toBe("[]");
        expect(sessionStorage.getItem("AI_sentBuffer")).toBe("[]");

        consent = true;
        reloadPage();

        expect(namesOf(sentItems())).toEqual(["Start"]);
    });
});
