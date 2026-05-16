import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Timesheet {
  id: number;
  employeeId: number;
  employeeName: string;
  month: number;
  year: number;
  monthName: string;
  totalWorkingDays: number;
  totalPresent: number;        // Regular working days with hours filled
  totalMedicalLeave: number;   // MC count
  totalAbsent: number;         // AL count (Annual Leave)
  totalLeave: number;          // EL count (Emergency Leave)
  totalHalfDay: number;
  totalPublicHolidays: number;
  totalWorkHours: number;
  status: string;
  generatedOn: string;
  approvedBy?: number;
  approvedOn?: string;
  remarks?: string;
}

export interface TimesheetRequest {
  employeeId: number;
  month: number;
  year: number;
  remarks?: string;
}

export interface WorkingDaysInfo {
  month: number;
  year: number;
  monthName: string;
  workingDays: number;
  publicHolidays: number;
  holidays: string[];
}

@Injectable({ providedIn: 'root' })
export class TimesheetService {
  private apiUrl = `${environment.apiUrl}/timesheets`;

  constructor(private http: HttpClient) {}

  getAll(year?: number, month?: number): Observable<Timesheet[]> {
    let url = this.apiUrl;
    const params: string[] = [];
    if (year) params.push(`year=${year}`);
    if (month) params.push(`month=${month}`);
    if (params.length > 0) url += '?' + params.join('&');
    return this.http.get<Timesheet[]>(url);
  }

  getById(id: number): Observable<Timesheet> {
    return this.http.get<Timesheet>(`${this.apiUrl}/${id}`);
  }

  getByEmployee(employeeId: number, year?: number): Observable<Timesheet[]> {
    let url = `${this.apiUrl}/employee/${employeeId}`;
    if (year) url += `?year=${year}`;
    return this.http.get<Timesheet[]>(url);
  }

  generate(request: TimesheetRequest): Observable<Timesheet> {
    return this.http.post<Timesheet>(`${this.apiUrl}/generate`, request);
  }

  generateAll(month: number, year: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/generate-all?month=${month}&year=${year}`, {});
  }

  submit(id: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/submit`, {});
  }

  approve(id: number, approvedByUserId: number, remarks?: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/approve`, {
      approvedByUserId,
      remarks
    });
  }

  reject(id: number, approvedByUserId: number, remarks?: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/reject`, {
      approvedByUserId,
      remarks
    });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  emailTimesheet(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/email`, {});
  }

  getWorkingDays(month: number, year: number): Observable<WorkingDaysInfo> {
    return this.http.get<WorkingDaysInfo>(
      `${this.apiUrl}/working-days?month=${month}&year=${year}`
    );
  }
}
