import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  QueryList,
  ViewChild,
  ViewChildren
} from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Subscription, filter } from 'rxjs';

export interface ModuleNavItem {
  label: string;
  route: string;
  exact?: boolean;
}

@Component({
  selector: 'app-module-subnav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="module-subnav" #navContainer>
      @for (item of items; track item.route) {
        <a
          #navLink
          class="module-subnav__link"
          [routerLink]="item.route"
          routerLinkActive="active"
          [routerLinkActiveOptions]="{ exact: item.exact ?? false }">
          {{ item.label }}
        </a>
      }
    </nav>
  `,
  styles: [`
    .module-subnav {
      display: flex;
      gap: 1rem;
      margin-bottom: 1rem;
      flex-wrap: wrap;
      border-bottom: 1px solid #eef2f6;
      padding-bottom: 0.5rem;
    }

    .module-subnav__link {
      color: #1f4e79;
      text-decoration: none;
      font-weight: 500;
      padding: 0.25rem 0;
      border-bottom: 2px solid transparent;
      flex-shrink: 0;
    }

    .module-subnav__link.active {
      border-bottom-color: #2e75b6;
      color: #2e75b6;
    }

    @media (max-width: 959px) {
      .module-subnav {
        flex-wrap: nowrap;
        overflow-x: auto;
        scroll-snap-type: x proximity;
        -webkit-overflow-scrolling: touch;
        scrollbar-width: thin;
        gap: 0.5rem;
        padding-bottom: 0.35rem;
      }

      .module-subnav::-webkit-scrollbar {
        height: 4px;
      }

      .module-subnav::-webkit-scrollbar-thumb {
        background: rgba(31, 78, 121, 0.25);
        border-radius: 4px;
      }

      .module-subnav__link {
        white-space: nowrap;
        scroll-snap-align: start;
        padding: 0.5rem 0.75rem;
      }
    }

    @media (max-width: 599px) {
      .module-subnav__link {
        font-size: 0.875rem;
        padding: 0.45rem 0.65rem;
      }
    }
  `]
})
export class ModuleSubnavComponent implements AfterViewInit, OnDestroy {
  @Input({ required: true }) items: ModuleNavItem[] = [];

  @ViewChild('navContainer') navContainer?: ElementRef<HTMLElement>;
  @ViewChildren('navLink', { read: ElementRef }) navLinks?: QueryList<ElementRef<HTMLElement>>;

  private routerSub?: Subscription;
  private linksSub?: Subscription;

  constructor(private readonly router: Router) {}

  ngAfterViewInit(): void {
    this.routerSub = this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => this.scrollActiveIntoView());

    this.linksSub = this.navLinks?.changes.subscribe(() => this.scrollActiveIntoView());
    setTimeout(() => this.scrollActiveIntoView());
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
    this.linksSub?.unsubscribe();
  }

  private scrollActiveIntoView(): void {
    const container = this.navContainer?.nativeElement;
    if (!container) return;

    const active = container.querySelector<HTMLElement>('a.active');
    active?.scrollIntoView({ inline: 'nearest', block: 'nearest', behavior: 'smooth' });
  }
}
