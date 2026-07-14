import { getNextColor } from '@fluentui/react-charts';

export function getChartColor(index: number): string {
  return getNextColor(index);
}

export const REPORT_CHART_COLORS = Array.from({ length: 8 }, (_, index) => getNextColor(index));
