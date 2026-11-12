import path from 'node:path';
import { fileURLToPath } from 'node:url';

// What the development server and the host started for it agree on. The origin is written with the loopback
// address, as the API of the host is: a browser treats the two as one network, and the page may call the API.
const web = path.dirname(fileURLToPath(import.meta.url));

export const port = 8098;

/** The origin of the page in a browser: the host is started with it as its one more allowed origin. */
export const origin = `http://127.0.0.1:${port}`;

/** Where the host writes the address and the token of its API, and the development server reads them. */
export const connectionFile = path.join(web, '.dev', 'host.json');

/** The static files of the user interface, the same that go beside the executable. */
export const contentFolder = path.join(web, 'wwwroot');

export const hostProject = path.join(web, '..', 'src', 'Bravo.Desktop');
