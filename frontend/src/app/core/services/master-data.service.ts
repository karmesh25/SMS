import { Injectable, inject } from '@angular/core';

import { ApiService } from './api.service';



@Injectable({ providedIn: 'root' })

export class MasterDataService {

  private readonly api = inject(ApiService);



  getSites() {

    return this.api.get<unknown[]>('/sites');

  }



  createSite(body: unknown) {

    return this.api.post<unknown>('/sites', body);

  }



  updateSite(id: string, body: unknown) {

    return this.api.put<unknown>(`/sites/${id}`, body);

  }



  deleteSite(id: string) {

    return this.api.delete<unknown>(`/sites/${id}`);

  }



  getWings(siteId: string) {

    return this.api.get<unknown[]>(`/wings/${siteId}`);

  }



  createWing(body: unknown) {

    return this.api.post<unknown>('/wings', body);

  }



  updateWing(id: string, body: unknown) {

    return this.api.put<unknown>(`/wings/${id}`, body);

  }



  deleteWing(id: string) {

    return this.api.delete<unknown>(`/wings/${id}`);

  }



  getFlatGrid(wingId: string) {

    return this.api.get<unknown>(`/flats/${wingId}/grid`);

  }



  getMainLedgers(siteId: string) {

    return this.api.get<unknown[]>(`/ledgers/main/${siteId}`);

  }



  createMainLedger(body: unknown) {

    return this.api.post<unknown>('/ledgers/main', body);

  }



  updateMainLedger(id: string, body: unknown) {

    return this.api.put<unknown>(`/ledgers/main/${id}`, body);

  }



  deleteMainLedger(id: string) {

    return this.api.delete<unknown>(`/ledgers/main/${id}`);

  }



  getSubLedgers(mainLedgerId: string) {

    return this.api.get<unknown[]>(`/ledgers/sub/${mainLedgerId}`);

  }



  searchSubLedgers(siteId: string, flatNo: string) {

    return this.api.get<unknown[]>('/ledgers/sub/search', { siteId, flatNo });

  }



  createSubLedger(body: unknown) {

    return this.api.post<unknown>('/ledgers/sub', body);

  }



  updateSubLedger(id: string, body: unknown) {

    return this.api.put<unknown>(`/ledgers/sub/${id}`, body);

  }



  deleteSubLedger(id: string) {

    return this.api.delete<unknown>(`/ledgers/sub/${id}`);

  }



  getConditions(siteId: string) {

    return this.api.get<unknown[]>(`/conditions/${siteId}`);

  }



  createCondition(body: unknown) {

    return this.api.post<unknown>('/conditions', body);

  }



  updateCondition(id: string, body: unknown) {

    return this.api.put<unknown>(`/conditions/${id}`, body);

  }



  deleteCondition(id: string) {

    return this.api.delete<unknown>(`/conditions/${id}`);

  }



  getConditionItems(conditionId: string) {

    return this.api.get<unknown[]>(`/conditions/${conditionId}/items`);

  }



  addConditionItem(conditionId: string, body: unknown) {

    return this.api.post<unknown>(`/conditions/${conditionId}/items`, body);

  }



  updateConditionItem(itemId: string, body: unknown) {

    return this.api.put<unknown>(`/conditions/items/${itemId}`, body);

  }



  deleteConditionItem(itemId: string) {

    return this.api.delete<unknown>(`/conditions/items/${itemId}`);

  }



  getBanks(siteId: string) {

    return this.api.get<unknown[]>(`/banks/${siteId}`);

  }



  createBank(body: unknown) {

    return this.api.post<unknown>('/banks', body);

  }



  updateBank(id: string, body: unknown) {

    return this.api.put<unknown>(`/banks/${id}`, body);

  }



  toggleBank(id: string) {

    return this.api.put<unknown>(`/banks/${id}/toggle`, {});

  }



  getBrokers(siteId: string) {

    return this.api.get<unknown[]>(`/brokers/${siteId}`);

  }



  createBroker(body: unknown) {

    return this.api.post<unknown>('/brokers', body);

  }



  updateBroker(id: string, body: unknown) {

    return this.api.put<unknown>(`/brokers/${id}`, body);

  }



  deleteBroker(id: string) {

    return this.api.delete<unknown>(`/brokers/${id}`);

  }

}

