/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ApplicationInsights, ICustomProperties } from '@microsoft/applicationinsights-web'
import { optionsController } from '../main';
import { debug } from '../debug';

export class Telemetry {
   static INSTRUMENT_KEY = "47a8970c-6293-408a-9cce-5b7b311574d3";

   appInsights: ApplicationInsights;
   sessionTracked: boolean;

   constructor() {

      // Configuration options at https://docs.microsoft.com/en-us/azure/azure-monitor/app/javascript
      this.appInsights = new ApplicationInsights({ config: {
         instrumentationKey: Telemetry.INSTRUMENT_KEY,
         isStorageUseDisabled: true,
         disableExceptionTracking: true,
         autoTrackPageVisitTime: true,
         disableAjaxTracking: true,
         enableDebug: (!!debug),
         enableAutoRouteTracking: true, // ?
         disableTelemetry: !optionsController.options.telemetryEnabled
      } });
      this.appInsights.loadAppInsights();

      if (optionsController.options.telemetryEnabled) {
         this.appInsights.trackPageView();
         this.sessionTracked = true;
      } else {
         this.sessionTracked = false;
      }

      optionsController.on("change", (changedOptions: any) => {
         if ("telemetryEnabled" in changedOptions) {

            this.appInsights.updateSnippetDefinitions({
               config: {
                  disableTelemetry: !optionsController.options.telemetryEnabled
               }
            });

            if (optionsController.options.telemetryEnabled && !this.sessionTracked)
               this.appInsights.trackPageView();
         }
     });
   }

   track(name: string, props?: ICustomProperties) {
      if (!optionsController.options.telemetryEnabled) return;

      this.appInsights.trackEvent({ name: name }, props);
   }
}