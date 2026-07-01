import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, from, map, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface FileDownloadOutcome {
  mode: 'browser' | 'pendrive';
  blob?: Blob;
  filename?: string;
  savedPath?: string;
  message?: string;
}

interface ExportSavedPayload {
  fileName: string;
  savedPath: string;
}

@Injectable({ providedIn: 'root' })
export class FileDownloadService {
  private readonly http = inject(HttpClient);

  download(path: string, params?: Record<string, string | number | boolean>): Observable<FileDownloadOutcome> {
    return this.http.get(`${environment.apiUrl}${path}`, {
      params: this.buildParams(params),
      responseType: 'blob',
      observe: 'response'
    }).pipe(
      switchMap(response => this.toOutcome(response))
    );
  }

  saveToBrowser(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  private toOutcome(response: HttpResponse<Blob>): Observable<FileDownloadOutcome> {
    const contentType = response.headers.get('Content-Type') ?? '';
    if (contentType.includes('application/json')) {
      return from((response.body ?? new Blob()).text()).pipe(
        map(text => {
          const parsed = JSON.parse(text) as ApiResponse<ExportSavedPayload>;
          return {
            mode: 'pendrive' as const,
            filename: parsed.data?.fileName,
            savedPath: parsed.data?.savedPath,
            message: parsed.message
          };
        })
      );
    }

    const filename = this.extractFilename(response.headers.get('Content-Disposition'))
      ?? `download-${new Date().toISOString().slice(0, 10)}`;

    return new Observable(observer => {
      observer.next({
        mode: 'browser',
        blob: response.body ?? new Blob(),
        filename
      });
      observer.complete();
    });
  }

  private extractFilename(contentDisposition: string | null): string | null {
    if (!contentDisposition) return null;
    const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(contentDisposition);
    return match?.[1]?.trim() ?? null;
  }

  private buildParams(params?: Record<string, string | number | boolean>): HttpParams | undefined {
    if (!params) return undefined;

    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      httpParams = httpParams.set(key, String(value));
    });
    return httpParams;
  }
}
