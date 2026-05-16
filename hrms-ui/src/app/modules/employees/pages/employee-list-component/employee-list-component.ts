import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { EmployeeService } from '../../../../core/services/employee.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Employee } from '../../../../core/models/employee.model';
import { ConfirmModalComponent } from '../../../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [CommonModule, ConfirmModalComponent, RouterLink],
  templateUrl: './employee-list-component.html',
  styleUrl: './employee-list-component.scss'
})
export class EmployeeListComponent implements OnInit {
  employees: Employee[] = [];
  loading = false;
  isAdmin = false;

  // Confirmation modal state
  showConfirmModal = false;
  confirmModalTitle = '';
  confirmModalMessage = '';
  confirmModalVariant: 'danger' | 'warning' | 'info' = 'danger';
  pendingDeleteId: number | null = null;
  pendingDeleteName = '';

  constructor(
    private employeeService: EmployeeService,
    private authService: AuthService,
    private router: Router,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.loading = true;
    // Admin sees all records (active + inactive), User sees only their own
    this.employeeService.getAll(this.isAdmin).subscribe({
      next: (data) => {
        this.employees = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toast.error('Load Failed', 'Could not load employees.');
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  addEmployee(): void { this.router.navigate(['/employees/add']); }
  editEmployee(id: number): void { this.router.navigate(['/employees/edit', id]); }

  deleteEmployee(id: number, name: string): void {
    this.pendingDeleteId = id;
    this.pendingDeleteName = name;
    this.confirmModalTitle = 'Delete Employee';
    this.confirmModalMessage = `Are you sure you want to delete "${name}"? This action cannot be undone.`;
    this.confirmModalVariant = 'danger';
    this.showConfirmModal = true;
  }

  onConfirmDelete(): void {
    if (!this.pendingDeleteId) return;
    this.employeeService.delete(this.pendingDeleteId).subscribe({
      next: () => {
        this.toast.success('Employee Deleted', `${this.pendingDeleteName} has been removed.`);
        this.pendingDeleteId = null;
        this.pendingDeleteName = '';
        this.loadEmployees();
      },
      error: (err: any) => {
        this.toast.error('Delete Failed', err?.error?.message ?? 'Failed to delete.');
        this.pendingDeleteId = null;
        this.pendingDeleteName = '';
      }
    });
  }

  onCancelDelete(): void {
    this.pendingDeleteId = null;
    this.pendingDeleteName = '';
  }
}
