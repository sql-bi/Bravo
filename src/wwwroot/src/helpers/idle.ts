/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

export class Idle {

    static AwayTimeout = 15000;
    static CatchingEvents = ["click", "mousemove", "mousedown", "keydown", "scroll", "mousewheel", "touchmove", "touchstart"];
    
    awayTimer: number;
    isAway: boolean;
    startTime: number;
    
    _time: number;
    get time(): number {
       this.updateIdleTime();
       return this._time;
    } 
 
    constructor() {
      this._time = 0;
      this.isAway = true;
       
       Idle.CatchingEvents.forEach(event => {
          document.addEventListener(event, ()=>this.mouseListener());
       });
       document.addEventListener("visibilitychange", ()=>this.visibilityListener());
    } 

    reset() {
      this._time = 0;
      this.updateStartTime();
    }
    
    destroy() {
       Idle.CatchingEvents.forEach(event => {
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
       this._time += (now - this.startTime); 
    }
 
    updateStartTime() {
       this.startTime = new Date().getTime();
    }
 
 }