import { Directive, Input, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { PermissionLevel } from '../../core/models/permission.model';

@Directive({
  selector: '[appHasPermission]',
  standalone: true
})
export class HasPermissionDirective {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly authService = inject(AuthService);

  private moduleKey = '';
  private level: PermissionLevel = 'view';

  @Input()
  set appHasPermission(moduleKey: string) {
    this.moduleKey = moduleKey;
    this.updateView();
  }

  @Input()
  set appHasPermissionLevel(level: PermissionLevel) {
    this.level = level;
    this.updateView();
  }

  constructor() {
    effect(() => {
      this.authService.currentUser();
      this.updateView();
    });
  }

  private updateView(): void {
    this.viewContainer.clear();

    if (this.moduleKey && this.authService.hasPermission(this.moduleKey, this.level)) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    }
  }
}
