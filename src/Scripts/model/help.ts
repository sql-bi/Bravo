/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { PageType } from '../controllers/page';
import { Dic } from '../helpers/utils';
import { strings } from './strings';

export interface HelpRes {
    title: strings;
    link: string;
    videoId: string;
}

export const help: Dic<HelpRes> = {
    ui: {
        title: strings.helpUserInterface,
        videoId: "763673584",
        link: "https://docs.sqlbi.com/bravo/user-interface"
    },
    connect: {
        title: strings.helpConnectVideo,
        videoId: "763673603",
        link: "https://docs.sqlbi.com/bravo/connect"
    },
    AnalyzeModel: {
        title: strings.AnalyzeModel,
        videoId: "763673832",
        link: "https://docs.sqlbi.com/bravo/analyze-model"
    },
    DaxFormatter: {
        title: strings.DaxFormatter,
        videoId: "763677100",
        link: "https://docs.sqlbi.com/bravo/format-dax"
    },
    ManageDates: {
        title: strings.ManageDates,
        videoId: "763679068",
        link: "https://docs.sqlbi.com/bravo/manage-dates"
    },
    templates: {
        title: strings.helpTemplates,
        videoId: "763684375",
        link: "https://docs.sqlbi.com/bravo/features/manage-dates/customize-date-template"
    },
    ExportData: {
        title: strings.ExportData,
        videoId: "763681111",
        link: "https://docs.sqlbi.com/bravo/export-data"
    },
    options: {
        title: strings.helpOptions,
        videoId: "763683383",
        link: "https://docs.sqlbi.com/bravo/configuration/options"
    }
}
