import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, CurrentUser } from '../models/api-response.model';

interface LoginRequest {
  username: string;
  password: string;
}

interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: CurrentUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private token: string | null = null;
  private refreshTokenValue: string | null = null;
  private readonly currentUserSignal = signal<CurrentUser | null>(null);

  readonly currentUser = this.currentUserSignal.asReadonly();

  constructor(private readonly http: HttpClient) {}

  login(credentials: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http
      .post<ApiResponse<LoginResponse>>(`${environment.apiUrl}/auth/login`, credentials)
      .pipe(
        tap((response) => {
          if (response.success && response.data) {
            this.token = response.data.token;
            this.refreshTokenValue = response.data.refreshToken;
            this.currentUserSignal.set(response.data.user);
          }
        })
      );
  }

  logout(): void {
    this.token = null;
    this.refreshTokenValue = null;
    this.currentUserSignal.set(null);
  }

  refreshToken(): Observable<ApiResponse<LoginResponse>> | null {
    if (!this.refreshTokenValue) {
      return null;
    }

    return this.http
      .post<ApiResponse<LoginResponse>>(`${environment.apiUrl}/auth/refresh`, {
        refreshToken: this.refreshTokenValue
      })
      .pipe(
        tap((response) => {
          if (response.success && response.data) {
            this.token = response.data.token;
            this.refreshTokenValue = response.data.refreshToken;
            this.currentUserSignal.set(response.data.user);
          }
        })
      );
  }

  isLoggedIn(): boolean {
    return !!this.token;
  }

  getToken(): string | null {
    return this.token;
  }

  getCurrentUser(): CurrentUser | null {
    return this.currentUserSignal();
  }

  hasRole(...roles: string[]): boolean {
    const user = this.currentUserSignal();
    return !!user && roles.some((role) => user.role.toLowerCase() === role.toLowerCase());
  }
}
