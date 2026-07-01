import { ModulePermission } from './permission.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

export interface CurrentUser {
  id: string;
  username: string;
  email?: string;
  roleId: string;
  role: string;
  permissions: ModulePermission[];
  isActive?: boolean;
  forcePasswordChange?: boolean;
  siteAccess: string[];
}
