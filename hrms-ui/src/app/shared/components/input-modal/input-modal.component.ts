import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-input-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './input-modal.component.html',
  styleUrl: './input-modal.component.scss'
})
export class InputModalComponent {
  @Input() show = false;
  @Input() title = 'Input Required';
  @Input() message = 'Please enter a value:';
  @Input() placeholder = 'Enter text...';
  @Input() required = false;
  @Input() inputType: 'text' | 'textarea' = 'text';
  @Input() confirmText = 'Submit';
  @Input() cancelText = 'Cancel';
  
  @Output() confirmed = new EventEmitter<string>();
  @Output() cancelled = new EventEmitter<void>();

  inputValue = '';

  onConfirm(): void {
    if (this.required && !this.inputValue.trim()) {
      return;
    }
    this.confirmed.emit(this.inputValue);
    this.inputValue = '';
  }

  onCancel(): void {
    this.cancelled.emit();
    this.inputValue = '';
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.onCancel();
    }
  }
}
