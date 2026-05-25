import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-payroll-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payroll-list-component.html',
  styleUrls: ['./payroll-list-component.scss']
})
export class PayrollListComponent implements OnInit {
  payrolls: any[] = [];
  filteredPayrolls: any[] = [];
  employees: any[] = [];
  loading = false;
  selectedPayroll: any = null;

  // Filters
  filterStatus: string | null = null;
  filterEmployeeId: number | null = null;
  filterMonth: number | null = new Date().getMonth() + 1;
  filterYear: number | null = new Date().getFullYear();

  statuses = ['Draft', 'Pending', 'Approved', 'Rejected', 'Processed'];

  months = [
    { value: 1, name: 'January' },  { value: 2,  name: 'February' },
    { value: 3, name: 'March' },    { value: 4,  name: 'April' },
    { value: 5, name: 'May' },      { value: 6,  name: 'June' },
    { value: 7, name: 'July' },     { value: 8,  name: 'August' },
    { value: 9, name: 'September' },{ value: 10, name: 'October' },
    { value: 11, name: 'November' },{ value: 12, name: 'December' }
  ];

  years: number[] = [];

  // Modals
  showDetailModal  = false;
  showApproveModal = false;
  showRejectModal  = false;
  showDeleteModal  = false;
  selectedPayrollId: number | null = null;
  rejectionReason  = '';
  approvalRemarks  = '';

  // Adjustments
  showAddAdjustment = false;
  savingAdj = false;
  adjError  = '';
  adjForm   = { adjustmentType: '', description: '', amount: 0 };

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private toast: ToastService,
    public router: Router
  ) {
    const currentYear = new Date().getFullYear();
    for (let year = currentYear - 2; year <= currentYear + 1; year++) {
      this.years.push(year);
    }
  }

  ngOnInit(): void {
    this.loadPayrolls();
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.http.get<any[]>(`${environment.apiUrl}/employees`).subscribe({
      next: (data) => this.employees = data.filter(e => e.isActive),
      error: (err) => { if (err.status !== 404) this.toast.error('Error', 'Failed to load employees'); }
    });
  }

  loadPayrolls(): void {
    this.loading = true;
    this.http.get<any[]>(`${environment.apiUrl}/payroll`).subscribe({
      next: (data) => { this.payrolls = data; this.applyFilters(); this.loading = false; },
      error: (err) => { if (err.status !== 404) this.toast.error('Error', 'Failed to load payrolls'); this.loading = false; }
    });
  }

  applyFilters(): void {
    this.filteredPayrolls = this.payrolls.filter(p => {
      if (this.filterStatus     && p.status     !== this.filterStatus)     return false;
      if (this.filterEmployeeId && p.employeeId !== this.filterEmployeeId) return false;
      if (this.filterMonth      && p.month      !== this.filterMonth)      return false;
      if (this.filterYear       && p.year       !== this.filterYear)       return false;
      return true;
    });
  }

  onFilterChange(): void { this.applyFilters(); }

  clearFilters(): void {
    this.filterStatus     = null;
    this.filterEmployeeId = null;
    this.filterMonth      = new Date().getMonth() + 1;
    this.filterYear       = new Date().getFullYear();
    this.applyFilters();
  }

  navigateToGenerate(): void { this.router.navigate(['/payroll/generate']); }

  viewDetails(id: number): void {
    this.http.get(`${environment.apiUrl}/payroll/${id}`).subscribe({
      next: (data) => { this.selectedPayroll = data; this.showDetailModal = true; },
      error: () => this.toast.error('Error', 'Failed to load payroll details')
    });
  }

  closeDetailModal(): void {
    this.showDetailModal  = false;
    this.selectedPayroll  = null;
    this.showAddAdjustment = false;
    this.adjError = '';
  }

  openAddAdjustment(): void  { this.adjForm = { adjustmentType: '', description: '', amount: 0 }; this.adjError = ''; this.showAddAdjustment = true; }
  cancelAddAdjustment(): void { this.showAddAdjustment = false; this.adjError = ''; }

  saveAdjustment(): void {
    if (!this.selectedPayroll) return;
    if (!this.adjForm.adjustmentType || !this.adjForm.description.trim() || this.adjForm.amount <= 0) {
      this.adjError = 'All fields are required and amount must be positive.'; return;
    }
    this.savingAdj = true; this.adjError = '';
    this.http.post<any>(`${environment.apiUrl}/payroll/${this.selectedPayroll.id}/adjustment`, {
      adjustmentType: this.adjForm.adjustmentType,
      description:    this.adjForm.description.trim(),
      amount:         this.adjForm.amount
    }).subscribe({
      next: () => {
        this.savingAdj = false; this.showAddAdjustment = false;
        this.toast.success('Adjustment Added', `${this.adjForm.adjustmentType} of RM ${this.adjForm.amount} added.`);
        this.viewDetails(this.selectedPayroll.id);
        this.loadPayrolls();
      },
      error: (err: any) => { this.savingAdj = false; this.adjError = err?.error?.message ?? 'Failed to add adjustment.'; }
    });
  }

  deleteAdjustment(adjustmentId: number): void {
    if (!this.selectedPayroll) return;
    this.http.delete<any>(`${environment.apiUrl}/payroll/${this.selectedPayroll.id}/adjustment/${adjustmentId}`).subscribe({
      next: () => { this.toast.success('Removed', 'Adjustment removed.'); this.viewDetails(this.selectedPayroll.id); this.loadPayrolls(); },
      error: (err: any) => this.toast.error('Error', err?.error?.message ?? 'Failed to remove adjustment.')
    });
  }

  openApproveModal(id: number): void  { this.selectedPayrollId = id; this.showApproveModal = true; }
  closeApproveModal(): void { this.showApproveModal = false; this.selectedPayrollId = null; }

  confirmApprove(): void {
    if (!this.selectedPayrollId) return;
    this.http.post(`${environment.apiUrl}/payroll/${this.selectedPayrollId}/approve`, {}).subscribe({
      next: () => { this.toast.success('Approved', 'Payroll approved successfully'); this.closeApproveModal(); this.loadPayrolls(); },
      error: () => this.toast.error('Error', 'Failed to approve payroll')
    });
  }

  openRejectModal(id: number): void  { this.selectedPayrollId = id; this.rejectionReason = ''; this.showRejectModal = true; }
  closeRejectModal(): void { this.showRejectModal = false; this.selectedPayrollId = null; this.rejectionReason = ''; }

  confirmReject(): void {
    if (!this.selectedPayrollId || !this.rejectionReason.trim()) return;
    this.http.post(`${environment.apiUrl}/payroll/${this.selectedPayrollId}/reject`, { rejectionReason: this.rejectionReason }).subscribe({
      next: () => { this.toast.success('Rejected', 'Payroll rejected'); this.closeRejectModal(); this.loadPayrolls(); },
      error: () => this.toast.error('Error', 'Failed to reject payroll')
    });
  }

  processPayroll(id: number): void {
    this.http.post(`${environment.apiUrl}/payroll/${id}/process`, {}).subscribe({
      next: () => { this.toast.success('Processed', 'Payroll marked as Processed. Go to Salary Release to send payment.'); this.loadPayrolls(); },
      error: () => this.toast.error('Error', 'Failed to process payroll')
    });
  }

  openDeleteModal(id: number): void  { this.selectedPayrollId = id; this.showDeleteModal = true; }
  closeDeleteModal(): void { this.showDeleteModal = false; this.selectedPayrollId = null; }

  confirmDelete(): void {
    if (!this.selectedPayrollId) return;
    this.http.delete(`${environment.apiUrl}/payroll/${this.selectedPayrollId}`).subscribe({
      next: () => { this.toast.success('Deleted', 'Payroll deleted'); this.closeDeleteModal(); this.loadPayrolls(); },
      error: () => this.toast.error('Error', 'Failed to delete payroll')
    });
  }

  getMonthName(month: number): string {
    const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    return months[month - 1] || '';
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      'Draft': 'draft', 'Pending': 'pending', 'Approved': 'approved',
      'Rejected': 'rejected', 'Processed': 'processed'
    };
    return map[status] || 'draft';
  }
}
