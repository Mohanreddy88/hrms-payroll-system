import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../../core/services/user.service';
import { ToastService } from '../../../../core/services/toast.service';
import { AppUser } from '../../../../core/models/user.model';
import { ConfirmModalComponent } from '../../../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './user-list-component.html',
  styleUrl: './user-list-component.scss'
})
export class UserListComponent implements OnInit {
  users: AppUser[] = [];
  loading = false;
  saving  = false;
  roleOptions = ['Admin', 'Employee', 'User'];
  showModal = false;
  isEdit    = false;
  editId: number | null = null;
  modalError = '';

  // Confirmation modal state
  showConfirmModal = false;
  confirmModalTitle = '';
  confirmModalMessage = '';
  confirmModalVariant: 'danger' | 'warning' | 'info' = 'danger';
  pendingDeleteUser: AppUser | null = null;

  form = { username: '', password: '', role: 'Admin', isActive: true };

  constructor(
    private userService: UserService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ,
    private router: Router
  ) {}

  ngOnInit(): void { this.loadUsers(); }

  loadUsers(): void {
    this.loading = true;
    this.userService.getAll().subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        // Only show error toast for actual errors (not empty data)
        if (err.status !== 404) {
          this.toast.error('Load Failed', 'Could not load users.');
        }
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openCreate(): void {
    this.isEdit = false; this.editId = null;
    this.form = { username: '', password: '', role: 'Admin', isActive: true };
    this.showModal = true; this.modalError = '';
  }

  openEdit(u: AppUser): void {
    this.isEdit = true; this.editId = u.id;
    this.form = { username: u.username, password: '', role: u.role, isActive: u.isActive };
    this.showModal = true; this.modalError = '';
  }

  closeModal(): void { this.showModal = false; this.modalError = ''; }

  save(): void {
    if (!this.form.username.trim()) return;
    this.saving = true;

    if (this.isEdit && this.editId !== null) {
      this.userService.update(this.editId, {
        username: this.form.username, role: this.form.role,
        isActive: this.form.isActive, password: this.form.password || undefined
      }).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success('User Updated', `${this.form.username} updated.`);
          this.closeModal(); this.loadUsers();
        },
        error: (err: any) => { this.saving = false; this.modalError = err?.error?.message ?? 'Failed to update.'; }
      });
    } else {
      if (!this.form.password.trim()) { this.modalError = 'Password is required.'; this.saving = false; return; }
      this.userService.create({
        username: this.form.username, password: this.form.password,
        role: this.form.role, isActive: this.form.isActive
      }).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success('User Created', `${this.form.username} created.`);
          this.closeModal(); this.loadUsers();
        },
        error: (err: any) => { this.saving = false; this.modalError = err?.error?.message ?? 'Failed to create.'; }
      });
    }
  }

  deleteUser(u: AppUser): void {
    this.pendingDeleteUser = u;
    this.confirmModalTitle = 'Delete User';
    this.confirmModalMessage = `Are you sure you want to delete user "${u.username}"? This action cannot be undone.`;
    this.confirmModalVariant = 'danger';
    this.showConfirmModal = true;
  }

  onConfirmDelete(): void {
    if (!this.pendingDeleteUser) return;
    this.userService.delete(this.pendingDeleteUser.id).subscribe({
      next: () => {
        this.toast.success('Deleted', `${this.pendingDeleteUser!.username} removed.`);
        this.pendingDeleteUser = null;
        this.loadUsers();
      },
      error: (err: any) => {
        this.toast.error('Delete Failed', err?.error?.message ?? 'Failed.');
        this.pendingDeleteUser = null;
      }
    });
  }

  onCancelDelete(): void {
    this.pendingDeleteUser = null;
  }

  getRoleClass(role: string): string { 
    if (role === 'Admin') return 'role-admin';
    if (role === 'Employee') return 'role-employee';
    return 'role-user';
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
