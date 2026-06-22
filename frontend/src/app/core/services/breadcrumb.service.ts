import { Injectable, inject, signal } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { BreadcrumbItem } from '../models/breadcrumb.model';

@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly itemsSignal = signal<BreadcrumbItem[]>([]);
  readonly items = this.itemsSignal.asReadonly();

  constructor() {
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.itemsSignal.set(this.buildBreadcrumbs());
    });
    this.itemsSignal.set(this.buildBreadcrumbs());
  }

  private buildBreadcrumbs(): BreadcrumbItem[] {
    const crumbs: BreadcrumbItem[] = [];
    let currentRoute: ActivatedRoute | null = this.route.root;
    let url = '';

    while (currentRoute) {
      if (currentRoute.outlet !== 'primary') {
        currentRoute = currentRoute.firstChild;
        continue;
      }

      const routeConfig = currentRoute.routeConfig;
      if (routeConfig?.path) {
        const segment = this.resolvePathSegment(routeConfig.path, currentRoute.snapshot.params);
        url += `/${segment}`;
      }

      const label = currentRoute.snapshot.data['breadcrumb'] as string | undefined;
      if (label) {
        const isLast = !this.hasMoreBreadcrumbChildren(currentRoute);
        crumbs.push({
          label,
          url: isLast ? undefined : url || '/'
        });
      }

      currentRoute = currentRoute.firstChild;
    }

    if (crumbs.length > 0 && crumbs[0].label !== 'Dashboard') {
      crumbs.unshift({ label: 'Dashboard', url: '/dashboard' });
    }

    return crumbs;
  }

  private resolvePathSegment(path: string, params: Record<string, string>): string {
    return path
      .split('/')
      .map((part) => (part.startsWith(':') ? params[part.slice(1)] ?? part : part))
      .join('/');
  }

  private hasMoreBreadcrumbChildren(route: ActivatedRoute): boolean {
    let child = route.firstChild;
    while (child) {
      if (child.outlet === 'primary' && child.snapshot.data['breadcrumb']) {
        return true;
      }
      child = child.firstChild;
    }
    return false;
  }
}
