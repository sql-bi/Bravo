/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { Locale } from '../i18n';
import { strings as _ } from '../strings';

const locale: Locale = {

    locale: "ru", //DO NOT TRANSLATE
    name: "Русский", //DO NOT TRANSLATE

    strings: {
        [_.addCtrlTitle]: "Открыть",
        [_.aggregatedTableName]: "Multiple tables",
        [_.AnalyzeModel]: "Анализ модели",
        [_.analyzeModelSummary]: `Размер вашего источника данных <strong>{size:bytes}</strong> байт, включая <strong>{count}</strong> стобцов`,
        [_.analyzeModelSummary2]: `, <span class="text-highlight"><strong>{count}</strong> of which {{are|is}} not referenced within the model.</span>`,//Don't understand
        [_.appUpdateAvailable]: "Доступна новая версия: {version}",
        [_.appUpdateChangelog]: "Журнал изменений",
        [_.appUpdateDownload]: "Загрузить",
        [_.appUpdateViewDetails]: "Показать детали",
        [_.appUpToDate]: "Bravo обновляется",
        [_.appVersion]: "Версия {version}",
        [_.backupReminder]: "Перед началом работы, не забудьте сделать резервную копию своего отчета - <b>некоторые изменения невозможно отменить</b>.",
        [_.BestPractices]: "Лучшие практики",
        [_.canceled]: "Отменено",
        [_.changeStatusAdded]: "Д",
        [_.changeStatusAddedTitle]: "Добавлена",
        [_.changeStatusDeleted]: "У",
        [_.changeStatusDeletedTitle]: "Удалена",
        [_.changeStatusModified]: "М",
        [_.changeStatusModifiedTitle]: "Модифицированна",
        [_.clearCtrlTitle]: "Очистить",
        [_.closeCtrlTitle]: "Закрыть",
        [_.closeOtherTabs]: "Закрыть другие",
        [_.closeTab]: "Закрыть",
        [_.collapseAllCtrlTitle]: "Свернуть всё",
        [_.columnExportedCompleted]: "Данная таблица была успешно экспортирована.",
        [_.columnExportedFailed]: "Данная таблица не была экспортирована из-за неизвестной ошибки.",
        [_.columnExportedTruncated]: "Данная таблица была усечена, поскольку количество строк превысило максимально допустимое.",
        [_.columnMeasureFormatted]: "Данная мера уже отформатирована.",
        [_.columnMeasureNotFormattedTooltip]: "Данная мера не отформатирована",
        [_.columnMeasureWithError]: "Данная мера содержит ошибки.",
        [_.columnUnreferencedExplanation]: `<span class="text-highlight">Столбцы без ссылок</span> как правило, можно удалить из модели для оптимизации производительности. Прежде чем удалять их, убедитесь, что вы не используете эти столбцы в каких-либо отчетах, что Bravo не может определить.`,
        [_.columnUnreferencedTooltip]: "В данной модели нет ссылок на этот столбец.",
        [_.confirmTabCloseMessage]: "Кажется, вы не сохранили изменения в этом документе.<br>Вы действительно хотите его закрыть?",
        [_.connectBrowse]: "Обзор",
        [_.connectDatasetsTableEndorsementCol]: "Подтвердить",
        [_.connectDatasetsTableNameCol]: "Имя",
        [_.connectDatasetsTableOwnerCol]: "Владелец",
        [_.connectDatasetsTableWorkspaceCol]: "Workspace",
        [_.connectDialogAttachPBIMenu]: "Источники данных на powerbi.com",
        [_.connectDialogConnectPBIMenu]: "Активные отчеты в Power BI Desktop",
        [_.connectDialogOpenVPXMenu]: "VPAX файлы",
        [_.connectDialogTitle]: "Открыть",
        [_.connectDragFile]: "Переместите VPAX файл сюда<br>или укажите расположение файла нажав кнопку Обзор",
        [_.connectNoReports]: "Активные отчеты не найдены.<br>Откройте отчет с помощью Power BI Desktop и подождите, пока он появится здесь.",
        [_.copiedErrorDetails]: "Скопировано!",
        [_.copy]: "Копировать",
        [_.copyErrorDetails]: "Копировать детали ошибки",
        [_.copyFormulaCtrlTitle]: "Копировать отформатированную меру",
        [_.copyMessage]: "Копировать сообщение",
        [_.copyright]: "Все права защищены.",
        [_.createIssue]: "Сообщить о проблеме", 
        [_.cut]: "Вырезать",
        [_.dataUsageLink]: "Как используются ваши данные?", 
        [_.dataUsageMessage]: `Чтобы отформатировать ваш код, Bravo отправляет показатели этого набора данных в DAX Formatter, службу, управляемую SQLBI, через безопасное соединение.<p><strong>Служба нигде не хранит ваши данные.< /strong></p><p>Некоторая информация, такая как наиболее часто используемые функции DAX, индекс сложности и средняя длина запроса, может быть сохранена для статистических целей.</p>`,
        [_.dataUsageTitle]: "Как используются ваши данные?",
        [_.DaxFormatter]: "Форматировать DAX",
        [_.daxFormatterAgreement]: "Для форматирования DAX Bravo отправляет ваши измерения в службу форматирования DAX.",
        [_.daxFormatterAnalyzeConfirm]: "Чтобы выполнить анализ, Bravo должен отправить все измерения в службу DAX Formatter. Вы уверены, что продолжаете?",
        [_.daxFormatterAutoPreviewOption]: "Автоматический предварительный просмотр",
        [_.daxFormatterFormat]: "Формат выбран",
        [_.daxFormatterFormatDisabled]: "Формат (не поддерживается)",
        [_.daxFormatterFormattedCode]: "Отформатировано (предварительная версия)",
        [_.daxFormatterOriginalCode]: "Текущий",
        [_.daxFormatterPreviewAllButton]: "Предварительный просмотр всех мер",
        [_.daxFormatterPreviewButton]: "Предварительный просмотр",
        [_.daxFormatterPreviewDesc]: "Чтобы создать предварительный просмотр, Bravo необходимо отправить эту меру в службу DAX Formatter.",
        [_.daxFormatterSuccessSceneMessage]: "Поздравляем, <strong>{count} мер</strong> отформатированно успешно.",
        [_.daxFormatterSummary]: `Ваш набор данных содержит {count} {{меру|меры}}: <span class="text-error"><strong>{errors:number}</strong> с ошибками</strong></span>, <span class="text-highlight"><strong>{formattable:number}</strong> для формотирования</span>, <strong>{analyzable:number}</strong> для анализа (<span class="link manual-analyze">проанализируй сейчас</span>).`,
        [_.daxFormatterSummaryNoAnalysis]: `Ваш набор данных содержит <strong>{count}</strong> {{меру|меры}}: <span class="text-error"><strong>{errors:number}</strong> с ошибками</strong></span> and <span class="text-highlight"><strong>{formattable:number}</strong> для форматирования.</span>`,
        [_.defaultTabName]: "Без имени",
        [_.dialogCancel]: "Отмена",
        [_.dialogOK]: "OK",
        [_.dialogOpen]: "Открыть",
        [_.docLimited]: "Ограничено",
        [_.docLimitedTooltip]: "Для этого документа доступны не все функции.",
        [_.doneCtrlTitle]: "Готово",
        [_.emailAddress]: "Адрес электронной почты",
        [_.emailAddressPlaceholder]: "Введите свой адрес электронной почты",
        [_.error]: "Ошибка",
        [_.errorAborted]: "Операция отменена.",
        [_.errorAnalysisServicesConnectionFailed]: "Проблема соединения между сервером и Bravo.",
        [_.errorCheckForUpdates]: "Невозможно проверить наличие обновлений - удаленный сервер недоступен.",
        [_.errorConnectionUnsupported]: "Подключение к запрошенному ресурсу не поддерживается.",
        [_.errorDatasetConnectionUnknown]: "Не указано соединение.",
        [_.errorDatasetsEmptyListing]: "Нет доступных открытых отчетов.",
        [_.errorDatasetsListing]: "Не удалось получить список наборов данных службы Power BI.",
        [_.errorExportDataFileError]: "При экспорте данных произошла ошибка. Повторите попытку.",
        [_.errorManageDateTemplateError]: "Произошло ошибка при выполнении механизма шаблонов DAX.",
        [_.errorNetworkError]: "Вы не подключены к сети Интернет.",
        [_.errorNone]: "Неизвестная ошибка.",
        [_.errorNotAuthorized]: "У вас нет прав для просмотра указанного ресурса.",
        [_.errorNotConnected]: "Вы не подключены к Power BI — пожалуйста, войдите в систему, чтобы продолжить.",
        [_.errorNotFound]: "Невозможно подключиться к указанному ресурсу.",
        [_.errorReportConnectionUnknown]: "Недопустимое соединение.",
        [_.errorReportConnectionUnsupportedAnalysisServecesCompatibilityMode]: "Режим совместимости экземпляра Power BI Desktop Analysis Services не является PowerBI.",
        [_.errorReportConnectionUnsupportedAnalysisServecesConnectionNotFound]: "Режим совместимости экземпляра Power BI Desktop Analysis Services не является PowerBI.",
        [_.errorReportConnectionUnsupportedAnalysisServecesProcessNotFound]: "Процесс службы Power BI Desktop Analysis Services не найден.", 
        [_.errorReportConnectionUnsupportedConnectionException]: "При подключении к экземпляру Power BI Desktop Analysis Services возникла ошибка.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionIsEmpty]: "Power BI Desktop Analysis Services не содержит баз данных. Попробуйте подключиться к отчету с помощью значка Bravo во внешних инструментах Power BI Desktop.",
        [_.errorReportConnectionUnsupportedDatabaseCollectionUnexpectedCount]: "Power BI Desktop Analysis Services содержит неожиданное количество баз данных (> 1), в то время как мы ожидаем ноль или одну.",
        [_.errorReportConnectionUnsupportedProcessNotYetReady]: "Процесс Power BI Desktop открывается или Analysis Services еще не готов.", 
        [_.errorReportsEmptyListing]: "No unopened reports available.",
        [_.errorRetry]: "Повторить",
        [_.errorSignInMsalExceptionOccurred]: "Непредвиденная ошибка в запросе на вход.",
        [_.errorSignInMsalTimeoutExpired]: "Запрос на вход был отменен, так как время ожидания истекло до завершения операции",
        [_.errorTimeout]: "Истекло время ожидания запроса",
        [_.errorTitle]: "Ой...",
        [_.errorTOMDatabaseDatabaseNotFound]: "База данных не существует в коллекции или у пользователя нет прав администратора для доступа к ней.",
        [_.errorTOMDatabaseUpdateConflictMeasure]: "Запрошенное обновление конфликтует с текущим состоянием целевого ресурса.",
        [_.errorTOMDatabaseUpdateErrorMeasure]: "Запрошенное обновление не выполнено, поскольку одна или несколько мер содержат ошибки.", 
        [_.errorTOMDatabaseUpdateFailed]: "Не удалось обновить базу данных при сохранении локальных изменений, внесенных в модель на сервере базы данных.",
        [_.errorTryingToUpdateMeasuresWithErrors]: `Запрошенное обновление не выполнено, поскольку следующие меры содержат ошибки:<br><strong>{measures}</strong>`,
        [_.errorUnhandled]: "Неизвестная ошибка. Сообщите о ней и укажите идентификатор трассировки, если он доступен.",
        [_.errorUnspecified]: "Неизвестная ошибка.",
        [_.errorUserSettingsSaveError]: "Не удалось сохранить настройки.",
        [_.errorVpaxFileContainsCorruptedData]: "Формат файла VPAX недействителен или содержит поврежденные данные.",
        [_.expandAllCtrlTitle]: "Развернуть все",
        [_.ExportData]: "Экспорт данных",
        [_.exportDataCSVCustomDelimiter]: "Пользовательский разделитель полей",
        [_.exportDataCSVDelimiter]: "Разделитель полей",
        [_.exportDataCSVDelimiterComma]: "Запятая",
        [_.exportDataCSVDelimiterDesc]: `Выберите символ, который будет использоваться в качестве разделителя каждого поля. <em>Automatic</em> использует символ по умолчанию вашего региона.`,
        [_.exportDataCSVDelimiterOther]: "Другое...",
        [_.exportDataCSVDelimiterPlaceholder]: "Символы",
        [_.exportDataCSVDelimiterSemicolon]: "Точка с запятой",
        [_.exportDataCSVDelimiterSystem]: "Автоматически",
        [_.exportDataCSVDelimiterTab]: "Табуляция",
        [_.exportDataCSVEncoding]: "Кодировка",
        [_.exportDataCSVEncodingDesc]: "",
        [_.exportDataCSVQuote]: "Заключить строки в кавычки",
        [_.exportDataCSVQuoteDesc]: "Убедитесь, что каждая строка заключена в двойные кавычки.",
        [_.exportDataExcelCreateExportSummary]: "Экспорт сведенных данных",
        [_.exportDataExcelCreateExportSummaryDesc]: "Добавить дополнительный лист в файл экспорта со сведенными данными.",
        [_.exportDataExport]: "Экспортировать выбранное",
        [_.exportDataExportAs]: "Экспортировать как",
        [_.exportDataExportAsDesc]: "",
        [_.exportDataExporting]: "Экспортировать {table}...",
        [_.exportDataExportingDone]: "Готово!",
        [_.exportDataNoColumns]: "Эту таблицу нельзя экспортировать, поскольку она не содержит столбцов.",
        [_.exportDataOpenFile]: "Открыть экспортированный файл",
        [_.exportDataOpenFolder]: "Открыть папку",
        [_.exportDataOptions]: "Параметры экспорта",
        [_.exportDataStartExporting]: "Инициализация...",
        [_.exportDataSuccessSceneMessage]: "<strong>{count}/{total} таблицы</strong> успешно экспортированы.",
        [_.exportDataSummary]: "Ваш набор данных содержит <strong>{count} таблицы</strong> которые можно экспортировать.",
        [_.exportDataTypeCSV]: "CSV (Comma-separated values)",
        [_.exportDataTypeXLSX]: "Excel таблица",
        [_.failed]: "Ошибка",
        [_.filterMeasuresWithErrorsCtrlTitle]: "Показать только неформатированные меры/меры с ошибками",
        [_.filterUnrefCtrlTitle]: "Показать только столбцы без ссылок",
        [_.formattingMeasures]: "Форматирование мер...",
        [_.goBackCtrlTitle]: "Отменить и вернуться назад",
        [_.groupByTableCtrlTitle]: "Группировать по таблице",
        [_.helpConnectVideo]: "Как подключить",
        [_.helpCtrlTitle]: "Помощь",
        [_.hideUnsupportedCtrlTitle]: "Только поддерживаемые",
        [_.less]: "Меньше",
        [_.license]: "Выпущено по лицензии MIT.",
        [_.loading]: "Загрузка...",
        [_.ManageDates]: "Управление датами",
        [_.manageDatesApplyCtrlTitle]: "Применить изменения",
        [_.manageDatesAuto]: "Авто",
        [_.manageDatesAutoScan]: "Автоматическое сканирование",
        [_.manageDatesAutoScanActiveRelationships]: "Активные связи",
        [_.manageDatesAutoScanDesc]: "Выберите <em>Полный</em>, чтобы просмотреть все столбцы, содержащие даты. Установите <em>Выбрать столбцы...</em>, чтобы выбрать используемые столбцы. Установите <em>Активные связи </em> и <em>Неактивные связи</em>, чтобы сканировать только столбцы со связями.",
        [_.manageDatesAutoScanDisabled]: "Отключено",
        [_.manageDatesAutoScanFirstYear]: "Первый год",
        [_.manageDatesAutoScanFirstYearDesc]: "",
        [_.manageDatesAutoScanFull]: "Полное",
        [_.manageDatesAutoScanInactiveRelationships]: "Неактивные связи",
        [_.manageDatesAutoScanLastYear]: "Последний год",
        [_.manageDatesAutoScanLastYearDesc]: "",
        [_.manageDatesAutoScanSelectedTablesColumns]: "Выбранные столбцы...",
        [_.manageDatesBrowserPlaceholder]: "Нет элементов для изменения",
        [_.manageDatesCalendarDesc]: "Выберите шаблон календаря для применения к этой модели. Bravo создаст необходимые таблицы или обновит их, сохранив при этом существующие связи.", 
        [_.manageDatesCalendarTemplateName]: "Шаблон календаря",
        [_.manageDatesCalendarTemplateNameDesc]: "Выберите <em>Ежемесячно</em> для календаря с разным количеством месяцев. Установите <em>Еженедельно</em> для календарей 445-454-544-ISO. Используйте <em>Пользовательский< /em> для гибких календарей переменной длины.",
        [_.manageDatesCreatingTables]: "Обновление модели...",
        [_.manageDatesDatesDesc]: "Настройте формат и расположение дат в вашей модели.",
        [_.manageDatesDatesTableDesc]: "Это таблица для использования в отчетах по датам.",
        [_.manageDatesDatesTableName]: "Таблица дат",
        [_.manageDatesDatesTableReferenceDesc]: "Это скрытая таблица, содержащая все функции DAX, используемые для генерации дат.",
        [_.manageDatesDatesTableReferenceName]: "Таблица определения дат",
        [_.manageDatesHolidaysDesc]: "Добавьте праздники в вашу модель. Bravo создаст необходимые таблицы или обновит их, сохранив при этом существующие отношения. ",
        [_.manageDatesHolidaysEnabledDesc]: "Добавьте таблицу праздников в вашу модель.",
        [_.manageDatesHolidaysEnabledName]: "Праздники",
        [_.manageDatesHolidaysTableDefinitionDesc]: "Это скрытая таблица, содержащая все функции DAX, используемые для создания праздников.",
        [_.manageDatesHolidaysTableDefinitionName]: "Таблица определения праздников",
        [_.manageDatesHolidaysTableDesc]: "Это таблица для использования в отчетах по праздникам.",
        [_.manageDatesHolidaysTableName]: "Таблица праздников",
        [_.manageDatesIntervalDesc]: "Выберите интервал дат для вашей модели.",
        [_.manageDatesISOCountryDesc]: "",
        [_.manageDatesISOCountryName]: "Страна праздников",
        [_.manageDatesISOCustomFormatDesc]: "Введите региональный формат, используя языковой тег IETF BCP 47. Например, en-US",
        [_.manageDatesISOCustomFormatName]: "Пользовательский формат",
        [_.manageDatesISOFormatDesc]: "Выберите региональный формат для дат.",
        [_.manageDatesISOFormatName]: "Региональный формат",
        [_.manageDatesISOFormatOther]: "Другое...",
        [_.manageDatesISOFormatOtherPlaceholder]: "Регион",
        [_.manageDatesMenuCalendar]: "Календарь",
        [_.manageDatesMenuDates]: "Даты",
        [_.manageDatesMenuHolidays]: "Праздники",
        [_.manageDatesMenuInterval]: "Интервал",
        [_.manageDatesMenuPreviewCode]: "Выражение",
        [_.manageDatesMenuPreviewModel]: "Изменения модели",
        [_.manageDatesMenuPreviewTable]: "Образец данных",
        [_.manageDatesMenuPreviewTreeDate]: "Даты",
        [_.manageDatesMenuPreviewTreeDateHolidays]: "Даты и праздники",
        [_.manageDatesMenuPreviewTreeTimeIntelligence]: "Операции со временем",
        [_.manageDatesMenuTimeIntelligence]: "Операции со временем",
        [_.manageDatesModelCheck]: "Проверка модели",
        [_.manageDatesPreview]: "Предпросмотр",
        [_.manageDatesPreviewCtrlTitle]: "Предпросмотр изменений",
        [_.manageDatesSampleData]: "Образец данных",
        [_.manageDatesSampleDataError]: "Невозможно сгенерировать образцы данных.",
        [_.manageDatesStatusCompatible]: `<div class="hero">Эта модель уже содержит несколько <b>таблиц дат, совместимых</b> с Bravo.</div>Если вы что-то здесь измените, эти таблицы будут обновлены, а их отношения останутся неизменными.`,
        [_.manageDatesStatusError]: `<div class="hero">Текущие настройки не могут быть применены.</div> Ошибка {error}`,
        [_.manageDatesStatusIncompatible]: `<div class="hero">Эта модель содержит несколько <b>таблиц дат, несовместимых</b> с Bravo.</div>Чтобы внести какие-либо изменения здесь, вам нужно выбрать другое имя для одной или нескольких таблиц, которые будут созданы этим инструментом.<br><br>Проверьте <b>Даты</b> и <b>Праздники</b>.`,
        [_.manageDatesStatusNotAvailable]: `<div class="hero">Эта модель больше недоступна.</div> Попробуйте перезапустить приложение.`,
        [_.manageDatesStatusOk]: `<div class="hero">Эта модель <b>совместима с функцией управления датами</b>.</div>Вы можете создавать новые таблицы дат, не беспокоясь о нарушении измерений или отчетов.`,
        [_.manageDatesSuccessSceneMessage]: "Поздравляем, ваша модель успешно обновлена.",
        [_.manageDatesTemplateFirstDayOfWeek]: "Первый день недели",
        [_.manageDatesTemplateFirstDayOfWeekDesc]: "Для еженедельного ISO установите <em>понедельник</em>.",
        [_.manageDatesTemplateFirstFiscalMonth]: "Первый месяц года",
        [_.manageDatesTemplateFirstFiscalMonthDesc]: "Для еженедельного ISO установите <em>Январь</em>.",
        [_.manageDatesTemplateMonthsInYear]: "Месяцы в году",
        [_.manageDatesTemplateMonthsInYearDesc]: "",
        [_.manageDatesTemplateNameConfig01]: "Стандарт",
        [_.manageDatesTemplateNameConfig02]: "Ежемесячный",
        [_.manageDatesTemplateNameConfig03]: "Пользовательский",
        [_.manageDatesTemplateNameConfig04]: "Еженедельный",
        [_.manageDatesTemplateQuarterWeekType]: "Еженедельная система", //???
        [_.manageDatesTemplateQuarterWeekTypeDesc]: "",
        [_.manageDatesTemplateTypeStartFiscalYear]: "Первый день финансового года",
        [_.manageDatesTemplateTypeStartFiscalYearDesc]: "Для Еженедельного ISO установите <em>Последний год</em>.",
        [_.manageDatesTemplateTypeStartFiscalYearFirst]: "Первый год",
        [_.manageDatesTemplateTypeStartFiscalYearLast]: "Последний год",
        [_.manageDatesTemplateWeeklyType]: "Последний день недели года",
        [_.manageDatesTemplateWeeklyTypeDesc]: "Если ваша неделя начинается в воскресенье, то последним днем недели будет суббота. Если вы выберете <em>Последний год</em>, финансовый год закончится в последнюю субботу последнего месяца. В противном случае, финансовый год заканчивается в субботу, ближайшую к последнему дню последнего месяца — это может быть в следующем году. Для Еженедельного ISO установите <em>Ближайший к концу года</em>.",
        [_.manageDatesTemplateWeeklyTypeLast]: "Последний год",
        [_.manageDatesTemplateWeeklyTypeNearest]: "Ближайший к концу года",
        [_.manageDatesTimeIntelligenceDesc]: "Создайте наиболее распространенные функции DAX Time Intelligence, доступные в вашей модели.",
        [_.manageDatesTimeIntelligenceEnabledDesc]: "",
        [_.manageDatesTimeIntelligenceEnabledName]: "Функции операций со временем",
        [_.manageDatesTimeIntelligenceTargetMeasuresAll]: "Все меры",
        [_.manageDatesTimeIntelligenceTargetMeasuresChoose]: "Выбранные меры...",
        [_.manageDatesTimeIntelligenceTargetMeasuresDesc]: "Выберите меру, используемую для создания функций анализа операций со временем.",
        [_.manageDatesTimeIntelligenceTargetMeasuresName]: "Целевая мера",
        [_.manageDatesYearRange]: "Интервал дат",
        [_.manageDatesYearRangeDesc]: "Выберите способ определения интервала дат в модели. Оставьте поля <em>Первый год</em> и/или <em>Последний год</em> пустыми, чтобы использовать автоматическое сканирование.",
        [_.menuCtrlTitle]: "Свернуть/развернуть меню",
        [_.minimizeCtrlTitle]: "Свернуть",
        [_.modelLanguage]: "Язык модели ({culture})",
        [_.more]: "Еще",
        [_.notificationCtrlTitle]: "Уведомления",
        [_.notificationsTitle]: "{count} уведомлений",
        [_.openSourcePayoff]: "{appName} это инструмент с открытым исходным кодом, разработанный и поддерживаемый SQLBI и сообществом Github. Присоединяйтесь к нам",
        [_.openWithDaxFormatterCtrlTitle]: "Форматировать онлайн с помощью DAX Formatter",  
        [_.optionAccount]: "Power BI аккаунт",
        [_.optionAccountDescription]: "Настройте учетную запись для доступа к онлайн-наборам данных Power BI.",
        [_.optionDiagnostic]: "Уровень диагностики",
        [_.optionDiagnosticDescription]: "Показывать ошибки и журналы на панели диагностики. Выберите <em>Основной</em>, чтобы регистрировать только несколько сообщений (которые будут анонимными). <em>Подробный</em> регистрирует все сообщения ( который останется целым)",
        [_.optionDiagnosticLevelBasic]: "Основной",
        [_.optionDiagnosticLevelNone]: "Нет",
        [_.optionDiagnosticLevelVerbose]: "Подробный",
        [_.optionDiagnosticMore]: "Чтобы сообщить о проблеме с приложением, используйте",
        [_.optionFormattingBreaks]: "Разрыв имени-выражения",
        [_.optionFormattingBreaksAuto]: "Авто",
        [_.optionFormattingBreaksDescription]: "Выберите способ разделения имени меры и выражения. Установите <em>Auto</em> для автоматического определения стиля, используемого в модели.",
        [_.optionFormattingBreaksInitial]: "Разрыв строки",
        [_.optionFormattingBreaksNone]: "Та же строка",
        [_.optionFormattingLines]: "Строки",
        [_.optionFormattingLinesDescription]: "Выберите короткие или длинные строки.",
        [_.optionFormattingLinesValueLong]: "Длинные строки",
        [_.optionFormattingLinesValueShort]: "Короткие строки",
        [_.optionFormattingPreview]: "Автоматический предпросмотр",
        [_.optionFormattingPreviewDescription]: "Автоматически отправлять меры в DAX Formatter для создания предварительных просмотров.",
        [_.optionFormattingSeparators]: "Разделители",
        [_.optionFormattingSeparatorsDescription]: "ыберите разделители для чисел и списков.",
        [_.optionFormattingSeparatorsValueAuto]: "Авто",
        [_.optionFormattingSeparatorsValueEU]: "А; Б; В; 1234,00",
        [_.optionFormattingSeparatorsValueUS]: "А, Б, В, 1234.00",
        [_.optionFormattingSpaces]: "Пробел",
        [_.optionFormattingSpacesDescription]: "Управлять пробелами после имен функций.",
        [_.optionFormattingSpacesValueBestPractice]: "Лучшая практика",
        [_.optionFormattingSpacesValueFalse]: "Без пробелов - ЕСЛИ( ",
        [_.optionFormattingSpacesValueTrue]: "Пробели - ЕСЛИ( ",
        [_.optionLanguage]: "Язык",
        [_.optionLanguageDescription]: "Выберите язык Bravo. Требуется перезагрузка.",
        [_.optionLanguageResetConfirm]: "Вы хотите перезагрузить Браво, чтобы применить новый язык?",
        [_.optionsDialogAboutMenu]: "О программе",
        [_.optionsDialogFormattingMenu]: "Форматирование",
        [_.optionsDialogGeneralMenu]: "Общие",
        [_.optionsDialogTelemetryMenu]: "Диагностика",
        [_.optionsDialogTitle]: "Опции",
        [_.optionTelemetry]: "Телеметрия",
        [_.optionTelemetryDescription]: "Отправлять анонимные данные об использовании в SQLBI.",
        [_.optionTelemetryMore]: "Помогите нам понять, как вы используете Bravo и как его улучшить. Никакая личная информация не собирается. Обратите внимание, что если этот параметр отключен, команда разработчиков не сможет собирать необработанные ошибки и предоставлять дополнительную информацию. или поддержку.",
        [_.optionTheme]: "Тема",
        [_.optionThemeDescription]: "Установите тему Bravo. Оставьте <em>Системную</em>, чтобы позволить ОС выбрать.",
        [_.optionThemeValueAuto]: "Системная",
        [_.optionThemeValueDark]: "Темная",
        [_.optionThemeValueLight]: "Светлая",
        [_.otherColumnsRowName]: "Маленькие заголовки столбцов...",
        [_.paste]: "Вставить",
        [_.powerBiObserving]: "Ожидание открытия файла в Power BI Desktop…",
        [_.powerBiObservingCancel]: "Отменить",
        [_.powerBiSigninDescription]: "Войдите в службу Power BI, чтобы подключить Bravo к вашим онлайн-наборам данных.",
        [_.powerBiSigninTitle]: "Power BI",
        [_.quickActionAttachPBITitle]: "Присоединиться к Power BI Desktop",
        [_.quickActionConnectPBITitle]: "Подключиться к Power BI Service",
        [_.quickActionOpenVPXTitle]: "Открыть файл Vertipaq Analyzer",
        [_.refreshCtrlTitle]: "Обновить",
        [_.refreshPreviewCtrlTitle]: "Обновить предпросмотр",
        [_.saveVpaxCtrlTile]: "Сохранить как VPAX",
        [_.savingVpax]: "Создание VPAX...",
        [_.sceneUnsupportedReason]: "Эта функция недоступна для этого источника данных.",
        [_.sceneUnsupportedReasonManageDatesAutoDateTimeEnabled]: `Модели с включенной автоматической датой/временем не поддерживаются.<br><span class="link" href="https://www.sqlbi.com/tv/disabling-auto-date- time-in-power-bi/">Отключение автоматической настройки даты и времени в Power BI (видео)</span>`,
        [_.sceneUnsupportedReasonManageDatesPBIDesktopModelOnly]: "Эта функция поддерживается только моделями в режиме Power BI Desktop.",
        [_.sceneUnsupportedReasonMetadataOnly]: "База данных была сгенерирована из файла VPAX, который включает только его метаданные.",
        [_.sceneUnsupportedReasonReadOnly]: "Подключение к этой базе данных доступно только для чтения.",
        [_.sceneUnsupportedReasonXmlaEndpointNotSupported]: "Конечная точка XMLA не поддерживается для этого набора данных.",
        [_.sceneUnsupportedTitle]: "Не поддерживается",
        [_.searchColumnPlaceholder]: "Поиск столбца",
        [_.searchDatasetPlaceholder]: "Поиск набора данных",
        [_.searchEntityPlaceholder]: "Поиск таблицы/столбца",
        [_.searchMeasurePlaceholder]: "Поиск меры",
        [_.searchPlaceholder]: "Поиск",
        [_.searchTablePlaceholder]: "Поиск таблицы",
        [_.settingsCtrlTitle]: "Опции",
        [_.sheetOrphan]: "Недоступно",
        [_.sheetOrphanPBIXTooltip]: "Отчет был закрыт в Power BI Desktop. Любая операция записи запрещена.",
        [_.sheetOrphanTooltip]: "Эта модель больше недоступна. Любая операция записи запрещена.",
        [_.showDiagnosticPane]: "Показать подробности",
        [_.sideCtrlTitle]: "Включить параллельный просмотр",
        [_.signedInCtrlTitle]: "Выполнен вход как {name}",
        [_.signIn]: "Войти",
        [_.signInCtrlTitle]: "Войти",
        [_.signOut]: "Выйти",
        [_.sqlbiPayoff]: "Bravo — это проектSQLBI.",
        [_.syncCtrlTitle]: "Синхронизировать",
        [_.tableColCardinality]: "Количественность",
        [_.tableColCardinalityTooltip]: "Кол-во уникальных элементов",
        [_.tableColColumn]: "Столбец",
        [_.tableColColumns]: "Столбцы",
        [_.tableColMeasure]: "Мера",
        [_.tableColPath]: "Таблица \\ Столбец",
        [_.tableColRows]: "Строки",
        [_.tableColSize]: "Размер",
        [_.tableColTable]: "Таблица",
        [_.tableColWeight]: "Вес",
        [_.tableSelectedCount]: "{count} выбрано",
        [_.tableValidationInvalid]: "Это имя нельзя использовать.",
        [_.tableValidationValid]: "Это имя допустимо.",
        [_.themeCtrlTitle]: "Сменить тему",
        [_.toggleTree]: "Переключить древо",
        [_.traceId]: "Id трассировки",
        [_.unknownMessage]: "Получено недопустимое сообщение",
        [_.updateChannelBeta]: "Бета",
        [_.updateChannelCanary]: "Canary",
        [_.updateChannelDev]: "Dev",
        [_.updateChannelStable]: "Стабильный", 
        [_.updateMessage]: "Доступна новая версия Bravo: {version}",
        [_.validating]: "Проверка...",
        [_.version]: "Версия",
        [_.welcomeHelpText]: "Посмотрите видео ниже, чтобы узнать, как использовать Bravo:",
        [_.welcomeHelpTitle]: "Как пользоваться Bravo?",
        [_.welcomeText]: "Bravo — это удобный набор инструментов Power BI, который можно использовать для анализа моделей, форматирования показателей, создания таблиц дат и экспорта данных.",
        [_.whitespacesTitle]: "Пробелы",
        [_.wrappingTitle]: "Автоматический перенос слов",
    }
}
export default locale;
