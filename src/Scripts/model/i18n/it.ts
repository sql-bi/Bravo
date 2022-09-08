/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Locale } from '../i18n';
import { strings as _ } from '../strings';

const locale: Locale = {
    
    locale: "it", //DO NOT TRANSLATE
    enName: "Italian", //DO NOT TRANSLATE
    name: "Italiano",

    strings: {
        [_.addCtrlTitle]: "Apri",
        [_.aggregatedTableName]: "Tabelle multiple",
        [_.AnalyzeModel]: "Analizza Modello",
        [_.analyzeModelSummary]: `Il tuo dataset è grande <strong>{size:bytes}</strong> e contiene <strong>{count}</strong> {{colonne|colonna}}`,
        [_.analyzeModelSummary2]: `, di cui <span class="text-highlight"><strong>{count}</strong> non {{sono referenziate|è referenziata}} nel modello.</span>`,
        [_.appExtensionName]: "Bravo Template Editor", // DO NOT TRANSLATE
        [_.appName]: "Bravo for Power BI", //DO NOT TRANSLATE
        [_.appUpdateAvailable]: "Nuova versione disponibile: {version}",
        [_.appUpdateChangelog]: "Changelog",
        [_.appUpdateDownload]: "Download",
        [_.appUpdateViewDetails]: "Vedi dettagli",
        [_.appUpToDate]: "Bravo è aggiornato!",
        [_.appVersion]: "Versione {version}",
        [_.backupReminder]: "Prima di procedere, ricorda di eseguire il backup del report: <b>alcune modifiche potrebbero non essere annullabili.</b>.",
        [_.BestPractices]: "Raccomandazioni",
        [_.canceled]: "Annullato",
        [_.changeStatusAdded]: "A",
        [_.changeStatusAddedTitle]: "Aggiunto",
        [_.changeStatusDeleted]: "E",
        [_.changeStatusDeletedTitle]: "Eliminato",
        [_.changeStatusModified]: "M",
        [_.changeStatusModifiedTitle]: "Modificato",
        [_.checking]: "Controllo in corso...",
        [_.clearCtrlTitle]: "Svuota",
        [_.closeCtrlTitle]: "Chiudi",
        [_.closeOtherTabs]: "Chiudi le altre",
        [_.closeTab]: "Chiudi",
        [_.collapseAllCtrlTitle]: "Nascondi tutto",
        [_.columnExportedCompleted]: "Questa tabella è stata esportata con successo.",
        [_.columnExportedFailed]: "Questa tabella non è stata esportata per qualche errore non specificato.",
        [_.columnExportedTruncated]: "Questa tabella è stata troncata perché il numero di righe ha superato il limite massimo consentito.",
        [_.columnMeasureFormatted]: "Questa misura è già formattata.",
        [_.columnMeasureNotFormattedTooltip]: "Questa misura non è formattata.",
        [_.columnMeasureWithError]: "Questa misura contiene degli errori.",
        [_.columnUnreferencedExplanation]: `<span class="text-highlight">Le colonne non referenziate</span> possono essere generalmente rimosse dal modello per ottimizzare le prestazioni. Prima di rimuoverle, però, verifica che non siano usate in nessun report, perché Bravo non non è in grado di saperlo.`,
        [_.columnUnreferencedTooltip]: "Questa colonna non è referenziata nel tuo modello.",
        [_.confirmTabCloseMessage]: "Alcune modifiche non sono state salvate.<br>Sicuro di voler chiudere?",
        [_.connectBrowse]: "Esplora",
        [_.connectDatasetsTableEndorsementCol]: "Approvazione",
        [_.connectDatasetsTableNameCol]: "Nome",
        [_.connectDatasetsTableOwnerCol]: "Proprietario",
        [_.connectDatasetsTableWorkspaceCol]: "Workspace",
        [_.connectDialogAttachPBIMenu]: "Dataset su powerbi.com",
        [_.connectDialogConnectPBIMenu]: "Report Attivi su Power BI Desktop",
        [_.connectDialogOpenVPXMenu]: "File VPAX",
        [_.connectDialogTitle]: "Apri",
        [_.connectDragFile]: "Trascina un file VPAX qui<br>oppure cerca un file sul tuo computer",
        [_.connectNoReports]: "Nessun report attivo disponibile.<br>Apri un report con Power BI Desktop e aspetta che appaia qui.",
        [_.copiedErrorDetails]: "Copiato!",
        [_.copy]: "Copia",
        [_.copyErrorDetails]: "Copia Errore",
        [_.copyFormulaCtrlTitle]: "Copia la misura formattata",
        [_.copyMessage]: "Copia Messaggio",
        [_.copyright]: "Tutti i diritti sono riservati.",
        [_.createIssue]: "Segnala Problema",
        [_.cut]: "Taglia",
        [_.dataUsageLink]: "Come sono usati i tuoi dati?", 
        [_.dataUsageMessage]: `Per formattare il codice DAX, Bravo deve inviare le misure di questo modello a DAX Formatter (un servizio gestito da SQLBI) attraverso una connessione sicura.<p><strong>Il servizio non salva i tuoi dati da nessuna parte.</strong></p><p>Alcune informazioni, come le funzioni DAX più usate, un indice di complessità e la media della lunghezza delle query ricevute, possono essere salvate per successive analisi.</p>`,
        [_.dataUsageTitle]: "Come sono usati i tuoi dati?",
        [_.DaxFormatter]: "Formatta DAX",
        [_.daxFormatterAgreement]: "Per formattare il codice DAX, Bravo ha bisogno di inviare le tue misure a DAX Formatter.",
        [_.daxFormatterAnalyzeConfirm]: "Per effettuare un'analisi, Bravo deve inviare tutte le misure al servizio DAX Formatter. Sei sicuro di continuare?",
        [_.daxFormatterAutoPreviewOption]: "Anteprima automatica",
        [_.daxFormatterFormat]: "Formatta",
        [_.daxFormatterFormatDisabled]: "Formatta (Non Supportato)",
        [_.daxFormatterFormattedCode]: "Formattata (Anteprima)",
        [_.daxFormatterOriginalCode]: "Attuale",
        [_.daxFormatterPreviewAllButton]: "Anteprima di tutte le misure",
        [_.daxFormatterPreviewButton]: "Anteprima",
        [_.daxFormatterPreviewDesc]: "Per generare un'anteprima, Bravo deve inviare questa misura a DAX Formatter.",
        [_.daxFormatterSuccessSceneMessage]: "Congratulazioni, <strong>{count} misur{{e|a}}</strong> {{sono state formattate|è stata formattata}} correttamente.",
        [_.daxFormatterSummary]: `Il dataset contiene {count} misur{{e|a}}: <span class="text-error"><strong>{errors:number}</strong> con errori</span>, <span class="text-highlight"><strong>{formattable:number}</strong> da formattare</span>, <strong>{analyzable:number}</strong> da analizzare (<span class="link manual-analyze">analizza ora</span>).`,
        [_.daxFormatterSummaryNoAnalysis]: `Il dataset contiene <strong>{count}</strong> misur{{e|a}}: <span class="text-error"><strong>{errors:number}</strong> con errori</span> e <span class="text-highlight"><strong>{formattable:number}</strong> da formattare.</span>`,
        [_.defaultTabName]: "Senza nome",
        [_.devCreateTemplateDialogOk]: "Crea",
        [_.devCreateTemplateLabelModel]: "Basato su",
        [_.devCreateTemplateLabelName]: "Nome template",
        [_.devCreateTemplateNotes]: "Fai clic su <strong>Crea</strong> per scegliere la cartella dove salvare il progetto di Visual Studio Code contenente il nuovo template.",
        [_.devCreateTemplateTitle]: "Nuovo Template",
        [_.devDefaultTemplateName]: "Senza titolo",
        [_.devShowInFolder]: "Mostra in Esplora Risorse",
        [_.devTemplateRemoveConfirmation]: "Il template <b>{template}</b> verrà rimosso da questo elenco, ma qualsiasi file esistente verrà mantenuto sul sistema.<br>Sei sicuro di procedere?",
        [_.devTemplatesBrowse]: "Sfoglia",
        [_.devTemplatesColAction]: "Azioni",
        [_.devTemplatesColName]: "Template",
        [_.devTemplatesColType]: "Tipo",
        [_.devTemplatesCreate]: "Nuovo template",
        [_.devTemplatesEdit]: "Modifica",
        [_.devTemplatesEditTitle]: "Modifica con Visual Studio Code",
        [_.devTemplatesEmpty]: "Nessun template di data personalizzato trovato.",
        [_.devTemplatesNotAvailable]: "Questo template non è più disponibile.",
        [_.devTemplatesRemove]: "Rimuovi",
        [_.devTemplatesTypeOrganization]: "Organizzazione",
        [_.devTemplatesTypeUser]: "Utente",
        [_.devTemplatesVSCodeDownload]: "Scarica Visual Studio Code",
        [_.devTemplatesVSCodeExtensionDownload]: "Scarica l'estensione {extension}",
        [_.devTemplatesVSCodeMessage]: "<p>Il progetto del template data verrà ora aperto con Visual Studio Code e richiede che l'estensione <b>{extension}</b> per essere compilato.</p><p>Se non è disponibile sul sistema, usa i seguenti link per scaricarli e installarli:</p> ",
        [_.devTemplatesVSCodeTitle]: "Apri con Visual Studio Code",
        [_.dialogCancel]: "Annulla",
        [_.dialogContinue]: "Continua",
        [_.dialogNeverShowAgain]: "Non mostrare più",
        [_.dialogOK]: "OK",
        [_.dialogOpen]: "Apri",
        [_.docLimited]: "Limitato",
        [_.docLimitedTooltip]: "Non tutte le funzionalità sono disponibili per questo documento.",
        [_.doneCtrlTitle]: "Completato",
        [_.emailAddress]: "Indirizzo Email",
        [_.emailAddressPlaceholder]: "Inserisci il tuo indirizzo email",
        [_.error]: "Errore",
        [_.errorAborted]: "Operazione annullata.",
        [_.errorAnalysisServicesConnectionFailed]: "Si è verificato un problema di connessione tra il server e Bravo.",
        [_.errorCheckForUpdates]: "Impossibile controllare gli aggiornamenti - server remoto non raggiungibile.",
        [_.errorConnectionUnsupported]: "La connessione alla risorsa richiesta non è supportata.",
        [_.errorDatasetConnectionUnknown]: "Connessione non specificata.",
        [_.errorDatasetsEmptyListing]: "Nessun report aperto disponibile.",
        [_.errorDatasetsListing]: "Impossibile recuperare la lista dei tuoi dataset su Power BI Service.",
        [_.errorExportDataFileError]: "Si è verificato un problema durante l'esportazione. Riprova.",
        [_.errorGetEnvironments]: "Inserisci un account Power BI valido.",
        [_.errorManageDateTemplateError]: "Si è verificata un'eccezione durante l'esecuzione del motore del modello DAX.",
        [_.errorManageDateTemplateError]: "Si è verificato un problema durante l'esecuzione del processo DAX template.",
        [_.errorNetworkError]: "Non sei connesso a Internet.",
        [_.errorNone]: "Errore non specificato.",
        [_.errorNotAuthorized]: "Non sei autorizzato ad accedere alla risorsa.",
        [_.errorNotConnected]: "Non sei connesso a Power BI - Accedi per continuare.",
        [_.errorNotFound]: "Impossibile connettersi alla risorsa.",
        [_.errorPathNotFound]: "Percorso non trovato o non hai i diritti per aprirlo.",
        [_.errorReportConnectionUnknown]: "Connessione invalida.",
        [_.errorReportConnectionUnsupportedAnalysisServicesCompatibilityMode]: "La compatibilità dell'istanza di Power BI Desktop Analysis Services non è PowerBI.",
        [_.errorReportConnectionUnsupportedAnalysisServicesConnectionNotFound]: "Connessione TCP a Power BI Desktop Analysis Services non trovata.",
        [_.errorReportConnectionUnsupportedAnalysisServicesProcessNotFound]: "Istanza di Power BI Desktop Analysis Services non trovata.", 
        [_.errorReportConnectionUnsupportedConnectionException]: "Si è verificato un problema durante la connessione all'istanza di Power BI Desktop Analysis Services.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionEmpty]: "L'istanza di Power BI Desktop Analysis Services non contiene nessun database. Prova a connetterti al report cliccando sull'icona di Bravo negli Strumenti Esterni di Power BI.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionUnexpectedCount]: "L'istanza di Power BI Desktop Analysis Services contiene un numero inaspettato di database (> 1).",
        [_.errorReportConnectionUnsupportedProcessNotReady]: "Power BI Desktop è in fase di apertura o l'istanza di Analysis Services non è ancora avviata.", 
        [_.errorReportsEmptyListing]: "Nessun altro report aperto.",
        [_.errorRetry]: "Riprova",
        [_.errorSignInMsalExceptionOccurred]: "Errore inaspettato durante l'accesso.",
        [_.errorSignInMsalTimeoutExpired]: "La richiesta di accesso è stata annullata perché è passato troppo tempo prima che l'operazione venisse completata.",
        [_.errorTemplateAlreadyExists]: "Un altro template con lo stesso percorso/nome esiste già: <br><b>{name}</b>",
        [_.errorTimeout]: "Timeout.",
        [_.errorTitle]: "Oops...",
        [_.errorTOMDatabaseDatabaseNotFound]: "Il database non esiste o l'utente non ha le autorizzazioni per accedervi.",
        [_.errorTOMDatabaseUpdateConflictMeasure]: "La richiesta di aggiornamento è fallita perché in conflitto con lo stato attuale della risorsa.",
        [_.errorTOMDatabaseUpdateErrorMeasure]: "La richiesta di aggiornamento è fallita perché una o più misure contengono errori.", 
        [_.errorTOMDatabaseUpdateFailed]: "La richiesta di aggiornamento è fallita mentre le modifiche locali venivano sincronizzate con il database remoto.",
        [_.errorTryingToUpdateMeasuresWithErrors]: `La richiesta di aggiornamento è fallita perché queste misure contengono degli errori:<br><strong>{measures}</strong>`,
        [_.errorUnhandled]: "Errore non gestito - Invia una segnalazione includendo il trace id se disponibile.",
        [_.errorUnspecified]: "Errore non specificato.",
        [_.errorUserSettingsSaveError]: "Impossibile salvare le opzioni.",
        [_.errorVpaxFileExportError]: "Si è verificato un errore durante l'esportazione del file VPAX.",
        [_.errorVpaxFileImportError]: "Si è verificato un errore durante l'importazione del file VPAX.",
        [_.expandAllCtrlTitle]: "Mostra tutto",
        [_.ExportData]: "Esporta Dati",
        [_.exportDataCSVCustomDelimiter]: "Delimitatore personalizzato",
        [_.exportDataCSVDelimiter]: "Delimitatore",
        [_.exportDataCSVDelimiterComma]: "Virgola",
        [_.exportDataCSVDelimiterDesc]: `Seleziona il carattere da usare come delimitatore dei campi. <em>Automatico</em> usa il carattere predefinito di sistema.`,
        [_.exportDataCSVDelimiterOther]: "Altro...",
        [_.exportDataCSVDelimiterPlaceholder]: "Carattere",
        [_.exportDataCSVDelimiterSemicolon]: "Punto e Virgola",
        [_.exportDataCSVDelimiterSystem]: "Automatico",
        [_.exportDataCSVDelimiterTab]: "Tab",
        [_.exportDataCSVEncoding]: "Codifica",
        [_.exportDataCSVEncodingDesc]: "",
        [_.exportDataCSVFolder]: "Salva in una sottocartella",
        [_.exportDataCSVFolderDesc]: "Salva file CSV generati in una sottocartella.",
        [_.exportDataCSVQuote]: "Stringhe tra virgolette",
        [_.exportDataCSVQuoteDesc]: "Racchiudi tutte le stringhe tra virgolette.",
        [_.exportDataExcelCreateExportSummary]: "Foglio di Riepilogo ",
        [_.exportDataExcelCreateExportSummaryDesc]: "Aggiungi un foglio al file Excel con il riepilogo del processo.",
        [_.exportDataExport]: "Esporta",
        [_.exportDataExportAs]: "Esporta come",
        [_.exportDataExportAsDesc]: "",
        [_.exportDataExporting]: "Esportazione {table} in corso...",
        [_.exportDataExportingDone]: "Completato!",
        [_.exportDataNoColumns]: "Questa tabella non può essere esportata perché non contiene nessuna colonna.",
        [_.exportDataNotQueryable]: "Questa tabella non può essere esportata perché contiene una o più colonne calcolate con un'espressione non valida o colonne che devono essere ricalcolate.",
        [_.exportDataOpenFile]: "Apri file",
        [_.exportDataOpenFolder]: "Apri cartella",
        [_.exportDataOptions]: "Opzioni",
        [_.exportDataStartExporting]: "Inizializzazione...",
        [_.exportDataSuccessSceneMessage]: "<strong>{count}/{total} tabell{{e|a}}</strong> {{sono state esportate|è stata esportata}} correttamente.",
        [_.exportDataSummary]: "Il dataset contiene <strong>{count} tabell{{e|a}}</strong> che {{possono|può}} essere esportat{{e|a}}.",
        [_.exportDataTruncated]: "Questa tabella ha troppe righe. Solo il primo milioni di righe può essere esportato in Excel. Utilizzare il formato CSV per esportare tutte le righe della tabella.",
        [_.exportDataTypeCSV]: "CSV (Valori separati da virgola)",
        [_.exportDataTypeXLSX]: "Foglio di calcolo Excel",
        [_.failed]: "Fallito",
        [_.filterMeasuresWithErrorsCtrlTitle]: "Mostra solo misure non formattate o con errori",
        [_.filterUnrefCtrlTitle]: "Mostra solo colonne non referenziate.",
        [_.formattingMeasures]: "Formattazione in corso...",
        [_.goBackCtrlTitle]: "Annulla e torna indietro",
        [_.groupByTableCtrlTitle]: "Raggruppa per Tabella",
        [_.helpConnectVideo]: "Come connettersi",
        [_.helpCtrlTitle]: "Aiuto",
        [_.hideUnsupportedCtrlTitle]: "Solo supportati",
        [_.less]: "Meno",
        [_.license]: "Rilasciato con licenza MIT.",
        [_.loading]: "Caricamento...",
        [_.ManageDates]: "Gestisci Date",
        [_.manageDatesApplyCtrlTitle]: "Applica",
        [_.manageDatesAuto]: "Auto",
        [_.manageDatesAutoScan]: "Scansione Automatica",
        [_.manageDatesAutoScanActiveRelationships]: "Relazioni Attive",
        [_.manageDatesAutoScanDesc]: "Seleziona <em>Completa</em> per scansionare tutte le colonne contenenti date. Imposta <em>Scegli Colonne...</em> per selezionare le colonne da usare. Imposta <em>Relazioni Attive</em> e <em>Relazioni Inattive</em> per scansionare solo le colonne con relazioni.",
        [_.manageDatesAutoScanDisabled]: "Disabilitata",
        [_.manageDatesAutoScanFirstYear]: "Primo Anno",
        [_.manageDatesAutoScanFirstYearDesc]: "",
        [_.manageDatesAutoScanFull]: "Completa",
        [_.manageDatesAutoScanInactiveRelationships]: "Relazioni Inattive",
        [_.manageDatesAutoScanLastYear]: "Ultimo Anno",
        [_.manageDatesAutoScanLastYearDesc]: "",
        [_.manageDatesAutoScanSelectedTablesColumns]: "Scegli Colonne...",
        [_.manageDatesBrowserPlaceholder]: "Nessun elemento da modificare.",
        [_.manageDatesCalendarDesc]: "Scegli un tipo di calendario da applicare al modello. Bravo creerà le tabelle necessarie o le aggiornerà, mantenendo attive le relazioni esistenti.", 
        [_.manageDatesCalendarTemplateName]: "Template",
        [_.manageDatesCalendarTemplateNameDesc]: "Scegli <em>Mensile</em> per calendari basati su un numero di mesi variabile. Imposta <em>Settimanale</em> per calendari 445-454-544-ISO. Usa <em>Personalizzato</em> per calendari flessibili di lunghezza variabile.",
        [_.manageDatesCreatingTables]: "Aggiornamento modello in corso...",
        [_.manageDatesDatesDesc]: "Seleziona il formato e dove posizionare le date nel modello.",
        [_.manageDatesDatesTableDesc]: "Questa è la tabella da usare nei report per le date.",
        [_.manageDatesDatesTableName]: "Tabella Date",
        [_.manageDatesDatesTableReferenceDesc]: "Questa è una tabella nascosta contenente tutto il codice DAX necessario a generare le date.",
        [_.manageDatesDatesTableReferenceName]: "Tabella Definizione Date",
        [_.manageDatesHolidaysDesc]: "Aggiungi le vacanze al tuo modello. Bravo creerà le tabelle necessarie o le aggiornerà, mantenendo attive le relazioni esistenti.",
        [_.manageDatesHolidaysEnabledDesc]: "Aggiungi la tabella vacanze al tuo modello.",
        [_.manageDatesHolidaysEnabledName]: "Vacanze",
        [_.manageDatesHolidaysTableDefinitionDesc]: "Questa è una tabella nascosta contenente tutto il codice DAX necessario a generare le vacanze.",
        [_.manageDatesHolidaysTableDefinitionName]: "Tabella Definizione Vacanze",
        [_.manageDatesHolidaysTableDesc]: "Questa è la tabella da usare nei report per le vacanze.",
        [_.manageDatesHolidaysTableName]: "Tabella Vacanze",
        [_.manageDatesIntervalDesc]: "Scegli un intervallo di date per il tuo modello.",
        [_.manageDatesISOCountryDesc]: "",
        [_.manageDatesISOCountryName]: "Paese Vacanze",
        [_.manageDatesISOCustomFormatDesc]: "Inserisci un formato internazionale utilizzando la notazione IETF BCP 47. Es. it-IT",
        [_.manageDatesISOCustomFormatName]: "Altro Formato",
        [_.manageDatesISOFormatDesc]: "Scegli il formato locale da usare per le date.",
        [_.manageDatesISOFormatName]: "Formato Date",
        [_.manageDatesISOFormatOther]: "Altro...",
        [_.manageDatesISOFormatOtherPlaceholder]: "Formato",
        [_.manageDatesManageTemplates]: "Gestisci template",
        [_.manageDatesMenuCalendar]: "Calendario",
        [_.manageDatesMenuDates]: "Date",
        [_.manageDatesMenuHolidays]: "Vacanze",
        [_.manageDatesMenuInterval]: "Intervallo",
        [_.manageDatesMenuPreviewCode]: "Espressione",
        [_.manageDatesMenuPreviewModel]: "Anteprima Modello",
        [_.manageDatesMenuPreviewTable]: "Campione Dati",
        [_.manageDatesMenuPreviewTreeDate]: "Date",
        [_.manageDatesMenuPreviewTreeDateHolidays]: "Date & Vacanze",
        [_.manageDatesMenuPreviewTreeTimeIntelligence]: "Time Intelligence",
        [_.manageDatesMenuTimeIntelligence]: "Time Intelligence",
        [_.manageDatesModelCheck]: "Verifica Modello",
        [_.manageDatesPreview]: "Anteprima...",
        [_.manageDatesPreviewCtrlTitle]: "Anteprima Modifiche",
        [_.manageDatesSampleData]: "Campione Dati",
        [_.manageDatesSampleDataError]: "Impossibile generare un campione dati.",
        [_.manageDatesStatusCompatible]: `<div class="hero">Questo modello <b>contiene già delle tabelle data compatibili</b> con Bravo.</div>Se applichi qualche modifica, queste tabelle verranno aggiornate ma le relazioni rimarranno intatte.`,
        [_.manageDatesStatusError]: `<div class="hero">Le impostazioni selezionate non possono essere applicate.</div>{error}`,
        [_.manageDatesStatusIncompatible]: `<div class="hero">Questo modello contiene alcune tabelle di data che <b>non sono compatibili</b> con Bravo.</div> Per apportare delle modifiche, è necessario scegliere un nome diverso per la tabella <b>Date</b> e/o <b>Vacanze</b>.`,
        [_.manageDatesStatusNotAvailable]: `<div class="hero">Questo modello non è più disponibile.</div> Prova a riavviare l'applicazione.`,
        [_.manageDatesStatusOk]: `<div class="hero">Questo modello <b>è compatibile con questo tool</b>.</div>Puoi creare nuove tabelle data senza preoccuparti di rompere alcuna misura o report esistenti.`,
        [_.manageDatesSuccessSceneMessage]: "Congratulazioni, il modello è stato aggiornato con successo.",
        [_.manageDatesTemplateFirstDayOfWeek]: "Primo Giorno della Settimana",
        [_.manageDatesTemplateFirstDayOfWeekDesc]: "Per calendari settimanali ISO, usa <em>Lunedì</em>.",
        [_.manageDatesTemplateFirstFiscalMonth]: "Primo Mese dell'Anno",
        [_.manageDatesTemplateFirstFiscalMonthDesc]: "Per calendari settimanali ISO, usa <em>Gennaio</em>.",
        [_.manageDatesTemplateMonthsInYear]: "Mesi nell'Anno",
        [_.manageDatesTemplateMonthsInYearDesc]: "",
        [_.manageDatesTemplateNameConfig01]: "Standard",
        [_.manageDatesTemplateNameConfig02]: "Standard (Fiscale)",
        [_.manageDatesTemplateNameConfig03]: "Mensile",
        [_.manageDatesTemplateNameConfig04]: "Mensile (Fiscale)",
        [_.manageDatesTemplateNameConfig05]: "Personalizzato",
        [_.manageDatesTemplateNameConfig06]: "Personalizzato (Fiscale)",
        [_.manageDatesTemplateNameConfig07]: "Settimanale",
        [_.manageDatesTemplateNameCurrent]: "Corrente",
        [_.manageDatesTemplateQuarterWeekType]: "Sistema",
        [_.manageDatesTemplateQuarterWeekTypeDesc]: "",
        [_.manageDatesTemplateTypeStartFiscalYear]: "Data che definisce l'anno fiscale",
        [_.manageDatesTemplateTypeStartFiscalYearDesc]: "Scegli quando iniziare l'anno fiscale. Per calendari settimanali ISO, usa <em>Ultimo dell'Anno</em>.",
        [_.manageDatesTemplateTypeStartFiscalYearFirst]: "Primo dell'Anno",
        [_.manageDatesTemplateTypeStartFiscalYearLast]: "Ultimo dell'Anno",
        [_.manageDatesTemplateWeeklyType]: "Ultimo Giorno della Settimana dell'Anno",
        [_.manageDatesTemplateWeeklyTypeDesc]: "e la tua settimana inizia di Lunedì, l'ultimo giorno della settimana è Domenica. Se scegli <em>Ultimo dell'Anno</em>, l'anno fiscale finisce l'ultima Domenica dell'ultimo mese. Altrimenti, l'anno fiscale finisce la Domenica più vicina all'ultimo giorno dell'ultimo mese - potrebbe essere all'anno successivo. Per calendari settimanali ISO, usa <em>Più Vicino alla Fine dell'Anno</em>.",
        [_.manageDatesTemplateWeeklyTypeLast]: "Ultimo dell'Anno",
        [_.manageDatesTemplateWeeklyTypeNearest]: "Più Vicino alla Fine dell'Anno",
        [_.manageDatesTimeIntelligenceDesc]: "Crea le funzioni di Time Intelligence più comuni al tuo modello.",
        [_.manageDatesTimeIntelligenceEnabledDesc]: "",
        [_.manageDatesTimeIntelligenceEnabledName]: "Funzioni di Time Intelligence",
        [_.manageDatesTimeIntelligenceTargetMeasuresAll]: "Tutte le Misure",
        [_.manageDatesTimeIntelligenceTargetMeasuresChoose]: "Scegli Misure...",
        [_.manageDatesTimeIntelligenceTargetMeasuresDesc]: "Scegli le misure utilizzate per creare le funzioni di Time Intelligence.",
        [_.manageDatesTimeIntelligenceTargetMeasuresName]: "Misure Target",
        [_.manageDatesYearRange]: "Intervallo Date",
        [_.manageDatesYearRangeDesc]: "Scegli come determinare l'intervallo di date del modello. Lascia il <em>Primo Anno</em> e/o l'<em>Ultimo Anno</em> vuoti per usare la scansione automatica.",
        [_.menuCtrlTitle]: "Mostra/Nascondi menu",
        [_.minimizeCtrlTitle]: "Riduci",
        [_.modelLanguage]: "Lingua modello ({culture})",
        [_.more]: "Di più",
        [_.notificationCtrlTitle]: "Notifiche",
        [_.notificationsTitle]: "{count} notific{{he|a}}",
        [_.openSourcePayoff]: "{appName} è uno strumento open-source sviluppato e mantenuto da SQLBI e dalla comunità su Github. Contribuisci su",
        [_.openWithDaxFormatterCtrlTitle]: "Formatta online con DAX Formatter",  
        [_.optionAccount]: "Account Power BI",
        [_.optionAccountDescription]: "Imposta l'account per accedere ai dataset online di Power BI.",
        [_.optionBrowserAuthentication]: "Autentica nel browser",
        [_.optionBrowserAuthenticationDescription]: "Autenticati utilizzando il browser predefinito. Questo è un metodo di accesso alternativo utile per risolvere problemi con l'autenticazione a due fattori.",
        [_.optionCheckForUpdates]: "Controllare automaticamente aggiornamenti",
        [_.optionDev]: "Abilita Template Date Utenti",
        [_.optionDevDescription]: "",
        [_.optionDiagnostic]: "Diagnostica",
        [_.optionDiagnosticDescription]: "Mostra errori e log in un pannello apposito. Scegli <em>Base</em> per registrare solo alcuni messaggi. <em>Integrale</em> registra invece tutti i messaggi.",
        [_.optionDiagnosticLevelBasic]: "Base",
        [_.optionDiagnosticLevelNone]: "Nessuna",
        [_.optionDiagnosticLevelVerbose]: "Integrale",
        [_.optionDiagnosticMore]: "Per segnalare bug nell'applicazione vai su",
        [_.optionFormattingBreaks]: "Separazione Nome-Espressione",
        [_.optionFormattingBreaksAuto]: "Auto",
        [_.optionFormattingBreaksDescription]: "Scegli come separare il nome della misura e l'espressione. Imposta <em>Auto</em> per scegliere automaticamente in base al modello.",
        [_.optionFormattingBreaksInitial]: "A Capo",
        [_.optionFormattingBreaksNone]: "Stessa Linea",
        [_.optionFormattingIncludeTimeIntelligence]: "Includi Time Intelligence",
        [_.optionFormattingIncludeTimeIntelligenceDescription]: "Includi misure create automaticamente da Manage Dates per la Time Intelligence.",
        [_.optionFormattingLines]: "Linee",
        [_.optionFormattingLinesDescription]: "Scegli se tenere le linee lunghe o corte.",
        [_.optionFormattingLinesValueLong]: "Linee lunghe",
        [_.optionFormattingLinesValueShort]: "Linee corte",
        [_.optionFormattingPreview]: "Anteprima Automatica",
        [_.optionFormattingPreviewDescription]: "Invia automaticamente le misure a DAX Formatter per generare le anteprime.",
        [_.optionFormattingSeparators]: "Separatori",
        [_.optionFormattingSeparatorsDescription]: "Scegli i separatori per i numeri e le liste.",
        [_.optionFormattingSeparatorsValueAuto]: "Auto",
        [_.optionFormattingSeparatorsValueEU]: "A; B; C; 1234,00",
        [_.optionFormattingSeparatorsValueUS]: "A, B, C, 1234.00",
        [_.optionFormattingSpaces]: "Spaziatura",
        [_.optionFormattingSpacesDescription]: "Gestisci gli spazi prima dei nomi delle funzioni.",
        [_.optionFormattingSpacesValueBestPractice]: "Raccomandato",
        [_.optionFormattingSpacesValueFalse]: "Nessuno spazio - IF( ",
        [_.optionFormattingSpacesValueTrue]: "Spazio - IF ( ",
        [_.optionInvalidValue]: "Non valido",
        [_.optionLanguage]: "Lingua",
        [_.optionLanguageDescription]: "Scegli la lingua di Bravo. Riavvio richiesto.",
        [_.optionLanguageResetConfirm]: "Vuoi riavviare Bravo per applicare la nuova lingua?",
        [_.optionPolicyNotice]: "Questa opzione è gestita dalla tua organizzazione.",
        [_.optionProxyAddress]: "Indirizzo Server Proxy",
        [_.optionProxyAddressDescription]: "Inserisci l'indirizzo del tuo server proxy.",
        [_.optionProxyBypassList]: "Escludi Indirizzi",
        [_.optionProxyBypassListDescription]: "Utilizzare il server proxy tranne che con gli indirizzi che nella lista sotto. Usa i punti e virgole (;) per separare più voci.",
        [_.optionProxyBypassOnLocal]: "Escludi Indirizzi Locali",
        [_.optionProxyBypassOnLocalDescription]: "Non utilizzare il proxy con gli indirizzi locali (intranet).",
        [_.optionProxyConfirmDeleteCredentials]: "Sicuro di voler rimuovere le credenziali personalizzate dal sistema?",
        [_.optionProxyCustomCredentials]: "Credenziali Personalizzate",
        [_.optionProxyCustomCredentialsDescription]: "Utilizza le credenziali predefinite del sistema per autenticarti sul server proxy. Lascia disabilitato per utilizzare le credenziali di sistema.",
        [_.optionProxyCustomCredentialsEdit]: "Modifica credenziali personalizzate",
        [_.optionProxyType]: "Server proxy",
        [_.optionProxyTypeCustom]: "Personalizzato",
        [_.optionProxyTypeDescription]: "Scegli un server proxy.",
        [_.optionProxyTypeNone]: "Nessuno",
        [_.optionProxyTypeSystem]: "Sistema",
        [_.optionResetAlerts]: "Ripristina avvisi",
        [_.optionResetAlertsButton]: "Ripristina",
        [_.optionResetAlertsDescription]: "Ripristina tutti gli avvisi nascosti manualmente.",
        [_.optionsDialogAboutMenu]: "Informazioni",
        [_.optionsDialogDevMenu]: "Template",
        [_.optionsDialogFormattingMenu]: "Formattazione",
        [_.optionsDialogGeneralMenu]: "Generale",
        [_.optionsDialogProxyMenu]: "Proxy",
        [_.optionsDialogTelemetryMenu]: "Diagnostica",
        [_.optionsDialogTitle]: "Opzioni",
        [_.optionTelemetry]: "Telemetria",
        [_.optionTelemetryDescription]: "Invia informazioni di utilizzo anonime a SQLBI.",
        [_.optionTelemetryMore]: "Aiutaci a capire come gli utenti usano Bravo e come migliorarlo. Nessuna informazione personale è inviata o salvata. Nota che se l'opzione è disattivata non saremo in grado di fornire supporto su errori non gestiti.",
        [_.optionTheme]: "Tema",
        [_.optionThemeDescription]: "Imposta il tema di Bravo. Seleziona <em>Sistema</em> per usare il tema di sistema.",
        [_.optionThemeValueAuto]: "Sistema",
        [_.optionThemeValueDark]: "Scuro",
        [_.optionThemeValueLight]: "Chiaro",
        [_.otherColumnsRowName]: "Altre colonne...",
        [_.paste]: "Incolla",
        [_.powerBiObserving]: "in attesa dell'apertura del file in Power BI Desktop...",
        [_.powerBiObservingCancel]: "Annulla",
        [_.powerBiSigninDescription]: "Accedi a Power BI Service per permette a Bravo di connettersi ai tuoi dataset.",
        [_.powerBiSigninTitle]: "Power BI",
        [_.quickActionAttachPBITitle]: "Connetti a Power BI Desktop",
        [_.quickActionConnectPBITitle]: "Connetti a Power BI Service",
        [_.quickActionOpenVPXTitle]: "Apri un file di Vertipaq Analyzer",
        [_.refreshCtrlTitle]: "Aggiorna",
        [_.refreshPreviewCtrlTitle]: "Aggiorna anteprima",
        [_.saveVpaxCtrlTile]: "Salva come VPAX",
        [_.savingVpax]: "Generazione VPAX in corso...",
        [_.sceneUnsupportedReason]: "La funzionalità non è disponibile per questa fonte dati.",
        [_.sceneUnsupportedReasonManageDatesAutoDateTimeEnabled]: `I modelli con l'opzione auto date/time attiva non sono supportati.<br><span class="link" href="https://www.sqlbi.com/tv/disabling-auto-date-time-in-power-bi/">Disabling auto date-time in Power BI (video)</span>`,
        [_.sceneUnsupportedReasonManageDatesEmptyTableCollection]: "Questa funzionalità è supportata solo da database che hanno almeno una tabella.",
        [_.sceneUnsupportedReasonManageDatesPBIDesktopModelOnly]: "Questa funzionalità è supportata solo su modelli in Power Bi Desktop.",
        [_.sceneUnsupportedReasonMetadataOnly]: "Il database è stato generato da un file VPAX che contiene solamente metadati.",
        [_.sceneUnsupportedReasonReadOnly]: "La connessione al database è di sola lettura.",
        [_.sceneUnsupportedReasonXmlaEndpointNotSupported]: "XMLA endpoint non è supportato per questo dataset.",
        [_.sceneUnsupportedTitle]: "Non supportato",
        [_.searchColumnPlaceholder]: "Cerca Colonna",
        [_.searchDatasetPlaceholder]: "Cerca Dataset",
        [_.searchEntityPlaceholder]: "Search Tabella/Colonna",
        [_.searchMeasurePlaceholder]: "Cerca Misura",
        [_.searchPlaceholder]: "Cerca",
        [_.searchTablePlaceholder]: "Cerca Tabella",
        [_.settingsCtrlTitle]: "Opzioni",
        [_.sheetOrphan]: "Non disponibile",
        [_.sheetOrphanPBIXTooltip]: "Il report è stato chiuso su Power BI Desktop. Qualunque operazione di scrittura è disabilitata.",
        [_.sheetOrphanTooltip]: "Questo modello non è più disponibile. Qualunque operazione di scrittura è disabilitata.",
        [_.showDiagnosticPane]: "Mostra Dettagli",
        [_.sideCtrlTitle]: "Visualizzazione affiancata",
        [_.signedInCtrlTitle]: "Autenticato come {name}",
        [_.signIn]: "Accedi",
        [_.signInCtrlTitle]: "Accedi",
        [_.signOut]: "Esci",
        [_.sqlbiPayoff]: "Bravo è un progetto di SQLBI.",
        [_.syncCtrlTitle]: "Sincronizza",
        [_.tableColCardinality]: "Cardinalità",
        [_.tableColCardinalityTooltip]: "Numero di elementi unici",
        [_.tableColColumn]: "Colonna",
        [_.tableColColumns]: "Colonne",
        [_.tableColMeasure]: "Misura",
        [_.tableColPath]: "Tabella \\ Colonna",
        [_.tableColRows]: "Righe",
        [_.tableColSize]: "Dimensioni",
        [_.tableColTable]: "Tabella",
        [_.tableColWeight]: "Impatto",
        [_.tableSelectedCount]: "{count} {{selezionate|selezionata}}",
        [_.tableValidationInvalid]: "Nome non valido",
        [_.tableValidationValid]: "Questo nome è valido",
        [_.themeCtrlTitle]: "Cambia Tema",
        [_.toggleTree]: "Cambia Visualizzazione",
        [_.traceId]: "Trace Id",
        [_.unknownMessage]: "Messaggio Non Valido",
        [_.updateChannelBeta]: "Beta",
        [_.updateChannelCanary]: "Canary",
        [_.updateChannelDev]: "Dev",
        [_.updateChannelStable]: "Stable", 
        [_.updateMessage]: "Una nuova versione di Bravo è disponibile: {version}",
        [_.validating]: "Validazione in corso...",
        [_.version]: "Versione",
        [_.welcomeHelpText]: "Guarda i video sotto per imparare a usare Bravo:",
        [_.welcomeHelpTitle]: "Come usare Bravo?",
        [_.welcomeText]: "Bravo è un comodo strumento per Power BI che può essere usato per analizzare i tuoi modelli, formattare le misure DAX, creare tabelle date ed esportare i dati.",
        [_.whitespacesTitle]: "Caratteri speciali",
        [_.wrappingTitle]: "A capo automatico",
    }
}
export default locale;
