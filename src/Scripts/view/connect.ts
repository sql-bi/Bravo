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
import { ConnectLocal } from './connect-local';
import { ConnectRemote } from './connect-remote';
import { ConnectFile } from './connect-file';

export interface ConnectResponse {
    action: string
    data: ConnectResponseData
}

export interface ConnectResponseData {
    doc: Doc
    lastOpenedMenu: string
}

export class Connect extends Dialog {

    menu: Menu;
    okButton: HTMLElement;
    openDocIds: string[];
    data: ConnectResponseData;

    constructor(openDocIds: string[]) {

        super("connect", document.body, i18n(strings.connectDialogTitle), [
            { name: i18n(strings.dialogOpen), action: "ok" },
            { name: i18n(strings.dialogCancel), action: "cancel", className: "button-alt" } 
        ]);

        this.openDocIds = openDocIds;

        this.okButton = _(".button[data-action=ok]", this.element);

        this.data = {
            doc: null,
            lastOpenedMenu: null
        };
        
        let connectLocal = new ConnectLocal(this);
        let connectRemote = new ConnectRemote(this);
        let connectFile = new ConnectFile(this);

        this.menu = new Menu("connect-menu", this.body, <Dic<MenuItem>>{
            "attach-pbi": {
                name: i18n(strings.connectDialogConnectPBIMenu),  
                onRender: element => { connectLocal.render(element) },
                onDestroy: ()=> { connectLocal.destroy() },
                onChange: ()=> { connectLocal.appear() },
            },
            "connect-pbi": {
                name: i18n(strings.connectDialogAttachPBIMenu), 
                onRender: element => { connectRemote.render(element) },
                onDestroy: ()=> { connectRemote.destroy() },
                onChange: ()=> { connectRemote.appear() },
            },
            "open-vpax": {    
                name: i18n(strings.connectDialogOpenVPXMenu),    
                onRender: element => { connectFile.render(element) },
                onDestroy: ()=> { connectFile.destroy() },
                onChange: ()=> { connectFile.appear() },
            }
        });

        this.listen();
    }

    listen() {

        this.menu.on("change", (item: string) => this.data.lastOpenedMenu = item);
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