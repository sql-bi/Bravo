/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { App } from '../controllers/app';
import { UpdateChannelType } from '../controllers/options';
import {  _, __ } from '../helpers/utils';
import { optionsController } from '../main';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { OptionsDialog } from './options-dialog';

export class OptionsDialogAbout {

    element: HTMLElement;
    dialog: OptionsDialog;

    constructor(dialog: OptionsDialog) {
        this.dialog = dialog;
    }

    render(element: HTMLElement) {
        this.element = element;
        
        const updateChannel = optionsController.options.updateChannel;

        let html = `
            <div class="cols">
                <div class="col coll">
                    <img src="images/bravo-shadows.svg">
                </div>
                <div class="col colr">
                    <h2>${i18n(strings.appName)}</h2>
                    <div class="version">
                        <select id="option-updatechannel">
                            ${Object.keys(UpdateChannelType).map(key => `
                                <option value="${(<any>UpdateChannelType)[key]}" ${(<any>UpdateChannelType)[key] == updateChannel ? "selected" : ""}>${i18n((<any>strings)[`updateChannel${key}`])}</option>
                            `).join("")}
                        </select> &nbsp;
                        ${i18n(strings.appVersion, { version: App.instance.currentVersion.toString()})}
                    </div>
                    <div class="update-status">
                        ${ App.instance.pendingVersion ? `
                            <div class="pending-update">${i18n(strings.appUpdateAvailable, { version: App.instance.pendingVersion.toString() })}</div>
                            <span class="link-button" data-download="${App.instance.pendingVersion.info.downloadUrl}">${i18n(strings.appUpdateDownload)}</span> &nbsp; 
                            <span class="link-button button-alt" data-href="${App.instance.pendingVersion.info.changelogUrl}">${i18n(strings.appUpdateChangelog)}</span>
                        ` : `
                            <div class="up-to-date">${i18n(strings.appUpToDate)}</div>
                        `}
                    </div>
                    
                    <div class="copyright">
                        ${new Date().getFullYear()} &copy; SQLBI Corp. ${i18n(strings.copyright)}<br>
                        ${i18n(strings.license)}
                    </div>

                </div>
            </div>
            <div class="sqlbi">
                <div><img src="images/sqlbi.svg"></div>
                <div>
                    ${i18n(strings.sqlbiPayoff)} &nbsp; 
                    <span class="link" data-href="https://www.sqlbi.com">www.sqlbi.com</span>
                </div>
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        _("#option-updatechannel").addEventListener("change", e => {
            let el = <HTMLSelectElement>e.currentTarget;
            optionsController.update("updateChannel", el.value);
        });
    }

    destroy() {
        this.dialog = null;
        this.element = null;
    }
}