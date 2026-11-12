import { spawn } from 'node:child_process';
import { existsSync, rmSync } from 'node:fs';
import { connectionFile, hostProject, origin } from './dev.config.mjs';

// Starts a development build of the host for a page in a browser: it allows the origin of the development
// server and writes the address and the token of its API where that server reads them. Its window opens as
// at every start; closing the window ends the host.

// A file left by a host that was ended abruptly would give the page a host that is no longer there.
rmSync(connectionFile, { force: true });

const host = spawn(
    'dotnet',
    ['run', '--project', hostProject, '-c', 'Release', '--', `--dev-origin=${origin}`, `--dev-connection-file=${connectionFile}`],
    { stdio: 'inherit' });

let ready = false;
const watch = setInterval(() => {
    if (existsSync(connectionFile)) {
        ready = true;
        clearInterval(watch);
        console.log(`The host is ready. Start the page with "npm run serve:UI" and open ${origin}`);
    }
}, 500);

host.on('exit', code => {
    clearInterval(watch);
    if (!ready && code === 0) {
        // The start was handed over to the instance that was running, which knows nothing of the browser.
        console.error('A development build of Bravo is already running, and this start was handed over to it: close its window and start again.');
        process.exit(1);
    }

    process.exit(code ?? 1);
});
