/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { App } from './controllers/app';
import { Auth } from './controllers/auth';
import { Host } from './controllers/host';
import { OptionsController } from './controllers/options';
import { ThemeController } from './controllers/theme';
import { Telemetry } from './controllers/telemetry';
import { PBIDesktop } from './controllers/pbi-desktop';
import { NotifyCenter } from './controllers/notifications';
import { Debug } from './controllers/debug';

// Load Tabulator modules
import { Tabulator, ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule  } from 'tabulator-tables';

Tabulator.registerModule([ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule]);

// Init the app
let debug = new Debug(process.env.MODE == "development");
let host = new Host(CONFIG.address, CONFIG.token);
let optionsController = new OptionsController(CONFIG.options);
let themeController = new ThemeController();
let auth = new Auth();
let telemetry = new Telemetry(CONFIG.telemetry);
let pbiDesktop = new PBIDesktop();
let notificationCenter = new NotifyCenter();
let app = new App();

console.log("Bravo for Power BI", CONFIG);

export { debug, host, optionsController, themeController, auth, telemetry, pbiDesktop, notificationCenter };