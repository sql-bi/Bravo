/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Locale } from '../i18n';
import { strings as _ } from '../strings';

const locale: Locale = {

    locale: "uk", // DO NOT TRANSLATE
    enName: "Ukrainian", // DO NOT TRANSLATE
    name: "українська",

    strings: {
        [_.addCtrlTitle]: "Відкрити",
        [_.aggregatedTableName]: "Декілька таблиць",
        [_.AnalyzeModel]: "Аналіз моделі",
        [_.analyzeModelSummary]: `Розмір вашого джерела даних <strong>{size:bytes}</strong> байт, включаючи <strong>{count}</strong> стовпчик{{s}}`,
        [_.analyzeModelSummary2]: `, <span class="text-highlight"><strong>{count}</strong> із яких {{are|is}} не згадуються в моделі.</span>`,
        [_.appExtensionName]: "Bravo Template Editor", // DO NOT TRANSLATE
        [_.appName]: "Bravo for Power BI", // DO NOT TRANSLATE
        [_.appUpdateAvailable]: "Доступна нова версія: {version}",
        [_.appUpdateChangelog]: "Журнал змін",
        [_.appUpdateDownload]: "Завантажити",
        [_.appUpdateViewDetails]: "Показати деталі",
        [_.appUpToDate]: "Актуальну версію Bravo встановлено",
        [_.appVersion]: "Версія {version}",
        [_.backupReminder]: "Перед початком роботи, не забудьте зробити резервну копію свого звіту - <b>деякі зміни неможливо скасувати</b>.",
        [_.BestPractices]: "Кращі практики",
        [_.canceled]: "Скасовано",
        [_.changeStatusAdded]: "A",
        [_.changeStatusAddedTitle]: "Додано",
        [_.changeStatusDeleted]: "D",
        [_.changeStatusDeletedTitle]: "Видалено",
        [_.changeStatusModified]: "M",
        [_.changeStatusModifiedTitle]: "Модифіковано",
        [_.checking]: "Перевірка...",
        [_.clearCtrlTitle]: "Очистити",
        [_.closeCtrlTitle]: "Закрити",
        [_.closeOtherTabs]: "Закрити інші",
        [_.closeTab]: "Закрити",
        [_.collapseAllCtrlTitle]: "Згорнути все",
        [_.columnExportedCompleted]: "Таблицю успішно експортовано.",
        [_.columnExportedFailed]: "Таблицю не було експортовано через невідому помилку.",
        [_.columnExportedTruncated]: "Дану таблицю було урізано, оскільки кількість рядків перевищила максимально допустиму.",
        [_.columnMeasureFormatted]: "Даний показник вже відформатовано.",
        [_.columnMeasureNotFormattedTooltip]: "Даний показник не відформатовано",
        [_.columnMeasureWithError]: "Цей показник містить помилки",
        [_.columnUnreferencedExplanation]: `<span class="text-highlight">Стовпці без посилань</span> як правило, можна видалити з моделі для оптимізації продуктивності. Перш ніж видаляти їх, переконайтеся, що ви не використовуєте ці стовпці в будь-яких звітах, що Bravo не може визначити.`,
        [_.columnUnreferencedTooltip]: "В даній моделі немає посилань на цей стовпець.",
        [_.confirmTabCloseMessage]: "Здається, ви не зберегли зміни в цьому документі.<br>Ви впевнені, що хочете закрити його?",
        [_.connectBrowse]: "Переглянути",
        [_.connectDatasetsTableEndorsementCol]: "Підтвердити",
        [_.connectDatasetsTableNameCol]: "Ім'я",
        [_.connectDatasetsTableOwnerCol]: "Власник",
        [_.connectDatasetsTableWorkspaceCol]: "Робочий простір",
        [_.connectDialogAttachPBIMenu]: "Джерела даних на powerbi.com",
        [_.connectDialogConnectPBIMenu]: "Активні звіти на Power BI Desktop",
        [_.connectDialogOpenVPXMenu]: "VPAX Файли",
        [_.connectDialogTitle]: "Відкрити",
        [_.connectDragFile]: "Перемістіть файл VPAX сюди<br>або вкажіть розташування файлу натиснувши кнопку Огляд",
        [_.connectNoReports]: "Активних звітів не знайдено.<br>Відкрийте зіт за допомогою Power BI Desktop та зачекайте, коли він з'явиться тут.",
        [_.copiedErrorDetails]: "Скопійовано!",
        [_.copy]: "Копіювати",
        [_.copyErrorDetails]: "Помилка копіювання",
        [_.copyFormulaCtrlTitle]: "Копіювати відформатований показник",
        [_.copyMessage]: "Скопіювати повідомлення",
        [_.copyright]: "Всі права захищені.",
        [_.createIssue]: "Повідомити про помилку",
        [_.cut]: "Вирізати",
        [_.dataUsageLink]: "Як використовуються ваші дані?", 
        [_.dataUsageMessage]: `Щоб відформатувати ваш код, Bravo надсилає показники цього набору даних у DAX Formatter, службу, керовану SQLBI, через безпечне з'єднання.<p><strong>Служба ніде не зберігає ваші дані.</strong></p><p>Деяка інформація, як функції DAX, які використовуються найчастіше, індекс складності та середня довжина запиту, може бути збережена для статистичних цілей.</p>`,
        [_.dataUsageTitle]: "Як використовуються ваші дані?",
        [_.DaxFormatter]: "Форматувати DAX",
        [_.daxFormatterAgreement]: "Для форматування DAX Bravo надсилає ваші вимірювання до служби форматування DAX.",
        [_.daxFormatterAnalyzeConfirm]: "Щоб виконати аналіз, Bravo має надіслати всі вимірювання до служби DAX Formatter. Ви впевнені, що хочете продовжити?",
        [_.daxFormatterAutoPreviewOption]: "Автоматичний попередній перегляд",
        [_.daxFormatterFormat]: "Формат обрано",
        [_.daxFormatterFormatDisabled]: "Формат (не підтримується)",
        [_.daxFormatterFormattedCode]: "Відформатовано (Попередній перегляд)",
        [_.daxFormatterOriginalCode]: "Поточний",
        [_.daxFormatterPreviewAllButton]: "Переглянути всі показники",
        [_.daxFormatterPreviewButton]: "Попередній перегляд",
        [_.daxFormatterPreviewDesc]: "Для створення попереднього перегляду Bravo необхідно відправити цей показник в сервіс DAX Formatter.",
        [_.daxFormatterSuccessSceneMessage]: "Вітаємо, <strong>{count} показник{{s}}</strong> {{were|was}} було успішно відформатовано.",
        [_.daxFormatterSummary]: `Ваш набір даних містить {count} {{показник|показники}}: <span class="text-error"><strong>{errors:number}</strong> з помилками</strong></span>, <span class="text-highlight"><strong>{formattable:number}</strong> для форматування</span>, <strong>{analyzable:number}</strong> для аналізу (<span class="link manual-analyze">проаналізуй зараз</span>).`,
        [_.daxFormatterSummaryNoAnalysis]: `Ваш набір даних містить <strong>{count}</strong> {{показник|показники}}: <span class="text-error"><strong>{errors:number}</strong> з помилками</strong></span> та <span class="text-highlight"><strong>{formattable:number}</strong> для форматування.</span>`,
        [_.defaultTabName]: "Без назви",
        [_.devCreateTemplateDialogOk]: "Створити",
        [_.devCreateTemplateLabelModel]: "На основі",
        [_.devCreateTemplateLabelName]: "Назва шаблону",
        [_.devCreateTemplateNotes]: "Натисніть <strong>Створити</strong> щоб обрати папку, в яку буде збережено проект Visual Studio Code, що містить новий шаблон.",
        [_.devCreateTemplateTitle]: "Новий шаблон даних",
        [_.devDefaultTemplateName]: "Без назви",
        [_.devShowInFolder]: "Показати у Провіднику файлів",
        [_.devTemplateRemoveConfirmation]: "Шаблон <b>{template}</b> буде видалено з цього листа, але всі існуючі файли будуть збережені в системі.<br>Ви впевнені, що хочете продовжити?",
        [_.devTemplatesBrowse]: "Переглянути",
        [_.devTemplatesColAction]: "Дії",
        [_.devTemplatesColName]: "Шаблон",
        [_.devTemplatesColType]: "Тип",
        [_.devTemplatesCreate]: "Новий шаблон",
        [_.devTemplatesEdit]: "Редагувати",
        [_.devTemplatesEditTitle]: "Редагувати в Visual Studio Code",
        [_.devTemplatesEmpty]: "Користувацьких шаблонів даних не знайдено.",
        [_.devTemplatesNotAvailable]: "Цей шаблон більше не доступний",
        [_.devTemplatesRemove]: "Видалити",
        [_.devTemplatesTypeOrganization]: "Організація",
        [_.devTemplatesTypeUser]: "Користувач",
        [_.devTemplatesVSCodeDownload]: "Завантажити Visual Studio Code",
        [_.devTemplatesVSCodeExtensionDownload]: "Завантажити {extension} додаток",
        [_.devTemplatesVSCodeMessage]: "<p>Проект шаблону данихтепер буде відкриватися в Visual Studio Code і вимагає виконання наступних дій і вимагає додатку <b>{extension}</b> для складання.</p><p>Якщо вони ще не встановлені у вашій системі, скористайтеся посиланням нижче, щоб завантажити та встановити їх:</p>",
        [_.devTemplatesVSCodeTitle]: "Відкрити з Visual Studio Code",
        [_.dialogCancel]: "Відмінити",
        [_.dialogContinue]: "Продовжити",
        [_.dialogNeverShowAgain]: "Не показувати знову",
        [_.dialogOK]: "OK",
        [_.dialogOpen]: "Відкрити",
        [_.docLimited]: "Обмежено",
        [_.docLimitedTooltip]: "Для цього документа доступні не всі функції.",
        [_.documentation]: "Документація",
        [_.doneCtrlTitle]: "Готово",
        [_.emailAddress]: "Електронна адреса",
        [_.emailAddressPlaceholder]: "Введіть вашу електронну адресу",
        [_.error]: "Помилка",
        [_.errorAborted]: "Операцію скасовано.",
        [_.errorAnalysisServicesConnectionFailed]: "Виникає проблема з'єднання між сервером і Bravo.",
        [_.errorCheckForUpdates]: "Неможливо перевірити наявність оновлень - віддалений сервер недоступний.",
        [_.errorConnectionUnsupported]: "З'єднання із запитуваним ресурсом не підтримується.",
        [_.errorDatasetConnectionUnknown]: "Невизначений зв'язок.",
        [_.errorDatasetsEmptyListing]: "Відкритих звітів немає.",
        [_.errorDatasetsListing]: "Не вдається отримати список наборів даних служби Power BI.",
        [_.errorExportDataFileError]: "Під час експорту даних сталася помилка. Будь ласка, спробуйте ще раз.",
        [_.errorGetEnvironments]: "Введіть дійсний обліковий запис Power BI.",
        [_.errorManageDateNoTemplates]: "В системі недостатньо шаблонів даних. Перевірте, чи не відключив їх адміністратор.",
        [_.errorManageDateTemplateError]: "Виникла помилка при виконанні шаблонного движка DAX.",
        [_.errorNetworkError]: "Ви не підключені до мережі Інтернет.",
        [_.errorNone]: "Невизначена помилка.",
        [_.errorNotAuthorized]: "Ви не авторизовані для перегляду зазначеного ресурсу.",
        [_.errorNotConnected]: "Ви не підключені до Power BI - будь ласка, увійдіть, щоб продовжити.",
        [_.errorNotFound]: "Не вдається підключитися до вказаного ресурсу.",
        [_.errorPathNotFound]: "Шлях не знайдено або у вас немає прав на його відкриття.",
        [_.errorReportConnectionUnknown]: "Невірне з'єднання.",
        [_.errorReportConnectionUnsupportedAnalysisServicesCompatibilityMode]: "Режим сумісності екземплярів Power BI Desktop Analysis Services не є режимом сумісності PowerBI.",
        [_.errorReportConnectionUnsupportedAnalysisServicesConnectionNotFound]: "Не знайдено TCP-з'єднання Power BI Desktop Analysis Services.",
        [_.errorReportConnectionUnsupportedAnalysisServicesProcessNotFound]: "Процес Power BI Desktop Analysis Services не знайдено.", 
        [_.errorReportConnectionUnsupportedConnectionException]: "Виникло виключення при підключенні до екземпляру Power BI Desktop Analysis Services.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionEmpty]: "Екземпляр Power BI Desktop Analysis Services не містить жодних баз даних. Спробуйте підключитися до звіту за допомогою іконки Bravo в розділі Зовнішні інструменти Power BI Desktop.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionUnexpectedCount]: "Екземпляр Power BI Desktop Analysis Services містить неочікувану кількість баз даних (> 1) очікується нуль або одиниця.",
        [_.errorReportConnectionUnsupportedProcessNotReady]: "Запускається процес Power BI Desktop або екземпляр Analysis Services ще не готовий.", 
        [_.errorReportsEmptyListing]: "Невідкритих звітів немає.",
        [_.errorRetry]: "Спробувати повторно",
        [_.errorSignInMsalExceptionOccurred]: "Неочікувана помилка в запиті на вхід.",
        [_.errorSignInMsalTimeoutExpired]: "Запит на вхід було скасовано, оскільки час очікування закінчився до завершення операції.",
        [_.errorTemplateAlreadyExists]: "Інший шаблон з таким самим шляхом/назвою вже існує: <br><b>{name}</b>",
        [_.errorTimeout]: "Запросити перерву",
        [_.errorTitle]: "Упс...",
        [_.errorTOMDatabaseDatabaseNotFound]: "База даних не існує в колекції або користувач не має прав адміністратора для доступу до неї.",
        [_.errorTOMDatabaseUpdateConflictMeasure]: "Запропоноване оновлення суперечить поточному стану цільового ресурсу.",
        [_.errorTOMDatabaseUpdateErrorMeasure]: "Запитуване оновлення не вдалося, оскільки один або декілька показників містять помилки.", 
        [_.errorTOMDatabaseUpdateFailed]: "Оновлення бази даних не відбулося при збереженні локальних змін, внесених до моделі, на сервері бази даних.",
        [_.errorTryingToUpdateMeasuresWithErrors]: `Запитуване оновлення не вдалося виконати, оскільки наступні заходи містять помилки:<br><strong>{показники}</strong>`,
        [_.errorUnhandled]: "Невиправлена помилка - будь ласка, повідомте про неї та надайте ідентифікатор трасування, якщо він є.",
        [_.errorUnspecified]: "Невизначена помилка.",
        [_.errorUserSettingsSaveError]: "Не вдається зберегти налаштування.",
        [_.errorVpaxFileExportError]: "Виникла помилка при експорті файлу VPAX.",
        [_.errorVpaxFileImportError]: "Виникла помилка при імпорті файлу VPAX.",
        [_.expandAllCtrlTitle]: "Розгорнути все",
        [_.ExportData]: "Експорт даних",
        [_.exportDataCSVCustomDelimiter]: "Користувацький роздільник поля",
        [_.exportDataCSVDelimiter]: "Розділювач полів",
        [_.exportDataCSVDelimiterComma]: "Кома",
        [_.exportDataCSVDelimiterDesc]: `Виберіть символ, який буде використовуватися в якості роздільника для кожного поля. <em>Automatic</em> використовує символ за замовчуванням вашої системи Culture.`,
        [_.exportDataCSVDelimiterOther]: "Інші...",
        [_.exportDataCSVDelimiterPlaceholder]: "Ієрогліф",
        [_.exportDataCSVDelimiterSemicolon]: "Крапка з комою",
        [_.exportDataCSVDelimiterSystem]: "Автоматичний",
        [_.exportDataCSVDelimiterTab]: "Табуляція",
        [_.exportDataCSVEncoding]: "Кодування",
        [_.exportDataCSVEncodingDesc]: "",
        [_.exportDataCSVFolder]: "Зберегти в підпапці",
        [_.exportDataCSVFolderDesc]: "Збережіть сформовані CSV-файли в підпапці.",
        [_.exportDataCSVQuote]: "Взяти рядки в лапки",
        [_.exportDataCSVQuoteDesc]: "Переконайтеся, що кожен рядок укладений у подвійні лапки.",
        [_.exportDataExcelCreateExportSummary]: "Експортувати звіт",
        [_.exportDataExcelCreateExportSummaryDesc]: "Додайте до експортного файлу додатковий аркуш з коротким змістом роботи.",
        [_.exportDataExport]: "Експорт обрано",
        [_.exportDataExportAs]: "Експортувати як",
        [_.exportDataExportAsDesc]: "",
        [_.exportDataExporting]: "Експортується {таблиця}...",
        [_.exportDataExportingDone]: "Готово!",
        [_.exportDataNoColumns]: "Ця таблиця не підлягає експорту, оскільки не містить жодного стовпчика.",
        [_.exportDataNotQueryable]: "Ця таблиця не може бути експортована, оскільки містить один або декілька розрахованих стовпців з невірним виразом або стовпців, які потребують перерахунку.",
        [_.exportDataOpenFile]: "Відкрити експортований файл",
        [_.exportDataOpenFolder]: "Відкрити папку експорту",
        [_.exportDataOptions]: "Параметри експорту",
        [_.exportDataStartExporting]: "Ініціалізація...",
        [_.exportDataSuccessSceneMessage]: "<strong>{count}/{total} таблиці{{s}}</strong> {{were|was}} успішно експортовані.",
        [_.exportDataSummary]: "Ваш набір даних містить <strong>{count} таблицю{{s}}</strong> яка може бути експортована.",
        [_.exportDataTruncated]: "Ця таблиця має занадто багато рядків. В Excel можна експортувати лише перший мільйон рядків. Використовуйте формат CSV для експорту всіх рядків таблиці.",
        [_.exportDataTypeCSV]: "CSV (Значення через кому)",
        [_.exportDataTypeXLSX]: "Електронна таблиця Excel",
        [_.failed]: "Помилка",
        [_.filterMeasuresWithErrorsCtrlTitle]: "Показувати тільки неформатовані показникик/показники з помилками",
        [_.filterUnrefCtrlTitle]: "Показувати тільки стовпці без посилань",
        [_.formattingMeasures]: "Форматування показників...",
        [_.goBackCtrlTitle]: "Скасувати і повернутися назад",
        [_.groupByTableCtrlTitle]: "Групування за таблицею",
        [_.helpConnectVideo]: "Як підключитися",
        [_.helpCtrlTitle]: "Допомога",
        [_.helpOptions]: "Варіанти",
        [_.helpUserInterface]: "Інтерфейс користувача",
        [_.helpTemplates]: "Дати шаблони",
        [_.hideUnsupportedCtrlTitle]: "Тільки підтримувані",
        [_.less]: "Менше",
        [_.license]: "Випущено за ліцензією MIT.",
        [_.loading]: "Завантаження...",
        [_.ManageDates]: "Керування датами",
        [_.manageDatesApplyCtrlTitle]: "Застосувати зміни",
        [_.manageDatesAuto]: "Авто",
        [_.manageDatesAutoScan]: "Автоматичне сканування",
        [_.manageDatesAutoScanActiveRelationships]: "Активні зв'язки",
        [_.manageDatesAutoScanDesc]: "Оберіть <em>Full</em> щоб просканувати всі стовпці, що містять дати. Встановіть <em>вибір стовпців...</em> для вибору стовпців, які будуть використовуватися. Встановіть <em>Активні зв'язки</em> та <em>Неактивні зв'язки</em> щоб просканувати стовпці тільки із зв'язками",
        [_.manageDatesAutoScanDisabled]: "Відключено",
        [_.manageDatesAutoScanFirstYear]: "Перший рік",
        [_.manageDatesAutoScanFirstYearDesc]: "",
        [_.manageDatesAutoScanFull]: "Повне",
        [_.manageDatesAutoScanInactiveRelationships]: "Неактивні зв'язки",
        [_.manageDatesAutoScanLastYear]: "Останній рік",
        [_.manageDatesAutoScanLastYearDesc]: "",
        [_.manageDatesAutoScanSelectedTablesColumns]: "Виберіть стовпці...",
        [_.manageDatesBrowserPlaceholder]: "Немає елементів для зміни",
        [_.manageDatesCalendarDesc]: "Виберіть шаблон календаря для застосування до цієї моделі. Bravo створить необхідні таблиці або оновить їх, зберігаючи існуючі зв'язки.", 
        [_.manageDatesCalendarTemplateName]: "Шаблон",
        [_.manageDatesCalendarTemplateNameDesc]: "Виберіть <em>Помісячно</em> для календаря з різною кількістю місяців. Встановіть <em>Щотижнево</em> для 445-454-544-ISO календарів. Використовуйте <em>Користувацький</em> для гнучких календарів змінної довжини.",
        [_.manageDatesCreatingTables]: "Оновлення моделі...",
        [_.manageDatesDatesDesc]: "Налаштуйте формат і розташування дат у вашій моделі.",
        [_.manageDatesDatesTableDesc]: "Це таблиця для використання у звітах за датами.",
        [_.manageDatesDatesTableName]: "Таблиця дат",
        [_.manageDatesDatesTableReferenceDesc]: "Це прихована таблиця, що містить всі функції DAX, які використовуються для генерації дат.",
        [_.manageDatesDatesTableReferenceName]: "Таблиця визначення дат",
        [_.manageDatesHolidaysDesc]: "Додайте свята до своєї моделі. Bravo створить необхідні таблиці або оновить їх, зберігаючи існуючі зв'язки.",
        [_.manageDatesHolidaysEnabledDesc]: "Додайте таблицю свят до своєї моделі.",
        [_.manageDatesHolidaysEnabledName]: "Свята",
        [_.manageDatesHolidaysTableDefinitionDesc]: "Це прихована таблиця, що містить всі функції DAX, які використовуються для генерації свят.",
        [_.manageDatesHolidaysTableDefinitionName]: "Таблиця визначення святкових днів",
        [_.manageDatesHolidaysTableDesc]: "Саме таку таблицю слід використовувати у звітах за святкові дні.",
        [_.manageDatesHolidaysTableName]: "Таблиця свят",
        [_.manageDatesIntervalDesc]: "Виберіть інтервал дат для вашої моделі.",
        [_.manageDatesISOCountryDesc]: "",
        [_.manageDatesISOCountryName]: "Святкові дні країни:",
        [_.manageDatesISOCustomFormatDesc]: "Введіть регіональний формат, використовуючи мовний тег IETF BCP 47. Наприклад en-US",
        [_.manageDatesISOCustomFormatName]: "Користувацький формат",
        [_.manageDatesISOFormatDesc]: "Оберіть регіональний формат дат.",
        [_.manageDatesISOFormatName]: "Регіональний формат",
        [_.manageDatesISOFormatOther]: "Інше...",
        [_.manageDatesISOFormatOtherPlaceholder]: "Регіон",
        [_.manageDatesManageTemplates]: "Керування шаблонами",
        [_.manageDatesMenuCalendar]: "Календар",
        [_.manageDatesMenuDates]: "Дати",
        [_.manageDatesMenuHolidays]: "Свята",
        [_.manageDatesMenuInterval]: "Інтервал",
        [_.manageDatesMenuPreviewCode]: "Вираження",
        [_.manageDatesMenuPreviewModel]: "Зміни в моделі",
        [_.manageDatesMenuPreviewTable]: "Вибіркові дані",
        [_.manageDatesMenuPreviewTreeDate]: "Дати",
        [_.manageDatesMenuPreviewTreeDateHolidays]: "Дати & Свята",
        [_.manageDatesMenuPreviewTreeTimeIntelligence]: "Операції з часом",
        [_.manageDatesMenuTimeIntelligence]: "Операції з часом",
        [_.manageDatesModelCheck]: "Перевірка моделі",
        [_.manageDatesPreview]: "Попередній перегляд",
        [_.manageDatesPreviewCtrlTitle]: "Попередній перегляд змін",
        [_.manageDatesSampleData]: "Вибіркові дані",
        [_.manageDatesSampleDataError]: "Неможливо згенерувати дані вибірки.",
        [_.manageDatesStatusCompatible]: `<div class="hero">Ця модель вже містить декілька <b>таблиць дат, сумісних</b> з Bravo.</div>Якщо ви щось тут зміните, то ці таблиці будуть оновлені, а їх зв'язки залишаться незмінними.`,
        [_.manageDatesStatusError]: `<div class="hero">The current settings cannot be applied.</div>{error}`,
        [_.manageDatesStatusIncompatible]: `<div class="hero">Ця модель містить деякі <b>таблиці дат</b>, які є несумісними </b> з Bravo.</div>Для внесення змін необхідно вибрати іншу назву для  <b>Дати</b> та/або <b>Свята</b> таблиці.`,
        [_.manageDatesStatusNotAvailable]: `<div class="hero">Ця модель більше не доступна.</div> Спробуйте перезапустити додаток.`,
        [_.manageDatesStatusOk]: `<div class="hero">Ця модель <b>умісна з функцією керування датами</b>.</div>Ви можете створювати нові таблиці дат, не турбуючись про порушення заходів або звітів.`,
        [_.manageDatesSuccessSceneMessage]: "Вітаємо, вашу модель успішно оновлено.",
        [_.manageDatesTemplateFirstDayOfWeek]: "Перший день тижня",
        [_.manageDatesTemplateFirstDayOfWeekDesc]: "Для тижневого ISO встановіть <em>Понеділок/em>.",
        [_.manageDatesTemplateFirstFiscalMonth]: "Перший місяць року",
        [_.manageDatesTemplateFirstFiscalMonthDesc]: "Для тижневого ISO встановіть <em>Січень</em>.",
        [_.manageDatesTemplateMonthsInYear]: "Місяці в році",
        [_.manageDatesTemplateMonthsInYearDesc]: "",
        [_.manageDatesTemplateNameConfig01]: "Стандарт",
        [_.manageDatesTemplateNameConfig02]: "Стандарт - Фіскальний",
        [_.manageDatesTemplateNameConfig03]: "Щомісячний",
        [_.manageDatesTemplateNameConfig04]: "Щомісячний - Фіскальний",
        [_.manageDatesTemplateNameConfig05]: "Користувацький",
        [_.manageDatesTemplateNameConfig06]: "Користувацький - Фіскальний",
        [_.manageDatesTemplateNameConfig07]: "Щотижневий",
        [_.manageDatesTemplateNameCurrent]: "Поточний",
        [_.manageDatesTemplateQuarterWeekType]: "Щотижнева система",
        [_.manageDatesTemplateQuarterWeekTypeDesc]: "",
        [_.manageDatesTemplateTypeStartFiscalYear]: "Дата визначення фінансового року",
        [_.manageDatesTemplateTypeStartFiscalYearDesc]: "Для тижневого ISO встановіть <em>Останній у цьому році</em>.",
        [_.manageDatesTemplateTypeStartFiscalYearFirst]: "Перший у році",
        [_.manageDatesTemplateTypeStartFiscalYearLast]: "Останній у році",
        [_.manageDatesTemplateWeeklyType]: "Останній будній день року",
        [_.manageDatesTemplateWeeklyTypeDesc]: "Якщо Ваш тиждень починається в неділю, то останнім робочим днем є субота. Якщо ви обираєте <em>Останній у році</em> фінансовий рік закінчується в останню суботу останнього місяця. В іншому випадку фінансовий рік закінчується в суботу, найближчу до останнього дня останнього місяця - це може бути і наступного року. Для Тижневого ISO встановіть значення <em>Найближче до кінця року</em>.",
        [_.manageDatesTemplateWeeklyTypeLast]: "Останній у цьому році",
        [_.manageDatesTemplateWeeklyTypeNearest]: "Ближче до кінця року",
        [_.manageDatesTimeIntelligenceDesc]: "Створіть найпоширеніші функції Time Intelligence DAX, доступні у вашій моделі.",
        [_.manageDatesTimeIntelligenceEnabledDesc]: "",
        [_.manageDatesTimeIntelligenceEnabledName]: "Функції операцій із часом",
        [_.manageDatesTimeIntelligenceTargetMeasuresAll]: "Усі показники",
        [_.manageDatesTimeIntelligenceTargetMeasuresChoose]: "Обрати показники...",
        [_.manageDatesTimeIntelligenceTargetMeasuresDesc]: "Виберіть показник, який використовується для генерації функцій Time Intelligence.",
        [_.manageDatesTimeIntelligenceTargetMeasuresName]: "Цільові показники",
        [_.manageDatesYearRange]: "Інтервал дат",
        [_.manageDatesYearRangeDesc]: "Виберіть спосіб визначення інтервалу дат моделі. Залишіть <em>Перший рік</em> та/або <em>Останній рік</em> порожнім для використання автоматичного сканування.",
        [_.menuCtrlTitle]: "Згорнути/розгорнути меню",
        [_.minimizeCtrlTitle]: "Мінімізувати",
        [_.modelLanguage]: "Мова моделі ({culture})",
        [_.more]: "Ще",
        [_.notificationCtrlTitle]: "Сповіщення",
        [_.notificationsTitle]: "{count} сповіщень{{s}}",
        [_.openSourcePayoff]: "{appName} це інструмент з відкритим вихідним кодом, розроблений і підтримуваний SQLBI та спільнотою Github. Приєднуйтесь до нас",
        [_.openWithDaxFormatterCtrlTitle]: "Форматувати онлайн за допомогою DAX Formatter",  
        [_.optionAccount]: "Power BI Акаунт",
        [_.optionAccountDescription]: "Налаштуйте обліковий запис для доступу до онлайн-наборів даних Power BI.",
        [_.optionBrowserAuthentication]: "Авторизація в браузері",
        [_.optionBrowserAuthenticationDescription]: "Авторизуйтесь за допомогою браузера за замовчуванням. Це альтернативний метод входу, який стане в нагоді для вирішення проблем з двофакторною автентифікацією.",
        [_.optionCheckForUpdates]: "Автоматична перевірка оновлень",
        [_.optionDev]: "Увімкнути шаблони дат користувачів",
        [_.optionDevDescription]: "",
        [_.optionDiagnostic]: "Рівень діагностики",
        [_.optionDiagnosticDescription]: "Відображення помилок і журналів на панелі діагностики. Оберіть <em>Базовий</em> щоб зареєструвати лише кілька повідомлень. <em>Докладний</em> реєструє всі повідомлення.",
        [_.optionDiagnosticLevelBasic]: "Базовий",
        [_.optionDiagnosticLevelNone]: "Ні",
        [_.optionDiagnosticLevelVerbose]: "Докладний",
        [_.optionDiagnosticMore]: "Щоб повідомити про проблему з додатком, будь ласка, використовуйте",
        [_.optionFormattingBreaks]: "Розбивка по імені-вираженню",
        [_.optionFormattingBreaksAuto]: "Авто",
        [_.optionFormattingBreaksDescription]: "Виберіть спосіб розділення назви показника та виразу. Встановіть <em>Авто</em>для автоматичного визначення стилю, використаного в моделі.",
        [_.optionFormattingBreaksInitial]: "Розрив рядка",
        [_.optionFormattingBreaksNone]: "Та ж лінія",
        [_.optionFormattingIncludeTimeIntelligence]: "Включити Time Intelligence",
        [_.optionFormattingIncludeTimeIntelligenceDescription]: "Включайте заходи, створені автоматично за допомогою Manage Dates для Time Intelligence.",
        [_.optionFormattingLines]: "Рядки",
        [_.optionFormattingLinesDescription]: "Виберіть короткі або довгі рядки.",
        [_.optionFormattingLinesValueLong]: "Довгі рядки",
        [_.optionFormattingLinesValueShort]: "Короткі рядки",
        [_.optionFormattingPreview]: "Автоматичний перегляд",
        [_.optionFormattingPreviewDescription]: "Автоматична відправка вимірювань в DAX Formatter для створення попереднього перегляду.",
        [_.optionFormattingSeparators]: "Роздільник",
        [_.optionFormattingSeparatorsDescription]: "Виберіть роздільники для чисел і списків.",
        [_.optionFormattingSeparatorsValueAuto]: "Авто",
        [_.optionFormattingSeparatorsValueEU]: "A; B; C; 1234,00",
        [_.optionFormattingSeparatorsValueUS]: "A, B, C, 1234.00",
        [_.optionFormattingSpaces]: "Інтервал",
        [_.optionFormattingSpacesDescription]: "Керування пробілами після назв функцій.",
        [_.optionFormattingSpacesValueBestPractice]: "Найкращі практики",
        [_.optionFormattingSpacesValueFalse]: "Без пробілів - ЯКЩО( ",
        [_.optionFormattingSpacesValueTrue]: "Пробіл - ЯКЩО ( ",
        [_.optionInvalidValue]: "Невірне значення",
        [_.optionLanguage]: "Мова",
        [_.optionLanguageDescription]: "Обери мову Bravo. Потрібне перезавантаження.",
        [_.optionLanguageResetConfirm]: "Ви хочете перезавантажити Bravo, щоб застосувати нову мову?",
        [_.optionPolicyNotice]: "Ця опція управляється вашою організацією.",
        [_.optionProxyAddress]: "Адреса проксі-сервера",
        [_.optionProxyAddressDescription]: "Вкажіть адресу вашого проксі-сервера.",
        [_.optionProxyBypassList]: "Виключити список адрес",
        [_.optionProxyBypassListDescription]: "Використовуйте проксі-сервер за винятком адрес, які починаються зі вставлених записів. Використовуйте крапку з комою (;) для розділення записів",
        [_.optionProxyBypassOnLocal]: "Обхід локальних адрес",
        [_.optionProxyBypassOnLocalDescription]: "Не використовуйте проксі з локальними (інтранет) адресами.",
        [_.optionProxyConfirmDeleteCredentials]: "Ви впевнені, що видалили користувацькі облікові дані з системи?",
        [_.optionProxyCustomCredentials]: "Користувацькі облікові дані",
        [_.optionProxyCustomCredentialsDescription]: "Використовуйте власні облікові дані для авторизації на проксі-сервері. Вимкнути для використання системних облікових даних.",
        [_.optionProxyCustomCredentialsEdit]: "Редагування користувацьких облікових даних",
        [_.optionProxyType]: "Проксі-сервер",
        [_.optionProxyTypeCustom]: "Користувацький",
        [_.optionProxyTypeDescription]: "Виберіть проксі-сервер.",
        [_.optionProxyTypeNone]: "Ніхто",
        [_.optionProxyTypeSystem]: "Система",
        [_.optionResetAlerts]: "Відновити оповіщення",
        [_.optionResetAlertsButton]: "Відновити",
        [_.optionResetAlertsDescription]: "Відновити всі приховані вручну оповіщення.",
        [_.optionsDialogAboutMenu]: "Про програму",
        [_.optionsDialogDevMenu]: "Шаблони",
        [_.optionsDialogFormattingMenu]: "Форматування",
        [_.optionsDialogGeneralMenu]: "Загальний",
        [_.optionsDialogProxyMenu]: "Проксі",
        [_.optionsDialogTelemetryMenu]: "Діагностика",
        [_.optionsDialogTitle]: "Параметри",
        [_.optionTelemetry]: "Телеметрія",
        [_.optionTelemetryDescription]: "Надсилайте анонімні дані про використання до SQLBI.",
        [_.optionTelemetryMore]: "Допоможіть нам зрозуміти, як ви використовуєте Bravo та як його покращити. Особиста інформація не збирається. Зверніть увагу, що якщо ця опція вимкнена, команда розробників не зможе збирати інформацію про необроблені помилки та надавати додаткову інформацію або підтримку.",
        [_.optionTheme]: "Тема",
        [_.optionThemeDescription]: "Задати тему для Bravo. Залиште <em>Систему</em> на вибір операційної системи.",
        [_.optionThemeValueAuto]: "Система",
        [_.optionThemeValueDark]: "Темна",
        [_.optionThemeValueLight]: "Світла",
        [_.otherColumnsRowName]: "Менші колонки...",
        [_.paste]: "Вставити",
        [_.powerBiObserving]: "Очікування відкриття файлу в Power BI Desktop...",
        [_.powerBiObservingCancel]: "Відміна",
        [_.powerBiSigninDescription]: "Увійдіть до служби Power BI, щоб підключити Bravo до ваших онлайн-наборів даних.",
        [_.powerBiSigninTitle]: "Power BI",
        [_.quickActionAttachPBITitle]: "Приєднання до робочого столу Power BI",
        [_.quickActionConnectPBITitle]: "Підключення до служби Power BI",
        [_.quickActionOpenVPXTitle]: "Відкрийте файл Vertipaq Analyzer",
        [_.refreshCtrlTitle]: "Оновити",
        [_.refreshPreviewCtrlTitle]: "Оновити попередній перегляд",
        [_.saveVpaxCtrlTile]: "Зберегти як VPAX",
        [_.savingVpax]: "Генерування VPAX...",
        [_.sceneUnsupportedReason]: "Ця функція недоступна для цього джерела даних.",
        [_.sceneUnsupportedReasonManageDatesAutoDateTimeEnabled]: `Моделі з увімкненою опцією автоматичного встановлення дати/часу не підтримуються.<br><span class="link" href="https://www.sqlbi.com/tv/disabling-auto-date-time-in-power-bi/">Відключення автоматичного визначення дати і часу в Power BI (video)</span>`,
        [_.sceneUnsupportedReasonManageDatesEmptyTableCollection]: "Ця функція підтримується тільки базами даних, які мають хоча б одну таблицю.",
        [_.sceneUnsupportedReasonManageDatesPBIDesktopModelOnly]: "Ця функція підтримується тільки моделями в режимі Power BI Desktop.",
        [_.sceneUnsupportedReasonMetadataOnly]: "База даних була створена з VPAX-файлу, який містить лише метадані.",
        [_.sceneUnsupportedReasonReadOnly]: "Підключення до цієї бази даних є тільки для читання.",
        [_.sceneUnsupportedReasonXmlaEndpointNotSupported]: "Кінцева точка XMLA не підтримується для цього набору даних.",
        [_.sceneUnsupportedTitle]: "Не підтримується",
        [_.searchColumnPlaceholder]: "Колонка пошуку",
        [_.searchDatasetPlaceholder]: "Набір даних для пошуку",
        [_.searchEntityPlaceholder]: "Таблиця/стовпець пошуку",
        [_.searchMeasurePlaceholder]: "Пошуковий показник",
        [_.searchPlaceholder]: "Пошук",
        [_.searchTablePlaceholder]: "Таблиця пошуку",
        [_.settingsCtrlTitle]: "Параметри",
        [_.sheetOrphan]: "Недоступно",
        [_.sheetOrphanPBIXTooltip]: "Звіт було закрито в Power BI Desktop. Будь-які операції запису заборонені.",
        [_.sheetOrphanTooltip]: "Ця модель більше не випускається. Будь-яка операція запису заборонена.",
        [_.showDiagnosticPane]: "Показати деталі",
        [_.sideCtrlTitle]: "Увімкнути паралельний перегляд",
        [_.signedInCtrlTitle]: "Авторизовано як {ім'я'}",
        [_.signIn]: "Увійти",
        [_.signInCtrlTitle]: "Увійти",
        [_.signOut]: "Вийти з системи",
        [_.sqlbiPayoff]: "Bravo - проект компанії SQLBI.",
        [_.syncCtrlTitle]: "Синхронізувати",
        [_.tableColCardinality]: "Кардинальність",
        [_.tableColCardinalityTooltip]: "Кількість унікальних елементів",
        [_.tableColColumn]: "Колонка",
        [_.tableColColumns]: "Стовпчики",
        [_.tableColMeasure]: "Показник",
        [_.tableColPath]: "Таблиця \\ Колонка",
        [_.tableColRows]: "Рядки",
        [_.tableColSize]: "Розмір",
        [_.tableColTable]: "Таблиця",
        [_.tableColWeight]: "Розмір",
        [_.tableSelectedCount]: "{count} Обрано",
        [_.tableValidationInvalid]: "Це ім'я не може бути використано",
        [_.tableValidationValid]: "Це ім'я є дійсним",
        [_.themeCtrlTitle]: "Змінити тему",
        [_.toggleTree]: "Переключити древо",
        [_.traceId]: "Ідентифікатор відстеження",
        [_.unknownMessage]: "Отримано невірне повідомлення",
        [_.updateChannelBeta]: "Бета",
        [_.updateChannelCanary]: "Canary",
        [_.updateChannelDev]: "Dev",
        [_.updateChannelStable]: "Стабільний", 
        [_.updateMessage]: "Доступна нова версія Bravo: {version}",
        [_.validating]: "Перевірка...",
        [_.version]: "Версія",
        [_.welcomeHelpText]: "Перегляньте відео нижче, щоб дізнатися, як користуватися Bravo:",
        [_.welcomeHelpTitle]: "Як користуватися Bravo?",
        [_.welcomeText]: "Bravo - це зручний інструментарій Power BI, який можна використовувати для аналізу моделей, форматування показників, створення таблиць дат та експорту даних.",
        [_.whitespacesTitle]: "Пробіли",
        [_.wrappingTitle]: "Автоматичне перенесення слів",
    }
}
export default locale;