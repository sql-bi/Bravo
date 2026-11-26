/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ApplicationInsights, ICustomProperties, IEventTelemetry, IExceptionTelemetry, IMetricTelemetry, IPageViewTelemetry, ITelemetryItem } from '@microsoft/applicationinsights-web'
import { Idle } from '../helpers/idle';
import { Utils } from '../helpers/utils';
import { optionsController, debug } from '../main';
import { TelemetryDetails } from '../generated/bravo';

/** What is told of an error that the page raises itself. */
export interface TrackedProblem {
   status?: number
   title?: string
   traceId?: string
}

export class Telemetry {

   appInsights: ApplicationInsights;
   appOpenTracked: boolean;
   enabled: boolean;
   history: string[] = [];
   idle: Idle;

   /**
    * @param details Where the telemetry goes and what its items carry, as the host gives them. Without a
    * connection string nothing is ever sent, whatever the option of the user says: the build sends it nowhere.
    */
   constructor(private readonly details: TelemetryDetails) {

      this.idle = new Idle();
      this.enabled = false;
      if (!details.connectionString) return;

      this.applyConsent();

      // The option of the user, which a policy locks, decides at once, both ways
      optionsController.on("telemetryEnabled.change", () => this.applyConsent());

      window.addEventListener('beforeunload', () => this.destroy());
   }

   /**
    * Follows the consent. On, the page creates its sender when it has none, and sends from then. Off, nothing
    * more is sent, not even what waits to be sent.
    */
   private applyConsent() {
      this.enabled = optionsController.getOption("telemetryEnabled");

      if (this.enabled && !this.appInsights)
         this.appInsights = this.createSender();

      if (this.appInsights) {
         // The SDK reads it at each item and at each send: off, it takes no item, and a send drops what waits
         this.appInsights.config.disableTelemetry = !this.enabled;
         if (!this.enabled)
            this.appInsights.flush(false);
      }

      this.trackAppOpen();
   }

   private createSender(): ApplicationInsights {

      // Configuration options at https://learn.microsoft.com/azure/azure-monitor/app/javascript-sdk-configuration.
      // As in 1.0.x, the SDK sends what the page tracks and its own critical errors, and keeps what waits in the
      // session storage of the page, which it sends again when the page loads again. No cookie, no automatic item
      const appInsights = new ApplicationInsights({ config: {
         connectionString: this.details.connectionString,
         disableCookiesUsage: true,
         disableExceptionTracking: true,
         disablePageUnloadEvents: ["beforeunload", "unload", "visibilitychange", "pagehide"],
         disablePageShowEvents: ["pageshow", "visibilitychange"],
         disableFetchTracking: true,
         disableAjaxTracking: true,
         autoTrackPageVisitTime: false,
         enableAutoRouteTracking: false
      } });
      appInsights.loadAppInsights();

      // The values of the host: the items of the page and of the host share the user, the run and the version
      appInsights.context.application.ver = this.details.version;
      appInsights.context.session.id = this.details.sessionId;
      appInsights.context.user.id = this.details.userId;

      appInsights.addTelemetryInitializer((item: ITelemetryItem) => {
         // The SDK times the first page view by itself
         if (item.baseType === "PageviewPerformanceData")
            return false;

         item.data = { ...item.data, ...this.details.properties };
         debug.logTelemetry(item.baseType, item.baseData, item.data);
      });

      return appInsights;
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

   trackError(problem: TrackedProblem, props?: ICustomProperties) {
      if (!this.enabled) return;

      const traceId = (problem.traceId ? problem.traceId : Utils.Text.uuid());
      const exception: IExceptionTelemetry = {
         id: traceId,
         exception: {
            name: String(problem.status),
            message: problem.title
         }
      };

      this.appInsights.trackException(exception, props);
   }

   track(eventName: string, props?: ICustomProperties) {
      if (!this.enabled) return;

      const event: IEventTelemetry = { name: eventName };

      this.appInsights.trackEvent(event, props);
   }

   trackPage(pageName: string) {
      if (!this.enabled) return;
      if (this.history.length && this.history[this.history.length - 1] == pageName) return;

      this.trackPageTime();
      this.history.push(pageName);

      const pageView: IPageViewTelemetry = { name: pageName };

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

         this.appInsights.trackMetric(metric, props);
      }
      this.idle.reset();
   }

   destroy() {
      this.trackPageTime();
      this.idle.destroy();
      this.trackAppClose();
      this.appInsights?.flush();
   }
}
