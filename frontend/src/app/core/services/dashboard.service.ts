import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface WingSummary {
  wingId: string;
  wingName: string;
  total: number;
  booked: number;
  available: number;
  bookingPercentage: number;
}

export interface RecentEntry {
  id: string;
  entryDate: string;
  entryType: string;
  mainLedgerName: string;
  subLedgerName?: string;
  amount: number;
}

export interface DashboardSummary {
  totalFlats: number;
  bookedFlats: number;
  availableFlats: number;
  cancelledFlats: number;
  bookingPercentage: number;
  totalAavak: number;
  totalJavak: number;
  netProfit: number;
  totalOutstanding: number;
  wingSummary: WingSummary[];
  recentEntries: RecentEntry[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly api = inject(ApiService);

  getSummary(siteId: string): Observable<{ success: boolean; data: DashboardSummary }> {
    return this.api.get<DashboardSummary>('/dashboard/summary', { siteId });
  }
}
