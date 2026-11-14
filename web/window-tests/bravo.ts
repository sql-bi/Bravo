import { type ChildProcess, execFile, spawn } from 'node:child_process';
import { existsSync, realpathSync } from 'node:fs';
import { mkdtemp, readdir, readFile, rm } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
import { promisify } from 'node:util';
import { type Browser, chromium, expect, type Page, test as base } from '@playwright/test';

// The fixture of the tests of the window: it starts the Bravo.exe that the build wrote, with a new profile of
// the webview and the DevTools protocol on a port that the engine chooses, attaches Playwright to the page, and
// closes the application as a user does. Nothing of the application changes for a test: the two environment
// variables are read by the WebView2 runtime, and the application passes no browser argument of its own.

const executable = path.join(__dirname, '..', '..', 'artifacts', 'bin', 'Bravo.Desktop', 'release', 'Bravo.exe');
const bundle = path.join(path.dirname(executable), 'wwwroot', 'js', 'main.js');
const pageOrigin = 'https://bravo.invalid';

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

/** The application of one test, with what its page did since the test attached to it. */
export class Bravo {

    readonly requests: PageRequest[] = [];
    readonly console: ConsoleEntry[] = [];
    readonly pageErrors: string[] = [];

    constructor(readonly page: Page, readonly address: string) {
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

    /** Forgets what the page did until now. */
    clear() {
        this.requests.length = 0;
        this.console.length = 0;
        this.pageErrors.length = 0;
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

        // A variable of the runtime that the environment of the tests already has would reach every start
        const inherited = Object.fromEntries(Object.entries(process.env).filter(([name]) => !name.startsWith('WEBVIEW2_')));

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
 * the browser processes of the webviews of Bravo.exe that run on the machine, and the names of what the folder
 * of the profile holds.
 */
async function describeStart(application: BravoProcess, profile: string): Promise<string> {
    const names = async (folder: string) => (await readdir(folder).catch((): string[] => [])).sort().join(', ') || 'nothing';
    const script = `
        $title = (Get-Process -Id $env:BRAVO_TEST_PID).MainWindowTitle
        $browsers = Get-CimInstance Win32_Process -Filter "Name = 'msedgewebview2.exe'" |
            Where-Object { $_.CommandLine -match 'webview-exe-name=Bravo.exe' -and $_.CommandLine -notmatch ' --type=' } |
            ForEach-Object { $_.CommandLine }
        "The main window of Bravo.exe is titled '$title'. The browser processes of the webviews of Bravo.exe run as: $($browsers -join ' | ')."`;
    const { stdout } = await execFileAsync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', script],
        { env: { ...process.env, BRAVO_TEST_PID: String(application.child.pid) } }).catch(error => ({ stdout: String(error) }));
    return `${stdout.trim().replace(/\r?\n/g, ' ')} The folder of the profile holds ${await names(profile)}; its folder EBWebView holds ${await names(path.join(profile, 'EBWebView'))}.`;
}

/** The port of the DevTools protocol, which the engine writes in its folder of the profile once it listens. */
async function readDevToolsPort(profile: string): Promise<number | undefined> {
    try {
        const [port] = (await readFile(path.join(profile, 'EBWebView', 'DevToolsActivePort'), 'utf8')).split('\n');
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

/** The processes of the webview of a profile, which end after the application. */
async function webViewProcesses(profile: string): Promise<number[]> {
    const { stdout } = await execFileAsync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command',
        "Get-CimInstance Win32_Process -Filter \"Name = 'msedgewebview2.exe'\" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($env:BRAVO_TEST_PROFILE) } | ForEach-Object { $_.ProcessId }"],
        { env: { ...process.env, BRAVO_TEST_PROFILE: path.basename(profile) } });
    return stdout.split(/\s+/).filter(id => id).map(Number);
}

async function waitForWebViewEnd(profile: string) {
    const limit = Date.now() + closeSeconds * 1000;
    let left = await webViewProcesses(profile);
    while (left.length && Date.now() < limit) {
        await delay(250);
        left = await webViewProcesses(profile);
    }
    if (left.length) {
        await Promise.all(left.map(id => execFileAsync('taskkill', ['/PID', String(id), '/F']).catch(() => { })));
        throw new Error(`${left.length} processes of the webview still ran ${closeSeconds} seconds after Bravo.exe ended: they were ended.`);
    }
}

/** Closes the application, waits for the end of its webview and removes its profile; returns the exit code. */
async function end(application: BravoProcess, browser: Browser | undefined, profile: string): Promise<number> {
    try {
        return await application.close();
    } finally {
        // Only now: closing a browser of the DevTools protocol would end the processes of the webview
        await browser?.close().catch(() => { });
        try {
            await waitForWebViewEnd(profile);
        } finally {
            await rm(profile, { recursive: true, force: true, maxRetries: 10 });
        }
    }
}

interface PageWithHost {
    bravoHost: { address: string }
}

export const test = base.extend<{ bravo: Bravo }>({

    // The start and the end of the application have a time of their own, which the test does not share
    bravo: [async ({ }, use, testInfo) => {
        checkBuild();

        // The long path: the temporary folder of a runner of continuous integration is a short 8.3 path
        const profile = await mkdtemp(path.join(realpathSync.native(os.tmpdir()), 'bravo-window-test-'));
        const application = new BravoProcess([], {
            WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS: '--remote-debugging-port=0',
            WEBVIEW2_USER_DATA_FOLDER: profile
        });

        let browser: Browser | undefined;
        let failure: unknown;
        try {
            const port = await waitFor('the DevTools protocol', application, () => readDevToolsPort(profile), () => describeStart(application, profile));
            await waitFor('its page', application, () => hasPage(port));

            browser = await chromium.connectOverCDP(`http://127.0.0.1:${port}`);
            const context = browser.contexts()[0];
            const page = context.pages().find(candidate => candidate.url().startsWith(pageOrigin));
            if (!page)
                throw new Error('Playwright was not told of the page of the application.');

            // Playwright Test traces only the contexts that it creates: this one is traced here, and the trace
            // is kept for a test that fails. It holds the headers of the requests, and so the token of the host.
            await context.tracing.start({ title: testInfo.title, screenshots: true, snapshots: true });

            await page.setViewportSize(pageSize);

            // Playwright gives a page the light color scheme unless told otherwise: the page has the one that
            // the window gives it, from the theme of the user
            await page.emulateMedia({ colorScheme: null });

            // The address only: no test reads the token of the host
            const address = await page.evaluate(() => (<PageWithHost><unknown>window).bravoHost.address);
            const bravo = new Bravo(page, address);

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

        const code = await end(application, browser, profile);
        if (failure instanceof Error) {
            // The code that the application ended with tells what happened to it
            failure.message += `\nBravo.exe ended with code ${code}.`;
        }
        if (failure)
            throw failure;

        expect(code, 'The exit code of Bravo.exe, closed as a user closes it').toBe(0);
    }, { timeout: (2 * startSeconds + 3 * closeSeconds) * 1000 }]
});

export { expect };
