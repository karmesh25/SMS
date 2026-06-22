import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should report not logged in initially', () => {
    expect(service.isLoggedIn()).toBeFalse();
  });

  it('hasRole returns false when no user', () => {
    expect(service.hasRole('Admin')).toBeFalse();
  });

  it('login stores user and token on success', () => {
    service.login({ username: 'admin', password: 'Admin@123' }).subscribe();
    const req = http.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({
      success: true,
      data: {
        token: 'jwt-token',
        refreshToken: 'refresh',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        user: { id: '1', username: 'admin', role: 'Admin', siteIds: [] }
      }
    });
    expect(service.isLoggedIn()).toBeTrue();
    expect(service.hasRole('Admin')).toBeTrue();
  });
});
