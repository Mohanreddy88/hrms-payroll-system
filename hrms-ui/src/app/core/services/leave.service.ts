import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LeaveType {
  id: number;
  name: string;
  code: string;
  description: string;
  defaultDaysPerYear: number;
  isActive: boolean;
  requiresApproval: boolean;
  isPaid: boolean;
  createdAt: string;
}

export interface LeaveBalance {
  id: number;
  employeeId: number;
  leaveTypeId: number;
  leaveTypeName: string;
  leaveTypeCode: string;
  year: number;
  totalDays: number;
  usedDays: number;
  balanceDays: number;
  carryForwardDays: number;
  updatedAt: string;
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  leaveTypeId: number;
  leaveTypeName: string;
  leaveTypeCode: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: string;
  requestedOn: string;
  approvedBy?: number;
  approvedOn?: string;
  approvalRemarks?: string;
  cancelledOn?: string;
  cancellationReason?: string;
}

export interface LeaveRequestCreate {
  employeeId: number;
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private apiUrl = `${environment.apiUrl}/leavemanagement`;

  constructor(private http: HttpClient) {}

  // Leave Types
  getLeaveTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${this.apiUrl}/leave-types/active`);
  }

  // Leave Balances
  getBalance(employeeId: number, year: number): Observable<LeaveBalance[]> {
    return this.http.get<LeaveBalance[]>(
      `${this.apiUrl}/balances/employee/${employeeId}?year=${year}`
    );
  }

  initializeBalance(employeeId: number, year: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/balances/initialize`, {
      employeeId,
      year
    });
  }

  // Leave Requests
  getMyRequests(employeeId: number, year?: number): Observable<LeaveRequest[]> {
    let url = `${this.apiUrl}/requests/employee/${employeeId}`;
    if (year) url += `?year=${year}`;
    return this.http.get<LeaveRequest[]>(url);
  }

  getPendingRequests(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/requests/pending`);
  }

  getAllRequests(status?: string, employeeId?: number): Observable<LeaveRequest[]> {
    let url = `${this.apiUrl}/requests?`;
    const params: string[] = [];
    if (status) params.push(`status=${status}`);
    if (employeeId) params.push(`employeeId=${employeeId}`);
    if (params.length > 0) url += params.join('&');
    return this.http.get<LeaveRequest[]>(url);
  }

  createRequest(request: LeaveRequestCreate): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.apiUrl}/requests`, request);
  }

  approveRequest(id: number, remarks: string): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(`${this.apiUrl}/requests/${id}/approve`, {
      status: 'Approved',
      approvalRemarks: remarks
    });
  }

  rejectRequest(id: number, remarks: string): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(`${this.apiUrl}/requests/${id}/reject`, {
      status: 'Rejected',
      approvalRemarks: remarks
    });
  }

  cancelRequest(id: number, reason: string): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(`${this.apiUrl}/requests/${id}/cancel`, { reason });
  }

  deleteRequest(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/requests/${id}`);
  }

  calculateDays(startDate: string, endDate: string): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/calculate-days?startDate=${startDate}&endDate=${endDate}`
    );
  }
}
