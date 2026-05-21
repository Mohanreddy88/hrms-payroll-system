import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationCenterComponent } from '../notification-center/notification-center.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, NotificationCenterComponent],
  templateUrl: './navbar-component.html',
  styleUrl: './navbar-component.scss'
})
export class NavbarComponent implements OnInit {
  username = 'ADMIN';
  avatarChar = 'A';
  userEmail = '';
  mobileMenuOpen = false;

  @Output() mobileMenuToggle = new EventEmitter<boolean>();

  constructor(
    private authService: AuthService,
    private http: HttpClient,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    const user = this.authService.getUser();
    if (user) {
      this.userEmail = user.username;
      const role = user.role?.toLowerCase();
      
      if (role === 'admin') {
        // For admin, just show "ADMIN"
        this.username = 'ADMIN';
        this.avatarChar = 'A';
      } else {
        // For employees, fetch and show their name
        const employeeId = user.employeeId;
        if (employeeId) {
          this.http.get<any>(`${environment.apiUrl}/employees/${employeeId}`).subscribe({
            next: (employee) => {
              this.username = employee.name.toUpperCase();
              this.avatarChar = employee.name.charAt(0).toUpperCase();
            },
            error: () => {
              // Fallback to email if fetch fails
              this.username = user.username.toUpperCase();
              this.avatarChar = user.username.charAt(0).toUpperCase();
            }
          });
        }
      }
    }
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
    this.mobileMenuToggle.emit(this.mobileMenuOpen);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen = false;
    this.mobileMenuToggle.emit(false);
  }

  logout(): void {
    this.authService.logout();
  }
}
