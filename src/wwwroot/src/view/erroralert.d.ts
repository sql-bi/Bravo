/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dialog } from './dialog';
export declare class ErrorAlert extends Dialog {
    constructor();
    show(message?: string): Promise<unknown>;
}
