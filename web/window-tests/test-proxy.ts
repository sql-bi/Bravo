import { createServer, type Server } from 'node:http';
import type { AddressInfo } from 'node:net';

// The proxy server of a test, on the loopback address, which the settings of the application name. It forwards
// nothing: it answers every request itself, with the update feed of the test, or asks for credentials that
// no test has, and keeps the addresses that it was asked for.

/** What the proxy server of a test does with a request: it answers it, or asks for credentials. */
export type TestProxyKind = 'answers' | 'asks';

export class TestProxyServer {

    /** The addresses that the proxy server was asked for. */
    readonly requests: string[] = [];

    private constructor(private readonly server: Server) { }

    static async start(kind: TestProxyKind, feed: string): Promise<TestProxyServer> {
        const server = createServer();
        const proxy = new TestProxyServer(server);
        server.on('request', (request, response) => {
            proxy.requests.push(request.url ?? '');
            if (kind === 'asks') {
                response.writeHead(407, { 'Proxy-Authenticate': 'Basic realm="test"', 'Content-Length': 0 });
                response.end();
            } else {
                response.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
                response.end(feed);
            }
        });

        await new Promise<void>((resolve, reject) => {
            server.once('error', reject);
            server.listen(0, '127.0.0.1', resolve);
        });

        return proxy;
    }

    /** The address of the proxy server, as the application writes it. */
    get address(): string {
        return `http://127.0.0.1:${(<AddressInfo>this.server.address()).port}/`;
    }

    close(): Promise<void> {
        this.server.closeAllConnections();
        return new Promise(resolve => this.server.close(() => resolve()));
    }
}
