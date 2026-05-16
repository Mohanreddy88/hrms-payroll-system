import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, interval } from 'rxjs';
import { environment } from '../../../environments/environment';

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
  public unreadCount$ = this.unreadCountSubject.asObservable();

  private notificationsSubject = new BehaviorSubject<Notification[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  constructor(private http: HttpClient) {
    // Poll for notifications every 30 seconds
    interval(30000).subscribe(() => {
      this.refreshUnreadCount();
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
      error: (err) => console.error('Failed to refresh notifications:', err)
    });
  }

  refreshUnreadCount(): void {
    this.getUnreadCount().subscribe({
      next: (data) => {
        this.unreadCountSubject.next(data.count);
      },
      error: (err) => console.error('Failed to refresh unread count:', err)
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
