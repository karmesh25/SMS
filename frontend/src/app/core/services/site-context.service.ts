import { Injectable, signal } from '@angular/core';

import { HttpClient } from '@angular/common/http';

import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';

import { ApiResponse } from '../models/api-response.model';



export interface Site {

  id: string;

  siteName: string;

  startDate?: string;

  address?: string;

  isActive: boolean;

}



@Injectable({ providedIn: 'root' })

export class SiteContextService {

  private readonly activeSiteIdSignal = signal<string | null>(null);

  private readonly sitesSignal = signal<Site[]>([]);



  readonly activeSiteId = this.activeSiteIdSignal.asReadonly();

  readonly sites = this.sitesSignal.asReadonly();



  constructor(private readonly http: HttpClient) {}



  loadSites(): Observable<ApiResponse<Site[]>> {

    return this.http.get<ApiResponse<Site[]>>(`${environment.apiUrl}/sites`).pipe(

      tap((response) => {

        if (response.success && response.data.length > 0) {

          this.sitesSignal.set(response.data);

          const activeId = this.activeSiteIdSignal();

          const current = response.data.find((s) => s.id === activeId);

          if (!current || !current.isActive) {

            const firstActive = response.data.find((s) => s.isActive);

            this.activeSiteIdSignal.set(firstActive?.id ?? response.data[0].id);

          } else if (!activeId) {

            this.activeSiteIdSignal.set(response.data[0].id);

          }

        }

      })

    );

  }



  setActiveSite(siteId: string): void {

    this.activeSiteIdSignal.set(siteId);

  }

}

