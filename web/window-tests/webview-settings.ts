import { execFile } from 'node:child_process';
import path from 'node:path';
import { promisify } from 'node:util';

// Where the webview of a test gets the two values that the WebView2 runtime reads when the application creates
// its environment: the browser argument that opens the DevTools protocol, and the folder of the profile. The
// application passes neither.
//
// A process that is not elevated gets them from the environment variables of the runtime. An elevated process
// ignores those variables and honors the policies of the machine
// (https://learn.microsoft.com/microsoft-edge/webview2/concepts/security#for-an-elevated-host-app-use-appropriate-override-flags).
// On a runner of GitHub Actions the tests run elevated: there, and only there, the same two values are given as
// policies of the machine for Bravo.exe, and taken back after each test. Elsewhere the registry is never touched.

const execFileAsync = promisify(execFile);

// The tools of Windows, and not the ones of the same name that a shell such as Git Bash puts first in the path
const system = path.join(process.env.SystemRoot ?? 'C:\\Windows', 'System32');
const whoami = path.join(system, 'whoami.exe');
const reg = path.join(system, 'reg.exe');

/** The browser argument of every test: the engine chooses the port and writes it in the profile. */
const browserArguments = '--remote-debugging-port=0';

/** The start of the name of the folder of the profile of a test. */
export const profilePrefix = 'bravo-window-test-';

const policies = 'HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\WebView2';
const argumentsKey = `${policies}\\AdditionalBrowserArguments`;
const folderKey = `${policies}\\UserDataFolder`;

/** The name of the value of a policy: the executable that it applies to, never the * of every application. */
const valueName = 'Bravo.exe';

export interface WebViewSettings {

    /** Where the webview gets the values, for the log of the run. */
    readonly source: string

    /** Gives the values to the next start of Bravo.exe; returns the environment variables of that start. */
    give(profile: string): Promise<NodeJS.ProcessEnv>

    /** Takes back what `give` wrote, also when it failed halfway. */
    takeBack(): Promise<void>
}

class EnvironmentVariables implements WebViewSettings {

    readonly source = 'the environment variables WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS and WEBVIEW2_USER_DATA_FOLDER';

    async give(profile: string) {
        return { WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS: browserArguments, WEBVIEW2_USER_DATA_FOLDER: profile };
    }

    async takeBack() { }
}

class MachinePolicies implements WebViewSettings {

    readonly source = `the policies of the machine AdditionalBrowserArguments and UserDataFolder for ${valueName}, under ${policies}: the process is elevated, on a runner of GitHub Actions`;

    /** The folder that the policy names since `give`, until `takeBack`. */
    private profile: string | undefined;

    async give(profile: string) {
        for (const key of [argumentsKey, folderKey]) {
            if (await readPolicy(key) !== undefined)
                throw new Error(`The policy ${key} already has a value ${valueName}: the tests do not replace it.`);
        }

        this.profile = profile;
        await writePolicy(argumentsKey, browserArguments);
        await writePolicy(folderKey, profile);
        return {};
    }

    async takeBack() {
        const profile = this.profile;
        if (profile === undefined) return;

        // give found no value: whatever has its data now was written by it
        if (await readPolicy(argumentsKey) === browserArguments)
            await deletePolicy(argumentsKey);
        if (await readPolicy(folderKey) === profile)
            await deletePolicy(folderKey);

        this.profile = undefined;
    }
}

/** Where the webview of the tests of a worker gets its values. */
export async function chooseWebViewSettings(): Promise<WebViewSettings> {
    if (!await isElevated())
        return new EnvironmentVariables();

    if (!onGitHubActions())
        throw new Error('The tests run in an elevated process, whose webview ignores the environment variables of the WebView2 runtime: run them from a terminal that is not elevated.');

    return new MachinePolicies();
}

/**
 * The end of a run: on a runner of GitHub Actions, no policy that a test wrote is left, also after a test that
 * failed. The data tell which values a test wrote.
 */
export async function removeLeftPolicies() {
    if (!onGitHubActions() || !await isElevated()) return;

    if (await readPolicy(argumentsKey) === browserArguments)
        await deletePolicy(argumentsKey);

    const folder = await readPolicy(folderKey);
    if (folder !== undefined && path.basename(folder).startsWith(profilePrefix))
        await deletePolicy(folderKey);
}

function onGitHubActions() {
    return process.env.GITHUB_ACTIONS === 'true';
}

/** Whether the process is elevated: its integrity level is High, S-1-16-12288, among the groups that whoami lists. */
async function isElevated(): Promise<boolean> {
    const { stdout } = await execFileAsync(whoami, ['/groups', '/fo', 'csv', '/nh']);
    return stdout.includes('"S-1-16-12288"');
}

/** The data of the value of Bravo.exe under a key of the policies, or undefined when there is none. */
async function readPolicy(key: string): Promise<string | undefined> {
    let stdout: string;
    try {
        ({ stdout } = await execFileAsync(reg, ['query', key, '/v', valueName]));
    } catch {
        // reg ends with 1 when the key or the value is not there
        return undefined;
    }

    return /^\s+Bravo\.exe\s+REG_\w+\s+(.*?)\s*$/im.exec(stdout)?.[1] ?? '';
}

async function writePolicy(key: string, data: string) {
    await execFileAsync(reg, ['add', key, '/v', valueName, '/t', 'REG_SZ', '/d', data, '/f']);
}

async function deletePolicy(key: string) {
    await execFileAsync(reg, ['delete', key, '/v', valueName, '/f']);
}
