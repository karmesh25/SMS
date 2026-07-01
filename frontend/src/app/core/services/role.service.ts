import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/api-response.model';
import { AppRole, CreateRoleRequest, UpdateRoleRequest } from '../models/permission.model';

@Injectable({ providedIn: 'root' })
export class RoleService {
  constructor(private readonly api: ApiService) {}

  getAll(): Observable<ApiResponse<AppRole[]>> {
    return this.api.get<AppRole[]>('/roles');
  }

  getById(id: string): Observable<ApiResponse<AppRole>> {
    return this.api.get<AppRole>(`/roles/${id}`);
  }

  create(request: CreateRoleRequest): Observable<ApiResponse<AppRole>> {
    return this.api.post<AppRole>('/roles', request);
  }

  update(id: string, request: UpdateRoleRequest): Observable<ApiResponse<AppRole>> {
    return this.api.put<AppRole>(`/roles/${id}`, request);
  }

  delete(id: string): Observable<ApiResponse<unknown>> {
    return this.api.delete(`/roles/${id}`);
  }
}
