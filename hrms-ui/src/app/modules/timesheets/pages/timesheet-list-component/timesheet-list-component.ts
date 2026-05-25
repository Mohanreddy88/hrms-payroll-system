import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TimesheetService, Timesheet } from '../../../../core/services/timesheet.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Employee } from '../../../../core/models/employee.model';
import { InputModalComponent } from '../../../../shared/components/input-modal/input-modal.component';
import { ConfirmModalComponent } from '../../../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-timesheet-list',
  standalone: true,
  imports: [CommonModule, FormsModule, InputModalComponent, ConfirmModalComponent],
  templateUrl: './timesheet-list-component.html',
  styleUrl: './timesheet-list-component.scss'
})
export class TimesheetListComponent implements OnInit {
  timesheets: Timesheet[] = [];
  employees: Employee[] = [];
  loading = false;
  generating = false;
  isAdmin = false;
  currentUserId = 0;

  // Modal state
  showModal = false;
  selectedTimesheet: Timesheet | null = null;
  
  // Input modal state
  showInputModal = false;
  inputModalTitle = '';
  inputModalMessage = '';
  inputModalRequired = false;
  inputModalType: 'text' | 'textarea' = 'text';
  pendingApprovalId: number | null = null;
  pendingRejectionId: number | null = null;
  
  // Confirm modal state
  showConfirmModal = false;
  confirmModalTitle = '';
  confirmModalMessage = '';
  confirmModalVariant: 'danger' | 'warning' | 'info' = 'warning';
  pendingGenerateAll = false;
  pendingDeleteId: number | null = null;

  // Filters
  filterYear: number = new Date().getFullYear();
  filterMonth: number | null = null;
  filterEmployeeId: number | null = null;
  filterStatus: string | null = null;

  // Generate form
  generateEmployeeId: number | null = null;
  generateMonth: number = new Date().getMonth() + 1;
  generateYear: number = new Date().getFullYear();

  months = [
    { value: 1, label: 'January' }, { value: 2, label: 'February' },
    { value: 3, label: 'March' }, { value: 4, label: 'April' },
    { value: 5, label: 'May' }, { value: 6, label: 'June' },
    { value: 7, label: 'July' }, { value: 8, label: 'August' },
    { value: 9, label: 'September' }, { value: 10, label: 'October' },
    { value: 11, label: 'November' }, { value: 12, label: 'December' }
  ];

  constructor(
    private timesheetService: TimesheetService,
    private employeeService: EmployeeService,
    private authService: AuthService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    // Note: currentUserId is 0 since auth doesn't store employee ID
    // Timesheet approval uses backend to determine approver
    this.loadEmployees();
    this.loadTimesheets();
  }

  loadEmployees(): void {
    this.employeeService.getActive().subscribe({
      next: (data) => {
        this.employees = data;
        this.cdr.detectChanges();
      }
    });
  }

  loadTimesheets(): void {
    this.loading = true;
    this.timesheetService.getAll(this.filterYear, this.filterMonth || undefined).subscribe({
      next: (data) => {
        this.timesheets = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toast.error('Load Failed', 'Could not load timesheets.');
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get filteredTimesheets(): Timesheet[] {
    return this.timesheets.filter(t => {
      if (this.filterEmployeeId && t.employeeId !== this.filterEmployeeId) return false;
      if (this.filterStatus && t.status !== this.filterStatus) return false;
      return true;
    });
  }

  applyFilters(): void {
    // Reload data from server when filters change
    this.loadTimesheets();
  }

  generateTimesheet(): void {
    if (!this.generateEmployeeId) {
      this.toast.warning('Required', 'Please select an employee');
      return;
    }

    this.generating = true;
    this.timesheetService.generate({
      employeeId: this.generateEmployeeId,
      month: this.generateMonth,
      year: this.generateYear
    }).subscribe({
      next: () => {
        this.toast.success('Generated', 'Timesheet generated successfully');
        this.generating = false;
        // Update filters to match generated period to show new timesheet
        this.filterMonth = this.generateMonth;
        this.filterYear = this.generateYear;
        this.loadTimesheets();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toast.error('Failed', err?.error?.message || 'Failed to generate timesheet');
        this.generating = false;
        this.cdr.detectChanges();
      }
    });
  }

  generateAll(): void {
    this.generating = true;
    this.timesheetService.generateAll(this.generateMonth, this.generateYear).subscribe({
      next: (res) => {
        const msg = `Generated: ${res.generated ?? 0}, Skipped: ${res.skipped ?? 0}` +
                    (res.errors?.length ? `, Errors: ${res.errors.length}` : '');
        this.toast.success('Timesheets Processed', msg);
        this.generating = false;
        // Update filters to match generated period to show new timesheets
        this.filterMonth = this.generateMonth;
        this.filterYear = this.generateYear;
        this.loadTimesheets();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toast.error('Failed', err?.error?.message || 'Failed to generate timesheets');
        this.generating = false;
        this.cdr.detectChanges();
      }
    });
  }

  submitTimesheet(id: number): void {
    this.timesheetService.submit(id).subscribe({
      next: () => {
        this.toast.success('Submitted', 'Timesheet submitted for approval');
        this.loadTimesheets();
      },
      error: (err) => {
        this.toast.error('Failed', err?.error?.message || 'Failed to submit');
      }
    });
  }

  approveTimesheet(id: number): void {
    this.pendingApprovalId = id;
    this.inputModalTitle = 'Approve Timesheet';
    this.inputModalMessage = 'Enter approval remarks (optional):';
    this.inputModalRequired = false;
    this.inputModalType = 'textarea';
    this.showInputModal = true;
  }

  onApprovalConfirmed(remarks: string): void {
    if (this.pendingApprovalId === null) return;
    
    const finalRemarks = remarks.trim() || 'Approved';
    this.timesheetService.approve(this.pendingApprovalId, this.currentUserId, finalRemarks).subscribe({
      next: () => {
        this.toast.success('Approved', 'Timesheet approved');
        this.loadTimesheets();
        this.closeInputModal();
      },
      error: (err) => {
        this.toast.error('Failed', err?.error?.message || 'Failed to approve');
        this.closeInputModal();
      }
    });
  }

  rejectTimesheet(id: number): void {
    this.pendingRejectionId = id;
    this.inputModalTitle = 'Reject Timesheet';
    this.inputModalMessage = 'Enter rejection reason (required):';
    this.inputModalRequired = true;
    this.inputModalType = 'textarea';
    this.showInputModal = true;
  }

  onRejectionConfirmed(remarks: string): void {
    if (this.pendingRejectionId === null) return;
    
    this.timesheetService.reject(this.pendingRejectionId, this.currentUserId, remarks).subscribe({
      next: () => {
        this.toast.warning('Rejected', 'Timesheet rejected');
        this.loadTimesheets();
        this.closeInputModal();
      },
      error: (err) => {
        this.toast.error('Failed', err?.error?.message || 'Failed to reject');
        this.closeInputModal();
      }
    });
  }

  onInputModalConfirmed(value: string): void {
    if (this.pendingApprovalId !== null) {
      this.onApprovalConfirmed(value);
    } else if (this.pendingRejectionId !== null) {
      this.onRejectionConfirmed(value);
    }
  }

  closeInputModal(): void {
    this.showInputModal = false;
    this.pendingApprovalId = null;
    this.pendingRejectionId = null;
  }

  viewDetails(timesheet: Timesheet): void {
    this.selectedTimesheet = timesheet;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.selectedTimesheet = null;
  }

  generateForAllEmployees(): void {
    this.pendingGenerateAll = true;
    this.confirmModalTitle = 'Generate All Timesheets';
    this.confirmModalMessage = 'This will generate timesheets for all active employees. Continue?';
    this.confirmModalVariant = 'info';
    this.showConfirmModal = true;
  }

  deleteTimesheet(id: number): void {
    this.pendingDeleteId = id;
    this.confirmModalTitle = 'Delete Timesheet';
    this.confirmModalMessage = 'Are you sure you want to delete this timesheet? This action cannot be undone.';
    this.confirmModalVariant = 'danger';
    this.showConfirmModal = true;
  }

  onConfirmModalConfirmed(): void {
    if (this.pendingGenerateAll) {
      this.generateAll();
      this.pendingGenerateAll = false;
    } else if (this.pendingDeleteId) {
      this.performDelete(this.pendingDeleteId);
      this.pendingDeleteId = null;
    }
    this.showConfirmModal = false;
  }

  onConfirmModalCancelled(): void {
    this.showConfirmModal = false;
    this.pendingGenerateAll = false;
    this.pendingDeleteId = null;
  }

  performDelete(id: number): void {
    this.timesheetService.delete(id).subscribe({
      next: () => {
        this.toast.success('Deleted', 'Timesheet deleted');
        this.loadTimesheets();
      },
      error: (err) => {
        this.toast.error('Failed', err?.error?.message || 'Failed to delete');
      }
    });
  }

  emailTimesheet(timesheet: Timesheet): void {
    this.timesheetService.emailTimesheet(timesheet.id).subscribe({
      next: () => {
        this.toast.success('Email Sent', `Timesheet with Excel attachment emailed to ${timesheet.employeeName}`);
        this.closeModal();
      },
      error: (err) => {
        console.error('Error emailing timesheet:', err);
        this.toast.error('Email Failed', err?.error?.message || 'Failed to send timesheet email');
      }
    });
  }

  getMonthName(month: number): string {
    return this.months.find(m => m.value === month)?.label || '';
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Approved': return 'status-approved';
      case 'Submitted': return 'status-submitted';
      case 'Rejected': return 'status-rejected';
      default: return 'status-draft';
    }
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
