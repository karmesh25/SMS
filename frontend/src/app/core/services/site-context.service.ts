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

  isSandbox?: boolean;

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
          const defaultSite = response.data.find((s) => s.isSandbox && s.isActive)
            ?? response.data.find((s) => s.isActive)
            ?? response.data[0];

          if (!current || !current.isActive) {

            this.activeSiteIdSignal.set(defaultSite.id);

          } else if (!activeId) {

            this.activeSiteIdSignal.set(defaultSite.id);

          }

        }

      })

    );

  }



  setActiveSite(siteId: string): void {

    this.activeSiteIdSignal.set(siteId);

  }

}

