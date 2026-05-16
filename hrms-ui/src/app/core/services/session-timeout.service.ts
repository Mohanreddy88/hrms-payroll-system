import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SessionTimeoutService {
  private readonly TIMEOUT_DURATION = 15 * 60 * 1000; // 15 minutes (900 seconds)
  private readonly WARNING_TIME = 10 * 60 * 1000; // Show warning at 10 minutes (600 seconds) - gives 5 min to respond
  private timeoutId: any;
  private warningTimeoutId: any;
  private isBrowser = typeof window !== 'undefined';
  private lastActivity: number = Date.now();
  private checkInterval: any;

  // Observable to show/hide warning modal
  public showWarning$ = new BehaviorSubject<boolean>(false);

  // Events that indicate user activity
  private readonly ACTIVITY_EVENTS = [
    'mousedown',
    'keypress',
    'scroll',
    'touchstart',
    'click'
  ];

  constructor(
    private router: Router,
    private authService: AuthService,
    private ngZone: NgZone
  ) {}

  /**
   * Start monitoring user activity and session timeout
   */
  startMonitoring(): void {
    if (!this.isBrowser) return;

    // Clear any existing monitoring
    this.stopMonitoring();

    // Record initial activity
    this.lastActivity = Date.now();

    // Set up activity listeners - just update lastActivity timestamp
    this.ACTIVITY_EVENTS.forEach(event => {
      window.addEventListener(event, this.handleActivity, true);
    });

    // Start checking for timeout every second
    this.startTimeoutCheck();
  }

  /**
   * Handle user activity - update timestamp
   */
  private handleActivity = (): void => {
    this.lastActivity = Date.now();
  }

  /**
   * Start interval to check for timeout
   */
  private startTimeoutCheck(): void {
    this.ngZone.runOutsideAngular(() => {
      this.checkInterval = setInterval(() => {
        this.ngZone.run(() => {
          this.checkTimeout();
        });
      }, 1000); // Check every second
    });
  }

  /**
   * Check if session should timeout or show warning
   */
  private checkTimeout(): void {
    const now = Date.now();
    const inactiveTime = now - this.lastActivity;

    // Show warning if inactive for WARNING_TIME
    if (inactiveTime >= this.WARNING_TIME && !this.showWarning$.value) {
      this.showWarningModal();
    }

    // Logout if inactive for TIMEOUT_DURATION
    if (inactiveTime >= this.TIMEOUT_DURATION) {
      this.handleSessionExpiry();
    }
  }

  /**
   * Stop monitoring and clear all listeners
   */
  stopMonitoring(): void {
    if (!this.isBrowser) return;

    // Clear interval
    if (this.checkInterval) {
      clearInterval(this.checkInterval);
      this.checkInterval = null;
    }

    // Hide warning modal if shown
    this.showWarning$.next(false);

    // Remove event listeners
    this.ACTIVITY_EVENTS.forEach(event => {
      window.removeEventListener(event, this.handleActivity, true);
    });
  }

  /**
   * Show warning modal to user
   */
  private showWarningModal(): void {
    this.showWarning$.next(true);
  }

  /**
   * User clicked "Stay Logged In" - extend session
   */
  public extendSession(): void {
    this.showWarning$.next(false);
    this.lastActivity = Date.now(); // Reset activity timestamp
  }

  /**
   * Handle session expiry - logout and redirect
   */
  private handleSessionExpiry(): void {
    // Store session expiry flag in localStorage before clearing session
    if (this.isBrowser) {
      localStorage.setItem('session_expired', 'true');
    }
    
    // Clear session data
    this.authService.clearSession();
    
    // Stop monitoring
    this.stopMonitoring();
    
    // Navigate to login
    this.router.navigate(['/login']);
  }
}
