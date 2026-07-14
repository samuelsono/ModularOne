import { Body1, Card, CardHeader, Text, type CardHeaderProps } from '@fluentui/react-components';

import type { ReportExecutionResult } from '../../types/report';
import { formatChangePercent, formatMetricValue } from '../../utils/chartData';

interface MetricCardWidgetProps {
  title: string;
  description?: string | null;
  data: ReportExecutionResult;
  action?: CardHeaderProps['action'];
}

export function MetricCardWidget({ title, description, data, action }: MetricCardWidgetProps) {
  const value = data.metric?.value ?? Number(data.rows[0]?.[0] ?? 0);
  const changePercent = formatChangePercent(data.metric?.changePercent);
  const comparisonValue = data.metric?.comparisonValue;

  const descriptionText = description
    ?? (comparisonValue != null && changePercent
      ? `vs prev ${formatMetricValue(comparisonValue)} (${changePercent})`
      : 'Fleet metric');

  return (
    <Card className="h-full min-h-0 flex flex-col">
      <CardHeader
        header={<Text weight="semibold">{title}</Text>}
        description={<Body1 className="font-thin!">{descriptionText}</Body1>}
        action={action}
      />
      <p className="flex flex-1 items-center text-4xl text-left font-thin px-4 pb-4">
        {formatMetricValue(value)}
      </p>
    </Card>
  );
}
