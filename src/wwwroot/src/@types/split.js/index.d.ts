// Type definitions for Split.js 
// Definitions by: Daniele Perilli <https://github.com/danieleperilli>

declare module 'split.js' {
    type Partial<T> = { [P in keyof T]?: T[P] }
    type CSSStyleDeclarationPartial = Partial<CSSStyleDeclaration>
    export interface SplitOptions {
        sizes?: number[]
        minSize?: number[] | number
        maxSize?: number | number[]
        expandToMin?: boolean
        gutterSize?: number
        gutterAlign?: string
        snapOffset?: number
        dragInterval?: number
        direction?: "horizontal" | "vertical";
        cursor?: string;
        gutter?: (index: number, direction: "horizontal" | "vertical") => HTMLElement;
        elementStyle?: (dimension: "width" | "height", elementSize: number, gutterSize: number, index: number) => CSSStyleDeclarationPartial;
        gutterStyle?: (dimension: "width" | "height", gutterSize: number, index: number) => CSSStyleDeclarationPartial;
        onDrag?: (sizes: number[])  => void;
        onDragStart?: (sizes: number[]) => void;
        onDragEnd?: (sizes: number[]) => void;
    }

    export interface SplitObject {
        setSizes: (sizes: number[]) => void;
        getSizes: () => number[];
        collapse: (index: number) => void;
        destroy: (preserveStyles?: boolean, preserveGutters?: boolean) => void;
    }

    function Split(elements: HTMLElement | string[], options?: SplitOptions): SplitObject;

    export default Split;
}