/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ApplicationInsights, ICustomProperties } from '@microsoft/applicationinsights-web'
import { optionsController, debug } from '../main';

export interface TelemetryConfig {
   instrumentationKey: string,
   contextDeviceOperatingSystem?: string,
   contextComponentVersion?: string,
   contextSessionId?: string,
   contextUserId?: string
}

export class Telemetry {

   appInsights: ApplicationInsights;

   constructor(config: TelemetryConfig) {

      // Configuration options at https://docs.microsoft.com/en-us/azure/azure-monitor/app/javascript
      this.appInsights = new ApplicationInsights({ config: {
         instrumentationKey: config.instrumentationKey,
         disableCookiesUsage: true,
         disableExceptionTracking: true,
         disablePageUnloadEvents: ["beforeunload", "unload", "visibilitychange", "pagehide"],
         disablePageShowEvents: ["pageshow", "visibilitychange"],
         disableAjaxTracking: true,
         autoTrackPageVisitTime: false,
         enableDebug: debug.enabled,
         enableAutoRouteTracking: false,
         disableTelemetry: !optionsController.options.telemetryEnabled
      } });
      this.appInsights.loadAppInsights();

      // Set telemetry context
      if (config.contextComponentVersion)
         this.appInsights.context.application.ver = config.contextComponentVersion;
      
      if (config.contextSessionId)
         this.appInsights.context.session.id = config.contextSessionId;

      if (config.contextUserId)
         this.appInsights.context.user.id = config.contextUserId;

      // Detect telemetry option change
      optionsController.on("change", (changedOptions: any) => {
         if ("telemetryEnabled" in changedOptions) {

            this.appInsights.updateSnippetDefinitions({
               config: {
                  disableTelemetry: !optionsController.options.telemetryEnabled
               }
            });
         }
     });

   }

   track(name: string, props?: ICustomProperties) {
      if (!optionsController.options.telemetryEnabled) return;

      this.appInsights.trackEvent({ name: name }, props);
   }

   
}
