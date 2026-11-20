import path from 'node:path';

// The tools of Windows, and not the ones of the same name that a shell such as Git Bash puts first in the path
const system = path.join(process.env.SystemRoot ?? 'C:\\Windows', 'System32');

export const whoami = path.join(system, 'whoami.exe');
export const reg = path.join(system, 'reg.exe');
