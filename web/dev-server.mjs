import { readFileSync } from 'node:fs';
import path from 'node:path';
import { connectionFile, contentFolder, origin, port } from './dev.config.mjs';

// What "webpack serve" is told, through webpack.config.js, to serve the user interface to a browser: the
// development bundle, rebuilt at every change, with the static files of wwwroot, against a development build
// of the host (dev-host.mjs). The page is the one that the window of Bravo shows, and it is given the same
// object, window.bravoHost, before any of its scripts: the user interface has no branch for the browser.

const index = path.join(contentFolder, 'index.html');

function readConnection() {
    try {
        const { address, token } = JSON.parse(readFileSync(connectionFile, 'utf8'));
        return (address && token ? { address, token } : null);
    } catch (ignore) {
        return null;
    }
}

// The object as the webview of the application defines it. The file is read at every request: a host that
// was started again is found by loading the page again.
function createPage(connection) {
    const value = JSON.stringify(connection).replace(/</g, '\\u003c');
    const script = `<script>Object.defineProperty(window, "bravoHost", { value: Object.freeze(${value}), writable: false, configurable: false, enumerable: false });</script>`;

    const html = readFileSync(index, 'utf8');
    const firstScript = html.indexOf('<script');
    if (firstScript < 0)
        throw new Error(`${index} has no script to put the connection before.`);

    return `${html.slice(0, firstScript)}${script}\n    ${html.slice(firstScript)}`;
}

function servePage(request, response, next) {
    const url = new URL(request.url, origin);
    if (request.method !== 'GET' || (url.pathname !== '/' && url.pathname !== '/index.html')) {
        next();
        return;
    }

    // The page holds the token: nothing keeps it
    response.setHeader('Cache-Control', 'no-store');

    const connection = readConnection();
    if (!connection) {
        response.statusCode = 503;
        response.setHeader('Content-Type', 'text/plain; charset=utf-8');
        response.end(
            'The development host is not running.\n\n' +
            'Start it with "npm run serve:host" in web/, wait for its window, and load this page again.\n' +
            'If a development build of Bravo is already running, close it first: a second start is handed over to it.\n');
        return;
    }

    response.setHeader('Content-Type', 'text/html; charset=utf-8');
    response.end(createPage(connection));
}

/** The options of webpack-dev-server. */
export default {
    // The loopback address only: the page that this server gives out carries the token of the host
    host: '127.0.0.1',
    port: port,
    allowedHosts: ['127.0.0.1'],
    static: { directory: contentFolder },
    hot: false,
    liveReload: true,
    // Only what stops the build covers the page: an error of the page itself is in the console of the browser
    client: { overlay: { errors: true, warnings: false, runtimeErrors: false } },
    setupMiddlewares: middlewares => {
        // After the checks of the server on the Host and Origin headers of a request, and before what serves
        // files: the page with the token is given only to a request that passed those checks. Without them
        // the server does not start, rather than give the page out unchecked.
        const checks = ['host-header-check', 'cross-origin-header-check'];
        const positions = checks.map(name => middlewares.findIndex(middleware => middleware.name === name));
        const missing = checks.filter(name => !middlewares.some(middleware => middleware.name === name));
        if (missing.length)
            throw new Error(`webpack-dev-server has no middleware named ${missing.join(', ')}: the page would be served before its checks.`);

        middlewares.splice(Math.max(...positions) + 1, 0, { name: 'bravo-page', middleware: servePage });
        return middlewares;
    }
};
