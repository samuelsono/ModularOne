import { useState } from 'react';
import type { JSXElement } from '@fluentui/react-components';
import { Button } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import type { Driver } from '../../types/driver';
import { DriverFormDialog } from './DriverFormDialog';

interface EditDriverProps {
  driver: Driver;
  onUpdated?: () => void;
}

export function EditDriver({ driver, onUpdated }: EditDriverProps): JSXElement {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        appearance="subtle"
        size="small"
        icon={<EditRegular />}
        aria-label={`Edit ${driver.name}`}
        onClick={() => setOpen(true)}
      />
      <DriverFormDialog
        mode="edit"
        driver={driver}
        open={open}
        onOpenChange={setOpen}
        onSaved={onUpdated}
      />
    </>
  );
}
