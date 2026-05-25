import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SelfServiceService, LeaveBalanceResponse, LeaveRequest, LeaveType, LeaveRequestCreate } from '../../../../core/services/self-service.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';
import { InputModalComponent } from '../../../../shared/components/input-modal/input-modal.component';

@Component({
  selector: 'app-my-leave',
  standalone: true,
  imports: [CommonModule, FormsModule, InputModalComponent],
  templateUrl: './my-leave-component.html',
  styleUrl: './my-leave-component.scss'
})
export class MyLeaveComponent implements OnInit {
  leaveBalance: LeaveBalanceResponse | null = null;
  leaveRequests: LeaveRequest[] = [];
  selectedYear = new Date().getFullYear();
  selectedStatus = 'All';
  years: number[] = [];
  statusOptions = ['All', 'Pending', 'Approved', 'Rejected', 'Cancelled'];

  activeTab: 'balance' | 'requests' | 'newRequest' = 'balance';
  
  // New Leave Request Form
  leaveTypes: LeaveType[] = [];
  newLeaveRequest: LeaveRequestCreate = {
    leaveTypeId: 0,
    startDate: '',
    endDate: '',
    reason: ''
  };
  
  // Input modal state
  showInputModal = false;
  inputModalTitle = '';
  inputModalMessage = '';
  pendingCancelId: number | null = null;

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
    this.loadLeaveBalance();
    this.loadLeaveRequests();
    this.loadLeaveTypes();
  }

  loadLeaveBalance(): void {
    this.loadingService.show();
    this.selfServiceService.getMyLeaveBalance(this.selectedYear)
      .subscribe({
        next: (data) => {
          this.leaveBalance = data;
          this.loadingService.hide();
        },
        error: (error) => {
          console.error('Error loading leave balance:', error);
          this.loadingService.hide();
          this.toast.error('Failed to Load Leave Balance', error.error?.message || 'Please try again.');
        }
      });
  }

  loadLeaveRequests(): void {
    this.loadingService.show();
    const status = this.selectedStatus === 'All' ? undefined : this.selectedStatus;
    this.selfServiceService.getMyLeaveRequests(status)
      .subscribe({
        next: (data) => {
          this.leaveRequests = data;
          this.loadingService.hide();
        },
        error: (error) => {
          console.error('Error loading leave requests:', error);
          this.loadingService.hide();
          this.toast.error('Failed to Load Leave Requests', error.error?.message || 'Please try again.');
        }
      });
  }

  onYearChange(): void {
    this.loadLeaveBalance();
  }

  onStatusChange(): void {
    this.loadLeaveRequests();
  }

  setTab(tab: 'balance' | 'requests' | 'newRequest'): void {
    this.activeTab = tab;
  }

  loadLeaveTypes(): void {
    this.selfServiceService.getActiveLeaveTypes()
      .subscribe({
        next: (data) => {
          this.leaveTypes = data;
        },
        error: (error) => {
          console.error('Error loading leave types:', error);
          this.toast.error('Failed to Load Leave Types', error.error?.message || 'Please try again.');
        }
      });
  }

  submitLeaveRequest(): void {
    // Validation
    if (!this.newLeaveRequest.leaveTypeId) {
      this.toast.warning('Validation Error', 'Please select a leave type');
      return;
    }
    if (!this.newLeaveRequest.startDate || !this.newLeaveRequest.endDate) {
      this.toast.warning('Validation Error', 'Please select start and end dates');
      return;
    }
    if (new Date(this.newLeaveRequest.endDate) < new Date(this.newLeaveRequest.startDate)) {
      this.toast.warning('Validation Error', 'End date cannot be before start date');
      return;
    }
    if (!this.newLeaveRequest.reason || this.newLeaveRequest.reason.trim().length < 5) {
      this.toast.warning('Validation Error', 'Please provide a reason (minimum 5 characters)');
      return;
    }

    this.loadingService.show();
    this.selfServiceService.submitLeaveRequest(this.newLeaveRequest)
      .subscribe({
        next: (response) => {
          this.loadingService.hide();
          this.toast.success('Leave Request Submitted!', 'Your leave request has been submitted successfully and is pending approval.');
          // Reset form
          this.newLeaveRequest = {
            leaveTypeId: 0,
            startDate: '',
            endDate: '',
            reason: ''
          };
          // Refresh data
          this.loadLeaveRequests();
          this.loadLeaveBalance();
          // Switch to requests tab
          this.activeTab = 'requests';
        },
        error: (error) => {
          console.error('Error submitting leave request:', error);
          this.loadingService.hide();
          this.toast.error('Submission Failed', error.error?.message || 'Failed to submit leave request. Please try again.');
        }
      });
  }

  cancelRequest(requestId: number): void {
    const request = this.leaveRequests.find(r => r.id === requestId);
    if (!request) return;
    
    if (!this.canCancelRequest(request.status)) {
      this.toast.warning('Cannot Cancel', 'Cannot cancel a leave request that has been approved');
      return;
    }
    
    this.pendingCancelId = requestId;
    this.inputModalTitle = 'Cancel Leave Request';
    this.inputModalMessage = 'Please enter cancellation reason:';
    this.showInputModal = true;
  }

  onCancelConfirmed(reason: string): void {
    if (!this.pendingCancelId) return;

    this.loadingService.show();
    this.selfServiceService.cancelLeaveRequest(this.pendingCancelId, reason)
      .subscribe({
        next: () => {
          this.loadingService.hide();
          this.toast.success('Request Cancelled', 'Your leave request has been cancelled successfully.');
          this.loadLeaveRequests();
          this.loadLeaveBalance();
          this.closeInputModal();
        },
        error: (error) => {
          console.error('Error cancelling request:', error);
          this.loadingService.hide();
          this.toast.error('Cancellation Failed', error.error?.message || 'Failed to cancel leave request.');
          this.closeInputModal();
        }
      });
  }

  closeInputModal(): void {
    this.showInputModal = false;
    this.pendingCancelId = null;
  }

  getStatusClass(status: string): string {
    const statusMap: { [key: string]: string } = {
      'Pending': 'status-pending',
      'Approved': 'status-approved',
      'Rejected': 'status-rejected',
      'Cancelled': 'status-cancelled'
    };
    return statusMap[status] || '';
  }

  getStatusIcon(status: string): string {
    const iconMap: { [key: string]: string } = {
      'Pending': 'bi-clock-fill',
      'Approved': 'bi-check-circle-fill',
      'Rejected': 'bi-x-circle-fill',
      'Cancelled': 'bi-slash-circle-fill'
    };
    return iconMap[status] || 'bi-question-circle-fill';
  }

  getBalancePercentage(balance: any): number {
    if (balance.totalDays === 0) return 0;
    return (balance.balanceDays / balance.totalDays) * 100;
  }

  getBalanceColor(percentage: number): string {
    if (percentage >= 70) return '#10b981'; // Green
    if (percentage >= 40) return '#f59e0b'; // Orange
    return '#ef4444'; // Red
  }

  canCancelRequest(status: string): boolean {
    return status !== 'Approved' && status !== 'Cancelled';
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
