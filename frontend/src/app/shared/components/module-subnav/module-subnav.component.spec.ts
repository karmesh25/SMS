import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ModuleNavItem, ModuleSubnavComponent } from './module-subnav.component';

@Component({
  standalone: true,
  imports: [ModuleSubnavComponent],
  template: `<app-module-subnav [items]="items" />`
})
class HostComponent {
  items: ModuleNavItem[] = [
    { label: 'Daily Entry', route: '/accounting/daily-entry', exact: true },
    { label: 'Dastavej', route: '/accounting/dastavej' }
  ];
}

describe('ModuleSubnavComponent', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent, RouterTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('renders a horizontal scroll nav container', () => {
    const nav = fixture.nativeElement.querySelector('nav.module-subnav') as HTMLElement;
    expect(nav).toBeTruthy();
    expect(nav.querySelectorAll('a.module-subnav__link').length).toBe(2);
  });

  it('keeps tab links from shrinking in the scroll strip', () => {
    const link = fixture.nativeElement.querySelector('a.module-subnav__link') as HTMLElement;
    expect(link.classList.contains('module-subnav__link')).toBeTrue();
    expect(getComputedStyle(link).flexShrink).toBe('0');
  });
});
