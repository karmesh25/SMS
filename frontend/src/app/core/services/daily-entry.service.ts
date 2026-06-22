import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export interface DailyEntry {
  id: string;
  siteId: string;
  entryType: string;
  entryDate: string;
  mainLedgerId: string;
  mainLedgerName: string;
  subLedgerId: string;
  subLedgerName: string;
  flatNo?: string;
  amount: number;
  cashBank: string;
  description?: string;
}

export interface ProfitSummary {
  totalAavak: number;
  totalJavak: number;
  profit: number;
}

export interface BankBalance {
  bankAccountId: string;
  bankName: string;
  accountNo: string;
  cashBankLabel: string;
  openingBalance: number;
  balance: number;
}

export interface BalanceSummary {
  cashBalance: number;
  bankBalances: BankBalance[];
}

@Injectable({ providedIn: 'root' })
export class DailyEntryService {
  private readonly api = inject(ApiService);

  getList(params: Record<string, string | number | boolean>) {
    return this.api.get<{ items: DailyEntry[]; totalCount: number }>('/daily-entries', params);
  }

  create(body: unknown) {
    return this.api.post<DailyEntry>('/daily-entries', body);
  }

  update(id: string, body: unknown) {
    return this.api.put<DailyEntry>(`/daily-entries/${id}`, body);
  }

  delete(id: string) {
    return this.api.delete<unknown>(`/daily-entries/${id}`);
  }

  getProfit(siteId: string) {
    return this.api.get<ProfitSummary>(`/daily-entries/profit/${siteId}`);
  }

  getBalance(siteId: string) {
    return this.api.get<BalanceSummary>(`/daily-entries/balance/${siteId}`);
  }
}
