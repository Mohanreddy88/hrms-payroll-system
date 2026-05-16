import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SelfServiceService } from '../../../../core/services/self-service.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './change-password-component.html',
  styleUrl: './change-password-component.scss'
})
export class ChangePasswordComponent {
  formData = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  showOldPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  constructor(
    private selfServiceService: SelfServiceService,
    private loadingService: LoadingService,
    private toast: ToastService,
    private router: Router
  ) {}

  changePassword(): void {
    // Validations
    if (!this.formData.oldPassword || !this.formData.newPassword || !this.formData.confirmPassword) {
      this.toast.warning('Missing Fields', 'All fields are required');
      return;
    }

    if (this.formData.newPassword.length < 6) {
      this.toast.warning('Weak Password', 'New password must be at least 6 characters long');
      return;
    }

    if (this.formData.newPassword !== this.formData.confirmPassword) {
      this.toast.error('Password Mismatch', 'New password and confirmation do not match');
      return;
    }

    this.loadingService.show();
    this.selfServiceService.changePassword(this.formData).subscribe({
      next: () => {
        this.toast.success('Password Changed', 'Your password has been updated successfully!');
        this.loadingService.hide();
        this.router.navigate(['/self-service/my-profile']);
      },
      error: (error) => {
        console.error('Error changing password:', error);
        this.loadingService.hide();
        this.toast.error('Password Change Failed', error.error?.message || 'Please try again.');
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/self-service/my-profile']);
  }

  togglePasswordVisibility(field: string): void {
    if (field === 'old') this.showOldPassword = !this.showOldPassword;
    if (field === 'new') this.showNewPassword = !this.showNewPassword;
    if (field === 'confirm') this.showConfirmPassword = !this.showConfirmPassword;
  }
}
