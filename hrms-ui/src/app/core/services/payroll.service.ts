import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Payroll,
  PayrollDetails,
  PayrollRequest,
  PayrollGenerateRequest,
  PayrollBulkGenerateRequest,
  PayrollCalculateRequest,
  PayrollEligibility,
  PayrollCalculation,
  PayrollAdjustmentRequest,
  PayrollApprovalRequest,
  PayrollRejectionRequest
} from '../models/payroll.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PayrollService {
  private apiUrl = `${environment.apiUrl}/payroll`;

  constructor(private http: HttpClient) {}

  /**
   * Get all payrolls with optional filters
   */
  getAll(filters?: { status?: string; month?: number; year?: number }): Observable<Payroll[]> {
    let params = new HttpParams();
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.month) params = params.set('month', filters.month.toString());
    if (filters?.year) params = params.set('year', filters.year.toString());
    
    return this.http.get<Payroll[]>(this.apiUrl, { params });
  }

  /**
   * Get payroll by ID with full details
   */
  getById(id: number): Observable<PayrollDetails> {
    return this.http.get<PayrollDetails>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get payrolls by employee
   */
  getByEmployee(employeeId: number): Observable<Payroll[]> {
    return this.http.get<Payroll[]>(`${this.apiUrl}/employee/${employeeId}`);
  }

  /**
   * Check if employee is eligible for payroll generation
   */
  checkEligibility(employeeId: number, month: number, year: number): Observable<PayrollEligibility> {
    return this.http.get<PayrollEligibility>(`${this.apiUrl}/eligibility/${employeeId}/${month}/${year}`);
  }

  /**
   * Calculate payroll (preview without saving)
   */
  calculate(request: PayrollCalculateRequest): Observable<PayrollCalculation> {
    return this.http.post<PayrollCalculation>(`${this.apiUrl}/calculate`, request);
  }

  /**
   * Generate payroll for single employee (attendance-driven)
   */
  generateSingle(request: PayrollGenerateRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/generate`, request);
  }

  /**
   * Generate payroll for multiple employees (bulk)
   */
  generateBulk(request: PayrollBulkGenerateRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/generate-bulk`, request);
  }

  /**
   * OLD: Manual payroll generation (kept for backward compatibility)
   */
  generate(payload: PayrollRequest): Observable<Payroll> {
    return this.http.post<Payroll>(this.apiUrl, payload);
  }

  /**
   * Approve payroll
   */
  approve(id: number, request?: PayrollApprovalRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/approve`, request || {});
  }

  /**
   * Reject payroll
   */
  reject(id: number, request: PayrollRejectionRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/reject`, request);
  }

  /**
   * Mark payroll as processed
   */
  process(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/process`, {});
  }

  /**
   * Add manual adjustment
   */
  addAdjustment(id: number, request: PayrollAdjustmentRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/adjustment`, request);
  }

  /**
   * Delete payroll
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Email payslip to employee
   */
  emailPayslip(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/email/${id}`, {});
  }

  /**
   * Email payslips to multiple employees in bulk
   */
  emailBulk(payrollIds: number[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/email-bulk`, payrollIds);
  }

  /**
   * Download payslip as PDF
   */
  downloadPayslipPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/pdf/${id}`, { responseType: 'blob' });
  }
}
