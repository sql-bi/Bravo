/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
"use strict";

class Connect extends Dialog {

    menu;
    okButton;
    localReportsTimeout;
    items = {
        "attach-pbi": {
            name: strings.connectDialogAttachPBILabel,  
            render: () => this.renderAttachPBI()
        },
        "connect-pbi": {
            name: strings.connectDialogConnectPBILabel, 
            render: () => this.renderConnectPBI()
        },
        "open-vpx": {    
            name: strings.connectDialogOpenVPXLabel,    
            render: () => this.renderOpenVPX()
        }
    };

    constructor() {

        super("connect", document.body, strings.connectDialogTitle, [
            { name: strings.dialogOpen, action: "ok" },
            { name: strings.dialogCancel, action: "cancel", className: "button-alt" } 
        ]);

        this.menu = new Menu("connect-menu", this.body, this.items);

        this.okButton = _(".button[data-action=ok]", this.element);

        this.listen();
    }

    listen() {
        this.menu.on("change", id => {
            let found = false;
            __(".item-body", this.body).forEach(div => {
                if (div.id == `body-${id}`) {
                    div.removeAttribute("hidden"); 
                    found = true;
                } else {
                    div.setAttribute("hidden", "");
                }
            });

            if (!found) {
                this.items[id].render();
            }

            if (id == 'open-vpx') {
                this.okButton.setAttribute("hidden", "");
            } else {
                this.okButton.removeAttribute("hidden");
                this.okButton.setAttribute("disabled", "");
            }
        });
    }

    show(selectedId) {
        this.menu.select(selectedId);
        return super.show();
    }

    renderAttachPBI() {
        let html = `
            <div id="body-attach-pbi" class="item-body">
                <div class="list">
                </div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        this.getLocalPBIReports();
        this.localReportsTimeout = setInterval(_ => this.getLocalPBIReports(), 5000);
    }

    getLocalPBIReports() {

        host.listPBIDesktopReports()
        .then(reports => {

            let listHTML = "<ul>";

            //TODO
            reports.forEach(report => {
                listHTML += `
                    <li data-name="" data-id=""></li>
                `;
            });

            listHTML += "</ul>";

            _("#body-attach-pbi .list", this.body).innerHTML = listHTML;

            __("#body-attach-pbi li", this.body).forEach(li => {
                li.addEventListener("click", e => {
                    e.preventDefault();

                    let el = e.currentTarget;
                    __("#body-attach-pbi li", this.body).forEach(_el => {
                        _el.classList.remove("active");
                    });
                    el.classList.add("active");

                    this.data = new Doc(el.dataset.name, "pbix", {
                        reportId: el.dataset.id
                    });
                    this.okButton.removeAttribute("disabled");
                });
            });
        })
        .catch(e => {
            _("#body-attach-pbi .list", this.body).innerHTML = `
                <div class="notice">${strings.connectNoReports}</div>
            `;
        });
    }

    renderConnectPBI() {
        let html = `
            <div id="body-connect-pbi" class="item-body">
                <div class="list">
                </div>
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        if (!account.signedIn) {
            _("#body-connect-pbi .list", this.body).innerHTML = `
                <div class="notice">
                    <div>
                        <p>${strings.errorNotConnected}</p>
                        <div class="signin button">${strings.signIn}</div>
                    </div>
                </div>
            `;
        } else {
            getRemotePBIDatasets();
        }
    }

    getRemotePBIDatasets() {
        host.listPBIServiceDatasets()
        .then(r => {
            //TODO

            __("#body-connect-pbi li", this.body).forEach(li => {
                li.addEventListener("click", e => {
                    e.preventDefault();
                    
                });
            });
        })
        .catch(err => {
            _("#body-connect-pbi .list", this.body).innerHTML = `
                <div class="notice">
                    <div>
                        <p>${strings.errorDatasetListing}</p>
                        <div class="retry-connect-pbi button button-alt">Retry</div>
                    </div>
                </div>
                
            `;

            _("retry-connect-pbi", this.body).addEventListener("click", e => {
                e.preventDefault();
                this.getRemotePBIDatasets();

            }); 
        });
    }

    renderOpenVPX() {

        let html = `
            <div id="body-open-vpx" class="item-body"> 
                <div class="drop-area list">
                    <p>${strings.connectDragFile}</p>
                
                    <div class="browse button">
                        ${strings.connectBrowse}
                    </div>
                </div>
                <input type="file" class="file-browser" accept=".vpax">
            </div>
        `;
        this.body.insertAdjacentHTML("beforeend", html);

        _("#body-open-vpx .browse", this.body).addEventListener("click", e => {
            _(".file-browser", this.body).dispatchEvent(new MouseEvent("click"));
        });

        _(".file-browser", this.body).addEventListener("change", e => {

            if (e.target.files && e.target.files.length) {
                let file = e.target.files[0];
                this.data = new Doc(file.name, "vpax", file);
                this.trigger("action-ok");
            }
        });
        

        let dropArea = _(".drop-area", this.body);
        dropArea.addEventListener('drop', (e) => {
            if (e.dataTransfer.files.length) {
                let file = e.dataTransfer.files[0];
                if (file.name.slice(-5) == ".vpax") {
                    this.data = new Doc(file.name, "vpax", file);
                    this.trigger("action-ok");
                }
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

    destroy() {
        clearInterval(this.localReportsTimeout);
        super.destroy();
    }
}