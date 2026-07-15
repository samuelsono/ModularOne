import * as React from "react";
import type { ButtonProps, JSXElement } from "@fluentui/react-components";
import {
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
  OverlayDrawer,
  Button,
  useRestoreFocusSource,
  useRestoreFocusTarget,
} from "@fluentui/react-components";
import { Dismiss24Regular } from "@fluentui/react-icons";


export const InfoDrawer = ({ children, title, trigger }: { children?: React.ReactNode, title?: string, trigger?: JSXElement  }): JSXElement => {
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
            {title}
          </DrawerHeaderTitle>
        </DrawerHeader>

        <DrawerBody className="p-0!">
          {children}
        </DrawerBody>
      </OverlayDrawer>


     { trigger ? <div onClick={() => setIsOpen(!isOpen)}>{trigger}</div> :
      <Button
        {...restoreFocusTargetAttributes}
        appearance="primary"
        onClick={() => setIsOpen(!isOpen)}
      >
        Info
      </Button> }
    </div>
  );
};