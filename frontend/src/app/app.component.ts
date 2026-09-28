import { Component, computed, effect, inject, OnDestroy, signal } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith, Subscription } from 'rxjs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { LoaderComponent } from './shared/components/loader/loader.component';
import { BreadcrumbComponent } from './shared/components/breadcrumb/breadcrumb.component';
import { HasPermissionDirective } from './shared/directives/has-permission.directive';
import { AuthService } from './core/services/auth.service';
import { SiteContextService } from './core/services/site-context.service';
import { ThemeService } from './core/services/theme.service';
import { ToastService } from './core/services/toast.service';
import { NotificationService, UserNotification } from './core/services/notification.service';

export interface NavLink {
  label: string;
  route: string;
  icon: string;
  moduleKey: string;
  exact?: boolean;
}

export interface NavSection {
  title: string;
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
    MatTooltipModule,
    MatMenuModule,
    MatBadgeModule,
    LoaderComponent,
    BreadcrumbComponent,
    HasPermissionDirective
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnDestroy {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly notificationService = inject(NotificationService);
  private readonly toast = inject(ToastService);

  readonly siteContext = inject(SiteContextService);
  readonly theme = inject(ThemeService);
  readonly currentUser = this.authService.currentUser;
  readonly mobileMenuOpen = signal(false);
  readonly notifications = signal<UserNotification[]>([]);
  readonly unreadCount = signal(0);

  private pollSub: Subscription | null = null;
  private pollTimer: ReturnType<typeof setInterval> | null = null;

  readonly isTabletDown = toSignal(
    this.breakpointObserver.observe('(max-width: 959px)').pipe(map((r) => r.matches)),
    { initialValue: false }
  );

  readonly isHandset = toSignal(
    this.breakpointObserver.observe('(max-width: 599px)').pipe(map((r) => r.matches)),
    { initialValue: false }
  );

  readonly sidenavMode = computed(() => (this.isTabletDown() ? 'over' : 'side') as 'over' | 'side');
  readonly sidenavOpened = computed(() => !this.isTabletDown() || this.mobileMenuOpen());

  readonly showNotifications = computed(() =>
    this.authService.hasRole('Admin', 'SuperAdmin') && this.authService.isLoggedIn()
  );

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
      url.startsWith('/license-expired') ||
      url.startsWith('/forbidden') ||
      url.startsWith('/not-found') ||
      url.startsWith('/server-error');
    return !isPublicRoute && this.authService.isLoggedIn();
  });

  readonly navSections: NavSection[] = [
    {
      title: 'Site Admin',
      items: [
        { label: 'Sites', route: '/admin/sites', icon: 'location_city', moduleKey: 'sites' },
        { label: 'Wings & Plots', route: '/admin/wings', icon: 'domain', moduleKey: 'wings' },
        { label: 'Conditions', route: '/admin/conditions', icon: 'rule', moduleKey: 'conditions' },
        { label: 'Ledgers', route: '/admin/ledgers', icon: 'menu_book', moduleKey: 'ledgers' },
        { label: 'Banks', route: '/admin/banks', icon: 'account_balance', moduleKey: 'banks' },
        { label: 'Brokers', route: '/admin/brokers', icon: 'handshake', moduleKey: 'brokers' },
        { label: 'Users', route: '/admin/users', icon: 'people', moduleKey: 'users' },
        { label: 'Devices', route: '/admin/devices', icon: 'phonelink_lock', moduleKey: 'devices' }
      ]
    },
    {
      title: 'Booking',
      items: [
        { label: 'Booking Grid', route: '/booking', icon: 'grid_view', moduleKey: 'booking', exact: true }
      ]
    },
    {
      title: 'Accounting',
      items: [
        { label: 'Daily Entry', route: '/accounting', icon: 'receipt', moduleKey: 'daily_entry', exact: true },
        { label: 'Dastavej', route: '/accounting/dastavej', icon: 'description', moduleKey: 'dastavej' },
        { label: 'Vyaj Khata', route: '/accounting/vyaj', icon: 'menu_book', moduleKey: 'vyaj' },
        { label: 'Journal Voucher', route: '/accounting/journal-voucher', icon: 'article', moduleKey: 'journal_voucher' }
      ]
    },
    {
      title: 'Reports',
      items: [
        { label: 'All Reports', route: '/reports', icon: 'assessment', moduleKey: 'reports', exact: true }
      ]
    }
  ];

  readonly dashboardLink: NavLink = {
    label: 'Dashboard',
    route: '/dashboard',
    icon: 'dashboard',
    moduleKey: 'dashboard',
    exact: true
  };

  readonly userInitials = computed(() => {
    const name = this.authService.currentUser()?.username ?? '';
    const parts = name.trim().split(/[\s._-]+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  });

  readonly visibleNavSections = computed(() => {
    this.authService.currentUser();
    return this.navSections
      .map((section) => ({
        ...section,
        items: section.items.filter((item) => this.authService.hasPermission(item.moduleKey, 'view'))
      }))
      .filter((section) => section.items.length > 0);
  });

  constructor() {
    effect(() => {
      if (this.authService.authReady()) {
        this.siteContext.loadSites().subscribe();
      }
    });

    effect(() => {
      const show = this.showShell() && this.showNotifications();
      const siteId = this.siteContext.activeSiteId();
      this.stopPolling();
      if (show) {
        this.refreshNotifications(siteId);
        this.pollTimer = setInterval(() => this.refreshNotifications(this.siteContext.activeSiteId()), 60_000);
      } else {
        this.notifications.set([]);
        this.unreadCount.set(0);
      }
    });

    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => {
      if (this.showShell() && this.showNotifications()) {
        this.refreshNotifications(this.siteContext.activeSiteId());
      }
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
    this.pollSub?.unsubscribe();
    this.pollSub = null;
  }

  refreshNotifications(siteId?: string | null): void {
    this.pollSub?.unsubscribe();
    this.pollSub = this.notificationService.getList(siteId).subscribe({
      next: (res) => {
        if (res.success) this.notifications.set(res.data ?? []);
      }
    });
    this.notificationService.getUnreadCount(siteId).subscribe({
      next: (res) => {
        if (res.success) this.unreadCount.set(res.data ?? 0);
      }
    });
  }

  onNotificationClick(n: UserNotification): void {
    if (!n.isRead) {
      this.notificationService.markRead(n.id).subscribe({
        next: () => {
          this.notifications.update((list) =>
            list.map((x) => (x.id === n.id ? { ...x, isRead: true } : x))
          );
          this.unreadCount.update((c) => Math.max(0, c - 1));
        }
      });
    }
    const route =
      n.module === 'vyaj'
        ? '/accounting/vyaj'
        : n.module === 'journal'
          ? '/accounting/journal-voucher'
          : '/accounting';
    void this.router.navigateByUrl(route);
  }

  restoreNotification(event: Event, n: UserNotification): void {
    event.stopPropagation();
    event.preventDefault();
    this.notificationService.restore(n.id).subscribe({
      next: (res) => {
        if (!res.success) {
          this.toast.error(res.message || 'Restore failed');
          return;
        }
        const msg = res.data?.message || res.message || 'Record restored';
        this.toast.success(msg);
        this.notifications.update((list) =>
          list.map((x) =>
            x.entityId === n.entityId && x.entityType === n.entityType
              ? { ...x, canRestore: false, resolvedAt: new Date().toISOString(), isRead: true }
              : x
          )
        );
        this.unreadCount.update((c) => Math.max(0, c - (n.isRead ? 0 : 1)));
        if (n.module === 'vyaj') {
          void this.router.navigateByUrl('/accounting/vyaj');
        }
      },
      error: () => this.toast.error('Failed to restore record')
    });
  }

  markAllNotificationsRead(): void {
    const siteId = this.siteContext.activeSiteId();
    this.notificationService.markAllRead(siteId).subscribe({
      next: () => {
        this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
        this.unreadCount.set(0);
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
