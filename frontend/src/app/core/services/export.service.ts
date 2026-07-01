import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { LoaderService } from './loader.service';
import { ToastService } from './toast.service';
import { FileDownloadOutcome, FileDownloadService } from './file-download.service';

export type ReportExportType =
  | 'all-entry'
  | 'balance-sheet'
  | 'till-date'
  | 'monthwise'
  | 'bank-statement'
  | 'sell-details'
  | 'installment';

@Injectable({ providedIn: 'root' })
export class ExportService {
  private readonly downloads = inject(FileDownloadService);
  private readonly loader = inject(LoaderService);
  private readonly toast = inject(ToastService);

  exportExcel(reportType: ReportExportType, filters: Record<string, string | number | boolean>): void {
    this.download('/export/excel', reportType, filters, 'xlsx');
  }

  exportPdf(reportType: ReportExportType, filters: Record<string, string | number | boolean>): void {
    this.download('/export/pdf', reportType, filters, 'pdf');
  }

  exportWord(reportType: ReportExportType, filters: Record<string, string | number | boolean>): void {
    this.download('/export/word', reportType, filters, 'docx');
  }

  private download(
    path: string,
    reportType: ReportExportType,
    filters: Record<string, string | number | boolean>,
    fallbackExt: string
  ): void {
    const params = { reportType, ...filters };
    this.loader.show();
    this.downloads.download(path, params).pipe(
      finalize(() => this.loader.hide())
    ).subscribe({
      next: (outcome) => this.handleOutcome(outcome, `${reportType}-${new Date().toISOString().slice(0, 10)}.${fallbackExt}`),
      error: (err) => this.toast.error(this.extractError(err))
    });
  }

  private handleOutcome(outcome: FileDownloadOutcome, fallbackFilename: string): void {
    if (outcome.mode === 'pendrive') {
      this.toast.success(outcome.message ?? `Saved to pendrive: ${outcome.savedPath ?? outcome.filename ?? 'export'}`);
      return;
    }

    if (outcome.blob) {
      this.downloads.saveToBrowser(outcome.blob, outcome.filename ?? fallbackFilename);
      this.toast.success('Export downloaded');
    }
  }

  private extractError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.error instanceof Blob) {
        return 'Export failed';
      }
      const body = err.error as { message?: string } | null;
      return body?.message ?? err.message ?? 'Export failed';
    }

    if (err instanceof Error) {
      return err.message;
    }

    return 'Export failed';
  }
}
