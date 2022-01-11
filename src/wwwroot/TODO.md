# TODO

@daniele
- Format DAX:
    - Catch measure errors
        - In preview
        - In format
    - Allow "Current/Formatted(Preview)" to be arranged side by side 
- Options/About window
- Track idle time

@alberto
- Fix login window size
- Optional pass the email address to Power BI auth
- Error when opening datasets which you are not authorized to open
- Error if "Untitled" report is empty
- Disable browser key shortcuts for refreshing (CTRL+F5/CTRL+R,CMD+R), printing, etc...
- Open Download window dialog from the host on "saveVPAX"
- Fix Icon DPI
- Implement External Tools web message

-- 2022-01-11

- Sometimes "ListDatasets" returns error 500 cors (when token expired, I guess because if I sign in them it works - we need 401)
- Automatic token refresh on API call?
- "GetModelFromDataset" returns error 406 (Not capable) if you're not logged - we need 401
- "UpdateDataset" returns unhandled error when if you're not logged (or token expired) - we need 401
- Subsequential formattings returns error 409 (Conflict)
- "UpdateReport" with local reports returns error

- Track API (every call, signin, signout) and app open/close
    - Do you track the session? Also after enabling the Telemetry from settings?
- Handle file dragged in the app
- Auto check closed reports? Does it impact performance? 