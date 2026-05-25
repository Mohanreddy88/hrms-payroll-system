import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EmployeeByDepartment {
  departmentId: number | null;
  departmentName: string;
  employeeCount: number;
}

export interface PayrollTrend {
  year: number;
  month: number;
  monthName: string;
  totalGross: number;
  totalNet: number;
  totalDeductions: number;
  employeeCount: number;
}

export interface AttendanceStatistics {
  year: number;
  month: number;
  totalRecords: number;
  presentCount: number;
  absentCount: number;
  leaveCount: number;
  halfDayCount: number;
  byDepartment: Array<{
    department: string;
    employeeCount: number;
    totalDays: number;
    presentDays: number;
    attendanceRate: number;
  }>;
}

export interface DashboardSummary {
  totalEmployees: number;
  totalDepartments: number;
  monthlyPayroll: number;
  monthlyPayslipCount: number;
  todayAttendance: number;
  todayAbsent: number;
}

export interface SalaryDistribution {
  below3k: number;
  range3kTo5k: number;
  range5kTo8k: number;
  range8kTo12k: number;
  above12k: number;
  averageSalary: number;
  medianSalary: number;
  minSalary: number;
  maxSalary: number;
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private apiUrl = `${environment.apiUrl}/analytics`;

  constructor(private http: HttpClient) {}

  getEmployeeCountByDepartment(): Observable<EmployeeByDepartment[]> {
    return this.http.get<EmployeeByDepartment[]>(`${this.apiUrl}/employee-count-by-department`);
  }

  getPayrollTrends(months: number = 6): Observable<PayrollTrend[]> {
    return this.http.get<PayrollTrend[]>(`${this.apiUrl}/payroll-trends?months=${months}`);
  }

  getAttendanceStatistics(year?: number, month?: number): Observable<AttendanceStatistics> {
    let url = `${this.apiUrl}/attendance-statistics`;
    const params: string[] = [];
    if (year) params.push(`year=${year}`);
    if (month) params.push(`month=${month}`);
    if (params.length > 0) url += '?' + params.join('&');
    return this.http.get<AttendanceStatistics>(url);
  }

  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/dashboard-summary`);
  }

  getSalaryDistribution(): Observable<SalaryDistribution> {
    return this.http.get<SalaryDistribution>(`${this.apiUrl}/salary-distribution`);
  }
}
