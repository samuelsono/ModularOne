import type { JSXElement } from '@fluentui/react-components';
import { Button } from '@fluentui/react-components';
import { VehicleCarRegular } from '@fluentui/react-icons';
import { VehicleFormDialog } from './VehicleFormDialog';

interface CreateVehicleProps {
  onCreated?: () => void;
}

export const CreateVehicle = ({ onCreated }: CreateVehicleProps): JSXElement => (
  <VehicleFormDialog
    mode="create"
    trigger={<Button appearance="primary" icon={<VehicleCarRegular />}>New Vehicle</Button>}
    onSaved={onCreated}
  />
);
