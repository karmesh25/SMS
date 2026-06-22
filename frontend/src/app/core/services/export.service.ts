import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, finalize, switchMap } from 'rxjs';
import { ApiService } from './api.service';
import { LoaderService } from './loader.service';
import { ToastService } from './toast.service';

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
  private readonly api = inject(ApiService);
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
    this.api.downloadBlob(path, params).pipe(
      switchMap(blob => this.ensureBlob(blob)),
      finalize(() => this.loader.hide())
    ).subscribe({
      next: (blob) => {
        const filename = `${reportType}-${new Date().toISOString().slice(0, 10)}.${fallbackExt}`;
        this.triggerDownload(blob, filename);
        this.toast.success('Export downloaded');
      },
      error: (err) => {
        if (err instanceof HttpErrorResponse && err.error instanceof Blob) {
          err.error.text().then(text => {
            try {
              const parsed = JSON.parse(text) as { message?: string };
              this.toast.error(parsed.message ?? 'Export failed');
            } catch {
              this.toast.error('Export failed');
            }
          });
          return;
        }
        this.toast.error(this.extractError(err));
      }
    });
  }

  private ensureBlob(blob: Blob): Observable<Blob> {
    if (blob.type !== 'application/json') {
      return new Observable(observer => {
        observer.next(blob);
        observer.complete();
      });
    }

    return new Observable(observer => {
      blob.text().then(text => {
        try {
          const parsed = JSON.parse(text) as { message?: string };
          observer.error(new Error(parsed.message ?? 'Export failed'));
        } catch {
          observer.error(new Error('Export failed'));
        }
      }).catch(() => observer.error(new Error('Export failed')));
    });
  }

  private triggerDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
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
