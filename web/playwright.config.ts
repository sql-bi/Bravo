import path from 'node:path';
import { defineConfig } from '@playwright/test';

// The tests of the user interface in the window of the application. Each test starts Bravo.exe and drives its
// webview through the DevTools protocol (window-tests/bravo.ts). The application has one instance for each
// user, so one test runs at a time.
const output = path.join(__dirname, '..', 'artifacts', 'window-tests');

export default defineConfig({
    testDir: 'window-tests',

    // Only the specs: the default pattern also takes the unit tests, *.test.ts
    testMatch: '**/*.spec.ts',

    workers: 1,
    fullyParallel: false,
    forbidOnly: !!process.env.CI,

    // A test that fails only sometimes is a defect to correct, not to repeat
    retries: 0,

    timeout: 60_000,
    outputDir: path.join(output, 'results'),
    reporter: process.env.CI
        ? [['list'], ['html', { outputFolder: path.join(output, 'report'), open: 'never' }]]
        : [['list']],

    use: {
        // A trace holds the headers of the requests, and so the token of the host of that test
        trace: 'retain-on-failure'
    }
});
