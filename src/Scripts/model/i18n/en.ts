/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Locale } from '../i18n';
import { strings as _ } from '../strings';

const locale: Locale = {

    locale: "en",
    name: "English",

    strings: {
        [_.addCtrlTitle]: "Open",
        [_.aggregatedTableName]: "Multiple tables",
        [_.AnalyzeModel]: "Analyze Model",
        [_.analyzeModelSummary]: `Your dataset is <strong>{size:bytes}</strong> large and contains <strong>{count}</strong> column{{s}}`,
        [_.analyzeModelSummary2]: `, <span class="text-highlight"><strong>{count}</strong> of which {{are|is}} not referenced within the model.</span>`,
        [_.appName]: "Bravo for Power BI", //DO NOT TRANSLATE
        [_.appUpdateAvailable]: "New version available: {version}",
        [_.appUpdateChangelog]: "Changelog",
        [_.appUpdateDownload]: "Download",
        [_.appUpdateViewDetails]: "View details",
        [_.appUpToDate]: "Bravo is up to date",
        [_.appVersion]: "Version {version}",
        [_.backupReminder]: "Before proceeding, remember to backup your report - <b>some changes may not be undoable</b>.",
        [_.BestPractices]: "Best Practices",
        [_.canceled]: "Canceled",
        [_.changeStatusAdded]: "A",
        [_.changeStatusAddedTitle]: "Added",
        [_.changeStatusDeleted]: "D",
        [_.changeStatusDeletedTitle]: "Deleted",
        [_.changeStatusModified]: "M",
        [_.changeStatusModifiedTitle]: "Modified",
        [_.clearCtrlTitle]: "Clear",
        [_.closeCtrlTitle]: "Close",
        [_.closeOtherTabs]: "Close others",
        [_.closeTab]: "Close",
        [_.collapseAllCtrlTitle]: "Collapse all",
        [_.columnExportedCompleted]: "This table was exported successfully.",
        [_.columnExportedFailed]: "This table was not exported due to an unspecified error.",
        [_.columnExportedTruncated]: "This table was truncated because the number of rows has exceeded the maximum allowed.",
        [_.columnMeasureFormatted]: "This measure is already formatted.",
        [_.columnMeasureNotFormattedTooltip]: "This measure is not formatted.",
        [_.columnMeasureWithError]: "This measure contains errors.",
        [_.columnUnreferencedExplanation]: `<span class="text-highlight">Unreferenced columns</span> can generally be removed from the model to optimize performance. Before removing them, make sure you are not using these columns in any reports, which Bravo cannot determine.`,
        [_.columnUnreferencedTooltip]: "This column is not referenced in your model.",
        [_.confirmTabCloseMessage]: "It seems you didn't save the changes to this document.<br>Are you sure to close it?",
        [_.connectBrowse]: "Browse",
        [_.connectDatasetsTableEndorsementCol]: "Endorsement",
        [_.connectDatasetsTableNameCol]: "Name",
        [_.connectDatasetsTableOwnerCol]: "Owner",
        [_.connectDatasetsTableWorkspaceCol]: "Workspace",
        [_.connectDialogAttachPBIMenu]: "Datasets on powerbi.com",
        [_.connectDialogConnectPBIMenu]: "Active Reports on Power BI Desktop",
        [_.connectDialogOpenVPXMenu]: "VPAX Files",
        [_.connectDialogTitle]: "Open",
        [_.connectDragFile]: "Drag a VPAX file here<br>or browse your computer",
        [_.connectNoReports]: "No active reports found.<br>Open a report with Power BI Desktop and wait for it to appear here.",
        [_.copiedErrorDetails]: "Copied!",
        [_.copy]: "Copy",
        [_.copyErrorDetails]: "Copy Error",
        [_.copyFormulaCtrlTitle]: "Copy formatted measure",
        [_.copyMessage]: "Copy Message",
        [_.copyright]: "All rights are reserved.",
        [_.createIssue]: "Report Issue",
        [_.cut]: "Cut",
        [_.dataUsageLink]: "How your data is used?", 
        [_.dataUsageMessage]: `To format your code, Bravo sends the measures of this dataset to DAX Formatter, a service managed by SQLBI, over a secure connection.<p><strong>The service does not store your data anywhere.</strong></p><p>Some information such as the most used DAX functions, a complexity index, and average query length can be saved for statistical purposes.</p>`,
        [_.dataUsageTitle]: "How your data is used?",
        [_.DaxFormatter]: "Format DAX",
        [_.daxFormatterAgreement]: "To format DAX, Bravo sends your measures to the DAX Formatter service.",
        [_.daxFormatterAnalyzeConfirm]: "To perform an analysis, Bravo must send all measures to the DAX Formatter service. Are you sure to continue?",
        [_.daxFormatterAutoPreviewOption]: "Automatic preview",
        [_.daxFormatterFormat]: "Format Selected",
        [_.daxFormatterFormatDisabled]: "Format (Unsupported)",
        [_.daxFormatterFormattedCode]: "Formatted (Preview)",
        [_.daxFormatterOriginalCode]: "Current",
        [_.daxFormatterPreviewAllButton]: "Preview All Measures",
        [_.daxFormatterPreviewButton]: "Preview",
        [_.daxFormatterPreviewDesc]: "To generate a preview, Bravo needs to send this measure to the DAX Formatter service.",
        [_.daxFormatterSuccessSceneMessage]: "Congratulations, <strong>{count} measure{{s}}</strong> {{were|was}} formatted successfully.",
        [_.daxFormatterSummary]: `Your dataset contains {count} measure{{s}}: <span class="text-error"><strong>{errors:number}</strong> with errors</strong></span>, <span class="text-highlight"><strong>{formattable:number}</strong> to format</span>, <strong>{analyzable:number}</strong> to analyze (<span class="link manual-analyze">analyze now</span>).`,
        [_.daxFormatterSummaryNoAnalysis]: `Your dataset contains <strong>{count}</strong> measure{{s}}: <span class="text-error"><strong>{errors:number}</strong> with errors</strong></span> and <span class="text-highlight"><strong>{formattable:number}</strong> to format.</span>`,
        [_.defaultTabName]: "Untitled",
        [_.dialogCancel]: "Cancel",
        [_.dialogOK]: "OK",
        [_.dialogOpen]: "Open",
        [_.docLimited]: "Limited",
        [_.docLimitedTooltip]: "Not all the features are available for this document.",
        [_.doneCtrlTitle]: "Done",
        [_.emailAddress]: "Email address",
        [_.emailAddressPlaceholder]: "Enter your email address",
        [_.error]: "Error",
        [_.errorAborted]: "Operation aborted.",
        [_.errorAnalysisServicesConnectionFailed]: "A connection problem arises between the server and Bravo.",
        [_.errorCheckForUpdates]: "Unable to check for updates - the remote server is unreachable.",
        [_.errorConnectionUnsupported]: "The connection to the requested resource is not supported.",
        [_.errorDatasetConnectionUnknown]: "Unspecified connection.",
        [_.errorDatasetsEmptyListing]: "No open reports available.",
        [_.errorDatasetsListing]: "Unable to retrieve the list of datasets of Power BI Service.",
        [_.errorExportDataFileError]: "Something wrong happened while exporting data. Please try again.",
        [_.errorManageDateTemplateError]: "An exception occurred while executing the DAX template engine.",
        [_.errorNetworkError]: "You are not connected to the Internet.",
        [_.errorNone]: "Unspecified error.",
        [_.errorNotAuthorized]: "You are not authorized to view the specified resource.",
        [_.errorNotConnected]: "You are not connected to Power BI - please sign in to proceed.",
        [_.errorNotFound]: "Unable to connect to the specified resource.",
        [_.errorReportConnectionUnknown]: "Invalid connection.",
        [_.errorReportConnectionUnsupportedAnalysisServecesCompatibilityMode]: "Power BI Desktop Analysis Services instance compatibility mode is not PowerBI.",
        [_.errorReportConnectionUnsupportedAnalysisServecesConnectionNotFound]: "Power BI Desktop Analysis Services TCP connection not found.",
        [_.errorReportConnectionUnsupportedAnalysisServecesProcessNotFound]: "Power BI Desktop Analysis Services instance process not found.", 
        [_.errorReportConnectionUnsupportedConnectionException]: "An exception was raised when connecting to the Power BI Desktop Analysis Services instance.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionIsEmpty]: "The Power BI Desktop Analysis Services instance does not contain any databases. Try to connect to the report using the Bravo icon in the External Tools of Power BI Desktop.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionUnexpectedCount]: "Power BI Desktop Analysis Services instance contains an unexpected number of databases (> 1) while we expect zero or one.",
        [_.errorReportConnectionUnsupportedProcessNotYetReady]: "Power BI Desktop process is opening or the Analysis Services instance is not yet ready.", 
        [_.errorReportsEmptyListing]: "No unopened reports available.",
        [_.errorRetry]: "Retry",
        [_.errorSignInMsalExceptionOccurred]: "Unexpected error in the sign-in request.",
        [_.errorSignInMsalTimeoutExpired]: "The sign-in request was canceled because the timeout period expired before the operation was completed.",
        [_.errorTimeout]: "Request timeout.",
        [_.errorTitle]: "Whoops...",
        [_.errorTOMDatabaseDatabaseNotFound]: "The database does not exist in the collection or the user does not have administrator rights to access it.",
        [_.errorTOMDatabaseUpdateConflictMeasure]: "The requested update conflicts with the current state of the target resource.",
        [_.errorTOMDatabaseUpdateErrorMeasure]: "The requested update failed because one or more measures contain errors.", 
        [_.errorTOMDatabaseUpdateFailed]: "The database update failed while saving the local changes made to the model on database server.",
        [_.errorTryingToUpdateMeasuresWithErrors]: `The requested update failed because the following measures contain errors:<br><strong>{measures}</strong>`,
        [_.errorUnhandled]: "Unhandled error - please report it and provide the trace id, if available.",
        [_.errorUnspecified]: "Unspecified error.",
        [_.errorUserSettingsSaveError]: "Unable to save the settings.",
        [_.errorVpaxFileContainsCorruptedData]: "The VPAX file format is not valid or contains corrupted data.",
        [_.expandAllCtrlTitle]: "Expand all",
        [_.ExportData]: "Export Data",
        [_.exportDataCSVCustomDelimiter]: "Custom Field Delimiter",
        [_.exportDataCSVDelimiter]: "Fields Delimiter",
        [_.exportDataCSVDelimiterComma]: "Comma",
        [_.exportDataCSVDelimiterDesc]: `Choose the character to use as delimiter of each field. <em>Automatic</em> uses the default character of your system Culture.`,
        [_.exportDataCSVDelimiterOther]: "Other...",
        [_.exportDataCSVDelimiterPlaceholder]: "Character",
        [_.exportDataCSVDelimiterSemicolon]: "Semicolon",
        [_.exportDataCSVDelimiterSystem]: "Automatic",
        [_.exportDataCSVDelimiterTab]: "Tab",
        [_.exportDataCSVEncoding]: "Encoding",
        [_.exportDataCSVEncodingDesc]: "",
        [_.exportDataCSVQuote]: "Enclose Strings in Quotes",
        [_.exportDataCSVQuoteDesc]: "Make sure every string is enclosed in double quotes.",
        [_.exportDataExcelCreateExportSummary]: "Export Summary",
        [_.exportDataExcelCreateExportSummaryDesc]: "Add an additional sheet to the export file with the summary of the job.",
        [_.exportDataExport]: "Export Selected",
        [_.exportDataExportAs]: "Export As",
        [_.exportDataExportAsDesc]: "",
        [_.exportDataExporting]: "Exporting {table}...",
        [_.exportDataExportingDone]: "Done!",
        [_.exportDataNoColumns]: "This table is not exportable because it does not contain any columns.",
        [_.exportDataOpenFile]: "Open Export File",
        [_.exportDataOpenFolder]: "Open Export Folder",
        [_.exportDataOptions]: "Export Options",
        [_.exportDataStartExporting]: "Initializing...",
        [_.exportDataSuccessSceneMessage]: "<strong>{count}/{total} table{{s}}</strong> {{were|was}} exported successfully.",
        [_.exportDataSummary]: "Your dataset contains <strong>{count} table{{s}}</strong> that can be exported.",
        [_.exportDataTypeCSV]: "CSV (Comma-separated values)",
        [_.exportDataTypeXLSX]: "Excel Spreadsheet",
        [_.failed]: "Failed",
        [_.filterMeasuresWithErrorsCtrlTitle]: "Show unformatted measures/measures with errors only",
        [_.filterUnrefCtrlTitle]: "Show unreferenced columns only",
        [_.formattingMeasures]: "Formatting measures...",
        [_.goBackCtrlTitle]: "Cancel and go back",
        [_.groupByTableCtrlTitle]: "Group by Table",
        [_.helpConnectVideo]: "How to Connect",
        [_.helpCtrlTitle]: "Help",
        [_.hideUnsupportedCtrlTitle]: "Supported only",
        [_.less]: "Less",
        [_.license]: "Released under MIT license.",
        [_.loading]: "Loading...",
        [_.ManageDates]: "Manage Dates",
        [_.manageDatesApplyCtrlTitle]: "Apply Changes",
        [_.manageDatesAuto]: "Auto",
        [_.manageDatesAutoScan]: "Automatic Scan",
        [_.manageDatesAutoScanActiveRelationships]: "Active Relationships",
        [_.manageDatesAutoScanDesc]: "Choose <em>Full</em> to scan all the columns containing dates. Set <em>Choose Columns...</em> to select the columns to use. Set <em>Active Relationships</em> and <em>Inactive Relationships</em> to scan only columns with relationships.",
        [_.manageDatesAutoScanDisabled]: "Disabled",
        [_.manageDatesAutoScanFirstYear]: "First Year",
        [_.manageDatesAutoScanFirstYearDesc]: "",
        [_.manageDatesAutoScanFull]: "Full",
        [_.manageDatesAutoScanInactiveRelationships]: "Inactive Relationships",
        [_.manageDatesAutoScanLastYear]: "Last Year",
        [_.manageDatesAutoScanLastYearDesc]: "",
        [_.manageDatesAutoScanSelectedTablesColumns]: "Choose Columns...",
        [_.manageDatesBrowserPlaceholder]: "No items to change",
        [_.manageDatesCalendarDesc]: "Choose a calendar template to apply to this model. Bravo will create the required tables or update them while keeping the existing relationships intact.", 
        [_.manageDatesCalendarTemplateName]: "Calendar Template",
        [_.manageDatesCalendarTemplateNameDesc]: "Select <em>Monthly</em> for calendar based on different number of months. Set <em>Weekly</em> for 445-454-544-ISO calendars. Use <em>Custom</em> for flexible calendars of variable length.",
        [_.manageDatesCreatingTables]: "Updating model...",
        [_.manageDatesDatesDesc]: "Configure the format and location of dates in your model.",
        [_.manageDatesDatesTableDesc]: "This is the table to use in reports for dates.",
        [_.manageDatesDatesTableName]: "Dates Table",
        [_.manageDatesDatesTableReferenceDesc]: "This is an hidden table containing all the DAX functions used to generate dates.",
        [_.manageDatesDatesTableReferenceName]: "Dates Definition Table",
        [_.manageDatesHolidaysDesc]: "Add holidays to your model. Bravo will create the required tables or update them while keeping the existing relationships intact. ",
        [_.manageDatesHolidaysEnabledDesc]: "Add the holidays table to your model.",
        [_.manageDatesHolidaysEnabledName]: "Holidays",
        [_.manageDatesHolidaysTableDefinitionDesc]: "This is an hidden table containing all the DAX functions used to generate holidays.",
        [_.manageDatesHolidaysTableDefinitionName]: "Holidays Definition Table",
        [_.manageDatesHolidaysTableDesc]: "This is the table to use in reports for holidays.",
        [_.manageDatesHolidaysTableName]: "Holidays Table",
        [_.manageDatesIntervalDesc]: "Select a date interval for your model.",
        [_.manageDatesISOCountryDesc]: "",
        [_.manageDatesISOCountryName]: "Holidays Country",
        [_.manageDatesISOCustomFormatDesc]: "Enter a regional format using the IETF BCP 47 language tag. E.g. en-US",
        [_.manageDatesISOCustomFormatName]: "Custom Format",
        [_.manageDatesISOFormatDesc]: "Choose the regional format for dates.",
        [_.manageDatesISOFormatName]: "Regional Format",
        [_.manageDatesISOFormatOther]: "Other...",
        [_.manageDatesISOFormatOtherPlaceholder]: "Region",
        [_.manageDatesMenuCalendar]: "Calendar",
        [_.manageDatesMenuDates]: "Dates",
        [_.manageDatesMenuHolidays]: "Holidays",
        [_.manageDatesMenuInterval]: "Interval",
        [_.manageDatesMenuPreviewCode]: "Expression",
        [_.manageDatesMenuPreviewModel]: "Model Changes",
        [_.manageDatesMenuPreviewTable]: "Sample Data",
        [_.manageDatesMenuPreviewTreeDate]: "Dates",
        [_.manageDatesMenuPreviewTreeDateHolidays]: "Dates & Holidays",
        [_.manageDatesMenuPreviewTreeTimeIntelligence]: "Time Intelligence",
        [_.manageDatesMenuTimeIntelligence]: "Time Intelligence",
        [_.manageDatesModelCheck]: "Model Check",
        [_.manageDatesPreview]: "Preview",
        [_.manageDatesPreviewCtrlTitle]: "Preview Changes",
        [_.manageDatesSampleData]: "Sample Data",
        [_.manageDatesSampleDataError]: "Unable to generate sample data.",
        [_.manageDatesStatusCompatible]: `<div class="hero">This model already contains some <b>date tables compatible</b> with Bravo.</div>If you change something here, these tables will be updated and their relationships will remain intact.`,
        [_.manageDatesStatusError]: `<div class="hero">The current settings cannot be applied.</div>{error}`,
        [_.manageDatesStatusIncompatible]: `<div class="hero">This model contains some <b>date tables that are not compatible</b> with Bravo.</div>To make any changes here, you need to choose a different name for one or more tables that will be created by this tool.<br><br>Check <b>Dates</b> and <b>Holidays</b>.`,
        [_.manageDatesStatusNotAvailable]: `<div class="hero">This model is no longer available.</div> Try to restart the application.`,
        [_.manageDatesStatusOk]: `<div class="hero">This model <b>is compatible with the Manage Dates feature</b>.</div>You can create new date tables without worrying about breaking measures or reports.`,
        [_.manageDatesSuccessSceneMessage]: "Congratulations, your model was updated successfully.",
        [_.manageDatesTemplateFirstDayOfWeek]: "First Day of the Week",
        [_.manageDatesTemplateFirstDayOfWeekDesc]: "For Weekly ISO, set <em>Monday</em>.",
        [_.manageDatesTemplateFirstFiscalMonth]: "First Month of the Year",
        [_.manageDatesTemplateFirstFiscalMonthDesc]: "For Weekly ISO, set <em>January</em>.",
        [_.manageDatesTemplateMonthsInYear]: "Months in Year",
        [_.manageDatesTemplateMonthsInYearDesc]: "",
        [_.manageDatesTemplateNameConfig01]: "Standard",
        [_.manageDatesTemplateNameConfig02]: "Monthly",
        [_.manageDatesTemplateNameConfig03]: "Custom",
        [_.manageDatesTemplateNameConfig04]: "Weekly",
        [_.manageDatesTemplateQuarterWeekType]: "Weekly System",
        [_.manageDatesTemplateQuarterWeekTypeDesc]: "",
        [_.manageDatesTemplateTypeStartFiscalYear]: "First Day of the Fiscal Year",
        [_.manageDatesTemplateTypeStartFiscalYearDesc]: "For Weekly ISO, set <em>Last of the Year</em>.",
        [_.manageDatesTemplateTypeStartFiscalYearFirst]: "First of the Year",
        [_.manageDatesTemplateTypeStartFiscalYearLast]: "Last of the Year",
        [_.manageDatesTemplateWeeklyType]: "Last Weekday of the Year",
        [_.manageDatesTemplateWeeklyTypeDesc]: "If your week starts on Sunday, then the last weekday is Saturday. If you choose <em>Last of the Year</em> the fiscal year ends on the last Saturday of the last month. Otherwise, the fiscal year ends on the Saturday closest to the last day of the last month - it could be on the next year. For Weekly ISO, set <em>Closest to the Year End</em>.",
        [_.manageDatesTemplateWeeklyTypeLast]: "Last of the Year",
        [_.manageDatesTemplateWeeklyTypeNearest]: "Closest to the Year End",
        [_.manageDatesTimeIntelligenceDesc]: "Create the most common Time Intelligence DAX functions available in your model.",
        [_.manageDatesTimeIntelligenceEnabledDesc]: "",
        [_.manageDatesTimeIntelligenceEnabledName]: "Time Intelligence Functions",
        [_.manageDatesTimeIntelligenceTargetMeasuresAll]: "All Measures",
        [_.manageDatesTimeIntelligenceTargetMeasuresChoose]: "Choose Measures...",
        [_.manageDatesTimeIntelligenceTargetMeasuresDesc]: "Choose the measure used to generate the Time Intelligence functions.",
        [_.manageDatesTimeIntelligenceTargetMeasuresName]: "Target Measures",
        [_.manageDatesYearRange]: "Date Interval",
        [_.manageDatesYearRangeDesc]: "Choose how to determine the date interval of the model. Leave <em>First Year</em> and/or <em>Last Year</em> empty to use the automatic scan.",
        [_.menuCtrlTitle]: "Collapse/Expand menu",
        [_.minimizeCtrlTitle]: "Minimize",
        [_.modelLanguage]: "Model language ({culture})",
        [_.more]: "More",
        [_.notificationCtrlTitle]: "Notifications",
        [_.notificationsTitle]: "{count} notification{{s}}",
        [_.openSourcePayoff]: "{appName} is an open-source tool developed and mantained by SQLBI and the Github community. Join us at",
        [_.openWithDaxFormatterCtrlTitle]: "Format online with DAX Formatter",  
        [_.optionAccount]: "Power BI Account",
        [_.optionAccountDescription]: "Set the account to access Power BI online datasets.",
        [_.optionDiagnostic]: "Diagnostics Level",
        [_.optionDiagnosticDescription]: "Show errors and logs in a diagnostics pane. Choose <em>Basic</em> to log only a few messages. <em>Verbose</em> logs all messages.",
        [_.optionDiagnosticLevelBasic]: "Basic",
        [_.optionDiagnosticLevelNone]: "None",
        [_.optionDiagnosticLevelVerbose]: "Verbose",
        [_.optionDiagnosticMore]: "To report an application issue, please use",
        [_.optionFormattingBreaks]: "Name-Expression Breaking",
        [_.optionFormattingBreaksAuto]: "Auto",
        [_.optionFormattingBreaksDescription]: "Choose how to separate measure name and expression. Set <em>Auto</em> to automatically determine the style used in the model.",
        [_.optionFormattingBreaksInitial]: "Line Break",
        [_.optionFormattingBreaksNone]: "Same Line",
        [_.optionFormattingLines]: "Lines",
        [_.optionFormattingLinesDescription]: "Choose to keep lines short or long.",
        [_.optionFormattingLinesValueLong]: "Long lines",
        [_.optionFormattingLinesValueShort]: "Short lines",
        [_.optionFormattingPreview]: "Automatic Preview",
        [_.optionFormattingPreviewDescription]: "Automatically send measures to DAX Formatter to generate previews.",
        [_.optionFormattingSeparators]: "Separators",
        [_.optionFormattingSeparatorsDescription]: "Choose the separators for numbers and lists.",
        [_.optionFormattingSeparatorsValueAuto]: "Auto",
        [_.optionFormattingSeparatorsValueEU]: "A; B; C; 1234,00",
        [_.optionFormattingSeparatorsValueUS]: "A, B, C, 1234.00",
        [_.optionFormattingSpaces]: "Spacing",
        [_.optionFormattingSpacesDescription]: "Manage spaces after function names.",
        [_.optionFormattingSpacesValueBestPractice]: "Best practice",
        [_.optionFormattingSpacesValueFalse]: "No space - IF( ",
        [_.optionFormattingSpacesValueTrue]: "Space - IF ( ",
        [_.optionLanguage]: "Language",
        [_.optionLanguageDescription]: "Choose the language of Bravo. Reload required.",
        [_.optionLanguageResetConfirm]: "Do you want to reload Bravo to apply the new language?",
        [_.optionsDialogAboutMenu]: "About",
        [_.optionsDialogFormattingMenu]: "Formatting",
        [_.optionsDialogGeneralMenu]: "General",
        [_.optionsDialogTelemetryMenu]: "Diagnostics",
        [_.optionsDialogTitle]: "Options",
        [_.optionTelemetry]: "Telemetry",
        [_.optionTelemetryDescription]: "Send anonymous usage data to SQLBI.",
        [_.optionTelemetryMore]: "Help us understand how you use Bravo and how to improve it. No personal information is collected. Please note that if this option is disabled, the development team will not able to collect any unhandled errors and provide additional information or support.",
        [_.optionTheme]: "Theme",
        [_.optionThemeDescription]: "Set the theme of Bravo. Leave <em>System</em> to let the OS choose.",
        [_.optionThemeValueAuto]: "System",
        [_.optionThemeValueDark]: "Dark",
        [_.optionThemeValueLight]: "Light",
        [_.otherColumnsRowName]: "Smaller columns...",
        [_.paste]: "Paste",
        [_.powerBiObserving]: "Waiting for file opening in Power BI Desktop...",
        [_.powerBiObservingCancel]: "Cancel",
        [_.powerBiSigninDescription]: "Sign in to Power BI Service to connect Bravo to your online datasets.",
        [_.powerBiSigninTitle]: "Power BI",
        [_.quickActionAttachPBITitle]: "Attach to Power BI Desktop",
        [_.quickActionConnectPBITitle]: "Connect to Power BI Service",
        [_.quickActionOpenVPXTitle]: "Open a Vertipaq Analyzer file",
        [_.refreshCtrlTitle]: "Refresh",
        [_.refreshPreviewCtrlTitle]: "Refresh preview",
        [_.saveVpaxCtrlTile]: "Save as VPAX",
        [_.savingVpax]: "Generating VPAX...",
        [_.sceneUnsupportedReason]: "This feature is not available for this data source.",
        [_.sceneUnsupportedReasonManageDatesAutoDateTimeEnabled]: `Models with auto date/time option enabled are not supported.<br><span class="link" href="https://www.sqlbi.com/tv/disabling-auto-date-time-in-power-bi/">Disabling auto date-time in Power BI (video)</span>`,
        [_.sceneUnsupportedReasonManageDatesEmptyTableCollection]: "This feature is supported only by databases that have at least one table.",
        [_.sceneUnsupportedReasonManageDatesPBIDesktopModelOnly]: "This feature is supported only by models in Power BI Desktop mode.",
        [_.sceneUnsupportedReasonMetadataOnly]: "The database was generated from a VPAX file which includes only its metadata.",
        [_.sceneUnsupportedReasonReadOnly]: "The connection to this database is read-only.",
        [_.sceneUnsupportedReasonXmlaEndpointNotSupported]: "The XMLA endpoint is not supported for this dataset.",
        [_.sceneUnsupportedTitle]: "Unsupported",
        [_.searchColumnPlaceholder]: "Search Column",
        [_.searchDatasetPlaceholder]: "Search Dataset",
        [_.searchEntityPlaceholder]: "Search Table/Column",
        [_.searchMeasurePlaceholder]: "Search Measure",
        [_.searchPlaceholder]: "Search",
        [_.searchTablePlaceholder]: "Search Table",
        [_.settingsCtrlTitle]: "Options",
        [_.sheetOrphan]: "Not available",
        [_.sheetOrphanPBIXTooltip]: "The report was closed in Power BI Desktop. Any writing operation is disallowed.",
        [_.sheetOrphanTooltip]: "This model is not available anymore. Any writing operation is disallowed.",
        [_.showDiagnosticPane]: "Show Details",
        [_.sideCtrlTitle]: "Toggle side-by-side view",
        [_.signedInCtrlTitle]: "Signed in as {name}",
        [_.signIn]: "Sign In",
        [_.signInCtrlTitle]: "Sign In",
        [_.signOut]: "Sign Out",
        [_.sqlbiPayoff]: "Bravo is a project of SQLBI.",
        [_.syncCtrlTitle]: "Synchronize",
        [_.tableColCardinality]: "Cardinality",
        [_.tableColCardinalityTooltip]: "Number of unique elements",
        [_.tableColColumn]: "Column",
        [_.tableColColumns]: "Columns",
        [_.tableColMeasure]: "Measure",
        [_.tableColPath]: "Table \\ Column",
        [_.tableColRows]: "Rows",
        [_.tableColSize]: "Size",
        [_.tableColTable]: "Table",
        [_.tableColWeight]: "Weight",
        [_.tableSelectedCount]: "{count} Selected",
        [_.tableValidationInvalid]: "This name cannot be used.",
        [_.tableValidationValid]: "This name is valid.",
        [_.themeCtrlTitle]: "Change Theme",
        [_.toggleTree]: "Toggle Tree",
        [_.traceId]: "Trace Id",
        [_.unknownMessage]: "Invalid Message Received",
        [_.updateChannelBeta]: "Beta",
        [_.updateChannelCanary]: "Canary",
        [_.updateChannelDev]: "Dev",
        [_.updateChannelStable]: "Stable", 
        [_.updateMessage]: "A new version of Bravo is available: {version}",
        [_.validating]: "Validating...",
        [_.version]: "Version",
        [_.welcomeHelpText]: "Watch the videos below to learn how to use Bravo:",
        [_.welcomeHelpTitle]: "How to use Bravo?",
        [_.welcomeText]: "Bravo is a handy Power BI toolkit that you can use to analyze your models, format measures, create date tables, and export data.",
        [_.whitespacesTitle]: "Whitespaces",
        [_.wrappingTitle]: "Auto word-wrap",
    }
}
export default locale;