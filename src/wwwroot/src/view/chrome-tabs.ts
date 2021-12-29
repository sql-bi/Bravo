/*!
 * Copyright (c) 2013 Adam Schwartz
 * Modified by @danieleperilli
 *
 * The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

import Draggabilly from 'draggabilly';
import { Dispatchable } from '../helpers/dispatchable';
import { _, __, Utils } from '../helpers/utils';

const TAB_CONTENT_MARGIN = 9
const TAB_CONTENT_OVERLAP_DISTANCE = 1

const TAB_OVERLAP_DISTANCE = (TAB_CONTENT_MARGIN * 2) + TAB_CONTENT_OVERLAP_DISTANCE

const TAB_CONTENT_MIN_WIDTH = 24
const TAB_CONTENT_MAX_WIDTH = 270

const TAB_SIZE_SMALL = 84
const TAB_SIZE_SMALLER = 60
const TAB_SIZE_MINI = 48

const noop = (_: any) => {}

const closest = (value:number, array: any[]) => {
  let closest = Infinity
  let closestIndex = -1

  array.forEach((v, i) => {
    if (Math.abs(value - v) < closest) {
      closest = Math.abs(value - v)
      closestIndex = i
    }
  })

  return closestIndex
}

const tabTemplate = `
  <div class="chrome-tab">
    <div class="chrome-tab-dividers"></div>
    <div class="chrome-tab-background">
      <svg version="1.1" xmlns="http://www.w3.org/2000/svg"><defs><symbol id="chrome-tab-geometry-left" viewBox="0 0 214 36"><path d="M17 0h197v36H0v-2c4.5 0 9-3.5 9-8V8c0-4.5 3.5-8 8-8z"/></symbol><symbol id="chrome-tab-geometry-right" viewBox="0 0 214 36"><use xlink:href="#chrome-tab-geometry-left"/></symbol><clipPath id="crop"><rect class="mask" width="100%" height="100%" x="0"/></clipPath></defs><svg width="52%" height="100%"><use xlink:href="#chrome-tab-geometry-left" width="214" height="36" class="chrome-tab-geometry"/></svg><g transform="scale(-1, 1)"><svg width="52%" height="100%" x="-100%" y="0"><use xlink:href="#chrome-tab-geometry-right" width="214" height="36" class="chrome-tab-geometry"/></svg></g></svg>
    </div>
    <div class="chrome-tab-content">
      <div class="chrome-tab-favicon"></div>
      <div class="chrome-tab-title"></div>
      <div class="chrome-tab-drag-handle"></div>
      <div class="chrome-tab-close"></div>
    </div>
  </div>
`

const defaultTabProperties = {
  title: "New tab",
  id: "",
  favicon: false
}

let instanceId = 0
  
export interface ChromeTabsReorderInfo {
  element: HTMLElement
  originalIndex: number
  destIndex: number
}

export class ChromeTabs extends Dispatchable {

    el: HTMLElement;
    instanceId: number;
    draggabillies: any[];
    isDragging: boolean;
    styleEl: HTMLElement;
    draggabillyDragging: any;

    constructor() {
      super();

      this.draggabillies = []
    }

    init(el: HTMLElement) {
      this.el = el

      this.instanceId = instanceId
      this.el.setAttribute('data-chrome-tabs-instance-id', String(this.instanceId))
      instanceId += 1

      this.setupStyleEl()
      this.setupEvents()
      this.layoutTabs()
      this.setupDraggabilly()
    }

    setupStyleEl() {
      this.styleEl = document.createElement('style')
      this.el.appendChild(this.styleEl)
    }

    setupEvents() {
      window.addEventListener('resize', _ => {
        this.cleanUpPreviouslyDraggedTabs()
        this.layoutTabs()
      })

      /*this.el.addEventListener('dblclick', event => {
        if ([this.el, this.tabContentEl].includes(event.target)) this.addTab()
      })*/

      this.tabEls.forEach((tabEl: HTMLElement) => this.setTabCloseEventListener(tabEl))
    }

    get tabEls() {
      return Array.prototype.slice.call(__('.chrome-tab', this.el))
    }

    get tabContentEl() {
      return _('.chrome-tabs-content', this.el)
    }

    get tabContentWidths() {
     const numberOfTabs = this.tabEls.length

      const widths = []
      
      for (let i = 0; i < numberOfTabs; i += 1) {
        let tabEl = this.tabEls[i]

        let additionalSpace = _('.chrome-tab-favicon', tabEl).clientWidth + _('.chrome-tab-close', tabEl).clientWidth + 20;

        const tabContentWidth = Utils.DOM.measureWidth(_('.chrome-tab-title', tabEl).textContent, { fontSize: 13, fontFamily: "Segoe UI Variable,Segoe UI,-apple-system,Helvetica Neue,sans-serif"});

        const targetWidth = tabContentWidth + additionalSpace + (2 * TAB_CONTENT_MARGIN);
  
        const clampedTargetWidth = Math.max(TAB_CONTENT_MIN_WIDTH, Math.min(TAB_CONTENT_MAX_WIDTH, targetWidth))
        const flooredClampedTargetWidth = Math.floor(clampedTargetWidth)

        widths.push(flooredClampedTargetWidth)
      }

      return widths
    }

    get tabContentPositions() {
      const positions: number[] = []
      const tabContentWidths = this.tabContentWidths

      let position = TAB_CONTENT_MARGIN
      tabContentWidths.forEach((width, i) => {
        const offset = i * TAB_CONTENT_OVERLAP_DISTANCE
        positions.push(position - offset)
        position += width
      })

      return positions
    }

    get tabPositions() {
      const positions: number[] = []

      this.tabContentPositions.forEach((contentPosition) => {
        positions.push(contentPosition - TAB_CONTENT_MARGIN)
      })

      return positions
    }

    layoutTabs() {
      const tabContentWidths = this.tabContentWidths

      this.tabEls.forEach((tabEl: HTMLElement, i: number) => {
        const contentWidth = tabContentWidths[i]
        const width: number = contentWidth + (2 * TAB_CONTENT_MARGIN);

        (<HTMLElement>tabEl).style.width = width + 'px'
        tabEl.removeAttribute('is-small')
        tabEl.removeAttribute('is-smaller')
        tabEl.removeAttribute('is-mini')

        if (contentWidth < TAB_SIZE_SMALL) tabEl.setAttribute('is-small', '')
        if (contentWidth < TAB_SIZE_SMALLER) tabEl.setAttribute('is-smaller', '')
        if (contentWidth < TAB_SIZE_MINI) tabEl.setAttribute('is-mini', '')
      })

      let styleHTML = ''
      this.tabPositions.forEach((position, i) => {
        styleHTML += `
          .chrome-tabs[data-chrome-tabs-instance-id="${ this.instanceId }"] .chrome-tab:nth-child(${ i + 1 }) {
            transform: translate3d(${ position }px, 0, 0)
          }
        `
      })
      this.styleEl.innerHTML = styleHTML
    }

    createNewTabEl() {
      const div = document.createElement('div')
      div.innerHTML = tabTemplate
      return <HTMLElement>div.firstElementChild
    }

    addTab(tabProperties: any, { animate = true, background = false } = {}) {
      const tabEl = this.createNewTabEl()

      if (animate) {
        tabEl.classList.add('chrome-tab-was-just-added')
        setTimeout(() => tabEl.classList.remove('chrome-tab-was-just-added'), 500)
      }

      tabProperties = Object.assign({}, defaultTabProperties, tabProperties)
      this.tabContentEl.appendChild(tabEl)
      this.setTabCloseEventListener(tabEl)
      this.updateTab(tabEl, tabProperties)
      this.trigger('tabAdd', tabEl)
      if (!background) this.setCurrentTab(tabEl)
      this.cleanUpPreviouslyDraggedTabs()
      this.layoutTabs()
      this.setupDraggabilly()
    }

    setTabCloseEventListener(tabEl: HTMLElement) {
      _('.chrome-tab-close', tabEl).addEventListener('click', e => {
        this.trigger("tabClose", tabEl) 
        /*this.removeTab(tabEl)*/
      });
      tabEl.addEventListener("auxclick", (e: MouseEvent) => {
          e.preventDefault();
          e.stopPropagation();
          if (e.which == 2) { // Middle click
            this.trigger("tabClose", tabEl)  
            //this.removeTab(tabEl)
          } else if (e.which == 3) { // Right click
              //TODO Show menu
          }
      });
    }

    get activeTabEl() {
      return _('.chrome-tab[active]', this.el)
    }

    hasActiveTab() {
      return !!this.activeTabEl
    }

    setCurrentTab(tabEl: HTMLElement) {
      const activeTabEl = this.activeTabEl
      if (activeTabEl === tabEl) return
      if (activeTabEl) activeTabEl.removeAttribute('active')
      tabEl.setAttribute('active', '')
      this.trigger('activeTabChange', tabEl)
    }

    removeTab(tabEl: HTMLElement) {
      if (tabEl === this.activeTabEl) {
        if (tabEl.nextElementSibling) {
          this.setCurrentTab(<HTMLElement>tabEl.nextElementSibling)
        } else if (tabEl.previousElementSibling) {
          this.setCurrentTab(<HTMLElement>tabEl.previousElementSibling)
        }
      }
      tabEl.parentNode.removeChild(tabEl)
      this.trigger('tabRemove', tabEl)
      this.cleanUpPreviouslyDraggedTabs()
      this.layoutTabs()
      this.setupDraggabilly()
    }

    updateTab(tabEl: HTMLElement, tabProperties: any) {
      _('.chrome-tab-title', tabEl).textContent = tabProperties.title

      const faviconEl = _('.chrome-tab-favicon', tabEl)
      if (tabProperties.favicon) {
        //faviconEl.style.backgroundImage = `url('${ tabProperties.favicon }')`
        faviconEl.classList.add(tabProperties.favicon);
        faviconEl.removeAttribute('hidden')
      } else {
        faviconEl.setAttribute('hidden', '')
        //faviconEl.removeAttribute('style')
      }

      if (tabProperties.id) {
        tabEl.setAttribute('data-tab-id', tabProperties.id)
      }
    }

    cleanUpPreviouslyDraggedTabs() {
      this.tabEls.forEach((tabEl: HTMLElement) => tabEl.classList.remove('chrome-tab-was-just-dragged'))
    }

    setupDraggabilly() {
      const tabEls = this.tabEls
      const tabPositions = this.tabPositions

      if (this.isDragging) {
        this.isDragging = false
        this.el.classList.remove('chrome-tabs-is-sorting')
        this.draggabillyDragging.element.classList.remove('chrome-tab-is-dragging')
        this.draggabillyDragging.element.style.transform = ''
        this.draggabillyDragging.dragEnd()
        this.draggabillyDragging.isDragging = false
        this.draggabillyDragging.positionDrag = noop // Prevent Draggabilly from updating tabEl.style.transform in later frames
        this.draggabillyDragging.destroy()
        this.draggabillyDragging = null
      }

      this.draggabillies.forEach(d => d.destroy())

      tabEls.forEach((tabEl: HTMLElement, originalIndex: number) => {
        const originalTabPositionX = tabPositions[originalIndex]
        const draggabilly = new Draggabilly(tabEl, {
          axis: 'x',
          handle: '.chrome-tab-drag-handle',
          containment: this.tabContentEl
        })

        this.draggabillies.push(draggabilly)

        draggabilly.on('pointerDown', _ => {
          this.setCurrentTab(tabEl)
        })

        draggabilly.on('dragStart', _ => {
          this.isDragging = true
          this.draggabillyDragging = draggabilly
          tabEl.classList.add('chrome-tab-is-dragging')
          this.el.classList.add('chrome-tabs-is-sorting')
        })

        draggabilly.on('dragEnd', _ => {
          this.isDragging = false
          const finalTranslateX = parseFloat(tabEl.style.left)
          tabEl.style.transform = `translate3d(0, 0, 0)`

          // Animate dragged tab back into its place
          requestAnimationFrame(_ => {
            tabEl.style.left = '0'
            tabEl.style.transform = `translate3d(${ finalTranslateX }px, 0, 0)`

            requestAnimationFrame(_ => {
              tabEl.classList.remove('chrome-tab-is-dragging')
              this.el.classList.remove('chrome-tabs-is-sorting')

              tabEl.classList.add('chrome-tab-was-just-dragged')

              requestAnimationFrame(_ => {
                tabEl.style.transform = ''

                this.layoutTabs()
                this.setupDraggabilly()
              })
            })
          })
        })

        draggabilly.on('dragMove', (event, pointer, moveVector) => {
          // Current index be computed within the event since it can change during the dragMove
          const tabEls = this.tabEls
          const currentIndex = tabEls.indexOf(tabEl)

          const currentTabPositionX = originalTabPositionX + moveVector.x
          const destinationIndexTarget = closest(currentTabPositionX, tabPositions)
          const destinationIndex = Math.max(0, Math.min(tabEls.length, destinationIndexTarget))

          if (currentIndex !== destinationIndex) {
            this.animateTabMove(tabEl, currentIndex, destinationIndex)
          }
        })
      })
    }

    animateTabMove(tabEl:Element, originIndex: number, destinationIndex: number) {
      if (destinationIndex < originIndex) {
        tabEl.parentNode.insertBefore(tabEl, this.tabEls[destinationIndex])
      } else {
        tabEl.parentNode.insertBefore(tabEl, this.tabEls[destinationIndex + 1])
      }
      this.trigger('tabReorder', <ChromeTabsReorderInfo>{ element: tabEl, originalIndex: originIndex, destIndex: destinationIndex })
      this.layoutTabs()
    }
}