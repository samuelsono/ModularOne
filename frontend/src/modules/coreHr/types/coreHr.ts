export interface AuditableEntity {

  createdAt?: string;

  createdByUserId?: string | null;

  createdByDisplayName?: string | null;

  updatedAt?: string | null;

  updatedByUserId?: string | null;

  updatedByDisplayName?: string | null;

}



export interface Company extends AuditableEntity {

  id: string;

  name: string;

  code: string;

  description: string | null;

  isActive: boolean;

  sortOrder: number;

}



export interface Department extends AuditableEntity {

  id: string;

  companyId: string;

  companyName: string;

  name: string;

  code: string;

  description: string | null;

  isActive: boolean;

  sortOrder: number;

}



export interface Position extends AuditableEntity {

  id: string;

  departmentId: string;

  departmentName: string;

  companyId: string;

  companyName: string;

  name: string;

  code: string;

  description: string | null;

  isActive: boolean;

  sortOrder: number;

}



export interface SaveCompanyRequest {

  name: string;

  code: string;

  description?: string | null;

  isActive: boolean;

  sortOrder: number;

}



export interface SaveDepartmentRequest {

  companyId: string;

  name: string;

  code: string;

  description?: string | null;

  isActive: boolean;

  sortOrder: number;

}



export interface SavePositionRequest {

  departmentId: string;

  name: string;

  code: string;

  description?: string | null;

  isActive: boolean;

  sortOrder: number;

}


