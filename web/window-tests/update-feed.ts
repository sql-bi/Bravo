import { createServer, type Server } from 'node:http';
import { closeServer, listenOnLoopback } from './loopback-server';

// The update feed of a test, which the application reads in place of the one of the releases: a development
// build checks only the feed that its command line names.

/** The text of an update feed that names a release, with the members that installed copies read. */
export function feedOf(version: string, download: string): string {
    return JSON.stringify({ channel: 'Stable', version, download, changelog: 'https://github.com/sql-bi/Bravo/releases' });
}

/** The feed of release 1.0.14, as the storage of the releases has it: older than any build of this branch. */
export const releasedFeed = feedOf('1.0.9805.22885', 'https://github.com/sql-bi/Bravo/releases/download/v1.0.14/Bravo.1.0.14.x64.msi');

/** Serves a feed as the storage of the releases does: UTF-16 with a byte order mark, and no charset. */
export class UpdateFeedServer {

    private constructor(private readonly server: Server, private readonly port: number) { }

    static async start(text: string): Promise<UpdateFeedServer> {
        const body = Buffer.concat([Buffer.from([0xFF, 0xFE]), Buffer.from(text, 'utf16le')]);
        const server = createServer((_, response) => {
            response.writeHead(200, { 'Content-Type': 'application/json' });
            response.end(body);
        });

        return new UpdateFeedServer(server, await listenOnLoopback(server));
    }

    get address(): string {
        return `http://127.0.0.1:${this.port}/currentversion.json`;
    }

    close(): Promise<void> {
        return closeServer(this.server);
    }
}
