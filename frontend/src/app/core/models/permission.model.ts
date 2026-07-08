export interface ModulePermission {
  moduleKey: string;
  canView: boolean;
  canManage: boolean;
}

export type PermissionLevel = 'view' | 'manage';

export const APP_MODULES: { key: string; label: string }[] = [
  { key: 'dashboard', label: 'Dashboard' },
  { key: 'sites', label: 'Sites' },
  { key: 'wings', label: 'Wings' },
  { key: 'conditions', label: 'Conditions' },
  { key: 'ledgers', label: 'Ledgers' },
  { key: 'banks', label: 'Banks' },
  { key: 'brokers', label: 'Brokers' },
  { key: 'users', label: 'Users' },
  { key: 'devices', label: 'Devices' },
  { key: 'booking', label: 'Booking' },
  { key: 'daily_entry', label: 'Daily Entry' },
  { key: 'journal_voucher', label: 'Journal Voucher' },
  { key: 'dastavej', label: 'Dastavej' },
  { key: 'vyaj', label: 'Vyaj Khata' },
  { key: 'reports', label: 'Reports' }
];

export interface AppRole {
  id: string;
  name: string;
  description?: string;
  isSystem: boolean;
  userCount: number;
  permissions: ModulePermission[];
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
  permissions: ModulePermission[];
}

export interface UpdateRoleRequest {
  name: string;
  description?: string;
  permissions: ModulePermission[];
}
