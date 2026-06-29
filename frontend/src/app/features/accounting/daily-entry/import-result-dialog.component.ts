import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { DailyEntryImportResult } from '../../../core/services/daily-entry.service';

@Component({
  selector: 'app-import-result-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Import Result</h2>
    <mat-dialog-content>
      <p><strong>{{ data.importedCount }}</strong> entries imported successfully.</p>
      @if (data.failedCount > 0) {
        <p><strong>{{ data.failedCount }}</strong> rows failed:</p>
        <ul class="error-list">
          @for (err of data.errors; track err.rowNumber + err.message) {
            <li>Row {{ err.rowNumber }}: {{ err.message }}</li>
          }
        </ul>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" (click)="close()">OK</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .error-list {
      max-height: 240px;
      overflow-y: auto;
      margin: 0.5rem 0 0;
      padding-left: 1.25rem;
      font-size: 0.875rem;
    }
    .error-list li { margin-bottom: 0.35rem; }
  `]
})
export class ImportResultDialogComponent {
  readonly data = inject<DailyEntryImportResult>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ImportResultDialogComponent>);

  close(): void {
    this.dialogRef.close();
  }
}
