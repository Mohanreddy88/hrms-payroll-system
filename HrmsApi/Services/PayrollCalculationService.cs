using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Services;

public class PayrollCalculationService : IPayrollCalculationService
{
    private readonly HrmsDbContext _db;

    // Malaysia statutory rates
    private const decimal EPF_EMPLOYEE_RATE    = 0.02m; // 2% employee contribution (as practised)
    private const decimal STANDARD_HOURS_PER_DAY = 8m;

    // ── SOCSO contribution table (employee share) — capped at RM5,000 salary ──────
    // Source: PERKESO contribution schedule 2026
    private static readonly (decimal MaxSalary, decimal EmployeeAmount)[] SocsoTable =
    [
        (30m,     0.10m),  (50m,     0.20m),  (70m,     0.25m),  (100m,    0.35m),
        (150m,    0.50m),  (200m,    0.65m),  (300m,    1.00m),  (400m,    1.35m),
        (500m,    1.70m),  (600m,    2.05m),  (700m,    2.40m),  (800m,    2.75m),
        (900m,    3.10m),  (1000m,   3.45m),  (1100m,   3.80m),  (1200m,   4.15m),
        (1300m,   4.50m),  (1400m,   4.85m),  (1500m,   5.20m),  (1600m,   5.55m),
        (1700m,   5.90m),  (1800m,   6.25m),  (1900m,   6.60m),  (2000m,   6.95m),
        (2100m,   7.30m),  (2200m,   7.65m),  (2300m,   8.00m),  (2400m,   8.35m),
        (2500m,   8.70m),  (2600m,   9.05m),  (2700m,   9.40m),  (2800m,   9.75m),
        (2900m,  10.10m),  (3000m,  10.45m),  (3100m,  10.80m),  (3200m,  11.15m),
        (3300m,  11.50m),  (3400m,  11.85m),  (3500m,  12.20m),  (3600m,  12.55m),
        (3700m,  12.90m),  (3800m,  13.25m),  (3900m,  13.60m),  (4000m,  13.95m),
        (4500m,  14.75m),  (5000m,  16.60m)   // ceiling: RM5,000 → RM19.75 employee
    ];
    // Above RM5,000: fixed at RM19.75 employee
    private const decimal SOCSO_CEILING_SALARY = 5000m;
    private const decimal SOCSO_CEILING_AMOUNT  = 19.75m;

    // ── PCB / MTD income tax brackets (chargeable annual income) ─────────────────
    // Source: LHDN / IRB Malaysia 2026
    private static readonly (decimal From, decimal To, decimal Rate, decimal PrevTax)[] PcbBrackets =
    [
        (0m,       5000m,    0.00m,  0m),
        (5001m,    20000m,   0.01m,  0m),
        (20001m,   35000m,   0.03m,  150m),
        (35001m,   50000m,   0.08m,  600m),
        (50001m,   70000m,   0.13m,  1800m),
        (70001m,   100000m,  0.21m,  4400m),
        (100001m,  400000m,  0.24m,  10700m),
        (400001m,  600000m,  0.245m, 82700m),
        (600001m,  2000000m, 0.25m,  131700m),
        (2000001m, decimal.MaxValue, 0.26m, 481700m)
    ];

    public PayrollCalculationService(HrmsDbContext db)
    {
        _db = db;
    }

    public async Task<PayrollEligibilityResult> CheckEligibilityAsync(int employeeId, int month, int year)
    {
        var result = new PayrollEligibilityResult();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Check if payroll already exists — warn but don't block (admin can regenerate)
        var existingPayroll = await _db.Payrolls
            .AnyAsync(p => p.EmployeeId == employeeId && p.Month == month && p.Year == year);

        if (existingPayroll)
        {
            result.Warnings.Add($"Payroll already exists for {new DateTime(year, month, 1):MMMM yyyy}. Generating will create a new record.");
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

        // Total calendar days in the month — used as divisor for daily rate
        var totalCalendarDays = DateTime.DaysInMonth(year, month);

        // Fetch public holidays for this month (national + any state)
        var publicHolidayDates = await _db.PublicHolidays
            .Where(h => h.Year == year && h.Date.Month == month)
            .Select(h => h.Date.Date)
            .ToListAsync();
        var publicHolidaySet = publicHolidayDates.ToHashSet();

        // Working days = weekdays excluding public holidays (for attendance tracking / ExpectedHours)
        var workingDays = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday
                && date.DayOfWeek != DayOfWeek.Sunday
                && !publicHolidaySet.Contains(date.Date))
            {
                workingDays++;
            }
        }
        result.WorkingDays = workingDays;
        result.ExpectedHours = workingDays * STANDARD_HOURS_PER_DAY;

        // Daily rate = BasicSalary / total calendar days in month
        // Public holidays are paid — employees are not penalised for them
        result.DailyRate = employee.Salary / totalCalendarDays;

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
            var leaveDaysInMonth = CalculateLeaveDaysInMonth(leave.StartDate, leave.EndDate, startDate, endDate, publicHolidaySet);

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

        // Hourly rate derived from daily rate (kept for attendance-based calculations)
        result.HourlyRate = result.DailyRate / STANDARD_HOURS_PER_DAY;

        // Unpaid leave deduction = number of unpaid days × daily rate
        // (salary / total calendar days — public holidays are paid, so no deduction for them)
        result.UnpaidLeaveDeduction = unpaidLeaveDays * result.DailyRate;

        // Update leave request deductions
        foreach (var leaveSummary in result.LeaveRequests)
        {
            if (!leaveSummary.IsPaid)
            {
                leaveSummary.DeductionAmount = leaveSummary.TotalDays * result.DailyRate;
            }
        }

        // Calculate salary components
        result.Allowances = 0; // Can be added manually via adjustments
        result.ManualDeductions = 0; // Can be added manually via adjustments
        result.GrossIncome = result.BasicSalary + result.Allowances;

        // Statutory deductions
        result.EpfAmount   = Math.Round(result.BasicSalary * EPF_EMPLOYEE_RATE, 2);
        result.SocsoAmount = CalculateSocso(result.BasicSalary);
        result.TaxAmount   = CalculatePcb(result.BasicSalary, result.EpfAmount);

        result.TotalDeductions = result.UnpaidLeaveDeduction
                               + result.EpfAmount
                               + result.SocsoAmount
                               + result.TaxAmount
                               + result.ManualDeductions;

        result.NetSalary = result.GrossIncome - result.TotalDeductions;

        return result;
    }

    private decimal CalculateLeaveDaysInMonth(DateTime leaveStart, DateTime leaveEnd,
        DateTime monthStart, DateTime monthEnd, HashSet<DateTime> publicHolidaySet)
    {
        var overlapStart = leaveStart > monthStart ? leaveStart : monthStart;
        var overlapEnd   = leaveEnd   < monthEnd   ? leaveEnd   : monthEnd;

        if (overlapStart > overlapEnd) return 0;

        // Count weekdays that are NOT public holidays
        // (a leave day on a public holiday doesn't consume a leave day)
        decimal days = 0;
        for (var date = overlapStart; date <= overlapEnd; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday
                && date.DayOfWeek != DayOfWeek.Sunday
                && !publicHolidaySet.Contains(date.Date))
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

        // If a payroll already exists for this month, remove it before inserting the new one
        // (unique constraint on EmployeeId+Month+Year)
        var existing = await _db.Payrolls
            .Include(p => p.AttendancePeriods)
            .Include(p => p.LeaveRequests)
            .Include(p => p.Adjustments)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.Month == month && p.Year == year);

        if (existing != null)
        {
            _db.PayrollAttendancePeriods.RemoveRange(existing.AttendancePeriods);
            _db.PayrollLeaveRequests.RemoveRange(existing.LeaveRequests);
            _db.PayrollAdjustments.RemoveRange(existing.Adjustments);
            _db.Payrolls.Remove(existing);
            await _db.SaveChangesAsync();
        }

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

    // ── SOCSO lookup table calculation ───────────────────────────────────────────
    private static decimal CalculateSocso(decimal monthlySalary)
    {
        if (monthlySalary <= 0) return 0m;
        if (monthlySalary > SOCSO_CEILING_SALARY) return SOCSO_CEILING_AMOUNT;

        foreach (var (maxSalary, employeeAmount) in SocsoTable)
        {
            if (monthlySalary <= maxSalary)
                return employeeAmount;
        }
        return SOCSO_CEILING_AMOUNT;
    }

    // ── PCB / MTD progressive tax calculation ────────────────────────────────────
    // Formula:
    //   Annual chargeable = (monthly salary × 12) − (EPF annual) − personal relief RM9,000
    //   Annual tax        = tax on chargeable income using brackets
    //   Monthly PCB       = annual tax ÷ 12
    private static decimal CalculatePcb(decimal monthlySalary, decimal monthlyEpf)
    {
        if (monthlySalary <= 0) return 0m;

        const decimal PERSONAL_RELIEF = 9000m;

        var annualGross      = monthlySalary * 12m;
        var annualEpf        = monthlyEpf    * 12m;
        var chargeableIncome = annualGross - annualEpf - PERSONAL_RELIEF;

        if (chargeableIncome <= 0) return 0m;

        decimal annualTax = 0m;

        foreach (var (from, to, rate, prevTax) in PcbBrackets)
        {
            if (chargeableIncome <= from - 1) break;

            var bracketTop    = Math.Min(chargeableIncome, to);
            var taxableInBand = bracketTop - (from - 1);
            annualTax = prevTax + (taxableInBand * rate);

            if (chargeableIncome <= to) break;
        }

        var monthlyPcb = Math.Round(annualTax / 12m, 2);
        return monthlyPcb;
    }
}
