/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _, __ } from '../helpers/utils';
import { help } from '../model/help';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from '../view/scene';
import { HelpDialog } from './help-dialog';

export class WelcomeScene extends Scene {

    constructor(id: string, container: HTMLElement) {
        super(id, container);
        this.element.classList.add("welcome");
        this.render();
    }

    render() {
        super.render();

        let html = `
            <div class="cols">
                <div class="col coll">
                    <header>
                        <h1>${i18n(strings.appName)}</h1>
                    </header> 
                    <p>${i18n(strings.welcomeText)}</p>
    
                    <div class="quick-actions">
                        <div class="ctrl quick-attach-pbi">
                            <img src="images/attach-pbi.svg">
                            <span class="name">${i18n(strings.quickActionAttachPBITitle)}</span>
                        </div>
                        <div class="ctrl quick-connect-pbi">
                            <img src="images/connect-pbi.svg">
                            <span class="name">${i18n(strings.quickActionConnectPBITitle)}</span>
                        </div>
                        <div class="ctrl quick-open-vpax">
                            <img src="images/vertipaq.svg">
                            <span class="name">${i18n(strings.quickActionOpenVPXTitle)}</span>
                        </div>
                    </div>
                </div>
                <div class="col colr">
                    <div class="sep"></div>
                    <div class="help-content">
                        <header>
                            <h2>${i18n(strings.welcomeHelpTitle)}</h2>
                        </header>
                        
                        <p>${i18n(strings.welcomeHelpText)}</p>

                        <div class="videos">
                            <ul>
                                ${Object.keys(help).map(id => `
                                    <li><span class="help-video ctrl icon-video" data-id="${id}">${i18n(help[id].title)}</span></li>
                                `).join("")}
                            </ul>
                        </div>

                        <p><span class="link" href="https://docs.sqlbi.com/bravo">${i18n(strings.documentation)}</span></p>
                        
                        <p class="note">${i18n(strings.openSourcePayoff, { appName: i18n(strings.appName) })} <span class="link" href="https://github.com/sql-bi/bravo">github.com/sql-bi/bravo</span></p>
                    </div>
                </div>
            </div>
        `;

        this.element.insertAdjacentHTML("beforeend", html);

        this.listen();
    }

    listen() {
        _(".quick-attach-pbi", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("quickAction", "attach-pbi");
        });

        _(".quick-connect-pbi", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("quickAction", "connect-pbi");
        });

        _(".quick-open-vpax", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("quickAction", "open-vpax");
        });

        __(".help-video", this.element).forEach((div: HTMLElement) => {
            div.addEventListener("click", e => {
                e.preventDefault();

                new HelpDialog(help[div.dataset.id]);
            });
        });
    } 
}