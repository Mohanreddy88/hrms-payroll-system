import { Component } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { ToastService, Toast } from '../../../core/services/toast.service';

/**
 * ToastComponent — fixed overlay in the top-right corner.
 * Subscribes to ToastService.toasts$ and renders each notification.
 * Place <app-toast></app-toast> once in admin-layout-component.html.
 */
@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule, AsyncPipe],
  template: `
    <div class="toast-container">
      @for (toast of toastService.toasts$ | async; track toast.id) {
        <div class="toast-item toast-{{ toast.type }}" role="alert">

          <!-- Icon -->
          <div class="toast-icon">
            @if (toast.type === 'success') { <i class="bi bi-check-circle-fill"></i> }
            @if (toast.type === 'error')   { <i class="bi bi-x-circle-fill"></i> }
            @if (toast.type === 'warning') { <i class="bi bi-exclamation-triangle-fill"></i> }
            @if (toast.type === 'info')    { <i class="bi bi-info-circle-fill"></i> }
          </div>

          <!-- Text -->
          <div class="toast-body">
            <div class="toast-title">{{ toast.title }}</div>
            @if (toast.message) {
              <div class="toast-msg">{{ toast.message }}</div>
            }
          </div>

          <!-- Close button -->
          <button class="toast-close" (click)="toastService.dismiss(toast.id)">
            <i class="bi bi-x"></i>
          </button>

        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 20px;
      right: 20px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 10px;
      min-width: 300px;
      max-width: 400px;
    }

    .toast-item {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 14px 16px;
      border-radius: 10px;
      box-shadow: 0 4px 20px rgba(0,0,0,0.15);
      animation: slideIn 0.25s ease;
      background: white;
      border-left: 4px solid;
    }

    /* Colour variants */
    .toast-success { border-color: #16a34a; }
    .toast-success .toast-icon { color: #16a34a; }

    .toast-error   { border-color: #dc2626; }
    .toast-error   .toast-icon { color: #dc2626; }

    .toast-warning { border-color: #d97706; }
    .toast-warning .toast-icon { color: #d97706; }

    .toast-info    { border-color: #2563eb; }
    .toast-info    .toast-icon { color: #2563eb; }

    .toast-icon { font-size: 18px; padding-top: 2px; flex-shrink: 0; }

    .toast-body { flex: 1; }
    .toast-title { font-weight: 700; font-size: 13px; color: #111; }
    .toast-msg   { font-size: 12px; color: #555; margin-top: 2px; }

    .toast-close {
      background: none; border: none; cursor: pointer;
      color: #aaa; font-size: 16px; padding: 0; line-height: 1;
      flex-shrink: 0;
    }
    .toast-close:hover { color: #333; }

    @keyframes slideIn {
      from { transform: translateX(100%); opacity: 0; }
      to   { transform: translateX(0);   opacity: 1; }
    }
  `]
})
export class ToastComponent {
  constructor(public toastService: ToastService) {}
}
