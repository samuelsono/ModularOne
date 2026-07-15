import {
  HomeRegular,
  MapRegular,
  PersonRegular,
  ArrowTrendingLinesRegular,
  VehicleCarRegular,
  CalendarRegular,
  MoneyRegular,
  DocumentRegular,
  PeopleTeamRegular,
  TargetRegular,
  BriefcaseRegular,
} from "@fluentui/react-icons";
import { NavLink } from "react-router-dom";
import { useStyles } from '@platform/shell/navStyles';
import { useActiveApp } from '@platform/shell/ActiveAppContext';
import { isModuleNavItem } from '@platform/permissions/apps';
import { Divider } from "@fluentui/react-components";

const NAV_ICONS: Record<string, typeof HomeRegular> = {
  Home: HomeRegular,
  Track: MapRegular,
  Vehicles: VehicleCarRegular,
  Drivers: PersonRegular,
  Reports: ArrowTrendingLinesRegular,
  Company: BriefcaseRegular,
  Dept: PeopleTeamRegular,
  Position: PersonRegular,
  Users: PeopleTeamRegular,
  Billing: MoneyRegular,
  Invoice: DocumentRegular,
  Receive: MoneyRegular,
  Settings: DocumentRegular,
  Requests: CalendarRegular,
  Approve: DocumentRegular,
  Calendar: CalendarRegular,
  Policy: DocumentRegular,
  Balance: CalendarRegular,
  Claims: MoneyRegular,
  Category: DocumentRegular,
  Runs: MoneyRegular,
  Payslip: DocumentRegular,
  Deduct: MoneyRegular,
  Reviews: TargetRegular,
  Goals: TargetRegular,
  Feedback: DocumentRegular,
  Jobs: BriefcaseRegular,
  Apply: DocumentRegular,
  Interview: PeopleTeamRegular,
  Offers: BriefcaseRegular,
};

const SideNavigation = () => {
  const styles = useStyles();
  const { currentNavItems, currentModule, isLoading } = useActiveApp();

  if (isLoading && currentNavItems.length === 0) {
    return null;
  }

  if (currentNavItems.length === 0) {
    return null;
  }

  return (
    <nav className={`${styles.sideNav} px-x bg-slate-50`} aria-label={`${currentModule?.name ?? 'Application'} navigation`}>
      {currentNavItems.map((entry) => {
        if (!isModuleNavItem(entry)) {
          return (
            <div
              key={entry.id}
              className="flex justify-center w-14 shrink-0"
              aria-hidden="true"
            >
              <div className="w-9 h-px bg-neutral-stroke-2" />
            </div>
          );
        }

        const Icon = NAV_ICONS[entry.shortLabel] ?? HomeRegular;

        return (
          <NavLink
            key={entry.path}
            to={entry.path}
            end={entry.path === '/' || entry.path === '/leave' || entry.path === '/expense'}
            title={entry.label}
            className={({ isActive }) =>
              `flex flex-col items-center justify-center w-14 h-14 hover:bg-gray-200 ${
                isActive ? 'bg-gray-100 font-semibold border-l-2 ' + styles.activeLink : ''
              }`
            }
          >
            <Icon className={styles.icon18} />
            <small>{entry.shortLabel}</small>
          </NavLink>
        );
      })}
    </nav>
  );
};

export default SideNavigation;
