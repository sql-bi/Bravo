/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ApplicationInsights, ICustomProperties } from '@microsoft/applicationinsights-web'
import { Utils } from '../helpers/utils';
import { optionsController, debug } from '../main';
import { ProblemDetails } from './host';

export interface TelemetryConfig {
   instrumentationKey: string,
   contextDeviceOperatingSystem?: string,
   contextComponentVersion?: string,
   contextSessionId?: string,
   contextUserId?: string
}

export class Telemetry {

   appInsights: ApplicationInsights;
   sessionStarted: boolean;
   enabled: boolean;

   constructor(config: TelemetryConfig) {

      this.enabled = optionsController.options.telemetryEnabled;

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
         disableTelemetry: !this.enabled
      } });
      this.appInsights.loadAppInsights();

      // Set telemetry context
      if (config.contextComponentVersion)
         this.appInsights.context.application.ver = config.contextComponentVersion;
      
      if (config.contextSessionId)
         this.appInsights.context.session.id = config.contextSessionId;

      if (config.contextUserId)
         this.appInsights.context.user.id = config.contextUserId;

      this.trackStart();

      // Detect telemetry option change
      optionsController.on("change", (changedOptions: any) => {
         if ("telemetryEnabled" in changedOptions) {

            this.enabled = optionsController.options.telemetryEnabled;

            this.appInsights.updateSnippetDefinitions({
               config: {
                  disableTelemetry: !this.enabled
               }
            });
            this.trackStart();
         }
     });

   }

   trackStart() {
      if (!this.enabled || this.sessionStarted) return;
      this.appInsights.startTrackPage();
      this.sessionStarted = true;
   }

   trackEnd() {
      if (!this.enabled) return;
      this.appInsights.stopTrackPage();
   }

   trackError(problem: ProblemDetails, props?: ICustomProperties) {
      if (!this.enabled) return;

      const traceId = (problem.traceId ? problem.traceId : Utils.Text.uuid());
      this.appInsights.trackException({ 
         id: traceId,
         exception: {
            name: String(problem.status),
            message: problem.title
         }
      }, props);
   }

   track(name: string, props?: ICustomProperties) {
      if (!this.enabled) return;

      this.appInsights.trackEvent({ name: name }, props);
   }


   destroy() {
      this.trackEnd();
   }
}
