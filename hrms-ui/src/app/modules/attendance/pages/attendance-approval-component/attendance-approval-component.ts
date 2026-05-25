import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';

interface AttendancePeriod {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeEmail: string;
  startDate: string;
  endDate: string;
  status: string;
  submittedAt: string;
  totalHours: number;
  leaveCount: number;
  alCount: number;
  elCount: number;
  mcCount: number;
  dayCount: number;
  rejectionReason?: string;
}

interface ApprovedLeave {
  id: number;
  leaveType: string;
  leaveTypeCode: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  approvedOn: string;
  approvalRemarks: string;
  status: string;
}

interface PendingLeave {
  id: number;
  leaveType: string;
  leaveTypeCode: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  requestedOn: string;
  status: string;
}

interface PeriodDay {
  id: number;
  date: string;
  hours: number;
  note: string;
  remarks: string;
  isPublicHoliday: boolean;
  isWeekend: boolean;
}

interface PeriodDetails {
  id: number;
  employeeName: string;
  employeeEmail: string;
  startDate: string;
  endDate: string;
  status: string;
  days: PeriodDay[];
  summary: {
    totalHours: number;
    workingDays: number;
    alCount: number;
    elCount: number;
    mcCount: number;
  };
  approvedLeaves: ApprovedLeave[];
  pendingLeaves: PendingLeave[];
  hasPendingLeaves: boolean;
}

@Component({
  selector: 'app-attendance-approval',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './attendance-approval-component.html',
  styleUrl: './attendance-approval-component.scss'
})
export class AttendanceApprovalComponent implements OnInit {
  allPeriods: AttendancePeriod[] = [];
  pendingPeriods: AttendancePeriod[] = [];
  selectedPeriod: PeriodDetails | null = null;
  showRejectModal: boolean = false;
  showApproveModal: boolean = false;
  rejectionReason: string = '';
  notifying: boolean = false;
  creatingLeave: boolean = false;
  onBehalfLeaveType: string = 'AL';
  
  // Filters
  filterStatus: string = 'Submitted';
  filterEmployeeId: number | null = null;
  filterMonth: number | null = null;
  filterYear: number = new Date().getFullYear();
  employees: any[] = [];
  years: number[] = [];
  months = [
    { value: 1, name: 'January' }, { value: 2, name: 'February' }, { value: 3, name: 'March' },
    { value: 4, name: 'April' }, { value: 5, name: 'May' }, { value: 6, name: 'June' },
    { value: 7, name: 'July' }, { value: 8, name: 'August' }, { value: 9, name: 'September' },
    { value: 10, name: 'October' }, { value: 11, name: 'November' }, { value: 12, name: 'December' }
  ];
  
  constructor(
    private http: HttpClient,
    private loadingService: LoadingService,
    private toast: ToastService
  ,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeFilters();
    this.loadEmployees();
    this.loadAllPeriods();
  }

  initializeFilters(): void {
    const currentYear = new Date().getFullYear();
    for (let year = currentYear - 2; year <= currentYear + 1; year++) {
      this.years.push(year);
    }
  }

  loadEmployees(): void {
    const url = `${environment.apiUrl}/employees`;
    this.http.get<any[]>(url).subscribe({
      next: (data) => {
        const seenIds = new Set<number>();
        const seenNames = new Set<string>();
        this.employees = data
          .filter(e => e.isActive)
          .filter(e => {
            const normalizedName = e.name.toLowerCase().trim();
            if (seenIds.has(e.id) || seenNames.has(normalizedName)) {
              return false;
            }
            seenIds.add(e.id);
            seenNames.add(normalizedName);
            return true;
          });
      },
      error: () => {
        this.toast.error('Error', 'Failed to load employees');
      }
    });
  }

  loadAllPeriods(): void {
    this.loadingService.show();
    let url = `${environment.apiUrl}/attendancemanagement/all`;
    
    // Build query params
    const params: string[] = [];
    if (this.filterStatus) params.push(`status=${this.filterStatus}`);
    if (this.filterEmployeeId) params.push(`employeeId=${this.filterEmployeeId}`);
    if (params.length > 0) url += '?' + params.join('&');
    
    this.http.get<AttendancePeriod[]>(url).subscribe({
      next: (data) => {
        this.allPeriods = data;
        this.applyFilters();
        this.loadingService.hide();
      },
      error: (error) => {
        console.error('Error loading periods:', error);
        this.loadingService.hide();
        this.toast.error('Failed to Load', 'Could not load attendance periods');
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.allPeriods];

    // Filter by month and year
    if (this.filterMonth || this.filterYear) {
      filtered = filtered.filter(p => {
        const startDate = new Date(p.startDate);
        const matchMonth = this.filterMonth ? startDate.getMonth() + 1 === this.filterMonth : true;
        const matchYear = this.filterYear ? startDate.getFullYear() === this.filterYear : true;
        return matchMonth && matchYear;
      });
    }

    this.pendingPeriods = filtered;
  }

  onFilterChange(): void {
    this.loadAllPeriods();
  }

  clearFilters(): void {
    this.filterStatus = 'Submitted';
    this.filterEmployeeId = null;
    this.filterMonth = null;
    this.filterYear = new Date().getFullYear();
    this.loadAllPeriods();
  }

  viewPeriodDetails(periodId: number): void {
    this.loadingService.show();
    const url = `${environment.apiUrl}/attendancemanagement/${periodId}/details`;
    
    this.http.get<PeriodDetails>(url).subscribe({
      next: (data) => {
        this.selectedPeriod = data;
        this.loadingService.hide();
      },
      error: (error) => {
        console.error('Error loading period details:', error);
        this.loadingService.hide();
        this.toast.error('Failed to Load', 'Could not load period details');
      }
    });
  }

  closeDetails(): void {
    this.selectedPeriod = null;
  }

  openApproveModal(): void {
    this.showApproveModal = true;
  }

  closeApproveModal(): void {
    this.showApproveModal = false;
  }

  confirmApprove(): void {
    if (!this.selectedPeriod) return;

    this.loadingService.show();
    const url = `${environment.apiUrl}/attendancemanagement/${this.selectedPeriod.id}/approve`;
    
    this.http.post(url, {}).subscribe({
      next: () => {
        this.toast.success('Approved', 'Attendance period approved successfully. Email sent to employee.');
        this.loadingService.hide();
        this.closeApproveModal();
        this.closeDetails();
        this.loadAllPeriods();
      },
      error: (error) => {
        console.error('Error approving period:', error);
        this.loadingService.hide();
        this.toast.error('Approval Failed', error.error?.message || 'Could not approve period');
      }
    });
  }

  openRejectModal(periodId: number): void {
    this.rejectionReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.rejectionReason = '';
  }

  confirmReject(): void {
    if (!this.rejectionReason.trim()) {
      this.toast.warning('Reason Required', 'Please provide a rejection reason');
      return;
    }

    if (!this.selectedPeriod) return;

    this.loadingService.show();
    const url = `${environment.apiUrl}/attendancemanagement/${this.selectedPeriod.id}/reject`;
    
    this.http.post(url, { rejectionReason: this.rejectionReason }).subscribe({
      next: () => {
        this.toast.success('Rejected', 'Attendance period rejected. Email sent to employee.');
        this.loadingService.hide();
        this.closeRejectModal();
        this.closeDetails();
        this.loadAllPeriods();
      },
      error: (error) => {
        console.error('Error rejecting period:', error);
        this.loadingService.hide();
        this.toast.error('Rejection Failed', error.error?.message || 'Could not reject period');
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', { 
      day: '2-digit', 
      month: 'short', 
      year: 'numeric' 
    });
  }

  getDayName(dateString: string): string {
    const date = new Date(dateString);
    const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    return days[date.getDay()];
  }

  isWeekend(dateString: string): boolean {
    const date = new Date(dateString);
    return date.getDay() === 0 || date.getDay() === 6;
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }

  isDayPendingLeave(dateString: string): boolean {
    if (!this.selectedPeriod || !this.selectedPeriod.pendingLeaves) return false;
    return this.selectedPeriod.pendingLeaves.some(leave => {
      const leaveStart = new Date(leave.startDate);
      const leaveEnd   = new Date(leave.endDate);
      const dayDate    = new Date(dateString);
      return dayDate >= leaveStart && dayDate <= leaveEnd;
    });
  }

  /** Returns days where employee set a leave note (AL/EL/MC) but has NO
   *  Any 0-hour working day (not weekend, not holiday) with no approved/pending
   *  leave request = unresolved — admin must act before approving attendance. */
  getMissingLeaveDays(): PeriodDay[] {
    if (!this.selectedPeriod) return [];

    const allLeaves = [
      ...(this.selectedPeriod.approvedLeaves || []),
      ...(this.selectedPeriod.pendingLeaves  || [])
    ];

    return this.selectedPeriod.days.filter(d => {
      // Only check working days (not weekends, not public holidays)
      if (d.isWeekend)       return false;
      if (d.isPublicHoliday) return false;
      // Must have 0 hours
      if (d.hours !== 0)     return false;

      // Check if any leave request (approved or pending) covers this date
      const dayDate = new Date(d.date);
      const coveredByLeave = allLeaves.some(leave => {
        const leaveStart = new Date(leave.startDate);
        const leaveEnd   = new Date(leave.endDate);
        return dayDate >= leaveStart && dayDate <= leaveEnd;
      });

      return !coveredByLeave; // 0-hour working day with NO leave request
    });
  }

  /** Returns a readable summary of leave notes on missing days e.g. "AL, MC" */
  getNotesForMissingDays(): string {
    const notes = [...new Set(this.getMissingLeaveDays().map(d => d.note).filter(n => n))];
    return notes.join(', ') || 'leave';
  }

  /** Sends an email to the employee listing the missing leave days */
  notifyEmployee(): void {
    if (!this.selectedPeriod) return;
    this.notifying = true;
    this.http.post(`${environment.apiUrl}/attendancemanagement/${this.selectedPeriod.id}/notify-missing-leaves`, {})
      .subscribe({
        next: (res: any) => {
          this.notifying = false;
          this.toast.success('Notified', `Email sent to ${this.selectedPeriod!.employeeEmail} for ${res.missingDays} missing day(s)`);
        },
        error: (err) => {
          this.notifying = false;
          this.toast.error('Failed', err?.error?.message || 'Could not send notification');
        }
      });
  }

  /** Admin creates and auto-approves a leave request on behalf of the employee */
  createLeaveOnBehalf(): void {
    if (!this.selectedPeriod) return;
    const missing = this.getMissingLeaveDays();
    if (missing.length === 0) {
      this.toast.warning('No Missing Days', 'All days already have leave notes or hours');
      return;
    }

    // Use first and last missing day as the date range
    const startDate = missing[0].date;
    const endDate   = missing[missing.length - 1].date;

    this.creatingLeave = true;
    this.http.post(`${environment.apiUrl}/attendancemanagement/${this.selectedPeriod.id}/create-leave-on-behalf`, {
      leaveTypeCode: this.onBehalfLeaveType,
      startDate,
      endDate,
      reason: `Leave taken — recorded by admin on behalf of employee`
    }).subscribe({
      next: (res: any) => {
        this.creatingLeave = false;
        this.toast.success('Leave Created', `${res.leaveType} (${res.totalDays} day(s)) created and approved`);
        // Reload period details to reflect new leave
        this.viewPeriodDetails(this.selectedPeriod!.id);
      },
      error: (err) => {
        this.creatingLeave = false;
        this.toast.error('Failed', err?.error?.message || 'Could not create leave');
      }
    });
  }
}
