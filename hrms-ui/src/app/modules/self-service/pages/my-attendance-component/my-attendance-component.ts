import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SelfServiceService, AttendanceResponse } from '../../../../core/services/self-service.service';
import { LoadingService } from '../../../../core/services/loading.service';
import { ToastService } from '../../../../core/services/toast.service';
import { AuthService } from '../../../../core/services/auth.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { environment } from '../../../../../environments/environment';

interface DayEntry {
  date: Date;
  dayName: string;
  dayNumber: number;
  hours: number;
  note: string;
  remarks: string;
  isPublicHoliday: boolean;
  isWeekend: boolean;
}

interface AttendancePeriod {
  id: number | null;
  startDate: Date;
  endDate: Date;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  rejectionReason?: string;
  days: DayEntry[];
}

@Component({
  selector: 'app-my-attendance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './my-attendance-component.html',
  styleUrl: './my-attendance-component.scss'
})
export class MyAttendanceComponent implements OnInit {
  periods: AttendancePeriod[] = [];
  allPeriods: AttendancePeriod[] = []; // All periods across all months
  currentPeriodIndex: number = 0;
  currentPeriodDays: DayEntry[] = [];
  currentPeriodStatus: 'Draft' | 'Submitted' | 'Approved' | 'Rejected' = 'Draft';
  currentPeriodRejectionReason: string = '';
  
  showWeekend: boolean = true;
  agreedToTerms: boolean = false;
  
  // Employee info (will be loaded from API)
  employeeName: string = 'Loading...';
  employeeCode: string = '';
  
  // Modal states
  showNoteModal: boolean = false;
  selectedDayIndex: number = -1;
  selectedDayForNote: number = -1;
  selectedNote: string = '';
  selectedRemarks: string = '';
  selectedDayDate: string = '';
  
  showAddPeriodModal: boolean = false;
  newPeriodStart: string = '';
  newPeriodEnd: string = '';
  
  showCommentModal: boolean = false;
  commentText: string = '';
  lastClickedDayIndex: number = -1;  // Track which input was last clicked
  
  showDeleteModal: boolean = false;
  hasClickedAnyInput: boolean = false;

  // Public holidays (should come from API - hardcoded for now)
  publicHolidays: string[] = [
    '2026-05-01', // Example: Labour Day
    '2026-05-25'  // Example: Vesak Day
  ];

  constructor(
    private http: HttpClient,
    private selfServiceService: SelfServiceService,
    private loadingService: LoadingService,
    private toast: ToastService,
    private authService: AuthService,
    private employeeService: EmployeeService
  ) {}

  ngOnInit(): void {
    this.loadEmployeeInfo();
    this.loadPeriods();
  }

  loadEmployeeInfo(): void {
    const employeeId = this.authService.getEmployeeId();
    if (employeeId) {
      this.employeeService.getById(employeeId).subscribe({
        next: (employee: any) => {
          this.employeeName = employee.name.toUpperCase();  // Display in uppercase
          this.employeeCode = employee.employeeCode;
        },
        error: (error) => {
          console.error('Load employee info error:', error);
          this.employeeName = 'EMPLOYEE';
          this.employeeCode = 'EMP000';
        }
      });
    } else {
      this.employeeName = 'EMPLOYEE';
      this.employeeCode = 'EMP000';
    }
  }

  loadPeriods(): void {
    this.loadingService.show();
    
    const url = `${environment.apiUrl}/selfservice/attendance-periods`;
    this.http.get<any[]>(url).subscribe({
      next: (apiPeriods) => {
        // Generate local period templates
        const localPeriods = this.generateAllPeriods();
        
        // Merge API data with local templates
        this.periods = localPeriods.map(localPeriod => {
          // Find matching period from API
          const apiPeriod = apiPeriods.find(ap => {
            const apiStart = new Date(ap.startDate);
            const apiEnd = new Date(ap.endDate);
            return apiStart.getTime() === localPeriod.startDate.getTime() &&
                   apiEnd.getTime() === localPeriod.endDate.getTime();
          });
          
          if (apiPeriod) {
            // Use API data
            return {
              id: apiPeriod.id,
              startDate: new Date(apiPeriod.startDate),
              endDate: new Date(apiPeriod.endDate),
              status: apiPeriod.status as any,
              rejectionReason: apiPeriod.rejectionReason,
              days: apiPeriod.days.map((d: any) => ({
                date: new Date(d.date),
                dayName: this.getDayName(new Date(d.date).getDay()),
                dayNumber: new Date(d.date).getDate(),
                hours: d.hours,
                note: d.note || '',
                remarks: d.remarks || '',
                isPublicHoliday: d.isPublicHoliday,
                isWeekend: d.isWeekend
              }))
            };
          } else {
            // Use local template (not saved yet)
            return localPeriod;
          }
        });
        
        // Find current period
        const today = new Date();
        this.currentPeriodIndex = this.findCurrentPeriodIndex(today);
        this.loadCurrentPeriod();
        this.loadingService.hide();
      },
      error: (error) => {
        console.error('Load periods error:', error);
        // Fall back to local generation
        this.periods = this.generateAllPeriods();
        const today = new Date();
        this.currentPeriodIndex = this.findCurrentPeriodIndex(today);
        this.loadCurrentPeriod();
        this.loadingService.hide();
      }
    });
  }

  generateAllPeriods(): AttendancePeriod[] {
    const periods: AttendancePeriod[] = [];
    const today = new Date();
    
    // Generate periods from 6 months ago to current month only
    // Previous button: goes back (Apr, Mar, Feb, Jan, Dec 2025, Nov 2025...)
    // Next button: stops at current month end (Jun)
    for (let monthOffset = -6; monthOffset <= 0; monthOffset++) {
      const targetDate = new Date(today.getFullYear(), today.getMonth() + monthOffset, 1);
      const year = targetDate.getFullYear();
      const month = targetDate.getMonth();
      
      // Cycle 1: 1st to 15th
      const cycle1Start = new Date(year, month, 1);
      const cycle1End = new Date(year, month, 15);
      
      // Cycle 2: 16th to end of month
      const cycle2Start = new Date(year, month, 16);
      const cycle2End = new Date(year, month + 1, 0); // Last day of month
      
      periods.push({
        id: null,
        startDate: cycle1Start,
        endDate: cycle1End,
        status: 'Draft', // Will be loaded from API
        days: this.generateDaysForPeriod(cycle1Start, cycle1End)
      });
      
      periods.push({
        id: null,
        startDate: cycle2Start,
        endDate: cycle2End,
        status: 'Draft', // Will be loaded from API
        days: this.generateDaysForPeriod(cycle2Start, cycle2End)
      });
    }
    
    // Sort by date (oldest first)
    return periods.sort((a, b) => a.startDate.getTime() - b.startDate.getTime());
  }

  findCurrentPeriodIndex(today: Date): number {
    const todayDate = today.getDate();
    
    for (let i = 0; i < this.periods.length; i++) {
      const period = this.periods[i];
      if (period.startDate <= today && period.endDate >= today) {
        return i;
      }
    }
    
    // Default to first period if not found
    return 0;
  }

  generateDaysForPeriod(startDate: Date, endDate: Date): DayEntry[] {
    const days: DayEntry[] = [];
    const currentDate = new Date(startDate);
    
    while (currentDate <= endDate) {
      const dateStr = this.formatDateToString(currentDate);
      const isPublicHoliday = this.publicHolidays.includes(dateStr);
      const isWeekend = currentDate.getDay() === 0 || currentDate.getDay() === 6;
      
      days.push({
        date: new Date(currentDate),
        dayName: this.getDayName(currentDate.getDay()),
        dayNumber: currentDate.getDate(),
        hours: 0, // Empty by default - employee fills daily
        note: '',
        remarks: '',
        isPublicHoliday: isPublicHoliday,
        isWeekend: isWeekend
      });
      
      currentDate.setDate(currentDate.getDate() + 1);
    }
    
    return days;
  }

  formatDateToString(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  getDayName(dayIndex: number): string {
    const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    return days[dayIndex];
  }

  formatDateForApi(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}T00:00:00`;
  }

  loadCurrentPeriod(): void {
    if (this.periods.length > 0 && this.currentPeriodIndex >= 0 && this.currentPeriodIndex < this.periods.length) {
      const period = this.periods[this.currentPeriodIndex];
      this.currentPeriodDays = period.days;
      this.currentPeriodStatus = period.status;
      this.currentPeriodRejectionReason = period.rejectionReason || '';
      this.agreedToTerms = false;
      
      // Always hide comment/delete buttons when loading a period
      // They will appear only after user clicks an input field
      this.hasClickedAnyInput = false;
    }
  }

  canEditPeriod(): boolean {
    // Can only edit if Draft or Rejected
    return this.currentPeriodStatus === 'Draft' || this.currentPeriodStatus === 'Rejected';
  }

  canSubmitPeriod(): boolean {
    // Can submit if Draft or Rejected
    return this.currentPeriodStatus === 'Draft' || this.currentPeriodStatus === 'Rejected';
  }

  canDeletePeriod(): boolean {
    // Can delete if period is Draft or Rejected and has any data entered
    return this.canEditPeriod() && this.hasClickedAnyInput;
  }

  isPeriodEditable(): boolean {
    return this.canEditPeriod();
  }

  getCurrentPeriodDisplay(): string {
    if (this.periods.length === 0) return '';
    const period = this.periods[this.currentPeriodIndex];
    const startStr = this.formatDisplayDate(period.startDate);
    const endStr = this.formatDisplayDate(period.endDate);
    return `${startStr} - ${endStr}`;
  }

  formatDisplayDate(date: Date): string {
    const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return `${days[date.getDay()]}, ${date.getDate()} ${months[date.getMonth()]} ${date.getFullYear()}`;
  }

  previousPeriod(): void {
    if (this.currentPeriodIndex > 0) {
      this.currentPeriodIndex--;
      this.loadCurrentPeriod();
    }
  }

  nextPeriod(): void {
    const today = new Date();
    const currentMonth = today.getMonth();
    const currentYear = today.getFullYear();
    
    if (this.currentPeriodIndex < this.periods.length - 1) {
      const nextPeriod = this.periods[this.currentPeriodIndex + 1];
      const nextMonth = nextPeriod.startDate.getMonth();
      const nextYear = nextPeriod.startDate.getFullYear();
      
      // Check if next period is beyond current month
      const isNextPeriodBeyondCurrentMonth = 
        nextYear > currentYear || 
        (nextYear === currentYear && nextMonth > currentMonth);
      
      // Block navigation if trying to go beyond current month
      if (isNextPeriodBeyondCurrentMonth) {
        this.toast.warning('Current Month Limit', 'Cannot navigate beyond the current month');
        return;
      }
      
      // Allow navigation within current month or to past months
      this.currentPeriodIndex++;
      this.loadCurrentPeriod();
    }
  }

  onShowWeekendChange(): void {
    // Just triggers re-render with ngIf conditions
  }

  isWeekend(date: Date): boolean {
    return date.getDay() === 0 || date.getDay() === 6;
  }

  onInputFocus(dayIndex: number): void {
    // Mark that user has interacted with any input field
    this.hasClickedAnyInput = true;
    // Track which day input was last clicked
    this.lastClickedDayIndex = dayIndex;
  }

  onHoursInput(event: any, dayIndex: number): void {
    // Mark that user has clicked any input
    this.hasClickedAnyInput = true;
    
    // Allow only numbers and decimal point
    let value = event.target.value;
    value = value.replace(/[^0-9.]/g, '');
    
    // Prevent multiple decimal points
    const parts = value.split('.');
    if (parts.length > 2) {
      value = parts[0] + '.' + parts.slice(1).join('');
    }
    
    event.target.value = value;
  }

  onHoursBlur(dayIndex: number): void {
    const day = this.currentPeriodDays[dayIndex];
    let value = parseFloat(day.hours.toString());
    
    if (isNaN(value) || value < 0) {
      day.hours = 0;
    } else if (value > 24) {
      day.hours = 24;
    } else {
      // Round to 0.5
      day.hours = Math.round(value * 2) / 2;
    }
    
    this.calculateTotal();
  }

  calculateTotal(): void {
    // Recalculates total - handled by getTotalHours()
  }

  getTotalHours(): number {
    let total = 0;
    for (const day of this.currentPeriodDays) {
      if (!this.showWeekend && this.isWeekend(day.date)) continue;
      total += Number(day.hours) || 0;
    }
    return total;
  }

  getLeaveCount(leaveType: string): number {
    let count = 0;
    for (const day of this.currentPeriodDays) {
      if (day.note === leaveType) {
        count++;
      }
    }
    return count;
  }

  selectDayForNote(dayIndex: number): void {
    // Only allow selection for editable days (not weekends, not holidays, and period is editable)
    const day = this.currentPeriodDays[dayIndex];
    if (day.isPublicHoliday || this.isWeekend(day.date) || !this.isPeriodEditable()) {
      return;
    }
    
    // Hide comment button when opening leave note modal
    this.hasClickedAnyInput = false;
    
    // Stop event propagation to prevent input field from being focused
    this.selectedDayIndex = dayIndex;
    this.selectedNote = day.note || '';
    this.selectedRemarks = day.remarks || '';
    this.selectedDayDate = this.formatDisplayDate(day.date);
    this.showNoteModal = true;
  }

  openNoteModal(dayIndex: number): void {
    // Hide comment button when opening leave note modal
    this.hasClickedAnyInput = false;
    
    this.selectedDayIndex = dayIndex;
    const day = this.currentPeriodDays[dayIndex];
    this.selectedNote = day.note || '';
    this.selectedRemarks = day.remarks || '';
    this.selectedDayDate = this.formatDisplayDate(day.date);
    this.showNoteModal = true;
  }

  closeNoteModal(): void {
    this.showNoteModal = false;
    this.selectedDayIndex = -1;
    this.selectedDayForNote = -1;
    this.selectedNote = '';
    this.selectedRemarks = '';
  }

  clearNote(): void {
    if (this.selectedDayIndex >= 0) {
      const day = this.currentPeriodDays[this.selectedDayIndex];
      day.note = '';
      day.remarks = '';
      this.selectedNote = '';
      this.selectedRemarks = '';
      this.closeNoteModal();
      this.toast.success('Cleared', 'Note cleared successfully');
    }
  }

  saveNote(): void {
    if (this.selectedDayIndex >= 0) {
      const day = this.currentPeriodDays[this.selectedDayIndex];
      day.note = this.selectedNote;
      day.remarks = this.selectedRemarks;
      
      // If leave is selected, set hours to 0
      if (this.selectedNote) {
        day.hours = 0;
      }
      
      this.closeNoteModal();
      this.toast.success('Note Saved', 'Leave type added successfully');
    }
  }

  createNewPeriod(): void {
    if (!this.newPeriodStart || !this.newPeriodEnd) {
      this.toast.warning('Missing Dates', 'Please select both start and end dates');
      return;
    }
    
    const start = new Date(this.newPeriodStart);
    const end = new Date(this.newPeriodEnd);
    
    if (start > end) {
      this.toast.error('Invalid Range', 'Start date must be before end date');
      return;
    }
    
    this.periods.push({
      id: null,
      startDate: start,
      endDate: end,
      status: 'Draft',
      days: this.generateDaysForPeriod(start, end)
    });
    
    this.currentPeriodIndex = this.periods.length - 1;
    this.loadCurrentPeriod();
    this.showAddPeriodModal = false;
    this.newPeriodStart = '';
    this.newPeriodEnd = '';
    this.toast.success('Period Created', 'New attendance period created successfully');
  }

  saveAttendance(): void {
    this.loadingService.show();
    const currentPeriod = this.periods[this.currentPeriodIndex];
    
    const payload = {
      id: currentPeriod.id,
      startDate: this.formatDateForApi(currentPeriod.startDate),
      endDate: this.formatDateForApi(currentPeriod.endDate),
      days: currentPeriod.days.map(d => ({
        date: this.formatDateForApi(d.date),
        hours: d.hours,
        note: d.note || null,
        remarks: d.remarks || null,
        isPublicHoliday: d.isPublicHoliday,
        isWeekend: d.isWeekend
      }))
    };
    
    const url = `${environment.apiUrl}/selfservice/attendance-periods`;
    this.http.post<any>(url, payload).subscribe({
      next: (response) => {
        currentPeriod.id = response.id;
        this.loadingService.hide();
        this.toast.success('Saved', 'Attendance data saved successfully');
      },
      error: (error) => {
        console.error('Save error:', error);
        this.loadingService.hide();
        this.toast.error('Save Failed', error.error?.detail || 'Could not save attendance');
      }
    });
  }

  submitAttendance(): void {
    if (!this.agreedToTerms) {
      this.toast.warning('Agreement Required', 'Please agree to the terms before submitting');
      return;
    }
    
    // First save, then submit
    this.loadingService.show();
    const currentPeriod = this.periods[this.currentPeriodIndex];
    
    const payload = {
      id: currentPeriod.id,
      startDate: this.formatDateForApi(currentPeriod.startDate),
      endDate: this.formatDateForApi(currentPeriod.endDate),
      days: currentPeriod.days.map(d => ({
        date: this.formatDateForApi(d.date),
        hours: d.hours,
        note: d.note || null,
        remarks: d.remarks || null,
        isPublicHoliday: d.isPublicHoliday,
        isWeekend: d.isWeekend
      }))
    };
    
    const saveUrl = `${environment.apiUrl}/selfservice/attendance-periods`;
    this.http.post<any>(saveUrl, payload).subscribe({
      next: (response) => {
        currentPeriod.id = response.id;
        
        // Now submit
        const submitUrl = `${environment.apiUrl}/selfservice/attendance-periods/${response.id}/submit`;
        this.http.post(submitUrl, {}).subscribe({
          next: () => {
            this.currentPeriodStatus = 'Submitted';
            currentPeriod.status = 'Submitted';
            this.loadingService.hide();
            this.toast.success('Submitted', 'Attendance submitted successfully for approval');
          },
          error: (error) => {
            console.error('Submit error:', error);
            this.loadingService.hide();
            this.toast.error('Submit Failed', error.error?.detail || 'Could not submit attendance');
          }
        });
      },
      error: (error) => {
        console.error('Save before submit error:', error);
        this.loadingService.hide();
        this.toast.error('Save Failed', error.error?.detail || 'Could not save attendance before submitting');
      }
    });
  }

  goToCurrentCycle(): void {
    // Jump to current period based on today's date
    const today = new Date();
    this.currentPeriodIndex = this.findCurrentPeriodIndex(today);
    this.loadCurrentPeriod();
    this.toast.success('Current Cycle', 'Jumped to current attendance cycle');
  }

  openCommentModal(): void {
    this.showCommentModal = true;
  }

  closeCommentModal(): void {
    this.showCommentModal = false;
  }

  saveComment(): void {
    if (!this.commentText.trim()) {
      this.toast.warning('Empty Comment', 'Please enter a comment before saving');
      return;
    }
    
    // TODO: Save comment via API
    this.toast.success('Comment Saved', 'Your comment has been saved successfully');
    this.commentText = '';
    this.closeCommentModal();
  }

  getSelectedCommentDateDisplay(): string {
    // Return the specific date of the last clicked input field
    if (this.lastClickedDayIndex >= 0 && this.lastClickedDayIndex < this.currentPeriodDays.length) {
      const day = this.currentPeriodDays[this.lastClickedDayIndex];
      return this.formatDisplayDate(day.date);
    }
    // Fallback to current period display
    return this.getCurrentPeriodDisplay();
  }

  openDeleteModal(): void {
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
  }

  confirmDelete(): void {
    // Reset all hours to 0 and clear notes
    this.currentPeriodDays.forEach(day => {
      day.hours = 0;
      day.note = '';
      day.remarks = '';
    });
    this.hasClickedAnyInput = false;
    this.closeDeleteModal();
    this.toast.success('Deleted', 'Attendance entry has been cleared');
  }

  deleteCurrentRow(): void {
    // Open delete confirmation modal instead of browser confirm
    this.openDeleteModal();
  }

  printAttendance(): void {
    window.print();
  }

  onBackdropClick(event: MouseEvent, modalType: 'note' | 'comment' | 'delete'): void {
    if (event.target === event.currentTarget) {
      switch (modalType) {
        case 'note':
          this.closeNoteModal();
          break;
        case 'comment':
          this.closeCommentModal();
          break;
        case 'delete':
          this.closeDeleteModal();
          break;
      }
    }
  }
}
