/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { View } from './view';
export interface DialogButton {
    name: string;
    action: string;
    className?: string;
}
export declare class Dialog extends View {
    body: HTMLElement;
    data: {};
    constructor(id: string, container: HTMLElement, title: string, buttons: DialogButton[]);
    show(): Promise<unknown>;
    hide(): void;
    destroy(): void;
}
