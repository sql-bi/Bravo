/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Dic } from '../helpers/utils';
import { VpaxModel } from './model';
export interface DocSourceData {
    file?: File;
    reportId?: string;
}
export declare class Doc {
    name: string;
    type: string;
    sourceData: DocSourceData;
    model: VpaxModel;
    formattedMeasures: Dic<string>;
    lastRefresh: number;
    isDirty: boolean;
    loaded: boolean;
    constructor(name: string, type: string, sourceData: DocSourceData);
    refresh(): Promise<void>;
}
