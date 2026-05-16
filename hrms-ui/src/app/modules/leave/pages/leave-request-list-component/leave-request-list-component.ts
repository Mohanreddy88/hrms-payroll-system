import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-leave-request-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-request-list-component.html',
  styleUrls: ['./leave-request-list-component.scss']
})
export class LeaveRequestListComponent implements OnInit {
  leaveRequests: any[] = [];
  filteredRequests: any[] = [];
  employees: any[] = [];
  isAdmin = false;
  loading = false;

  // Filters
  filterStatus: string | null = null;
  filterYear: number | null = new Date().getFullYear();
  filterEmployeeId: number | null = null;
  filterMonth: number | null = null;

  months = [
    { value: 1, name: 'January' },
    { value: 2, name: 'February' },
    { value: 3, name: 'March' },
    { value: 4, name: 'April' },
    { value: 5, name: 'May' },
    { value: 6, name: 'June' },
    { value: 7, name: 'July' },
    { value: 8, name: 'August' },
    { value: 9, name: 'September' },
    { value: 10, name: 'October' },
    { value: 11, name: 'November' },
    { value: 12, name: 'December' }
  ];

  years: number[] = [];

  // Modals
  showApprovalModal = false;
  showCancelModal = false;
  showDeleteModal = false;
  selectedRequest: any = null;
  requestToCancel: any = null;
  requestToDelete: any = null;
  approvalAction: 'approve' | 'reject' = 'approve';
  approvalRemarks = '';
  rejectionReason = '';
  cancelReason = '';

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private toast: ToastService,
    private router: Router
  ) {
    const currentYear = new Date().getFullYear();
    for (let year = currentYear - 5; year <= currentYear + 1; year++) {
      this.years.push(year);
    }
  }

  ngOnInit(): void {
    this.isAdmin = this.authService.getRole() === 'Admin';
    this.loadEmployees();
    this.loadRequests();
  }

  loadEmployees(): void {
    this.http.get<any[]>(`${environment.apiUrl}/employees`).subscribe({
      next: (data) => {
        const seenIds = new Set<number>();
        const seenNames = new Set<string>();
        this.employees = data.filter(e => e.isActive).filter(e => {
          const normalizedName = e.name.toLowerCase().trim();
          if (seenIds.has(e.id) || seenNames.has(normalizedName)) return false;
          seenIds.add(e.id);
          seenNames.add(normalizedName);
          return true;
        });
      },
      error: () => this.toast.error('Error', 'Failed to load employees')
    });
  }

  loadRequests(): void {
    this.loading = true;
    const url = this.isAdmin
      ? `${environment.apiUrl}/leavemanagement/requests`
      : `${environment.apiUrl}/leavemanagement/my-requests`;

    this.http.get<any[]>(url).subscribe({
      next: (data) => {
        this.leaveRequests = data;
        this.applyFilters();
        this.loading = false;
      },
      error: () => {
        this.toast.error('Error', 'Failed to load leave requests');
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.filteredRequests = this.leaveRequests.filter(req => {
      if (this.filterStatus && req.status !== this.filterStatus) return false;
      if (this.filterYear) {
        const year = new Date(req.startDate).getFullYear();
        if (year !== this.filterYear) return false;
      }
      if (this.filterEmployeeId && req.employeeId !== this.filterEmployeeId) return false;
      if (this.filterMonth) {
        const month = new Date(req.startDate).getMonth() + 1;
        if (month !== this.filterMonth) return false;
      }
      return true;
    });
  }

  clearFilters(): void {
    this.filterStatus = null;
    this.filterYear = new Date().getFullYear();
    this.filterEmployeeId = null;
    this.filterMonth = null;
    this.applyFilters();
  }

  newRequest(): void {
    this.router.navigate(['/leave/requests/new']);
  }

  openApprovalModal(request: any, action: 'approve' | 'reject'): void {
    this.selectedRequest = request;
    this.approvalAction = action;
    this.approvalRemarks = '';
    this.rejectionReason = '';
    this.showApprovalModal = true;
  }

  closeApprovalModal(): void {
    this.showApprovalModal = false;
    this.selectedRequest = null;
  }

  confirmApproval(): void {
    if (!this.selectedRequest) return;

    const url = this.approvalAction === 'approve'
      ? `${environment.apiUrl}/leavemanagement/requests/${this.selectedRequest.id}/approve`
      : `${environment.apiUrl}/leavemanagement/requests/${this.selectedRequest.id}/reject`;

    const payload = this.approvalAction === 'approve'
      ? { approvalRemarks: this.approvalRemarks }
      : { rejectionReason: this.rejectionReason };

    this.http.post(url, payload).subscribe({
      next: () => {
        this.toast.success('Success', `Leave request ${this.approvalAction}d successfully`);
        this.closeApprovalModal();
        this.loadRequests();
      },
      error: () => this.toast.error('Error', `Failed to ${this.approvalAction} request`)
    });
  }

  cancelRequest(request: any): void {
    this.selectedRequest = request;
    this.requestToCancel = request;
    this.showCancelModal = true;
  }

  closeCancelModal(): void {
    this.showCancelModal = false;
    this.selectedRequest = null;
    this.requestToCancel = null;
  }

  confirmCancel(): void {
    if (!this.selectedRequest) return;

    this.http.post(`${environment.apiUrl}/leavemanagement/requests/${this.selectedRequest.id}/cancel`, {}).subscribe({
      next: () => {
        this.toast.success('Success', 'Leave request cancelled');
        this.closeCancelModal();
        this.loadRequests();
      },
      error: () => this.toast.error('Error', 'Failed to cancel request')
    });
  }

  deleteRequest(request: any): void {
    this.selectedRequest = request;
    this.requestToDelete = request;
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.selectedRequest = null;
    this.requestToDelete = null;
  }

  confirmDelete(): void {
    if (!this.selectedRequest) return;

    this.http.delete(`${environment.apiUrl}/leavemanagement/requests/${this.selectedRequest.id}`).subscribe({
      next: () => {
        this.toast.success('Success', 'Leave request deleted');
        this.closeDeleteModal();
        this.loadRequests();
      },
      error: () => this.toast.error('Error', 'Failed to delete request')
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      'Pending': 'pending',
      'Approved': 'approved',
      'Rejected': 'rejected',
      'Cancelled': 'cancelled'
    };
    return map[status] || 'pending';
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
