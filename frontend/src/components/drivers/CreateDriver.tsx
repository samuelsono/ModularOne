import type { JSXElement } from '@fluentui/react-components';
import { Button } from '@fluentui/react-components';
import { PersonAccountsRegular } from '@fluentui/react-icons';
import { DriverFormDialog } from './DriverFormDialog';

interface CreateDriverProps {
  onCreated?: () => void;
}

export const CreateDriver = ({ onCreated }: CreateDriverProps): JSXElement => (
  <DriverFormDialog
    mode="create"
    onSaved={onCreated}
    trigger={
      <Button appearance="primary" icon={<PersonAccountsRegular />}>
        New Driver
      </Button>
    }
  />
);
