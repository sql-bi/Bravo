/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dispatchable } from '../helpers/dispatchable';
export declare class Theme extends Dispatchable {
    device: string;
    current: string;
    constructor();
    change(theme: string): void;
    apply(theme?: string): void;
    get isDark(): boolean;
    get isLight(): boolean;
}
export declare let theme: Theme;
