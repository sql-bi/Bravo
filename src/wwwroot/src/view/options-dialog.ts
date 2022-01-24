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

export interface OptionStruct {
    option?: string,
    icon?: string
    name: string
    description?: string
    type: OptionType
    values?: string[][]
    value?: any,
    onChange?: (e: Event) => void,
    onClick?: (e: Event) => void,
    custom?: ()=> string
}

export enum OptionType {
    button,
    select,
    switch,
    description,
    custom
}
export class OptionsDialog extends Dialog {

    menu: Menu;

    constructor() {

        super("options", document.body, i18n(strings.optionsDialogTitle), [
            { name: i18n(strings.dialogOK), action: "ok", className: "button-alt" }
        ]);
        
        let generalDialog = new OptionsDialogGeneral(this);
        let formattingDialog = new OptionsDialogFormatting(this);
        let telemetryDialog = new OptionsDialogTelemetry(this);
        let aboutDialog = new OptionsDialogAbout(this);

        this.menu = new Menu("options-menu", this.body, <Dic<MenuItem>>{
            "general": {
                name: i18n(strings.optionsDialogGeneralMenu),  
                onRender: element => { generalDialog.render(element) },
                onDestroy: ()=> { generalDialog.destroy() }
            },
            "formatting": {
                name: i18n(strings.optionsDialogFormattingMenu),  
                onRender: element => { formattingDialog.render(element) },
                onDestroy: ()=> { formattingDialog.destroy() }
            },
            "telemetry": {
                name: i18n(strings.optionsDialogTelemetryMenu),  
                onRender: element => { telemetryDialog.render(element) },
                onDestroy: ()=> { telemetryDialog.destroy() }
            },     
            "about": {
                name: i18n(strings.optionsDialogAboutMenu), 
                onRender: element => { aboutDialog.render(element) },
                onDestroy: ()=> { aboutDialog.destroy() },
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