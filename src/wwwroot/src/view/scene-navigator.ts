/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { _ } from '../helpers/utils';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { Scene } from './scene';

export class NavigatorScene extends Scene {

    onBack: ()=>void;
    path: string[];
    toolbar: HTMLElement;

    constructor(id: string, container: HTMLElement, path: string[], onBack?: ()=>void) {
        super(id, container, path.length ? path[0] : null);
        this.path = path;
        this.onBack = onBack;
    }

    render() {
        super.render();

        let breadcrumbHtml = ``;
        this.path.forEach((value, index) => {
            if (index)
                breadcrumbHtml += `<div class="slash icon-right"></div>`;

            const single = (this.path.length == 1);
            const parent = (index == 0);
            const clickable = (index > 0 && index < this.path.length - 1);
            breadcrumbHtml += `<div class="item${parent ? " parent": (clickable ? " child" : "")}${single? " solo" : ""}"${parent ? ` title="${value}"` : ""} data-index="${index}">${value}</div>`;
        });

        let html = `
            <header>
                <h1 class="icon">
                    <div class="breadcrumb">${breadcrumbHtml}</div>
                </h1>
                <div class="toolbar"></div>
            </header>
            ${this.onBack ? `
                <div class="go-back ctrl icon-previous" title="${i18n(strings.goBackCtrlTitle)}"></div>
            ` : ""}
            <div class="scene-content"></div>
        `;
        this.element.insertAdjacentHTML("beforeend", html);
        this.toolbar = _("header .toolbar", this.element);
        this.body = _(".scene-content", this.element);

        if (this.onBack) {
            _(".go-back", this.element).addEventListener("click", e => {
                e.preventDefault();
                this.back(1);
            });
        }

        this.element.addLiveEventListener("click", ".breadcrumb .child", (e, element) => {
            e.preventDefault();
            this.back(Number(element.dataset.index));
        });
    }

    back(index: number) {
        if (this.onBack)
            this.onBack();

        if (index > 0) {
            for (let i = 0; i < index; i++)
                this.pop();
        }
    }

    update() {
        super.update();

        // Update title
        let titleElement = _("h1 .parent", this.element);
        titleElement.setAttribute("title", this.title);
        titleElement.innerText = this.title;
    }
}