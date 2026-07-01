import { Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { WingManagementComponent } from './wing-management.component';
import { PlotManagementComponent } from './plot-management.component';

@Component({
  selector: 'app-wings-plots-shell',
  standalone: true,
  imports: [MatTabsModule, WingManagementComponent, PlotManagementComponent],
  template: `
    <mat-tab-group animationDuration="0ms">
      <mat-tab label="Wings">
        <div class="tab-body">
          <app-wing-management />
        </div>
      </mat-tab>
      <mat-tab label="Plots">
        <div class="tab-body">
          <app-plot-management />
        </div>
      </mat-tab>
    </mat-tab-group>
  `,
  styles: [`.tab-body { padding-top: 1rem; }`]
})
export class WingsPlotsShellComponent {}
