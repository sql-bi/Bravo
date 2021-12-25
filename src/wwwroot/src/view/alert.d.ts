/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dialog } from './dialog';
export declare class Alert extends Dialog {
    constructor(id: string, title: string);
    show(message?: string): Promise<unknown>;
}
