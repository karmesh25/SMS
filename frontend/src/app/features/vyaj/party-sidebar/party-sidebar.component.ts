import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { SelectOption } from '../../../shared/components/searchable-select/searchable-select.component';
import { VyajPartyKind, VyajPartySummary } from '../models/vyaj.models';

export interface NewVyajPartyPayload {
  name: string;
  partyKind: VyajPartyKind;
  mainLedgerId?: string | null;
  subLedgerId?: string | null;
}

@Component({
  selector: 'app-vyaj-party-sidebar',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    HasPermissionDirective,
    IndianCurrencyPipe
  ],
  templateUrl: './party-sidebar.component.html',
  styleUrl: './party-sidebar.component.scss'
})
export class VyajPartySidebarComponent {
  readonly parties = input.required<VyajPartySummary[]>();
  readonly selectedPartyId = input<string | null>(null);
  readonly mainLedgerOptions = input<SelectOption<string>[]>([]);
  readonly subLedgerOptions = input<SelectOption<string>[]>([]);

  readonly partySelected = output<string>();
  readonly addParty = output<NewVyajPartyPayload>();
  readonly mainLedgerChanged = output<string>();

  newPartyName = '';
  newPartyKind: VyajPartyKind = 'direct';
  newMainLedgerId = '';
  newSubLedgerId = '';

  onMainChange(): void {
    this.newSubLedgerId = '';
    this.mainLedgerChanged.emit(this.newMainLedgerId);
  }

  submitParty(): void {
    const name = this.newPartyName.trim();
    if (!name) return;

    this.addParty.emit({
      name,
      partyKind: this.newPartyKind,
      mainLedgerId: this.newMainLedgerId || null,
      subLedgerId: this.newSubLedgerId || null
    });
    this.newPartyName = '';
    this.newPartyKind = 'direct';
    this.newMainLedgerId = '';
    this.newSubLedgerId = '';
  }
}
