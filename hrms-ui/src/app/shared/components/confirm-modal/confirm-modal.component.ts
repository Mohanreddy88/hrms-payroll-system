import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-modal.component.html',
  styleUrls: ['./confirm-modal.component.scss']
})
export class ConfirmModalComponent {
  @Input() show = false;
  @Input() title = 'Confirm Action';
  @Input() message = 'Are you sure you want to proceed?';
  @Input() confirmText = 'Confirm';
  @Input() cancelText = 'Cancel';
  @Input() variant: 'danger' | 'warning' | 'info' = 'warning';

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  onConfirm(): void {
    this.confirmed.emit();
    this.show = false;
  }

  onCancel(): void {
    this.cancelled.emit();
    this.show = false;
  }

  onDialogClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }
}
