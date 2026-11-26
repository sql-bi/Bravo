import { type ChildProcess, execFile, spawn } from 'node:child_process';
import { existsSync, realpathSync } from 'node:fs';
import { mkdtemp, readdir, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
import { promisify } from 'node:util';
import { type Browser, chromium, expect, type Page, type Request, test as base } from '@playwright/test';
import { PolicyKey, type PolicyValues } from './policy-key';
import { TestIngestion } from './test-ingestion';
import { type TestProxyKind, TestProxyServer } from './test-proxy';
import { releasedFeed, UpdateFeedServer } from './update-feed';
import { chooseWebViewSettings, type WebViewSettings } from './webview-settings';

// The fixture of the tests of the window: it starts the Bravo.exe that the build wrote, with a new data folder,
// which holds its settings and the profile of its webview, and the DevTools protocol on a port that the engine
// chooses, attaches Playwright to the page, and closes the application as a user does. Nothing of the
// application changes for a test: the browser argument is read by the WebView2 runtime (webview-settings.ts),
// and the application passes none of its own. Its arguments name the data folder of the test, where a test can
// put the file of the settings that the application finds at its start (settingsFile), the update feed of the
// test, which a server of the test serves (update-feed.ts), and the key of the policies of the test, which the
// test writes (policy-key.ts). A test that opens a page in the default browser of the machine answers the request
// itself (answerBrowserRequests), and so does one that asks for the credentials of the proxy server or forgets
// them (answerProxyCredentialRequests). With the option proxyServer the settings name a proxy server of the test
// (test-proxy.ts). With the option telemetry the application sends its telemetry to an endpoint of the test
// (test-ingestion.ts). The proxy variables of the environment of the tests do not reach the application.

const executable = path.join(__dirname, '..', '..', 'artifacts', 'bin', 'Bravo.Desktop', 'release', 'Bravo.exe');
const bundle = path.join(path.dirname(executable), 'wwwroot', 'js', 'main.js');
const pageOrigin = 'https://bravo.invalid';
const settingsFileName = 'usersettings.json';

/** The size of the page in every test, whatever the size of the window. */
export const pageSize = { width: 1280, height: 800 };

const startSeconds = 60;
const closeSeconds = 10;

const execFileAsync = promisify(execFile);

/** A request of the page, without its headers: they hold the token of the host. */
export interface PageRequest {
    method: string
    url: string
}

export interface ConsoleEntry {
    type: string
    text: string
}

/** What a test answers a request with, in place of the host. */
interface HostAnswer {
    status: number
    json?: unknown
}

/** The application of one test, with what its page did since the test attached to it. */
export class Bravo {

    readonly requests: PageRequest[] = [];
    readonly console: ConsoleEntry[] = [];
    readonly pageErrors: string[] = [];
    /** The addresses that the page asked the host to open in the default browser. */
    readonly openedInBrowser: string[] = [];
    private readonly answers = new Map<string, (request: Request) => HostAnswer>();

    /**
     *  proxy The proxy server of the test that the settings of the application name, when the test has one.
     *  ingestion The endpoint of the test that the application sends its telemetry to, when the test has one.
     */
    constructor(readonly page: Page, readonly address: string, readonly dataFolder: string, readonly proxy?: TestProxyServer, readonly ingestion?: TestIngestion) {
        page.on('request', request => this.requests.push({ method: request.method(), url: request.url() }));
        page.on('console', message => this.console.push({ type: message.type(), text: message.text() }));
        page.on('pageerror', error => this.pageErrors.push(String(error)));
    }

    /** The requests of the page to the host, as `METHOD /path`, without the query and the preflights. */
    hostRequests(): string[] {
        return this.requests
            .filter(request => request.url.startsWith(`${this.address}/`) && request.method !== 'OPTIONS')
            .map(request => `${request.method} ${new URL(request.url).pathname}`);
    }

    /** The text of the file of the settings in the data folder of the application. */
    async readSettingsFile(): Promise<string> {
        return await readFile(path.join(this.dataFolder, settingsFileName), 'utf8');
    }

    /** Forgets what the page did until now. */
    clear() {
        this.requests.length = 0;
        this.console.length = 0;
        this.pageErrors.length = 0;
        this.openedInBrowser.length = 0;
    }

    /**
     * Answers in place of the host the requests that open a page in the default browser, and keeps their
     * addresses in `openedInBrowser`.
     */
    async answerBrowserRequests() {
        await this.answerInPlaceOfHost('/browser/open', request => {
            this.openedInBrowser.push((<{ address: string }>request.postDataJSON()).address);
            return { status: 204 };
        });
    }

    /**
     * Answers in place of the host the request that asks the user for the credentials of the proxy server, as a
     * user who entered them or who did not, and the one that forgets them. The host would show a dialog of
     * Windows that no test can close, and would delete from the Credential Manager of the machine.
     */
    async answerProxyCredentialRequests(entered: boolean) {
        await this.answerInPlaceOfHost('/network/proxy-credentials', request =>
            (request.method() === 'DELETE' ? { status: 204 } : { status: 200, json: { entered } }));
    }

    /**
     * Answers in place of the host the requests whose path starts with a prefix; a second call for the prefix
     * changes the answer. Only a test that needs it calls it: while the page has a route, Playwright answers
     * every CORS preflight of the page itself, in place of the host, and turns off the cache.
     */
    private async answerInPlaceOfHost(prefix: string, answer: (request: Request) => HostAnswer) {
        const isRouted = this.answers.has(prefix);
        this.answers.set(prefix, answer);
        if (isRouted) return;

        await this.page.route(url => url.href.startsWith(`${this.address}${prefix}`), async route => {
            const { status, json } = this.answers.get(prefix)(route.request());
            await route.fulfill({ status, json, headers: { 'Access-Control-Allow-Origin': pageOrigin } });
        });
    }

    /** Starts Bravo.exe once more, as a user or Power BI Desktop does, and returns the code that it ends with. */
    async startAgain(args: string[]): Promise<number> {
        const second = new BravoProcess(args, {});
        const code = await second.exitWithin(startSeconds);
        if (code === undefined) {
            await second.kill();
            throw new Error(`The second start of Bravo.exe did not end within ${startSeconds} seconds: its processes were ended.`);
        }

        return code;
    }
}

/** A process of Bravo.exe, started with no shell, so that its identifier is the one of the application. */
class BravoProcess {

    readonly child: ChildProcess;
    readonly exited: Promise<number>;
    exitCode: number | undefined;

    constructor(args: string[], environment: NodeJS.ProcessEnv) {

        // A variable of the runtime that the environment of the tests already has would reach every start, and
        // so would a proxy that it names: .NET reads it before the settings of Windows
        const inherited = Object.fromEntries(Object.entries(process.env)
            .filter(([name]) => !name.startsWith('WEBVIEW2_') && !/^(HTTP|HTTPS|ALL|NO)_PROXY$/i.test(name)));

        this.child = spawn(executable, args, { env: { ...inherited, ...environment }, stdio: 'ignore' });
        this.exited = new Promise((resolve, reject) => {
            this.child.once('error', reject);
            this.child.once('exit', code => {
                this.exitCode = code ?? -1;
                resolve(this.exitCode);
            });
        });

        // A process that could not start is reported to whoever awaits its end
        this.exited.catch(() => { });
    }

    get running() {
        return this.exitCode === undefined;
    }

    /** The exit code, or undefined when the process still runs after that many seconds. */
    async exitWithin(seconds: number): Promise<number | undefined> {
        const timer = new AbortController();
        try {
            return await Promise.race([this.exited, delay(seconds * 1000, undefined, { signal: timer.signal })]);
        } finally {
            timer.abort();
        }
    }

    /** Asks the window to close, as a user does, and returns the exit code; ends the processes if it does not. */
    async close(): Promise<number> {
        if (!this.running) return this.exitCode;

        // Without /F, taskkill posts WM_CLOSE to the windows of the process
        await execFileAsync('taskkill', ['/PID', String(this.child.pid)]).catch(() => { });

        const code = await this.exitWithin(closeSeconds);
        if (code === undefined) {
            await this.kill();
            throw new Error(`Bravo.exe did not close within ${closeSeconds} seconds of the request to close its window: its processes were ended.`);
        }

        return code;
    }

    async kill() {
        if (this.running)
            await execFileAsync('taskkill', ['/PID', String(this.child.pid), '/T', '/F']).catch(() => { });
    }
}

function checkBuild() {
    const build = 'In web/, run "npm run build:UI"; then, at the root of the repository, "dotnet build src/Bravo.Desktop -c Release"';
    if (!existsSync(executable))
        throw new Error(`Bravo.exe is not there: ${executable}. ${build}.`);
    if (!existsSync(bundle))
        throw new Error(`The bundle of the user interface is not beside Bravo.exe: ${bundle}. ${build}, which copies it there.`);
}

/** Waits for a value, asking again every tenth of a second, while the application runs. */
async function waitFor<T>(what: string, application: BravoProcess, probe: () => Promise<T | undefined>, explain = async () => ''): Promise<T> {
    const limit = Date.now() + startSeconds * 1000;
    for (; ;) {
        const value = await probe();
        if (value !== undefined) return value;

        if (!application.running) {
            // A start that is handed over to the instance that runs ends with 0, and shows nothing
            if (application.exitCode === 0)
                throw new Error('A development build of Bravo is already running, and the start of the test was handed over to it: close its window and run the tests again.');
            throw new Error(`Bravo.exe ended with code ${application.exitCode} before ${what} was there.`);
        }
        if (Date.now() > limit)
            throw new Error(`Bravo.exe did not show ${what} within ${startSeconds} seconds. ${await explain()}`.trimEnd());

        await delay(100);
    }
}

/**
 * What tells why the engine did not answer: the title of the window of the application, the command lines of
 * the browser processes of the webviews of Bravo.exe that run on the machine, and the names of what the data
 * folder of the test and the profile of the webview in it hold.
 */
async function describeStart(application: BravoProcess, dataFolder: string): Promise<string> {
    const names = async (folder: string) => (await readdir(folder).catch((): string[] => [])).sort().join(', ') || 'nothing';
    const script = `
        $title = (Get-Process -Id $env:BRAVO_TEST_PID).MainWindowTitle
        $browsers = Get-CimInstance Win32_Process -Filter "Name = 'msedgewebview2.exe'" |
            Where-Object { $_.CommandLine -match 'webview-exe-name=Bravo.exe' -and $_.CommandLine -notmatch ' --type=' } |
            ForEach-Object { $_.CommandLine }
        "The main window of Bravo.exe is titled '$title'. The browser processes of the webviews of Bravo.exe run as: $($browsers -join ' | ')."`;
    const { stdout } = await execFileAsync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', script],
        { env: { ...process.env, BRAVO_TEST_PID: String(application.child.pid) } }).catch(error => ({ stdout: String(error) }));
    return `${stdout.trim().replace(/\r?\n/g, ' ')} The data folder holds ${await names(dataFolder)}; the folder EBWebView of its profile holds ${await names(engineFolder(dataFolder))}.`;
}

/** The folder of the engine in the profile of the webview, which the application keeps in its data folder. */
function engineFolder(dataFolder: string) {
    return path.join(dataFolder, 'WebView2', 'EBWebView');
}

/** The port of the DevTools protocol, which the engine writes in its folder once it listens. */
async function readDevToolsPort(dataFolder: string): Promise<number | undefined> {
    try {
        const [port] = (await readFile(path.join(engineFolder(dataFolder), 'DevToolsActivePort'), 'utf8')).split('\n');
        return Number(port) || undefined;
    } catch {
        return undefined;
    }
}

/** Whether the engine lists the page of the application: Playwright is told only of the pages that exist when it attaches. */
async function hasPage(port: number): Promise<true | undefined> {
    const targets = <{ type: string, url: string }[]>await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
    return targets.some(target => target.type === 'page' && target.url.startsWith(pageOrigin)) || undefined;
}

/** The processes of the webview whose profile is in a data folder, which end after the application. */
async function webViewProcesses(dataFolder: string): Promise<number[]> {
    const { stdout } = await execFileAsync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command',
        "Get-CimInstance Win32_Process -Filter \"Name = 'msedgewebview2.exe'\" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($env:BRAVO_TEST_FOLDER) } | ForEach-Object { $_.ProcessId }"],
        { env: { ...process.env, BRAVO_TEST_FOLDER: path.basename(dataFolder) } });
    return stdout.split(/\s+/).filter(id => id).map(Number);
}

async function waitForWebViewEnd(dataFolder: string) {
    const limit = Date.now() + closeSeconds * 1000;
    let left = await webViewProcesses(dataFolder);
    while (left.length && Date.now() < limit) {
        await delay(250);
        left = await webViewProcesses(dataFolder);
    }
    if (left.length) {
        await Promise.all(left.map(id => execFileAsync('taskkill', ['/PID', String(id), '/F']).catch(() => { })));
        throw new Error(`${left.length} processes of the webview still ran ${closeSeconds} seconds after Bravo.exe ended: they were ended.`);
    }
}

/** Closes the application, waits for the end of its webview and removes its data folder; returns the exit code. */
async function end(application: BravoProcess | undefined, browser: Browser | undefined, dataFolder: string): Promise<number | undefined> {
    try {
        return await application?.close();
    } finally {
        // Only now: closing a browser of the DevTools protocol would end the processes of the webview
        await browser?.close().catch(() => { });
        try {
            await waitForWebViewEnd(dataFolder);
        } finally {
            await rm(dataFolder, { recursive: true, force: true, maxRetries: 10 });
        }
    }
}

/** The file of the settings that names a proxy server of the user, as the application keeps it: the kind of the proxy is a number there. */
function proxySettings(address: string) {
    return JSON.stringify({ proxy: { type: 2, useDefaultCredentials: true, address, bypassOnLocal: false } });
}

interface PageWithHost {
    bravoHost: { address: string }
}

export const test = base.extend<{ bravo: Bravo, updateFeed: string | null, settingsFile: string | null, policies: PolicyValues | null, proxyServer: TestProxyKind | null, telemetry: boolean }, { webView: WebViewSettings }>({

    // The update feed that the application of a test reads, by default one with an older release; with null
    // the application has none, and its update check fails
    updateFeed: [releasedFeed, { option: true }],

    // The text of the file of the settings that the application finds in its data folder at its start; with
    // null, the default, there is no file
    settingsFile: [null, { option: true }],

    // The values of the policies that the application reads at its start, by their names; with null, the
    // default, it reads no key of policies
    policies: [null, { option: true }],

    // What the proxy server of the test does, which the settings of the application then name as the proxy
    // server of the user: it answers with the update feed, or asks for credentials. With null, the default,
    // the test has none. A test gives this option or the file of the settings, not both
    proxyServer: [null, { option: true }],

    // Whether the application sends its telemetry, to an endpoint of the test; by default it sends none
    telemetry: [false, { option: true }],

    // Decided once for the tests of a worker, and said in the log of the run
    webView: [async ({ }, use) => {
        const webView = await chooseWebViewSettings();
        console.log(`The webview of Bravo.exe gets the DevTools port from ${webView.source}.`);
        await use(webView);
    }, { scope: 'worker' }],

    // The start and the end of the application have a time of their own, which the test does not share
    bravo: [async ({ webView, updateFeed, settingsFile, policies, proxyServer, telemetry }, use, testInfo) => {
        checkBuild();

        // The long path: the temporary folder of a runner of continuous integration is a short 8.3 path
        const dataFolder = await mkdtemp(path.join(realpathSync.native(os.tmpdir()), 'bravo-window-test-'));

        let feed: UpdateFeedServer | undefined;
        let testProxy: TestProxyServer | undefined;
        let ingestion: TestIngestion | undefined;
        let policyKey: PolicyKey | undefined;
        let application: BravoProcess | undefined;
        let browser: Browser | undefined;
        let failure: unknown;
        try {
            if (proxyServer !== null && settingsFile !== null)
                throw new Error('A test gives the option proxyServer or the file of the settings, not both.');

            testProxy = (proxyServer === null ? undefined : await TestProxyServer.start(proxyServer, updateFeed ?? releasedFeed));
            const settings = (testProxy ? proxySettings(testProxy.address) : settingsFile);
            if (settings !== null)
                await writeFile(path.join(dataFolder, settingsFileName), settings, 'utf8');

            feed = (updateFeed === null ? undefined : await UpdateFeedServer.start(updateFeed));
            policyKey = (policies === null ? undefined : await PolicyKey.write(policies));
            ingestion = (telemetry ? await TestIngestion.start(pageOrigin) : undefined);
            const args = [
                `--dev-data-folder=${dataFolder}`,
                ...(feed ? [`--dev-update-feed=${feed.address}`] : []),
                ...(policyKey ? [`--dev-policy-key=${policyKey.name}`] : []),
                ...(ingestion ? [`--dev-telemetry-endpoint=${ingestion.origin}`] : [])
            ];
            const started = new BravoProcess(args, await webView.give());
            application = started;

            const port = await waitFor('the DevTools protocol', started, () => readDevToolsPort(dataFolder), () => describeStart(started, dataFolder));
            await waitFor('its page', started, () => hasPage(port));

            browser = await chromium.connectOverCDP(`http://127.0.0.1:${port}`);
            const context = browser.contexts()[0];

            // The engine lists the address that the first navigation goes to before the page commits it, and
            // Playwright tells the address that the page committed: on a slow first start they differ for a while
            const page = await waitFor('its page to Playwright', started,
                async () => context.pages().find(candidate => candidate.url().startsWith(pageOrigin)),
                async () => `Playwright tells the pages ${context.pages().map(candidate => candidate.url()).join(', ') || 'none'}.`);

            // Playwright Test traces only the contexts that it creates: this one is traced here, and the trace
            // is kept for a test that fails. It holds the headers of the requests, and so the token of the host.
            await context.tracing.start({ title: testInfo.title, screenshots: true, snapshots: true });

            await page.setViewportSize(pageSize);

            // Playwright gives a page the light color scheme unless told otherwise: the page has the one that
            // the window gives it, from the theme of the user
            await page.emulateMedia({ colorScheme: null });

            // The address only: no test reads the token of the host
            const address = await page.evaluate(() => (<PageWithHost><unknown>window).bravoHost.address);
            const bravo = new Bravo(page, address, dataFolder, testProxy, ingestion);

            await expect(page.locator('#ctrl-theme'), 'The page did not start').toBeVisible({ timeout: startSeconds * 1000 });

            await use(bravo);

            if (testInfo.status !== testInfo.expectedStatus) {
                const screenshot = testInfo.outputPath('screenshot.png');
                await page.screenshot({ path: screenshot });
                const trace = testInfo.outputPath('trace.zip');
                await context.tracing.stop({ path: trace });
                testInfo.attachments.push(
                    { name: 'screenshot', path: screenshot, contentType: 'image/png' },
                    { name: 'trace', path: trace, contentType: 'application/zip' });
            } else {
                await context.tracing.stop();
            }

        } catch (error) {
            failure = error;
        }

        let code: number | undefined;
        try {
            code = await end(application, browser, dataFolder);
        } finally {
            await feed?.close();
            await testProxy?.close();
            await ingestion?.close();
            await policyKey?.remove();
            await webView.takeBack();
        }

        if (failure instanceof Error && code !== undefined) {
            // The code that the application ended with tells what happened to it
            failure.message += `\nBravo.exe ended with code ${code}.`;
        }
        if (failure)
            throw failure;

        expect(code, 'The exit code of Bravo.exe, closed as a user closes it').toBe(0);
    }, { timeout: (2 * startSeconds + 3 * closeSeconds) * 1000 }]
});

export { expect };
