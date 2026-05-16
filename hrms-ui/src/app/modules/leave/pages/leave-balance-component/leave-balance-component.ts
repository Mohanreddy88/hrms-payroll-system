import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { LeaveService, LeaveBalance } from '../../../../core/services/leave.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-leave-balance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-balance-component.html',
  styleUrls: ['./leave-balance-component.scss']
})
export class LeaveBalanceComponent implements OnInit {
  balances: LeaveBalance[] = [];
  employees: any[] = [];
  loading = false;
  currentYear = new Date().getFullYear();
  isAdmin = false;

  // Admin filters
  selectedEmployeeId: number | null = null;
  selectedYear: number = this.currentYear;
  years: number[] = [];

  constructor(
    private leaveService: LeaveService,
    private authService: AuthService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private http: HttpClient
  ) {
    // Generate year options (current year - 2 to current year + 1)
    for (let year = this.currentYear - 2; year <= this.currentYear + 1; year++) {
      this.years.push(year);
    }
  }

  ngOnInit(): void {
    this.isAdmin = this.authService.getRole() === 'Admin';
    
    if (this.isAdmin) {
      this.loadEmployees();
    } else {
      this.loadBalances();
    }
  }

  loadEmployees(): void {
    this.http.get<any[]>(`${environment.apiUrl}/employees/active`).subscribe({
      next: (data) => {
        this.employees = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading employees:', err);
        this.toast.error('Load Failed', 'Could not load employees');
      }
    });
  }

  loadBalances(): void {
    if (this.isAdmin && !this.selectedEmployeeId) {
      // Admin must select an employee first
      this.balances = [];
      return;
    }

    this.loading = true;
    console.log('Loading leave balances...');
    
    // For admin: use selected employee, for employee: use placeholder ID 1
    // TODO: Implement proper user-to-employee mapping via backend
    const employeeId = this.isAdmin ? this.selectedEmployeeId! : 1;
    
    this.leaveService.getBalance(employeeId, this.selectedYear).subscribe({
      next: (data: any) => {
        console.log('Leave balances received:', data);
        this.balances = data;
        this.loading = false;
        this.cdr.detectChanges();
        
        if (data.length === 0) {
          this.toast.info('No Leave Balances', 'Leave balances need to be initialized. Contact your HR administrator.');
        }
      },
      error: (err: any) => {
        console.error('Error loading leave balances:', err);
        const message = err.status === 404 
          ? 'Employee record not found. Please ensure you have active employees in the system.'
          : err.error?.message || 'Failed to load leave balances. The employee may not have initialized balances yet.';
        this.toast.error('Load Failed', message);
        this.loading = false;
      }
    });
  }

  onFilterChange(): void {
    if (this.selectedEmployeeId) {
      this.loadBalances();
    }
  }

  getProgressPercentage(balance: LeaveBalance): number {
    if (balance.totalDays === 0) return 0;
    return (balance.usedDays / balance.totalDays) * 100;
  }

  getProgressColor(percentage: number): string {
    if (percentage >= 80) return 'danger';
    if (percentage >= 50) return 'warning';
    return 'success';
  }

  getLeaveIcon(code: string): string {
    const icons: Record<string, string> = {
      'AL': 'calendar-check',
      'ML': 'heart-pulse',
      'EL': 'exclamation-circle',
      'CL': 'people',
      'MTL': 'person-heart',
      'PTL': 'person-check',
      'UL': 'calendar-x',
      'SL': 'book',
      'HL': 'moon-stars',
      'RL': 'arrow-repeat'
    };
    return icons[code] || 'calendar';
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
