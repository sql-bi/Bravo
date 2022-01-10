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

   constructor() {

      // Configuration options at https://docs.microsoft.com/en-us/azure/azure-monitor/app/javascript
      this.appInsights = new ApplicationInsights({ config: {
         instrumentationKey: Telemetry.INSTRUMENT_KEY,
         disableCookiesUsage: true,
         disableExceptionTracking: true,
         disablePageUnloadEvents: ["beforeunload", "unload", "visibilitychange", "pagehide"],
         disablePageShowEvents: ["pageshow", "visibilitychange"],
         disableAjaxTracking: true,
         autoTrackPageVisitTime: false,
         enableDebug: (!!debug),
         enableAutoRouteTracking: false,
         disableTelemetry: !optionsController.options.telemetryEnabled
      } });
      this.appInsights.loadAppInsights();

      optionsController.on("change", (changedOptions: any) => {
         if ("telemetryEnabled" in changedOptions) {

            this.appInsights.updateSnippetDefinitions({
               config: {
                  disableTelemetry: !optionsController.options.telemetryEnabled
               }
            });
         }
     });

     this.catchIdle();

   }

   track(name: string, props?: ICustomProperties) {
      if (!optionsController.options.telemetryEnabled) return;

      this.appInsights.trackEvent({ name: name }, props);
   }

   catchIdle() {
      
   }
}

export class Idle {

   static AwayTimeout = 15000;
   static Interactions = ["click", "mousemove", "mousedown", "keydown", "scroll", "mousewheel", "touchmove", "touchstart"];
   
   awayTimer: number;
   isAway: boolean;

   startTime: number;
   
   _idleTime: number;
   get idleTime(): number {
      this.updateIdleTime();
      return this._idleTime;
   } 

   constructor() {
      this._idleTime = 0;
      this.isAway = true;
      
      Idle.Interactions.forEach(event => {
         document.addEventListener(event, ()=>this.mouseListener());
      });
      document.addEventListener("visibilitychange", ()=>this.visibilityListener());
   } 
   
   destroy() {
      Idle.Interactions.forEach(event => {
         document.removeEventListener(event, ()=>this.mouseListener());
      });
      document.removeEventListener("visibilitychange", ()=>this.visibilityListener());
   }

   mouseListener() {
      if (this.isAway)
         this.active();

      window.clearTimeout(this.awayTimer);
      this.awayTimer = window.setTimeout(()=> {
         this.away();
     }, Idle.AwayTimeout);
   }

   visibilityListener() {
      if (document.visibilityState === 'hidden') {
         window.clearTimeout(this.awayTimer);
         this.away();
      } else if (document.visibilityState === 'visible') {
      	this.mouseListener();
      }
   }

   active() {
      this.isAway = false;
      this.updateStartTime();
   }

   away() {
      if (this.isAway) return;
      this.isAway = true;
      this.updateIdleTime();
   }

   updateIdleTime() {
      const now = new Date().getTime();
      this._idleTime += (now - this.startTime); 
   }

   updateStartTime() {
      this.startTime = new Date().getTime();
   }

}