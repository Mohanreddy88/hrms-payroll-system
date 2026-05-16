import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DepartmentService, Department } from '../../../../core/services/department.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmModalComponent } from '../../../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './department-list-component.html',
  styleUrls: ['./department-list-component.scss']
})
export class DepartmentListComponent implements OnInit {
  departments: Department[] = [];
  loading = false;
  saving = false;
  showModal = false;
  isEdit = false;
  editId: number | null = null;
  modalError = '';

  // Confirmation modal state
  showConfirmModal = false;
  confirmModalTitle = '';
  confirmModalMessage = '';
  confirmModalVariant: 'danger' | 'warning' | 'info' = 'danger';
  pendingDeleteDepartment: Department | null = null;

  form = { name: '', description: '', isActive: true };

  constructor(
    private departmentService: DepartmentService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDepartments();
  }

  loadDepartments(): void {
    this.loading = true;
    this.departmentService.getAll().subscribe({
      next: (data) => {
        this.departments = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Department load error:', err);
        // Only show error toast for actual errors (not empty data)
        if (err.status !== 404) {
          const errorMsg = err?.error?.message || err?.message || 'Could not load departments. Please check if API is running.';
          this.toast.error('Load Failed', errorMsg);
        }
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openCreate(): void {
    this.isEdit = false;
    this.editId = null;
    this.form = { name: '', description: '', isActive: true };
    this.showModal = true;
    this.modalError = '';
  }

  openEdit(dept: Department): void {
    this.isEdit = true;
    this.editId = dept.id;
    this.form = { name: dept.name, description: dept.description, isActive: dept.isActive };
    this.showModal = true;
    this.modalError = '';
  }

  closeModal(): void {
    this.showModal = false;
    this.modalError = '';
  }

  save(): void {
    if (!this.form.name.trim()) {
      this.modalError = 'Department name is required.';
      return;
    }

    this.saving = true;

    if (this.isEdit && this.editId !== null) {
      this.departmentService.update(this.editId, {
        name: this.form.name.trim(),
        description: this.form.description.trim(),
        isActive: this.form.isActive
      }).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success('Department Updated', `${this.form.name} has been updated.`);
          this.closeModal();
          this.loadDepartments();
        },
        error: (err: any) => {
          this.saving = false;
          this.modalError = err?.error?.message ?? 'Failed to update department.';
        }
      });
    } else {
      this.departmentService.create({
        name: this.form.name.trim(),
        description: this.form.description.trim(),
        isActive: this.form.isActive
      }).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success('Department Created', `${this.form.name} has been added.`);
          this.closeModal();
          this.loadDepartments();
        },
        error: (err: any) => {
          this.saving = false;
          this.modalError = err?.error?.message ?? 'Failed to create department.';
        }
      });
    }
  }

  deleteDepartment(dept: Department): void {
    this.pendingDeleteDepartment = dept;
    this.confirmModalTitle = 'Delete Department';
    this.confirmModalMessage = `Are you sure you want to delete "${dept.name}"? This action cannot be undone.`;
    this.confirmModalVariant = 'danger';
    this.showConfirmModal = true;
  }

  onConfirmDelete(): void {
    if (!this.pendingDeleteDepartment) return;
    this.departmentService.delete(this.pendingDeleteDepartment.id).subscribe({
      next: () => {
        this.toast.success('Deleted', `${this.pendingDeleteDepartment!.name} has been removed.`);
        this.pendingDeleteDepartment = null;
        this.loadDepartments();
      },
      error: (err: any) => {
        this.toast.error('Delete Failed', err?.error?.message ?? 'Failed to delete department.');
        this.pendingDeleteDepartment = null;
      }
    });
  }

  onCancelDelete(): void {
    this.pendingDeleteDepartment = null;
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
