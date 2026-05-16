using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Services;

public class PayrollCalculationService : IPayrollCalculationService
{
    private readonly HrmsDbContext _db;

    // Malaysia statutory rates
    private const decimal EPF_EMPLOYEE_RATE = 0.02m;   // 2% employee contribution
    private const decimal SOCSO_RATE = 0.005m;         // 0.5%
    private const decimal TAX_RATE = 0.1197m;          // 11.97% PCB (income tax)
    private const decimal STANDARD_HOURS_PER_DAY = 8m;

    public PayrollCalculationService(HrmsDbContext db)
    {
        _db = db;
    }

    public async Task<PayrollEligibilityResult> CheckEligibilityAsync(int employeeId, int month, int year)
    {
        var result = new PayrollEligibilityResult();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Check if payroll already exists
        var existingPayroll = await _db.Payrolls
            .AnyAsync(p => p.EmployeeId == employeeId && p.Month == month && p.Year == year);

        if (existingPayroll)
        {
            result.Errors.Add($"Payroll already exists for {new DateTime(year, month, 1):MMMM yyyy}");
            return result;
        }

        // Check attendance periods (find periods that overlap with the month)
        var attendancePeriods = await _db.AttendancePeriods
            .Where(ap => ap.EmployeeId == employeeId
                      && ap.StartDate <= endDate
                      && ap.EndDate >= startDate)
            .ToListAsync();

        result.TotalAttendancePeriods = attendancePeriods.Count;
        result.ApprovedAttendancePeriods = attendancePeriods.Count(ap => ap.Status == "Approved");
        result.PendingAttendancePeriods = attendancePeriods.Count(ap => ap.Status == "Submitted");
        var draftAttendancePeriods = attendancePeriods.Count(ap => ap.Status == "Draft");
        var rejectedAttendancePeriods = attendancePeriods.Count(ap => ap.Status == "Rejected");

        if (result.TotalAttendancePeriods == 0)
        {
            result.Errors.Add("No attendance records found for this month. Please create and approve attendance before generating payroll.");
        }
        else if (result.ApprovedAttendancePeriods == 0)
        {
            // Has attendance but none approved
            if (result.PendingAttendancePeriods > 0)
            {
                result.Errors.Add($"Attendance not approved yet. Please go to 'Attendance Approval' page and approve {result.PendingAttendancePeriods} pending attendance period(s) before generating payroll.");
            }
            else if (draftAttendancePeriods > 0)
            {
                result.Errors.Add($"{draftAttendancePeriods} attendance period(s) are still in Draft status. Employee must submit them, then admin should approve in 'Attendance Approval' page.");
            }
            else if (rejectedAttendancePeriods > 0)
            {
                result.Errors.Add($"All {rejectedAttendancePeriods} attendance period(s) were rejected. Please review and resubmit attendance before generating payroll.");
            }
            else
            {
                result.Errors.Add("No approved attendance found. Please go to 'Attendance Approval' page to approve attendance before generating payroll.");
            }
        }
        else if (result.ApprovedAttendancePeriods < result.TotalAttendancePeriods)
        {
            // Has some approved but not all
            if (result.PendingAttendancePeriods > 0)
            {
                result.Errors.Add($"{result.PendingAttendancePeriods} attendance period(s) still pending approval. Please approve all attendance in 'Attendance Approval' page before generating payroll. ({result.ApprovedAttendancePeriods}/{result.TotalAttendancePeriods} approved)");
            }
            else
            {
                var unapprovedCount = result.TotalAttendancePeriods - result.ApprovedAttendancePeriods;
                result.Errors.Add($"{unapprovedCount} attendance period(s) not yet approved. Please approve all attendance before generating payroll. ({result.ApprovedAttendancePeriods}/{result.TotalAttendancePeriods} approved)");
            }
        }

        // Check leave requests
        var leaveRequests = await _db.LeaveRequests
            .Where(lr => lr.EmployeeId == employeeId
                      && lr.StartDate <= endDate
                      && lr.EndDate >= startDate)
            .ToListAsync();

        result.TotalLeaveRequests = leaveRequests.Count;
        result.PendingLeaveRequests = leaveRequests.Count(lr => lr.Status == "Pending");

        if (result.PendingLeaveRequests > 0)
        {
            result.Warnings.Add($"{result.PendingLeaveRequests} leave request(s) still pending - may affect calculation");
        }

        result.IsEligible = result.Errors.Count == 0;
        return result;
    }

    public async Task<PayrollCalculationResult> CalculatePayrollAsync(int employeeId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get employee details
        var employee = await _db.Employees.FindAsync(employeeId)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var result = new PayrollCalculationResult
        {
            EmployeeId = employeeId,
            EmployeeName = employee.Name,
            Month = month,
            Year = year,
            BasicSalary = employee.Salary
        };

        // Calculate working days (exclude weekends)
        var workingDays = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }
        result.WorkingDays = workingDays;
        result.ExpectedHours = workingDays * STANDARD_HOURS_PER_DAY;

        // Get approved attendance periods (find periods that overlap with the month)
        var attendancePeriods = await _db.AttendancePeriods
            .Include(ap => ap.Days)
            .Where(ap => ap.EmployeeId == employeeId
                      && ap.StartDate <= endDate
                      && ap.EndDate >= startDate
                      && ap.Status == "Approved")
            .ToListAsync();

        // Calculate total hours worked
        decimal totalHours = 0;
        foreach (var period in attendancePeriods)
        {
            var hoursInPeriod = period.Days.Sum(d => d.Hours);
            totalHours += hoursInPeriod;

            result.AttendancePeriods.Add(new AttendancePeriodSummary
            {
                Id = period.Id,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                HoursWorked = hoursInPeriod,
                Status = period.Status
            });
        }
        result.AttendanceHours = totalHours;

        // Get leave requests (approved and pending)
        var leaveRequests = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == employeeId
                      && lr.StartDate <= endDate
                      && lr.EndDate >= startDate
                      && lr.Status != "Rejected" && lr.Status != "Cancelled")
            .ToListAsync();

        // Calculate paid and unpaid leave days
        int paidLeaveDays = 0;
        int unpaidLeaveDays = 0;

        foreach (var leave in leaveRequests)
        {
            var isPaid = leave.LeaveType.Code != "UL"; // UL = Unpaid Leave
            var leaveDaysInMonth = CalculateLeaveDaysInMonth(leave.StartDate, leave.EndDate, startDate, endDate);

            if (isPaid)
            {
                paidLeaveDays += (int)Math.Ceiling(leaveDaysInMonth);
            }
            else
            {
                unpaidLeaveDays += (int)Math.Ceiling(leaveDaysInMonth);
            }

            result.LeaveRequests.Add(new LeaveRequestSummary
            {
                Id = leave.Id,
                LeaveType = leave.LeaveType.Name,
                LeaveTypeCode = leave.LeaveType.Code,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                TotalDays = leaveDaysInMonth,
                IsPaid = isPaid,
                DeductionAmount = 0 // Calculated below
            });
        }

        result.PaidLeaveDays = paidLeaveDays;
        result.UnpaidLeaveDays = unpaidLeaveDays;

        // Calculate hourly rate (based on expected hours for the month)
        result.HourlyRate = result.ExpectedHours > 0
            ? result.BasicSalary / result.ExpectedHours
            : 0;

        // Calculate unpaid leave deduction
        result.UnpaidLeaveDeduction = unpaidLeaveDays * STANDARD_HOURS_PER_DAY * result.HourlyRate;

        // Update leave request deductions
        foreach (var leaveSummary in result.LeaveRequests)
        {
            if (!leaveSummary.IsPaid)
            {
                leaveSummary.DeductionAmount = leaveSummary.TotalDays * STANDARD_HOURS_PER_DAY * result.HourlyRate;
            }
        }

        // Calculate salary components
        result.Allowances = 0; // Can be added manually via adjustments
        result.ManualDeductions = 0; // Can be added manually via adjustments
        result.GrossIncome = result.BasicSalary + result.Allowances;

        // Statutory deductions
        result.EpfAmount = Math.Round(result.BasicSalary * EPF_EMPLOYEE_RATE, 2);
        result.SocsoAmount = Math.Round(result.BasicSalary * SOCSO_RATE, 2);
        result.TaxAmount = Math.Round(result.GrossIncome * TAX_RATE, 2);

        result.TotalDeductions = result.UnpaidLeaveDeduction
                               + result.EpfAmount
                               + result.SocsoAmount
                               + result.TaxAmount
                               + result.ManualDeductions;

        result.NetSalary = result.GrossIncome - result.TotalDeductions;

        return result;
    }

    private decimal CalculateLeaveDaysInMonth(DateTime leaveStart, DateTime leaveEnd, DateTime monthStart, DateTime monthEnd)
    {
        // Calculate overlap between leave period and the month
        var overlapStart = leaveStart > monthStart ? leaveStart : monthStart;
        var overlapEnd = leaveEnd < monthEnd ? leaveEnd : monthEnd;

        if (overlapStart > overlapEnd) return 0;

        // Count working days (exclude weekends)
        decimal days = 0;
        for (var date = overlapStart; date <= overlapEnd; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                days++;
            }
        }

        return days;
    }

    public async Task<Payroll> GeneratePayrollAsync(int employeeId, int month, int year, int createdById)
    {
        // Check eligibility first
        var eligibility = await CheckEligibilityAsync(employeeId, month, year);
        if (!eligibility.IsEligible)
        {
            throw new InvalidOperationException(
                $"Employee not eligible for payroll: {string.Join(", ", eligibility.Errors)}"
            );
        }

        // Calculate payroll
        var calculation = await CalculatePayrollAsync(employeeId, month, year);

        // Create payroll record
        var payroll = new Payroll
        {
            EmployeeId = employeeId,
            Month = month,
            Year = year,
            BasicSalary = calculation.BasicSalary,
            Allowances = calculation.Allowances,
            Deductions = calculation.UnpaidLeaveDeduction + calculation.ManualDeductions,
            EpfAmount = calculation.EpfAmount,
            SocsoAmount = calculation.SocsoAmount,
            TaxAmount = calculation.TaxAmount,
            GrossIncome = calculation.GrossIncome,
            NetSalary = calculation.NetSalary,
            Status = "Draft",
            AttendanceHours = calculation.AttendanceHours,
            ExpectedHours = calculation.ExpectedHours,
            PaidLeaveDays = calculation.PaidLeaveDays,
            UnpaidLeaveDays = calculation.UnpaidLeaveDays,
            GeneratedOn = DateTime.UtcNow,
            Remarks = $"Auto-generated from {calculation.AttendancePeriods.Count} attendance period(s) and {calculation.LeaveRequests.Count} leave request(s)"
        };

        _db.Payrolls.Add(payroll);
        await _db.SaveChangesAsync();

        // Link attendance periods
        foreach (var ap in calculation.AttendancePeriods)
        {
            _db.PayrollAttendancePeriods.Add(new PayrollAttendancePeriod
            {
                PayrollId = payroll.Id,
                AttendancePeriodId = ap.Id,
                HoursWorked = ap.HoursWorked
            });
        }

        // Link leave requests
        foreach (var lr in calculation.LeaveRequests)
        {
            _db.PayrollLeaveRequests.Add(new PayrollLeaveRequest
            {
                PayrollId = payroll.Id,
                LeaveRequestId = lr.Id,
                LeaveDays = lr.TotalDays,
                IsPaid = lr.IsPaid,
                DeductionAmount = lr.DeductionAmount
            });
        }

        await _db.SaveChangesAsync();

        return payroll;
    }

    public async Task<BulkPayrollGenerationResult> GenerateBulkPayrollAsync(
        List<int> employeeIds, int month, int year, int createdById)
    {
        var result = new BulkPayrollGenerationResult();

        foreach (var employeeId in employeeIds)
        {
            try
            {
                var payroll = await GeneratePayrollAsync(employeeId, month, year, createdById);
                result.GeneratedPayrollIds.Add(payroll.Id);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                var employee = await _db.Employees.FindAsync(employeeId);
                result.Errors.Add(new PayrollGenerationError
                {
                    EmployeeId = employeeId,
                    EmployeeName = employee?.Name ?? $"ID {employeeId}",
                    ErrorMessage = ex.Message
                });
                result.FailureCount++;
            }
        }

        return result;
    }

    public async Task<Payroll> ApprovePayrollAsync(int payrollId, int approvedById, string? remarks = null)
    {
        var payroll = await _db.Payrolls.FindAsync(payrollId)
            ?? throw new KeyNotFoundException($"Payroll {payrollId} not found");

        if (payroll.Status == "Approved" || payroll.Status == "Processed")
        {
            throw new InvalidOperationException($"Payroll is already {payroll.Status}");
        }

        payroll.Status = "Approved";
        payroll.ApprovedBy = approvedById;
        payroll.ApprovedOn = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            payroll.Remarks = (payroll.Remarks ?? "") + $"\nApproval: {remarks}";
        }

        await _db.SaveChangesAsync();
        return payroll;
    }

    public async Task<Payroll> RejectPayrollAsync(int payrollId, int rejectedById, string reason)
    {
        var payroll = await _db.Payrolls.FindAsync(payrollId)
            ?? throw new KeyNotFoundException($"Payroll {payrollId} not found");

        if (payroll.Status == "Processed")
        {
            throw new InvalidOperationException("Cannot reject a processed payroll");
        }

        payroll.Status = "Rejected";
        payroll.Remarks = (payroll.Remarks ?? "") + $"\nRejected by User {rejectedById}: {reason}";

        await _db.SaveChangesAsync();
        return payroll;
    }

    public async Task<Payroll> ProcessPayrollAsync(int payrollId, int processedById)
    {
        var payroll = await _db.Payrolls.FindAsync(payrollId)
            ?? throw new KeyNotFoundException($"Payroll {payrollId} not found");

        if (payroll.Status != "Approved")
        {
            throw new InvalidOperationException("Only approved payrolls can be processed");
        }

        payroll.Status = "Processed";
        payroll.ProcessedBy = processedById;
        payroll.ProcessedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return payroll;
    }
}
