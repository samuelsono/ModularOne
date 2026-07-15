import { useState } from "react";
import type { SelectTabData, SelectTabEvent } from "@fluentui/react-components";
import { Subtitle2, Tab, TabList } from "@fluentui/react-components";
import { ReportsTable } from '@modules/reporting/components/ReportsTable';
import AppFilters from '@platform/ui/AppFilters';
import { DashboardManagementPanel } from '@modules/reporting/components/dashboard/DashboardManagementPanel';
import { DashboardPicker } from '@modules/reporting/components/dashboard/DashboardPicker';
import { ReportBuilderDialog } from '@modules/reporting/components/reports/ReportBuilderDialog';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { getSelectedDashboardId, setSelectedDashboardId } from '@modules/reporting/utils/dashboardStorage';

const filters = [
  { name: "status", label: "Status", value: "active" },
  { name: "type", label: "Type", value: "chart" },
  { name: "table", label: "Location", value: "city" },
];

type ReportsTab = "reports" | "dashboards";

const ReportsPage = () => {
  const searchQuery = usePageSearchQuery();
  const [selectedTab, setSelectedTab] = useState<ReportsTab>("reports");
  const [refreshToken, setRefreshToken] = useState(0);
  const [dashboardId, setDashboardId] = useState<string | null>(getSelectedDashboardId());

  const handleDashboardChange = (id: string) => {
    setDashboardId(id);
    setSelectedDashboardId(id);
  };

  const handleChanged = () => {
    setRefreshToken((value) => value + 1);
  };

  return (
    <div className="flex flex-col w-full h-full px-3 pt-3 overflow-y-hidden">
      <div className="flex flex-wrap items-center justify-between gap-3 mb-3">
        <div className="flex flex-wrap items-center gap-3 mx-3 min-w-0">
          <Subtitle2 className="mb-0">Reports management</Subtitle2>
          <DashboardPicker
            value={dashboardId}
            onChange={handleDashboardChange}
            refreshToken={refreshToken}
            className="min-w-[200px]"
          />
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <AppFilters filters={filters} onFilterChange={() => {}} />
          {selectedTab === "reports" && (
            <ReportBuilderDialog onSaved={handleChanged} />
          )}
        </div>
      </div>

      <TabList
        className="mx-3 mb-3"
        selectedValue={selectedTab}
        onTabSelect={(_event: SelectTabEvent, data: SelectTabData) => setSelectedTab(data.value as ReportsTab)}
      >
        <Tab value="reports">Reports</Tab>
        <Tab value="dashboards">Dashboards &amp; Sections</Tab>
      </TabList>

      {selectedTab === "reports" ? (
        <div className="flex flex-col w-full flex-1 min-h-0 bg-white rounded shadow overflow-hidden">
          <div className="p-3 border-b border-[#e3e5e7]">
            <Subtitle2 className="">Your Reports</Subtitle2>
          </div>
          <div className="flex-1 min-h-0 overflow-auto">
            <ReportsTable
              refreshToken={refreshToken}
              dashboardId={dashboardId}
              searchQuery={searchQuery}
              onChanged={handleChanged}
            />
          </div>
        </div>
      ) : (
        <div className="flex flex-col w-full flex-1 min-h-0 overflow-hidden">
          <DashboardManagementPanel
            selectedDashboardId={dashboardId}
            onDashboardChange={handleDashboardChange}
            refreshToken={refreshToken}
            searchQuery={searchQuery}
            onChanged={handleChanged}
          />
        </div>
      )}
    </div>
  );
};

export default ReportsPage;
