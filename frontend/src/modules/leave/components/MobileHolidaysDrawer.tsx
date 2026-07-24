import * as React from "react";
import type { JSXElement } from "@fluentui/react-components";
import { tokens } from "@fluentui/react-components";
import {
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
  OverlayDrawer,
  Button,
  useRestoreFocusSource,
  useRestoreFocusTarget,
} from "@fluentui/react-components";
import { CalendarRegular, Dismiss24Regular } from "@fluentui/react-icons";
import { UpcomingHolidaysList } from '@modules/leave/components/UpcomingHolidaysList';

import {
  Calendar,
} from "@fluentui/react-calendar-compat";

export const MobileHolidaysDrawer = (): JSXElement => {
  const [isOpen, setIsOpen] = React.useState(false);

  // all Drawers need manual focus restoration attributes
  // unless (as in the case of some inline drawers, you do not want automatic focus restoration)
  const restoreFocusTargetAttributes = useRestoreFocusTarget();
  const restoreFocusSourceAttributes = useRestoreFocusSource();

  return (
    <div>
      <OverlayDrawer
        modalType="non-modal"
        {...restoreFocusSourceAttributes}
        open={isOpen}
        position="end"
        onOpenChange={(_, { open }) => setIsOpen(open)}
      >
        <DrawerHeader>
          <DrawerHeaderTitle
            action={
              <Button
                appearance="subtle"
                aria-label="Close"
                icon={<Dismiss24Regular />}
                onClick={() => setIsOpen(false)}
              />
            }
          >
            Holidays
          </DrawerHeaderTitle>
        </DrawerHeader>

        <DrawerBody className="p-2!">
          <div className="rounded mb-3 flex flex-col items-center" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
                              <Calendar 
                                  highlightSelectedMonth
                                  showMonthPickerAsOverlay
                                  showGoToToday={false}
                                  className="w-full"  
                              />
          
                            </div>
                            
                            <UpcomingHolidaysList />
        </DrawerBody>
      </OverlayDrawer>

      <Button
        {...restoreFocusTargetAttributes}
        appearance="primary"
        onClick={() => setIsOpen(!isOpen)}
        icon={<CalendarRegular />}
      >
        
      </Button>
    </div>
  );
};