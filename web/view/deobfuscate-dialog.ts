/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { _, __ } from '../helpers/utils';
import { i18n } from '../model/i18n';
import { strings } from '../model/strings';
import { Dialog } from './dialog';

export class DeobfuscateDialog extends Dialog {

    constructor() {
        super("deobfuscate", document.body, i18n(strings.deobfuscateDialogTitle), [], "icon-lock", false, true);
    }

    show() {
        let html = `
            <div class="deobfuscate-content">
                <div class="drop-area list">
                    <p>${i18n(strings.deobfuscateDialogDragFile)}</p>

                    <div class="browse button">
                        ${i18n(strings.connectBrowse)}
                    </div>
                </div>
                <input type="file" class="dict-file-browser" accept=".dict" hidden>
                <p class="note">${i18n(strings.deobfuscateDialogNote)}</p>
            </div>
        `;

        this.body.insertAdjacentHTML("beforeend", html);

        // Browse button click
        _(".browse", this.element).addEventListener("click", e => {
            _(".dict-file-browser", this.element).dispatchEvent(new MouseEvent("click"));
        });

        // File input change
        _(".dict-file-browser", this.element).addEventListener("change", e => {
            let fileElement = (<any>e.target);
            if (fileElement.files && fileElement.files.length) {
                this.handleSelectedFile(fileElement.files[0], _(".drop-area", this.element));
            }
            fileElement.value = "";
        });

        // Prevent drops on the dialog overlay from reaching the global handler in app.ts
        this.element.addEventListener("dragover", (e) => {
            e.preventDefault();
            e.stopPropagation();
            e.dataTransfer.dropEffect = "none";
        });
        this.element.addEventListener("drop", (e) => {
            e.preventDefault();
            e.stopPropagation();
        });

        // Drag and drop on the drop-area
        let dropArea = _(".drop-area", this.element);

        dropArea.addEventListener("drop", (e) => {
            e.preventDefault();
            e.stopPropagation();
            dropArea.classList.remove("highlight");
            if (e.dataTransfer.files.length) {
                this.handleSelectedFile(e.dataTransfer.files[0], dropArea);
            }
        }, false);

        ["dragenter", "dragover"].forEach(eventName => {
            dropArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
                (<DragEvent>e).dataTransfer.dropEffect = "copy";
                dropArea.classList.add("highlight");
            }, false);
        });

        ["dragleave"].forEach(eventName => {
            dropArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
                dropArea.classList.remove("highlight");
            }, false);
        });

        return super.show();
    }

    selectFile(file: File): boolean {
        if (!file) return false;

        const fileName = file.name.toLowerCase();
        if (!fileName.endsWith(".dict")) return false;

        this.data = { dictionaryFile: file };
        this.trigger("action", "deobfuscate");
        return true;
    }

    handleSelectedFile(file: File, dropArea: HTMLElement): void {
        if (!this.selectFile(file)) {
            dropArea.classList.add("reject");
            setTimeout(() => dropArea.classList.remove("reject"), 1000);
        }
    }
}
