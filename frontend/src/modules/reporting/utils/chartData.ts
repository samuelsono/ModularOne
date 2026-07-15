import type {
  ChartDataPoint,
  GroupedVerticalBarChartData,
  LineChartPoints,
  VerticalBarChartDataPoint,
} from '@fluentui/react-charts';

import type { ReportExecutionResult } from '@modules/reporting/types/report';
import { getChartColor } from './chartColors';

export interface FluentReportChartModel {
  isEmpty: boolean;
  verticalBarPoints: VerticalBarChartDataPoint[];
  groupedBarData: GroupedVerticalBarChartData[];
  lineSeries: LineChartPoints[];
  lineCategories: string[];
  donutPoints: ChartDataPoint[];
}

interface WideChartTable {
  categoryHeader: string;
  categories: string[];
  seriesNames: string[];
  values: Map<string, number>;
}

export function toFluentReportChartModel(result: ReportExecutionResult): FluentReportChartModel {
  const emptyModel: FluentReportChartModel = {
    isEmpty: true,
    verticalBarPoints: [{ x: 'No data', y: 0, legend: 'No data' }],
    groupedBarData: [],
    lineSeries: [],
    lineCategories: [],
    donutPoints: [{ legend: 'No data', data: 0 }],
  };

  if (result.rows.length === 0) {
    return emptyModel;
  }

  const wide = toWideChartTable(result);
  if (!wide) {
    return emptyModel;
  }

  const verticalBarPoints = wide.categories.map((category, categoryIndex) => ({
    x: category,
    y: wide.values.get(`${category}::${wide.seriesNames[0]}`) ?? 0,
    legend: category,
    color: getChartColor(categoryIndex),
  }));

  const groupedBarData: GroupedVerticalBarChartData[] = wide.categories.map((category) => ({
    name: category,
    series: wide.seriesNames.map((series, seriesIndex) => ({
      key: series,
      legend: series,
      data: wide.values.get(`${category}::${series}`) ?? 0,
      color: getChartColor(seriesIndex),
    })),
  }));

  const lineSeries: LineChartPoints[] = wide.seriesNames.map((series, seriesIndex) => ({
    legend: series,
    color: getChartColor(seriesIndex),
    data: wide.categories.map((category, index) => ({
      x: index,
      y: wide.values.get(`${category}::${series}`) ?? 0,
    })),
  }));

  const donutPoints: ChartDataPoint[] = wide.seriesNames.length === 1
    ? wide.categories.map((category, categoryIndex) => ({
        legend: category,
        data: wide.values.get(`${category}::${wide.seriesNames[0]}`) ?? 0,
        color: getChartColor(categoryIndex),
      }))
    : wide.seriesNames.map((series, seriesIndex) => ({
        legend: series,
        data: wide.categories.reduce(
          (total, category) => total + (wide.values.get(`${category}::${series}`) ?? 0),
          0,
        ),
        color: getChartColor(seriesIndex),
      }));

  return {
    isEmpty: false,
    verticalBarPoints,
    groupedBarData,
    lineSeries,
    lineCategories: wide.categories,
    donutPoints,
  };
}

function toWideChartTable(result: ReportExecutionResult): WideChartTable | null {
  if (result.columns.length === 0) {
    return null;
  }

  if (result.columns.length === 3 && result.rows.every((row) => row.length === 3)) {
    return pivotLongToWide(result.columns, result.rows);
  }

  if (result.columns.length < 2) {
    return null;
  }

  const categoryHeader = result.columns[0] ?? 'Category';
  const seriesNames = result.columns.slice(1).map((column) => String(column));
  const categories: string[] = [];
  const values = new Map<string, number>();

  for (const row of result.rows) {
    const category = String(row[0] ?? 'Unknown');
    if (!categories.includes(category)) {
      categories.push(category);
    }

    seriesNames.forEach((series, index) => {
      values.set(`${category}::${series}`, Number(row[index + 1] ?? 0));
    });
  }

  return {
    categoryHeader,
    categories,
    seriesNames,
    values,
  };
}

function pivotLongToWide(
  columns: string[],
  rows: (string | number | null)[][],
): WideChartTable {
  const categoryIndex = 0;
  const seriesIndex = 1;
  const valueIndex = 2;

  const categories: string[] = [];
  const seriesNames: string[] = [];

  for (const row of rows) {
    const category = String(row[categoryIndex] ?? 'Unknown');
    const series = String(row[seriesIndex] ?? 'Unknown');
    if (!categories.includes(category)) {
      categories.push(category);
    }
    if (!seriesNames.includes(series)) {
      seriesNames.push(series);
    }
  }

  const values = new Map<string, number>();
  for (const row of rows) {
    const key = `${row[categoryIndex]}::${row[seriesIndex]}`;
    values.set(key, Number(row[valueIndex] ?? 0));
  }

  return {
    categoryHeader: columns[categoryIndex] ?? 'Category',
    categories,
    seriesNames,
    values,
  };
}

export function formatMetricValue(value: number): string {
  if (Number.isInteger(value)) {
    return value.toLocaleString('en-ZA');
  }

  return value.toLocaleString('en-ZA', { maximumFractionDigits: 2 });
}

export function formatChangePercent(changePercent?: number | null): string | null {
  if (changePercent === null || changePercent === undefined) {
    return null;
  }

  const sign = changePercent >= 0 ? '+' : '';
  return `${sign}${changePercent.toFixed(1)}%`;
}
