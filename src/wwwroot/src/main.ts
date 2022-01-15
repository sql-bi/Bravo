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
import { Debug } from './controllers/debug';

// Load Tabulator modules
import { Tabulator, ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule  } from 'tabulator-tables';
Tabulator.registerModule([ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule]);

// Init the app
let debug = new Debug(process.env.MODE == "development");
let host = new Host(CONFIG.address);
let optionsController = new OptionsController();
let themeController = new ThemeController(CONFIG.theme);
let auth = new Auth();
let telemetry = new Telemetry(CONFIG.telemetry);
let pbiDesktop = new PBIDesktop();
let app = new App();

export { debug, host, optionsController, themeController, auth, telemetry, pbiDesktop };