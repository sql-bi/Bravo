/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ApplicationInsights, ICustomProperties, IEventTelemetry, IExceptionTelemetry, IMetricTelemetry, IPageViewTelemetry } from '@microsoft/applicationinsights-web'
import { Idle } from '../helpers/idle';
import { Utils } from '../helpers/utils';
import { optionsController, debug } from '../main';
import { ProblemDetails } from './host';

export interface TelemetryConfig {
   instrumentationKey: string,
   contextDeviceOperatingSystem?: string,
   contextComponentVersion?: string,
   contextSessionId?: string,
   contextUserId?: string,
   globalProperties?: any
}

export class Telemetry {

   appInsights: ApplicationInsights;
   appOpenTracked: boolean;
   enabled: boolean;
   history: string[] = [];
   idle: Idle;

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
         //enableDebug: debug.enabled,
         autoTrackPageVisitTime: false,
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
      
      if (config.globalProperties) {
         this.appInsights.addTelemetryInitializer(initializer => {
            for (let property in config.globalProperties)
               initializer.data[property] = config.globalProperties[property];
         });
      }

      this.trackAppOpen();

      // Detect telemetry option change
      optionsController.on("telemetryEnabled.change", (changedOptions: any) => {

         this.enabled = optionsController.options.telemetryEnabled;

         this.appInsights.updateSnippetDefinitions({
            config: {
               disableTelemetry: !this.enabled
            }
         });
         this.trackAppOpen();
         
      });

      window.addEventListener('beforeunload', e => {
         this.destroy();
      });

      this.idle = new Idle();
   }

   trackAppOpen() {
      if (!this.enabled) return;
      if (this.appOpenTracked) return;

      this.track("Start");
      this.appOpenTracked = true;
   }

   trackAppClose() {
      if (!this.enabled) return;
      this.track("End");
   }

   trackError(problem: ProblemDetails, props?: ICustomProperties) {
      if (!this.enabled) return;

      const traceId = (problem.traceId ? problem.traceId : Utils.Text.uuid());
      const exception: IExceptionTelemetry = { 
         id: traceId,
         exception: {
            name: String(problem.status),
            message: problem.title
         }
      };
      
      if (!debug.catchTelemetryTracking("error", exception, props))
         this.appInsights.trackException(exception, props);
   }

   track(eventName: string, props?: ICustomProperties) {
      if (!this.enabled) return;

      const event: IEventTelemetry = { name: eventName };

      if (!debug.catchTelemetryTracking("event", event, props))
         this.appInsights.trackEvent(event, props);
   }

   trackPage(pageName: string) {
      if (!this.enabled) return;
      if (this.history.length && this.history[this.history.length - 1] == pageName) return;

      this.trackPageTime();
      this.history.push(pageName);

      const pageView: IPageViewTelemetry = { name: pageName };

      if (!debug.catchTelemetryTracking("pageView", pageView))
         this.appInsights.trackPageView(pageView);
   }

   trackPreviousPage() {
      if (this.history.length > 1) {
         this.trackPage(this.history[this.history.length - 2]);
      }
   }

   trackPageTime() {
      if (!this.enabled) return;
      if (!this.history.length) return;

      const seconds = Math.round(this.idle.time / 1000);
      if (seconds) {
         const metric: IMetricTelemetry = { name: "PageVisitTime", average: seconds };
         const props: ICustomProperties = { "PageName": this.history[this.history.length - 1] };

         if (!debug.catchTelemetryTracking("metric", metric, props))
            this.appInsights.trackMetric(metric, props);
      }
      this.idle.reset();
   }

   destroy() {
      this.trackPageTime();
      this.idle.destroy();
      this.trackAppClose();
      this.appInsights.flush();
   }
}
