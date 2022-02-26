/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { OptionsStore } from '../controllers/options';
import { DateConfiguration } from '../model/dates';

export class ManageDatesScenePane {

    element: HTMLElement;
    config: OptionsStore<DateConfiguration>;

    get enabled(): boolean {
        return true;
    }

    constructor(config: OptionsStore<DateConfiguration>) {
        this.config = config;
    }

    render(element: HTMLElement) {
        this.element = element;

        //TODO isEnabled label
    }

    destroy() {
        this.config = null;
    }
}