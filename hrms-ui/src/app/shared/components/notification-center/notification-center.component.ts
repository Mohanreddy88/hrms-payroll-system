import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { NotificationService, Notification } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './notification-center.component.html',
  styleUrls: ['./notification-center.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationCenterComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  unreadCount = 0;
  isOpen = false;
  loading = false;

  private subscriptions: Subscription[] = [];

  constructor(
    private notificationService: NotificationService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadNotifications();

    // Subscribe to unread count — OnPush: only mark dirty when value actually changes
    const countSub = this.notificationService.unreadCount$.subscribe(count => {
      if (this.unreadCount !== count) {
        this.unreadCount = count;
        this.cdr.markForCheck();
      }
    });
    this.subscriptions.push(countSub);

    // Subscribe to notifications — only re-render when list actually changes
    const notifSub = this.notificationService.notifications$.subscribe(notifications => {
      this.notifications = notifications;
      this.cdr.markForCheck();
    });
    this.subscriptions.push(notifSub);

    // Initial refresh
    this.notificationService.refreshUnreadCount();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  toggleDropdown(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.loadNotifications();
    }
  }

  closeDropdown(): void {
    this.isOpen = false;
  }

  loadNotifications(): void {
    this.loading = true;
    this.cdr.markForCheck();
    this.notificationService.getNotifications().subscribe({
      next: (notifications) => {
        this.notifications = notifications;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  markAsRead(notification: Notification): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe({
        next: () => {
          notification.isRead = true;
          notification.readAt = new Date().toISOString();
          this.notificationService.refreshUnreadCount();
          this.cdr.markForCheck();
          if (notification.link) {
            this.router.navigate([notification.link]);
            this.closeDropdown();
          }
        }
      });
    } else {
      if (notification.link) {
        this.router.navigate([notification.link]);
        this.closeDropdown();
      }
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => {
          n.isRead = true;
          n.readAt = new Date().toISOString();
        });
        this.notificationService.refreshUnreadCount();
        this.cdr.markForCheck();
      }
    });
  }

  deleteNotification(notification: Notification, event: Event): void {
    event.stopPropagation();
    this.notificationService.deleteNotification(notification.id).subscribe({
      next: () => {
        this.notifications = this.notifications.filter(n => n.id !== notification.id);
        this.notificationService.refreshUnreadCount();
        this.cdr.markForCheck();
      }
    });
  }

  formatDateTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  getIcon(type: string): string {
    return this.notificationService.getNotificationIcon(type);
  }

  getColor(type: string): string {
    return this.notificationService.getNotificationColor(type);
  }
}
