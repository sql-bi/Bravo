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
- UI not displayed when the 'CONFIG.options.customOptions' property is not provided or is null (i.e. fresh installation or usersettings json is removed/emptied)

**@alberto**
- Fix login window size?
- Fix blurring icon

- Icon in task manager
    See GH issue https://github.com/tryphotino/photino.NET/issues/85
- Different carriage returns in formatting service response (fixable on UI side)
    Fixed on PR #51 (this is a DaxFormatter service issue, fix moved to .NET side)
- Network error doens't return on getModelFromDataset - also the CPU raises - check if the problem is on the host
    Fixed on PR #50