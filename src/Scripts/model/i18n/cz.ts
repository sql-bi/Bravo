/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Locale } from '../i18n';
import { strings as _ } from '../strings';

const locale: Locale = {

    locale: "cz", //DO NOT TRANSLATE
    enName: "Czech", //DO NOT TRANSLATE
    name: "čeština",

    strings: {
        [_.addCtrlTitle]: "Otevřít",
        [_.aggregatedTableName]: "Více tabulek",
        [_.AnalyzeModel]: "Analyzovat model",
        [_.analyzeModelSummary]: `Vaše datová sada je <strong>{size:bytes}</strong> veliký a obsahuje <strong>{count}</strong> {{sloupce|sloupec}}`,
        [_.analyzeModelSummary2]: `, z toho <span class="text-highlight"><strong>{count}</strong> {{jsou|je}} bez reference v rámci modelu.</span>`,
        [_.appName]: "Bravo for Power BI", //DO NOT TRANSLATE
        [_.appUpdateAvailable]: "Nová verze je dostupná: {version}",
        [_.appUpdateChangelog]: "Seznam změn",
        [_.appUpdateDownload]: "Stáhnout",
        [_.appUpdateViewDetails]: "Zobrazit detaily",
        [_.appUpToDate]: "Bravo je aktuální",
        [_.appVersion]: "Verze {version}",
        [_.backupReminder]: "Před procesováním, prosím pamatujte, že je dobré váš report zálohovat - <b>některé změny mohou být neobnovitelné</b>.",
        [_.BestPractices]: "Best Practice",
        [_.canceled]: "Zrušeno",
        [_.changeStatusAdded]: "P",
        [_.changeStatusAddedTitle]: "Přidáno",
        [_.changeStatusDeleted]: "S",
        [_.changeStatusDeletedTitle]: "Smazáno",
        [_.changeStatusModified]: "U",
        [_.changeStatusModifiedTitle]: "Upraveno",
        [_.checking]: "Kontrola ...",
        [_.clearCtrlTitle]: "Vymazat",
        [_.closeCtrlTitle]: "Zavřít",
        [_.closeOtherTabs]: "Zavřít ostatní",
        [_.closeTab]: "Zavřít",
        [_.collapseAllCtrlTitle]: "Sbalit vše",
        [_.columnExportedCompleted]: "Tabulka byla exportována úspěšně.",
        [_.columnExportedFailed]: "Tabulka nemohla být exportována z důvodu neočekávané chyby.",
        [_.columnExportedTruncated]: "Tabulka byla zkrácena, protože obsahuje větší počet řádků než je povoleno.",
        [_.columnMeasureFormatted]: "Tato measure již formátována je.",
        [_.columnMeasureNotFormattedTooltip]: "Tato measure není formátovaná.",
        [_.columnMeasureWithError]: "Tato measure obsahuje chyby.",
        [_.columnUnreferencedExplanation]: `<span class="text-highlight">Nereferncované sloupce</span> lze obecně z modelu odstranit, aby se optimalizoval výkon. Před jejich odstraněním se ujistěte, že tyto sloupce nepoužíváte v žádných sestavách, které Bravo nedokáže určit.`,
        [_.columnUnreferencedTooltip]: "Sloupec není referencován ve vašem modelu.",
        [_.confirmTabCloseMessage]: "Vypadá to, že jste neuložili změny v tomto dokumentu.<br>Opravdu si jej přejete zavřít?",
        [_.connectBrowse]: "Procházet",
        [_.connectDatasetsTableEndorsementCol]: "Endorsement",
        [_.connectDatasetsTableNameCol]: "Jméno",
        [_.connectDatasetsTableOwnerCol]: "Vlastník",
        [_.connectDatasetsTableWorkspaceCol]: "Pracovní prostor",
        [_.connectDialogAttachPBIMenu]: "Datová sada v powerbi.com",
        [_.connectDialogConnectPBIMenu]: "Aktivní sestava v Power BI Desktop",
        [_.connectDialogOpenVPXMenu]: "VPAX Soubory",
        [_.connectDialogTitle]: "Otevřít",
        [_.connectDragFile]: "Vložte sem VPAX soubor<br>nebo jej vyhledejte ve svém počítači",
        [_.connectNoReports]: "Nebyla nalezena žádná otevřená sestava.<br>Otevřete sestavu v Power BI Desktop a počkejte než se zde objeví.",
        [_.copiedErrorDetails]: "Zkopírováno!",
        [_.copy]: "Kopírovat",
        [_.copyErrorDetails]: "Zkopírovat chybu",
        [_.copyFormulaCtrlTitle]: "Zkopírovat formátovanou measure",
        [_.copyMessage]: "Zkopírovat zprávu",
        [_.copyright]: "Všechna práva vyhrazena.",
        [_.createIssue]: "Nahlásit chybu/problém",
        [_.cut]: "Vyjmout",
        [_.dataUsageLink]: "Jak jsou vaše data využita?", 
        [_.dataUsageMessage]: `Pro formátování vašeho kódu Bravo odesílá kód measure z datové sady do DAX Formatter, který je spravován SQLBI, přes zabezpečené propojení.<p><strong>Služba vaše data nikde neukládá.</strong></p><p>Některé informace, jako jsou nejpoužívanější funkce jazyka DAX, index složitosti a průměrná délka dotazu, lze uložit pro statistické účely.</p>`,
        [_.dataUsageTitle]: "Jak jsou vaše data využita?",
        [_.DaxFormatter]: "Formátovat DAX",
        [_.daxFormatterAgreement]: "Pro formátování DAX odešle Bravo vaše measures do služby DAX Formatter.",
        [_.daxFormatterAnalyzeConfirm]: "Pro provedení analýzy musí Bravo odeslat všechny measures službě DAX Formatter. Opravdu chcete pokračovat?",
        [_.daxFormatterAutoPreviewOption]: "Automatický náhled",
        [_.daxFormatterFormat]: "Formát zvolen",
        [_.daxFormatterFormatDisabled]: "Formát (Není podporovaný)",
        [_.daxFormatterFormattedCode]: "Formát (Náhled)",
        [_.daxFormatterOriginalCode]: "Současný",
        [_.daxFormatterPreviewAllButton]: "Náhled všech Measures",
        [_.daxFormatterPreviewButton]: "Náhled",
        [_.daxFormatterPreviewDesc]: "Aby bylo možné vygenerovat náhled, musí Bravo odeslat tuto measure do služby DAX Formatter.",
        [_.daxFormatterSuccessSceneMessage]: "Blahopřejeme, <strong>{count} measure{{s}}</strong> {{jsou|je}} zformátována úspěšně.",
        [_.daxFormatterSummary]: `Vaše datová sadat obsahuje {count} measure{{s}}: <span class="text-error"><strong>{errors:number}</strong> s chybami</strong></span>, <span class="text-highlight"><strong>{formattable:number}</strong> k formátování</span>, <strong>{analyzable:number}</strong> k analýze (<span class="link manual-analyze">analyzovat teď</span>).`,
        [_.daxFormatterSummaryNoAnalysis]: `Vaše datová sadat obsahuje <strong>{count}</strong> measure{{s}}: <span class="text-error"><strong>{errors:number}</strong> s chybami</strong></span> a <span class="text-highlight"><strong>{formattable:number}</strong> k formátování.</span>`,
        [_.defaultTabName]: "Nepojmenovaná",
        [_.dialogCancel]: "Zrušit",
        [_.dialogOK]: "OK",
        [_.dialogOpen]: "Otevřít",
        [_.docLimited]: "Omezeno",
        [_.docLimitedTooltip]: "Ne všechny funkkce jsou dostupné pro tento dokument.",
        [_.doneCtrlTitle]: "Hotovo",
        [_.emailAddress]: "E-mailová adresa",
        [_.emailAddressPlaceholder]: "Vložte svou e-mailovou adresu",
        [_.error]: "Chyba",
        [_.errorAborted]: "Operace přerušena.",
        [_.errorAnalysisServicesConnectionFailed]: "Mezi serverem a Bravo vznikl problém s připojením.",
        [_.errorCheckForUpdates]: "Nelze zkontrolovat aktualizace – vzdálený server je nedostupný.",
        [_.errorConnectionUnsupported]: "Připojení k požadovanému zdroji není podporováno.",
        [_.errorDatasetConnectionUnknown]: "Nespecifikované spojení.",
        [_.errorDatasetsEmptyListing]: "Nejsou k dispozici žádné otevřené sestavy.",
        [_.errorDatasetsListing]: "Nelze načíst seznam datových sad služby Power BI.",
        [_.errorExportDataFileError]: "Při exportu dat se něco pokazilo. Prosím zkuste to znovu.",
        [_.errorGetEnvironments]: "Zadejte platný účet Power BI.",
        [_.errorManageDateTemplateError]: "Při provádění šablony DAX došlo k výjimce.",
        [_.errorNetworkError]: "Nejste připojeni k internetu.",
        [_.errorNone]: "Nespecifikovaná chyba.",
        [_.errorNotAuthorized]: "Nemáte oprávnění prohlížet zadaný zdroj.",
        [_.errorNotConnected]: "Nejste připojeni k Power BI – pro pokračování se prosím přihlaste.",
        [_.errorNotFound]: "Nelze se připojit k zadanému prostředku.",
        [_.errorReportConnectionUnknown]: "Neplatné připojení.",
        [_.errorReportConnectionUnsupportedAnalysisServicesCompatibilityMode]: "Režim kompatibility instance Power BI Desktop Analysis Services není Power BI.",
        [_.errorReportConnectionUnsupportedAnalysisServicesConnectionNotFound]: "Připojení TCP služby Power BI Desktop Analysis Services nebylo nalezeno.",
        [_.errorReportConnectionUnsupportedAnalysisServicesProcessNotFound]: "Proces instance služby Power BI Desktop Analysis Services nebyl nalezen.", 
        [_.errorReportConnectionUnsupportedConnectionException]: "Při připojování k instanci Power BI Desktop Analysis Services byla vyvolána výjimka.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionEmpty]: "Instance Power BI Desktop Analysis Services neobsahuje žádné databáze. Zkuste se k sestavě připojit pomocí ikony Bravo v Externích nástrojích Power BI Desktopu.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionUnexpectedCount]: "Instance Power BI Desktop Analysis Services obsahuje neočekávaný počet databází (> 1), zatímco my očekáváme nulu nebo jednu.",
        [_.errorReportConnectionUnsupportedProcessNotReady]: "Otevírá se proces Power BI Desktopu nebo instance služby Analysis Services ještě není připravena.", 
        [_.errorReportsEmptyListing]: "Nejsou k dispozici žádné neotevřené chyby.",
        [_.errorRetry]: "Opakovat",
        [_.errorSignInMsalExceptionOccurred]: "Neočekávaná chyba v rámci přihlášení.",
        [_.errorSignInMsalTimeoutExpired]: "Požadavek na přihlášení byl zrušen, protože časový limit vypršel před dokončením operace.",
        [_.errorTimeout]: "Časový limit požadavku vypršel.",
        [_.errorTitle]: "Whoops...",
        [_.errorTOMDatabaseDatabaseNotFound]: "The database does not exist in the collection or the user does not have administrator rights to access it.",
        [_.errorTOMDatabaseUpdateConflictMeasure]: "The requested update conflicts with the current state of the target resource.",
        [_.errorTOMDatabaseUpdateErrorMeasure]: "The requested update failed because one or more measures contain errors.", 
        [_.errorTOMDatabaseUpdateFailed]: "The database update failed while saving the local changes made to the model on database server.",
        [_.errorTryingToUpdateMeasuresWithErrors]: `The requested update failed because the following measures contain errors:<br><strong>{measures}</strong>`,
        [_.errorUnhandled]: "Neošetřená chyba – nahlašte ji prosím a uveďte ID trasování, je-li k dispozici.",
        [_.errorUnspecified]: "Nespecifikovaná chyba.",
        [_.errorUserSettingsSaveError]: "Nastavení nelze uložit.",
        [_.errorVpaxFileExportError]: "Při exportu souboru VPAX došlo k chybě.",
        [_.errorVpaxFileImportError]: "Při importu souboru VPAX došlo k chybě.",
        [_.expandAllCtrlTitle]: "Rozbalit vše",
        [_.ExportData]: "Export dat",
        [_.exportDataCSVCustomDelimiter]: "Vlastní oddělovač",
        [_.exportDataCSVDelimiter]: "Oddělovač polí",
        [_.exportDataCSVDelimiterComma]: "Čárka",
        [_.exportDataCSVDelimiterDesc]: `Vyberte znak, který chcete použít jako oddělovač polí. <em>Automaticky</em> používá výchozí znak vaší systémové kultury.`,
        [_.exportDataCSVDelimiterOther]: "Ostatní...",
        [_.exportDataCSVDelimiterPlaceholder]: "Znak",
        [_.exportDataCSVDelimiterSemicolon]: "Středník",
        [_.exportDataCSVDelimiterSystem]: "Automatický",
        [_.exportDataCSVDelimiterTab]: "Tab",
        [_.exportDataCSVEncoding]: "Kódování",
        [_.exportDataCSVEncodingDesc]: "",
        [_.exportDataCSVFolder]: "Ušetřete v podsložce",
        [_.exportDataCSVFolderDesc]: "Uložte generované soubory CSV v podsložce.",
        [_.exportDataCSVQuote]: "Uzavřete textový řetězec do uvozovek",
        [_.exportDataCSVQuoteDesc]: "Ujistěte se, že každý řetězec je uzavřen do dvojitých uvozovek.",
        [_.exportDataExcelCreateExportSummary]: "Exportovat souhrn",
        [_.exportDataExcelCreateExportSummaryDesc]: "Přidejte do exportovaného souboru další list se shrnutím úlohy.",
        [_.exportDataExport]: "Exportovat vybrané",
        [_.exportDataExportAs]: "Exportovat jako",
        [_.exportDataExportAsDesc]: "",
        [_.exportDataExporting]: "Exportuji {table}...",
        [_.exportDataExportingDone]: "Hotovo!",
        [_.exportDataNoColumns]: "Tuto tabulku nelze exportovat, protože neobsahuje žádné sloupce.",
        [_.exportDataNotQueryable]: "Tuto tabulku nelze exportovat, protože obsahuje jeden nebo více sloupců vypočtených pomocí neplatného výrazu nebo sloupců, které je třeba přepočítat.",
        [_.exportDataOpenFile]: "Otevřít Exportovaný Soubor",
        [_.exportDataOpenFolder]: "Otevřít Složku s Exporty",
        [_.exportDataOptions]: "Možnosti Exportu",
        [_.exportDataStartExporting]: "Inicializace...",
        [_.exportDataSuccessSceneMessage]: "<strong>{count}/{total} {{tabulek|tabulka}}</strong> bylo úspěšně exportováno.",
        [_.exportDataSummary]: "Vaše datová sada obsahuje <strong>{count} {{tabulek|tabulku}}</strong>, které mohou být exportovány.",
        [_.exportDataTypeCSV]: "CSV (Hodnoty oddělené čárkami)",
        [_.exportDataTypeXLSX]: "Excelová tabulka",
        [_.failed]: "Nepodařilo se",
        [_.filterMeasuresWithErrorsCtrlTitle]: "Zobrazit neformátované measures pouze s chybami",
        [_.filterUnrefCtrlTitle]: "Zobrazit pouze nereferencované sloupce",
        [_.formattingMeasures]: "Formátuji measures...",
        [_.goBackCtrlTitle]: "Zrušit a vrátit se zpět",
        [_.groupByTableCtrlTitle]: "Seskupit tabulky",
        [_.helpConnectVideo]: "Jak se připojit",
        [_.helpCtrlTitle]: "Pomoc",
        [_.hideUnsupportedCtrlTitle]: "Podporované pouze",
        [_.less]: "Méně",
        [_.license]: "Vydáno pod licencí MIT.",
        [_.loading]: "Načítám...",
        [_.ManageDates]: "Spravovat datumy",
        [_.manageDatesApplyCtrlTitle]: "Aplikovat změny",
        [_.manageDatesAuto]: "Auto",
        [_.manageDatesAutoScan]: "Automatický skan",
        [_.manageDatesAutoScanActiveRelationships]: "Aktivní vazba",
        [_.manageDatesAutoScanDesc]: "Vyberte <em>Plný</em> pro skenování všech sloupců obsahujících data. Chcete-li vybrat sloupce, které chcete použít, nastavte <em>Vybrat sloupce...</em>. Nastavte <em>Aktivní vazby</em> a <em>Neaktivní vazby</em>, chcete-li prohledávat pouze sloupce se vztahy.",
        [_.manageDatesAutoScanDisabled]: "Zakázáno",
        [_.manageDatesAutoScanFirstYear]: "První rok",
        [_.manageDatesAutoScanFirstYearDesc]: "",
        [_.manageDatesAutoScanFull]: "Plný",
        [_.manageDatesAutoScanInactiveRelationships]: "Neaktivní vazba",
        [_.manageDatesAutoScanLastYear]: "Poslední rok",
        [_.manageDatesAutoScanLastYearDesc]: "",
        [_.manageDatesAutoScanSelectedTablesColumns]: "Vybrat sloupce...",
        [_.manageDatesBrowserPlaceholder]: "Žádné položky ke změně",
        [_.manageDatesCalendarDesc]: "Vyberte šablonu kalendáře, kterou chcete použít na tento model. Bravo vytvoří požadované tabulky nebo je aktualizuje, přičemž zachová stávající vazby nedotčené.", 
        [_.manageDatesCalendarTemplateName]: "Šablona kalendáře",
        [_.manageDatesCalendarTemplateNameDesc]: "Vyberte <em>Měsíčně</em> pro kalendář založený na různém počtu měsíců. Nastavte <em>Týdně</em> pro kalendáře 445-454-544-ISO. Pro flexibilní kalendáře s proměnnou délkou použijte <em>Vlastní</em>.",
        [_.manageDatesCreatingTables]: "Upraviji model...",
        [_.manageDatesDatesDesc]: "Nastavte formát a umístění dat v modelu.",
        [_.manageDatesDatesTableDesc]: "Toto je tabulka pro použití v přehledech pro datumy.",
        [_.manageDatesDatesTableName]: "Datumová/Datová tabulka",
        [_.manageDatesDatesTableReferenceDesc]: "Toto je skrytá tabulka obsahující všechny funkce jazyka DAX používané ke generování dat.",
        [_.manageDatesDatesTableReferenceName]: "Tabulka Definice Datumů",
        [_.manageDatesHolidaysDesc]: "Přidejte do svého modelu svátky. Bravo vytvoří požadované tabulky nebo je aktualizuje, přičemž zachová stávající vazby nedotčené. ",
        [_.manageDatesHolidaysEnabledDesc]: "Přidejte do svého modelu tabulku svátků.",
        [_.manageDatesHolidaysEnabledName]: "Svátky",
        [_.manageDatesHolidaysTableDefinitionDesc]: "Toto je skrytá tabulka obsahující všechny funkce jazyka DAX používané ke generování svátků.",
        [_.manageDatesHolidaysTableDefinitionName]: "Tabulka Definice Svátků",
        [_.manageDatesHolidaysTableDesc]: "Toto je tabulka pro použití v přehledech pro svátky.",
        [_.manageDatesHolidaysTableName]: "Tabulka Svátků",
        [_.manageDatesIntervalDesc]: "Vyberte interval data pro váš model.",
        [_.manageDatesISOCountryDesc]: "",
        [_.manageDatesISOCountryName]: "Svátky Země",
        [_.manageDatesISOCustomFormatDesc]: "Zadejte regionální formát pomocí jazykové značky IETF BCP 47. Např. en-US nebo cs-CZ",
        [_.manageDatesISOCustomFormatName]: "Vlastní formát",
        [_.manageDatesISOFormatDesc]: "Vyberte pro datum regionální formát.",
        [_.manageDatesISOFormatName]: "Regionální Formát",
        [_.manageDatesISOFormatOther]: "Ostatní...",
        [_.manageDatesISOFormatOtherPlaceholder]: "Region",
        [_.manageDatesMenuCalendar]: "Kalendář",
        [_.manageDatesMenuDates]: "Datumy",
        [_.manageDatesMenuHolidays]: "Svátky",
        [_.manageDatesMenuInterval]: "Interval",
        [_.manageDatesMenuPreviewCode]: "Výraz",
        [_.manageDatesMenuPreviewModel]: "Změny Modelu",
        [_.manageDatesMenuPreviewTable]: "Vzorek dat",
        [_.manageDatesMenuPreviewTreeDate]: "Datumy",
        [_.manageDatesMenuPreviewTreeDateHolidays]: "Datumy a Svátky",
        [_.manageDatesMenuPreviewTreeTimeIntelligence]: "Time Intelligence",
        [_.manageDatesMenuTimeIntelligence]: "Time Intelligence",
        [_.manageDatesModelCheck]: "Kontrola Modelu",
        [_.manageDatesPreview]: "Náhled",
        [_.manageDatesPreviewCtrlTitle]: "Náhled změn",
        [_.manageDatesSampleData]: "Vzorek dat",
        [_.manageDatesSampleDataError]: "Nelze vygenerovat ukázková data.",
        [_.manageDatesStatusCompatible]: `<div class="hero">Tento model již obsahuje některé <b>tabulky datumů kompatibilní</b> s Bravo.</div>Pokud zde něco změníte, tyto tabulky budou aktualizovány a jejich vazby zůstanou nedotčeny.`,
        [_.manageDatesStatusError]: `<div class="hero">Aktuální nastavení nelze bohužel použít.</div>{error}`,
        [_.manageDatesStatusIncompatible]: `<div class="hero">Model obsahuje několik <b>datumových tabulek, které nejsou kompatibilní</b> s Bravo.</div>Chcete-li zde provést jakékoli změny, musíte pro jednu nebo více tabulek zvolit jiný název který bude vytvořen tímto nástrojem.<br><br>Zaškrtněte <b>Datumy</b> a <b>Svátky</b>.`,
        [_.manageDatesStatusNotAvailable]: `<div class="hero">Model již není dostupný.</div> Zkuste aplikaci restartovat.`,
        [_.manageDatesStatusOk]: `<div class="hero">Model <b>je kompatibilní s funkcí Spravovat data</b>.</div>Můžete vytvářet nové tabulky dat, aniž byste se museli obávat porušení opatření nebo přehledů.`,
        [_.manageDatesSuccessSceneMessage]: "Gratulujeme, váš model byl úspěšně aktualizován.",
        [_.manageDatesTemplateFirstDayOfWeek]: "První den v týdnu",
        [_.manageDatesTemplateFirstDayOfWeekDesc]: "Pro Týdenní ISO nastavte <em>Pondělí</em>.",
        [_.manageDatesTemplateFirstFiscalMonth]: "První měsíc v roce",
        [_.manageDatesTemplateFirstFiscalMonthDesc]: "Pro Týdenní ISO nastavte <em>Leden</em>.",
        [_.manageDatesTemplateMonthsInYear]: "Měsíce v Roce",
        [_.manageDatesTemplateMonthsInYearDesc]: "",
        [_.manageDatesTemplateNameConfig01]: "Standard",
        [_.manageDatesTemplateNameConfig02]: "Měsíční",
        [_.manageDatesTemplateNameConfig03]: "Vlastní",
        [_.manageDatesTemplateNameConfig04]: "Týdenní",
        [_.manageDatesTemplateQuarterWeekType]: "Týdenní systém",
        [_.manageDatesTemplateQuarterWeekTypeDesc]: "",
        [_.manageDatesTemplateTypeStartFiscalYear]: "Datum definování fiskálního roku",
        [_.manageDatesTemplateTypeStartFiscalYearDesc]: "Pro Týdenní ISO nastavte <em>Poslední v roce</em>.",
        [_.manageDatesTemplateTypeStartFiscalYearFirst]: "První v roce",
        [_.manageDatesTemplateTypeStartFiscalYearLast]: "Poslední v roce",
        [_.manageDatesTemplateWeeklyType]: "Poslední všední den v roce",
        [_.manageDatesTemplateWeeklyTypeDesc]: "Pokud váš týden začíná v neděli, pak posledním dnem v týdnu je sobota. Pokud zvolíte <em>Poslední den roku</em>, fiskální rok končí poslední sobotu posledního měsíce. V opačném případě končí fiskální rok v sobotu, která je nejblíže poslednímu dni posledního měsíce – může to být i v příštím roce. Pro Týdenní ISO nastavte <em>Nejblíže ke konci roku</em>.",
        [_.manageDatesTemplateWeeklyTypeLast]: "Poslední v roce",
        [_.manageDatesTemplateWeeklyTypeNearest]: "Nejblíže ke konci roku",
        [_.manageDatesTimeIntelligenceDesc]: "Vytvořte nejběžnější funkce Time Intelligence DAX dostupné ve vašem modelu.",
        [_.manageDatesTimeIntelligenceEnabledDesc]: "",
        [_.manageDatesTimeIntelligenceEnabledName]: "Time Intelligence Funkce",
        [_.manageDatesTimeIntelligenceTargetMeasuresAll]: "Všechny Measures",
        [_.manageDatesTimeIntelligenceTargetMeasuresChoose]: "Vyberte measures...",
        [_.manageDatesTimeIntelligenceTargetMeasuresDesc]: "Vyberte measure použitou ke generování funkcí Time Intelligence.",
        [_.manageDatesTimeIntelligenceTargetMeasuresName]: "Vybrané Measures",
        [_.manageDatesYearRange]: "Datumové rozmezí",
        [_.manageDatesYearRangeDesc]: "Vyberte, jak určit interval dat modelu. Chcete-li použít automatické skenování, ponechte pole <em>První rok</em> a/nebo <em>Poslední rok</em> prázdné.",
        [_.menuCtrlTitle]: "Sbalit/rozbalit nabídku",
        [_.minimizeCtrlTitle]: "Minimalizovat",
        [_.modelLanguage]: "Jazyk modelu ({culture})",
        [_.more]: "Vice",
        [_.notificationCtrlTitle]: "Upozornení",
        [_.notificationsTitle]: "{count} upozornění",
        [_.openSourcePayoff]: "{appName} je open-source nástroj vyvinutý a spravovaný SQLBI a komunitou Github. Připojte se k nám na",
        [_.openWithDaxFormatterCtrlTitle]: "Formátujte online pomocí DAX Formatter",  
        [_.optionAccount]: "Power BI Účet",
        [_.optionAccountDescription]: "Nastavte účet pro přístup k online datovým sadám Power BI.",
        [_.optionBrowserAuthentication]: "Autentizovat v prohlížeči",
        [_.optionBrowserAuthenticationDescription]: "Autentizovat pomocí výchozího prohlížeče.Toto je alternativní metoda přihlášení, která je užitečná pro řešení problémů se dvoufaktorovou ověřováním.",
        [_.optionCheckForUpdates]: "Automaticky zkontrolujte aktualizace",
        [_.optionDiagnostic]: "Diagnostická úroveň",
        [_.optionDiagnosticDescription]: "Zobrazit chyby a protokoly v podokně diagnostiky. Zvolte <em>Základní</em>, chcete-li protokolovat pouze několik zpráv. <em>Úplný</em> protokoluje všechny zprávy.",
        [_.optionDiagnosticLevelBasic]: "Základní",
        [_.optionDiagnosticLevelNone]: "Žádný",
        [_.optionDiagnosticLevelVerbose]: "Úplný",
        [_.optionDiagnosticMore]: "Chcete-li nahlásit problém s aplikací, prosím použijte",
        [_.optionFormattingBreaks]: "Zalomení názvu-výrazu",
        [_.optionFormattingBreaksAuto]: "Auto",
        [_.optionFormattingBreaksDescription]: "Vyberte, jak oddělit název míry a výraz. Nastavením <em>Automaticky</em> automaticky určíte styl použitý v modelu.",
        [_.optionFormattingBreaksInitial]: "Zalomení řádku",
        [_.optionFormattingBreaksNone]: "Stejný řádek",
        [_.optionFormattingIncludeTimeIntelligence]: "Zahrnout časovou inteligenci",
        [_.optionFormattingIncludeTimeIntelligenceDescription]: "Zahrňte opatření vytvořená automaticky správou dat pro časovou inteligenci.",
        [_.optionFormattingLines]: "Řádky",
        [_.optionFormattingLinesDescription]: "Zvolte, zda budou řádky krátké nebo dlouhé.",
        [_.optionFormattingLinesValueLong]: "Dlouhé řádky",
        [_.optionFormattingLinesValueShort]: "Krátké řádky",
        [_.optionFormattingPreview]: "Automatický náhled",
        [_.optionFormattingPreviewDescription]: "Automaticky posílat measures do DAX Formatter pro generování náhledů.",
        [_.optionFormattingSeparators]: "Oddělovače",
        [_.optionFormattingSeparatorsDescription]: "Vyberte oddělovače pro čísla a seznamy.",
        [_.optionFormattingSeparatorsValueAuto]: "Auto",
        [_.optionFormattingSeparatorsValueEU]: "A; B; C; 1234,00",
        [_.optionFormattingSeparatorsValueUS]: "A, B, C, 1234.00",
        [_.optionFormattingSpaces]: "Mezery",
        [_.optionFormattingSpacesDescription]: "Spravujte mezery za názvy funkcí.",
        [_.optionFormattingSpacesValueBestPractice]: "Best practice",
        [_.optionFormattingSpacesValueFalse]: "Bez mezery - IF( ",
        [_.optionFormattingSpacesValueTrue]: "S mezerami - IF ( ",
        [_.optionInvalidValue]: "Neplatná hodnota",
        [_.optionLanguage]: "Jazyk",
        [_.optionLanguageDescription]: "Vyberte jazyk Bravo. Je nutné znovu načíst.",
        [_.optionLanguageResetConfirm]: "Chcete znovu načíst Bravo, abyste jej měli se zvoleným jazykem?",
        [_.optionPolicyNotice]: "Tato možnost je řízena správcem systému.",
        [_.optionProxyAddress]: "Adresa serveru proxy",
        [_.optionProxyAddressDescription]: "Uveďte adresu serveru proxy.",
        [_.optionProxyBypassList]: "Vyloučit seznam adres",
        [_.optionProxyBypassListDescription]: "Použijte server proxy s výjimkou adres, které začínají vloženými položkami.Použijte polokolony (;) k oddělení položek.",
        [_.optionProxyBypassOnLocal]: "Obcházky místních adres",
        [_.optionProxyBypassOnLocalDescription]: "Nepoužívejte proxy s místními (intranetovými) adresami.",
        [_.optionProxyConfirmDeleteCredentials]: "Určitě odstraníte vlastní přihlašovací údaje ze systému?",
        [_.optionProxyCustomCredentials]: "Vlastní pověření",
        [_.optionProxyCustomCredentialsDescription]: "K ověření na server proxy použijte vlastní přihlašovací údaje.Nechte se používat přihlašovací údaje systému.",
        [_.optionProxyCustomCredentialsEdit]: "Upravit vlastní přihlašovací údaje",
        [_.optionProxyType]: "Proxy server",
        [_.optionProxyTypeCustom]: "Zvyk",
        [_.optionProxyTypeDescription]: "Vyberte server proxy.",
        [_.optionProxyTypeNone]: "Žádný",
        [_.optionProxyTypeSystem]: "Systém",
        [_.optionsDialogAboutMenu]: "O aplikaci",
        [_.optionsDialogFormattingMenu]: "Formátování",
        [_.optionsDialogGeneralMenu]: "Obecné",
        [_.optionsDialogProxyMenu]: "Proxy",
        [_.optionsDialogTelemetryMenu]: "Diagnostika",
        [_.optionsDialogTitle]: "Nastavení",
        [_.optionTelemetry]: "Telemetrie",
        [_.optionTelemetryDescription]: "Odesílejte anonymní data o využití do SQLBI.",
        [_.optionTelemetryMore]: "Pomozte nám pochopit, jak používáte Bravo a jak jej vylepšit. Neshromažďují se žádné osobní údaje. Upozorňujeme, že pokud je tato možnost zakázána, vývojový tým nebude moci shromažďovat žádné neošetřené chyby a poskytovat další informace nebo podporu.",
        [_.optionTheme]: "Téma",
        [_.optionThemeDescription]: "Nastavte téma Bravo. Ponechte <em>Systém</em> a nechte vybrat váš operační systém.",
        [_.optionThemeValueAuto]: "Systém",
        [_.optionThemeValueDark]: "Tmavý",
        [_.optionThemeValueLight]: "Světlý",
        [_.otherColumnsRowName]: "Menší sloupce...",
        [_.paste]: "Vložit",
        [_.powerBiObserving]: "Čekání na otevření souboru v Power BI Desktopu...",
        [_.powerBiObservingCancel]: "Zrušit",
        [_.powerBiSigninDescription]: "Přihlaste se ke službě Power BI a připojte Bravo k vašim online datovým sadám.",
        [_.powerBiSigninTitle]: "Power BI",
        [_.quickActionAttachPBITitle]: "Připojit na Power BI Desktop",
        [_.quickActionConnectPBITitle]: "Připojit do Power BI Service",
        [_.quickActionOpenVPXTitle]: "Otevřít soubor Vertipaq Analyzeru",
        [_.refreshCtrlTitle]: "Obnovit",
        [_.refreshPreviewCtrlTitle]: "Obnovit náhled",
        [_.saveVpaxCtrlTile]: "Uložit jako VPAX",
        [_.savingVpax]: "Vytvářím soubor VPAX...",
        [_.sceneUnsupportedReason]: "Tato funkce není pro tento zdroj dat dostupná.",
        [_.sceneUnsupportedReasonManageDatesAutoDateTimeEnabled]: `Modely s povolenou možností automatického data/času nejsou podporovány.<br><span class="link" href="https://www.sqlbi.com/tv/disabling-auto-date-time-in-power-bi /">Zakázání automatického data a času v Power BI (video)</span>`,
        [_.sceneUnsupportedReasonManageDatesEmptyTableCollection]: "Funkce je podporována pouze databázemi, které mají alespoň jednu tabulku.",
        [_.sceneUnsupportedReasonManageDatesPBIDesktopModelOnly]: "Tuto funkci podporují pouze modely v režimu Power BI Desktop.",
        [_.sceneUnsupportedReasonMetadataOnly]: "Databáze byla vygenerována ze souboru VPAX, který obsahuje pouze jeho metadata.",
        [_.sceneUnsupportedReasonReadOnly]: "Připojení k této databázi je v režimu pouze pro čtení.",
        [_.sceneUnsupportedReasonXmlaEndpointNotSupported]: "Koncový bod XMLA není pro tuto datovou sadu podporován.",
        [_.sceneUnsupportedTitle]: "Nepodporováno",
        [_.searchColumnPlaceholder]: "Vyhledat sloupec",
        [_.searchDatasetPlaceholder]: "Vyhledat datovou sadu",
        [_.searchEntityPlaceholder]: "Vyhledat tabulku/sloupec",
        [_.searchMeasurePlaceholder]: "Vyhledat measure",
        [_.searchPlaceholder]: "Hledat",
        [_.searchTablePlaceholder]: "Vyhledat tabulku",
        [_.settingsCtrlTitle]: "Možnosti",
        [_.sheetOrphan]: "Není dostupný",
        [_.sheetOrphanPBIXTooltip]: "Sestava byla uzavřena v Power BI Desktopu. Jakákoli operace zápisu je zakázána.",
        [_.sheetOrphanTooltip]: "Model již není dostupný. Jakákoli operace zápisu je zakázána.",
        [_.showDiagnosticPane]: "Ukázat detaily",
        [_.sideCtrlTitle]: "Přepnout zobrazení vedle sebe",
        [_.signedInCtrlTitle]: "Přihlášen jako {name}",
        [_.signIn]: "Přihlásit",
        [_.signInCtrlTitle]: "Přihlásit",
        [_.signOut]: "Odhlásit",
        [_.sqlbiPayoff]: "Bravo je projekt SQLBI.",
        [_.syncCtrlTitle]: "Synchronizovat",
        [_.tableColCardinality]: "Kardinalita",
        [_.tableColCardinalityTooltip]: "Počet unikátních elementů",
        [_.tableColColumn]: "Sloupec",
        [_.tableColColumns]: "Sloupce",
        [_.tableColMeasure]: "Measure",
        [_.tableColPath]: "Tabulka \\ Sloupec",
        [_.tableColRows]: "Řádek",
        [_.tableColSize]: "Velikost",
        [_.tableColTable]: "Tabulka",
        [_.tableColWeight]: "Velikost",
        [_.tableSelectedCount]: "{count} vybraný",
        [_.tableValidationInvalid]: "Tento název nelze použít",
        [_.tableValidationValid]: "Tento název je použitelný",
        [_.themeCtrlTitle]: "Změnit téma",
        [_.toggleTree]: "Přepnout strom",
        [_.traceId]: "ID stopy",
        [_.unknownMessage]: "Byla přijata neplatná zpráva",
        [_.updateChannelBeta]: "Beta",
        [_.updateChannelCanary]: "Prvotina",
        [_.updateChannelDev]: "Vývoj",
        [_.updateChannelStable]: "Stabilní", 
        [_.updateMessage]: "K dispozici je nová verze Bravo: {version}",
        [_.validating]: "Ověřování...",
        [_.version]: "Verze",
        [_.welcomeHelpText]: "Podívejte se na videa níže a zjistěte, jak používat Bravo:",
        [_.welcomeHelpTitle]: "Jak používat Bravo?",
        [_.welcomeText]: "Bravo je šikovná sada nástrojů Power BI, kterou můžete použít k analýze modelů, formátování measures, vytváření tabulek s daty a exportu dat.",
        [_.whitespacesTitle]: "Mezery",
        [_.wrappingTitle]: "Automatické zalamování slov",
    }
}
export default locale;
