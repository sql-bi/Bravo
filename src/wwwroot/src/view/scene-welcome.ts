/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { Scene } from '../view/scene';

export class WelcomeScene extends Scene {

    constructor(id: string, container: HTMLElement) {
        super(id, container);
        this.element.classList.add("welcome");
        this.render();
    }

    render() {
        super.render();
        
        let helpVideos = [
            {
                name: i18n(strings.helpConnectVideo),
                videoId: ""
            },
            {
                name: i18n(strings.AnalyzeModel),
                videoId: ""
            },
            {
                name: i18n(strings.DaxFormatter),
                videoId: ""
            },
            {
                name: i18n(strings.ManageDates),
                videoId: ""
            },
            {
                name: i18n(strings.ExportData),
                videoId: ""
            },
            /*{
                name: i18n(strings.BestPractices),
                videoId: ""
            }*/
        ];

        let html = `
            <div class="cols">
                <div class="col coll">
                    <header>
                        <h1>${i18n(strings.welcomeTitle)}</h1>
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
                                ${helpVideos.map(video => `
                                    <li><span class="help-video ctrl icon-video" data-video="${video.videoId}">${video.name}</span></li>
                                `).join("")}
                            </ul>
                        </div>

                        <p class="note">${i18n(strings.openSourcePayoff)} <span class="link" data-href="https://github.com/sql-bi/bravo">github.com/sql-bi/bravo</span></p>
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
    } 
}