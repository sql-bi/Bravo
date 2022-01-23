# TODO

**@daniele**
- Format DAX
    - Current/Formatted side by side?

- Notification center messages
- Options/About window
- Telemetry 
    - Track idle time
    - Track UI events

- Manage dates UI
- Export Data UI
- i18n search for the first locale with the passed region
- Fix all custom sorters


**@alberto**

- Fix blurring icon

- Icon in **task manager** (not taskbar)

- Error on **ExportVpax** for local PBI Desktop report. Returns TOMDatabaseDatabaseNotFound but it should be work.
Replication steps:
    1. Open PBI Desktop without choosing anything
    2. Open Bravo and connect to Untitled
    3. From the untitled report in PBI Desktop open an existing report
    3. Click on the Sync icon in Bravo

    You are able to open the same report directly in Bravo.

- App returns "UnsupportedDatabaseCollectionIsEmpty" when opening a PBI Desktop report connected to a valid dataset online **if you're not logged in** (tried a new report connected to yours Adventure Works). Probably we need to introduce a new ConnectionStatus for this case, otherwise the UI can't know it needs to show the sign-in dialog.


- Error after logging and immediately calling listDatasets:
    Error HTTP/500: Internal Server Error. The format of value '' is invalid.
Trace Id: 00-9ed301aa19a38b40ba6443ab9d3583b6-3e1936c8575c514c-00