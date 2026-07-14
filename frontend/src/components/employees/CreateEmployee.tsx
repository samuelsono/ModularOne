import { useState } from 'react';
import type { JSXElement } from '@fluentui/react-components';
import { Button } from '@fluentui/react-components';
import { PersonAddRegular } from '@fluentui/react-icons';
import { usePermissions } from '../../hooks/usePermissions';
import { EmployeeFormDialog } from './EmployeeFormDialog';

interface CreateEmployeeProps {
  onCreated?: () => void;
}

export function CreateEmployee({ onCreated }: CreateEmployeeProps): JSXElement | null {
  const { canEditUsers } = usePermissions();
  const [open, setOpen] = useState(false);

  if (!canEditUsers) {
    return null;
  }

  return (
    <>
      <Button appearance="primary" icon={<PersonAddRegular />} onClick={() => setOpen(true)}>
        Add employee
      </Button>
      <EmployeeFormDialog
        open={open}
        onOpenChange={setOpen}
        onSaved={() => {
          setOpen(false);
          onCreated?.();
        }}
      />
    </>
  );
}
