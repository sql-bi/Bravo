/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Locale } from '../i18n';
import { strings as _ } from '../strings';

const locale: Locale = {

    locale: "pl", //DO NOT TRANSLATE
    enName: "Polish", //DO NOT TRANSLATE
    name: "Polski",

    strings: {
        [_.addCtrlTitle]: "Otwórz",
        [_.aggregatedTableName]: "Wiele tabel",
        [_.AnalyzeModel]: "Analizuj model",
        [_.analyzeModelSummary]: `Twój zestaw danych waży <strong>{size:bytes}</strong> i zawiera <strong>{count}</strong> {{kolumn|kolumnę}}`,
        [_.analyzeModelSummary2]: `, <span class="text-highlight">do <strong>{count}</strong> z nich nie ma odniesienia w modelu.</span>`,
        [_.appName]: "Bravo for Power BI", //DO NOT TRANSLATE
        [_.appUpdateAvailable]: "Dostępna jest nowa wersja: {version}",
        [_.appUpdateChangelog]: "Changelog",
        [_.appUpdateDownload]: "Download",
        [_.appUpdateViewDetails]: "Zobacz szczegóły",
        [_.appUpToDate]: "Bravo jest aktualne",
        [_.appVersion]: "Wersja {version}",
        [_.backupReminder]: "Przed kontynuowaniem zapisz kopię Twojego raportu - <b>niektórych zmian nie będziesz w stanie cofnąć</b>.",
        [_.BestPractices]: "Dobre praktyki",
        [_.canceled]: "Anulowane",
        [_.changeStatusAdded]: "A",
        [_.changeStatusAddedTitle]: "Dodane",
        [_.changeStatusDeleted]: "D",
        [_.changeStatusDeletedTitle]: "Usunięte",
        [_.changeStatusModified]: "M",
        [_.changeStatusModifiedTitle]: "Zmodyfikowane",
        [_.clearCtrlTitle]: "Wyczyść",
        [_.closeCtrlTitle]: "Zamknij",
        [_.closeOtherTabs]: "Zamknij pozostałe",
        [_.closeTab]: "Zamknij",
        [_.collapseAllCtrlTitle]: "Zwiń wszystko",
        [_.columnExportedCompleted]: "Ta tabela została wyeksportowana.",
        [_.columnExportedFailed]: "Ta tabela nie została wyeksportowana z powodu nieokreślonego błędu.",
        [_.columnExportedTruncated]: "Ta tabela została skrócona ponieważ liczba wierszy przekroczyła maksymalną dozwoloną..",
        [_.columnMeasureFormatted]: "Ta miara jest już sformatowana.",
        [_.columnMeasureNotFormattedTooltip]: "Ta miara nie jest sformatowana.",
        [_.columnMeasureWithError]: "Ta miara zawiera błędy.",
        [_.columnUnreferencedExplanation]: `<span class="text-highlight">Kolumny bez odniesienia</span> co do zasady mogą zostać usunięte z modelu aby poprawić jego wydajność. Przed usunieciem upewnij się, że nie używasz tych kolumn w swoich raportach - Bravo nie jest w stanie tego samodzielnie określić.`,
        [_.columnUnreferencedTooltip]: "Ta kolumna nie ma odniesienia w Twoim modelu.",
        [_.confirmTabCloseMessage]: "Wygląda na to, że nie zapisałeś zmian.<br>Na pewno chcesz zamknąć?",
        [_.connectBrowse]: "Przeglądaj",
        [_.connectDatasetsTableEndorsementCol]: "Poparcie",
        [_.connectDatasetsTableNameCol]: "Nazwa",
        [_.connectDatasetsTableOwnerCol]: "Właściciel",
        [_.connectDatasetsTableWorkspaceCol]: "Obszar roboczy",
        [_.connectDialogAttachPBIMenu]: "Zestawy danych na powerbi.com",
        [_.connectDialogConnectPBIMenu]: "Aktywne raporty w Power BI Desktop",
        [_.connectDialogOpenVPXMenu]: "Pliki VPAX",
        [_.connectDialogTitle]: "Otwórz",
        [_.connectDragFile]: "Przeciągnij plik VPAX tutaj<br>albo znajdź na swoim komputerze",
        [_.connectNoReports]: "Nie znaleziono aktywnych raportów.<br>Otwórz raport w Power BI Desktop i zaczekaj na jego pojawienie się tutaj.",
        [_.copiedErrorDetails]: "Skopiowane!",
        [_.copy]: "Kopiuj",
        [_.copyErrorDetails]: "Błąd kopiowania",
        [_.copyFormulaCtrlTitle]: "Kopiuj sformatowaną miarę",
        [_.copyMessage]: "Kopiuj wiadomość",
        [_.copyright]: "Wszystkie prawa zastrzeżone.",
        [_.createIssue]: "Zgłoś problem",
        [_.cut]: "Wytnij",
        [_.dataUsageLink]: "W jaki sposób są używane Twoje dane?", 
        [_.dataUsageMessage]: `W celu sformatowania Twojego kodu Bravo wysyła za pomocą bezpiecznego połączenia miary do DAX Formatter, usługi zarządzanej przez SQLBI.<p><strong>Usługa nie zachowuje Twoich danych.</strong></p><p>Niektóre informacje jak na przykład najczęściej używane funkcje DAX, wskaźnik złożoności i przeciętny rozmiar kwerendy mogą zostać zachowane do celów statystycznych.</p>`,
        [_.dataUsageTitle]: "Jak Twoje dane są używane?",
        [_.DaxFormatter]: "Formatuj DAX",
        [_.daxFormatterAgreement]: "Aby sformatować DAX Bravo wysyła Twoje miary do usługi DAX Formatter.",
        [_.daxFormatterAnalyzeConfirm]: "Aby przeprowadzić analizę Bravo musi wysłać wszystkie miary do usługi DAX Formatter. Czy chcesz kontynować?",
        [_.daxFormatterAutoPreviewOption]: "Automatyczny podgląd",
        [_.daxFormatterFormat]: "Formatuj wybrane",
        [_.daxFormatterFormatDisabled]: "Formatuj (nieobsługiwane)",
        [_.daxFormatterFormattedCode]: "Sformatowane (podgląd)",
        [_.daxFormatterOriginalCode]: "Bieżące",
        [_.daxFormatterPreviewAllButton]: "Podgląd dla wszystkich miar",
        [_.daxFormatterPreviewButton]: "Podgląd",
        [_.daxFormatterPreviewDesc]: "Aby wygenerować podgląd Bravo musi wysłać tę miarę do usługi DAX Formatter.",
        [_.daxFormatterSuccessSceneMessage]: "Gratulacje! Liczba miar sofrmatowanych prawidłowo: <strong>{count}</strong>.",
        [_.daxFormatterSummary]: `Liczba miar w Twoim zestawie danych - {count}: <span class="text-error"><strong>{errors:number}</strong> zawiera błędy</strong></span>, <span class="text-highlight"><strong>{formattable:number}</strong> do sformatowania</span>, <strong>{analyzable:number}</strong> do analizy (<span class="link manual-analyze">analizuj teraz</span>).`,
        [_.daxFormatterSummaryNoAnalysis]: `Liczba miar w Twoim zestawie danych - <strong>{count}</strong>: <span class="text-error"><strong>{errors:number}</strong> z błędami</strong></span> i <span class="text-highlight"><strong>{formattable:number}</strong> do sformatowania.</span>`,
        [_.defaultTabName]: "Bez nazwy",
        [_.dialogCancel]: "Anuluj",
        [_.dialogOK]: "OK",
        [_.dialogOpen]: "Otwórz",
        [_.docLimited]: "Ograniczone",
        [_.docLimitedTooltip]: "Nie wszystkie funkcje są obecnie dostępne.",
        [_.doneCtrlTitle]: "Gotowe",
        [_.emailAddress]: "Adres email",
        [_.emailAddressPlaceholder]: "Podaj adres email",
        [_.error]: "Błąd",
        [_.errorAborted]: "Zadanie przerwane.",
        [_.errorAnalysisServicesConnectionFailed]: "Napotkaliśmy na błąd połączenia między serwerem a Bravo.",
        [_.errorCheckForUpdates]: "Nie możemy sprawdzić aktualizacji - serwer jest nieosiągalny.",
        [_.errorConnectionUnsupported]: "Połączenie z wybranym zasobem nie jest obsługiwane.",
        [_.errorDatasetConnectionUnknown]: "Nieokreślone połączenie..",
        [_.errorDatasetsEmptyListing]: "Brak dostepnych otwartych raportów.",
        [_.errorDatasetsListing]: "Nie możemy pobrać listy zestawów danych z Usługi Power BI.",
        [_.errorExportDataFileError]: "Nie udało się wyeksportować danych. Spróbuj ponownie.",
        [_.errorManageDateTemplateError]: "Wystąpił wyjątek podczas wykonywania silnika szablonów DAX.",
        [_.errorNetworkError]: "Nie masz połączenia z internetem.",
        [_.errorNone]: "Nieokreślony błąd.",
        [_.errorNotAuthorized]: "Nie masz uprawnień do wglądu w wybrane zasoby.",
        [_.errorNotConnected]: "Nie jesteś połączony z Power BI - zaloguj się aby kontynuować.",
        [_.errorNotFound]: "Nie możemy połączyć się z wybranym zasobem.",
        [_.errorReportConnectionUnknown]: "Nieprawidłowe połączenie.",
        [_.errorReportConnectionUnsupportedAnalysisServecesCompatibilityMode]: "Instancja Power BI Desktop Analysis Services nie jest PowerBI.",
        [_.errorReportConnectionUnsupportedAnalysisServecesConnectionNotFound]: "Nie znaleziono połączenia TCP z Power BI Desktop Analysis Services.",
        [_.errorReportConnectionUnsupportedAnalysisServecesProcessNotFound]: "Nie znaleziono instancji połączenia z Power BI Desktop Analysis Services.", 
        [_.errorReportConnectionUnsupportedConnectionException]: "Połączenie z instancją Power BI Desktop Analysis Services uruchomiło wyjątek.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionIsEmpty]: "Instancja Power BI Desktop Analysis Services nie zawiera żadnych baz danych. Spróbuj połączyć się z raportem używając ikony Bravo w zakładce narzędzia zewnętrzne Power BI Desktop.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionUnexpectedCount]: "Instancja Power BI Desktop Analysis Services zawiera nieoczekiwaną liczbę baz danych (> 1) podczas gdy spodziewaliśmy się żadnej lub jednej.",
        [_.errorReportConnectionUnsupportedProcessNotYetReady]: "Power BI Desktop uruchamia się lub instancja Analysis Services nie jest jeszcze gotowa.", 
        [_.errorReportsEmptyListing]: "Brak dostępnych, nieotwartych raportów.",
        [_.errorRetry]: "Spróbuj ponownie",
        [_.errorSignInMsalExceptionOccurred]: "Nieoczekiwany błąd w trakcie logowania.",
        [_.errorSignInMsalTimeoutExpired]: "Logowanie zostało anulowane ponieważ czas opracji przekroczył dozwolony.",
        [_.errorTimeout]: "Limit czasu żądania.",
        [_.errorTitle]: "Whoops...",
        [_.errorTOMDatabaseDatabaseNotFound]: "Baza danych nie istnieje w kolekcji lub użytkownik nie posiada praw dostępu administratora.",
        [_.errorTOMDatabaseUpdateConflictMeasure]: "Oczekiwana aktualizacja konfliktuje z obecnym stanem zasobu docelowego.",
        [_.errorTOMDatabaseUpdateErrorMeasure]: "Operacja nieudana ponieważ jedna lub więcej miar zawiera błędy.", 
        [_.errorTOMDatabaseUpdateFailed]: "Operacja nieudana w trakcie zapisywania lokalnych zmian do modelu na serwerze.",
        [_.errorTryingToUpdateMeasuresWithErrors]: `Operacja nieudana ponieważ nastepujące miary zawierają błędy:<br><strong>{measures}</strong>`,
        [_.errorUnhandled]: "Nieobsługiwany błąd - prosimy zgłoś go podając trace id jeżeli dostępne.",
        [_.errorUnspecified]: "Nieokreślony błąd.",
        [_.errorUserSettingsSaveError]: "Nie możemy zapisać ustawień.",
        [_.errorVpaxFileImportError]: "Podczas importowania pliku VPAX wystąpił błąd.",
        [_.errorVpaxFileExportError]: "Podczas eksportu pliku VPAX wystąpił błąd.",
        [_.expandAllCtrlTitle]: "Rozwiń wszystko",
        [_.ExportData]: "Eksportuj dane",
        [_.exportDataCSVCustomDelimiter]: "Niestandardowy ogranicznik",
        [_.exportDataCSVDelimiter]: "Ogranicznik",
        [_.exportDataCSVDelimiterComma]: "Przecinek",
        [_.exportDataCSVDelimiterDesc]: `Wybierz znak służący jako ogranicznik pól. <em>Automatyczny</em> używa domyślnego znaku zgodnego z kodem kulturowym Twojego systemu.`,
        [_.exportDataCSVDelimiterOther]: "Inne...",
        [_.exportDataCSVDelimiterPlaceholder]: "Znak",
        [_.exportDataCSVDelimiterSemicolon]: "Średnik",
        [_.exportDataCSVDelimiterSystem]: "Automatyczny",
        [_.exportDataCSVDelimiterTab]: "Tab",
        [_.exportDataCSVEncoding]: "Kodowanie",
        [_.exportDataCSVEncodingDesc]: "",
        [_.exportDataCSVQuote]: "Umieść tekst w cudzysłowie",
        [_.exportDataCSVQuoteDesc]: "Upewnij się, że każdy tekst umieszczony jest w cudzysłowie.",
        [_.exportDataExcelCreateExportSummary]: "Eksportuj podsumowanie",
        [_.exportDataExcelCreateExportSummaryDesc]: "Dodaj nowy skoroszyt do eksportu zawierający podsumowanie zadania.",
        [_.exportDataExport]: "Eksportuj wybrane",
        [_.exportDataExportAs]: "Eksportuj jako",
        [_.exportDataExportAsDesc]: "",
        [_.exportDataExporting]: "Eksportuję {table}...",
        [_.exportDataExportingDone]: "Gotowe!",
        [_.exportDataNoColumns]: "Tej tabeli nie można eksportować, ponieważ nie zawiera ona żadnych kolumn.",
        [_.exportDataNotQueryable]: "Ta tabela nie może zostać wyeksportowana, ponieważ zawiera jedną lub więcej kolumn obliczonych za pomocą nieprawidłowego wyrażenia lub kolumn, które wymagają aktualizacji (tzn. ponownego obliczenia).",
        [_.exportDataOpenFile]: "Otwórz wyeksportowany plik",
        [_.exportDataOpenFolder]: "Otwórz folder eksportu",
        [_.exportDataOptions]: "Ustawienia eksportu",
        [_.exportDataStartExporting]: "Inicjalizuję...",
        [_.exportDataSuccessSceneMessage]: "Liczba tabel prawidłowo wyeksportowanych: <strong>{count}/{total}.",
        [_.exportDataSummary]: "Liczba tabel, których nie mogliśmy eksportować: <strong>{count} table{{s}}</strong>.",
        [_.exportDataTypeCSV]: "CSV (wartości rozdzielone przecinkami)",
        [_.exportDataTypeXLSX]: "Arkusz Excel",
        [_.failed]: "Niepowodzenie",
        [_.filterMeasuresWithErrorsCtrlTitle]: "Pokaż niesformatowane miary zawierające błędy.",
        [_.filterUnrefCtrlTitle]: "Pokaż tylko kolumny bez odniesienia",
        [_.formattingMeasures]: "Formatuję miary...",
        [_.goBackCtrlTitle]: "Anuluj i wróć",
        [_.groupByTableCtrlTitle]: "Grupuj po tabelach",
        [_.helpConnectVideo]: "Jak połączyć",
        [_.helpCtrlTitle]: "Pomoc",
        [_.hideUnsupportedCtrlTitle]: "Tylko obsługiwane",
        [_.less]: "Mniej",
        [_.license]: "Udostępnione na licencji MIT.",
        [_.loading]: "Ładuję...",
        [_.ManageDates]: "Zarządzaj datami",
        [_.manageDatesApplyCtrlTitle]: "Zastosuj zmiany",
        [_.manageDatesAuto]: "Auto",
        [_.manageDatesAutoScan]: "Autmatyczne skanowanie",
        [_.manageDatesAutoScanActiveRelationships]: "Aktywne relacje",
        [_.manageDatesAutoScanDesc]: "Wybierz <em>Pełne</em> aby przeskanować wszystkie kolumny zawierające daty. Wybierz <em>Wybierz kolumny...</em> aby wybrać kolumny do użycia. Wybierz <em>Aktywne relacje</em> i <em>Niekatywne relacje</em> aby przeskanować tylko kolumny z relacjami.",
        [_.manageDatesAutoScanDisabled]: "Wyłączone",
        [_.manageDatesAutoScanFirstYear]: "Rok początkowy",
        [_.manageDatesAutoScanFirstYearDesc]: "",
        [_.manageDatesAutoScanFull]: "Pełne",
        [_.manageDatesAutoScanInactiveRelationships]: "Nieaktywne relacje",
        [_.manageDatesAutoScanLastYear]: "Rok końcowy",
        [_.manageDatesAutoScanLastYearDesc]: "",
        [_.manageDatesAutoScanSelectedTablesColumns]: "Wybierz kolumny...",
        [_.manageDatesBrowserPlaceholder]: "Brak elementów do zmiany",
        [_.manageDatesCalendarDesc]: "Wybierz szablon kalendarza, który ma być zastosowany do tego modelu. Bravo utworzy wymagane tabele lub zaktualizuje je, zachowując istniejące relacje.", 
        [_.manageDatesCalendarTemplateName]: "Szablon kalendarza",
        [_.manageDatesCalendarTemplateNameDesc]: "Wybierz <em>Miesięczny</em> dla kalendarza opartego o miesiące. Wybierz <em>Tygodniowy</em> dla kalendarza 445-454-544-ISO. Wybierz <em>Niestandardowy</em> dla innych kalendarzy o zmiennej długości.",
        [_.manageDatesCreatingTables]: "Aktualizuję model...",
        [_.manageDatesDatesDesc]: "Skonfiguruj format i położenie dat w modelu.",
        [_.manageDatesDatesTableDesc]: "To jest abela, której należy używać w raportach dotyczących dat.",
        [_.manageDatesDatesTableName]: "Tabela dat",
        [_.manageDatesDatesTableReferenceDesc]: "To jest ukryta tabela zawierająca wszystkie funkcje DAX używane do generowania dat.",
        [_.manageDatesDatesTableReferenceName]: "Tabela definicji dat",
        [_.manageDatesHolidaysDesc]: "Dodaj święta do swojego modelu. Bravo utworzy wymagane tabele lub zaktualizuje je, zachowując istniejące relacje. ",
        [_.manageDatesHolidaysEnabledDesc]: "Dodaj do modelu tabelę świąt.",
        [_.manageDatesHolidaysEnabledName]: "Święta",
        [_.manageDatesHolidaysTableDefinitionDesc]: "To jest ukryta tabela zawierająca wszystkie funkcje DAX używane do generowania świąt.",
        [_.manageDatesHolidaysTableDefinitionName]: "Tabela definicji świąt",
        [_.manageDatesHolidaysTableDesc]: "To jest tabela, której należy używać w raportach dotyczących świąt.",
        [_.manageDatesHolidaysTableName]: "Tabela świąt",
        [_.manageDatesIntervalDesc]: "Wybierz interwał dat dla Twojego modelu.",
        [_.manageDatesISOCountryDesc]: "",
        [_.manageDatesISOCountryName]: "Święta w kraju",
        [_.manageDatesISOCustomFormatDesc]: "Wprowadź format regionalny, używając znacznika IETF BCP 47. Np. en-US",
        [_.manageDatesISOCustomFormatName]: "Niestandardowy format",
        [_.manageDatesISOFormatDesc]: "Wybierz formatowanie regionalne dat.",
        [_.manageDatesISOFormatName]: "Formatowanie regionalne",
        [_.manageDatesISOFormatOther]: "Inne...",
        [_.manageDatesISOFormatOtherPlaceholder]: "Region",
        [_.manageDatesMenuCalendar]: "Kalendarz",
        [_.manageDatesMenuDates]: "Daty",
        [_.manageDatesMenuHolidays]: "Święta",
        [_.manageDatesMenuInterval]: "Interwał",
        [_.manageDatesMenuPreviewCode]: "Wyrażenie",
        [_.manageDatesMenuPreviewModel]: "Zmiany w modelu",
        [_.manageDatesMenuPreviewTable]: "Przykładowe dane",
        [_.manageDatesMenuPreviewTreeDate]: "Daty",
        [_.manageDatesMenuPreviewTreeDateHolidays]: "Daty i święta",
        [_.manageDatesMenuPreviewTreeTimeIntelligence]: "Analiza czasowa",
        [_.manageDatesMenuTimeIntelligence]: "Analiza czasowa",
        [_.manageDatesModelCheck]: "Sprawdzenie modelu",
        [_.manageDatesPreview]: "Podgląd",
        [_.manageDatesPreviewCtrlTitle]: "Podejrzyj zmiany",
        [_.manageDatesSampleData]: "Przykładowe dane",
        [_.manageDatesSampleDataError]: "Nie możemy wygenerować przykładowych danych.",
        [_.manageDatesStatusCompatible]: `<div class="hero">Twój model zawiera już <b>tabele dat kompatybilne</b> z Bravo.</div>Jeżeli coś tu zmienisz, tabele zostaną zaktualizowane a ich relacje pozostaną działające.`,
        [_.manageDatesStatusError]: `<div class="hero">Nie możemy zastosować wybranych ustawień.</div>{error}`,
        [_.manageDatesStatusIncompatible]: `<div class="hero">Twój model zawiera <b>tabele dat niekompatybilne</b> z Bravo.</div>Aby dokonać tutaj zmian, musisz wybrać inną nazwę dla jednej lub więcej tabel które zostaną tutaj stworzone.<br><br>Sprawdź <b>Daty</b> i <b>Święta</b>.`,
        [_.manageDatesStatusNotAvailable]: `<div class="hero">Ten model nie jest już dostępny.</div> Spróbuj ponownie uruchomić aplikację.`,
        [_.manageDatesStatusOk]: `<div class="hero">Twój model <b>jest kompatybilny z funkcją zarządzania datami</b>.</div>Możesz stworzyć nowe tabele dat bez wpływu na działanie miar lub raportów.`,
        [_.manageDatesSuccessSceneMessage]: "Gratulacje! Twój model został zaktualizowany.",
        [_.manageDatesTemplateFirstDayOfWeek]: "Pierwszy dzień tygodnia",
        [_.manageDatesTemplateFirstDayOfWeekDesc]: "Dla tygodniowego ISO ustaw <em>Poniedziałek</em>.",
        [_.manageDatesTemplateFirstFiscalMonth]: "Pierwszy miesiąc w roku",
        [_.manageDatesTemplateFirstFiscalMonthDesc]: "Dla tygodniowego ISO ustaw <em>Styczeń</em>.",
        [_.manageDatesTemplateMonthsInYear]: "Miesięcy w roku",
        [_.manageDatesTemplateMonthsInYearDesc]: "",
        [_.manageDatesTemplateNameConfig01]: "Standard",
        [_.manageDatesTemplateNameConfig02]: "Miesięczny",
        [_.manageDatesTemplateNameConfig03]: "Niestandardowy",
        [_.manageDatesTemplateNameConfig04]: "Tygodniowy",
        [_.manageDatesTemplateQuarterWeekType]: "Tygodniowy systemowy",
        [_.manageDatesTemplateQuarterWeekTypeDesc]: "",
        [_.manageDatesTemplateTypeStartFiscalYear]: "Pierwszy dzień roku obrachunkowego",
        [_.manageDatesTemplateTypeStartFiscalYearDesc]: "Dla tygodniowego ISO ustaw <em>Ostatni dzień roku</em>.",
        [_.manageDatesTemplateTypeStartFiscalYearFirst]: "Pierwszy dzień roku",
        [_.manageDatesTemplateTypeStartFiscalYearLast]: "Ostatni dzień roku",
        [_.manageDatesTemplateWeeklyType]: "Ostatni dzień tygodnia roku",
        [_.manageDatesTemplateWeeklyTypeDesc]: "Jeżeli Twój tydzień zaczyna się w niedzielę - ostatnim dniem będzie sobota. Jeżeli wybierzesz <em>Ostatni dzień roku</em> rok obrachunkowy skończy się na ostatniej sobocie ostatniego miesiąca. W przeciwnym wypadku rok obrachunkowy skończy się na sobocie najbliższej ostatniego dnia ostatniego miesiąca - możliwe, że kolejnego roku. Dla tygodniowego ISO ustaw <em>Najbliżej końca roku</em>.",
        [_.manageDatesTemplateWeeklyTypeLast]: "Ostatni w roku",
        [_.manageDatesTemplateWeeklyTypeNearest]: "Najbliżej końca roku",
        [_.manageDatesTimeIntelligenceDesc]: "Utwórz najczęściej używane funkcje analizy czasowej DAX dostępne w Twoim modelu.",
        [_.manageDatesTimeIntelligenceEnabledDesc]: "",
        [_.manageDatesTimeIntelligenceEnabledName]: "Funkcje analizy czasowej",
        [_.manageDatesTimeIntelligenceTargetMeasuresAll]: "Wszystkie miary",
        [_.manageDatesTimeIntelligenceTargetMeasuresChoose]: "Wybierz miary...",
        [_.manageDatesTimeIntelligenceTargetMeasuresDesc]: "Wybierz miarę do wygenerowania funkcji analizy czasowej.",
        [_.manageDatesTimeIntelligenceTargetMeasuresName]: "Miary docelowe",
        [_.manageDatesYearRange]: "Przedział dat",
        [_.manageDatesYearRangeDesc]: "Wybierz sposób określania przedziału dat dla modelu. Zostaw <em>Rok początkowy</em> i/lub <em>Rok końcowy</em> puste, aby użyć automatycznego skanowania.",
        [_.menuCtrlTitle]: "Zwiń/rozwiń menu",
        [_.minimizeCtrlTitle]: "Minimalizuj",
        [_.modelLanguage]: "Język modelu ({culture})",
        [_.more]: "Więcej",
        [_.notificationCtrlTitle]: "Powiadomienia",
        [_.notificationsTitle]: "Liczba powiadomień: {count}",
        [_.openSourcePayoff]: "{appName} jest narzędziem open-source stworzonym i utrzymywanym przez SQLBI i społeczność Github. Dołącz do nas w",
        [_.openWithDaxFormatterCtrlTitle]: "Formatuj online z DAX Formatter",  
        [_.optionAccount]: "Konto Power BI",
        [_.optionAccountDescription]: "Ustaw konto, aby uzyskać dostęp do zbiorów danych Power BI online.",
        [_.optionDiagnostic]: "Poziom diagnostyki",
        [_.optionDiagnosticDescription]: "Pokaż błędy i dzienniki w okienku diagnostycznym. Wybierz <em>Podstawowy</em>, aby rejestrować tylko kilka komunikatów. <em>Szczegółowy</em> rejestruje wszystkie komunikaty",
        [_.optionDiagnosticLevelBasic]: "Podstawowy",
        [_.optionDiagnosticLevelNone]: "Brak",
        [_.optionDiagnosticLevelVerbose]: "Szczegółowy",
        [_.optionDiagnosticMore]: "Aby zgłosić problem z aplikacją, użyj",
        [_.optionFormattingBreaks]: "Name-Expression Breaking",
        [_.optionFormattingBreaksAuto]: "Auto",
        [_.optionFormattingBreaksDescription]: "Wybierz sposób oddzielania nazwy miary i wyrażenia. Ustaw opcję <em>Auto</em>, aby automatycznie określić styl używany w modelu.",
        [_.optionFormattingBreaksInitial]: "Łamanie linii",
        [_.optionFormattingBreaksNone]: "W tej samej linii",
        [_.optionFormattingIncludeTimeIntelligence]: "Obejmują inteligencję czasu",
        [_.optionFormattingIncludeTimeIntelligenceDescription]: "Uwzględnij środki tworzone automatycznie przez zarządzanie datami inteligencji czasu.",
        [_.optionFormattingLines]: "Linie",
        [_.optionFormattingLinesDescription]: "Wybierzy czy linie mają być długie czy krótkie.",
        [_.optionFormattingLinesValueLong]: "Długie linie",
        [_.optionFormattingLinesValueShort]: "Krótkie linie",
        [_.optionFormattingPreview]: "Automatyczny podgląd",
        [_.optionFormattingPreviewDescription]: "Automatyczne wysyłaj miar do DAX Formatter w celu wygenerowania podglądu.",
        [_.optionFormattingSeparators]: "Separatory",
        [_.optionFormattingSeparatorsDescription]: "Wybierz separatory dla liczb i list.",
        [_.optionFormattingSeparatorsValueAuto]: "Auto",
        [_.optionFormattingSeparatorsValueEU]: "A; B; C; 1234,00",
        [_.optionFormattingSeparatorsValueUS]: "A, B, C, 1234.00",
        [_.optionFormattingSpaces]: "Odstępy",
        [_.optionFormattingSpacesDescription]: "Zarządzaj odstepami po nazwach funkcji.",
        [_.optionFormattingSpacesValueBestPractice]: "Najlepsze praktyki",
        [_.optionFormattingSpacesValueFalse]: "Brak spacji - IF( ",
        [_.optionFormattingSpacesValueTrue]: "Spacja - IF ( ",
        [_.optionLanguage]: "Język",
        [_.optionLanguageDescription]: "Wybierz język Bravo. Wymagane ponowne uruchomienie.",
        [_.optionLanguageResetConfirm]: "Czy chcesz uruchomić Bravo ponownie w celu zastosowania nowego języka?",
        [_.optionsDialogAboutMenu]: "Informacje",
        [_.optionsDialogFormattingMenu]: "Formatowanie",
        [_.optionsDialogGeneralMenu]: "Ogólne",
        [_.optionsDialogTelemetryMenu]: "Diagnostyka",
        [_.optionsDialogTitle]: "Opcje",
        [_.optionTelemetry]: "Telemetria",
        [_.optionTelemetryDescription]: "Wyślij anonimowe dane użytkowania do SQLBI.",
        [_.optionTelemetryMore]: "Pomóż nam zrozumieć, w jaki sposób korzystasz z Bravo i jak możemy go ulepszyć. Nie zbieramy żadnych danych osobowych. Pamiętaj, że jeśli ta opcja jest wyłączona, zespół programistów nie będzie mógł zbierać nieobsługiwanych błędów i dostarczać dodatkowych informacji lub pomocy technicznej.",
        [_.optionTheme]: "Motyw",
        [_.optionThemeDescription]: "Ustaw motyw dla Bravo. Pozostaw opcję <em>System</em>, żeby motyw był zgodny z ustawionym w systemie.",
        [_.optionThemeValueAuto]: "System",
        [_.optionThemeValueDark]: "Ciemny",
        [_.optionThemeValueLight]: "Jasny",
        [_.otherColumnsRowName]: "Mniejsze kolumny...",
        [_.paste]: "Wklej",
        [_.powerBiObserving]: "Oczekiwanie na otwarcie pliku w programie Power BI Desktop...",
        [_.powerBiObservingCancel]: "Anuluj",
        [_.powerBiSigninDescription]: "Zaloguj się do usługi Power BI, aby połączyć Bravo z Twoimi zbiorami danych online.",
        [_.powerBiSigninTitle]: "Power BI",
        [_.quickActionAttachPBITitle]: "Zaąłcz do Power BI Desktop",
        [_.quickActionConnectPBITitle]: "Połącz się z usługą Power BI",
        [_.quickActionOpenVPXTitle]: "Otwieranie pliku Vertipaq Analyzer",
        [_.refreshCtrlTitle]: "Odśwież",
        [_.refreshPreviewCtrlTitle]: "Odśwież podgląd",
        [_.saveVpaxCtrlTile]: "Zapisz jako VPAX",
        [_.savingVpax]: "Generuję VPAX...",
        [_.sceneUnsupportedReason]: "Ta funkcja nie jest dostępna dla tego źródła danych.",
        [_.sceneUnsupportedReasonManageDatesAutoDateTimeEnabled]: `Mmodele z włączoną opcją automatycznej daty/czasu nie są obsługiwane.<br><span class="link" href="https://www.sqlbi.com/tv/disabling-auto-date-time-in-power-bi/">Wyłączanie automatycznej daty w Power BI (wideo)</span>`,
        [_.sceneUnsupportedReasonManageDatesEmptyTableCollection]: "This feature is supported only by databases that have at least one table.",
        [_.sceneUnsupportedReasonManageDatesPBIDesktopModelOnly]: "Ta funkcja jest obsługiwana tylko przez bazy danych, które mają co najmniej jedną tabelę.",
        [_.sceneUnsupportedReasonMetadataOnly]: "Baza danych została wygenerowana z pliku VPAX, który zawiera tylko metadane.",
        [_.sceneUnsupportedReasonReadOnly]: "Połączenie z tą bazą danych jest tylko do odczytu.",
        [_.sceneUnsupportedReasonXmlaEndpointNotSupported]: "Punkt końcowy XMLA nie jest obsługiwany dla tego zbioru danych.",
        [_.sceneUnsupportedTitle]: "Nieobsługiwane",
        [_.searchColumnPlaceholder]: "Znajdź kolumnę",
        [_.searchDatasetPlaceholder]: "Znajdź zbiór danych",
        [_.searchEntityPlaceholder]: "Znajdź tabelę/kolumnę",
        [_.searchMeasurePlaceholder]: "Znajdź miarę",
        [_.searchPlaceholder]: "Szukaj",
        [_.searchTablePlaceholder]: "Znajdź tabelę",
        [_.settingsCtrlTitle]: "Opcje",
        [_.sheetOrphan]: "Niedostępne",
        [_.sheetOrphanPBIXTooltip]: "Raport został zamknięty w programie Power BI Desktop. Nie można wykonywać żadnych operacji zapisu.",
        [_.sheetOrphanTooltip]: "Ten model nie jest już dostępny. Nie można wykonywać żadnych operacji zapisu.",
        [_.showDiagnosticPane]: "Pokaż szczegóły",
        [_.sideCtrlTitle]: "Przełączanie widoku side-by-side",
        [_.signedInCtrlTitle]: "Zalogowano jako {name}",
        [_.signIn]: "Zaloguj",
        [_.signInCtrlTitle]: "Zaloguj",
        [_.signOut]: "Wyloguj",
        [_.sqlbiPayoff]: "Bravo jest projektem SQLBI.",
        [_.syncCtrlTitle]: "Synchronizacja",
        [_.tableColCardinality]: "Liczność",
        [_.tableColCardinalityTooltip]: "Liczba unikalnych elementów",
        [_.tableColColumn]: "Kolumna",
        [_.tableColColumns]: "Kolumny",
        [_.tableColMeasure]: "Miara",
        [_.tableColPath]: "Tabela \\ Kolumna",
        [_.tableColRows]: "Wiersze",
        [_.tableColSize]: "Rozmiar",
        [_.tableColTable]: "Tabela",
        [_.tableColWeight]: "Waga",
        [_.tableSelectedCount]: "Wybrano {count}",
        [_.tableValidationInvalid]: "Ta nazwa nie może być użyta.",
        [_.tableValidationValid]: "Nazwa prawidłowa.",
        [_.themeCtrlTitle]: "Zmień motyw",
        [_.toggleTree]: "Przełącz widok drzewa",
        [_.traceId]: "Trace Id",
        [_.unknownMessage]: "Odebrano nieprawidłowy komunikat",
        [_.updateChannelBeta]: "Beta",
        [_.updateChannelCanary]: "Canary",
        [_.updateChannelDev]: "Dev",
        [_.updateChannelStable]: "Stabilna", 
        [_.updateMessage]: "Dostępna jest nowa wersja Bravo: {version}",
        [_.validating]: "Walidacja...",
        [_.version]: "Wersja",
        [_.welcomeHelpText]: "Obejrzyj poniższe filmy, aby dowiedzieć się, jak korzystać z Bravo:",
        [_.welcomeHelpTitle]: "Jak korzystać z Bravo?",
        [_.welcomeText]: "Bravo to poręczny zestaw narzędzi Power BI, który można wykorzystać do analizowania modeli, formatowania miar, tworzenia tabel i eksportowania danych.",
        [_.whitespacesTitle]: "Białe znaki",
        [_.wrappingTitle]: "Automatyczne zawijanie wierszy",
    }
}
export default locale;