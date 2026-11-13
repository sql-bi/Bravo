import { defineConfig } from 'vitest/config';

// The unit tests of the user interface. They read the sources that webpack bundles, which Vite transforms for
// them, and run in a simulated browser: many modules use the document when they are loaded.
export default defineConfig({
    test: {
        include: ['tests/**/*.test.ts'],
        environment: 'jsdom',

        // A module that leads to the views loads the chart of the model analysis, whose package imports a
        // folder of chart.js: Node refuses that, Vite resolves it as webpack does
        server: { deps: { inline: ['chartjs-chart-treemap'] } },

        // Nothing that a test replaced is left for the next one
        restoreMocks: true,
        unstubGlobals: true
    }
});
