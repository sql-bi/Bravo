/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { App } from './controllers/app';
import { Auth } from './controllers/auth';
import { Host } from './controllers/host';
import { DaxFormatterLineStyle, DaxFormatterSpacingStyle, OptionsController } from './controllers/options';
import { ThemeController, ThemeType } from './controllers/theme';

let host = new Host();

let optionsController = new OptionsController("host", {
    // Default options
    theme: ThemeType.Auto,
    telemetryEnabled: true,
    customOptions: {
        editorZoom: 1,
        daxFormatter: {
            spacingStyle: DaxFormatterSpacingStyle.BestPractice,
            lineStyle: DaxFormatterLineStyle.LongLine
        }
    }
});

let themeController =  new ThemeController();

let auth = new Auth();

let app = new App();

export { host, optionsController, themeController, auth };