import * as React from "react";
import type { JSXElement } from "@fluentui/react-components";
import {
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Button,
  useId,
} from "@fluentui/react-components";

type ConfirmActionProps = {
  title: string;
  message: string;
  actionName: string;
  onAction: () => void;
  onCancel?: () => void;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  trigger?: React.ReactNode;
  destructive?: boolean;
};

export const ConfirmAction = ({
  title,
  message,
  actionName,
  onAction,
  onCancel,
  open,
  onOpenChange,
  trigger,
  destructive = false,
}: ConfirmActionProps): JSXElement => {
  const dialogId = useId("dialog-");

  const handleCancel = () => {
    onCancel?.();
    onOpenChange?.(false);
  };

  const handleAction = () => {
    onAction();
    onOpenChange?.(false);
  };

  const dialogBody = (
    <DialogSurface
      aria-labelledby={`${dialogId}-title`}
      aria-describedby={`${dialogId}-content`}
    >
      <DialogBody>
        <DialogTitle id={`${dialogId}-title`}>
          {title}
        </DialogTitle>
        <DialogContent id={`${dialogId}-content`}>
          {message ? `${message}. ` : ""}Are you sure you want to continue?
        </DialogContent>
        <DialogActions>
          {open === undefined ? (
            <DialogTrigger disableButtonEnhancement>
              <Button
                appearance={destructive ? "primary" : "primary"}
                onClick={handleAction}
              >
                {actionName}
              </Button>
            </DialogTrigger>
          ) : (
            <Button
              appearance={destructive ? "primary" : "primary"}
              onClick={handleAction}
            >
              {actionName}
            </Button>
          )}
          {open === undefined ? (
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary" onClick={handleCancel}>
                Cancel
              </Button>
            </DialogTrigger>
          ) : (
            <Button appearance="secondary" onClick={handleCancel}>
              Cancel
            </Button>
          )}
        </DialogActions>
      </DialogBody>
    </DialogSurface>
  );

  if (open !== undefined) {
    return (
      <Dialog open={open} onOpenChange={(_, data) => onOpenChange?.(data.open)}>
        {dialogBody}
      </Dialog>
    );
  }

  return (
    <Dialog>
      <DialogTrigger disableButtonEnhancement>
        {React.isValidElement(trigger) ? trigger : <Button>{actionName}</Button>}
      </DialogTrigger>
      {dialogBody}
    </Dialog>
  );
};
