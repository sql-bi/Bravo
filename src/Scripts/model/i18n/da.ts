/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Locale } from '../i18n';
import { strings as _ } from '../strings';

const locale: Locale = {

    locale: "da", // DO NOT TRANSLATE
    enName: "Danish", // DO NOT TRANSLATE
    name: "Dansk",

    strings: {
        [_.addCtrlTitle]: "Åbn",
        [_.aggregatedTableName]: "Flere tabeller",
        [_.AnalyzeModel]: "Analysér Model",
        [_.analyzeModelSummary]: `Dit datasæt er <strong>{size:bytes}</strong> stort og indeholder <strong>{count}</strong> kolonne{{r}}`,
        [_.analyzeModelSummary2]: `, <span class="text-highlight">hvoraf <strong>{count}</strong> ikke refereres noget sted i modellen.</span>`,
        [_.appName]: "Bravo for Power BI", // DO NOT TRANSLATE
        [_.appUpdateAvailable]: "Ny version tilgængelig: {version}",
        [_.appUpdateChangelog]: "Ændringslog",
        [_.appUpdateDownload]: "Download",
        [_.appUpdateViewDetails]: "Vis detaljer",
        [_.appUpToDate]: "Bravo er fuldt opdateret",
        [_.appVersion]: "Version {version}",
        [_.backupReminder]: "Husk at tage en backup af din rapport inden du fortsætter - <b>ikke alle ændringer kan fortrydes</b>.",
        [_.BestPractices]: "Bedste praksis",
        [_.canceled]: "Annuleret",
        [_.changeStatusAdded]: "A",
        [_.changeStatusAddedTitle]: "Tilføjet",
        [_.changeStatusDeleted]: "D",
        [_.changeStatusDeletedTitle]: "Slettet",
        [_.changeStatusModified]: "M",
        [_.changeStatusModifiedTitle]: "Ændred",
        [_.checking]: "Kontrol ...",
        [_.clearCtrlTitle]: "Ryd",
        [_.closeCtrlTitle]: "Luk",
        [_.closeOtherTabs]: "Luk øvrige",
        [_.closeTab]: "Luk",
        [_.collapseAllCtrlTitle]: "Kollaps alle",
        [_.columnExportedCompleted]: "Tabellen blev eksporteret korrekt.",
        [_.columnExportedFailed]: "Tabellen kunne ikke eksporteres grundet en uspecifik fejl.",
        [_.columnExportedTruncated]: "Tabellen blev afkortet fordi antallet af rækker overskred det højst tilladte.",
        [_.columnMeasureFormatted]: "Denne måling er allerede formatteret.",
        [_.columnMeasureNotFormattedTooltip]: "Denne måling er ikke formatteret.",
        [_.columnMeasureWithError]: "Denne måling indeholder fejl.",
        [_.columnUnreferencedExplanation]: `<span class="text-highlight">Ikke-refererede kolonner</span> forbedre generelt ydeevnen hvis de fjernes fra modellen. Kontrollér om kolonnerne anvendes i dine rapporter inden du fjerner dem, da Bravo kan ikke afgøre dette.`,
        [_.columnUnreferencedTooltip]: "Denne kolonne bliver ikke refereret i din model.",
        [_.confirmTabCloseMessage]: "Ændringer til dette dokument er ikke blevet gemt.<br>Er du sikker på du vil lukke det?",
        [_.connectBrowse]: "Gennemse",
        [_.connectDatasetsTableEndorsementCol]: "Fremhævning",
        [_.connectDatasetsTableNameCol]: "Navn",
        [_.connectDatasetsTableOwnerCol]: "Ejr",
        [_.connectDatasetsTableWorkspaceCol]: "Arbejdsområde",
        [_.connectDialogAttachPBIMenu]: "Datasæt på powerbi.com",
        [_.connectDialogConnectPBIMenu]: "Aktive Rapporter i Power BI Desktop",
        [_.connectDialogOpenVPXMenu]: "VPAX Filer",
        [_.connectDialogTitle]: "Åbn",
        [_.connectDragFile]: "Træk en VPAX fil herhen<br>eller gennemse din computer",
        [_.connectNoReports]: "Ingen aktive rapporter fundet.<br>Åbn en rapport i Power BI Desktop og vent til den dukker op her.",
        [_.copiedErrorDetails]: "Kopieret!",
        [_.copy]: "Kopier",
        [_.copyErrorDetails]: "Fejl ved kopiering",
        [_.copyFormulaCtrlTitle]: "Kopier formatteret måling",
        [_.copyMessage]: "Kopier meddelelse",
        [_.copyright]: "Alle rettigheder forbeholdes.",
        [_.createIssue]: "Rapportér fejl",
        [_.cut]: "Klip",
        [_.dataUsageLink]: "Hvordan anvendes dine data?", 
        [_.dataUsageMessage]: `For at formattere din kode sender Bravo målingerne i dette datasæt til DAX Formatter, en onlinetjeneste der håndteres af SQLBI, over en siker forbindelse.<p><strong>Denne tjeneste lagrer ikke dine data nogen steder.</strong></p><p>Nogle oplysninger, såsom mest anvendte DAX function, kompleksitetsindeks, og gennemsnitlig forespørgselslængde kan blive lagret til brug for statistik.</p>`,
        [_.dataUsageTitle]: "Hvordan anvendes dine data?",
        [_.DaxFormatter]: "Formater DAX",
        [_.daxFormatterAgreement]: "Bravo sender dine målinger til DAX Formatter tjenesten, for at formattere din DAX kode.",
        [_.daxFormatterAnalyzeConfirm]: "For at udføre analysen sender Bravo alle dine målinger til DAX Formatter services. Vil du fortsætte?",
        [_.daxFormatterAutoPreviewOption]: "Automatisk forhåndsvisning",
        [_.daxFormatterFormat]: "Formater valgte",
        [_.daxFormatterFormatDisabled]: "Formater (Ikke tilgængelig)",
        [_.daxFormatterFormattedCode]: "Formateret (Forhåndsvisning)",
        [_.daxFormatterOriginalCode]: "Aktuel",
        [_.daxFormatterPreviewAllButton]: "Forhåndsvis alle målinger",
        [_.daxFormatterPreviewButton]: "Forhåndsvisning",
        [_.daxFormatterPreviewDesc]: "Bravo sender dine målinger til DAX Formatter tjenesten, for at skabe en forhåndsvisning.",
        [_.daxFormatterSuccessSceneMessage]: "Tillykke, <strong>{count} måling{{er}}</strong> blev formatteret korrekt.",
        [_.daxFormatterSummary]: `Dit datasæt indeholder {count} måling{{er}}: <span class="text-error"><strong>{errors:number}</strong> med fejl</strong></span>, <span class="text-highlight"><strong>{formattable:number}</strong> der kan formatteres</span>, <strong>{analyzable:number}</strong> der kan analyseres (<span class="link manual-analyze">analysér nu</span>).`,
        [_.daxFormatterSummaryNoAnalysis]: `Dit datasæt indeholder <strong>{count}</strong> måling{{er}}: <span class="text-error"><strong>{errors:number}</strong> med fejl</strong></span> og <span class="text-highlight"><strong>{formattable:number}</strong> der kan formatteres.</span>`,
        [_.defaultTabName]: "Unavngivet",
        [_.dialogCancel]: "Annuller",
        [_.dialogOK]: "OK",
        [_.dialogOpen]: "Åbn",
        [_.docLimited]: "Begrænset",
        [_.docLimitedTooltip]: "Ikke alle funktioner er tilgængelige for dette dokument.",
        [_.doneCtrlTitle]: "Færdig",
        [_.emailAddress]: "Email adresse",
        [_.emailAddressPlaceholder]: "Indtast din email adresse",
        [_.error]: "Fejl",
        [_.errorAborted]: "Handling afbrudt.",
        [_.errorAnalysisServicesConnectionFailed]: "Der opstod et problem i forbindelsen mellem serveren og Bravo.",
        [_.errorCheckForUpdates]: "Kan ikke tjekke for opdateringer - der kan ikke forbindes til serveren.",
        [_.errorConnectionUnsupported]: "En forbindelse til den efterspurgte ressource understøttes ikke.",
        [_.errorDatasetConnectionUnknown]: "Ukendt forbindelsestype.",
        [_.errorDatasetsEmptyListing]: "Ingen åbne rapporter tilgængelige.",
        [_.errorDatasetsListing]: "Kan ikke hente listen af datasæt i Power BI-tjenesten.",
        [_.errorExportDataFileError]: "Der opstod en fejl under eksport af data. Prøv igen.",
        [_.errorGetEnvironments]: "Indtast en gyldig Power BI -konto.",
        [_.errorManageDateTemplateError]: "Der opstod en fejl under afvikling af DAX skabelonsmekanismen.",
        [_.errorNetworkError]: "Du er ikke forbundet til Internettet.",
        [_.errorNone]: "Ukendt fejl.",
        [_.errorNotAuthorized]: "Du har ikke adgang til den angivne ressource.",
        [_.errorNotConnected]: "Du er ikke forbundet til Power BI - log ind for at fortsætte.",
        [_.errorNotFound]: "Kan ikke forbinde til den angivne ressource.",
        [_.errorReportConnectionUnknown]: "Ugyldig forbindelse.",
        [_.errorReportConnectionUnsupportedAnalysisServicesCompatibilityMode]: "Power BI Desktop Analysis Services instansens kompatibilitetstilstand er ikke 'PowerBI'.",
        [_.errorReportConnectionUnsupportedAnalysisServicesConnectionNotFound]: "Power BI Desktop Analysis Services TCP forbindelse ikke fundet.",
        [_.errorReportConnectionUnsupportedAnalysisServicesProcessNotFound]: "Power BI Desktop Analysis Services instansen blev ikke fundet.", 
        [_.errorReportConnectionUnsupportedConnectionException]: "Der opstod en fejl under forbindelse til Power BI Desktop Analysis Services instansen.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionEmpty]: "Power BI Desktop Analysis Services instansen indeholder ingen databaser. Prøv at forbinde til rapporten ved at anvende Bravo-ikonet på båndet med Eksterne Værktøjer i Power BI Desktop.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionUnexpectedCount]: "Power BI Desktop Analysis Services instansen indeholder et uventet antal databaser (> 1) - der burde højst være én.",
        [_.errorReportConnectionUnsupportedProcessNotReady]: "Power BI Desktop processen er ved at starte op eller Analysis Services instansen er endnu ikke klar.", 
        [_.errorReportsEmptyListing]: "Ingen uåbnede rapporter tilgængelige.",
        [_.errorRetry]: "Forsøg igen",
        [_.errorSignInMsalExceptionOccurred]: "Ukendt fejl opstod i forbindelse med lo- ind.",
        [_.errorSignInMsalTimeoutExpired]: "Log-ind blev afbrudt fordi timeout perioden udløb inden processen blev gennemført.",
        [_.errorTimeout]: "Forespørgselstimeout.",
        [_.errorTitle]: "Ups...",
        [_.errorTOMDatabaseDatabaseNotFound]: "Databasen findes ikke i samlingen eller brugeren har ikke administrative rettigheder til den.",
        [_.errorTOMDatabaseUpdateConflictMeasure]: "Den anmodede ændring er i konflikt med den aktuelle tilstand af destinationsressources.",
        [_.errorTOMDatabaseUpdateErrorMeasure]: "Den anmodede ændring fejlede fordi én eller flere målinger indeholder fejl.", 
        [_.errorTOMDatabaseUpdateFailed]: "Der opstod en fejl under forsøget på at gemme ændringer til databasen.",
        [_.errorTryingToUpdateMeasuresWithErrors]: `Ændringsanmodningen fejlede fordi følgende målinger indeholder fejl:<br><strong>{measures}</strong>`,
        [_.errorUnhandled]: "Uventet fejl - venligst indrapportér den og oplys sporings id, hvis muligt.",
        [_.errorUnspecified]: "Ukendt fejl.",
        [_.errorUserSettingsSaveError]: "Indstillingerne kunne ikke gemmes.",
        [_.errorVpaxFileExportError]: "Der opstod en fejl under eksport af VPAX-filen.",
        [_.errorVpaxFileImportError]: "Der opstod en fejl under import af VPAX-filen.",
        [_.expandAllCtrlTitle]: "Udvid alle",
        [_.ExportData]: "Eksportér data",
        [_.exportDataCSVCustomDelimiter]: "Brugerdefineret feltafgrænser",
        [_.exportDataCSVDelimiter]: "Feltafgrænser",
        [_.exportDataCSVDelimiterComma]: "Komma",
        [_.exportDataCSVDelimiterDesc]: `Vælg hvilket tegn der skal anvendes til afgrænsning af felter. <em>Automatisk</em> bruger standardafgrænserens for de aktuelle sprogindstillinger.`,
        [_.exportDataCSVDelimiterOther]: "Anden...",
        [_.exportDataCSVDelimiterPlaceholder]: "Tegn",
        [_.exportDataCSVDelimiterSemicolon]: "Semikolon",
        [_.exportDataCSVDelimiterSystem]: "Automatisk",
        [_.exportDataCSVDelimiterTab]: "Tab",
        [_.exportDataCSVEncoding]: "Tegnsæt",
        [_.exportDataCSVEncodingDesc]: "",
        [_.exportDataCSVFolder]: "Gem i en undermappe",
        [_.exportDataCSVFolderDesc]: "Gem genererede CSV -filer i en undermappe.",
        [_.exportDataCSVQuote]: "Afgræns strenge med citationstegn",
        [_.exportDataCSVQuoteDesc]: "Sørger for at strenge er afgrænset af citationstegn.",
        [_.exportDataExcelCreateExportSummary]: "Eksport Opsummering",
        [_.exportDataExcelCreateExportSummaryDesc]: "Tilføj et ekstra ark i den eksporterede fil, med en opsummering af eksportjobbet.",
        [_.exportDataExport]: "Eksportér Valgte",
        [_.exportDataExportAs]: "Eksportér Som...",
        [_.exportDataExportAsDesc]: "",
        [_.exportDataExporting]: "Eksporterer {table}...",
        [_.exportDataExportingDone]: "Færdig!",
        [_.exportDataNoColumns]: "Denne tabel kan ikke eksporteres da den ikke indeholder nogen kolonner.",
        [_.exportDataNotQueryable]: "Denne tabel kan ikke eksporteres da den indeholder én eller flere beregnede kolonner med ugyldige udtryk eller som skal genberegnes.",
        [_.exportDataOpenFile]: "Åbn Eksport Fil",
        [_.exportDataOpenFolder]: "Åbn Eksport Folder",
        [_.exportDataOptions]: "Eksport Indstillinger",
        [_.exportDataStartExporting]: "Initialiserer...",
        [_.exportDataSuccessSceneMessage]: "<strong>{count}/{total} tabel{{ler}}</strong> blev exporteret korrekt.",
        [_.exportDataSummary]: "Dit datasæt indeholder <strong>{count} tabel{{ler}}</strong> der kan eksporteres.",
        [_.exportDataTypeCSV]: "CSV (Kommasepareret format)",
        [_.exportDataTypeXLSX]: "Excel Regneark",
        [_.failed]: "Fejl",
        [_.filterMeasuresWithErrorsCtrlTitle]: "Vis kun ikke-formatterede målinger/målinger med fejl",
        [_.filterUnrefCtrlTitle]: "Vis kun ikke-refererede kolonner",
        [_.formattingMeasures]: "Formatterer målinger...",
        [_.goBackCtrlTitle]: "Annuller og gå tilbage",
        [_.groupByTableCtrlTitle]: "Gruppér efter tabel",
        [_.helpConnectVideo]: "Hvordan oprettes forbindelse",
        [_.helpCtrlTitle]: "Hjælp",
        [_.hideUnsupportedCtrlTitle]: "Kun understøttede",
        [_.less]: "Færre",
        [_.license]: "Udgivet under MIT licens.",
        [_.loading]: "Indlæser...",
        [_.ManageDates]: "Datostyring",
        [_.manageDatesApplyCtrlTitle]: "Anvend Ændringer",
        [_.manageDatesAuto]: "Auto",
        [_.manageDatesAutoScan]: "Automatisk scanning",
        [_.manageDatesAutoScanActiveRelationships]: "Aktiv Relations",
        [_.manageDatesAutoScanDesc]: "Vælg <em>Fuld</em> for at scanne alle kolonner der indeholder datoværdier. Vælg <em>Angiv Kolonner...</em> for at udvælge kolonner der skal scannes. Vælg <em>Aktive Relationer</em> eller <em>Inaktive Relationer</em> for kun at scanne kolonner der deltager i relationer.",
        [_.manageDatesAutoScanDisabled]: "Deaktiveret",
        [_.manageDatesAutoScanFirstYear]: "Første År",
        [_.manageDatesAutoScanFirstYearDesc]: "",
        [_.manageDatesAutoScanFull]: "Fuld",
        [_.manageDatesAutoScanInactiveRelationships]: "Inaktive Relationer",
        [_.manageDatesAutoScanLastYear]: "Sidste År",
        [_.manageDatesAutoScanLastYearDesc]: "",
        [_.manageDatesAutoScanSelectedTablesColumns]: "Angiv Kolonner...",
        [_.manageDatesBrowserPlaceholder]: "Ingen elementer at ændre",
        [_.manageDatesCalendarDesc]: "Vælg en kalenderskabelon der skal anvendes på modellen. Bravo opretter eller opdaterer de nødvendige tabeller mens eksisterende tabelrelationer holdes intakt.", 
        [_.manageDatesCalendarTemplateName]: "Kalenderskabelon",
        [_.manageDatesCalendarTemplateNameDesc]: "Vælg <em>Månedlig</em> for kalendere baseret på forskellige antal måneder. Vælg <em>Ugentlig</em> for 445-454-544-ISO kalendere. Vælg <em>Brugerdefineret</em> for flexible kalendere med variabel længde.",
        [_.manageDatesCreatingTables]: "Opdaterer model...",
        [_.manageDatesDatesDesc]: "Indstil format og placering af datoer i din model.",
        [_.manageDatesDatesTableDesc]: "Denne tabel anvendes til at angive og filtrere datoer i rapporter.",
        [_.manageDatesDatesTableName]: "Datotabel",
        [_.manageDatesDatesTableReferenceDesc]: "Dette er en skjult tabel som indeholder de nødvendige DAX funktioner der anvendes til at generere datoer.",
        [_.manageDatesDatesTableReferenceName]: "Dato Definitionstabel",
        [_.manageDatesHolidaysDesc]: "Tilføj helligdage til modellenl. Bravo will oprette eller opdatere de nødvendige tabeller mens eksisterende tabelrelationer holdes intakt.",
        [_.manageDatesHolidaysEnabledDesc]: "Tilføj en helligdagstabel til din model.",
        [_.manageDatesHolidaysEnabledName]: "Helligdage",
        [_.manageDatesHolidaysTableDefinitionDesc]: "Dette er en skjult tabel som indeholder de nødvendige DAX funktioner der anvendes til at generere helligdage.",
        [_.manageDatesHolidaysTableDefinitionName]: "Helligdage Definitionstabel",
        [_.manageDatesHolidaysTableDesc]: "Denne tabel anvendes til at angive og filtrere helligdage i rapporter.",
        [_.manageDatesHolidaysTableName]: "Helligdagstabel",
        [_.manageDatesIntervalDesc]: "Vælg et datointerval for din model.",
        [_.manageDatesISOCountryDesc]: "",
        [_.manageDatesISOCountryName]: "Vælg land for helligdage",
        [_.manageDatesISOCustomFormatDesc]: "Indtast et regionalt format ved brug af IETF BCP 47-sprogkoden. F.eks. da-DK",
        [_.manageDatesISOCustomFormatName]: "Brugerdefineret Format",
        [_.manageDatesISOFormatDesc]: "Vælg et regional format for datoer.",
        [_.manageDatesISOFormatName]: "Regionalt Format",
        [_.manageDatesISOFormatOther]: "Anden...",
        [_.manageDatesISOFormatOtherPlaceholder]: "Region",
        [_.manageDatesMenuCalendar]: "Kalender",
        [_.manageDatesMenuDates]: "Datoer",
        [_.manageDatesMenuHolidays]: "Helligdage",
        [_.manageDatesMenuInterval]: "Interval",
        [_.manageDatesMenuPreviewCode]: "Udtryk",
        [_.manageDatesMenuPreviewModel]: "Modelændringer",
        [_.manageDatesMenuPreviewTable]: "Eksempeldata",
        [_.manageDatesMenuPreviewTreeDate]: "Datoer",
        [_.manageDatesMenuPreviewTreeDateHolidays]: "Datoer & Helligdage",
        [_.manageDatesMenuPreviewTreeTimeIntelligence]: "Tidsintelligens",
        [_.manageDatesMenuTimeIntelligence]: "Tidsintelligens",
        [_.manageDatesModelCheck]: "Model Check",
        [_.manageDatesPreview]: "Forhåndsvisning",
        [_.manageDatesPreviewCtrlTitle]: "Forhåndsvisning af ændringer",
        [_.manageDatesSampleData]: "Eksempeldata",
        [_.manageDatesSampleDataError]: "Kan ikke generere eksempeldata.",
        [_.manageDatesStatusCompatible]: `<div class="hero">Modellen indeholder allerede <b>datotabeller som er kompatible</b> med Bravo.</div>Hvis du laver ændringer her, bliver disse tabeller ændret mens tabelrelationer holdes intakt.`,
        [_.manageDatesStatusError]: `<div class="hero">Indstillingerne kan ikke anvendes.</div>{error}`,
        [_.manageDatesStatusIncompatible]: `<div class="hero">Modellen indeholder én eller flere <b>datotabeller som ikke er kompatible</b> med Bravo.</div>For at lave ændringer skal du bruge et andet navn til de tabeller der oprettes af værktøjet.<br><br>Check <b>Datoer</b> og <b>Helligdage</b>.`,
        [_.manageDatesStatusNotAvailable]: `<div class="hero">Modellen er ikke længere tilgængelig.</div> Prøv at genstarte applikationen.`,
        [_.manageDatesStatusOk]: `<div class="hero">Denne model <b>er kompatibel med funktionen Datostyring</b>.</div>Der kan oprettes nye tabeller uden målinger eller rapporter påvirkes.`,
        [_.manageDatesSuccessSceneMessage]: "Tillykke, din model er blevet opdateret.",
        [_.manageDatesTemplateFirstDayOfWeek]: "Første Dag i ugen",
        [_.manageDatesTemplateFirstDayOfWeekDesc]: "Ved brug af ugentlig ISO, anvend <em>Mandag</em>.",
        [_.manageDatesTemplateFirstFiscalMonth]: "Første Måned i året",
        [_.manageDatesTemplateFirstFiscalMonthDesc]: "Ved brug af ugentlig ISO, anvend <em>Januar</em>.",
        [_.manageDatesTemplateMonthsInYear]: "Måneder i året",
        [_.manageDatesTemplateMonthsInYearDesc]: "",
        [_.manageDatesTemplateNameConfig01]: "Standard",
        [_.manageDatesTemplateNameConfig02]: "Månedlig",
        [_.manageDatesTemplateNameConfig03]: "Brugerdefineret",
        [_.manageDatesTemplateNameConfig04]: "Ugentlig",
        [_.manageDatesTemplateQuarterWeekType]: "Ugesystem",
        [_.manageDatesTemplateQuarterWeekTypeDesc]: "",
        [_.manageDatesTemplateTypeStartFiscalYear]: "Dato definerer regnskabsåret",
        [_.manageDatesTemplateTypeStartFiscalYearDesc]: "Ved brug af ugentlig ISO, anvend <em>Årets slutning</em>.",
        [_.manageDatesTemplateTypeStartFiscalYearFirst]: "Årets begyndelse",
        [_.manageDatesTemplateTypeStartFiscalYearLast]: "Årets slutning",
        [_.manageDatesTemplateWeeklyType]: "Sidste Ugedag i året",
        [_.manageDatesTemplateWeeklyTypeDesc]: "Sidste Ugedag er lørdag hvis din uge starter på en søndag. Hvis du vælger <em>Årets slutning</em> slutter regnskabsåret den sidste lørdag i den sidste måned. Ellers slutter regnskabsåret den lørdag der ligger tættest på den sidste dag i den sidste måned, hvilket kan være i det nye år. Ved brug af ugentlig ISO, vælg <em>Tættest på årets slutning</em>.",
        [_.manageDatesTemplateWeeklyTypeLast]: "Årets slutning",
        [_.manageDatesTemplateWeeklyTypeNearest]: "Tættest på årets slutning",
        [_.manageDatesTimeIntelligenceDesc]: "Tilføj de mest almindelige Tidsintelligens DAX funktioner til din model.",
        [_.manageDatesTimeIntelligenceEnabledDesc]: "",
        [_.manageDatesTimeIntelligenceEnabledName]: "Tidsintelligens Funktioner",
        [_.manageDatesTimeIntelligenceTargetMeasuresAll]: "Alle Målinger",
        [_.manageDatesTimeIntelligenceTargetMeasuresChoose]: "Vælg Målinger...",
        [_.manageDatesTimeIntelligenceTargetMeasuresDesc]: "Vælg målinger der skal anvendes til at genererere Tidsintelligens funbktioner.",
        [_.manageDatesTimeIntelligenceTargetMeasuresName]: "Valgte Målinger",
        [_.manageDatesYearRange]: "Datointerval",
        [_.manageDatesYearRangeDesc]: "Angiv modellens datospænd. Hvis <em>Første År</em> og/eller <em>Sidste År</em> er blank, bestemmes dette automatisk ud fra data i modellen.",
        [_.menuCtrlTitle]: "Kollaps/Udvid menu",
        [_.minimizeCtrlTitle]: "Minimer",
        [_.modelLanguage]: "Modelsprog ({culture})",
        [_.more]: "Mere",
        [_.notificationCtrlTitle]: "Underretninger",
        [_.notificationsTitle]: "{count} underretning{{er}}",
        [_.openSourcePayoff]: "{appName} er et open-source værktøj udviklet og vedligeholdt af SQLBI og Github fællesskabet. Deltag på",
        [_.openWithDaxFormatterCtrlTitle]: "Formatter online med DAX Formatter",  
        [_.optionAccount]: "Power BI Konto",
        [_.optionAccountDescription]: "Angiv kontoen der skal anvendes for at tilgå Power BI online datasæt.",
        [_.optionBrowserAuthentication]: "Autentificer i browser",
        [_.optionBrowserAuthenticationDescription]: "Autentificer ved hjælp af standardbrowseren.Dette er en alternativ login-metode, der er nyttig til at løse problemer med to-faktor-godkendelse.",
        [_.optionCheckForUpdates]: "Kontroller automatisk for opdateringer",
        [_.optionDiagnostic]: "Diagnostikniveau",
        [_.optionDiagnosticDescription]: "Vis fejl og logs in et diagnostikpanel. Vælg <em>Grundlæggende</em> for kun at logge de vigtigste hændelser. <em>Udvidet</em> logger alle hændelser.",
        [_.optionDiagnosticLevelBasic]: "Grundlæggende",
        [_.optionDiagnosticLevelNone]: "Ingen",
        [_.optionDiagnosticLevelVerbose]: "Udvidet",
        [_.optionDiagnosticMore]: "For at indrapportere en fejl, anvend",
        [_.optionFormattingBreaks]: "Navn-Udtryk Adskillelse",
        [_.optionFormattingBreaksAuto]: "Auto",
        [_.optionFormattingBreaksDescription]: "Vælg hvordan målingsnavn og -udtryk skal adskilles. Ved <em>Auto</em> bruges den type adskillelse som allerede anvendes i modellen.",
        [_.optionFormattingBreaksInitial]: "Linjeskift",
        [_.optionFormattingBreaksNone]: "Samme linje",
        [_.optionFormattingIncludeTimeIntelligence]: "Inkluder tidsintelligens",
        [_.optionFormattingIncludeTimeIntelligenceDescription]: "Medtag foranstaltninger oprettet automatisk efter administrationsdatoer for tidsinformation.",
        [_.optionFormattingLines]: "Linjelængde",
        [_.optionFormattingLinesDescription]: "Vælg om linjer skal holdes korte eller lange.",
        [_.optionFormattingLinesValueLong]: "Lange linjer",
        [_.optionFormattingLinesValueShort]: "Korte linjer",
        [_.optionFormattingPreview]: "Automatisk Forhåndsvisning",
        [_.optionFormattingPreviewDescription]: "Send automatisk målinger til DAX Formatter for at skabe forhåndsvisninger.",
        [_.optionFormattingSeparators]: "Separatorer",
        [_.optionFormattingSeparatorsDescription]: "Vælg hvilken type separator der anvendes til tal og lister.",
        [_.optionFormattingSeparatorsValueAuto]: "Auto",
        [_.optionFormattingSeparatorsValueEU]: "A; B; C; 1234,00",
        [_.optionFormattingSeparatorsValueUS]: "A, B, C, 1234.00",
        [_.optionFormattingSpaces]: "Mellemrum",
        [_.optionFormattingSpacesDescription]: "Vælg om der skal indsættes mellemrum efter funktionsnavne.",
        [_.optionFormattingSpacesValueBestPractice]: "Bedste praksis",
        [_.optionFormattingSpacesValueFalse]: "Intet mellemrum - IF( ",
        [_.optionFormattingSpacesValueTrue]: "Mellemrum - IF ( ",
        [_.optionInvalidValue]: "Ugyldig værdi",
        [_.optionLanguage]: "Sprog",
        [_.optionLanguageDescription]: "Vælg det sprog der skal anvendes i Bravo. Genstart nødvendig.",
        [_.optionLanguageResetConfirm]: "Vil du genstarte Bravo for at anvende det valgte sprog?",
        [_.optionPolicyNotice]: "Denne mulighed administreres af din organisation.",
        [_.optionProxyAddress]: "Proxy -serveradresse",
        [_.optionProxyAddressDescription]: "Giv adressen på din proxyserver.",
        [_.optionProxyBypassList]: "Ekskluder adresselisten",
        [_.optionProxyBypassListDescription]: "Brug proxyserveren undtagen adresser, der starter med de indsatte poster.Brug semikoloner () til at adskille poster.",
        [_.optionProxyBypassOnLocal]: "Omgå lokale adresser",
        [_.optionProxyBypassOnLocalDescription]: "Brug ikke proxy med lokale (intranet) adresser.",
        [_.optionProxyConfirmDeleteCredentials]: "Er du sikker på at fjerne de brugerdefinerede legitimationsoplysninger fra systemet?",
        [_.optionProxyCustomCredentials]: "Brugerdefinerede legitimationsoplysninger",
        [_.optionProxyCustomCredentialsDescription]: "Brug brugerdefinerede legitimationsoplysninger til at autentificere til proxyserveren.Forlad for at bruge systemoplysningerne.",
        [_.optionProxyCustomCredentialsEdit]: "Rediger brugerdefinerede legitimationsoplysninger",
        [_.optionProxyType]: "Proxyserver",
        [_.optionProxyTypeCustom]: "Brugerdefinerede",
        [_.optionProxyTypeDescription]: "Vælg en proxyserver.",
        [_.optionProxyTypeNone]: "Ingen",
        [_.optionProxyTypeSystem]: "system",
        [_.optionsDialogAboutMenu]: "Om",
        [_.optionsDialogFormattingMenu]: "Formattering",
        [_.optionsDialogGeneralMenu]: "Generelt",
        [_.optionsDialogProxyMenu]: "Proxy",
        [_.optionsDialogTelemetryMenu]: "Diagnostik",
        [_.optionsDialogTitle]: "Indstillinger",
        [_.optionTelemetry]: "Telemetri",
        [_.optionTelemetryDescription]: "Send anonym brugsstatistik til SQLBI.",
        [_.optionTelemetryMore]: "Hjælp os med at forbedre Bravo ved at sende statistik om hvordan produktet anvendes. Ingen personhenførbare oplysninger indsamles. Bemærk, at hvis dette er fravalgt kan udviklerteamet ikke indsamle information om uventede fejl, hvilket gør det vanskeligere at udøve support.",
        [_.optionTheme]: "Tema",
        [_.optionThemeDescription]: "Angiv tema der anvendes af Bravo. Vælg <em>System</em> for at lade OS'et bestemme.",
        [_.optionThemeValueAuto]: "System",
        [_.optionThemeValueDark]: "Mørk",
        [_.optionThemeValueLight]: "Lys",
        [_.otherColumnsRowName]: "Mindre kolonner...",
        [_.paste]: "Indsæt",
        [_.powerBiObserving]: "Venter på at filen åbnes i Power BI Desktop...",
        [_.powerBiObservingCancel]: "Annuller",
        [_.powerBiSigninDescription]: "Log ind i Power BI-tjenesten for at forbinde til onlinedatasæt med Bravo.",
        [_.powerBiSigninTitle]: "Power BI",
        [_.quickActionAttachPBITitle]: "Forbind til Power BI Desktop",
        [_.quickActionConnectPBITitle]: "Forbind til Power BI-tjenesten",
        [_.quickActionOpenVPXTitle]: "Åbn en Vertipaq Analyzer fil",
        [_.refreshCtrlTitle]: "Opdater",
        [_.refreshPreviewCtrlTitle]: "Opdater forhåndsvisning",
        [_.saveVpaxCtrlTile]: "Gem som VPAX",
        [_.savingVpax]: "Genererer VPAX...",
        [_.sceneUnsupportedReason]: "Denne funktion er ikke tilgængelig for den aktuelle datakilde.",
        [_.sceneUnsupportedReasonManageDatesAutoDateTimeEnabled]: `Modeller med automatisk dato/tid er ikke understøttet.<br><span class="link" href="https://www.sqlbi.com/tv/disabling-auto-date-time-in-power-bi/">Slå automatisk dato/tid fra i Power BI (video, engelsk)</span>`,
        [_.sceneUnsupportedReasonManageDatesEmptyTableCollection]: "Denne funktion undersøttes kun i databaser der har mindst én tabel.",
        [_.sceneUnsupportedReasonManageDatesPBIDesktopModelOnly]: "Denne funktion understøttes kun på modeller i Power BI Desktop-tilstand.",
        [_.sceneUnsupportedReasonMetadataOnly]: "Databasen blev genskabt fra en VPAX fil der kun inkluderer metadata.",
        [_.sceneUnsupportedReasonReadOnly]: "Forbindelsen til denne database er skrivebeskyttet.",
        [_.sceneUnsupportedReasonXmlaEndpointNotSupported]: "XMLA-slutpunktet understøttes ikke for dette datasæt.",
        [_.sceneUnsupportedTitle]: "Ikke understøttet",
        [_.searchColumnPlaceholder]: "Find Kolonne",
        [_.searchDatasetPlaceholder]: "Find Datasæt",
        [_.searchEntityPlaceholder]: "Find Tabel/Kolonne",
        [_.searchMeasurePlaceholder]: "Find Måling",
        [_.searchPlaceholder]: "Søg",
        [_.searchTablePlaceholder]: "Find Tabel",
        [_.settingsCtrlTitle]: "Indstillinger",
        [_.sheetOrphan]: "Ikke tilgængelig",
        [_.sheetOrphanPBIXTooltip]: "Rapporten blev lukket i Power BI Desktop. Ændringer kan ikke længere gemmes.",
        [_.sheetOrphanTooltip]: "Modellen er ikke længere tilgængelig. Ændringer kan ikke gemmes.",
        [_.showDiagnosticPane]: "Vis Detaljer",
        [_.sideCtrlTitle]: "Brug side-om-side visning",
        [_.signedInCtrlTitle]: "Logget ind som {name}",
        [_.signIn]: "Log ind",
        [_.signInCtrlTitle]: "Log ind",
        [_.signOut]: "Log ud",
        [_.sqlbiPayoff]: "Bravo er skabt af SQLBI.",
        [_.syncCtrlTitle]: "Synkronisér",
        [_.tableColCardinality]: "Kardinalitet",
        [_.tableColCardinalityTooltip]: "Antal unikke værdier",
        [_.tableColColumn]: "Kolonne",
        [_.tableColColumns]: "Kolonner",
        [_.tableColMeasure]: "Måling",
        [_.tableColPath]: "Tabel \\ Kolonne",
        [_.tableColRows]: "Rækker",
        [_.tableColSize]: "Størrelse",
        [_.tableColTable]: "Tabel",
        [_.tableColWeight]: "Vægt",
        [_.tableSelectedCount]: "{count} valgte",
        [_.tableValidationInvalid]: "Dette navn kan ikke anvendes",
        [_.tableValidationValid]: "Dette navn er ugyldigt",
        [_.themeCtrlTitle]: "Skift tema",
        [_.toggleTree]: "Skift træ",
        [_.traceId]: "Sproings-id",
        [_.unknownMessage]: "Ugyldig besked modtagetd",
        [_.updateChannelBeta]: "Beta",
        [_.updateChannelCanary]: "Canary",
        [_.updateChannelDev]: "Dev",
        [_.updateChannelStable]: "Stable", 
        [_.updateMessage]: "En ny version af Bravo er tilgængelig: {version}",
        [_.validating]: "Validerer...",
        [_.version]: "Version",
        [_.welcomeHelpText]: "Se videoerne nedenfor for at lære hvordan Bravo anvendes:",
        [_.welcomeHelpTitle]: "Hvordan anvendes Bravo?",
        [_.welcomeText]: "Bravo er et smart Power BI værktøj der kan bruges til at analysere din model, formattere målinger, oprette dataotabeller, og eksportere data.",
        [_.whitespacesTitle]: "Blanke tegn",
        [_.wrappingTitle]: "Automatisk orddeling",
    }
}
export default locale;
