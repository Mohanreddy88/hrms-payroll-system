import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Department {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  employeeCount?: number;
  createdAt?: string;
}

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private apiUrl = `${environment.apiUrl}/departments`;

  constructor(private http: HttpClient) {}

  /** Returns only active departments — used in employee form dropdown */
  getActive(): Observable<Department[]> {
    return this.http.get<Department[]>(this.apiUrl);
  }

  /** Returns all departments (active + inactive) — used in department master list */
  getAll(): Observable<Department[]> {
    return this.http.get<Department[]>(`${this.apiUrl}/all`);
  }

  /** Get department by ID */
  getById(id: number): Observable<Department> {
    return this.http.get<Department>(`${this.apiUrl}/${id}`);
  }

  /** Create a new department */
  create(department: { name: string; description: string; isActive: boolean }): Observable<Department> {
    return this.http.post<Department>(this.apiUrl, department);
  }

  /** Update an existing department */
  update(id: number, department: { name: string; description: string; isActive: boolean }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, department);
  }

  /** Delete a department */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
