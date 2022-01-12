# TODO

@daniele
- Sync
    - update name from listreports
X Error handling
    - Problem Detail
    - Instance contains Internal error code
    If HTTPStatus = 400 -
         - application error
    else
        unhandled error
    BravoProblem enum messages

x Remove export as vpax download
x Export as VPAX near sync
x Signin window dark input border
- Format DAX:
    - Fix: don't replace all formatted measures with the one returned
    - Catch measure errors
        - In preview
        - In format
    - Allow "Current/Formatted(Preview)" to be arranged side by side
    - Add format with DAX Formatter link 
- Options/About window
- Track idle time
- Handle not signing in DAX Format action
X Remove links
X Hash for vpax
- Telemetry context
- Allow to copy trace id

@alberto
- Fix login window size
- Fix Icon DPI
- Implement External Tools web message

-- 2022-01-11

-- Sometimes "ListDatasets" returns error 500 cors (when token expired, I guess because if I sign in them it works - we need 401)
-- Automatic token refresh on API call?
X- "GetModelFromDataset" returns error 406 (Not capable) if you're not logged - we need 401
-- "UpdateDataset" returns unhandled error when if you're not logged (or token expired) - we need 401
-- Subsequential formattings returns error 409 (Conflict)
-- "UpdateReport" with local reports returns error

-- Track API (every call, signin, signout) and app open/close
    - Do you track the session? Also after enabling the Telemetry from settings?
-- Handle file dragged in the app

-- 2022-01-12

- Can't abort exportVpax
