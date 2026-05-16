import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SelfServiceService, EmployeeProfile } from '../../../../core/services/self-service.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-my-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './my-profile-component.html',
  styleUrl: './my-profile-component.scss'
})
export class MyProfileComponent implements OnInit {
  profile: EmployeeProfile | null = null;
  isEditMode = false;
  apiUrl = environment.apiUrl;

  // Editable fields
  editData = {
    phone: '',
    email: '',
    bankId: 0,
    accountNumber: ''
  };

  constructor(
    private selfServiceService: SelfServiceService,
    private loadingService: LoadingService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loadingService.show();
    this.selfServiceService.getMyProfile().subscribe({
      next: (data) => {
        this.profile = data;
        // Initialize edit data
        this.editData = {
          phone: data.phone || '',
          email: data.email || '',
          bankId: data.bankId || 0,
          accountNumber: data.accountNumber || ''
        };
        this.loadingService.hide();
      },
      error: (error) => {
        console.error('Error loading profile:', error);
        this.loadingService.hide();
        this.toast.error('Failed to Load Profile', error.error?.message || 'Please try again or contact HR.');
      }
    });
  }

  enableEditMode(): void {
    this.isEditMode = true;
  }

  cancelEdit(): void {
    this.isEditMode = false;
    // Reset to original values
    if (this.profile) {
      this.editData = {
        phone: this.profile.phone || '',
        email: this.profile.email || '',
        bankId: this.profile.bankId || 0,
        accountNumber: this.profile.accountNumber || ''
      };
    }
  }

  saveProfile(): void {
    this.loadingService.show();
    this.selfServiceService.updateMyProfile(this.editData).subscribe({
      next: (response) => {
        this.loadingService.hide();
        this.toast.success('Profile Updated', 'Your profile information has been updated successfully!');
        this.isEditMode = false;
        this.loadProfile(); // Reload to get fresh data
      },
      error: (error) => {
        console.error('Error updating profile:', error);
        this.loadingService.hide();
        this.toast.error('Update Failed', error.error?.message || 'Failed to update profile. Please try again.');
      }
    });
  }

  getProfilePictureUrl(): string {
    if (!this.profile?.profilePicture) {
      return 'assets/img/default-avatar.svg';
    }
    return `${this.apiUrl}/uploads/profiles/${this.profile.profilePicture}`;
  }
}
