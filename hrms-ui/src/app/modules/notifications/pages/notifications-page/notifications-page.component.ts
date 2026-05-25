import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NotificationService, Notification } from '../../../../core/services/notification.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './notifications-page.component.html',
  styleUrls: ['./notifications-page.component.scss']
})
export class NotificationsPageComponent implements OnInit {
  notifications: Notification[] = [];
  filtered: Notification[] = [];
  loading = false;
  filterType: string = 'all'; // 'all' | 'unread' | 'read'

  constructor(
    private service: NotificationService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading = true;
    this.service.getNotifications().subscribe({
      next: (data) => {
        this.notifications = data;
        this.applyFilter();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilter(): void {
    if (this.filterType === 'unread') {
      this.filtered = this.notifications.filter(n => !n.isRead);
    } else if (this.filterType === 'read') {
      this.filtered = this.notifications.filter(n => n.isRead);
    } else {
      this.filtered = this.notifications;
    }
  }

  onFilterChange(): void {
    this.applyFilter();
  }

  markAsRead(n: Notification): void {
    if (n.isRead) return;
    this.service.markAsRead(n.id).subscribe({
      next: () => {
        n.isRead = true;
        this.service.refreshUnreadCount();
        this.applyFilter();
        this.cdr.detectChanges();
      }
    });
  }

  markAllAsRead(): void {
    this.service.markAllAsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
        this.service.refreshUnreadCount();
        this.applyFilter();
        this.toast.success('Done', 'All notifications marked as read.');
        this.cdr.detectChanges();
      }
    });
  }

  deleteNotification(n: Notification): void {
    this.service.deleteNotification(n.id).subscribe({
      next: () => {
        this.notifications = this.notifications.filter(x => x.id !== n.id);
        this.service.refreshUnreadCount();
        this.applyFilter();
        this.cdr.detectChanges();
      }
    });
  }

  clearAll(): void {
    const ids = this.notifications.map(n => n.id);
    Promise.all(ids.map(id => this.service.deleteNotification(id).toPromise())).then(() => {
      this.notifications = [];
      this.filtered = [];
      this.service.refreshUnreadCount();
      this.toast.success('Cleared', 'All notifications cleared.');
      this.cdr.detectChanges();
    });
  }

  unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);
    if (diffMins < 1)  return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7)  return `${diffDays}d ago`;
    return date.toLocaleDateString('en-MY', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  getIcon(type: string): string {
    return this.service.getNotificationIcon(type);
  }

  getColor(type: string): string {
    return this.service.getNotificationColor(type);
  }
}
