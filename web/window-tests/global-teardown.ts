import { removeLeftPolicies } from './webview-settings';

// The end of the run of the tests of the window, after a test that failed as well
export default async function globalTeardown() {
    await removeLeftPolicies();
}
