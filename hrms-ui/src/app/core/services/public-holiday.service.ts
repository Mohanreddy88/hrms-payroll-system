import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PublicHoliday {
  id: number;
  name: string;
  date: string;
  year: number;
  isNational: boolean;
  state?: string;
  description?: string;
  createdAt?: string;
}

export interface PublicHolidayRequest {
  name: string;
  date: string;
  isNational: boolean;
  state?: string;
  description?: string;
}

@Injectable({ providedIn: 'root' })
export class PublicHolidayService {
  private apiUrl = `${environment.apiUrl}/publicholidays`;

  constructor(private http: HttpClient) {}

  getAll(year?: number): Observable<PublicHoliday[]> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    return this.http.get<PublicHoliday[]>(this.apiUrl, { params });
  }

  getYears(): Observable<number[]> {
    return this.http.get<number[]>(`${this.apiUrl}/years`);
  }

  create(req: PublicHolidayRequest): Observable<PublicHoliday> {
    return this.http.post<PublicHoliday>(this.apiUrl, req);
  }

  update(id: number, req: PublicHolidayRequest): Observable<PublicHoliday> {
    return this.http.put<PublicHoliday>(`${this.apiUrl}/${id}`, req);
  }

  delete(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}
