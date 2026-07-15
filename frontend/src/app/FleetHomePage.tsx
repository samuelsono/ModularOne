import { DashboardRenderer } from '@modules/reporting/components/dashboard/DashboardRenderer';

/** App composition: fleet home hosts the reporting dashboard without fleet→reporting import. */
export default function FleetHomePage() {
  return (
    <div className="flex flex-col h-full overflow-y-scroll">
      <div className="flex flex-col gap-3 px-3 pb-20">
        <DashboardRenderer />
      </div>
    </div>
  );
}
