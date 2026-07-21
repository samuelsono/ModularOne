import * as React from 'react';
import { Body1, Button, Card, CardHeader, Text, tokens } from '@fluentui/react-components';
import { MoreHorizontal20Regular } from '@fluentui/react-icons/svg/more-horizontal';

import type { ReportWithData } from '@modules/reporting/types/report';
import { ChartWidget } from './ChartWidget';
import { ApiTableWidget } from './ApiTableWidget';
import { MapWidget } from './MapWidget';
import { MetricCardWidget } from './MetricCardWidget';
import { ReportCardSettingsDialog } from './ReportCardSettingsDialog';
import AppTitle from '@platform/ui/AppTitle';

interface ReportWidgetProps {
  item: ReportWithData;
  sectionId?: string;
  onUpdated?: () => void;
}

export function ReportWidget({ item, sectionId, onUpdated }: ReportWidgetProps) {
  const { report, data } = item;
  const [settingsOpen, setSettingsOpen] = React.useState(false);
  const canEdit = Boolean(sectionId);

  const menuButton = canEdit ? (
    <Button
      appearance="transparent"
      icon={<MoreHorizontal20Regular />}
      aria-label="Report settings"
      onClick={() => setSettingsOpen(true)}
    />
  ) : undefined;

  return (
    <div className="h-full min-h-0 flex flex-col">
      {report.reportType === 'MetricCard' ? (
        <MetricCardWidget
          title={report.name}
          description={report.description}
          data={data}
          action={menuButton}
        />
      ) : report.reportType === 'ApiTable' ? (
        <Card className="h-full min-h-0 flex flex-col">
          <CardHeader
            header={<Text weight="semibold">{report.name}</Text>}
            description={report.description ? <Body1 className="font-thin!">{report.description}</Body1> : undefined}
            action={menuButton}
          />
          <div className="flex-1 min-h-0 overflow-auto px-2 pb-3">
            <ApiTableWidget data={data} />
          </div>
        </Card>
      ) : report.reportType === 'Map' ? (
        <Card className="h-full min-h-0 flex flex-col">
          <CardHeader
            header={<Text weight="semibold">{report.name}</Text>}
            description={report.description ? <Body1 className="font-thin!">{report.description}</Body1> : undefined}
            action={menuButton}
          />
          <div className="flex-1 min-h-0">
            <MapWidget data={data} />
          </div>
        </Card>
      ) : (<>
        {/* <Card className="h-full min-h-0 flex flex-col">
          <CardHeader
            header={<Text weight="semibold">{report.name}</Text>}
            description={report.description ? <Body1 className="font-thin!">{report.description}</Body1> : undefined}
            action={menuButton}
          />
          <div className="flex flex-1 min-h-0 h-full items-center px-2 pb-3">
            <ChartWidget reportType={report.reportType} data={data} />
          </div>
        </Card> */}
        <div
          className="h-full min-h-0 flex flex-col shadow rounded p-4 border"
          style={{
            backgroundColor: tokens.colorNeutralBackground1,
            borderColor: tokens.colorNeutralStroke3,
          }}
        >
          <div className='flex items-center justify-between w-full'>
              <div className='flex flex-col'>
                <Text weight="semibold">{report.name}</Text>
                {report.description ? <Body1 className="font-thin!">{report.description}</Body1> : undefined}
              </div>
              {menuButton}
          </div>

          <div className="flex flex-1 min-h-0 h-full items-center px-2 pb-3">
            <ChartWidget reportType={report.reportType} data={data} />
          </div>
        </div>
      </>

      )}

      {canEdit && sectionId && (
        <ReportCardSettingsDialog
          open={settingsOpen}
          sectionId={sectionId}
          item={item}
          onOpenChange={setSettingsOpen}
          onSaved={onUpdated}
        />
      )}
    </div>
  );
}
