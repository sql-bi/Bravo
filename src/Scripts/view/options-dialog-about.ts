/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { AppVersion } from '../controllers/app';
import { UpdateChannelType } from '../controllers/options';
import { Loader } from '../helpers/loader';
import {  _, __ } from '../helpers/utils';
import { app, logger, optionsController, themeController } from '../main';
import { AppError } from '../model/exceptions';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';

export class OptionsDialogAbout {

    updateStatusDiv: HTMLElement;

    render(element: HTMLElement) {
     
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
                        <span class="display-version">${i18n(strings.appVersion, { version: app.currentVersion.info.build})}</span>
                        <span class="ctrl copy-version icon-copy" title="${i18n(strings.copy)}"></span>
                    </div>
                    <div class="update-status list"></div>
                    <div class="auto-check-option">
                        <label><input type="checkbox" ${optionsController.options.updateCheckEnabled ? " checked" : ""}>  &nbsp;${i18n(strings.optionCheckForUpdates)}</label>
                    </div>
                </div>
            </div>
            <div class="sqlbi">
                <div><img src="images/sqlbi.svg" class="light-logo"><img src="images/sqlbi-invert.svg" class="dark-logo"></div>
                <div>
                    ${i18n(strings.sqlbiPayoff)} &nbsp; 
                    <span class="link" href="https://www.sqlbi.com">www.sqlbi.com</span>
                    
                    <div class="copyright">
                        ${new Date().getFullYear()} &copy; SQLBI Corp. ${i18n(strings.copyright)}
                    </div>
                </div>
            </div>
        `;
        element.insertAdjacentHTML("beforeend", html);
        this.updateStatusDiv = _(".update-status", element);

        _("#option-updatechannel", element).addEventListener("change", e => {
            let el = <HTMLSelectElement>e.currentTarget;
            optionsController.update("updateChannel", el.value);
            this.checkForUpdates(true);
        });

        _(".copy-version", element).addEventListener("click", e => {
            e.preventDefault();
            navigator.clipboard.writeText(app.currentVersion.toString());
        });

        _(".auto-check-option input", element).addEventListener("change", e => {
            let el = <HTMLInputElement>e.currentTarget;
            optionsController.update("updateCheckEnabled", el.checked);
        });
        this.checkForUpdates();
    }

    checkForUpdates(force = false) {

        let statusHtml = (newVersion: AppVersion) => 
            newVersion ? `
                <div>
                    <div class="pending-update">${i18n(strings.appUpdateAvailable, { version: newVersion.toString() })}</div>
                    <span class="button" href="${newVersion.info.downloadUrl}" target="downloader">${i18n(strings.appUpdateDownload)}</span> &nbsp; 
                    <span class="link" href="${newVersion.info.changelogUrl}">${i18n(strings.appUpdateChangelog)}</span>
                </div>
            ` : `
                <div class="up-to-date">${i18n(strings.appUpToDate)}</div>
            `;

        if (!app.newVersion || force) {

            this.updateStatusDiv.innerHTML = Loader.html(false, false, 5);
            
            app.checkForUpdates()
                .then(newVersion => {
                    this.updateStatusDiv.innerHTML = statusHtml(newVersion);
                })
                .catch((error: AppError) => {

                    this.updateStatusDiv.innerHTML = `<div class="notice">${i18n(strings.errorCheckForUpdates)}</div>`;
 
                    try { logger.logError(error); } catch(ignore) {}
                });
        } else {
            this.updateStatusDiv.innerHTML = statusHtml(app.newVersion);
        }
    }
}