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
  role: string;
  isActive?: boolean;
  forcePasswordChange?: boolean;
  siteAccess: string[];
}
