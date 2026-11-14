/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { describe, expect, it } from 'vitest';
import {
    TabularDatabaseFeature,
    TabularDatabaseFeatureUnsupportedReason,
    TabularTableFeature,
    TabularTableFeatureUnsupportedReason
} from '../../model/tabular';

// The members of a numeric enum, without the names that TypeScript adds for each value
function members(enumObject: object) {
    return Object.fromEntries(Object.entries(enumObject).filter(([name]) => isNaN(Number(name))));
}

// The flags travel with the host. Several were written as a shift by more than 31 bits, which JavaScript takes
// modulo 32: these are the values that the user interface has always had.
describe("the flags of the features", () => {

    it("of a table keep their values", () => {
        expect(members(TabularTableFeature)).toEqual({
            None: 0,
            ExportData: 65536,
            All: 65536,
        });
    });

    it("of a table that is not supported keep their values", () => {
        expect(members(TabularTableFeatureUnsupportedReason)).toEqual({
            None: 0,
            ExportDataNoColumns: 65536,
            ExportDataNotQueryable: 131072,
        });
    });

    it("of a database keep their values", () => {
        expect(members(TabularDatabaseFeature)).toEqual({
            None: 0,
            AnalyzeModelPage: 16,
            AnalyzeModelSynchronize: 32,
            AnalyzeModelExportVpax: 64,
            AnalyzeModelDeobfuscateVpax: 128,
            AnalyzeModelAll: 112,
            FormatDaxPage: 256,
            FormatDaxSynchronize: 512,
            FormatDaxUpdateModel: 1024,
            FormatDaxDeobfuscateVpax: 2048,
            FormatDaxAll: 1792,
            ManageDatesPage: 4096,
            ManageDatesSynchronize: 8192,
            ManageDatesUpdateModel: 16384,
            ManageDatesAll: 28672,
            ExportDataPage: 65536,
            ExportDataSynchronize: 131072,
            ExportDataAll: 196608,
            AllUpdateModel: 17408,
            AllSynchronize: 139808,
            All: 227184,
        });
    });

    it("of a database that is not supported keep their values", () => {
        expect(members(TabularDatabaseFeatureUnsupportedReason)).toEqual({
            None: 0,
            ReadOnly: 2,
            MetadataOnly: 4,
            XmlaEndpointNotSupported: 8,
            ManageDatesAutoDateTimeEnabled: 4096,
            ManageDatesPBIDesktopModelOnly: 8192,
            ManageDatesEmptyTableCollection: 16384,
        });
    });
});
