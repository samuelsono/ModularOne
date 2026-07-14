import * as React from 'react';
import type { JSXElement, SelectTabData, SelectTabEvent, TabValue } from '@fluentui/react-components';
import {
  Button,
  Checkbox,
  Combobox,
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
  BuildingRegular,
  CardUiRegular,
  ContactCardRegular,
  MapRegular,
} from '@fluentui/react-icons';
import { DatePicker } from '@fluentui/react-datepicker-compat';
import { ApiError } from '../../services/apiClient';
import { createDriver, getDriver, updateDriver } from '../../services/driverService';
import type { Driver, SaveDriverRequest } from '../../types/driver';
import { formatDateOnlyForApi, parseDateOnly } from '../../utils/dateOnly';
import { DriverLoginSection } from './DriverLoginSection';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'row',
    columnGap: '50px',
    paddingTop: '30px',
    minHeight: '40vh',
  },
});

const GENDERS = ['Male', 'Female', 'Other'];
const LICENCE_CODES = ['A', 'A1', 'B', 'C', 'C1', 'EB', 'EC', 'EC1'];
const PROVINCES = [
  'Eastern Cape',
  'Free State',
  'Gauteng',
  'KwaZulu-Natal',
  'Limpopo',
  'Mpumalanga',
  'Northern Cape',
  'North West',
  'Western Cape',
];
const EMPLOYMENT_TYPES = ['Full-time', 'Part-time', 'Contract', 'Temporary'];
const EMPLOYMENT_STATUSES = ['Active', 'Inactive', 'Suspended', 'On Leave'];

interface DriverFormDialogProps {
  mode: 'create' | 'edit';
  driver?: Driver;
  trigger?: React.ReactElement;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  onSaved?: () => void;
}

export function DriverFormDialog({
  mode,
  driver,
  trigger,
  open: controlledOpen,
  onOpenChange,
  onSaved,
}: DriverFormDialogProps): JSXElement {
  const styles = useStyles();
  const isEdit = mode === 'edit';
  const [internalOpen, setInternalOpen] = React.useState(false);
  const open = controlledOpen ?? internalOpen;
  const setOpen = onOpenChange ?? setInternalOpen;

  const [selectedTab, setSelectedTab] = React.useState<TabValue>('contact');
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const [firstName, setFirstName] = React.useState('');
  const [lastName, setLastName] = React.useState('');
  const [workEmail, setWorkEmail] = React.useState('');
  const [workPhone, setWorkPhone] = React.useState('');
  const [whatsappNumber, setWhatsappNumber] = React.useState('');
  const [gender, setGender] = React.useState('Male');
  const [licenceNumber, setLicenceNumber] = React.useState('');
  const [idNumber, setIdNumber] = React.useState('');
  const [licenceIssued, setLicenceIssued] = React.useState<Date | null | undefined>(undefined);
  const [licenceExpiry, setLicenceExpiry] = React.useState<Date | null | undefined>(undefined);
  const [licenceCode, setLicenceCode] = React.useState('B');
  const [hasPdp, setHasPdp] = React.useState(false);
  const [unitName, setUnitName] = React.useState('');
  const [streetNumber, setStreetNumber] = React.useState('');
  const [streetName, setStreetName] = React.useState('');
  const [suburb, setSuburb] = React.useState('');
  const [city, setCity] = React.useState('');
  const [province, setProvince] = React.useState('Gauteng');
  const [postalCode, setPostalCode] = React.useState('');
  const [employeeNumber, setEmployeeNumber] = React.useState('');
  const [jobTitle, setJobTitle] = React.useState('');
  const [department, setDepartment] = React.useState('');
  const [branch, setBranch] = React.useState('');
  const [employmentType, setEmploymentType] = React.useState('Full-time');
  const [employmentStatus, setEmploymentStatus] = React.useState('Active');
  const [workStartDate, setWorkStartDate] = React.useState<Date | null | undefined>(undefined);
  const [manager, setManager] = React.useState('');
  const [currentDriver, setCurrentDriver] = React.useState<Driver | undefined>(driver);

  const resetForm = React.useCallback(() => {
    setSelectedTab('contact');
    setFirstName('');
    setLastName('');
    setWorkEmail('');
    setWorkPhone('');
    setWhatsappNumber('');
    setGender('Male');
    setLicenceNumber('');
    setIdNumber('');
    setLicenceIssued(undefined);
    setLicenceExpiry(undefined);
    setLicenceCode('B');
    setHasPdp(false);
    setUnitName('');
    setStreetNumber('');
    setStreetName('');
    setSuburb('');
    setCity('');
    setProvince('Gauteng');
    setPostalCode('');
    setEmployeeNumber('');
    setJobTitle('');
    setDepartment('');
    setBranch('');
    setEmploymentType('Full-time');
    setEmploymentStatus('Active');
    setWorkStartDate(undefined);
    setManager('');
    setError(null);
  }, []);

  const populateFromDriver = React.useCallback((value: Driver) => {
    setSelectedTab('contact');
    setFirstName(value.firstName);
    setLastName(value.lastName);
    setWorkEmail(value.workEmail);
    setWorkPhone(value.workPhone);
    setWhatsappNumber(value.whatsappNumber ?? '');
    setGender(value.gender || 'Other');
    setLicenceNumber(value.licenceNumber);
    setIdNumber(value.idNumber);
    setLicenceIssued(parseDateOnly(value.licenceIssued));
    setLicenceExpiry(parseDateOnly(value.licenceExpiry));
    setLicenceCode(value.licenceCode ?? 'B');
    setHasPdp(value.hasPdp);
    setUnitName(value.unitName ?? '');
    setStreetNumber(value.streetNumber ?? '');
    setStreetName(value.streetName ?? '');
    setSuburb(value.suburb ?? '');
    setCity(value.city ?? '');
    setProvince(value.province ?? 'Gauteng');
    setPostalCode(value.postalCode ?? '');
    setEmployeeNumber(value.employeeNumber ?? '');
    setJobTitle(value.jobTitle ?? '');
    setDepartment(value.department ?? '');
    setBranch(value.branch ?? '');
    setEmploymentType(value.employmentType || 'Full-time');
    setEmploymentStatus(value.employmentStatus || 'Active');
    setWorkStartDate(parseDateOnly(value.workStartDate));
    setManager(value.manager ?? '');
    setError(null);
  }, []);

  const refreshDriver = React.useCallback(async () => {
    if (!driver?.id) {
      return;
    }

    try {
      const updated = await getDriver(driver.id);
      setCurrentDriver(updated);
      populateFromDriver(updated);
      onSaved?.();
    } catch {
      onSaved?.();
    }
  }, [driver?.id, onSaved, populateFromDriver]);

  React.useEffect(() => {
    if (!open) {
      return;
    }

    if (isEdit && driver) {
      setCurrentDriver(driver);
      populateFromDriver(driver);
      return;
    }

    if (!isEdit) {
      resetForm();
    }
  }, [open, isEdit, driver, populateFromDriver, resetForm]);

  const buildRequest = (): SaveDriverRequest => ({
    firstName: firstName.trim(),
    lastName: lastName.trim(),
    workEmail: workEmail.trim(),
    workPhone: workPhone.trim(),
    whatsappNumber: whatsappNumber.trim() || undefined,
    gender,
    licenceNumber: licenceNumber.trim(),
    idNumber: idNumber.trim(),
    licenceIssued: licenceIssued ? formatDateOnlyForApi(licenceIssued) : null,
    licenceExpiry: licenceExpiry ? formatDateOnlyForApi(licenceExpiry) : null,
    licenceCode,
    hasPdp,
    unitName: unitName.trim() || undefined,
    streetNumber: streetNumber.trim() || undefined,
    streetName: streetName.trim() || undefined,
    suburb: suburb.trim() || undefined,
    city: city.trim() || undefined,
    province,
    postalCode: postalCode.trim() || undefined,
    employeeNumber: employeeNumber.trim() || undefined,
    jobTitle: jobTitle.trim() || undefined,
    department: department.trim() || undefined,
    branch: branch.trim() || undefined,
    employmentType,
    employmentStatus,
    workStartDate: workStartDate ? formatDateOnlyForApi(workStartDate) : null,
    manager: manager.trim() || undefined,
  });

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const request = buildRequest();
      if (isEdit && driver) {
        await updateDriver(driver.id, request);
      } else {
        await createDriver(request);
      }

      setOpen(false);
      if (!isEdit) {
        resetForm();
      }
      onSaved?.();
    } catch (submitError) {
      const message = submitError instanceof ApiError
        ? submitError.message
        : `Failed to ${isEdit ? 'update' : 'create'} driver.`;
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  const dialog = (
    <DialogSurface aria-describedby={undefined} className="flex! flex-col! min-w-[900px]!">
      <form onSubmit={(event) => void handleSubmit(event)} className="px-6">
        <DialogBody>
          <DialogTitle>{isEdit ? `Edit Driver — ${driver?.driverId ?? ''}` : 'Create New Driver'}</DialogTitle>
          {error && (
            <MessageBar intent="error" className="mb-3">
              <MessageBarBody>{error}</MessageBarBody>
            </MessageBar>
          )}
          <DialogContent className={styles.content}>
            <TabList selectedValue={selectedTab} vertical onTabSelect={(_event: SelectTabEvent, data: SelectTabData) => setSelectedTab(data.value)}>
              <Tab icon={<ContactCardRegular />} value="contact">Contact information</Tab>
              <Tab icon={<CardUiRegular />} value="license">License</Tab>
              <Tab icon={<MapRegular />} value="address">Address</Tab>
              <Tab icon={<BuildingRegular />} value="work">Work</Tab>
            </TabList>

            <div className="w-full">
              {selectedTab === 'contact' && (
                <div className="grid grid-cols-2 gap-x-6 gap-y-3 mb-10 w-full">
                  <Field label="Firstname" required>
                    <Input required value={firstName} onChange={(_, data) => setFirstName(data.value)} />
                  </Field>
                  <Field label="Lastname" required>
                    <Input required value={lastName} onChange={(_, data) => setLastName(data.value)} />
                  </Field>
                  <Field label="Work Email" required className="col-span-2">
                    <Input required type="email" value={workEmail} onChange={(_, data) => setWorkEmail(data.value)} />
                  </Field>
                  <Field label="Work Phone" required>
                    <Input required value={workPhone} onChange={(_, data) => setWorkPhone(data.value)} />
                  </Field>
                  <Field label="Whatsapp Number">
                    <Input value={whatsappNumber} onChange={(_, data) => setWhatsappNumber(data.value)} />
                  </Field>
                  <Field label="Gender">
                    <Combobox value={gender} onOptionSelect={(_, data) => setGender(data.optionValue ?? 'Other')}>
                      {GENDERS.map((option) => (
                        <Option key={option} value={option}>{option}</Option>
                      ))}
                    </Combobox>
                  </Field>
                </div>
              )}

              {selectedTab === 'license' && (
                <div className="grid grid-cols-2 gap-x-6 gap-y-3 mb-10 w-full">
                  <Field label="Licence Number" required>
                    <Input required value={licenceNumber} onChange={(_, data) => setLicenceNumber(data.value)} />
                  </Field>
                  <Field label="ID Number" required>
                    <Input required value={idNumber} onChange={(_, data) => setIdNumber(data.value)} />
                  </Field>
                  <Field label="Date of Issue" required>
                    <DatePicker
                      placeholder="Select a date..."
                      value={licenceIssued ?? undefined}
                      onSelectDate={setLicenceIssued}
                    />
                  </Field>
                  <Field label="Date of Expiry" required>
                    <DatePicker
                      placeholder="Select a date..."
                      value={licenceExpiry ?? undefined}
                      onSelectDate={setLicenceExpiry}
                    />
                  </Field>
                  <Field label="Licence Code" required>
                    <Combobox value={licenceCode} onOptionSelect={(_, data) => setLicenceCode(data.optionValue ?? 'B')}>
                      {LICENCE_CODES.map((option) => (
                        <Option key={option} value={option}>{option}</Option>
                      ))}
                    </Combobox>
                  </Field>
                  <Field label="Public Driver's Permit (PDP)">
                    <Checkbox
                      checked={hasPdp}
                      onChange={(_, data) => setHasPdp(Boolean(data.checked))}
                      label={hasPdp ? 'Has PDP' : 'Does not have PDP'}
                    />
                  </Field>
                </div>
              )}

              {selectedTab === 'address' && (
                <div className="grid grid-cols-2 gap-x-6 gap-y-3 mb-6 w-full">
                  <Field label="Unit / Complex Name" className="col-span-2">
                    <Input value={unitName} onChange={(_, data) => setUnitName(data.value)} placeholder="e.g. Sunrise Apartments, Unit 4B" />
                  </Field>
                  <Field label="Street Number" required>
                    <Input required value={streetNumber} onChange={(_, data) => setStreetNumber(data.value)} />
                  </Field>
                  <Field label="Street Name" required>
                    <Input required value={streetName} onChange={(_, data) => setStreetName(data.value)} />
                  </Field>
                  <Field label="Suburb" required>
                    <Input required value={suburb} onChange={(_, data) => setSuburb(data.value)} />
                  </Field>
                  <Field label="City / Town" required>
                    <Input required value={city} onChange={(_, data) => setCity(data.value)} />
                  </Field>
                  <Field label="Province" required>
                    <Combobox value={province} onOptionSelect={(_, data) => setProvince(data.optionValue ?? 'Gauteng')}>
                      {PROVINCES.map((option) => (
                        <Option key={option} value={option}>{option}</Option>
                      ))}
                    </Combobox>
                  </Field>
                  <Field label="Postal Code" required>
                    <Input required value={postalCode} onChange={(_, data) => setPostalCode(data.value)} />
                  </Field>
                </div>
              )}

              {selectedTab === 'work' && (
                <div className="grid grid-cols-2 gap-x-6 gap-y-3 mb-6 w-full">
                  <Field label="Employee / Staff Number">
                    <Input value={employeeNumber} onChange={(_, data) => setEmployeeNumber(data.value)} />
                  </Field>
                  <Field label="Job Title" required>
                    <Input required value={jobTitle} onChange={(_, data) => setJobTitle(data.value)} />
                  </Field>
                  <Field label="Department">
                    <Input value={department} onChange={(_, data) => setDepartment(data.value)} />
                  </Field>
                  <Field label="Branch / Location">
                    <Input value={branch} onChange={(_, data) => setBranch(data.value)} />
                  </Field>
                  <Field label="Employment Type">
                    <Combobox value={employmentType} onOptionSelect={(_, data) => setEmploymentType(data.optionValue ?? 'Full-time')}>
                      {EMPLOYMENT_TYPES.map((option) => (
                        <Option key={option} value={option}>{option}</Option>
                      ))}
                    </Combobox>
                  </Field>
                  <Field label="Employment Status">
                    <Combobox value={employmentStatus} onOptionSelect={(_, data) => setEmploymentStatus(data.optionValue ?? 'Active')}>
                      {EMPLOYMENT_STATUSES.map((option) => (
                        <Option key={option} value={option}>{option}</Option>
                      ))}
                    </Combobox>
                  </Field>
                  <Field label="Start Date">
                    <DatePicker
                      placeholder="Select a date..."
                      value={workStartDate ?? undefined}
                      onSelectDate={setWorkStartDate}
                    />
                  </Field>
                  <Field label="Manager / Supervisor">
                    <Input value={manager} onChange={(_, data) => setManager(data.value)} />
                  </Field>
                  {isEdit && currentDriver && (
                    <DriverLoginSection driver={currentDriver} onChanged={() => void refreshDriver()} />
                  )}
                </div>
              )}
            </div>
          </DialogContent>

          <DialogActions className="mt-auto">
            <Button appearance="secondary" disabled={isSubmitting} onClick={() => setOpen(false)}>
              Close
            </Button>
            <Button
              type="submit"
              appearance="primary"
              disabled={isSubmitting}
              icon={isSubmitting ? <Spinner size="tiny" /> : undefined}
            >
              {isSubmitting ? 'Saving...' : 'Save'}
            </Button>
          </DialogActions>
        </DialogBody>
      </form>
    </DialogSurface>
  );

  if (isEdit) {
    return (
      <Dialog modalType="modal" open={open} onOpenChange={(_, data) => setOpen(data.open)}>
        {dialog}
      </Dialog>
    );
  }

  return (
    <Dialog modalType="modal" open={open} onOpenChange={(_, data) => setOpen(data.open)}>
      {trigger && <DialogTrigger disableButtonEnhancement>{trigger}</DialogTrigger>}
      {dialog}
    </Dialog>
  );
}
