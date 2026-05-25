import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PublicHolidayService, PublicHoliday } from '../../../../core/services/public-holiday.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmModalComponent } from '../../../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-public-holidays',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './public-holidays-component.html',
  styleUrls: ['./public-holidays-component.scss']
})
export class PublicHolidaysComponent implements OnInit {
  holidays: PublicHoliday[] = [];
  years: number[] = [];
  selectedYear: number = new Date().getFullYear();

  loading = false;
  saving = false;
  showModal = false;
  isEdit = false;
  editId: number | null = null;
  modalError = '';

  showConfirmModal = false;
  confirmTitle = '';
  confirmMessage = '';
  pendingDelete: PublicHoliday | null = null;

  form = {
    name: '',
    date: '',
    isNational: true,
    state: '',
    description: ''
  };

  malaysiaStates = [
    'Johor', 'Kedah', 'Kelantan', 'Kuala Lumpur', 'Labuan', 'Melaka',
    'Negeri Sembilan', 'Pahang', 'Perak', 'Perlis', 'Pulau Pinang',
    'Putrajaya', 'Sabah', 'Sarawak', 'Selangor', 'Terengganu'
  ];

  constructor(
    private service: PublicHolidayService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadYears();
    this.loadHolidays();
  }

  loadYears(): void {
    this.service.getYears().subscribe({
      next: (years) => { this.years = years; this.cdr.detectChanges(); },
      error: () => { this.years = [new Date().getFullYear()]; }
    });
  }

  loadHolidays(): void {
    this.loading = true;
    this.service.getAll(this.selectedYear).subscribe({
      next: (data) => { this.holidays = data; this.loading = false; this.cdr.detectChanges(); },
      error: (err) => {
        this.loading = false;
        if (err.status !== 404) this.toast.error('Load Failed', err?.error?.message ?? 'Could not load holidays.');
        this.cdr.detectChanges();
      }
    });
  }

  onYearChange(): void {
    this.loadHolidays();
  }

  openCreate(): void {
    this.isEdit = false;
    this.editId = null;
    this.form = { name: '', date: '', isNational: true, state: '', description: '' };
    this.showModal = true;
    this.modalError = '';
  }

  openEdit(h: PublicHoliday): void {
    this.isEdit = true;
    this.editId = h.id;
    this.form = {
      name: h.name,
      date: h.date.substring(0, 10),
      isNational: h.isNational,
      state: h.state ?? '',
      description: h.description ?? ''
    };
    this.showModal = true;
    this.modalError = '';
  }

  closeModal(): void {
    this.showModal = false;
    this.modalError = '';
  }

  save(): void {
    if (!this.form.name.trim()) { this.modalError = 'Holiday name is required.'; return; }
    if (!this.form.date)        { this.modalError = 'Date is required.'; return; }

    this.saving = true;
    const payload = {
      name: this.form.name.trim(),
      date: this.form.date,
      isNational: this.form.isNational,
      state: this.form.isNational ? undefined : this.form.state.trim() || undefined,
      description: this.form.description.trim() || undefined
    };

    const request$ = this.isEdit && this.editId !== null
      ? this.service.update(this.editId, payload)
      : this.service.create(payload);

    request$.subscribe({
      next: () => {
        this.saving = false;
        this.toast.success(this.isEdit ? 'Holiday Updated' : 'Holiday Created', `${payload.name} has been ${this.isEdit ? 'updated' : 'added'}.`);
        this.closeModal();
        this.loadHolidays();
        this.loadYears();
      },
      error: (err: any) => {
        this.saving = false;
        this.modalError = err?.error?.message ?? 'Failed to save holiday.';
      }
    });
  }

  deleteHoliday(h: PublicHoliday): void {
    this.pendingDelete = h;
    this.confirmTitle = 'Delete Holiday';
    this.confirmMessage = `Are you sure you want to delete "${h.name}"? This cannot be undone.`;
    this.showConfirmModal = true;
  }

  onConfirmDelete(): void {
    if (!this.pendingDelete) return;
    this.service.delete(this.pendingDelete.id).subscribe({
      next: () => {
        this.toast.success('Deleted', `${this.pendingDelete!.name} has been removed.`);
        this.pendingDelete = null;
        this.loadHolidays();
      },
      error: (err: any) => {
        this.toast.error('Delete Failed', err?.error?.message ?? 'Failed to delete holiday.');
        this.pendingDelete = null;
      }
    });
  }

  onCancelDelete(): void {
    this.pendingDelete = null;
  }

  nationalCount(): number {
    return this.holidays.filter(h => h.isNational).length;
  }

  stateCount(): number {
    return this.holidays.filter(h => !h.isNational).length;
  }
}
