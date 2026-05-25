import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SelfServiceService, Timesheet } from '../../../../core/services/self-service.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmModalComponent } from '../../../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-my-timesheet',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './my-timesheet-component.html',
  styleUrl: './my-timesheet-component.scss'
})
export class MyTimesheetComponent implements OnInit {
  timesheets: Timesheet[] = [];
  selectedYear = new Date().getFullYear();
  selectedMonth: number | null = null;
  years: number[] = [];
  months = [
    { value: null, label: 'All Months' },
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' }
  ];

  // Upload section
  showUploadSection = false;
  uploadMonth: number = new Date().getMonth() + 1;
  uploadYear: number = new Date().getFullYear();
  selectedFile: File | null = null;
  uploadPreview: any = null;

  // Confirmation modal
  showConfirmModal = false;
  confirmTimesheetId: number | null = null;
  confirmTimesheetMonth: string = '';

  constructor(
    private selfServiceService: SelfServiceService,
    private loadingService: LoadingService,
    private toast: ToastService
  ,
    private router: Router
  ) {
    // Generate last 3 years
    const currentYear = new Date().getFullYear();
    for (let i = 0; i < 3; i++) {
      this.years.push(currentYear - i);
    }
  }

  ngOnInit(): void {
    this.loadTimesheets();
  }

  loadTimesheets(): void {
    this.loadingService.show();
    this.selfServiceService.getMyTimesheets(this.selectedYear, this.selectedMonth || undefined)
      .subscribe({
        next: (data) => {
          this.timesheets = data;
          this.loadingService.hide();
        },
        error: (error) => {
          console.error('Error loading timesheets:', error);
          this.loadingService.hide();
          this.toast.error('Failed to Load Timesheets', error.error?.message || 'Please try again.');
        }
      });
  }

  onFilterChange(): void {
    this.loadTimesheets();
  }

  openSubmitConfirmation(timesheetId: number, monthName: string): void {
    this.confirmTimesheetId = timesheetId;
    this.confirmTimesheetMonth = monthName;
    this.showConfirmModal = true;
  }

  closeConfirmModal(): void {
    this.showConfirmModal = false;
    this.confirmTimesheetId = null;
    this.confirmTimesheetMonth = '';
  }

  onSubmitConfirmed(): void {
    if (this.confirmTimesheetId === null) {
      this.toast.error('Error', 'No timesheet selected');
      this.showConfirmModal = false;
      return;
    }

    this.loadingService.show();
    this.selfServiceService.submitTimesheet(this.confirmTimesheetId)
      .subscribe({
        next: (response) => {
          this.loadingService.hide();
          this.showConfirmModal = false;
          this.confirmTimesheetId = null;
          this.confirmTimesheetMonth = '';
          this.toast.success('Timesheet Submitted!', 'Your timesheet has been submitted for approval.');
          this.loadTimesheets();
        },
        error: (error) => {
          console.error('Error submitting timesheet:', error);
          this.loadingService.hide();
          this.showConfirmModal = false;
          this.confirmTimesheetId = null;
          this.confirmTimesheetMonth = '';
          this.toast.error('Submission Failed', error.error?.message || 'Failed to submit timesheet.');
        }
      });
  }

  getStatusClass(status: string): string {
    const statusMap: { [key: string]: string } = {
      'Draft': 'status-draft',
      'Submitted': 'status-submitted',
      'Approved': 'status-approved',
      'Rejected': 'status-rejected'
    };
    return statusMap[status] || '';
  }

  getStatusIcon(status: string): string {
    const iconMap: { [key: string]: string } = {
      'Draft': 'bi-pencil-square',
      'Submitted': 'bi-clock-fill',
      'Approved': 'bi-check-circle-fill',
      'Rejected': 'bi-x-circle-fill'
    };
    return iconMap[status] || 'bi-question-circle-fill';
  }

  getAttendancePercentage(timesheet: Timesheet): number {
    if (timesheet.totalWorkingDays === 0) return 0;
    return (timesheet.totalPresent / timesheet.totalWorkingDays) * 100;
  }

  getAttendanceColor(percentage: number): string {
    if (percentage >= 90) return '#10b981'; // Green
    if (percentage >= 75) return '#f59e0b'; // Orange
    return '#ef4444'; // Red
  }

  toggleUploadSection(): void {
    this.showUploadSection = !this.showUploadSection;
    if (this.showUploadSection) {
      this.resetUploadForm();
    }
  }

  resetUploadForm(): void {
    this.uploadMonth = new Date().getMonth() + 1;
    this.uploadYear = new Date().getFullYear();
    this.selectedFile = null;
    this.uploadPreview = null;
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'application/vnd.ms-excel'];
    if (!allowedTypes.includes(file.type)) {
      this.toast.error('Invalid File Type', 'Please upload an Excel file (.xlsx or .xls)');
      event.target.value = '';
      return;
    }

    // Validate file size (max 5MB)
    if (file.size > 5 * 1024 * 1024) {
      this.toast.error('File Too Large', 'File size must be less than 5MB');
      event.target.value = '';
      return;
    }

    this.selectedFile = file;
    this.uploadPreview = {
      name: file.name,
      size: (file.size / 1024).toFixed(2) + ' KB',
      type: file.name.split('.').pop()?.toUpperCase()
    };
  }

  triggerFileInput(): void {
    const fileInput = document.getElementById('timesheetFileInput') as HTMLInputElement;
    if (fileInput) {
      fileInput.click();
    }
  }

  clearSelectedFile(): void {
    this.selectedFile = null;
    this.uploadPreview = null;
    // Reset file input
    const fileInput = document.getElementById('timesheetFileInput') as HTMLInputElement;
    if (fileInput) fileInput.value = '';
  }

  downloadTemplate(): void {
    this.loadingService.show();
    this.selfServiceService.downloadTimesheetTemplate(this.uploadMonth, this.uploadYear)
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `Timesheet_Template_${this.uploadYear}_${this.uploadMonth.toString().padStart(2, '0')}.xlsx`;
          link.click();
          window.URL.revokeObjectURL(url);
          this.loadingService.hide();
          this.toast.success('Download Started', 'Your timesheet template is downloading...');
        },
        error: (error) => {
          console.error('Error downloading template:', error);
          this.loadingService.hide();
          this.toast.error('Download Failed', error.error?.message || 'Failed to download template');
        }
      });
  }

  uploadTimesheet(): void {
    if (!this.selectedFile) {
      this.toast.warning('No File Selected', 'Please select a file to upload');
      return;
    }

    this.loadingService.show();
    this.selfServiceService.uploadTimesheetExcel(this.selectedFile, this.uploadMonth, this.uploadYear)
      .subscribe({
        next: (response) => {
          this.loadingService.hide();
          this.toast.success('Upload Successful!', response.message || 'Your timesheet has been uploaded and calculated successfully.');
          this.showUploadSection = false;
          this.resetUploadForm();
          this.loadTimesheets();
        },
        error: (error) => {
          console.error('Error uploading timesheet:', error);
          this.loadingService.hide();
          this.toast.error('Upload Failed', error.error?.message || 'Failed to upload timesheet. Please check the file and try again.');
        }
      });
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
