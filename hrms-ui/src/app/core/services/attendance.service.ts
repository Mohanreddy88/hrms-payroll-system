import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Attendance, AttendanceRequest } from '../models/attendance.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private apiUrl = `${environment.apiUrl}/attendance`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Attendance[]> {
    return this.http.get<Attendance[]>(this.apiUrl);
  }

  getByDate(date: string): Observable<Attendance[]> {
    return this.http.get<Attendance[]>(`${this.apiUrl}?date=${date}`);
  }

  mark(payload: AttendanceRequest): Observable<Attendance> {
    return this.http.post<Attendance>(this.apiUrl, payload);
  }

  update(id: number, payload: AttendanceRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
