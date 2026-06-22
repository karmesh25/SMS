import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export interface Booking {
  id: string;
  flatId: string;
  flatNo: string;
  wingName: string;
  siteId: string;
  memberSubLedgerId: string;
  memberName: string;
  brokerId?: string;
  brokerName?: string;
  conditionId: string;
  conditionName: string;
  bookingDate: string;
  customerContact?: string;
  sqft: number;
  rate: number;
  totalPrice: number;
  brokeragePct: number;
  brokerageAmount: number;
  customerType: string;
  isArjaMarjaSell: boolean;
  status: string;
  cancelDate?: string;
  dastavejDate?: string;
  satakhatDate?: string;
  documentNumber?: string;
  serviceTax?: number;
  notes?: string;
}

export interface FlatDetail {
  id: string;
  flatNo: string;
  sqft: number;
  status: string;
  wingName: string;
}

export interface InstallmentMilestone {
  id: string;
  bookingId: string;
  milestoneName: string;
  sortOrder: number;
  dueAmount: number;
  paidAmount: number;
  remainingAmount: number;
  dueDate: string;
  paidDate?: string;
  status: string;
}

export interface InstallmentSummary {
  bookingId: string;
  totalDue: number;
  totalPaid: number;
  totalRemaining: number;
  percentagePaid: number;
  daysSinceLastPayment?: number;
  daysFromBooking: number;
  milestones: InstallmentMilestone[];
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly api = inject(ApiService);

  getBySite(siteId: string, params?: Record<string, string | number | boolean>) {
    return this.api.get<{ items: Booking[]; totalCount: number }>(`/bookings/${siteId}`, params);
  }

  getById(id: string) {
    return this.api.get<Booking>(`/bookings/detail/${id}`);
  }

  getByFlat(flatId: string) {
    return this.api.get<Booking>(`/bookings/by-flat/${flatId}`);
  }

  getFlatDetail(flatId: string) {
    return this.api.get<FlatDetail>(`/bookings/flat-detail/${flatId}`);
  }

  create(body: unknown) {
    return this.api.post<Booking>('/bookings', body);
  }

  update(id: string, body: unknown) {
    return this.api.put<Booking>(`/bookings/${id}`, body);
  }

  cancel(id: string, body: unknown) {
    return this.api.post<Booking>(`/bookings/${id}/cancel`, body);
  }

  getInstallments(bookingId: string) {
    return this.api.get<InstallmentSummary>(`/installments/${bookingId}`);
  }

  recordPayment(body: unknown) {
    return this.api.post<InstallmentMilestone>('/installments', body);
  }

  getDastavejList(siteId: string) {
    return this.api.get<DastavejBookingRow[]>(`/bookings/dastavej/${siteId}`);
  }

  updateDastavej(id: string, body: unknown) {
    return this.api.put<Booking>(`/bookings/${id}/dastavej-satakhat`, body);
  }
}

export interface DastavejBookingRow {
  id: string;
  flatNo: string;
  memberName: string;
  bookingDate: string;
  dastavejDate?: string;
  satakhatDate?: string;
  documentNumber?: string;
  serviceTax?: number;
  status: string;
}
