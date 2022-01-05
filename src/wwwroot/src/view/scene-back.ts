/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { _ } from '../helpers/utils';
import { strings } from '../model/strings';
import { Scene } from './scene';

export class BackableScene extends Scene {
    rendered: boolean;
    title: string;
    onBack: (()=>void) | boolean;

    constructor(id: string, container: HTMLElement, title?: string, onBack?: (()=>void) | boolean) {
        super(id, container, title);
        this.onBack = onBack;
    }

    render() {
        super.render();

        if (this.onBack) {
            let html = `
                <div class="go-back ctrl icon-previous" title="${strings.goBackCtrlTitle}"></div>
            `;

            this.element.insertAdjacentHTML("beforeend", html); 

            _(".go-back", this.element).addEventListener("click", e => {
                e.preventDefault();
                if (typeof this.onBack === "function")
                    this.onBack();
                this.trigger("pop");
            });
        }
    }

}