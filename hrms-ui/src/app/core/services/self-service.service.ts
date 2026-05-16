import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface EmployeeProfile {
  id: number;
  name: string;
  email: string;
  phone: string;
  icPassport: string;
  taxNumber: string;
  departmentId: number;
  departmentName: string;
  designation: string;
  joinDate: string;
  salary: number;
  bankId: number;
  bankName: string;
  accountNumber: string;
  profilePicture: string;
  isActive: boolean;
  createdAt: string;
}

export interface UpdateProfileRequest {
  phone?: string;
  email?: string;
  bankId?: number;
  accountNumber?: string;
}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface Payslip {
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
}

export interface AttendanceRecord {
  id: number;
  date: string;
  status: string;
  checkIn: string;
  checkOut: string;
  workHours: number;
  remarks: string;
  createdAt: string;
}

export interface AttendanceResponse {
  startDate: string;
  endDate: string;
  summary: {
    totalRecords: number;
    presentCount: number;
    absentCount: number;
    leaveCount: number;
    halfDayCount: number;
    totalWorkHours: number;
    averageWorkHours: number;
  };
  records: AttendanceRecord[];
}

export interface LeaveBalance {
  id: number;
  leaveTypeId: number;
  leaveTypeName: string;
  leaveTypeCode: string;
  year: number;
  totalDays: number;
  usedDays: number;
  balanceDays: number;
  carryForwardDays: number;
  createdAt: string;
  updatedAt: string;
}

export interface LeaveBalanceResponse {
  employeeId: number;
  employeeName: string;
  year: number;
  balances: LeaveBalance[];
}

export interface LeaveRequest {
  id: number;
  leaveTypeId: number;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: string;
  requestedOn: string;
  approvedBy: number | null;
  approvedOn: string | null;
  approvalRemarks: string | null;
  cancelledOn: string | null;
  cancellationReason: string | null;
}

export interface Timesheet {
  id: number;
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
  approvedBy: number | null;
  approvedOn: string | null;
  remarks: string | null;
}

export interface LeaveRequestCreate {
  employeeId?: number;
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface LeaveType {
  id: number;
  name: string;
  code: string;
  description: string;
  defaultDaysPerYear: number;
  isActive: boolean;
  requiresApproval: boolean;
  isPaid: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class SelfServiceService {
  private apiUrl = `${environment.apiUrl}/selfservice`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  // Profile
  getMyProfile(): Observable<EmployeeProfile> {
    return this.http.get<EmployeeProfile>(`${this.apiUrl}/my-profile`);
  }

  updateMyProfile(data: UpdateProfileRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/my-profile`, data);
  }

  changePassword(data: ChangePasswordRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/change-password`, data);
  }

  // Payslips
  getMyPayslips(year?: number, month?: number): Observable<Payslip[]> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    if (month) params = params.set('month', month.toString());
    return this.http.get<Payslip[]>(`${this.apiUrl}/my-payslips`, { params });
  }

  // Attendance
  getMyAttendance(startDate?: string, endDate?: string): Observable<AttendanceResponse> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<AttendanceResponse>(`${this.apiUrl}/my-attendance`, { params });
  }

  // Leave Balance
  getMyLeaveBalance(year?: number): Observable<LeaveBalanceResponse> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    return this.http.get<LeaveBalanceResponse>(`${this.apiUrl}/my-leave-balance`, { params });
  }

  // Leave Requests
  getMyLeaveRequests(status?: string): Observable<LeaveRequest[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/my-leave-requests`, { params });
  }

  // Timesheets
  getMyTimesheets(year?: number, month?: number): Observable<Timesheet[]> {
    let params = new HttpParams();
    if (year) params = params.set('year', year.toString());
    if (month) params = params.set('month', month.toString());
    return this.http.get<Timesheet[]>(`${this.apiUrl}/my-timesheets`, { params });
  }

  submitTimesheet(timesheetId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/submit-timesheet/${timesheetId}`, {});
  }

  uploadTimesheetExcel(file: File, month: number, year: number): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('month', month.toString());
    formData.append('year', year.toString());
    return this.http.post(`${this.apiUrl}/upload-timesheet`, formData);
  }

  downloadTimesheetTemplate(month: number, year: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/timesheet-template`, {
      params: { month: month.toString(), year: year.toString() },
      responseType: 'blob'
    });
  }

  // Leave Request Submission
  getActiveLeaveTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${environment.apiUrl}/leavemanagement/leave-types/active`);
  }

  submitLeaveRequest(request: LeaveRequestCreate): Observable<LeaveRequest> {
    if (!request.employeeId) {
      const employeeId = this.authService.getEmployeeId();
      if (employeeId) {
        request = { ...request, employeeId };
      }
    }
    return this.http.post<LeaveRequest>(`${environment.apiUrl}/leavemanagement/requests`, request);
  }

  cancelLeaveRequest(requestId: number, reason: string): Observable<any> {
    return this.http.put(`${environment.apiUrl}/leavemanagement/requests/${requestId}/cancel`, { reason });
  }
}
