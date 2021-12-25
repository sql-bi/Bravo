/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dispatchable } from '../helpers/dispatchable';
export interface OptionsData {
    theme?: string;
    formatter?: {
        zoom: number;
        spacing: number;
        lines: "long" | "short";
        separators: string;
    };
    model?: {
        showAllColumns: boolean;
        groupByTable: boolean;
        showUnrefOnly: boolean;
    };
    telemetry?: boolean;
}
export declare class Options extends Dispatchable {
    storageName: string;
    mode: string;
    data: OptionsData;
    constructor(mode: "host" | "browser", defaultData: OptionsData);
    listen(): void;
    load(defaultData: OptionsData): void;
    save(retry?: boolean): void;
    update(option: string, value: any): void;
}
export declare let options: Options;
