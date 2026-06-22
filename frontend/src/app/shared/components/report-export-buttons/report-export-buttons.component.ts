import { Component, Input, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { ExportService, ReportExportType } from '../../../core/services/export.service';

@Component({
  selector: 'app-report-export-buttons',
  standalone: true,
  imports: [MatButtonModule],
  template: `
    <button mat-stroked-button type="button" [disabled]="disabled" (click)="exportExcel()">Excel</button>
    <button mat-stroked-button type="button" [disabled]="disabled" (click)="exportPdf()">PDF</button>
    <button mat-stroked-button type="button" [disabled]="disabled" (click)="exportWord()">Word</button>
    <button mat-stroked-button type="button" (click)="print()">Print</button>
  `,
  styles: [`
    :host {
      display: inline-flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
    }
  `]
})
export class ReportExportButtonsComponent {
  private readonly exportService = inject(ExportService);

  @Input({ required: true }) reportType!: ReportExportType;
  @Input({ required: true }) filters: Record<string, string | number | boolean> = {};
  @Input() disabled = false;

  exportExcel(): void {
    this.exportService.exportExcel(this.reportType, this.filters);
  }

  exportPdf(): void {
    this.exportService.exportPdf(this.reportType, this.filters);
  }

  exportWord(): void {
    this.exportService.exportWord(this.reportType, this.filters);
  }

  print(): void {
    window.print();
  }
}
