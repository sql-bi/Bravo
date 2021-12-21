/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

// All the strings used in the app
const strings = {
    appUrl: "https://bravo.bi",
    appGithubUrl: "https://github.com/sql-bi/bravo",
    daxFormatterUrl: "https://www.daxformatter.com",
    appName: "Bravo for Power BI",
    menuCtrlTitle: "Collapse/Expand menu",
    addCtrlTitle: "Open",
    daxFormatterName: "Format DAX",
    daxFormatterTitle: "Format DAX",
    analyzeModelName: "Analyze Model",
    manageDatesName: "Manage Dates",
    manageDatesTitle: "Manage Dates",
    exportDataName: "Export Data",
    exportDataTitle: "Export Data",
    bestPracticesName: "Best Practices",
    bestPracticesTitle: "Best Practices",
    settingsCtrlTitle: "Settings",
    helpCtrlTitle: "Help",
    filterUnrefCtrlTitle: "Show Unreferenced Columns only",
    groupByTableCtrlTitle: "Group by Table",
    expandAllCtrlTitle: "Expand all",
    collapseAllCtrlTitle: "Collapse all",
    saveVpaxCtrlTile: "Save as VPAX",
    refreshCtrlTitle: "Refresh",

    defaultTabName: "Untitled",
    welcomeTitle: "Welcome to Bravo for Power BI",
    welcomeText: "Bravo is a powerful toolkit for your Power BI datasets that you can use to quick manage models, create date tables, export data, and more. Start by opening a dataset:",
    welcomeHelpTitle: "How to use Bravo?",
    welcomeHelpText: "Watch the videos below to learn how to use Bravo:",
    helpConnectVideo: "Connect Bravo to your Data",
    openSourcePayoff: `Bravo for Power BI is an open-source tool developed and mantained by SQLBI and the Github community. Join us at <a href="https://github.com/sql-bi/bravo" target="_blank" class="ext-link">github.com/sql-bi/bravo</a>`,
    quickActionAttachPBITitle: "Attach to Power BI Desktop",
    quickActionConnectPBITitle: "Connect to Power BI Service",
    quickActionOpenVPXTitle: "Open a Vertipaq Analyzer file",
    connectDialogTitle: "Open",
    connectDialogAttachPBILabel: "Active Reports on Power BI Desktop",
    connectDialogConnectPBILabel: "Datasets on powerbi.com",
    connectDialogOpenVPXLabel: "VPAX Files",
    connectDragFile: "Drag a VPAX file here or",
    connectBrowse: "Browse",
    connectNoReports: "No active reports found.<br>Open a report with Power BI Desktop and wait for it to appear here.",
    dialogOK: "OK",
    dialogCancel: "Cancel",
    dialogOpen: "Open",
    confirmTabCloseMessage: "It seems you didn't save the changes to this document.<br>Are you sure to close it?",
    
  
    analyzeModelSummary: (size, columnsCount, unreferecedCount) => `Your dataset is <strong>${size}</strong> large and contains <strong>${columnsCount}</strong> columns, <span class="text-highlight"><strong>${unreferecedCount}</strong> of which ${unreferecedCount == 1 ? "is" : "are"} not referenced.</span>`,
    analyzeModelTableColColumn: "Column",
    analyzeModelTableColTable: "Table",
    analyzeModelTableColEntity: "Table \\ Column",
    analyzeModelTableColSize: "Size",
    analyzeModelTableColWeight: "Weight",
    analyzeModelTableColCardinality: "Cardinality",
    analyzeModelTableColCardinalityTooltip: "Number of unique elements",
    analyzeModelTableColRows: "Rows",
    otherColumnsRowName: "Smaller columns...",
    aggregatedTableName: "Multiple tables",

    columnWarningExplanation: `Unreferenced columns can generally be removed from the model to optimize performance. Before removing them, make sure you are not using these columns in any reports, which Bravo cannot determine.`,
    columnWarningTooltip: "This column is not referenced in your model.",
    daxFormatterSummary: count => `Your report contains <strong>${count} measures</strong> that can be formatted.`,
    daxFormatterTableColMeasure: "Measure",
    daxFormatterTableColTable: "Table",
    daxFormatterTableSelected: count => `${count} Selected`,
    daxFormatterOriginalCode: "Current",
    daxFormatterFormattedCode: "Formatted (Preview)",
    daxFormatterFormat: "Format",
    daxFormatterAgreement: `To format DAX, Bravo sends your measures to the DAX Formatter service.`,
    daxFormatterPreviewDesc: "To generate a preview, Bravo needs to send this measure to DAX Formatter.",
    daxFormatterPreviewAllOption: "Generate ",
    daxFormatterPreviewButton: "Generate Preview",
    dataUsageLink: "How your data is used?", 
    dataUsageTitle: "How your data is used?",
    dataUsageMessage: `
        To format your code, Bravo sends the measures to DAX Formatter, a service managed by SQLBI, over a secure connection.</>

        <p><strong>The service does not store your data anywhere.</strong></p>
        <p>Some information such as the most used DAX functions, a complexity index, and average query length can be saved for statistical purposes.</p>
        <p><a href="https://www.daxformatter.com" target="_blank">www.daxformatter.com</a></p>
        `,
    signIn: "Sign In",
    searchPlaceholder: "Search",
    searchColumnPlaceholder: "Search Column",
    previewPlaceholder: "Select a measure to see the preview...",
    errorTitle: "Whoops...",
    errorGeneric: "Error",
    errorUnspecified: "Unspecified error.",
    errorNotConnected: "You're not connected to Power BI - please sign in to proceed.",
    errorDatasetListing: "Unable to retrieve the list of datasets of Power BI Service.",
};