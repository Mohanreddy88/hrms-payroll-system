import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ToastService } from '../../../../core/services/toast.service';
import { PayrollService } from '../../../../core/services/payroll.service';
import { YtdSummary, EaForm } from '../../../../core/models/payroll.model';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-salary-release',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './salary-release-component.html',
  styleUrls: ['./salary-release-component.scss']
})
export class SalaryReleaseComponent implements OnInit {
  payrolls: any[] = [];
  filteredPayrolls: any[] = [];
  employees: any[] = [];
  loading = false;

  // Filters — default to current month/year, Processed only
  filterEmployeeId: number | null = null;
  filterMonth: number | null = new Date().getMonth() + 1;
  filterYear:  number | null = new Date().getFullYear();

  months = [
    { value: 1,  name: 'January' },  { value: 2,  name: 'February' },
    { value: 3,  name: 'March' },    { value: 4,  name: 'April' },
    { value: 5,  name: 'May' },      { value: 6,  name: 'June' },
    { value: 7,  name: 'July' },     { value: 8,  name: 'August' },
    { value: 9,  name: 'September' },{ value: 10, name: 'October' },
    { value: 11, name: 'November' }, { value: 12, name: 'December' }
  ];
  years: number[] = [];

  // Bulk email
  selectedIds: Set<number> = new Set();
  bulkEmailing = false;

  // Bank export
  exportingBank = false;

  // YTD modal
  showYtdModal = false;
  ytdLoading   = false;
  ytdData: YtdSummary | null = null;
  ytdYear = new Date().getFullYear();

  // EA Form modal
  showEaModal = false;
  eaLoading   = false;
  eaData: EaForm | null = null;
  eaYear = new Date().getFullYear();

  constructor(
    private http: HttpClient,
    private toast: ToastService,
    public  router: Router,
    private payrollService: PayrollService
  ) {
    const currentYear = new Date().getFullYear();
    for (let y = currentYear - 2; y <= currentYear + 1; y++) this.years.push(y);
  }

  ngOnInit(): void {
    this.loadEmployees();
    this.loadProcessed();
  }

  loadEmployees(): void {
    this.http.get<any[]>(`${environment.apiUrl}/employees`).subscribe({
      next: (data) => this.employees = data.filter(e => e.isActive),
      error: () => {}
    });
  }

  loadProcessed(): void {
    this.loading = true;
    this.http.get<any[]>(`${environment.apiUrl}/payroll?status=Processed`).subscribe({
      next: (data) => { this.payrolls = data; this.applyFilters(); this.loading = false; },
      error: (err) => { if (err.status !== 404) this.toast.error('Error', 'Failed to load payrolls'); this.loading = false; }
    });
  }

  applyFilters(): void {
    this.filteredPayrolls = this.payrolls.filter(p => {
      if (this.filterEmployeeId && p.employeeId !== this.filterEmployeeId) return false;
      if (this.filterMonth      && p.month      !== this.filterMonth)      return false;
      if (this.filterYear       && p.year       !== this.filterYear)       return false;
      return true;
    });
    // clear selections that are no longer visible
    const visibleIds = new Set(this.filteredPayrolls.map(p => p.id));
    this.selectedIds.forEach(id => { if (!visibleIds.has(id)) this.selectedIds.delete(id); });
  }

  onFilterChange(): void { this.applyFilters(); }

  clearFilters(): void {
    this.filterEmployeeId = null;
    this.filterMonth = new Date().getMonth() + 1;
    this.filterYear  = new Date().getFullYear();
    this.applyFilters();
  }

  // ── Checkbox selection ────────────────────────────────────────────────────────
  toggleSelect(id: number): void {
    if (this.selectedIds.has(id)) this.selectedIds.delete(id);
    else this.selectedIds.add(id);
  }

  isSelected(id: number): boolean { return this.selectedIds.has(id); }

  toggleSelectAll(): void {
    const allIds = this.filteredPayrolls.map(p => p.id);
    const allSelected = allIds.every(id => this.selectedIds.has(id)) && allIds.length > 0;
    if (allSelected) { this.selectedIds.clear(); }
    else { allIds.forEach(id => this.selectedIds.add(id)); }
  }

  get allSelected(): boolean {
    const ids = this.filteredPayrolls.map(p => p.id);
    return ids.length > 0 && ids.every(id => this.selectedIds.has(id));
  }

  // ── Email single ──────────────────────────────────────────────────────────────
  emailPayslip(id: number): void {
    this.http.post(`${environment.apiUrl}/payroll/email/${id}`, {}).subscribe({
      next: () => this.toast.success('Sent', 'Payslip emailed successfully.'),
      error: () => this.toast.error('Error', 'Failed to send email.')
    });
  }

  // ── Bulk email ────────────────────────────────────────────────────────────────
  emailSelected(): void {
    if (this.selectedIds.size === 0) {
      this.toast.warning('No Selection', 'Please select at least one record.'); return;
    }
    this.bulkEmailing = true;
    this.http.post<any>(`${environment.apiUrl}/payroll/email-bulk`, Array.from(this.selectedIds)).subscribe({
      next: (res) => {
        this.bulkEmailing = false; this.selectedIds.clear();
        this.toast.success('Emails Sent', `Payslips sent to ${res.sentCount} employee(s).`);
      },
      error: (err) => {
        this.bulkEmailing = false;
        this.toast.error('Error', err?.error?.message ?? 'Failed to send bulk emails.');
      }
    });
  }

  // ── Bank export ───────────────────────────────────────────────────────────────
  downloadBankExport(): void {
    if (!this.filterMonth || !this.filterYear) {
      this.toast.warning('Select Period', 'Please select a month and year before exporting.'); return;
    }
    this.exportingBank = true;
    this.payrollService.downloadBankExport(this.filterMonth, this.filterYear).subscribe({
      next: (blob) => {
        this.exportingBank = false;
        if (blob.type === 'application/json' || blob.type === 'application/json; charset=utf-8') {
          blob.text().then(text => {
            const json = JSON.parse(text);
            this.toast.warning('No Records', json.message || 'No Processed payroll records found for selected period.');
          });
          return;
        }
        const url = window.URL.createObjectURL(blob);
        const a   = document.createElement('a');
        const mon = String(this.filterMonth).padStart(2, '0');
        a.href     = url;
        a.download = `BankPayment_${this.filterYear}-${mon}_Processed.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.exportingBank = false;
        this.toast.error('Export Failed', err?.error?.message || 'Export failed.');
      }
    });
  }

  // ── YTD ───────────────────────────────────────────────────────────────────────
  openYtd(employeeId: number, year: number): void {
    this.ytdYear = year; this.ytdData = null; this.showYtdModal = true; this.ytdLoading = true;
    this.payrollService.getYtd(employeeId, year).subscribe({
      next: (data) => { this.ytdData = data; this.ytdLoading = false; },
      error: (err) => { this.ytdLoading = false; this.toast.error('YTD Error', err?.error?.message || 'Could not load YTD.'); this.showYtdModal = false; }
    });
  }
  closeYtdModal(): void { this.showYtdModal = false; this.ytdData = null; }

  // ── EA Form ───────────────────────────────────────────────────────────────────
  openEaForm(employeeId: number, year: number): void {
    this.eaYear = year; this.eaData = null; this.showEaModal = true; this.eaLoading = true;
    this.payrollService.getEaForm(employeeId, year).subscribe({
      next: (data) => { this.eaData = data; this.eaLoading = false; },
      error: (err) => { this.eaLoading = false; this.toast.error('EA Form Error', err?.error?.message || 'Could not load EA Form.'); this.showEaModal = false; }
    });
  }
  closeEaModal(): void { this.showEaModal = false; this.eaData = null; }
  printEaForm(): void  { window.print(); }

  // ── Helpers ───────────────────────────────────────────────────────────────────
  getMonthName(month: number): string {
    const m = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    return m[month - 1] || '';
  }

  get totalNetSalary(): number { return this.filteredPayrolls.reduce((s, p) => s + p.netSalary, 0); }
  get totalEmployees(): number { return this.filteredPayrolls.length; }
}
