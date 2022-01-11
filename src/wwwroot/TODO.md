# TODO

@daniele
- Format DAX:
    - Fix last step layout
    - Catch measure errors
    - Allow "Current/Formatted(Preview)" to be arranged side by side
    - Fix Dax Formatter table losing active row on sync
    - Disable DAX Formatter table checkboxes on sync
- Fix table row hover color    


@alberto
- Automatic token refresh on API call?
- Fix login window size
- Optional pass the email address to Power BI auth
- Sometimes "ListDatasets" returns error 500 cors (when token expired, I guess because if I sign in them it works - we need 401)
- Error when opening datasets which you are not authorized to open
- Error if "Untitled" report is empty
- Disable browser key shortcuts for refreshing (CTRL+F5/CTRL+R,CMD+R), printing, etc...
- Open Download window dialog from the host on "saveVPAX"
- Fix Icon DPI
- Implement External Tools web message

-- 2022-01-10

- "GetModelFromDataset" returns error 406 (Not capable) if you're not logged - we need 401
- "UpdateDataset" return unhandled error when if you're not logged (or token expired) - we need 401
- Subsequential formattings returns error 409 (Conflict)
- Format DAX error with local reports
- Track Api (calls, signin, signout) and app open/close
    - Do you track the session? Also after enabling the Telemetry from settings?

- Auto check closed reports? Does it impact performance? 