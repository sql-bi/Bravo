import { createServer, type IncomingMessage, type Server } from 'node:http';
import { gunzipSync } from 'node:zlib';
import { closeServer, listenOnLoopback } from './loopback-server';

// The endpoint of Application Insights of a test, on the loopback address, which the application names with
// --dev-telemetry-endpoint: no item of a test reaches a real resource. It accepts every item, as the ingestion
// does, and keeps it. The page posts to it from its own origin, after the preflight of the engine; the host
// posts compressed lines.

/** An item of the telemetry as the SDKs put it on the wire. */
export interface TelemetryItem {
    name: string
    tags: Record<string, string>
    data: { baseType: string, baseData: { name?: string, properties?: Record<string, string> } }
}

export class TestIngestion {

    /** The items that the endpoint received, in their order. */
    readonly items: TelemetryItem[] = [];

    private constructor(private readonly server: Server, private readonly port: number) { }

    /** Starts the endpoint, which answers the page of an origin. */
    static async start(pageOrigin: string): Promise<TestIngestion> {
        const server = createServer();
        const ingestion = new TestIngestion(server, await listenOnLoopback(server));
        server.on('request', async (request, response) => {
            const cors = { 'Access-Control-Allow-Origin': pageOrigin };
            if (request.method === 'OPTIONS') {
                response.writeHead(204, {
                    ...cors,
                    'Access-Control-Allow-Methods': 'POST',
                    'Access-Control-Allow-Headers': request.headers['access-control-request-headers'] ?? ''
                });
                response.end();
                return;
            }

            const items = parseItems(await readBody(request));
            ingestion.items.push(...items);
            response.writeHead(200, { ...cors, 'Content-Type': 'application/json' });
            response.end(JSON.stringify({ itemsReceived: items.length, itemsAccepted: items.length, errors: [] }));
        });

        return ingestion;
    }

    /** The origin of the endpoint, as the command line of the application names it. */
    get origin(): string {
        return `http://127.0.0.1:${this.port}`;
    }

    close(): Promise<void> {
        return closeServer(this.server);
    }
}

async function readBody(request: IncomingMessage): Promise<string> {
    const chunks: Buffer[] = [];
    for await (const chunk of request)
        chunks.push(<Buffer>chunk);

    const body = Buffer.concat(chunks);
    return (request.headers['content-encoding'] === 'gzip' ? gunzipSync(body) : body).toString('utf8');
}

// The page posts a JSON array; the host, one item on each line
function parseItems(body: string): TelemetryItem[] {
    const text = body.trim();
    if (text.startsWith('['))
        return <TelemetryItem[]>JSON.parse(text);

    return text.split('\n').filter(line => line.trim()).map(line => <TelemetryItem>JSON.parse(line));
}
