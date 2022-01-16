/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
 * 
 * Based on Contextual.js <https://github.com/LucasReade/Contextual.js>
 * MIT License
*/

import { Utils } from './utils';

export interface ContextMenuOptions {
    width?: number | "auto"
    isSticky?: boolean
    items: ContextMenuItemOptions[]
}

export interface ContextMenuItemOptions {
    label: string
    type?: ContextMenuItemType
    markup?: string
    icon?: string
    cssIcon?: string
    shortcut?: string
    onClick?: () => void
    enabled?: boolean | (() => boolean)
    items?: ContextMenuItemOptions[]
}

export enum ContextMenuItemType {
    separator,
    label,
    custom,
    multiButton,
    subMenu,
    hoverMenu,
    normal
}

export class ContextMenu{

    position: boolean;
    menuControl: HTMLElement;

    /**
     * Creates a new contextual menu
     * @param {object} options options which build the menu e.g. position and items
     * @param {number} options.width sets the width of the menu including children
     * @param {boolean} options.isSticky sets how the menu apears, follow the mouse or sticky
     * @param {Array<ContextMenuItem>} options.items sets the default items in the menu
     */
    constructor(options: ContextMenuOptions, event?: Event){   
        ContextMenu.closeMenu();

        this.position = options.isSticky != null ? options.isSticky : false;
        this.menuControl = createElement(`<ul class='contextualJs contextualMenu'></ul>`);

        if (options.width != "auto")
            this.menuControl.style.width = (options.width != null ? options.width + 'px' : '200px');

        options.items.forEach(i => {
            let item = new ContextMenuItem(i);
            this.menuControl.appendChild(item.element);
        });
            
        if (event){
            event.stopPropagation()
            document.body.appendChild(this.menuControl);
            ContextMenu.positionMenu(this.position, event, this.menuControl);        
        }

        document.addEventListener("click", e => {
            this.catch(e);
        });   
        document.addEventListener("auxclick", e => {
            this.catch(e);
        }); 
    }

    catch(e: MouseEvent) {
        if(!(<HTMLElement>e.target).classList.contains('contextualJs')){
            ContextMenu.closeMenu();
        }
    }

    /**
     * Adds item to this contextual menu instance
     * @param {ContextMenuItem} item item to add to the contextual menu
     */
    add(item: ContextMenuItem){
        this.menuControl.appendChild(item.element);
    }
    /**
     * Makes this contextual menu visible
     */
    show(event: Event){
        event.stopPropagation();
        document.body.appendChild(this.menuControl);
        ContextMenu.positionMenu(this.position, event, this.menuControl);    
    }
    /**
     * Hides this contextual menu
     */
    hide(){
        ContextMenu.closeMenu();
    }
    /**
     * Toggle visibility of menu
     */
    toggle(event: Event){
        event.stopPropagation();
        if(this.menuControl.parentElement != document.body){
            document.body.appendChild(this.menuControl);
            ContextMenu.positionMenu(this.position, event, this.menuControl);        
        }else{
            ContextMenu.closeMenu();
        }
    }

    static closeMenu() {
        let openMenuItem = document.querySelector('.contextualMenu:not(.contextualMenuHidden)');
        if (openMenuItem != null)
            document.body.removeChild(openMenuItem);  
    }

    static positionMenu(docked: boolean, event: Event, menu: HTMLElement) {
        
        if (docked) {
            let target = <HTMLElement>event.target;
            menu.style.left = ((target.offsetLeft + menu.offsetWidth) >= window.innerWidth) ? 
                ((target.offsetLeft - menu.offsetWidth) + target.offsetWidth)+"px"
                    : (target.offsetLeft)+"px";

            menu.style.top = ((target.offsetTop + menu.offsetHeight) >= window.innerHeight) ?
                (target.offsetTop - menu.offsetHeight)+"px"    
                    : (target.offsetHeight + target.offsetTop)+"px";
        } else {
            let clientX = (<MouseEvent>event).clientX;
            menu.style.left = ((clientX + menu.offsetWidth) >= window.innerWidth) ?
                ((clientX - menu.offsetWidth))+"px"
                    : (clientX)+"px";
            
            let clientY = (<MouseEvent>event).clientY;
            menu.style.top = ((clientY + menu.offsetHeight) >= window.innerHeight) ?
                (clientY - menu.offsetHeight)+"px"    
                    : (clientY)+"px";
        }
    }

    static async editorContextMenu(event: Event, selectedText = "", text = "", element: HTMLInputElement = null): Promise<ContextMenu> {

        const ctrl = (Utils.Platform.isMac ? "⌘" : "Ctrl");

        const clipboardText = await navigator.clipboard.readText();

        let items = [];
        if (element) 
            items.push({ label: "Cut", cssIcon: "icon-cut", shortcut: `${ctrl}+X`, enabled: (selectedText != ""), onClick: () => { 
                navigator.clipboard.writeText(selectedText);
                element.value = text.substring(0, element.selectionStart) + text.substring(element.selectionEnd);
            }});

        items.push({ label: "Copy", cssIcon: "icon-copy", shortcut: `${ctrl}+X`, enabled: (text != ""), onClick: () => { 
            navigator.clipboard.writeText(selectedText != "" ? selectedText : text);
        }});

        if (element) 
            items.push({ label: "Paste", cssIcon: "icon-paste", shortcut: `${ctrl}+V`, enabled: (clipboardText != ""), onClick: () => { 
                element.value = text.substring(0, element.selectionStart) + clipboardText + text.substring(element.selectionEnd);
            }});

        return new ContextMenu({ items: items }, event);

    }
}  

export class ContextMenuItem{
    element: HTMLElement;

    constructor(options: ContextMenuItemOptions){

        let enabled = true;
        if (typeof options.enabled !== "undefined") {
            if (typeof options.enabled === "boolean") {
                enabled = options.enabled;
            } else {
                enabled = options.enabled();
            }
        }

        switch(options.type){
            case ContextMenuItemType.separator:
                this.separator();
                break;
            case ContextMenuItemType.custom:
                this.custom(options.markup);
                break;
            case ContextMenuItemType.multiButton: 
                this.multiButton(options.items);
                break;
            case ContextMenuItemType.subMenu:
                this.subMenu(options.label, options.items, (options.icon !== undefined ? options.icon : ''), (options.cssIcon !== undefined ? options.cssIcon : ''), enabled);
                break;
            case ContextMenuItemType.hoverMenu: 
                this.hoverMenu(options.label, options.items, (options.icon !== undefined ? options.icon : ''), (options.cssIcon !== undefined ? options.cssIcon : ''), enabled);
                break;
            case ContextMenuItemType.label:
                this.label(options.label, (options.icon !== undefined ? options.icon : ''), (options.cssIcon !== undefined ? options.cssIcon : ''));
                break;
            case ContextMenuItemType.normal:
            default:
                this.button(options.label, options.onClick, (options.shortcut !== undefined ? options.shortcut : ''), (options.icon !== undefined ? options.icon : ''), (options.cssIcon !== undefined ? options.cssIcon : ''), enabled);       
        }
    }

    label (label: string, icon = '', cssIcon = '') {
        this.element = createElement( `
            <li class='contextualJs contextualMenuItemOuter'>
                <div class='contextualJs contextualMenuItem static'>
                    ${icon != ''? `<img src='${icon}' class='contextualJs contextualMenuItemIcon'/>` : `<div class='contextualJs contextualMenuItemIcon ${cssIcon != '' ? cssIcon : ''}'></div>`}
                    <span class='contextualJs contextualMenuItemTitle'>${label}</span>
                </div>
            </li>`);               
    }

    button(label: string, onClick: () => void, shortcut = '', icon = '', cssIcon = '', enabled = true){
        this.element = createElement( `
            <li class='contextualJs contextualMenuItemOuter'>
                <div class='contextualJs contextualMenuItem ${enabled == true ? '' : 'disabled'}'>
                    ${icon != ''? `<img src='${icon}' class='contextualJs contextualMenuItemIcon'/>` : `<div class='contextualJs contextualMenuItemIcon ${cssIcon != '' ? cssIcon : ''}'></div>`}
                    <span class='contextualJs contextualMenuItemTitle'>${label}</span>
                    <span class='contextualJs contextualMenuItemTip'>${shortcut == ''? '' : shortcut}</span>
                </div>
            </li>`);               

            if (enabled){
                this.element.addEventListener('click', (e) => {
                    e.stopPropagation();
                    if (onClick) onClick();  
                    ContextMenu.closeMenu();
                }, false);
            } 
    }
    custom(markup: string){
        this.element = createElement(`<li class='contextualJs contextualCustomEl'>${markup}</li>`);
    }
    hoverMenu(label: string, items?: ContextMenuItemOptions[], icon = '', cssIcon = '', enabled = true){
        this.element = createElement(`
            <li class='contextualJs contextualHoverMenuOuter'>
                <div class='contextualJs contextualHoverMenuItem ${enabled == true ? '' : 'disabled'}'>
                    ${icon != ''? `<img src='${icon}' class='contextualJs contextualMenuItemIcon'/>` : `<div class='contextualJs contextualMenuItemIcon ${cssIcon != '' ? cssIcon : ''}'></div>`}
                    <span class='contextualJs contextualMenuItemTitle'>${label}</span>
                    <span class='contextualJs contextualMenuItemOverflow'>></span>
                </div>
                <ul class='contextualJs contextualHoverMenu'>
                </ul>
            </li>
        `);

        let childMenu = this.element.querySelector('.contextualHoverMenu'),
        menuItem = this.element.querySelector('.contextualHoverMenuItem');

        if (items !== undefined) {
            items.forEach(i => {
                let item = new ContextMenuItem(i);
                childMenu.appendChild(item.element);
            });
        }
        if (enabled){
            menuItem.addEventListener('mouseenter', () => {});
            menuItem.addEventListener('mouseleave', () => {});
        }
    }
    multiButton(items: ContextMenuItemOptions[]) {
        this.element = createElement(`
            <li class='contextualJs contextualMultiItem'>
            </li>
        `);
        items.forEach(i => {
            let item = new ContextMenuItem(i);
            this.element.appendChild(item.element);
        });
    }
    subMenu(label: string, items?: ContextMenuItemOptions[], icon = '', cssIcon = '', enabled = true) {
        this.element = createElement( `
            <li class='contextualJs contextualMenuItemOuter'>
                <div class='contextualJs contextualMenuItem ${enabled == true ? '' : 'disabled'}'>
                    ${icon != ''? `<img src='${icon}' class='contextualJs contextualMenuItemIcon'/>` : `<div class='contextualJs contextualMenuItemIcon ${cssIcon != '' ? cssIcon : ''}'></div>`}
                    <span class='contextualJs contextualMenuItemTitle'>${label}</span>
                    <span class='contextualJs contextualMenuItemOverflow'>
                        <span class='contextualJs contextualMenuItemOverflowLine'></span>
                        <span class='contextualJs contextualMenuItemOverflowLine'></span>
                        <span class='contextualJs contextualMenuItemOverflowLine'></span>
                    </span>
                </div>
                <ul class='contextualJs contextualSubMenu contextualMenuHidden'>
                </ul>
            </li>`); 

        let childMenu = this.element.querySelector('.contextualSubMenu'),
            menuItem = this.element.querySelector('.contextualMenuItem');

        if(items !== undefined) {
            items.forEach(i => {
                let item = new ContextMenuItem(i);
                childMenu.appendChild(item.element);
            });
        }
        if(enabled == true){
            menuItem.addEventListener('click',() => {
                menuItem.classList.toggle('SubMenuActive');
                childMenu.classList.toggle('contextualMenuHidden');
            }, false);
        }
    }
    separator() {
        this.element = createElement(`<li class='contextualJs contextualMenuSeparator'><span></span></li>`);
    }
}

function createElement(html: string) {
    var el = document.createElement('div');
    el.innerHTML = html;
    return <HTMLElement>el.firstElementChild;
}