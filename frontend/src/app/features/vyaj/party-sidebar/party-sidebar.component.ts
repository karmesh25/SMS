import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { HasRoleDirective } from '../../../shared/directives/has-role.directive';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { VyajPartySummary } from '../models/vyaj.models';

@Component({
  selector: 'app-vyaj-party-sidebar',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    HasRoleDirective,
    IndianCurrencyPipe
  ],
  templateUrl: './party-sidebar.component.html',
  styleUrl: './party-sidebar.component.scss'
})
export class VyajPartySidebarComponent {
  readonly parties = input.required<VyajPartySummary[]>();
  readonly selectedPartyId = input<string | null>(null);

  readonly partySelected = output<string>();
  readonly addParty = output<string>();

  newPartyName = '';

  submitParty(): void {
    const name = this.newPartyName.trim();
    if (!name) return;
    this.addParty.emit(name);
    this.newPartyName = '';
  }
}
