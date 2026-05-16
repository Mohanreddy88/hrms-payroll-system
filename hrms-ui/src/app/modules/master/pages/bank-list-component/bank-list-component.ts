import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BankService } from '../../../../core/services/bank.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Bank } from '../../../../core/models/bank.model';
import { ConfirmModalComponent } from '../../../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-bank-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './bank-list-component.html',
  styleUrls: ['./bank-list-component.scss']
})
export class BankListComponent implements OnInit {
  banks: Bank[] = [];
  loading = false;
  saving = false;
  showModal = false;
  isEdit = false;
  editId: number | null = null;
  modalError = '';

  // Confirmation modal state
  showConfirmModal = false;
  confirmModalTitle = '';
  confirmModalMessage = '';
  confirmModalVariant: 'danger' | 'warning' | 'info' = 'danger';
  pendingDeleteBank: Bank | null = null;

  form = { name: '', isActive: true };

  constructor(
    private bankService: BankService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadBanks();
  }

  loadBanks(): void {
    this.loading = true;
    this.bankService.getAll().subscribe({
      next: (data) => {
        this.banks = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toast.error('Load Failed', 'Could not load banks.');
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openCreate(): void {
    this.isEdit = false;
    this.editId = null;
    this.form = { name: '', isActive: true };
    this.showModal = true;
    this.modalError = '';
  }

  openEdit(bank: Bank): void {
    this.isEdit = true;
    this.editId = bank.id;
    this.form = { name: bank.name, isActive: bank.isActive };
    this.showModal = true;
    this.modalError = '';
  }

  closeModal(): void {
    this.showModal = false;
    this.modalError = '';
  }

  save(): void {
    if (!this.form.name.trim()) {
      this.modalError = 'Bank name is required.';
      return;
    }

    this.saving = true;

    if (this.isEdit && this.editId !== null) {
      this.bankService.update(this.editId, {
        name: this.form.name.trim(),
        isActive: this.form.isActive
      }).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success('Bank Updated', `${this.form.name} has been updated.`);
          this.closeModal();
          this.loadBanks();
        },
        error: (err: any) => {
          this.saving = false;
          this.modalError = err?.error?.message ?? 'Failed to update bank.';
        }
      });
    } else {
      this.bankService.create({
        name: this.form.name.trim(),
        isActive: this.form.isActive
      }).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success('Bank Created', `${this.form.name} has been added.`);
          this.closeModal();
          this.loadBanks();
        },
        error: (err: any) => {
          this.saving = false;
          this.modalError = err?.error?.message ?? 'Failed to create bank.';
        }
      });
    }
  }

  deleteBank(bank: Bank): void {
    this.pendingDeleteBank = bank;
    this.confirmModalTitle = 'Delete Bank';
    this.confirmModalMessage = `Are you sure you want to delete "${bank.name}"? This action cannot be undone.`;
    this.confirmModalVariant = 'danger';
    this.showConfirmModal = true;
  }

  onConfirmDelete(): void {
    if (!this.pendingDeleteBank) return;
    this.bankService.delete(this.pendingDeleteBank.id).subscribe({
      next: () => {
        this.toast.success('Deleted', `${this.pendingDeleteBank!.name} has been removed.`);
        this.pendingDeleteBank = null;
        this.loadBanks();
      },
      error: (err: any) => {
        this.toast.error('Delete Failed', err?.error?.message ?? 'Failed to delete bank.');
        this.pendingDeleteBank = null;
      }
    });
  }

  onCancelDelete(): void {
    this.pendingDeleteBank = null;
  }

  backToHome(): void {
    this.router.navigate(['/home']);
  }
}
