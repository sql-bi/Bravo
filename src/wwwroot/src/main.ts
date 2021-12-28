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
import { debug } from './debug';

let host = new Host(CONFIG.address);
let optionsController = new OptionsController();
let themeController =  new ThemeController(CONFIG.theme);
let auth = new Auth();
let app = new App();

export { host, optionsController, themeController, auth };