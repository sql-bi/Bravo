/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { _ } from '../helpers/utils';
import { host } from '../main';
import { HelpRes } from '../model/help';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { Dialog } from './dialog';

export class HelpDialog extends Dialog {
    help: HelpRes;

    constructor(help: HelpRes) {
        super("help", document.body, "", [], "", false, true);

        this.help = help;
        this.show();
    }

    show() {

        if (this.help.link) {
            _("header", this.element).insertAdjacentHTML("afterbegin", `
                <div class="ctrl open-docs icon-docs solo" title="${i18n(strings.documentation)}"></div>
            `);        

            _(".open-docs", this.element).addEventListener("click", e => {
                e.preventDefault();
                host.navigateTo(this.help.link);
            });
        }

        let html = `
            <div class="video-container">
                <iframe src="https://player.vimeo.com/video/${this.help.videoId}?pip=false" frameborder="0" allow="autoplay; fullscreen" allowfullscreen></iframe>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        return super.show();
    }


}