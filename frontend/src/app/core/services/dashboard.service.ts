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

export interface DashboardVyajParty {
  id: string;
  name: string;
  vyajDue: number;
  principalDue: number;
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
  totalVyajDue: number;
  totalVyajPrincipalDue: number;
  wingSummary: WingSummary[];
  recentEntries: RecentEntry[];
  vyajParties: DashboardVyajParty[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly api = inject(ApiService);

  getSummary(siteId: string): Observable<{ success: boolean; data: DashboardSummary }> {
    return this.api.get<DashboardSummary>('/dashboard/summary', { siteId });
  }
}
