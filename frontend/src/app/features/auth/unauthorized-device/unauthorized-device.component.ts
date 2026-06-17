import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-unauthorized-device',
  standalone: true,
  imports: [MatCardModule, PageHeaderComponent],
  template: `
    <app-page-header title="Unauthorized Device"></app-page-header>
    <mat-card>
      <mat-card-content>
        <p>This application is licensed for a specific device. Contact your administrator.</p>
      </mat-card-content>
    </mat-card>
  `
})
export class UnauthorizedDeviceComponent {}
