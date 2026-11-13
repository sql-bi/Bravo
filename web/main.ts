/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { App, AppVersion } from './controllers/app';
import { Auth } from './controllers/auth';
import { Host } from './controllers/host';
import { connectHost } from './controllers/host-fetch';
import { HostEvents } from './controllers/host-events';
import { Options, OptionsController } from './controllers/options';
import { ThemeController } from './controllers/theme';
import { Telemetry } from './controllers/telemetry';
import { PBIDesktop } from './controllers/pbi-desktop';
import { NotifyCenter } from './controllers/notifications';
import { Logger } from './controllers/logger';
import { Debug } from './controllers/debug';
import { showStartFailure } from './view/start-failure';
import { ApplicationDetails, DiagnosticLevel, getApplication, getOptions } from './generated/bravo';

// Load Tabulator modules
import { Tabulator, ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule, TooltipModule  } from 'tabulator-tables';

Tabulator.registerModule([ColumnCalcsModule, DataTreeModule, FilterModule, FormatModule, InteractionModule, ResizeColumnsModule, ResizeTableModule, SelectRowModule, SortModule, TooltipModule]);

// Until the options are known the page follows the color scheme that it is told: in the window of Bravo the
// theme of the user, which the host knows already, in a browser the color mode of the system. Nothing flashes
// in the other mode while the host answers.
if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches)
    document.body.classList.add("dark");

// What the other modules import from here. Each one has its value before the first of them is created.
let application: ApplicationDetails;
let debug: Debug;
let host: Host;
let optionsController: OptionsController;
let themeController: ThemeController;
let logger: Logger;
let telemetry: Telemetry;
let auth: Auth;
let pbiDesktop: PBIDesktop;
let notificationCenter: NotifyCenter;
let hostEvents: HostEvents;
let app: App;

/**
 * Starts the app. The page is the same in the window of Bravo and in a browser: whoever serves it has set
 * window.bravoHost, the address and the token of the API of the host, before this script. What the app needs
 * before its first part exists comes from the API.
 */
async function start() {

    const connection = window.bravoHost;
    if (!connection)
        throw new Error("The page was not given the address and the token of the host (window.bravoHost).");

    connectHost(connection);
    const [applicationDetails, userOptions] = await Promise.all([getApplication(), getOptions()]);

    application = applicationDetails;
    debug = new Debug(application.isDevelopmentBuild);
    host = new Host(connection.address, connection.token);

    // No policies and no telemetry settings yet: the host has no route for them
    optionsController = new OptionsController(<Options>userOptions, {});
    themeController = new ThemeController();
    logger = new Logger(optionsController.options.diagnosticLevel !== DiagnosticLevel.None);
    telemetry = new Telemetry();
    auth = new Auth();
    pbiDesktop = new PBIDesktop();
    notificationCenter = new NotifyCenter();
    hostEvents = new HostEvents(connection.address);

    app = new App(new AppVersion({
        version: application.version
    }));

    hostEvents.start();
}

start().catch(error => {
    // Nothing of the app exists, or not all of it: the page says so in place of the app
    console.error(error);
    showStartFailure(document.querySelector(".root"), error);
});

export { application, debug, host, optionsController, themeController, auth, telemetry, pbiDesktop, notificationCenter, logger, hostEvents, app };
