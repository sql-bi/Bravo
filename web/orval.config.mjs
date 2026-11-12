import { defineConfig } from 'orval';

// The client of the host API is generated from the OpenAPI document of the repository, which a test of the
// host keeps equal to the routes. The output folder is not committed: every build writes it again.
export default defineConfig({
    bravo: {
        input: {
            target: '../src/Bravo.Api/openapi.json'
        },
        output: {
            target: './generated/bravo.ts',
            client: 'fetch',
            mode: 'single',
            override: {
                // Every request goes through one function, which knows the address and the token of the host.
                mutator: {
                    path: './controllers/host-fetch.ts',
                    name: 'hostFetch'
                },
                fetch: {
                    // A function returns the body of the answer. An error answer is thrown by hostFetch.
                    includeHttpResponseReturnType: false
                }
            }
        }
    }
});
