import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface UserNotification {
  id: string;
  siteId: string;
  module: string;
  entityType: string;
  entityId: string;
  title: string;
  message: string;
  oldValues?: string | null;
  newValues?: string | null;
  isRead: boolean;
  createdAt: string;
  actorUsername?: string | null;
  canRestore?: boolean;
  resolvedAt?: string | null;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);

  getList(siteId?: string | null, take = 40): Observable<{ success: boolean; data: UserNotification[] }> {
    const params: Record<string, string | number> = { take };
    if (siteId) params['siteId'] = siteId;
    return this.api.get<UserNotification[]>('/notifications', params);
  }

  getUnreadCount(siteId?: string | null): Observable<{ success: boolean; data: number }> {
    const params: Record<string, string> = {};
    if (siteId) params['siteId'] = siteId;
    return this.api.get<number>('/notifications/unread-count', params);
  }

  markRead(id: string): Observable<{ success: boolean }> {
    return this.api.post(`/notifications/${id}/read`, {});
  }

  markAllRead(siteId?: string | null): Observable<{ success: boolean }> {
    const qs = siteId ? `?siteId=${encodeURIComponent(siteId)}` : '';
    return this.api.post(`/notifications/read-all${qs}`, {});
  }

  restore(id: string): Observable<{
    success: boolean;
    data: { restored: boolean; alreadyActive: boolean; message: string; entityId: string; entityType: string };
    message?: string;
  }> {
    return this.api.post(`/notifications/${id}/restore`, {});
  }
}
