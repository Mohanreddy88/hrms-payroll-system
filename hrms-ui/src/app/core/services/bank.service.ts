import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Bank } from '../models/bank.model';
import { environment } from '../../../environments/environment';

/**
 * BankService — handles all HTTP calls to /api/banks.
 * Supports CRUD operations for Bank Master management.
 */
@Injectable({ providedIn: 'root' })
export class BankService {
  private apiUrl = `${environment.apiUrl}/banks`;

  constructor(private http: HttpClient) {}

  /** Returns only active banks — used in employee form dropdown */
  getActive(): Observable<Bank[]> {
    return this.http.get<Bank[]>(this.apiUrl);
  }

  /** Returns all banks (active + inactive) — used in bank master list */
  getAll(): Observable<Bank[]> {
    return this.http.get<Bank[]>(`${this.apiUrl}/all`);
  }

  /** Get bank by ID */
  getById(id: number): Observable<Bank> {
    return this.http.get<Bank>(`${this.apiUrl}/${id}`);
  }

  /** Create a new bank */
  create(bank: { name: string; isActive: boolean }): Observable<Bank> {
    return this.http.post<Bank>(this.apiUrl, bank);
  }

  /** Update an existing bank */
  update(id: number, bank: { name: string; isActive: boolean }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, bank);
  }

  /** Delete a bank */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
