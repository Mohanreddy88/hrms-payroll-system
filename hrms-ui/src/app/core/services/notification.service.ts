import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, interval } from 'rxjs';
import { distinctUntilChanged } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface Notification {
  id: number;
  title: string;
  message: string;
  type: string;
  link: string | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private unreadCountSubject = new BehaviorSubject<number>(0);
  // distinctUntilChanged ensures badge only re-renders when count actually changes
  public unreadCount$ = this.unreadCountSubject.asObservable().pipe(distinctUntilChanged());

  private notificationsSubject = new BehaviorSubject<Notification[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  constructor(private http: HttpClient, private auth: AuthService) {
    // Poll every 30 seconds — only when user is logged in
    interval(30000).subscribe(() => {
      if (this.auth.isLoggedIn()) {
        this.refreshUnreadCount();
      }
    });
  }

  getNotifications(unreadOnly = false): Observable<Notification[]> {
    return this.http.get<Notification[]>(
      `${environment.apiUrl}/notification${unreadOnly ? '?unreadOnly=true' : ''}`
    );
  }

  getUnreadCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${environment.apiUrl}/notification/unread-count`);
  }

  markAsRead(id: number): Observable<any> {
    return this.http.post(`${environment.apiUrl}/notification/${id}/mark-read`, {});
  }

  markAllAsRead(): Observable<any> {
    return this.http.post(`${environment.apiUrl}/notification/mark-all-read`, {});
  }

  deleteNotification(id: number): Observable<any> {
    return this.http.delete(`${environment.apiUrl}/notification/${id}`);
  }

  refreshNotifications(): void {
    this.getNotifications().subscribe({
      next: (notifications) => {
        this.notificationsSubject.next(notifications);
        const unreadCount = notifications.filter(n => !n.isRead).length;
        this.unreadCountSubject.next(unreadCount);
      },
      error: (err) => { if (err.status !== 0) console.error('Failed to refresh notifications:', err); }
    });
  }

  refreshUnreadCount(): void {
    if (!this.auth.isLoggedIn()) return;
    this.getUnreadCount().subscribe({
      next: (data) => {
        // Only emit if count changed — prevents badge re-render on every poll
        if (data.count !== this.unreadCountSubject.getValue()) {
          this.unreadCountSubject.next(data.count);
        }
      },
      error: (err) => {
        // status 0 = API unreachable (restarting/offline) — suppress console noise
        if (err.status !== 0) {
          console.error('Failed to refresh unread count:', err);
        }
      }
    });
  }

  getNotificationIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'leave_approved': 'check-circle-fill',
      'leave_rejected': 'x-circle-fill',
      'attendance_approved': 'calendar-check-fill',
      'attendance_rejected': 'calendar-x-fill',
      'payslip_generated': 'file-earmark-text-fill',
      'system': 'info-circle-fill'
    };
    return icons[type] || 'bell-fill';
  }

  getNotificationColor(type: string): string {
    const colors: { [key: string]: string } = {
      'leave_approved': '#10b981',
      'leave_rejected': '#ef4444',
      'attendance_approved': '#3b82f6',
      'attendance_rejected': '#f59e0b',
      'payslip_generated': '#8b5cf6',
      'system': '#6b7280'
    };
    return colors[type] || '#6b7280';
  }
}
