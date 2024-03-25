/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import {  _, __ } from '../helpers/utils';
import { Doc, DocType } from '../model/doc';
import { i18n } from '../model/i18n'; 
import { strings } from '../model/strings';
import { ConnectMenuItem } from './connect-item';

export class ConnectFile extends ConnectMenuItem {

    render(element: HTMLElement) {
        super.render(element);
        
        let html = `
            <div class="drop-area list">
                <p>${i18n(strings.connectDragFile)}</p>
            
                <div class="browse button">
                    ${i18n(strings.connectBrowse)}
                </div>
            </div>
            <input type="file" class="file-browser" accept=".vpax,.ovpax">
        `;
        this.element.insertAdjacentHTML("beforeend", html);

        _(".browse", this.element).addEventListener("click", e => {
            _(".file-browser", this.element).dispatchEvent(new MouseEvent("click"));
        });

        _(".file-browser", this.element).addEventListener("change", e => {
            
            let fileElement = (<any>e.target);
            if (fileElement.files) {
                let files: File[] = fileElement.files;
                if (files.length) {
                    this.openFile(files[0]);
                }
            }
        });
        

        let dropArea = _(".drop-area", this.element);
        dropArea.addEventListener('drop', (e) => {
            if (e.dataTransfer.files.length) {
                this.openFile(e.dataTransfer.files[0]);
            }
        }, false);

        ['dragenter', 'dragover'].forEach(eventName => {
            dropArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                dropArea.classList.add('highlight');
            }, false);
        });
          
        ['dragleave', 'drop'].forEach(eventName => {
            dropArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                dropArea.classList.remove('highlight');
            }, false);
        });
    }

    appear() {
        this.dialog.okButton.toggle(false);
    }

    openFile(file: File) {
        if (file && (file.name.slice(-5) == ".vpax" || file.name.slice(-6) == ".ovpax")) {
            this.dialog.data.doc = new Doc(file.name, DocType.vpax, file);
            this.dialog.trigger("action", "ok");
        }
    }
}