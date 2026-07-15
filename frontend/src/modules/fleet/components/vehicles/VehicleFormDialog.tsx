import * as React from 'react';
import type { JSXElement, SelectTabData, SelectTabEvent, TabValue } from '@fluentui/react-components';
import {
  Button,
  Combobox,
  Dropdown,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Field,
  Input,
  makeStyles,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Tab,
  TabList,
} from '@fluentui/react-components';
import {
  DocumentRegular,
  LocationRegular,
  VehicleCarRegular,
  WrenchRegular,
} from '@fluentui/react-icons';
import { DatePicker } from '@fluentui/react-datepicker-compat';
import { ApiError } from '@platform/api/apiClient';
import { createVehicle, updateVehicle } from '@modules/fleet/services/vehicleService';
import { getDrivers } from '@modules/fleet/services/driverService';
import type { CreateVehicleRequest, Vehicle } from '@modules/fleet/types/vehicle';
import type { Driver } from '@modules/fleet/types/driver';
import { usePermissions } from '@platform/permissions/usePermissions';
import { formatDateOnlyForApi, parseDateOnly } from '@platform/utils/dateOnly';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'row',
    columnGap: '50px',
    paddingTop: '30px',
    minHeight: '40vh',
  },
});

const VEHICLE_TYPES = [
  'Motor Vehicle',
  'Light Delivery Vehicle',
  'Minibus',
  'Panel Van',
  'Heavy Motor Vehicle',
  'Motor Cycle',
  'Bus',
  'Semi-Trailer',
  'Trailer',
];

const FUEL_TYPES = ['Petrol', 'Diesel', 'Electric', 'Hybrid'];

interface VehicleFormDialogProps {
  mode: 'create' | 'edit';
  vehicle?: Vehicle;
  trigger?: React.ReactElement;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  onSaved?: () => void;
}

export function VehicleFormDialog({
  mode,
  vehicle,
  trigger,
  open: controlledOpen,
  onOpenChange,
  onSaved,
}: VehicleFormDialogProps): JSXElement {
  const styles = useStyles();
  const { canWriteSubmodule } = usePermissions();
  const canAssignDriver = canWriteSubmodule('fleet', 'vehicles');
  const isEdit = mode === 'edit';
  const [internalOpen, setInternalOpen] = React.useState(false);
  const open = controlledOpen ?? internalOpen;
  const setOpen = onOpenChange ?? setInternalOpen;

  const [selectedTab, setSelectedTab] = React.useState<TabValue>('vehicle');
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const [registrationNumber, setRegistrationNumber] = React.useState('');
  const [make, setMake] = React.useState('');
  const [model, setModel] = React.useState('');
  const [year, setYear] = React.useState('');
  const [colour, setColour] = React.useState('');
  const [vehicleType, setVehicleType] = React.useState('Light Delivery Vehicle');
  const [fuelType, setFuelType] = React.useState('Diesel');
  const [vin, setVin] = React.useState('');
  const [engineNumber, setEngineNumber] = React.useState('');
  const [tare, setTare] = React.useState('');
  const [gvm, setGvm] = React.useState('');
  const [registeredOwner, setRegisteredOwner] = React.useState('');
  const [licenceDiscExpiry, setLicenceDiscExpiry] = React.useState<Date | null | undefined>(undefined);
  const [assignedDriverId, setAssignedDriverId] = React.useState('');
  const [drivers, setDrivers] = React.useState<Driver[]>([]);

  const resetForm = React.useCallback(() => {
    setRegistrationNumber('');
    setMake('');
    setModel('');
    setYear('');
    setColour('');
    setVehicleType('Light Delivery Vehicle');
    setFuelType('Diesel');
    setVin('');
    setEngineNumber('');
    setTare('');
    setGvm('');
    setRegisteredOwner('');
    setLicenceDiscExpiry(undefined);
    setAssignedDriverId('');
    setSelectedTab('vehicle');
    setError(null);
  }, []);

  const populateFromVehicle = React.useCallback((source: Vehicle) => {
    setRegistrationNumber(source.registrationNumber);
    setMake(source.make);
    setModel(source.model);
    setYear(source.year ? `${source.year}` : '');
    setColour(source.colour);
    setVehicleType(source.vehicleType || 'Light Delivery Vehicle');
    setFuelType(source.fuelType || 'Diesel');
    setVin(source.vin);
    setEngineNumber(source.engineNumber);
    setTare(source.tare ? `${source.tare}` : '');
    setGvm(source.gvm ? `${source.gvm}` : '');
    setRegisteredOwner(source.registeredOwner);
    setLicenceDiscExpiry(parseDateOnly(source.licenceDiscExpiry));
    setAssignedDriverId(source.assignedDriverId ?? '');
    setError(null);
  }, []);

  React.useEffect(() => {
    if (!open || !canAssignDriver) {
      return;
    }

    void getDrivers()
      .then((response) => setDrivers(response.items))
      .catch(() => setDrivers([]));
  }, [open, canAssignDriver]);

  React.useEffect(() => {
    if (!open) {
      return;
    }

    if (isEdit && vehicle) {
      populateFromVehicle(vehicle);
      return;
    }

    if (!isEdit) {
      resetForm();
    }
  }, [open, isEdit, vehicle, populateFromVehicle, resetForm]);

  const buildRequest = (): CreateVehicleRequest => ({
    registrationNumber: registrationNumber.trim(),
    make: make.trim(),
    model: model.trim(),
    year: Number(year) || 0,
    colour: colour.trim(),
    vehicleType,
    fuelType,
    vin: vin.trim() || undefined,
    engineNumber: engineNumber.trim() || undefined,
    tare: Number(tare) || 0,
    gvm: Number(gvm) || 0,
    registeredOwner: registeredOwner.trim() || undefined,
    licenceDiscExpiry: licenceDiscExpiry
      ? formatDateOnlyForApi(licenceDiscExpiry)
      : null,
    assignedDriverId: assignedDriverId || null,
  });

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const request = buildRequest();
      if (isEdit) {
        if (!vehicle) {
          throw new Error('Vehicle is required for edit.');
        }
        await updateVehicle(vehicle.id, request);
      } else {
        await createVehicle(request);
      }

      if (!isEdit) {
        resetForm();
      }

      setOpen(false);
      onSaved?.();
    } catch (submitError) {
      const message = submitError instanceof ApiError
        ? submitError.message
        : isEdit
          ? 'Failed to update vehicle.'
          : 'Failed to create vehicle.';
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const dialog = (
    <DialogSurface aria-describedby={undefined} className="flex! min-w-[900px]! flex-col!">
      <form onSubmit={handleSubmit} className="px-6">
        <DialogBody>
          <DialogTitle>{isEdit ? 'Edit Vehicle' : 'Add New Vehicle'}</DialogTitle>
          {error && (
            <MessageBar intent="error">
              <MessageBarBody>{error}</MessageBarBody>
            </MessageBar>
          )}
          <DialogContent className={styles.content}>
            <TabList
              selectedValue={selectedTab}
              vertical
              onTabSelect={(_event: SelectTabEvent, data: SelectTabData) => setSelectedTab(data.value)}
            >
              <Tab icon={<VehicleCarRegular />} value="vehicle">Vehicle Details</Tab>
              <Tab icon={<WrenchRegular />} value="technical">Technical</Tab>
              <Tab icon={<DocumentRegular />} value="registration">Registration</Tab>
              <Tab icon={<LocationRegular />} value="telematics">Telematics</Tab>
            </TabList>
            <div className="w-full">
              {selectedTab === 'vehicle' && (
                <div className="mb-6 grid w-full grid-cols-2 gap-x-6 gap-y-3">
                  <Field label="Registration Number" required>
                    <Input
                      required
                      value={registrationNumber}
                      placeholder="e.g. CA 123-456"
                      onChange={(_, data) => setRegistrationNumber(data.value)}
                    />
                  </Field>
                  <Field label="Make" required>
                    <Input
                      required
                      value={make}
                      placeholder="e.g. Toyota"
                      onChange={(_, data) => setMake(data.value)}
                    />
                  </Field>
                  <Field label="Model" required>
                    <Input
                      required
                      value={model}
                      placeholder="e.g. Hilux 2.8 GD-6"
                      onChange={(_, data) => setModel(data.value)}
                    />
                  </Field>
                  <Field label="Model Year" required>
                    <Input
                      required
                      type="number"
                      value={year}
                      placeholder="e.g. 2022"
                      onChange={(_, data) => setYear(data.value)}
                    />
                  </Field>
                  <Field label="Colour" required>
                    <Input
                      required
                      value={colour}
                      placeholder="e.g. White"
                      onChange={(_, data) => setColour(data.value)}
                    />
                  </Field>
                  <Field label="Vehicle Type" required>
                    <Combobox
                      aria-label="Vehicle Type"
                      value={vehicleType}
                      onOptionSelect={(_, data) => setVehicleType(data.optionText ?? vehicleType)}
                    >
                      {VEHICLE_TYPES.map((type) => (
                        <Option key={type}>{type}</Option>
                      ))}
                    </Combobox>
                  </Field>
                  <Field label="Fuel Type" required>
                    <Combobox
                      aria-label="Fuel Type"
                      value={fuelType}
                      onOptionSelect={(_, data) => setFuelType(data.optionText ?? fuelType)}
                    >
                      {FUEL_TYPES.map((type) => (
                        <Option key={type}>{type}</Option>
                      ))}
                    </Combobox>
                  </Field>
                </div>
              )}

              {selectedTab === 'technical' && (
                <div className="mb-6 grid w-full grid-cols-2 gap-x-6 gap-y-3">
                  <Field label="VIN (Vehicle Identification Number)" className="col-span-2">
                    <Input
                      value={vin}
                      placeholder="17-character VIN"
                      onChange={(_, data) => setVin(data.value)}
                    />
                  </Field>
                  <Field label="Engine Number">
                    <Input
                      value={engineNumber}
                      onChange={(_, data) => setEngineNumber(data.value)}
                    />
                  </Field>
                  <Field label="Tare (kg)">
                    <Input
                      type="number"
                      value={tare}
                      placeholder="e.g. 1870"
                      onChange={(_, data) => setTare(data.value)}
                    />
                  </Field>
                  <Field label="GVM — Gross Vehicle Mass (kg)">
                    <Input
                      type="number"
                      value={gvm}
                      placeholder="e.g. 3200"
                      onChange={(_, data) => setGvm(data.value)}
                    />
                  </Field>
                </div>
              )}

              {selectedTab === 'registration' && (
                <div className="mb-6 grid w-full grid-cols-2 gap-x-6 gap-y-3">
                  <Field label="Registered Owner" className="col-span-2">
                    <Input
                      value={registeredOwner}
                      onChange={(_, data) => setRegisteredOwner(data.value)}
                    />
                  </Field>
                  <Field label="Licence Disc Expiry">
                    <DatePicker
                      placeholder="Select a date..."
                      value={licenceDiscExpiry}
                      onSelectDate={setLicenceDiscExpiry}
                    />
                  </Field>
                  {canAssignDriver && (
                    <Field label="Assigned driver" className="col-span-2">
                      <Dropdown
                        placeholder="Link vehicle to a driver"
                        selectedOptions={assignedDriverId ? [assignedDriverId] : []}
                        value={
                          drivers.find((driver) => driver.id === assignedDriverId)?.name
                          ?? vehicle?.assignedDriverName
                          ?? ''
                        }
                        onOptionSelect={(_, data) => setAssignedDriverId(data.optionValue ?? '')}
                      >
                        <Option value="">No assigned driver</Option>
                        {drivers.map((driver) => (
                          <Option key={driver.id} value={driver.id} text={driver.name}>
                            {driver.name} ({driver.driverId})
                          </Option>
                        ))}
                      </Dropdown>
                    </Field>
                  )}
                </div>
              )}

              {selectedTab === 'telematics' && (
                <div className="py-4 text-sm text-neutral-foreground-3">
                  Telematics fields are synced from CarTrack when the vehicle is linked to a tracker device.
                </div>
              )}
            </div>
          </DialogContent>
          <DialogActions className="mt-auto">
            <Button
              appearance="secondary"
              disabled={isSubmitting}
              onClick={() => setOpen(false)}
            >
              Close
            </Button>
            <Button
              type="submit"
              appearance="primary"
              disabled={isSubmitting || !registrationNumber.trim() || !make.trim() || !model.trim()}
              icon={isSubmitting ? <Spinner size="tiny" /> : undefined}
            >
              {isSubmitting ? 'Saving...' : isEdit ? 'Save changes' : 'Save'}
            </Button>
          </DialogActions>
        </DialogBody>
      </form>
    </DialogSurface>
  );

  if (trigger) {
    return (
      <Dialog
        modalType="modal"
        open={open}
        onOpenChange={(_, data) => {
          setOpen(data.open);
          if (!data.open && !isEdit) {
            resetForm();
          }
        }}
      >
        <DialogTrigger disableButtonEnhancement>{trigger}</DialogTrigger>
        {dialog}
      </Dialog>
    );
  }

  return (
    <Dialog
      modalType="modal"
      open={open}
      onOpenChange={(_, data) => {
        setOpen(data.open);
        if (!data.open && !isEdit) {
          resetForm();
        }
      }}
    >
      {dialog}
    </Dialog>
  );
}
