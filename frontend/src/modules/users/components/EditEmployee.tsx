import { useState } from 'react';
import type { JSXElement } from '@fluentui/react-components';
import { Button } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import type { UserListItem } from '@modules/users/types/user';
import { EmployeeFormDialog } from './EmployeeFormDialog';

interface EditEmployeeProps {
  employee: UserListItem;
  onUpdated?: () => void;
}

export function EditEmployee({ employee, onUpdated }: EditEmployeeProps): JSXElement {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        appearance="subtle"
        size="small"
        icon={<EditRegular />}
        aria-label={`Edit ${employee.displayName ?? employee.username}`}
        onClick={() => setOpen(true)}
      />
      <EmployeeFormDialog
        employee={employee}
        open={open}
        onOpenChange={setOpen}
        onSaved={onUpdated}
      />
    </>
  );
}
