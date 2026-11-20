import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { reg, whoami } from './windows-tools';

// Where the webview of a test gets the browser argument that opens the DevTools protocol, which the WebView2
// runtime reads when the application creates its environment and which the application does not pass. The
// profile is not given: the application keeps it in its data folder, which the fixture names on its command line.
//
// A process that is not elevated gets the argument from an environment variable of the runtime. An elevated
// process ignores that variable and honors the policies of the machine
// (https://learn.microsoft.com/microsoft-edge/webview2/concepts/security#for-an-elevated-host-app-use-appropriate-override-flags).
// On a runner of GitHub Actions the tests run elevated: there, and only there, the same value is given as a policy
// of the machine for Bravo.exe, and taken back after each test. Elsewhere the registry is never touched.

const execFileAsync = promisify(execFile);

/** The browser argument of every test: the engine chooses the port and writes it in the profile. */
const browserArguments = '--remote-debugging-port=0';

const argumentsKey = 'HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\WebView2\\AdditionalBrowserArguments';

/** The name of the value of a policy: the executable that it applies to, never the * of every application. */
const valueName = 'Bravo.exe';

export interface WebViewSettings {

    /** Where the webview gets the value, for the log of the run. */
    readonly source: string

    /** Gives the value to the next start of Bravo.exe; returns the environment variables of that start. */
    give(): Promise<NodeJS.ProcessEnv>

    /** Takes back what `give` wrote, also when it failed halfway. */
    takeBack(): Promise<void>
}

class EnvironmentVariables implements WebViewSettings {

    readonly source = 'the environment variable WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS';

    async give() {
        return { WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS: browserArguments };
    }

    async takeBack() { }
}

class MachinePolicies implements WebViewSettings {

    readonly source = `the policy of the machine AdditionalBrowserArguments for ${valueName}: the process is elevated, on a runner of GitHub Actions`;

    /** Whether the policy has the value of `give`, until `takeBack`. */
    private given = false;

    async give() {
        if (await readPolicy() !== undefined)
            throw new Error(`The policy ${argumentsKey} already has a value ${valueName}: the tests do not replace it.`);

        this.given = true;
        await writePolicy();
        return {};
    }

    async takeBack() {
        if (!this.given) return;

        // give found no value: whatever has its data now was written by it
        await deleteWrittenPolicy();

        this.given = false;
    }
}

/** Where the webview of the tests of a worker gets its value. */
export async function chooseWebViewSettings(): Promise<WebViewSettings> {
    if (!await isElevated())
        return new EnvironmentVariables();

    if (!onGitHubActions())
        throw new Error('The tests run in an elevated process, whose webview ignores the environment variables of the WebView2 runtime: run them from a terminal that is not elevated.');

    return new MachinePolicies();
}

/**
 * The end of a run: on a runner of GitHub Actions, no policy that a test wrote is left, also after a test that
 * failed. The data tell whether a test wrote the value.
 */
export async function removeLeftPolicies() {
    if (!onGitHubActions() || !await isElevated()) return;

    await deleteWrittenPolicy();
}

function onGitHubActions() {
    return process.env.GITHUB_ACTIONS === 'true';
}

/** Whether the process is elevated: its integrity level is High, S-1-16-12288, among the groups that whoami lists. */
async function isElevated(): Promise<boolean> {
    const { stdout } = await execFileAsync(whoami, ['/groups', '/fo', 'csv', '/nh']);
    return stdout.includes('"S-1-16-12288"');
}

/** The data of the value of Bravo.exe under the key of the policy, or undefined when there is none. */
async function readPolicy(): Promise<string | undefined> {
    let stdout: string;
    try {
        ({ stdout } = await execFileAsync(reg, ['query', argumentsKey, '/v', valueName]));
    } catch {
        // reg ends with 1 when the key or the value is not there
        return undefined;
    }

    return /^\s+Bravo\.exe\s+REG_\w+\s+(.*?)\s*$/im.exec(stdout)?.[1] ?? '';
}

async function writePolicy() {
    await execFileAsync(reg, ['add', argumentsKey, '/v', valueName, '/t', 'REG_SZ', '/d', browserArguments, '/f']);
}

/** Deletes the value of Bravo.exe when its data are the ones that the tests write. */
async function deleteWrittenPolicy() {
    if (await readPolicy() === browserArguments)
        await execFileAsync(reg, ['delete', argumentsKey, '/v', valueName, '/f']);
}
