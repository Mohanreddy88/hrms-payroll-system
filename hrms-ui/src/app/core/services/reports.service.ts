import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DepartmentPayrollSummary {
  year: number;
  month: number;
  monthName: string;
  departments: {
    departmentId: number;
    departmentName: string;
    employeeCount: number;
    totalGross: number;
    totalNet: number;
    totalEpf: number;
    totalSocso: number;
    totalTax: number;
    totalDeductions: number;
    averageSalary: number;
  }[];
  totals: {
    totalEmployees: number;
    totalGross: number;
    totalNet: number;
    totalEpf: number;
    totalSocso: number;
    totalTax: number;
    totalDeductions: number;
  };
}

export interface AttendanceReport {
  startDate: string;
  endDate: string;
  totalRecords: number;
  presentCount: number;
  absentCount: number;
  leaveCount: number;
  halfDayCount: number;
  byDepartment: {
    department: string;
    totalRecords: number;
    presentCount: number;
    absentCount: number;
    leaveCount: number;
    halfDayCount: number;
  }[];
  records: {
    id: number;
    date: string;
    employeeId: number;
    employeeName: string;
    departmentName: string;
    status: string;
    checkIn: string;
    checkOut: string;
    workHours: number;
    remarks: string;
  }[];
}

export interface EmployeeDirectory {
  totalEmployees: number;
  activeEmployees: number;
  inactiveEmployees: number;
  byDepartment: {
    department: string;
    employeeCount: number;
    employees: any[];
  }[];
  allEmployees: any[];
}

export interface PayrollHistory {
  employeeId: number;
  employeeName: string;
  recordCount: number;
  averageNetSalary: number;
  totalPaid: number;
  records: {
    id: number;
    month: number;
    year: number;
    monthName: string;
    basicSalary: number;
    allowances: number;
    deductions: number;
    epfAmount: number;
    socsoAmount: number;
    taxAmount: number;
    grossIncome: number;
    netSalary: number;
    generatedOn: string;
  }[];
}

export interface MonthlySummary {
  year: number;
  month: number;
  monthName: string;
  payroll: {
    totalPayslips: number;
    totalGross: number;
    totalNet: number;
    totalEpf: number;
    totalSocso: number;
    totalTax: number;
    averageSalary: number;
  };
  attendance: {
    totalRecords: number;
    presentCount: number;
    absentCount: number;
    leaveCount: number;
    halfDayCount: number;
    averageWorkHours: number;
  };
}

@Injectable({
  providedIn: 'root'
})
export class ReportsService {
  private apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  getDepartmentPayrollSummary(year: number, month: number): Observable<DepartmentPayrollSummary> {
    const params = new HttpParams()
      .set('year', year.toString())
      .set('month', month.toString());
    return this.http.get<DepartmentPayrollSummary>(`${this.apiUrl}/department-payroll-summary`, { params });
  }

  getAttendanceByDateRange(startDate: string, endDate: string): Observable<AttendanceReport> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<AttendanceReport>(`${this.apiUrl}/attendance-by-date-range`, { params });
  }

  getEmployeeDirectory(includeInactive: boolean = false): Observable<EmployeeDirectory> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<EmployeeDirectory>(`${this.apiUrl}/employee-directory`, { params });
  }

  getPayrollHistory(employeeId: number, months: number = 12): Observable<PayrollHistory> {
    const params = new HttpParams()
      .set('employeeId', employeeId.toString())
      .set('months', months.toString());
    return this.http.get<PayrollHistory>(`${this.apiUrl}/payroll-history`, { params });
  }

  getMonthlySummary(year: number, month: number): Observable<MonthlySummary> {
    const params = new HttpParams()
      .set('year', year.toString())
      .set('month', month.toString());
    return this.http.get<MonthlySummary>(`${this.apiUrl}/monthly-summary`, { params });
  }

  // Export methods
  exportPayrollToExcel(year: number, month: number): Observable<Blob> {
    const params = new HttpParams()
      .set('year', year.toString())
      .set('month', month.toString());
    return this.http.get(`${this.apiUrl}/export/payroll-excel`, {
      params,
      responseType: 'blob'
    });
  }

  exportEmployeesToExcel(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/employees-excel`, {
      responseType: 'blob'
    });
  }

  exportAttendanceToExcel(startDate: string, endDate: string): Observable<Blob> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get(`${this.apiUrl}/export/attendance-excel`, {
      params,
      responseType: 'blob'
    });
  }

  // Helper method to download blob as file
  downloadFile(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(url);
  }
}
