/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dic, _, __ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Dialog } from './dialog';
import { Menu, MenuItem } from './menu';
import { OptionsDialogAbout } from './options-dialog-about';
import { OptionsDialogFormatting } from './options-dialog-formatting';
import { OptionsDialogGeneral } from './options-dialog-general';
import { OptionsDialogTelemetry } from './options-dialog-telemetry';
import { OptionsDialogProxy } from './options-dialog-proxy';

export class OptionsDialog extends Dialog {

    menu: Menu;

    constructor() {

        super("options", document.body, i18n(strings.optionsDialogTitle), [
            { name: i18n(strings.dialogOK), action: "ok", className: "button-alt" }
        ]);
        
        let generalPane = new OptionsDialogGeneral();
        let formattingPane = new OptionsDialogFormatting();
        let telemetryPane = new OptionsDialogTelemetry();
        let proxyPane = new OptionsDialogProxy();
        let aboutPane = new OptionsDialogAbout();

        this.menu = new Menu("options-menu", this.body, <Dic<MenuItem>>{
            "general": {
                name: i18n(strings.optionsDialogGeneralMenu),  
                onRender: element => { generalPane.render(element) },
            },
            "formatting": {
                name: i18n(strings.optionsDialogFormattingMenu),  
                onRender: element => { formattingPane.render(element) },
            },
            "proxy": {
                name: i18n(strings.optionsDialogProxyMenu),  
                onRender: element => { proxyPane.render(element) },
            },  
            "telemetry": {
                name: i18n(strings.optionsDialogTelemetryMenu),  
                onRender: element => { telemetryPane.render(element) },
            },  
            "about": {
                name: i18n(strings.optionsDialogAboutMenu), 
                onRender: element => { aboutPane.render(element) },
            }
        });
    }

    show(selectedId?: string) {
        if (!selectedId)
            selectedId = Object.keys(this.menu.items)[0];
        this.menu.select(selectedId);

        return super.show();
    }

    destroy() {
        this.menu.destroy();
        this.menu = null;

        super.destroy();
    }
}