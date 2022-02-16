/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { AppVersion } from '../controllers/app';
import { UpdateChannelType } from '../controllers/options';
import { Loader } from '../helpers/loader';
import {  _, __ } from '../helpers/utils';
import { app, optionsController } from '../main';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { OptionsDialog } from './options-dialog';

export class OptionsDialogAbout {

    element: HTMLElement;
    dialog: OptionsDialog;
    updateStatusDiv: HTMLElement;

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
                        ${i18n(strings.appVersion, { version: app.currentVersion.toString()})}
                    </div>
                    <div class="update-status list"></div>
                    
                    

                </div>
            </div>
            <div class="sqlbi">
                <div><img src="images/sqlbi.svg"></div>
                <div>
                    ${i18n(strings.sqlbiPayoff)} &nbsp; 
                    <span class="link" href="https://www.sqlbi.com">www.sqlbi.com</span>
                    
                    <div class="copyright">
                        ${new Date().getFullYear()} &copy; SQLBI Corp. ${i18n(strings.copyright)}
                    </div>
                </div>
            </div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);
        this.updateStatusDiv = _(".update-status", this.element);

        _("#option-updatechannel").addEventListener("change", e => {
            let el = <HTMLSelectElement>e.currentTarget;
            optionsController.update("updateChannel", el.value);
            this.checkForUpdates(true);
        });
        this.checkForUpdates();
    }

    checkForUpdates(force = false) {

        let statusHtml = (pendingVersion: AppVersion) => 
            pendingVersion ? `
                <div>
                    <div class="pending-update">${i18n(strings.appUpdateAvailable, { version: pendingVersion.toString() })}</div>
                    <span class="button" href="${pendingVersion.info.downloadUrl}" target="downloader">${i18n(strings.appUpdateDownload)}</span> &nbsp; 
                    <span class="button button-alt" href="${pendingVersion.info.changelogUrl}">${i18n(strings.appUpdateChangelog)}</span>
                </div>
            ` : `
                <div class="up-to-date">${i18n(strings.appUpToDate)}</div>
            `;

        if (!app.pendingVersion || force) {

            this.updateStatusDiv.innerHTML = Loader.html(false, false, 5);
            
            app.checkForUpdates()
                .then(pendingVersion => {
                    this.updateStatusDiv.innerHTML = statusHtml(pendingVersion);
                });
        } else {
            this.updateStatusDiv.innerHTML = statusHtml(app.pendingVersion);
        }
    }

    destroy() {
        this.dialog = null;
        this.element = null;
    }
}