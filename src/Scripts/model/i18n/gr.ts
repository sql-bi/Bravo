/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Locale } from '../i18n';
import { strings as _ } from '../strings';

const locale: Locale = {

    locale: "el", // DO NOT TRANSLATE
    enName: "Greek", // DO NOT TRANSLATE
    name: "Ελληνικά",

    strings: {
        [_.addCtrlTitle]: "Άνοιγμα",
        [_.aggregatedTableName]: "Πολλαπλοί πίνακες",
        [_.AnalyzeModel]: "Ανάλυσε το Μοντέλο",
        [_.analyzeModelSummary]: `Το σετ δεδομένων σου είναι <strong>{size:bytes}</strong> σε μέγεθος και περιέχει <strong>{count}</strong> κολώνα{{ες}}`,
        [_.analyzeModelSummary2]: `, <span class="text-highlight">όπου <strong>{count}</strong> από αυτ{{ές|ή}} δεν χρησιμοποιούνται σε αυτό το μοντέλο.</span>`,
        [_.appName]: "Bravo for Power BI", // DO NOT TRANSLATE
        [_.appUpdateAvailable]: "Νέα έκδοση είναι διαθέσιμη: {version}",
        [_.appUpdateChangelog]: "Αλλαγές",
        [_.appUpdateDownload]: "Λήψη Αρχείου",
        [_.appUpdateViewDetails]: "Προβολή λεπτομερειών",
        [_.appUpToDate]: "Έχεις την τελευταία έκδοση του Bravo",
        [_.appVersion]: "Έκδοση {version}",
        [_.backupReminder]: "Πριν προχωρήσεις, κράτησε αντίγραφο ασφαλείας της αναφοράς σου (<b>κάποιες αλλαγές μπορεί να μην είναι αναστρέψιμες</b>).",
        [_.BestPractices]: "Βέλτιστες πρακτικές",
        [_.canceled]: "Ακυρώθηκε",
        [_.changeStatusAdded]: "Π",
        [_.changeStatusAddedTitle]: "Προστέθηκε",
        [_.changeStatusDeleted]: "Δ",
        [_.changeStatusDeletedTitle]: "Διαγράφηκε",
        [_.changeStatusModified]: "Μ",
        [_.changeStatusModifiedTitle]: "Μετατράπηκε",
        [_.clearCtrlTitle]: "Καθαρισμός",
        [_.closeCtrlTitle]: "Κλείσιμο",
        [_.closeOtherTabs]: "Κλείσιμο των άλλων",
        [_.closeTab]: "Κλείσιμο",
        [_.collapseAllCtrlTitle]: "Σύμπτυξη όλων",
        [_.columnExportedCompleted]: "Τα δεδομένα του πίνακα έγιναν εξαγωγή με επιτυχία.",
        [_.columnExportedFailed]: "Η εξαγωγή των δεδομένων του πίνακα απέτυχε εξαιτίας ενός απροσδιόριστου σφάλματος.",
        [_.columnExportedTruncated]: "Τα δεδομένα του πίνακα μειώθηκαν καθώς ο αριθμός των γραμμών υπερέβη τον επιτρεπόμενο αριθμό των εγγραφών.",
        [_.columnMeasureFormatted]: "Η μέτρηση αυτή είναι ήδη μορφοποιημένη.",
        [_.columnMeasureNotFormattedTooltip]: "Η μέτρηση αυτή δεν είναι μορφοποιημένη.",
        [_.columnMeasureWithError]: "Η μέτρηση αυτή περιέχει σφάλματα.",
        [_.columnUnreferencedExplanation]: `<span class="text-highlight">Μη χρησιμοποιούμενες κολώνες</span> καλό είναι να αφαιρεθούν από το μοντέλο καθώς θα βελτιωθεί η απόδοση του. Πριν όμως προχωρήσεις σε αυτό πρέπει να βεβαιωθείς ότι αυτές που θα αφαιρέσεις δεν χρησιμοποιούνται σε κάποια άλλη αναφορά που αυτή την στιγμή το Bravo δεν μπορεί να γνωρίζει.`,
        [_.columnUnreferencedTooltip]: "Αυτή η κολώνα δεν χρησιμοποιείται στο μοντέλο αυτό.",
        [_.confirmTabCloseMessage]: "Φαίνεται ότι δεν έχεις αποθηκεύσει τις αλλαγές που έχεις πραγματοποιήσει σε αυτό το αρχείο.<br>Είσαι σίγουρος ότι θέλεις να το κλείσεις χωρίς να το αποθηκεύσεις;",
        [_.connectBrowse]: "Αναζήτηση",
        [_.connectDatasetsTableEndorsementCol]: "Επικύρωση",
        [_.connectDatasetsTableNameCol]: "Όνομα",
        [_.connectDatasetsTableOwnerCol]: "Ιδιοκτήτης",
        [_.connectDatasetsTableWorkspaceCol]: "Χώρος εργασίας",
        [_.connectDialogAttachPBIMenu]: "Σετ δεδομένων στο powerbi.com",
        [_.connectDialogConnectPBIMenu]: "Ανοιχτές αναφορές στο Power BI Desktop",
        [_.connectDialogOpenVPXMenu]: "VPAX Αρχεία",
        [_.connectDialogTitle]: "'Ανοιγμα",
        [_.connectDragFile]: "Σύρε και άσε ένα VPAX αρχείο εδώ <br>ή αναζήτησε ένα στον υπολογιστή σου",
        [_.connectNoReports]: "Δεν βρέθηκαν ανοιχτές αναφορές<br>Άνοιξε μια αναφορά στο Power BI Desktop και περίμενε μέχρι να εμφανιστεί εδώ.",
        [_.copiedErrorDetails]: "Αντιγράφηκε!",
        [_.copy]: "Αντιγραφή",
        [_.copyErrorDetails]: "Σφάλμα στην αντιγραφή",
        [_.copyFormulaCtrlTitle]: "Αντιγραφή μορφοποιημένης μέτρησης",
        [_.copyMessage]: "Μήνυμα αντιγραφής",
        [_.copyright]: "Όλα τα δικαιώματα είναι δεσμευμένα.",
        [_.createIssue]: "Αναφορά προβλήματος",
        [_.cut]: "Αποκοπή",
        [_.dataUsageLink]: "Πως χρησιμοποιούνται τα δεδομένα σου;", 
        [_.dataUsageMessage]: `Για την μορφοποίηση του κώδικα σου, το Bravo στέλνει τις μετρήσεις που έχεις στα δεδομένα σου στο DAX Formatter (μια υπηρεσία του SQLBI), χρησιμοποιώντας ασφαλή δικτυακή επικοινωνία.<p><strong>Η υπηρεσία αυτή δεν αποθηκεύει τίποτα από τα δεδομένα σου.</strong></p><p>Κάποιες πληροφορίες αποθηκεύονται για στατιστικούς λόγους όπως το ποιες συναρτήσεις DAX χρησιμοποιούνται περισσότερο, η περιπλοκότητα των δεικτών, και ο μ.ό. του μεγέθους του ερωτήματος.</p>`,
        [_.dataUsageTitle]: "Πως χρησιμοποιούνται τα δεδομένα σου;",
        [_.DaxFormatter]: "Μορφοποίηση DAX",
        [_.daxFormatterAgreement]: "Για την μορφοποίηση της DAX, το Bravo στέλνει τις μετρήσεις που έχεις στα δεδομένα σου στην υπηρεσία DAX Formatter.",
        [_.daxFormatterAnalyzeConfirm]: "Για την εκτέλεση της ανάλυσης, το Bravo πρέπει να στείλει όλες τις μετρήσεις στην υπηρεσία DAX Formatter. Θέλεις να συνεχίσεις;?",
        [_.daxFormatterAutoPreviewOption]: "Αυτόματη προεπισκόπηση",
        [_.daxFormatterFormat]: "Επιλέχθηκε μορφοποίηση",
        [_.daxFormatterFormatDisabled]: "Μορφοποίηση (Μη υποστηριζόμενη)",
        [_.daxFormatterFormattedCode]: "Μορφοποίηση (Προεπισκόπηση)",
        [_.daxFormatterOriginalCode]: "Τρέχον",
        [_.daxFormatterPreviewAllButton]: "Προεπισκόπηση όλων των μετρήσεων",
        [_.daxFormatterPreviewButton]: "Προεπισκόπηση",
        [_.daxFormatterPreviewDesc]: "Για την δημιουργία της προεπισκόπησης, το Bravo πρέπει να στείλει τη μέτρηση αυτή στην υπηρεσία DAX Formatter.",
        [_.daxFormatterSuccessSceneMessage]: "Συγχαρητήρια, <strong>{count} {{μετρήσεις|μέτρηση}}</strong> {{μορφοποιήθηκαν|μορφοποιήθηκε}} με επιτυχία.",
        [_.daxFormatterSummary]: `Τα δεδομένα σου περιέχουν {count} {{μετρήσεις|μέτρηση}}: <span class="text-error"><strong>{errors:number}</strong> με σφάλματα</strong></span>, <span class="text-highlight"><strong>{formattable:number}</strong> για μορφοποίηση</span>, <strong>{analyzable:number}</strong> για ανάλυση (<span class="link manual-analyze">ανάλυση τώρα</span>).`,
        [_.daxFormatterSummaryNoAnalysis]: `Τα δεδομένα σου περιέχουν <strong>{count}</strong> {{μετρήσεις|μέτρηση}}: <span class="text-error"><strong>{errors:number}</strong> με σφάλματα </strong></span> και <span class="text-highlight"><strong>{formattable:number}</strong> με μορφοποίηση.</span>`,
        [_.defaultTabName]: "Χωρίς τίτλο",
        [_.dialogCancel]: "Ακύρωση",
        [_.dialogOK]: "OK",
        [_.dialogOpen]: "Άνοιγμα",
        [_.docLimited]: "Περιορισμένο",
        [_.docLimitedTooltip]: "Δεν είναι διαθέσιμες όλες οι επιλογές σε αυτό το αρχείο.",
        [_.doneCtrlTitle]: "Ολοκληρώθηκε",
        [_.emailAddress]: "Ηλεκτρονικό ταχυδρομείο",
        [_.emailAddressPlaceholder]: "Πληκτρολόγησε τη διεύθυνση του ηλεκτρονικού σου ταχυδρομείου",
        [_.error]: "Σφάλμα",
        [_.errorAborted]: "Η λειτουργία ματαιώθηκε.",
        [_.errorAnalysisServicesConnectionFailed]: "Υπάρχει πρόβλημα στην σύνδεση μεταξύ του σέρβερ και του Bravo.",
        [_.errorCheckForUpdates]: "Δεν μπορεί να γίνει έλεγχος για ενημερώσεις - ο απομακρυσμένος σέρβερ δεν μπορεί να βρεθεί.",
        [_.errorConnectionUnsupported]: "Δεν υποστηρίζεται η σύνδεση σε αυτή την αιτούμενη πηγή.",
        [_.errorDatasetConnectionUnknown]: "Υπάρχει αοριστία στην σύνδεση.",
        [_.errorDatasetsEmptyListing]: "Δεν υπάρχουν ανοικτές αναφορές.",
        [_.errorDatasetsListing]: "Αδυναμία ανάκτησης της λίστας συνόλου δεδομένων από την υπηρεσία του Power BI.",
        [_.errorExportDataFileError]: "Κάτι πήγε λάθος κατά την εξαγωγή των δεδομένων. Παρακαλώ δοκίμασε ξανά.",
        [_.errorManageDateTemplateError]: "Παρουσιάστηκε σφάλμα κατά την εκτέλεση του DAX template engine.",
        [_.errorNetworkError]: "Δεν είσαι συνδεδεμένος στο Internet.",
        [_.errorNone]: "Απροσδιόριστο σφάλμα.",
        [_.errorNotAuthorized]: "Δεν έχεις την εξουσιοδότηση για να δείτε τη συγκεκριμένο πηγή.",
        [_.errorNotConnected]: "Δεν είσαι συνδεδεμένος στο Power BI - Συνδέσου για να συνεχίσεις.",
        [_.errorNotFound]: "Αδυναμία σύνδεσης στην συγκεκριμένη πηγή.",
        [_.errorReportConnectionUnknown]: "Μη έγκυρη σύνδεση.",
        [_.errorReportConnectionUnsupportedAnalysisServecesCompatibilityMode]: "Η συμβατότητα του Power BI Desktop Analysis Services instance δεν είναι PowerBI.",
        [_.errorReportConnectionUnsupportedAnalysisServecesConnectionNotFound]: "Δεν εντοπίστηκε σύνδεση TCP με τα Power BI Desktop Analysis Services.",
        [_.errorReportConnectionUnsupportedAnalysisServecesProcessNotFound]: "Δεν εντοπίστηκε ενεργό Power BI Desktop Analysis Services instance.", 
        [_.errorReportConnectionUnsupportedConnectionException]: "Δημιουργήθηκε σφάλμα σύνδεσης με το Power BI Desktop Analysis Services instance.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionIsEmpty]: "Το Power BI Desktop Analysis Services instance δεν περιέχει κάποια βάση δεδομένων. Προσπάθησε να συνδεθείς σε μια αναφορά χρησιμοποιώντας το εικονίδιο του Bravo στα εξωτερικά εργαλεία του Power BI Desktop.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionUnexpectedCount]: "Το Power BI Desktop Analysis Services instance περιέχει ένα μη αναμενόμενο αριθμό από βάσεις δεδομένων (> 1) ενώ θα έπρεπε να είναι μηδέν ή μία.",
        [_.errorReportConnectionUnsupportedProcessNotYetReady]: "Το Power BI Desktop προσπαθεί να ανοίξει ή το Analysis Services instance ακόμα δεν είναι σε κατάσταση λειτουργίας.", 
        [_.errorReportsEmptyListing]: "Δεν υπάρχουν διαθέσιμες ανοικτές αναφορές.",
        [_.errorRetry]: "Εκ νέου προσπάθεια",
        [_.errorSignInMsalExceptionOccurred]: "Αναπάντεχο σφάλμα κατά την προσπάθεια σύνδεσης.",
        [_.errorSignInMsalTimeoutExpired]: "Η προσπάθεια σύνδεσης διακόπηκε επειδή πέρασε ο χρόνος εξαιτίας λήξης του χρόνου που έχει οριστεί για την ολοκλήρωση της(timeout).",
        [_.errorTimeout]: "Λήξη χρόνου(timeout).",
        [_.errorTitle]: "Όυπςςς",
        [_.errorTOMDatabaseDatabaseNotFound]: "Η βάση δεδομένων δεν υπάρχει στην συλλογή των βάσεων ή ο χρήστης δεν έχει δικαιώματα διαχειριστή ώστε να μπορεί να την προσπελάσει.",
        [_.errorTOMDatabaseUpdateConflictMeasure]: "Η ενημέρωση αυτή δεν μπορεί να πραγματοποιηθεί καθώς έρχεται σε αντίθεση με την τρέχουσα κατάσταση της πηγής που θέλεις να εφαρμοστεί.",
        [_.errorTOMDatabaseUpdateErrorMeasure]: "Η ενημέρωση αυτή απέτυχε καθώς μία ή περισσότερες μετρήσεις περιέχουν σφάλματα.", 
        [_.errorTOMDatabaseUpdateFailed]: "Η ενημέρωση της βάσης δεδομένων απέτυχε κατά την στιγμή που προσπάθησε να αποθηκεύσει τις τοπικές αλλαγές στο μοντέλο που υπάρχει στον σέρβερ των βάσεων δεδομένων.",
        [_.errorTryingToUpdateMeasuresWithErrors]: `Η ενημέρωση αυτή απέτυχε επειδή οι παρακάτω μετρήσεις περιέχουν σφάλματα:<br><strong>{measures}</strong>`,
        [_.errorUnhandled]: "Μη διαχειριστικό σφάλμα - Παρακαλούμε να μας ενημερώσετε για αυτό σημειώνοντας το κωδικό εντοπισμού του (trace id), αν αυτό είναι διαθέσιμο.",
        [_.errorUnspecified]: "Απροσδιόριστο σφάλμα.",
        [_.errorUserSettingsSaveError]: "Οι ρυθμίσεις δεν κατέστη δυνατόν να αποθηκευτούν.",
        [_.errorVpaxFileImportError]: "Προέκυψε σφάλμα κατά την εξαγωγή του αρχείου VPAX.",
        [_.errorVpaxFileExportError]: "Προέκυψε σφάλμα κατά την εξαγωγή του αρχείου VPAX.",
        [_.expandAllCtrlTitle]: "Ανάπτυξη όλων",
        [_.ExportData]: "Εξαγωγή δεδομένων",
        [_.exportDataCSVCustomDelimiter]: "Προσαρμοσμένος οριοθέτης πεδίου",
        [_.exportDataCSVDelimiter]: "Οριοθέτης Πεδίων",
        [_.exportDataCSVDelimiterComma]: "Κόμμα",
        [_.exportDataCSVDelimiterDesc]: `Επιλογή του χαρακτήρα που θα χρησιμοποιηθεί σαν οριοθέτης για κάθε πεδίο. <em>Αυτόματα</em> θα επιλεχθεί ο εξ ορισμού προεπιλεγμένος χαρακτήρας με βάση την ρύθμιση γλώσσας που έχει οριστεί στο σύστημα σου.`,
        [_.exportDataCSVDelimiterOther]: "Ακόμα...",
        [_.exportDataCSVDelimiterPlaceholder]: "Χαρακτήρας",
        [_.exportDataCSVDelimiterSemicolon]: "Χαρακτήρας Semicolon",
        [_.exportDataCSVDelimiterSystem]: "Αυτόματος",
        [_.exportDataCSVDelimiterTab]: "Χαρακτήρας Tab",
        [_.exportDataCSVEncoding]: "Κωδικοποίηση",
        [_.exportDataCSVEncodingDesc]: "",
        [_.exportDataCSVQuote]: "Βάλε εισαγωγικά στα αλφαριθμητικά",
        [_.exportDataCSVQuoteDesc]: "Βεβαιώσου ότι τα αλφαριθμητικά είναι μέσα σε διπλά εισαγωγικά.",
        [_.exportDataExcelCreateExportSummary]: "Εξαγωγή Περίληψης",
        [_.exportDataExcelCreateExportSummaryDesc]: "Προσθήκη ενός ακόμα φύλλου στο εξαγόμενο αρχείο το οποίο θα περιέχει την περίληψη της εργασίας.",
        [_.exportDataExport]: "Εξαγωγή Επιλεγμένων",
        [_.exportDataExportAs]: "Εξαγωγή ως",
        [_.exportDataExportAsDesc]: "",
        [_.exportDataExporting]: "Γίνεται εξαγωγή του {table}...",
        [_.exportDataExportingDone]: "Ολοκληρώθηκε!",
        [_.exportDataNoColumns]: "Δεν μπορεί να γίνει εξαγωγή του πίνακα επειδή δεν περιέχει καμία κολώνα.",
        [_.exportDataNotQueryable]: "Δεν μπορεί να γίνει εξαγωγή του πίνακα επειδή περιέχει μία ή περισσότερες υπολογιζόμενες κολώνες με μη έγκυρες εκφράσεις ή αυτές χρειάζονται επανυπολογισμό.",
        [_.exportDataOpenFile]: "Άνοιγμα Εξαγόμενου Αρχείου",
        [_.exportDataOpenFolder]: "Άνοιγμα Φακέλου Εξαγωγής",
        [_.exportDataOptions]: "Παράμετροι Εξαγωγής",
        [_.exportDataStartExporting]: "Αρχικοποίηση...",
        [_.exportDataSuccessSceneMessage]: "<strong>{count}/{total} πίνακας{{ες}}</strong> {{έγιναν|έγινε}} εξαγωγή με επιτυχία.",
        [_.exportDataSummary]: "Το σετ δεδομένων περιέχει <strong>{count} πίνακα{{ες}}</strong> για εξαγωγή.",
        [_.exportDataTypeCSV]: "CSV (Comma-separated values)",
        [_.exportDataTypeXLSX]: "Φύλλο Excel",
        [_.failed]: "Αποτυχία",
        [_.filterMeasuresWithErrorsCtrlTitle]: "Προβολή μόνο των μη μορφοποιημένων μετρήσεων με σφάλματα",
        [_.filterUnrefCtrlTitle]: "Προβολή μόνο των κολώνων που δεν χρησιμοποιούνται",
        [_.formattingMeasures]: "Μορφοποίηση των μετρήσεων...",
        [_.goBackCtrlTitle]: "Ακύρωση και επιστροφή",
        [_.groupByTableCtrlTitle]: "Ομαδοποίηση ανά Πίνακα",
        [_.helpConnectVideo]: "Πως θα συνδεθείς",
        [_.helpCtrlTitle]: "Βοήθεια",
        [_.hideUnsupportedCtrlTitle]: "Μόνο τα υποστηριζόμενα",
        [_.less]: "Λιγότερα",
        [_.license]: "Εκχώρηση με MIT αδειοδότηση.",
        [_.loading]: "Φόρτωση...",
        [_.ManageDates]: "Διαχείριση Ημ/νιών",
        [_.manageDatesApplyCtrlTitle]: "Εφαρμογή Αλλαγών",
        [_.manageDatesAuto]: "Αυτόματα",
        [_.manageDatesAutoScan]: "Αυτόματη Σάρωση",
        [_.manageDatesAutoScanActiveRelationships]: "Ενεργή Σχέση",
        [_.manageDatesAutoScanDesc]: "Επιλογή <em>Πλήρης</em> για το έλεγχο των κολώνων που περιέχουν ημερομηνίες. Ορισμός <em>Επιλογή Κολώνων...</em> για την επιλογή των κολώνων που θα χρησιμοποιηθούν. Ορισμός <em>Ενεργών Σχέσεων</em> και <em>Ανενεργών Σχέσεων</em> για την εύρεση μόνων των κολώνων με σχέσεις.",
        [_.manageDatesAutoScanDisabled]: "Απενεργοποιημένο",
        [_.manageDatesAutoScanFirstYear]: "Πρώτος Χρόνος",
        [_.manageDatesAutoScanFirstYearDesc]: "",
        [_.manageDatesAutoScanFull]: "Πλήρης",
        [_.manageDatesAutoScanInactiveRelationships]: "Ανενεργή Σχέση",
        [_.manageDatesAutoScanLastYear]: "Τελευταίος Χρόνος",
        [_.manageDatesAutoScanLastYearDesc]: "",
        [_.manageDatesAutoScanSelectedTablesColumns]: "Επιλογή Πεδίων/Κολώνων...",
        [_.manageDatesBrowserPlaceholder]: "Δεν υπάρχουν αντικείμενα για να αλλάξουν",
        [_.manageDatesCalendarDesc]: "Επιλογή ενός υποδείγματος ημερολογίου που θα ενταχθεί στο μοντέλο αυτό. Το Bravo θα δημιουργήσει τους απαραίτητους πίνακες ή θα ενημερώσει αυτούς και παράλληλα θα διατηρήσει τις υπάρχουσες σχέσεις αναλλοίωτες.", 
        [_.manageDatesCalendarTemplateName]: "Υπόδειγμα Ημερολογίου",
        [_.manageDatesCalendarTemplateNameDesc]: "Επιλογή <em>Μηνιαία</em> για ημερολόγιο με διαφορετικό αριθμό μηνών. Ορισμός <em>Εβδομαδιαία</em> για 445-454-544-ISO ημερολόγια. Χρησιμοποίηση <em>Προσαρμογή</em> για ημερολόγια μεταβλητού μεγέθους.",
        [_.manageDatesCreatingTables]: "Ενημέρωση μοντέλου...",
        [_.manageDatesDatesDesc]: "Ρύθμιση της μορφοποίησης και χώρας για τις ημερομηνίες στο μοντέλο σου.",
        [_.manageDatesDatesTableDesc]: "Αυτός είναι ο πίνακας για χρήση στις αναφορές για τις ημερομηνίες.",
        [_.manageDatesDatesTableName]: "Πίνακας Ημ/νιών",
        [_.manageDatesDatesTableReferenceDesc]: "Αυτός είναι ένας κρυφός πίνακας ο οποίος περιέχει όλα τα DAX functions που χρησιμοποιούνται για την δημιουργία των ημερομηνιών.",
        [_.manageDatesDatesTableReferenceName]: "Πίνακας Ορισμού Ημ/νιών",
        [_.manageDatesHolidaysDesc]: "Πρόσθεσε αργίες στο μοντέλο σου. Το Bravo θα δημιουργήσει τους απαραίτητους πίνακες ή θα ενημερώσει αυτούς ενώ παράλληλα θα διατηρήσει τις υπάρχουσες σχέσεις αναλλοίωτες. ",
        [_.manageDatesHolidaysEnabledDesc]: "Πρόσθεσε το πίνακα αργιών στο μοντέλο σου.",
        [_.manageDatesHolidaysEnabledName]: "Αργίες",
        [_.manageDatesHolidaysTableDefinitionDesc]: "Αυτός είναι ο κρυφός πίνακας ο οποίος περιέχει όλα τα DAX functions που χρησιμοποιούνται για την δημιουργία των αργιών.",
        [_.manageDatesHolidaysTableDefinitionName]: "Ορισμός πίνακα Αργιών",
        [_.manageDatesHolidaysTableDesc]: "Αυτός είναι ο πίνακας για χρήση στις αναφορές για τις αργίες.",
        [_.manageDatesHolidaysTableName]: "Πίνακας Αργιών",
        [_.manageDatesIntervalDesc]: "Επέλεξε τα μεσοδιαστήματα ημ/νιών για το μοντέλο σου.",
        [_.manageDatesISOCountryDesc]: "",
        [_.manageDatesISOCountryName]: "Χώρα Αργιών",
        [_.manageDatesISOCustomFormatDesc]: "Καταχώρισε τη μορφοποίηση της χώρας που θα χρησιμοποιηθεί για την IETF BCP 47 ετικέτα γλώσσας. π.χ. en-US",
        [_.manageDatesISOCustomFormatName]: "Προσαρμοσμένη μορφοποίηση",
        [_.manageDatesISOFormatDesc]: "Επέλεξε την μορφοποίηση χώρας για τις ημερομηνίες.",
        [_.manageDatesISOFormatName]: "Μορφοποίηση χώρας",
        [_.manageDatesISOFormatOther]: "Ακόμα...",
        [_.manageDatesISOFormatOtherPlaceholder]: "Περιοχή",
        [_.manageDatesMenuCalendar]: "Ημερολόγιο",
        [_.manageDatesMenuDates]: "Ημ/νίες",
        [_.manageDatesMenuHolidays]: "Αργίες",
        [_.manageDatesMenuInterval]: "Μεσοδιαστήματα",
        [_.manageDatesMenuPreviewCode]: "Παραστάσεις",
        [_.manageDatesMenuPreviewModel]: "Αλλαγές Μοντέλου",
        [_.manageDatesMenuPreviewTable]: "Δείγμα δεδομένων",
        [_.manageDatesMenuPreviewTreeDate]: "Ημ/νίες",
        [_.manageDatesMenuPreviewTreeDateHolidays]: "Ημ/νίες & Αργίες",
        [_.manageDatesMenuPreviewTreeTimeIntelligence]: "Χρονική νοημοσύνη",
        [_.manageDatesMenuTimeIntelligence]: "Χρονική νοημοσύνη",
        [_.manageDatesModelCheck]: "Έλεγχος Μοντέλου",
        [_.manageDatesPreview]: "Προεπισκόπηση",
        [_.manageDatesPreviewCtrlTitle]: "Προεπισκόπηση Αλλαγών",
        [_.manageDatesSampleData]: "Δείγμα δεδομένων",
        [_.manageDatesSampleDataError]: "Αδυναμία δημιουργίας δείγματος δεδομένων.",
        [_.manageDatesStatusCompatible]: `<div class="hero">Το μοντέλο αυτό ήδη περιέχει <b>συμβατούς πίνακες ημ/νιών</b> με το Bravo.</div>Αλλάζοντας κάτι εδώ, αυτοί οι πίνακες θα ενημερωθούν και οι σχέσεις τους θα παραμείνουν ως έχουν.`,
        [_.manageDatesStatusError]: `<div class="hero">Οι τρέχουσες ρυθμίσεις δεν μπορούν αν εφαρμοστούν.</div>{error}`,
        [_.manageDatesStatusIncompatible]: `<div class="hero">Το μοντέλο αυτό περιέχει <b>πίνακες ημ/νιών που δεν είναι συμβατοί</b> με το Bravo.</div>Κάνοντας αλλαγές εδώ θα χρειαστεί να διαλέξεις διαφορετικό όνομα πίνακα για κάθε πίνακα που θα δημιουργηθεί από το εργαλείο αυτό.<br><br>Έλεγχος για <b>Ημ/νίες</b> και <b>Αργίες</b>.`,
        [_.manageDatesStatusNotAvailable]: `<div class="hero">Αυτό το μοντέλο δεν πλέον διαθέσιμο.</div> Προσπαθήσε να κάνει επανεκίνηση την εφαρμογή.`,
        [_.manageDatesStatusOk]: `<div class="hero">Το μοντέλο αυτό <b>είναι συμβατό με την δυνατότητα που δίνει η Διαχείριση Ημ/νιών</b>.</div>Μπορείς να δημιουργήσεις ένα νέο πίνακα ημ/νιών χωρίς να ανησυχείς ότι θα καταστραφούν οι μετρήσεις ή οι αναφορές.`,
        [_.manageDatesSuccessSceneMessage]: "Συγχαρητήρια, το μοντέλο σου ενημερώθηκε με επιτυχία.",
        [_.manageDatesTemplateFirstDayOfWeek]: "Πρώτη Ημέρα της Εβδομάδας",
        [_.manageDatesTemplateFirstDayOfWeekDesc]: "Για το Εβδομαδιαίο ISO, όρισε την <em>Δευτέρα</em>.",
        [_.manageDatesTemplateFirstFiscalMonth]: "Πρώτος Μήνας του Έτους",
        [_.manageDatesTemplateFirstFiscalMonthDesc]: "Για το Εβδομαδιαίο ISO, όρισε τον <em>Ιανουάριο</em>.",
        [_.manageDatesTemplateMonthsInYear]: "Μήνες στο Έτος",
        [_.manageDatesTemplateMonthsInYearDesc]: "",
        [_.manageDatesTemplateNameConfig01]: "Καθιερωμένο",
        [_.manageDatesTemplateNameConfig02]: "Μηνιαία",
        [_.manageDatesTemplateNameConfig03]: "Προσαρμογή",
        [_.manageDatesTemplateNameConfig04]: "Εβδομαδιαία",
        [_.manageDatesTemplateQuarterWeekType]: "Εβδομαδιαίο Σύστημα",
        [_.manageDatesTemplateQuarterWeekTypeDesc]: "",
        [_.manageDatesTemplateTypeStartFiscalYear]: "Πρώτη ημέρα του Οικονομικού Έτους",
        [_.manageDatesTemplateTypeStartFiscalYearDesc]: "Για το Εβδομαδιαίο ISO, όρισε το <em>Τέλος του Έτους</em>.",
        [_.manageDatesTemplateTypeStartFiscalYearFirst]: "Αρχή του Έτους",
        [_.manageDatesTemplateTypeStartFiscalYearLast]: "Τέλος του Έτους",
        [_.manageDatesTemplateWeeklyType]: "Τελευταίο ΣΚ του Έτους",
        [_.manageDatesTemplateWeeklyTypeDesc]: "Εάν η εβδομάδα σου ξεκινάει με Κυριακή τότε το τέλος της είναι Σάββατο. Αν επιλέξεις το <em>Τέλος του Έτους</em> το οικονομικό έτος τελειώνει το τελευταίο Σάββατο του τελευταίου μήνα. Σε κάθε άλλη περίπτωση, το οικονομικό έτος τελειώνει το Σάββατο που είναι πλησιέστερα στην τελευταία ημ/νία του τελευταίου μήνα - μπορεί να είναι και στον επόμενο χρόνο. Για το εβδομαδιαίο ISO, επέλεξε το <em>Πλησιέστερα στο Τέλος του Έτους</em>.",
        [_.manageDatesTemplateWeeklyTypeLast]: "Τέλος του Έτους",
        [_.manageDatesTemplateWeeklyTypeNearest]: "Πλησιέστερα στο Τέλος του Έτους",
        [_.manageDatesTimeIntelligenceDesc]: "Δημιούργησε τις περισσότερο χρησιμοποιούμενες DAX συναρτήσεις Χρονικής Νοημοσύνης στο μοντέλο σου.",
        [_.manageDatesTimeIntelligenceEnabledDesc]: "",
        [_.manageDatesTimeIntelligenceEnabledName]: "Συναρτήσεις Χρονική Νοημοσύνης",
        [_.manageDatesTimeIntelligenceTargetMeasuresAll]: "Όλες οι Μετρήσεις",
        [_.manageDatesTimeIntelligenceTargetMeasuresChoose]: "Επιλογή Μετρήσεων...",
        [_.manageDatesTimeIntelligenceTargetMeasuresDesc]: "Επέλεξε τις μετρήσεις που θα χρησιμοποιηθούν για την δημιουργία των συναρτήσεων Χρονικής νοημοσύνης.",
        [_.manageDatesTimeIntelligenceTargetMeasuresName]: "Στοχευμένες Μετρήσεις",
        [_.manageDatesYearRange]: "Μεσοδιάστημα Ημ/νιών",
        [_.manageDatesYearRangeDesc]: "Επέλεξε το πως θα ορίσεις το μεσοδιάστημα ημ/νιών στο μοντέλο σου. Άφησε το <em>Πρώτο Έτος</em> και/ή το <em>Τελευταίο Έτος</em> κενό για να γίνει αυτόματη εύρεση.",
        [_.menuCtrlTitle]: "Σύμπτυξη/Ανάπτυξη μενού",
        [_.minimizeCtrlTitle]: "Ελαχιστοποίηση",
        [_.modelLanguage]: "Γλώσσα Μοντέλου ({culture})",
        [_.more]: "Περισσότερα",
        [_.notificationCtrlTitle]: "Ειδοποιήσεις",
        [_.notificationsTitle]: "{count} {{ειδοποιήσεις|ειδοποίηση}}",
        [_.openSourcePayoff]: "{appName} είναι ένα εργαλείου ανοικτού λογισμικού που αναπτύχθηκε και συντηρείτε από το SQLBI και την κοινότητα Github. Συνδέσου μαζί μας στο",
        [_.openWithDaxFormatterCtrlTitle]: "Οnline μορφοποίηση με το DAX Formatter",  
        [_.optionAccount]: "Λογ/μός Power BI",
        [_.optionAccountDescription]: "Όρισε το λογ/μο με τον οποίο μπορείς να προσπελάσεις τα online σετ δεδομένων στο Power BI online.",
        [_.optionDiagnostic]: "Επίπεδο Διαγνωστικών",
        [_.optionDiagnosticDescription]: "Προβολή σφαλμάτων και μηνυμάτων καταγραφής στο διαγνωστικό παράθυρο. Επέλεξε <em>Βασική</em> για την καταγραφή μερικών μηνυμάτων. <em>Αναλυτική</em> για την καταγραφή όλων των μηνυμάτων.",
        [_.optionDiagnosticLevelBasic]: "Βασική",
        [_.optionDiagnosticLevelNone]: "Καθόλου",
        [_.optionDiagnosticLevelVerbose]: "Αναλυτική",
        [_.optionDiagnosticMore]: "Για την αναφορά κάποιου προβλήματος στην εφαρμογή, παρακαλούμε κάνει χρήση",
        [_.optionFormattingBreaks]: "Διαχωρισμός ονοματολογίας",
        [_.optionFormattingBreaksAuto]: "Αυτόματα",
        [_.optionFormattingBreaksDescription]: "Επέλεξε πως θα διαχωρίζονται οι μετρήσεις και οι εκφράσεις. Όρισε <em>Αυτόματα</em> για τον αυτόματο τρόπο ορισμού που θα χρησιμοποιηθεί στο μοντέλο.",
        [_.optionFormattingBreaksInitial]: "Διαχώριση Γραμμής",
        [_.optionFormattingBreaksNone]: "Ίδια Γραμμή",
        [_.optionFormattingIncludeTimeIntelligence]: "Συμπεριλάβετε το χρόνο νοημοσύνης",
        [_.optionFormattingIncludeTimeIntelligenceDescription]: "Συμπεριλάβετε τα μέτρα που δημιουργούνται αυτόματα με τις ημερομηνίες διαχείρισης για το χρόνο πληροφοριών.",
        [_.optionFormattingLines]: "Γραμμές",
        [_.optionFormattingLinesDescription]: "Επέλεξε το αν θα κρατήσεις μεγάλες ή μικρές γραμμές.",
        [_.optionFormattingLinesValueLong]: "Μεγάλες γραμμές",
        [_.optionFormattingLinesValueShort]: "Μικρές γραμμές",
        [_.optionFormattingPreview]: "Αυτόματη Προεπισκόπηση",
        [_.optionFormattingPreviewDescription]: "Αυτόματη αποστολή των μετρήσεων στο DAX Formatter για την δημιουργία προεπισκόπησης.",
        [_.optionFormattingSeparators]: "Διαχωριστές",
        [_.optionFormattingSeparatorsDescription]: "Επιλογή των διαχωριστικών σημείων σε αριθμούς και λίστες.",
        [_.optionFormattingSeparatorsValueAuto]: "Αυτόματα",
        [_.optionFormattingSeparatorsValueEU]: "A; B; C; 1234,00",
        [_.optionFormattingSeparatorsValueUS]: "A, B, C, 1234.00",
        [_.optionFormattingSpaces]: "Απόσταση μεταξύ",
        [_.optionFormattingSpacesDescription]: "Διαχείριση κενών μετά από τα ονόματα των συναρτήσεων.",
        [_.optionFormattingSpacesValueBestPractice]: "Βέλτιστη πρακτική",
        [_.optionFormattingSpacesValueFalse]: "Χωρίς κενό - IF( ",
        [_.optionFormattingSpacesValueTrue]: "Κενό - IF ( ",
        [_.optionLanguage]: "Γλώσσα",
        [_.optionLanguageDescription]: "Επιλογή της γλώσσας στο Bravo. Απαιτείται επανεκκίνηση.",
        [_.optionLanguageResetConfirm]: "Θέλεις να επανεκκινήσεις το Bravo και να εφαρμόσεις την νέα επιλεγμένη γλώσσα;",
        [_.optionsDialogAboutMenu]: "Σχετικά",
        [_.optionsDialogFormattingMenu]: "Μορφοποίηση",
        [_.optionsDialogGeneralMenu]: "Γενικά",
        [_.optionsDialogTelemetryMenu]: "Διαγνωστικά",
        [_.optionsDialogTitle]: "Επιλογές",
        [_.optionTelemetry]: "Τηλεμετρία",
        [_.optionTelemetryDescription]: "Ανώνυμη αποστολή δεδομένων χρήσης στο SQLBI.",
        [_.optionTelemetryMore]: "Βοήθησε μας να κατανοήσουμε πως χρησιμοποιείς το Bravo ώστε να το βελτιώσουμε. Δεν συγκεντρώνονται προσωπικές πληροφορίες. Να επισημανθεί ότι αν αυτή η επιλογή είναι απενεργοποιημένη, η ομάδα ανάπτυξης δεν είναι σε θέση να συλλέξει τα οποιαδήποτε μη ελεγχόμενα σφάλματα εμφανιστούν ώστε να μπορεί να τα υποστηρίξει.",
        [_.optionTheme]: "Θέμα",
        [_.optionThemeDescription]: "Επιλογή του θέματος για το Bravo. Αφήνοντας του <em>Συστήματος</em> επιτρέπεται στο λειτουργικό να διαλέξει.",
        [_.optionThemeValueAuto]: "Συστήματος",
        [_.optionThemeValueDark]: "Σκοτεινό",
        [_.optionThemeValueLight]: "Ανοικτό",
        [_.otherColumnsRowName]: "Μικρότερες κολώνες...",
        [_.paste]: "Επικόλληση",
        [_.powerBiObserving]: "Αναμονή για το άνοιγμα αρχείου από το Power BI Desktop...",
        [_.powerBiObservingCancel]: "Ακύρωση",
        [_.powerBiSigninDescription]: "Είσοδος στο Power BI Service για να συνδεθεί το Bravo στο online σετ δεδομένων.",
        [_.powerBiSigninTitle]: "Power BI",
        [_.quickActionAttachPBITitle]: "Προσάρτηση στο Power BI Desktop",
        [_.quickActionConnectPBITitle]: "Σύνδεση στο Power BI Service",
        [_.quickActionOpenVPXTitle]: "Άνοιγμα ενός Vertipaq Analyzer αρχείου",
        [_.refreshCtrlTitle]: "Ανανέωση",
        [_.refreshPreviewCtrlTitle]: "Ανανέωση προεπισκόπησης",
        [_.saveVpaxCtrlTile]: "Αποθήκευση ως VPAX",
        [_.savingVpax]: "Δημιουργία VPAX...",
        [_.sceneUnsupportedReason]: "Η λειτουργία αυτή είναι διαθέσιμη μόνο για αυτή τη πηγή δεδομένων.",
        [_.sceneUnsupportedReasonManageDatesAutoDateTimeEnabled]: `Μοντέλα με ενεργοποιημένη την επιλογή Αυτόματης ημερομηνίας/ώρας δεν υποστηρίζονται.<br><span class="link" href="https://www.sqlbi.com/tv/disabling-auto-date-time-in-power-bi/">Απενεργοποίηση της Αυτόματης επιλογής ημερομηνίας/ώρας(βίντεο)</span>`,
        [_.sceneUnsupportedReasonManageDatesEmptyTableCollection]: "Η λειτουργία αυτή υποστηρίζεται μόνο σε βάσεις δεδομένων που έχουν τουλάχιστον ένα πίνακα.",
        [_.sceneUnsupportedReasonManageDatesPBIDesktopModelOnly]: "Αυτή η λειτουργία υποστηρίζεται μόνο από μοντέλα που είναι στο Power BI Desktop.",
        [_.sceneUnsupportedReasonMetadataOnly]: "Η βάση δεδομένων δημιουργήθηκε από το VPAX αρχείο και περιέχει μόνο τα μεταδεδομένα.",
        [_.sceneUnsupportedReasonReadOnly]: "Η επικοινωνία με αυτή την βάση δεδομένων είναι σε κατάσταση ανάγνωσης μόνο.",
        [_.sceneUnsupportedReasonXmlaEndpointNotSupported]: "Το XMLA άκρο δεν υποστηρίζεται για αυτό το σετ δεδομένων.",
        [_.sceneUnsupportedTitle]: "Δεν υποστηρίζεται",
        [_.searchColumnPlaceholder]: "Αναζήτηση Κολώνας",
        [_.searchDatasetPlaceholder]: "Αναζήτηση σετ δεδομένων",
        [_.searchEntityPlaceholder]: "Αναζήτηση Πίνακα/Κολώνας",
        [_.searchMeasurePlaceholder]: "Αναζήτηση Μέτρησης",
        [_.searchPlaceholder]: "Αναζήτηση",
        [_.searchTablePlaceholder]: "Αναζήτηση πίνακα",
        [_.settingsCtrlTitle]: "Επιλογές",
        [_.sheetOrphan]: "Μη διαθέσιμο",
        [_.sheetOrphanPBIXTooltip]: "Η αναφορά αυτή δεν είναι ανοικτή στο Power BI Desktop. Οποιαδήποτε ενέργεια καταχώρησης ή αλλαγής δεν επιτρέπεται.",
        [_.sheetOrphanTooltip]: "Αυτό το μοντέλο δεν είναι πλέον διαθέσιμο. Οποιαδήποτε ενέργεια ή λειτουργία δεν επιτρέπεται.",
        [_.showDiagnosticPane]: "Εμφάνιση Λεπτομερειών",
        [_.sideCtrlTitle]: "Τοποθέτηση σε θέση πλάι-πλάι",
        [_.signedInCtrlTitle]: "Είσοδος ως {name}",
        [_.signIn]: "Είσοδος",
        [_.signInCtrlTitle]: "Είσοδος",
        [_.signOut]: "Εξόδος",
        [_.sqlbiPayoff]: "Το Bravo είναι δημιουργία του SQLBI.",
        [_.syncCtrlTitle]: "Συγχρονισμός",
        [_.tableColCardinality]: "Πληθικότητα",
        [_.tableColCardinalityTooltip]: "Αριθμός από μοναδικά στοιχεία",
        [_.tableColColumn]: "Κολώνα",
        [_.tableColColumns]: "Κολώνες",
        [_.tableColMeasure]: "Μέτρηση",
        [_.tableColPath]: "Πίνακας \\ Κολώνα",
        [_.tableColRows]: "Γραμμές",
        [_.tableColSize]: "Μέγεθος",
        [_.tableColTable]: "Πίνακας",
        [_.tableColWeight]: "Συμμετοχή",
        [_.tableSelectedCount]: "{count} Επιλεγμένα",
        [_.tableValidationInvalid]: "Αυτή ονομασία δεν μπορεί να χρησιμοποιηθεί.",
        [_.tableValidationValid]: "Αυτή η ονομασία είναι έγκυρη.",
        [_.themeCtrlTitle]: "Αλλαγή θέματος",
        [_.toggleTree]: "Εναλλαγή σε δέντρο",
        [_.traceId]: "Αριθμός ίχνους",
        [_.unknownMessage]: "Ένα μη έγκυρο μήνυμα λήφθηκε",
        [_.updateChannelBeta]: "Beta",
        [_.updateChannelCanary]: "Canary",
        [_.updateChannelDev]: "Dev",
        [_.updateChannelStable]: "Stable", 
        [_.updateMessage]: "Μια νέα έκδοση του Bravo είναι διαθέσιμη: {version}",
        [_.validating]: "Έλεγχος εγκυρότητας...",
        [_.version]: "Έκδοση",
        [_.welcomeHelpText]: "Παρακολούθησε το παρακάτω βίντεο για να μάθεις πως να χρησιμοποιήσεις το Bravo:",
        [_.welcomeHelpTitle]: "Πως να χρησιμοποιήσεις το Bravo?",
        [_.welcomeText]: "Το Bravo είναι μια χρηστική Power BI εργαλειοθήκη την οποία μπορείς να χρησιμοποιήσεις για να αναλύσεις τα μοντέλα σου, να μορφοποιήσεις μετρήσεις, να δημιουργήσεις πίνακες και να εξάγεις δεδομένα.",
        [_.whitespacesTitle]: "Κενά διαστήματα",
        [_.wrappingTitle]: "Αυτόματη αναδίπλωση λέξεων",
    }
}
export default locale;