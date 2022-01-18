/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dic, _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Dialog } from './dialog';
import { Menu, MenuItem } from './menu';
import { AboutOptionsDialog } from './options-dialog-about';
import { GeneralOptionsDialog } from './options-dialog-general';

export class OptionsDialog extends Dialog {

    menu: Menu;

    constructor() {

        super("options-dialog", document.body, "", [
            { name: i18n(strings.dialogOK), action: "ok" }
        ]);
        
        let generalDialog = new GeneralOptionsDialog(this);
        let aboutDialog = new AboutOptionsDialog(this);

        this.menu = new Menu("options-menu", this.body, <Dic<MenuItem>>{
            "general": {
                name: i18n(strings.optionsDialogGeneralMenu),  
                onRender: element => { generalDialog.render(element) },
                onDestroy: ()=> { generalDialog.destroy() }
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