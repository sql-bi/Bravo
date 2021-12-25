/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dispatchable } from "../helpers/dispatchable";
export declare class View extends Dispatchable {
    id: string;
    element: HTMLElement;
    body: HTMLElement;
    constructor(id: string, container: HTMLElement);
    show(): void;
    hide(): void;
}
