import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  CreateVyajEntryRequest,
  CreateVyajPartyRequest,
  CreateVyajPaymentRequest,
  UpdateVyajPartyRequest,
  VyajEntry,
  VyajPartyDetail,
  VyajPartySummary,
  VyajPayment
} from '../../features/vyaj/models/vyaj.models';

@Injectable({ providedIn: 'root' })
export class VyajService {
  private readonly api = inject(ApiService);

  getParties(siteId: string): Observable<{ success: boolean; data: VyajPartySummary[] }> {
    return this.api.get<VyajPartySummary[]>('/vyaj/parties', { siteId });
  }

  getPartyDetail(partyId: string): Observable<{ success: boolean; data: VyajPartyDetail }> {
    return this.api.get<VyajPartyDetail>(`/vyaj/parties/${partyId}`);
  }

  createParty(dto: CreateVyajPartyRequest): Observable<{ success: boolean; data: VyajPartySummary }> {
    return this.api.post<VyajPartySummary>('/vyaj/parties', dto);
  }

  updateParty(partyId: string, dto: UpdateVyajPartyRequest): Observable<{ success: boolean; data: VyajPartySummary }> {
    return this.api.put<VyajPartySummary>(`/vyaj/parties/${partyId}`, dto);
  }

  deleteParty(partyId: string): Observable<{ success: boolean }> {
    return this.api.delete(`/vyaj/parties/${partyId}`);
  }

  createEntry(dto: CreateVyajEntryRequest): Observable<{ success: boolean; data: VyajEntry }> {
    return this.api.post<VyajEntry>('/vyaj/entries', dto);
  }

  toggleEntryClosed(entryId: string, isClosed: boolean): Observable<{ success: boolean; data: VyajEntry }> {
    return this.api.patch<VyajEntry>(`/vyaj/entries/${entryId}/closed`, { isClosed });
  }

  deleteEntry(entryId: string): Observable<{ success: boolean }> {
    return this.api.delete(`/vyaj/entries/${entryId}`);
  }

  createPayment(dto: CreateVyajPaymentRequest): Observable<{ success: boolean; data: VyajPayment }> {
    return this.api.post<VyajPayment>('/vyaj/payments', dto);
  }

  deletePayment(paymentId: string): Observable<{ success: boolean }> {
    return this.api.delete(`/vyaj/payments/${paymentId}`);
  }
}
