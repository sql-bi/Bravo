/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { App } from './controllers/app';
import { Auth } from './controllers/auth';
import { Host } from './controllers/host';
import { OptionsController } from './controllers/options';
import { ThemeController, ThemeType } from './controllers/theme';

// Load Tabulator modules
import { Tabulator, ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule  } from 'tabulator-tables';
Tabulator.registerModule([ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule]);

// Load sample database and intercept API calls in debug mode
import { debug } from './debug';

// Disable log in production
if (process.env.MODE == "production") {
    console.group = 
    console.groupCollapsed = 
    console.groupEnd =
    console.log = 
    console.warn = 
    console.error = 
    function() {};
} 

console.log(`--- Bravo for Power BI started in ${ debug ? "debug" : process.env.MODE } mode ---`);

// Init the app
let host = new Host(CONFIG.address);
let optionsController = new OptionsController();
let themeController =  new ThemeController(CONFIG.theme);
let auth = new Auth();
let app = new App();

export { host, optionsController, themeController, auth };