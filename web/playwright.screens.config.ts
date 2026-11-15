import path from 'node:path';
import { defineConfig } from '@playwright/test';
import windowTests from './playwright.config';

// The screens of the window that two builds of the bundle are compared on, with the fixture and the settings of
// the tests of the window. "npm run screens -- take <name>" runs it; "npm run test:window" and continuous
// integration do not.
export default defineConfig({
    ...windowTests,
    testMatch: 'take-screens.ts',
    // The trace and the screenshot of a failed run, apart from the screens
    outputDir: path.join(__dirname, '..', 'artifacts', 'window-tests', 'screens-results'),
    reporter: [['list']]
});
