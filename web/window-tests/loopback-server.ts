import type { Server } from 'node:http';
import type { AddressInfo } from 'node:net';

// A server of a test: on the loopback address, on a port that the system chooses.

/** Makes a server listen, and returns its port. */
export async function listenOnLoopback(server: Server): Promise<number> {
    await new Promise<void>((resolve, reject) => {
        server.once('error', reject);
        server.listen(0, '127.0.0.1', resolve);
    });

    return (<AddressInfo>server.address()).port;
}

/** Stops a server, with the connections that are still open. */
export function closeServer(server: Server): Promise<void> {
    server.closeAllConnections();
    return new Promise(resolve => server.close(() => resolve()));
}
