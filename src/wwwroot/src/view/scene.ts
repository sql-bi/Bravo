/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/
import { View } from './view';

export class Scene extends View {
    rendered: boolean;
    title: string;

    constructor(id: string, container: HTMLElement, title?: string) {
        super(id, container);
        this.element.classList.add("scene");
        this.hide();
        
        this.title = title;
        this.rendered = false;
    }

    render() {
        this.rendered = true;
        this.show();
    }

    update() {

    }

    push(scene: Scene) {
        this.trigger("push", scene);
    }

    pop() {
        this.trigger("pop");
    }

    splice(scene: Scene) {
        this.trigger("splice", scene);
    }

    reload() {
        this.element.innerHTML = "";
        this.rendered = false;
        this.render();
    }
}