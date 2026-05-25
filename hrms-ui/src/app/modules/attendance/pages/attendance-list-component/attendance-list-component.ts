import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { EmployeeService } from '../../../../core/services/employee.service';
import { AuthService } from '../../../../core/services/auth.service';
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
  totalHours: number;
  dayCount: number;
  submittedAt: string;
  approvedAt?: string;
  approvedBy?: number;
  approverName?: string;
  rejectionReason?: string;
  expanded?: boolean;
}

interface PeriodDay {
  date: string;
  hours: number;
  note: string;
  remarks: string;
  isPublicHoliday: boolean;
  isWeekend: boolean;
}

@Component({
  selector: 'app-attendance-list-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './attendance-list-component.html',
  styleUrl: './attendance-list-component.scss'
})
export class AttendanceListComponent implements OnInit {
  periods: AttendancePeriod[] = [];
  allPeriods: AttendancePeriod[] = [];
  periodDays: Map<number, PeriodDay[]> = new Map();
  employees: any[] = [];
  loading = false;
  loadingDays = false;
  errorMessage = '';
  isAdmin = false;

  // Filters
  selectedEmployeeId: number | null = null;
  selectedMonth: number = new Date().getMonth() + 1;
  selectedYear: number = new Date().getFullYear();
  selectedStatus: string = 'Approved';
  years: number[] = [];
  statuses = ['Approved', 'Rejected', 'All'];

  constructor(
    private http: HttpClient,
    private employeeService: EmployeeService,
    private authService: AuthService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {
    // Generate years from 2020 to current + 1
    const currentYear = new Date().getFullYear();
    for (let year = 2020; year <= currentYear + 1; year++) {
      this.years.push(year);
    }
  }

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.loadEmployees();
    this.loadPeriods();
  }

  loadEmployees(): void {
    this.employeeService.getAll().subscribe({
      next: (data: any[]) => {
        // Filter active employees and remove duplicates by both ID and Name
        const seenIds = new Set<number>();
        const seenNames = new Set<string>();
        
        const uniqueEmployees = data
          .filter(e => e.isActive)
          .filter(e => {
            const normalizedName = e.name.toLowerCase().trim();
            
            // Skip if we've seen this ID or Name before
            if (seenIds.has(e.id) || seenNames.has(normalizedName)) {
              return false;
            }
            
            // Mark as seen
            seenIds.add(e.id);
            seenNames.add(normalizedName);
            return true;
          });
        
        this.employees = uniqueEmployees;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toast.error('Error', 'Failed to load employees');
      }
    });
  }

  loadPeriods(): void {
    this.loading = true;
    let url = `${environment.apiUrl}/attendancemanagement/all`;

    // Add status filter (backend)
    const params: string[] = [];
    if (this.selectedStatus && this.selectedStatus !== 'All') {
      params.push(`status=${this.selectedStatus}`);
    }
    if (this.selectedEmployeeId) {
      params.push(`employeeId=${this.selectedEmployeeId}`);
    }
    if (params.length > 0) {
      url += '?' + params.join('&');
    }

    this.http.get<any[]>(url).subscribe({
      next: (data) => {
        this.allPeriods = data.map(p => ({ ...p, expanded: false }));
        this.applyFilters();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Failed to load attendance periods.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.allPeriods];

    // Filter by month and year (frontend)
    filtered = filtered.filter(p => {
      const date = new Date(p.startDate);
      return date.getMonth() + 1 === this.selectedMonth && date.getFullYear() === this.selectedYear;
    });

    this.periods = filtered;
  }

  onFilterChange(): void {
    this.loadPeriods(); // Reload with backend filters (status, employeeId)
  }

  clearFilters(): void {
    this.selectedEmployeeId = null;
    this.selectedMonth = new Date().getMonth() + 1;
    this.selectedYear = new Date().getFullYear();
    this.selectedStatus = 'Approved';
    this.loadPeriods();
  }

  togglePeriodDetails(period: AttendancePeriod): void {
    period.expanded = !period.expanded;
    
    // Load days if expanding and not already loaded
    if (period.expanded && !this.periodDays.has(period.id)) {
      this.loadPeriodDays(period.id);
    }
  }

  loadPeriodDays(periodId: number): void {
    this.loadingDays = true;
    const url = `${environment.apiUrl}/attendancemanagement/${periodId}/details`;
    
    this.http.get<any>(url).subscribe({
      next: (data) => {
        this.periodDays.set(periodId, data.days || []);
        this.loadingDays = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toast.error('Error', 'Failed to load period details');
        this.loadingDays = false;
      }
    });
  }

  getPeriodDays(periodId: number): PeriodDay[] {
    return this.periodDays.get(periodId) || [];
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      'Approved': 'approved',
      'Rejected': 'rejected',
      'Submitted': 'submitted',
      'Draft': 'draft'
    };
    return map[status] || 'draft';
  }

  getMonthName(month: number): string {
    const months = ['January', 'February', 'March', 'April', 'May', 'June', 
                    'July', 'August', 'September', 'October', 'November', 'December'];
    return months[month - 1];
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatDateTime(dateString: string): string {
    return new Date(dateString).toLocaleString('en-US', { 
      day: '2-digit', 
      month: 'short', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getDayName(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', { weekday: 'short' });
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
