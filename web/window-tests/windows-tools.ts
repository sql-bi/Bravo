import { execFile } from 'node:child_process';
import path from 'node:path';
import { promisify } from 'node:util';

// The tools of Windows, and not the ones of the same name that a shell such as Git Bash puts first in the path

const execFileAsync = promisify(execFile);
const system = path.join(process.env.SystemRoot ?? 'C:\\Windows', 'System32');

/** Runs whoami.exe with the arguments; returns what it wrote. */
export function whoami(args: string[]) {
    return execFileAsync(path.join(system, 'whoami.exe'), args);
}

/** Runs reg.exe with the arguments; returns what it wrote, and fails when it ends with another code than 0. */
export function reg(args: string[]) {
    return execFileAsync(path.join(system, 'reg.exe'), args);
}
