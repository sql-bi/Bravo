import { spawnSync } from 'node:child_process';
import { readdir, readFile, rm } from 'node:fs/promises';
import path from 'node:path';

// The command of the screens of the window, which compares two builds of the bundle (ADR 0019):
//
//   npm run screens -- take <name>              takes the screens of the bundle beside Bravo.exe
//   npm run screens -- compare <name> <name>    compares two sets of screens, byte for byte
//
// A set is the folder artifacts/screens/<name>, which take replaces. Compare names the screens that differ or
// that only one set has, and ends with 1 when there is one, with 2 when it cannot compare. Rendering changes
// with the machine: two sets are compared only when one machine took both, one after the other.

const web = path.join(import.meta.dirname, '..');
const sets = path.join(web, '..', 'artifacts', 'screens');
const usage = 'Usage: npm run screens -- take <name>, or npm run screens -- compare <name> <name>';

class UsageError extends Error { }

function setFolder(name: string | undefined): string {
    if (!name || !/^[A-Za-z0-9][\w.-]*$/.test(name))
        throw new UsageError(`A set of screens is named by letters, digits, ".", "-" and "_": ${name ?? 'no name'}. ${usage}`);
    return path.join(sets, name);
}

async function screensOf(folder: string): Promise<string[]> {
    const names = await readdir(folder).catch((): string[] => []);
    return names.filter(name => name.endsWith('.png')).sort();
}

/** Runs take-screens.ts with the Playwright Test of the project, which writes the set in a new folder. */
async function take(name: string): Promise<number> {
    const folder = setFolder(name);
    await rm(folder, { recursive: true, force: true });

    const playwright = path.join(web, 'node_modules', '@playwright', 'test', 'cli.js');
    const result = spawnSync(process.execPath, [playwright, 'test', '--config', path.join(web, 'playwright.screens.config.ts')],
        { cwd: web, stdio: 'inherit', env: { ...process.env, BRAVO_SCREENS_FOLDER: folder } });
    if (result.status === 0)
        console.log(`The screens are in ${folder}.`);
    return result.status ?? 1;
}

async function compare(first: string, second: string): Promise<number> {
    const folders = [setFolder(first), setFolder(second)];
    const [a, b] = await Promise.all(folders.map(screensOf));
    for (const [index, screens] of [a, b].entries()) {
        if (!screens.length)
            throw new UsageError(`The set ${folders[index]} has no screen: take it with "npm run screens -- take ${[first, second][index]}".`);
    }

    const differences: string[] = [];
    for (const screen of [...new Set([...a, ...b])].sort()) {
        if (!a.includes(screen) || !b.includes(screen)) {
            differences.push(`${screen}: only in ${a.includes(screen) ? first : second}`);
            continue;
        }
        const [one, other] = await Promise.all(folders.map(folder => readFile(path.join(folder, screen))));
        if (!one.equals(other))
            differences.push(`${screen}: the images differ`);
    }

    const count = new Set([...a, ...b]).size;
    if (!differences.length) {
        console.log(`The ${count} screens of ${first} and ${second} are the same bytes.`);
        return 0;
    }

    console.log(`${differences.length} of the ${count} screens of ${first} and ${second} differ:`);
    for (const difference of differences)
        console.log(`  ${difference}`);
    return 1;
}

async function run(args: string[]): Promise<number> {
    const [command, ...names] = args;
    if (command === 'take' && names.length === 1)
        return await take(names[0]);
    if (command === 'compare' && names.length === 2)
        return await compare(names[0], names[1]);
    throw new UsageError(usage);
}

run(process.argv.slice(2)).then(code => {
    process.exitCode = code;
}, (error: unknown) => {
    if (!(error instanceof UsageError))
        throw error;
    console.error(error.message);
    process.exitCode = 2;
});
