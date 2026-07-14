import type { ReportExecutionResult } from '../../types/report';
import { ReportWidget } from './ReportWidget';

interface ReportPreviewProps {
  name: string;
  reportType: ReportExecutionResult['reportType'];
  data: ReportExecutionResult | null;
  loading?: boolean;
  error?: string | null;
}

export function ReportPreview({ name, reportType, data, loading, error }: ReportPreviewProps) {
  if (loading) {
    return <div className="p-4 text-sm text-neutral-500">Running preview...</div>;
  }

  if (error) {
    return <div className="p-4 text-sm text-red-600">{error}</div>;
  }

  if (!data) {
    return <div className="p-4 text-sm text-neutral-500">Configure the report and click Preview to see a live chart.</div>;
  }

  return (
    <ReportWidget
      item={{
        report: {
          id: 'preview',
          sectionId: '',
          name,
          reportType,
          size: 'Medium',
          sortOrder: 0,
          isVisible: true,
          targetTable: '',
          aggregateFunction: 'Count',
          groupByColumns: [],
          filters: [],
          comparisonEnabled: false,
          placements: [],
          updatedAt: new Date().toISOString(),
        },
        data,
      }}
    />
  );
}
