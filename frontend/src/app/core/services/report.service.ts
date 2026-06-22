import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export interface PagedReport<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly api = inject(ApiService);

  getAllEntry(params: Record<string, string | number | boolean>) {
    return this.api.get<PagedReport<unknown>>('/reports/all-entry', params);
  }

  getBalanceSheet(params: Record<string, string | number | boolean>) {
    return this.api.get<unknown>('/reports/balance-sheet', params);
  }

  getTillDate(params: Record<string, string | number | boolean>) {
    return this.api.get<PagedReport<unknown>>('/reports/till-date', params);
  }

  getMonthwise(params: Record<string, string | number | boolean>) {
    return this.api.get<unknown[]>('/reports/monthwise', params);
  }

  getBankStatement(params: Record<string, string | number | boolean>) {
    return this.api.get<unknown>('/reports/bank-statement', params);
  }

  getSellDetails(params: Record<string, string | number | boolean>) {
    return this.api.get<unknown>('/reports/sell-details', params);
  }

  getInstallment(params: Record<string, string | number | boolean>) {
    return this.api.get<unknown[]>('/reports/installment', params);
  }
}
