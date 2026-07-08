import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import { FileDownloadService } from './file-download.service';

export interface JournalVoucherLine {
  id?: string;
  subLedgerId: string;
  subLedgerName?: string;
  mainLedgerName?: string;
  entryType: 'dr' | 'cr';
  amount: number;
  lineNo: number;
}

export interface JournalVoucher {
  id: string;
  siteId: string;
  voucherNo: string;
  voucherDate: string;
  narration?: string;
  totalDebit: number;
  totalCredit: number;
  lines: JournalVoucherLine[];
}

export interface JournalVoucherFilter {
  siteId: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class JournalVoucherService {
  private readonly api = inject(ApiService);
  private readonly downloads = inject(FileDownloadService);

  getList(filter: JournalVoucherFilter) {
    return this.api.get<{ items: JournalVoucher[]; totalCount: number; page: number; pageSize: number }>(
      '/journal-vouchers',
      filter as unknown as Record<string, string | number | boolean>
    );
  }

  getById(id: string) {
    return this.api.get<JournalVoucher>(`/journal-vouchers/${id}`);
  }

  create(body: unknown) {
    return this.api.post<JournalVoucher>('/journal-vouchers', body);
  }

  update(id: string, body: unknown) {
    return this.api.put<JournalVoucher>(`/journal-vouchers/${id}`, body);
  }

  delete(id: string) {
    return this.api.delete<unknown>(`/journal-vouchers/${id}`);
  }

  exportLedgerExcel(siteId: string, params?: { dateFrom?: string; dateTo?: string }) {
    return this.downloads.download('/journal-vouchers/export/ledger-excel', { siteId, ...params });
  }

  exportLedgerPdf(siteId: string, params?: { dateFrom?: string; dateTo?: string }) {
    return this.downloads.download('/journal-vouchers/export/ledger-pdf', { siteId, ...params });
  }
}
