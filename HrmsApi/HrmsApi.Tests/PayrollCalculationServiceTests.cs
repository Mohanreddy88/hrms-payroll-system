using HrmsApi.Data;
using HrmsApi.Models;
using HrmsApi.Services;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace HrmsApi.Tests;

/// <summary>
/// Tests for PayrollCalculationService - Critical business logic tests
/// Tests the 2% EPF calculation, unpaid leave deductions, statutory deductions
/// </summary>
public class PayrollCalculationServiceTests
{
    private HrmsDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new HrmsDbContext(options);
    }

    [Fact]
    public async Task CalculatePayroll_WithBasicSalary_ShouldCalculate2PercentEPF()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var employee = new Employee
        {
            Id = 1,
            Name = "John Doe",
            EmployeeCode = "EMP001",
            Salary = 5000m, // RM 5000 basic salary
            Email = "john@test.com"
        };

        db.Employees.Add(employee);

        // Create approved attendance period for May 2026
        var attendancePeriod = new AttendancePeriod
        {
            Id = 1,
            EmployeeId = 1,
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 31),
            Status = "Approved",
            Days = new List<AttendancePeriodDay>
            {
                new() { Date = new DateTime(2026, 5, 1), Hours = 8 },
                new() { Date = new DateTime(2026, 5, 2), Hours = 8 },
                new() { Date = new DateTime(2026, 5, 5), Hours = 8 },
                // Total: 24 hours
            }
        };

        db.AttendancePeriods.Add(attendancePeriod);
        await db.SaveChangesAsync();

        // Act
        var result = await service.CalculatePayrollAsync(employeeId: 1, month: 5, year: 2026);

        // Assert
        result.BasicSalary.Should().Be(5000m);
        
        // EPF should be 2% of basic salary (NOT 11%)
        result.EpfAmount.Should().Be(100m); // 5000 * 0.02 = 100
        
        // SOCSO should be 0.5% of basic salary
        result.SocsoAmount.Should().Be(25m); // 5000 * 0.005 = 25
        
        // Tax should be 11.97% of gross income
        result.TaxAmount.Should().Be(598.50m); // 5000 * 0.1197 = 598.50
        
        result.GrossIncome.Should().Be(5000m);
        
        // Net = Gross - EPF - SOCSO - Tax - Deductions
        // Net = 5000 - 100 - 25 - 598.50 - 0 = 4276.50
        result.NetSalary.Should().Be(4276.50m);
    }

    [Fact]
    public async Task CalculatePayroll_WithUnpaidLeave_ShouldDeductCorrectly()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var employee = new Employee
        {
            Id = 2,
            Name = "Jane Smith",
            EmployeeCode = "EMP002",
            Salary = 6000m,
            Email = "jane@test.com"
        };

        db.Employees.Add(employee);

        // Approved attendance
        var attendancePeriod = new AttendancePeriod
        {
            Id = 2,
            EmployeeId = 2,
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 31),
            Status = "Approved",
            Days = new List<AttendancePeriodDay>
            {
                new() { Date = new DateTime(2026, 5, 1), Hours = 8 }
            }
        };

        // Unpaid leave type
        var unpaidLeaveType = new LeaveType
        {
            Id = 1,
            Name = "Unpaid Leave",
            Code = "UL",
            IsPaid = false,
            IsActive = true
        };

        // 2 days unpaid leave
        var leaveRequest = new LeaveRequest
        {
            Id = 1,
            EmployeeId = 2,
            LeaveTypeId = 1,
            StartDate = new DateTime(2026, 5, 5),
            EndDate = new DateTime(2026, 5, 6),
            Status = "Approved",
            TotalDays = 2
        };

        db.AttendancePeriods.Add(attendancePeriod);
        db.LeaveTypes.Add(unpaidLeaveType);
        db.LeaveRequests.Add(leaveRequest);
        await db.SaveChangesAsync();

        // Act
        var result = await service.CalculatePayrollAsync(employeeId: 2, month: 5, year: 2026);

        // Assert
        result.UnpaidLeaveDays.Should().Be(2);
        
        // Unpaid leave deduction should be calculated
        // Expected hours for May (working days only): around 22 days * 8 hours
        // Hourly rate = 6000 / expected hours
        // Deduction = 2 days * 8 hours * hourly rate
        result.UnpaidLeaveDeduction.Should().BeGreaterThan(0);
        
        // Net salary should be reduced by unpaid leave
        result.NetSalary.Should().BeLessThan(result.BasicSalary);
    }

    [Fact]
    public async Task CheckEligibility_WithNoAttendance_ShouldReturnNotEligible()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var employee = new Employee
        {
            Id = 3,
            Name = "No Attendance Employee",
            EmployeeCode = "EMP003",
            Salary = 4000m,
            Email = "noattendance@test.com"
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        // Act
        var result = await service.CheckEligibilityAsync(employeeId: 3, month: 5, year: 2026);

        // Assert
        result.IsEligible.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("No attendance records found"));
        result.TotalAttendancePeriods.Should().Be(0);
    }

    [Fact]
    public async Task CheckEligibility_WithPendingAttendance_ShouldReturnNotEligible()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var employee = new Employee
        {
            Id = 4,
            Name = "Pending Employee",
            EmployeeCode = "EMP004",
            Salary = 5000m,
            Email = "pending@test.com"
        };

        db.Employees.Add(employee);

        // Pending (not approved) attendance
        var attendancePeriod = new AttendancePeriod
        {
            Id = 3,
            EmployeeId = 4,
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 31),
            Status = "Submitted", // Not approved yet
            Days = new List<AttendancePeriodDay>()
        };

        db.AttendancePeriods.Add(attendancePeriod);
        await db.SaveChangesAsync();

        // Act
        var result = await service.CheckEligibilityAsync(employeeId: 4, month: 5, year: 2026);

        // Assert
        result.IsEligible.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Attendance not approved"));
        result.PendingAttendancePeriods.Should().Be(1);
        result.ApprovedAttendancePeriods.Should().Be(0);
    }

    [Fact]
    public async Task CheckEligibility_WithApprovedAttendance_ShouldReturnEligible()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var employee = new Employee
        {
            Id = 5,
            Name = "Approved Employee",
            EmployeeCode = "EMP005",
            Salary = 5000m,
            Email = "approved@test.com"
        };

        db.Employees.Add(employee);

        var attendancePeriod = new AttendancePeriod
        {
            Id = 4,
            EmployeeId = 5,
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 31),
            Status = "Approved",
            Days = new List<AttendancePeriodDay>()
        };

        db.AttendancePeriods.Add(attendancePeriod);
        await db.SaveChangesAsync();

        // Act
        var result = await service.CheckEligibilityAsync(employeeId: 5, month: 5, year: 2026);

        // Assert
        result.IsEligible.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.ApprovedAttendancePeriods.Should().Be(1);
    }

    [Fact]
    public async Task GeneratePayroll_WhenAlreadyExists_ShouldThrowException()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var employee = new Employee
        {
            Id = 6,
            Name = "Duplicate Payroll",
            EmployeeCode = "EMP006",
            Salary = 5000m,
            Email = "duplicate@test.com"
        };

        db.Employees.Add(employee);

        var attendancePeriod = new AttendancePeriod
        {
            Id = 5,
            EmployeeId = 6,
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 31),
            Status = "Approved",
            Days = new List<AttendancePeriodDay>()
        };

        db.AttendancePeriods.Add(attendancePeriod);
        await db.SaveChangesAsync();

        // Generate payroll first time
        await service.GeneratePayrollAsync(employeeId: 6, month: 5, year: 2026, createdById: 1);

        // Act & Assert - Try to generate again
        var act = async () => await service.GeneratePayrollAsync(employeeId: 6, month: 5, year: 2026, createdById: 1);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task ApprovePayroll_WhenDraft_ShouldChangeStatusToApproved()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var payroll = new Payroll
        {
            Id = 1,
            EmployeeId = 1,
            Month = 5,
            Year = 2026,
            BasicSalary = 5000,
            Status = "Draft",
            NetSalary = 4000
        };

        db.Payrolls.Add(payroll);
        await db.SaveChangesAsync();

        // Act
        var result = await service.ApprovePayrollAsync(payrollId: 1, approvedById: 10, remarks: "Looks good");

        // Assert
        result.Status.Should().Be("Approved");
        result.ApprovedBy.Should().Be(10);
        result.ApprovedOn.Should().NotBeNull();
        result.Remarks.Should().Contain("Looks good");
    }

    [Fact]
    public async Task ApprovePayroll_WhenAlreadyApproved_ShouldThrowException()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var payroll = new Payroll
        {
            Id = 2,
            EmployeeId = 1,
            Month = 5,
            Year = 2026,
            BasicSalary = 5000,
            Status = "Approved", // Already approved
            NetSalary = 4000
        };

        db.Payrolls.Add(payroll);
        await db.SaveChangesAsync();

        // Act & Assert
        var act = async () => await service.ApprovePayrollAsync(payrollId: 2, approvedById: 10);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already Approved*");
    }

    [Fact]
    public async Task ProcessPayroll_WhenApproved_ShouldChangeStatusToProcessed()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var payroll = new Payroll
        {
            Id = 3,
            EmployeeId = 1,
            Month = 5,
            Year = 2026,
            BasicSalary = 5000,
            Status = "Approved",
            NetSalary = 4000
        };

        db.Payrolls.Add(payroll);
        await db.SaveChangesAsync();

        // Act
        var result = await service.ProcessPayrollAsync(payrollId: 3, processedById: 10);

        // Assert
        result.Status.Should().Be("Processed");
        result.ProcessedBy.Should().Be(10);
        result.ProcessedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPayroll_WhenNotApproved_ShouldThrowException()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var payroll = new Payroll
        {
            Id = 4,
            EmployeeId = 1,
            Month = 5,
            Year = 2026,
            BasicSalary = 5000,
            Status = "Draft", // Not approved
            NetSalary = 4000
        };

        db.Payrolls.Add(payroll);
        await db.SaveChangesAsync();

        // Act & Assert
        var act = async () => await service.ProcessPayrollAsync(payrollId: 4, processedById: 10);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only approved payrolls can be processed*");
    }

    [Fact]
    public async Task RejectPayroll_WithReason_ShouldChangeStatusToRejected()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var service = new PayrollCalculationService(db);

        var payroll = new Payroll
        {
            Id = 5,
            EmployeeId = 1,
            Month = 5,
            Year = 2026,
            BasicSalary = 5000,
            Status = "Draft",
            NetSalary = 4000
        };

        db.Payrolls.Add(payroll);
        await db.SaveChangesAsync();

        // Act
        var result = await service.RejectPayrollAsync(payrollId: 5, rejectedById: 10, reason: "Incorrect calculations");

        // Assert
        result.Status.Should().Be("Rejected");
        result.Remarks.Should().Contain("Rejected by User 10");
        result.Remarks.Should().Contain("Incorrect calculations");
    }
}
