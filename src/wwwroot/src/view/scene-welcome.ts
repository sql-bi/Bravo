/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { _ } from '../helpers/utils';
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
                name: strings.helpConnectVideo,
                videoId: ""
            },
            {
                name: strings.AnalyzeModel,
                videoId: ""
            },
            {
                name: strings.DaxFormatter,
                videoId: ""
            },
            {
                name: strings.ManageDates,
                videoId: ""
            },
            {
                name: strings.ExportData,
                videoId: ""
            },
            {
                name: strings.BestPractices,
                videoId: ""
            }
        ];

        let html = `
            <div class="cols">
                <div class="col coll">
                    <header>
                        <h1>${strings.welcomeTitle}</h1>
                    </header> 
                    <p>${strings.welcomeText}</p>
    
                    <div class="quick-actions">
                        <div class="ctrl quick-attach-pbi">
                            <img src="images/attach-pbi.svg">
                            <span class="name">${strings.quickActionAttachPBITitle}</span>
                        </div>
                        <div class="ctrl quick-connect-pbi">
                            <img src="images/connect-pbi.svg">
                            <span class="name">${strings.quickActionConnectPBITitle}</span>
                        </div>
                        <div class="ctrl quick-open-vpx">
                            <img src="images/vertipaq.svg">
                            <span class="name">${strings.quickActionOpenVPXTitle}</span>
                        </div>
                    </div>
                </div>
                <div class="col colr">
                    <div class="sep"></div>
                    <div class="help-content">
                        <header>
                            <h2>${strings.welcomeHelpTitle}</h2>
                        </header>
                        
                        <p>${strings.welcomeHelpText}</p>

                        <div class="videos">
                            <ul>
                                ${helpVideos.map(video => `
                                    <li><span class="help-video ctrl icon-video" data-video="${video.videoId}">${video.name}</span></li>
                                `).join("")}
                            </ul>
                        </div>

                        <p class="note">${strings.openSourcePayoff}</p>
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

        _(".quick-open-vpx", this.element).addEventListener("click", e => {
            e.preventDefault();
            this.trigger("quickAction", "open-vpx");
        });
    } 
}