import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LeaveService, LeaveBalance } from '../../../../core/services/leave.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-leave-balance',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './leave-balance-component.html',
  styleUrls: ['./leave-balance-component.scss']
})
export class LeaveBalanceComponent implements OnInit {
  balances: LeaveBalance[] = [];
  loading = false;
  currentYear = new Date().getFullYear();
  isAdmin = false;

  constructor(
    private leaveService: LeaveService,
    private authService: AuthService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.getRole() === 'Admin';
    this.loadBalances();
  }

  loadBalances(): void {
    this.loading = true;
    console.log('Loading leave balances...');
    
    // Note: Using placeholder employee ID 1
    // TODO: Implement proper user-to-employee mapping via backend
    const employeeId = 1;
    
    this.leaveService.getBalance(employeeId, this.currentYear).subscribe({
      next: (data: any) => {
        console.log('Leave balances received:', data);
        console.log('Data type:', typeof data);
        console.log('Is array:', Array.isArray(data));
        
        this.balances = data;
        this.loading = false;
        this.cdr.detectChanges();
        
        console.log('Balances assigned:', this.balances);
        console.log('Loading set to false');
        console.log('Change detection triggered');
        
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
      },
      complete: () => {
        console.log('Leave balance loading complete');
      }
    });
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
