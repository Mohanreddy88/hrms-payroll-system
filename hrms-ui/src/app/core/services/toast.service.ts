import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

/** Toast severity levels */
export type ToastType = 'success' | 'error' | 'warning' | 'info';

/** Shape of a single toast notification */
export interface Toast {
  id: number;
  type: ToastType;
  title: string;
  message: string;
}

/**
 * ToastService — global in-memory notification queue.
 *
 * Usage anywhere in the app:
 *   this.toast.success('Employee Saved', 'Employee record created successfully.');
 *   this.toast.error('Save Failed', 'Please check the form and try again.');
 *
 * The ToastContainerComponent subscribes to toasts$ and renders them.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  /** Auto-incrementing ID so each toast has a unique key */
  private nextId = 0;

  /** Live list of active toasts — components subscribe to this */
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  toasts$ = this.toastsSubject.asObservable();

  /** Default display duration in milliseconds */
  private readonly DEFAULT_DURATION = 4000;

  // ── Public helpers ────────────────────────────────────────

  success(title: string, message = ''): void {
    this.add('success', title, message);
  }

  error(title: string, message = ''): void {
    this.add('error', title, message);
  }

  warning(title: string, message = ''): void {
    this.add('warning', title, message);
  }

  info(title: string, message = ''): void {
    this.add('info', title, message);
  }

  /** Manually dismiss a toast by its ID */
  dismiss(id: number): void {
    this.toastsSubject.next(
      this.toastsSubject.value.filter(t => t.id !== id)
    );
  }

  // ── Private ───────────────────────────────────────────────

  private add(type: ToastType, title: string, message: string): void {
    const id = ++this.nextId;
    const toast: Toast = { id, type, title, message };

    // Append to active list
    this.toastsSubject.next([...this.toastsSubject.value, toast]);

    // Auto-remove after duration
    setTimeout(() => this.dismiss(id), this.DEFAULT_DURATION);
  }
}
