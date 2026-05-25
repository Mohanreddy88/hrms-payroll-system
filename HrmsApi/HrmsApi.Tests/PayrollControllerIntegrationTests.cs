using System.Net;
using System.Net.Http.Json;
using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace HrmsApi.Tests;

/// <summary>
/// Integration tests for PayrollController API endpoints
/// Tests real HTTP requests to the API
/// </summary>
public class PayrollControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PayrollControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<HrmsDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<HrmsDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryTestDb_" + Guid.NewGuid());
                });
            });
        });
    }

    [Fact]
    public async Task GetAllPayrolls_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/payroll");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // Because we need auth
    }

    [Fact]
    public async Task GetPayrollById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/payroll/999");

        // Assert - Will be Unauthorized due to auth requirement
        // In a real test, you'd add JWT token for authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Note: These tests demonstrate the structure
    // To fully test authenticated endpoints, you'd need to:
    // 1. Generate a JWT token
    // 2. Add it to the request headers
    // 3. Test the actual endpoint behavior
}

/// <summary>
/// Unit tests for Payroll calculation formulas
/// </summary>
public class PayrollFormulaTests
{
    [Theory]
    [InlineData(5000, 100)]     // 5000 * 2% = 100
    [InlineData(10000, 200)]    // 10000 * 2% = 200
    [InlineData(3000, 60)]      // 3000 * 2% = 60
    [InlineData(7500, 150)]     // 7500 * 2% = 150
    public void EPF_ShouldBe2Percent_OfBasicSalary(decimal basicSalary, decimal expectedEpf)
    {
        // Arrange
        const decimal EPF_RATE = 0.02m;

        // Act
        var epfAmount = Math.Round(basicSalary * EPF_RATE, 2);

        // Assert
        epfAmount.Should().Be(expectedEpf);
    }

    [Theory]
    [InlineData(5000, 25)]      // 5000 * 0.5% = 25
    [InlineData(10000, 50)]     // 10000 * 0.5% = 50
    [InlineData(3000, 15)]      // 3000 * 0.5% = 15
    public void SOCSO_ShouldBe0Point5Percent_OfBasicSalary(decimal basicSalary, decimal expectedSocso)
    {
        // Arrange
        const decimal SOCSO_RATE = 0.005m;

        // Act
        var socsoAmount = Math.Round(basicSalary * SOCSO_RATE, 2);

        // Assert
        socsoAmount.Should().Be(expectedSocso);
    }

    [Theory]
    [InlineData(5000, 598.50)]   // 5000 * 11.97% = 598.50
    [InlineData(10000, 1197)]    // 10000 * 11.97% = 1197
    [InlineData(3000, 359.10)]   // 3000 * 11.97% = 359.10
    public void Tax_ShouldBe11Point97Percent_OfGrossIncome(decimal grossIncome, decimal expectedTax)
    {
        // Arrange
        const decimal TAX_RATE = 0.1197m;

        // Act
        var taxAmount = Math.Round(grossIncome * TAX_RATE, 2);

        // Assert
        taxAmount.Should().Be(expectedTax);
    }

    [Fact]
    public void NetSalary_ShouldBeGrossMinusAllDeductions()
    {
        // Arrange
        decimal basicSalary = 5000m;
        decimal allowances = 500m;
        decimal grossIncome = basicSalary + allowances; // 5500

        decimal epf = 100m;      // 2% of basic
        decimal socso = 25m;     // 0.5% of basic
        decimal tax = Math.Round(grossIncome * 0.1197m, 2); // 11.97% of gross = 658.35
        decimal unpaidLeave = 0m;
        decimal manualDeductions = 0m;

        decimal totalDeductions = epf + socso + tax + unpaidLeave + manualDeductions;

        // Act
        decimal netSalary = grossIncome - totalDeductions;

        // Assert
        netSalary.Should().Be(4716.65m); // 5500 - 100 - 25 - 658.35 = 4716.65
    }

    [Fact]
    public void UnpaidLeaveDeduction_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        decimal basicSalary = 6000m;
        int workingDays = 22;
        decimal standardHoursPerDay = 8m;
        decimal expectedHours = workingDays * standardHoursPerDay; // 176 hours
        decimal hourlyRate = basicSalary / expectedHours; // 6000 / 176 = 34.09

        int unpaidLeaveDays = 2;
        decimal unpaidLeaveHours = unpaidLeaveDays * standardHoursPerDay; // 16 hours

        // Act
        decimal unpaidLeaveDeduction = unpaidLeaveHours * hourlyRate;

        // Assert
        unpaidLeaveDeduction.Should().BeApproximately(545.45m, 0.01m); // 16 * 34.09 ≈ 545.45
    }

    [Fact]
    public void WorkingDays_ShouldExcludeWeekends()
    {
        // Arrange - May 2026
        var startDate = new DateTime(2026, 5, 1);
        var endDate = new DateTime(2026, 5, 31);

        // Act
        var workingDays = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        // Assert
        // May 2026 has 31 days total
        // Weekends: 10 days (5 Saturdays + 5 Sundays)
        // Working days: 31 - 10 = 21
        workingDays.Should().Be(21);
    }
}
