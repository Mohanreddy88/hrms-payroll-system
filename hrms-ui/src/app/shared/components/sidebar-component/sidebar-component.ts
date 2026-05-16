import { Component, OnInit, Input } from '@angular/core';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-sidebar-component',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './sidebar-component.html',
  styleUrl: './sidebar-component.scss'
})
export class SidebarComponent implements OnInit {
  isAdmin = false;
  masterMenuOpen = false;
  payrollMenuOpen = false;
  attendanceMenuOpen = false;
  leaveMenuOpen = false;
  reportsMenuOpen = false;
  selfServiceMenuOpen = false;

  @Input() isMobileMenuOpen = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const user = this.authService.getUser();
    this.isAdmin = user?.role === 'Admin';

    // Auto-expand menu based on current route
    this.updateMenuStateFromRoute(this.router.url);

    // Listen to route changes and keep menus open
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.updateMenuStateFromRoute(event.url);
      });
  }

  private updateMenuStateFromRoute(url: string): void {
    // Keep Payroll menu open if on any payroll route
    if (url.includes('/payroll')) {
      this.payrollMenuOpen = true;
    }
    // Keep Attendance menu open if on any attendance route
    if (url.includes('/attendance')) {
      this.attendanceMenuOpen = true;
    }
    // Keep Master menu open if on any master route
    if (url.includes('/master')) {
      this.masterMenuOpen = true;
    }
    // Keep Leave menu open if on any leave route
    if (url.includes('/leave')) {
      this.leaveMenuOpen = true;
    }
    // Keep Reports menu open if on any reports route
    if (url.includes('/reports')) {
      this.reportsMenuOpen = true;
    }
    // Keep Self-Service menu open if on any self-service route
    if (url.includes('/self-service')) {
      this.selfServiceMenuOpen = true;
    }
  }

  toggleMasterMenu(): void {
    this.masterMenuOpen = !this.masterMenuOpen;
  }

  togglePayrollMenu(): void {
    this.payrollMenuOpen = !this.payrollMenuOpen;
  }

  toggleAttendanceMenu(): void {
    this.attendanceMenuOpen = !this.attendanceMenuOpen;
  }

  toggleLeaveMenu(): void {
    this.leaveMenuOpen = !this.leaveMenuOpen;
  }

  toggleReportsMenu(): void {
    this.reportsMenuOpen = !this.reportsMenuOpen;
  }

  toggleSelfServiceMenu(): void {
    this.selfServiceMenuOpen = !this.selfServiceMenuOpen;
  }
}
