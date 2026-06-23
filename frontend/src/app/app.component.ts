import { Component, computed, effect, inject, signal } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { LoaderComponent } from './shared/components/loader/loader.component';
import { BreadcrumbComponent } from './shared/components/breadcrumb/breadcrumb.component';
import { HasRoleDirective } from './shared/directives/has-role.directive';
import { AuthService } from './core/services/auth.service';
import { SiteContextService } from './core/services/site-context.service';

export interface NavLink {
  label: string;
  route: string;
  icon: string;
  roles: string[];
  exact?: boolean;
}

export interface NavSection {
  title: string;
  roles: string[];
  items: NavLink[];
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatDividerModule,
    LoaderComponent,
    BreadcrumbComponent,
    HasRoleDirective
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly siteContext = inject(SiteContextService);
  readonly currentUser = this.authService.currentUser;
  readonly mobileMenuOpen = signal(false);

  readonly isTabletDown = toSignal(
    this.breakpointObserver.observe('(max-width: 959px)').pipe(map((r) => r.matches)),
    { initialValue: false }
  );

  /** Small phones (≤599px) */
  readonly isHandset = toSignal(
    this.breakpointObserver.observe('(max-width: 599px)').pipe(map((r) => r.matches)),
    { initialValue: false }
  );

  readonly sidenavMode = computed(() => (this.isTabletDown() ? 'over' : 'side') as 'over' | 'side');
  readonly sidenavOpened = computed(() => !this.isTabletDown() || this.mobileMenuOpen());

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event) => (event as NavigationEnd).urlAfterRedirects),
      startWith(this.router.url)
    ),
    { initialValue: this.router.url }
  );

  readonly showShell = computed(() => {
    const url = this.currentUrl();
    const isPublicRoute =
      url.startsWith('/login') ||
      url.startsWith('/unauthorized-device') ||
      url.startsWith('/forbidden') ||
      url.startsWith('/not-found') ||
      url.startsWith('/server-error');
    return !isPublicRoute && this.authService.isLoggedIn();
  });

  readonly navSections: NavSection[] = [
    {
      title: 'Site Admin',
      roles: ['SuperAdmin', 'Admin'],
      items: [
        { label: 'Sites', route: '/admin/sites', icon: 'location_city', roles: ['SuperAdmin', 'Admin'] },
        { label: 'Wings', route: '/admin/wings', icon: 'domain', roles: ['SuperAdmin', 'Admin'] },
        { label: 'Conditions', route: '/admin/conditions', icon: 'rule', roles: ['SuperAdmin', 'Admin'] },
        { label: 'Ledgers', route: '/admin/ledgers', icon: 'menu_book', roles: ['SuperAdmin', 'Admin'] },
        { label: 'Banks', route: '/admin/banks', icon: 'account_balance', roles: ['SuperAdmin', 'Admin'] },
        { label: 'Brokers', route: '/admin/brokers', icon: 'handshake', roles: ['SuperAdmin', 'Admin'] },
        { label: 'Users', route: '/admin/users', icon: 'people', roles: ['SuperAdmin'] },
        { label: 'Devices', route: '/admin/devices', icon: 'phonelink_lock', roles: ['SuperAdmin'] }
      ]
    },
    {
      title: 'Booking',
      roles: ['SuperAdmin', 'Admin', 'OfficeStaff'],
      items: [
        { label: 'Flat Grid', route: '/booking', icon: 'grid_view', roles: ['SuperAdmin', 'Admin', 'OfficeStaff'], exact: true }
      ]
    },
    {
      title: 'Accounting',
      roles: ['SuperAdmin', 'Admin', 'OfficeStaff', 'ViewOnly'],
      items: [
        { label: 'Daily Entry', route: '/accounting', icon: 'receipt', roles: ['SuperAdmin', 'Admin', 'OfficeStaff'], exact: true },
        { label: 'Dastavej', route: '/accounting/dastavej', icon: 'description', roles: ['SuperAdmin', 'Admin', 'OfficeStaff'] },
        { label: 'Vyaj Khata', route: '/vyaj', icon: 'menu_book', roles: ['SuperAdmin', 'Admin', 'OfficeStaff', 'ViewOnly'], exact: true }
      ]
    },
    {
      title: 'Reports',
      roles: ['SuperAdmin', 'Admin', 'OfficeStaff', 'ViewOnly'],
      items: [
        { label: 'All Reports', route: '/reports', icon: 'assessment', roles: ['SuperAdmin', 'Admin', 'OfficeStaff', 'ViewOnly'], exact: true }
      ]
    }
  ];

  readonly dashboardLink: NavLink = {
    label: 'Dashboard',
    route: '/dashboard',
    icon: 'dashboard',
    roles: ['SuperAdmin', 'Admin', 'OfficeStaff', 'ViewOnly'],
    exact: true
  };

  constructor() {
    effect(() => {
      if (this.authService.authReady()) {
        this.siteContext.loadSites().subscribe();
      }
    });
  }

  onSiteChange(siteId: string): void {
    this.siteContext.setActiveSite(siteId);
  }

  toggleSidenav(): void {
    this.mobileMenuOpen.update((v) => !v);
  }

  closeSidenavOnNavigate(): void {
    if (this.isTabletDown()) {
      this.mobileMenuOpen.set(false);
    }
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => void this.router.navigate(['/login']),
      error: () => void this.router.navigate(['/login'])
    });
  }
}
