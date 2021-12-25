import { Doc } from '../model/doc';
import { Scene } from '../view/scene';
import { VpaxModelColumn } from '../model/model';
import { Tabulator } from 'tabulator-tables';
import Chart from "chart.js/auto";
import { TreemapScriptableContext } from 'chartjs-chart-treemap';
interface TabulatorVpaxModelColumn extends VpaxModelColumn {
    name: string;
    _containsUnreferenced?: boolean;
    _aggregated?: boolean;
    _children?: TabulatorVpaxModelColumn[];
}
export declare class AnalyzeModelScene extends Scene {
    table: Tabulator;
    chart: Chart;
    topSize: {
        tables: number;
        columns: number;
    };
    searchBox: HTMLInputElement;
    fullData: TabulatorVpaxModelColumn[];
    nestedData: TabulatorVpaxModelColumn[];
    nestedAggregatedData: TabulatorVpaxModelColumn[];
    aggregatedData: TabulatorVpaxModelColumn[];
    showAllColumns: boolean;
    groupByTable: boolean;
    showUnrefOnly: boolean;
    constructor(id: string, container: HTMLElement, doc: Doc);
    render(): void;
    aggregateData(data: TabulatorVpaxModelColumn[]): TabulatorVpaxModelColumn[];
    nestData(data: TabulatorVpaxModelColumn[]): TabulatorVpaxModelColumn[];
    updateData(): void;
    updateTable(redraw?: boolean, startExpanded?: boolean): void;
    expandTableColumns(): void;
    applyFilters(): void;
    unreferencedFilter(data: TabulatorVpaxModelColumn): boolean;
    chartItem(context: TreemapScriptableContext): any;
    chartColors(context: TreemapScriptableContext, type: string): string;
    updateChart(): void;
    restoreTableRowsActiveStatus(): void;
    updateToolbar(): void;
    update(): void;
    listen(): void;
}
export {};
