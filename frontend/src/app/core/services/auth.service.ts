import { Injectable, signal } from '@angular/core';

import { HttpClient } from '@angular/common/http';

import { Observable, tap, firstValueFrom, finalize } from 'rxjs';

import { environment } from '../../../environments/environment';

import { ApiResponse, CurrentUser } from '../models/api-response.model';
import { PermissionLevel } from '../models/permission.model';



interface LoginRequest {

  username: string;

  password: string;

}



interface LoginResponse {

  token?: string;

  refreshToken?: string;

  expiresAt?: string;

  user?: CurrentUser;

  Token?: string;

  RefreshToken?: string;

  ExpiresAt?: string;

  User?: CurrentUser;

}



@Injectable({ providedIn: 'root' })

export class AuthService {

  private token: string | null = null;

  private refreshTokenValue: string | null = null;

  private tokenExpiresAt: Date | null = null;

  private refreshTimer: ReturnType<typeof setTimeout> | null = null;



  private readonly currentUserSignal = signal<CurrentUser | null>(null);

  private readonly authReadySignal = signal(false);



  readonly currentUser = this.currentUserSignal.asReadonly();

  readonly authReady = this.authReadySignal.asReadonly();



  constructor(private readonly http: HttpClient) {}



  login(credentials: LoginRequest): Observable<ApiResponse<LoginResponse>> {

    return this.http

      .post<ApiResponse<LoginResponse>>(`${environment.apiUrl}/auth/login`, credentials)

      .pipe(tap((response) => this.handleAuthResponse(response)));

  }



  logout(): Observable<ApiResponse<unknown>> {

    return this.http.post<ApiResponse<unknown>>(`${environment.apiUrl}/auth/logout`, {}).pipe(

      finalize(() => this.clearSession())

    );

  }



  refreshToken(): Observable<ApiResponse<LoginResponse>> {

    return this.http

      .post<ApiResponse<LoginResponse>>(`${environment.apiUrl}/auth/refresh`, {

        refreshToken: this.refreshTokenValue

      })

      .pipe(tap((response) => this.handleAuthResponse(response)));

  }



  async tryRefreshToken(): Promise<boolean> {

    if (!this.refreshTokenValue) {

      return false;

    }



    try {

      const response = await firstValueFrom(this.refreshToken());

      return !!response.success;

    } catch {

      this.clearSession();

      return false;

    }

  }



  isLoggedIn(): boolean {

    return !!this.getToken() && (!this.tokenExpiresAt || this.tokenExpiresAt > new Date());

  }



  getToken(): string | null {

    const value = this.token?.trim();

    return value ? value : null;

  }



  hasRefreshToken(): boolean {

    return !!this.refreshTokenValue?.trim();

  }



  getCurrentUser(): CurrentUser | null {

    return this.currentUserSignal();

  }



  hasRole(...roles: string[]): boolean {

    const user = this.currentUserSignal();

    return !!user && roles.some((role) => user.role.toLowerCase() === role.toLowerCase());

  }



  isSuperAdmin(): boolean {

    return this.hasRole('SuperAdmin');

  }



  hasPermission(moduleKey: string, level: PermissionLevel = 'view'): boolean {

    const user = this.currentUserSignal();

    if (!user) {

      return false;

    }



    if (this.isSuperAdmin()) {

      return true;

    }



    const perm = user.permissions?.find((p) => p.moduleKey === moduleKey);

    if (!perm) {

      return false;

    }



    return level === 'manage' ? perm.canManage : perm.canView;

  }



  clearSession(): void {

    if (this.refreshTimer) {

      clearTimeout(this.refreshTimer);

      this.refreshTimer = null;

    }



    this.token = null;

    this.refreshTokenValue = null;

    this.tokenExpiresAt = null;

    this.currentUserSignal.set(null);

    this.authReadySignal.set(false);

  }



  private handleAuthResponse(response: ApiResponse<LoginResponse>): void {

    if (!response.success || !response.data) {

      return;

    }



    const data = response.data;

    const token = data.token ?? data.Token;

    const refreshToken = data.refreshToken ?? data.RefreshToken;

    const expiresAt = data.expiresAt ?? data.ExpiresAt;

    const user = data.user ?? data.User;



    if (!token?.trim() || !refreshToken?.trim() || !user) {

      return;

    }



    this.token = token.trim();

    this.refreshTokenValue = refreshToken.trim();

    this.tokenExpiresAt = expiresAt ? new Date(expiresAt) : null;

    this.currentUserSignal.set({

      id: user.id,

      username: user.username,

      email: user.email,

      roleId: user.roleId ?? (user as { roleId?: string }).roleId ?? '',

      role: user.role,

      permissions: user.permissions ?? [],

      isActive: user.isActive,

      forcePasswordChange: user.forcePasswordChange,

      siteAccess: (user.siteAccess as unknown as { siteName: string }[] | string[])?.map((s) =>

        typeof s === 'string' ? s : s.siteName

      ) ?? []

    });

    this.authReadySignal.set(true);

    this.scheduleSilentRefresh();

  }



  private scheduleSilentRefresh(): void {

    if (this.refreshTimer) {

      clearTimeout(this.refreshTimer);

    }



    if (!this.tokenExpiresAt) {

      return;

    }



    const refreshAt = this.tokenExpiresAt.getTime() - 15 * 60 * 1000;

    const delay = Math.max(refreshAt - Date.now(), 30_000);



    this.refreshTimer = setTimeout(() => {

      void this.tryRefreshToken();

    }, delay);

  }

}


