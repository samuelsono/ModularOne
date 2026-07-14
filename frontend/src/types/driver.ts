export interface Driver {
  id: string;
  driverId: string;
  name: string;
  email: string;
  contactNumber: string;
  gender: string;
  department: string;
  licenceNumber: string;
  licenceIssued: string | null;
  licenceExpiry: string | null;
  firstName: string;
  lastName: string;
  workEmail: string;
  workPhone: string;
  whatsappNumber: string | null;
  idNumber: string;
  licenceCode: string | null;
  hasPdp: boolean;
  unitName: string | null;
  streetNumber: string | null;
  streetName: string | null;
  suburb: string | null;
  city: string | null;
  province: string | null;
  postalCode: string | null;
  employeeNumber: string | null;
  jobTitle: string | null;
  branch: string | null;
  employmentType: string;
  employmentStatus: string;
  workStartDate: string | null;
  manager: string | null;
  createdAt?: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
  linkedUser?: LinkedUser | null;
}

export interface LinkedUser {
  userId: string;
  username: string;
  email: string;
  displayName: string | null;
  isActive: boolean;
}

export interface DriversResponse {
  items: Driver[];
  total: number;
}

export interface SaveDriverRequest {
  firstName: string;
  lastName: string;
  workEmail: string;
  workPhone: string;
  whatsappNumber?: string;
  gender?: string;
  licenceNumber: string;
  idNumber: string;
  licenceIssued?: string | null;
  licenceExpiry?: string | null;
  licenceCode?: string;
  hasPdp: boolean;
  unitName?: string;
  streetNumber?: string;
  streetName?: string;
  suburb?: string;
  city?: string;
  province?: string;
  postalCode?: string;
  employeeNumber?: string;
  jobTitle?: string;
  department?: string;
  branch?: string;
  employmentType?: string;
  employmentStatus?: string;
  workStartDate?: string | null;
  manager?: string;
}
