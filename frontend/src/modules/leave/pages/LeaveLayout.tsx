import { Outlet } from 'react-router-dom';
import { tokens, Subtitle2, Text } from '@fluentui/react-components';
import { UpcomingHolidaysList } from '@modules/leave/components/UpcomingHolidaysList';

import {
  addMonths,
  addYears,
  addDays,
  Calendar,
} from "@fluentui/react-calendar-compat";
import { MobileHolidaysDrawer } from '../components/MobileHolidaysDrawer';

export default function LeaveLayout() {
  return (
    <div className="flex flex-col h-screen min-h-0 p-3 gap-2" style={{ backgroundColor: tokens.colorNeutralBackground3 }}>
      <div className='flex justify-between items-start'>
      <div className='flex flex-col'>
        <Subtitle2>Leave management</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3 w-4/5">
          Dashboard, requests, approvals, and team leave visibility.
        </Text>
      </div>
      
      <div className='flex gap-3 xl:hidden'>
        <MobileHolidaysDrawer />
      </div>
      </div>
      <div className='flex gap-3'>

      <div className="flex-1 min-h-0 h-[90vh] overflow-auto rounded shadow py-3" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
        <Outlet />
      </div>
      <div className="hidden xl:inline w-[300px] max-w-4/12 mb-6 max-h-[50vh]">
                  <div className="rounded shadow p-1 mb-3 flex flex-col items-center" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
                    <Calendar 
                        highlightSelectedMonth
                        showMonthPickerAsOverlay
                        showGoToToday={false}
                        className="w-full"  
                    />

                  </div>
                  
                  <UpcomingHolidaysList />
                 </div>
      </div>

    </div>
  );
}
