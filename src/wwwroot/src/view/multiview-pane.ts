/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { View } from './view';
import { Menu, MenuItem } from './menu';
import { Dic, _, __ } from '../helpers/utils';
import Split, { SplitObject } from "split.js";

export enum MultiViewPaneMode {
    Tabs = 0,
    VerticalSplit = 1,
    HorizontalSplit = 2,
}

export interface ViewPane extends MenuItem {
    
}


export class MultiViewPane extends View {

    changeModeControl: HTMLElement;
    _mode: MultiViewPaneMode;
    panes: Dic<ViewPane>;
    menu: Menu;
    split: SplitObject;

    set mode(mode: MultiViewPaneMode) {

        Object.keys(MultiViewPaneMode).forEach(m => {
            if (!isNaN(Number(m))) {
                const textMode = MultiViewPaneMode[Number(m)];
                if (Number(m) == mode) {
                    this.element.classList.add(`layout-${textMode}`);
                    this.changeModeControl.classList.add(`icon-${textMode}`)
                } else {
                    this.element.classList.remove(`layout-${textMode}`);
                    this.changeModeControl.classList.remove(`icon-${textMode}`)
                }
            }
        });

        this._mode = mode;
        this.updateLayout();

        this.trigger("mode.change", mode);
    }

    get mode(): MultiViewPaneMode {
        return this._mode;
    }

    constructor(id: string, container: HTMLElement, panes: Dic<ViewPane>, mode: MultiViewPaneMode = MultiViewPaneMode.Tabs) {
        super(id, container);
        this.element.classList.add("multiview-pane");

        this.menu = new Menu("multiview-menu", this.element, panes, false);
         _("#multiview-menu .menu", this.element).insertAdjacentHTML("beforeend", `
            <div class="toolbar">
                <div class="change-mode ctrl solo icon-${MultiViewPaneMode[mode]}"></div>
            </div>
        `);
        this.changeModeControl = _(".change-mode", this.element);

        this.panes = panes;
        this.mode = mode;

        this.listen();
        this.updateSplit();
    }
    
    listen() {
        this.changeModeControl.addEventListener("click", e => {
            e.preventDefault();

            let modes = Object.keys(MultiViewPaneMode).filter(key => !isNaN(Number(key)));
            this.mode = (this.mode < modes.length - 1 ? this.mode + 1 : 0);
        });
    }

    updateLayout() {
        
        if (this.mode == MultiViewPaneMode.Tabs) {
            this.menu.select(this.menu.currentItems[0]);
        } else {
            this.menu.select(Object.keys(this.panes));
        }
        this.updateSplit();
    }

    updateSplit() {
        this.destroySplit();
        if (this.mode != MultiViewPaneMode.Tabs) {
            this.split = Split(Object.keys(this.panes).map(id => `#${this.element.id} #body-${id}`), {
                //sizes: [50, 50], 
                //minSize: [400, 0],
                gutterSize: 10,
                direction: (this.mode == MultiViewPaneMode.HorizontalSplit ? "vertical" : "horizontal"),
                cursor: (this.mode == MultiViewPaneMode.HorizontalSplit ? "ns-resize" : "ew-resize" ),
                onDragStart: sizes => {
                    this.element.classList.add("dragging");
                },
                onDragEnd: sizes => {
                    this.trigger("size.change", sizes);
                    this.element.classList.remove("dragging");
                }
            });
        }
    }

    destroySplit() {
        if (this.split) {
            this.split.destroy(false, false);
            this.split = null;
        }
    }

    destroy() {

        this.menu.destroy();
        this.menu = null;

        this.destroySplit();

        super.destroy();
    }


}