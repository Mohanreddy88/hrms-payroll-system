export interface Payroll {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  month: number;
  year: number;
  monthYear: string;
  basicSalary: number;
  allowances: number;
  deductions: number;
  epfAmount: number;
  socsoAmount: number;
  taxAmount: number;
  grossIncome: number;
  netSalary: number;
  status: string; // Draft, Under Review, Pending Approval, Approved, Processed, Rejected
  attendanceHours?: number;
  expectedHours?: number;
  paidLeaveDays?: number;
  unpaidLeaveDays?: number;
  generatedOn: string;
  approvedBy?: number;
  approverName?: string;
  approvedOn?: string;
  processedBy?: number;
  processorName?: string;
  processedOn?: string;
  remarks?: string;
}

export interface PayrollDetails extends Payroll {
  attendancePeriods: AttendancePeriodSummary[];
  leaveRequests: LeaveRequestSummary[];
  adjustments: PayrollAdjustment[];
}

export interface AttendancePeriodSummary {
  id: number;
  attendancePeriodId: number;
  startDate: string;
  endDate: string;
  hoursWorked: number;
  status: string;
}

export interface LeaveRequestSummary {
  id: number;
  leaveRequestId: number;
  leaveType: string;
  leaveTypeCode: string;
  startDate: string;
  endDate: string;
  leaveDays: number;
  isPaid: boolean;
  deductionAmount: number;
}

export interface PayrollAdjustment {
  id: number;
  adjustmentType: string;
  description: string;
  amount: number;
  createdBy: string;
  createdAt: string;
}

export interface PayrollRequest {
  employeeId: number;
  month: number;
  year: number;
  basicSalary: number;
  allowances: number;
  deductions: number;
}

export interface PayrollGenerateRequest {
  employeeId: number;
  month: number;
  year: number;
}

export interface PayrollBulkGenerateRequest {
  employeeIds: number[];
  month: number;
  year: number;
}

export interface PayrollCalculateRequest {
  employeeId: number;
  month: number;
  year: number;
}

export interface PayrollEligibility {
  isEligible: boolean;
  warnings: string[];
  errors: string[];
  totalAttendancePeriods: number;
  approvedAttendancePeriods: number;
  pendingAttendancePeriods: number;
  totalLeaveRequests: number;
  pendingLeaveRequests: number;
}

export interface PayrollCalculation {
  employeeId: number;
  employeeName: string;
  month: number;
  year: number;
  basicSalary: number;
  hourlyRate: number;
  expectedHours: number;
  attendanceHours: number;
  workingDays: number;
  paidLeaveDays: number;
  unpaidLeaveDays: number;
  unpaidLeaveDeduction: number;
  allowances: number;
  manualDeductions: number;
  grossIncome: number;
  epfAmount: number;
  socsoAmount: number;
  taxAmount: number;
  totalDeductions: number;
  netSalary: number;
  attendancePeriods: AttendancePeriodSummary[];
  leaveRequests: LeaveRequestSummary[];
}

export interface PayrollAdjustmentRequest {
  adjustmentType: string; // Allowance, Deduction, Bonus, Overtime
  description: string;
  amount: number;
}

export interface PayrollApprovalRequest {
  remarks?: string;
}

export interface PayrollRejectionRequest {
  reason: string;
}
