import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { LoadingSpinnerComponent } from './shared/components/loading-spinner/loading-spinner.component';
import { ConfirmModalComponent } from './shared/components/confirm-modal/confirm-modal.component';
import { AuthService } from './core/services/auth.service';
import { SessionTimeoutService } from './core/services/session-timeout.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, LoadingSpinnerComponent, ConfirmModalComponent, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  private authSubscription?: Subscription;
  private warningSubscription?: Subscription;
  
  showSessionWarning = false;

  constructor(
    private authService: AuthService,
    public sessionTimeoutService: SessionTimeoutService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Monitor authentication state and start/stop session timeout accordingly
    this.authSubscription = this.authService.isLoggedIn().subscribe(isLoggedIn => {
      if (isLoggedIn) {
        this.sessionTimeoutService.startMonitoring();
      } else {
        this.sessionTimeoutService.stopMonitoring();
      }
    });

    // Monitor session warning state
    this.warningSubscription = this.sessionTimeoutService.showWarning$.subscribe(show => {
      this.showSessionWarning = show;
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy(): void {
    this.authSubscription?.unsubscribe();
    this.warningSubscription?.unsubscribe();
    this.sessionTimeoutService.stopMonitoring();
  }

  onStayLoggedIn(): void {
    this.sessionTimeoutService.extendSession();
  }
}
