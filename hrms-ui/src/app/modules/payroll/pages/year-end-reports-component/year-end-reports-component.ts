import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ToastService } from '../../../../core/services/toast.service';
import { PayrollService } from '../../../../core/services/payroll.service';
import { EaForm } from '../../../../core/models/payroll.model';
import { environment } from '../../../../../environments/environment';

interface YearEndEmployee {
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  email: string;
  designation: string;
  departmentName: string;
  monthsProcessed: number;
  totalMonths: number;
  ytdGross: number;
  ytdBasic: number;
  ytdAllowances: number;
  ytdEpf: number;
  ytdSocso: number;
  ytdTax: number;
  ytdNet: number;
  isEaReady: boolean;
  hasEmail: boolean;
}

@Component({
  selector: 'app-year-end-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './year-end-reports-component.html',
  styleUrls: ['./year-end-reports-component.scss']
})
export class YearEndReportsComponent implements OnInit {
  selectedYear = new Date().getFullYear() - 1; // default to previous year (EA is for last year)
  years: number[] = [];

  loading     = false;
  hasRecords  = false;
  message     = '';
  employees: YearEndEmployee[] = [];

  // Bulk email
  bulkSending = false;
  sentIds: Set<number> = new Set();   // tracks who was sent in this session
  sendingIds: Set<number> = new Set();// tracks in-progress sends

  // EA Form modal
  showEaModal  = false;
  eaLoading    = false;
  eaData: EaForm | null = null;
  eaEmployeeId = 0;
  eaEmployeeName = '';
  sendingEaEmail = false;

  constructor(
    private http: HttpClient,
    private toast: ToastService,
    private payrollService: PayrollService
  ) {
    const current = new Date().getFullYear();
    for (let y = current - 3; y <= current; y++) this.years.push(y);
  }

  ngOnInit(): void { this.loadSummary(); }

  loadSummary(): void {
    this.loading = true;
    this.hasRecords = false;
    this.employees = [];
    this.sentIds.clear();
    this.http.get<any>(`${environment.apiUrl}/payroll/year-end-summary/${this.selectedYear}`).subscribe({
      next: (data) => {
        this.loading    = false;
        this.hasRecords = data.hasRecords;
        this.message    = data.message ?? '';
        this.employees  = data.employees ?? [];
      },
      error: () => { this.loading = false; this.toast.error('Error', 'Failed to load year-end summary.'); }
    });
  }

  onYearChange(): void { this.loadSummary(); }

  // ── Getters ───────────────────────────────────────────────────────────────────
  get readyCount(): number  { return this.employees.filter(e => e.isEaReady).length; }
  get totalNet(): number    { return this.employees.reduce((s, e) => s + e.ytdNet, 0); }
  get sentCount(): number   { return this.sentIds.size; }

  // ── Send single EA Form ───────────────────────────────────────────────────────
  sendEaEmail(emp: YearEndEmployee): void {
    this.sendingIds.add(emp.employeeId);
    this.http.post(`${environment.apiUrl}/payroll/email-ea-form/${emp.employeeId}/${this.selectedYear}`, {}).subscribe({
      next: () => {
        this.sendingIds.delete(emp.employeeId);
        this.sentIds.add(emp.employeeId);
        this.toast.success('Sent', `EA Form emailed to ${emp.employeeName}.`);
      },
      error: (err) => {
        this.sendingIds.delete(emp.employeeId);
        this.toast.error('Failed', err?.error?.message || 'Could not send EA Form.');
      }
    });
  }

  isSending(id: number): boolean { return this.sendingIds.has(id); }
  isSent(id: number): boolean    { return this.sentIds.has(id); }

  // ── Bulk send all ready employees ─────────────────────────────────────────────
  sendAllEaForms(): void {
    const ready = this.employees.filter(e => e.isEaReady && e.hasEmail && !this.sentIds.has(e.employeeId));
    if (ready.length === 0) {
      this.toast.warning('Nothing to send', 'All eligible EA Forms have already been sent.'); return;
    }
    this.bulkSending = true;
    let completed = 0;
    let failed = 0;
    ready.forEach(emp => {
      this.sendingIds.add(emp.employeeId);
      this.http.post(`${environment.apiUrl}/payroll/email-ea-form/${emp.employeeId}/${this.selectedYear}`, {}).subscribe({
        next: () => {
          this.sendingIds.delete(emp.employeeId);
          this.sentIds.add(emp.employeeId);
          completed++;
          if (completed + failed === ready.length) {
            this.bulkSending = false;
            this.toast.success('Bulk Send Complete', `${completed} EA Form(s) sent successfully.${failed > 0 ? ` ${failed} failed.` : ''}`);
          }
        },
        error: () => {
          this.sendingIds.delete(emp.employeeId);
          failed++;
          if (completed + failed === ready.length) {
            this.bulkSending = false;
            this.toast.success('Bulk Send Complete', `${completed} EA Form(s) sent successfully. ${failed} failed.`);
          }
        }
      });
    });
  }

  // ── EA Form preview modal ─────────────────────────────────────────────────────
  openEaForm(emp: YearEndEmployee): void {
    this.eaEmployeeId   = emp.employeeId;
    this.eaEmployeeName = emp.employeeName;
    this.eaData         = null;
    this.showEaModal    = true;
    this.eaLoading      = true;
    this.payrollService.getEaForm(emp.employeeId, this.selectedYear).subscribe({
      next: (data) => { this.eaData = data; this.eaLoading = false; },
      error: (err) => {
        this.eaLoading = false;
        this.toast.error('EA Form Error', err?.error?.message || 'Could not load EA Form.');
        this.showEaModal = false;
      }
    });
  }

  closeEaModal(): void { this.showEaModal = false; this.eaData = null; this.sendingEaEmail = false; }
  printEaForm(): void  { window.print(); }

  confirmSendEaEmail(): void {
    this.sendingEaEmail = true;
    this.http.post(`${environment.apiUrl}/payroll/email-ea-form/${this.eaEmployeeId}/${this.selectedYear}`, {}).subscribe({
      next: () => {
        this.sendingEaEmail = false;
        this.sentIds.add(this.eaEmployeeId);
        this.toast.success('EA Form Sent', `EA Form for ${this.selectedYear} emailed to ${this.eaEmployeeName}.`);
        this.closeEaModal();
      },
      error: (err) => {
        this.sendingEaEmail = false;
        this.toast.error('Email Failed', err?.error?.message || 'Could not send EA Form email.');
      }
    });
  }
}
