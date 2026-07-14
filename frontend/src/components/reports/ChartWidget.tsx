import { useLayoutEffect, useMemo, useRef, useState } from 'react';
import {
  DonutChart,
  GroupedVerticalBarChart,
  LineChart,
  VerticalBarChart,
} from '@fluentui/react-charts';
import { Text } from '@fluentui/react-components';

import type { ReportExecutionResult, ReportType } from '../../types/report';
import { toFluentReportChartModel } from '../../utils/chartData';
import { REPORT_CHART_COLORS } from '../../utils/chartColors';

interface ChartWidgetProps {
  reportType: ReportType;
  data: ReportExecutionResult;
  height?: string;
}

function parseHeightPx(height: string): number {
  const parsed = Number.parseInt(height, 10);
  return Number.isFinite(parsed) ? parsed : 220;
}

function useChartParentRef() {
  const containerRef = useRef<HTMLDivElement>(null);
  const [parentRef, setParentRef] = useState<HTMLElement | null>(null);

  useLayoutEffect(() => {
    setParentRef(containerRef.current);
  });

  return { containerRef, parentRef };
}

export function ChartWidget({ reportType, data, height = '220px' }: ChartWidgetProps) {
  const { containerRef, parentRef } = useChartParentRef();
  const chartModel = useMemo(() => toFluentReportChartModel(data), [data]);
  const chartHeight = parseHeightPx(height);
  const isMultiSeries = chartModel.groupedBarData.some((group) => group.series.length > 1);

  const sharedProps = {
    parentRef,
    culture: 'en-ZA',
  };

  return (
    <div ref={containerRef} className="h-full w-full min-h-0" style={{ height, minHeight: height }}>
      {chartModel.isEmpty ? (
        <div className="flex h-full items-center justify-center">
          <Text className="text-sm text-neutral-foreground-3">No data</Text>
        </div>
      ) : reportType === 'Pie' || reportType === 'Donut' ? (
        <DonutChart
          {...sharedProps}
          data={{ chartData: chartModel.donutPoints }}
          innerRadius={reportType === 'Donut' ? Math.round(chartHeight * 0.22) : 0}
          hideLegend={false}
        />
      ) : reportType === 'Line' ? (
        <LineChart
          {...sharedProps}
          data={{ lineChartData: chartModel.lineSeries }}
          tickValues={chartModel.lineCategories}
        />
      ) : isMultiSeries ? (
        <GroupedVerticalBarChart
          {...sharedProps}
          data={chartModel.groupedBarData}
          colors={REPORT_CHART_COLORS}
        />
      ) : (
        <VerticalBarChart
          {...sharedProps}
          data={chartModel.verticalBarPoints}
          colors={REPORT_CHART_COLORS}
          useSingleColor={false}
        />
      )}
    </div>
  );
}
