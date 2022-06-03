/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { App, AppVersion } from './controllers/app';
import { Auth } from './controllers/auth';
import { Host } from './controllers/host';
import { DiagnosticLevelType, OptionsController } from './controllers/options';
import { ThemeController } from './controllers/theme';
import { Telemetry } from './controllers/telemetry';
import { PBIDesktop } from './controllers/pbi-desktop';
import { NotifyCenter } from './controllers/notifications';
import { Logger } from './controllers/logger';
import { Debug } from './controllers/debug';

// Load Tabulator modules
import { Tabulator, ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule, TooltipModule  } from 'tabulator-tables';

Tabulator.registerModule([ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule, TooltipModule]);

// Init the app
let debug = new Debug(!!CONFIG.debug);
let host = new Host(CONFIG.address, CONFIG.token);
let optionsController = new OptionsController(CONFIG.options);
let themeController = new ThemeController();
let logger = new Logger(CONFIG.options.diagnosticLevel !== DiagnosticLevelType.None);
let auth = new Auth();
let telemetry = new Telemetry(CONFIG.telemetry);
let pbiDesktop = new PBIDesktop();
let notificationCenter = new NotifyCenter();

let app = new App(new AppVersion({
    version: CONFIG.version,
    build: CONFIG.build
}));

export { debug, host, optionsController, themeController, auth, telemetry, pbiDesktop, notificationCenter, logger, app };