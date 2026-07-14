import { DashboardRenderer } from '../components/dashboard/DashboardRenderer';

const Dashboard = () => (
  <div className="flex flex-col h-full overflow-y-scroll">
    <div className="flex flex-col gap-3 px-3 pb-20">
    <DashboardRenderer />
    </div>
  </div>
);

export default Dashboard;
