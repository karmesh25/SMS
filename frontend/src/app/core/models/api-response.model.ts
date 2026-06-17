export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

export interface CurrentUser {
  id: string;
  username: string;
  role: string;
  siteAccess: string[];
}
