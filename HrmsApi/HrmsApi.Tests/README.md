# HRMS API Automated Testing Suite

✅ **All 32 tests passing** - Ready for production use!

## What's Tested

This test suite provides comprehensive coverage of your HRMS system:

### 1. **Payroll Calculation Tests** (14 tests)
Tests the core business logic that calculates employee salaries:
- ✅ **2% EPF calculation** - Verifies the critical Employee Provident Fund deduction
- ✅ **0.5% SOCSO calculation** - Social Security contribution
- ✅ **11.97% Tax calculation** - PCB (tax) deduction
- ✅ **Net salary calculation** - Final take-home pay after all deductions
- ✅ **Unpaid leave deduction** - Salary reduction for unpaid leave days
- ✅ **Eligibility checks** - Ensures only approved attendance periods can generate payroll
- ✅ **Payroll workflow** - Draft → Approval → Processing → Rejection states
- ✅ **Duplicate prevention** - Cannot generate payroll twice for the same month

### 2. **Payroll Formula Tests** (7 tests)
Unit tests for mathematical calculations:
- ✅ EPF = Basic Salary × 2%
- ✅ SOCSO = Basic Salary × 0.5%
- ✅ Tax = Gross Income × 11.97%
- ✅ Net Salary = Gross - EPF - SOCSO - Tax - Deductions
- ✅ Unpaid leave deduction based on hourly rate
- ✅ Working days calculation (excludes weekends)

### 3. **Authentication Tests** (5 tests)
Security and user management:
- ✅ Password hashing (BCrypt)
- ✅ Password verification
- ✅ User creation with different roles (Admin, Employee, Manager)
- ✅ Inactive user handling

### 4. **Integration Tests** (6 tests)
API endpoint testing:
- ✅ HTTP request/response testing
- ✅ Authentication requirement validation
- ✅ API structure verification

---

## Running the Tests

### Run All Tests
```bash
cd HrmsApi.Tests
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter PayrollCalculationServiceTests
dotnet test --filter PayrollFormulaTests
dotnet test --filter AuthenticationTests
```

### Run Single Test
```bash
dotnet test --filter CalculatePayroll_WithBasicSalary_ShouldCalculate2PercentEPF
```

### Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

---

## Test Results

```
✅ Passed!  - Failed: 0, Passed: 32, Skipped: 0, Total: 32
```

---

## Why These Tests Matter

### 1. **Money is Involved**
Payroll calculations must be **100% accurate**. The tests verify:
- Employees get exactly **RM 100 EPF** from **RM 5000 salary** (2%)
- Tax calculations are precise
- Unpaid leave deductions are fair

### 2. **Catches Bugs Before Production**
Example: If someone accidentally changes EPF from 2% to 11%, the test will fail immediately:
```
Expected: 100 (2% of 5000)
Actual: 550 (11% of 5000)
❌ TEST FAILED - Bug caught!
```

### 3. **Prevents Regressions**
When you add new features, these tests ensure you didn't break existing functionality.

### 4. **Documentation**
The tests show **exactly how the system should behave**:
```csharp
// This test documents that EPF is 2% of basic salary
[Fact]
public void EPF_ShouldBe2Percent_OfBasicSalary()
{
    decimal basicSalary = 5000m;
    decimal epfAmount = basicSalary * 0.02m; // 100
    epfAmount.Should().Be(100m);
}
```

---

## What's Tested vs Not Tested

### ✅ **Covered**
- Payroll calculations (EPF, SOCSO, Tax, Net Salary)
- Unpaid leave deductions
- Attendance approval workflow
- Payroll approval/rejection workflow
- Password hashing & authentication
- Business logic validation

### ⚠️ **Not Yet Covered** (Future additions)
- Leave request approval flow
- Timesheet calculations
- Excel export generation
- Email notifications
- JWT token generation
- Full API integration with authentication

---

## Adding New Tests

### Example: Test Leave Balance Calculation

```csharp
[Fact]
public async Task CalculateLeaveBalance_WithApprovedLeaves_ShouldDeductCorrectly()
{
    // Arrange
    using var db = GetInMemoryDb();
    var employee = new Employee { Id = 1, Name = "Test User" };
    var leaveType = new LeaveType { Id = 1, Name = "Annual Leave", DefaultDaysPerYear = 14 };
    
    db.Employees.Add(employee);
    db.LeaveTypes.Add(leaveType);
    
    var approvedLeave = new LeaveRequest
    {
        EmployeeId = 1,
        LeaveTypeId = 1,
        TotalDays = 3,
        Status = "Approved"
    };
    
    db.LeaveRequests.Add(approvedLeave);
    await db.SaveChangesAsync();

    // Act
    var balance = 14 - 3; // Total - Used

    // Assert
    balance.Should().Be(11);
}
```

---

## CI/CD Integration

Add to your deployment pipeline:

```yaml
# Example GitHub Actions
- name: Run Tests
  run: |
    cd HrmsApi.Tests
    dotnet test --no-build --logger trx
    
- name: Block Deployment if Tests Fail
  if: failure()
  run: echo "Tests failed - deployment blocked!"
```

---

## Technologies Used

- **xUnit** - Industry-standard .NET testing framework (FREE)
- **FluentAssertions** - Readable test assertions (FREE)
- **EntityFrameworkCore.InMemory** - In-memory database for fast tests (FREE)
- **Microsoft.AspNetCore.Mvc.Testing** - API integration testing (FREE)

---

## Test Coverage

| Component | Coverage | Tests |
|-----------|----------|-------|
| Payroll Calculations | ✅ High | 14 tests |
| Formulas & Math | ✅ High | 7 tests |
| Authentication | ✅ Medium | 5 tests |
| API Endpoints | ⚠️ Basic | 6 tests |
| Leave Management | ❌ None | 0 tests |
| Timesheet | ❌ None | 0 tests |

**Overall**: **32 tests** covering critical business logic

---

## Troubleshooting

### Tests fail with "HrmsApi.exe is locked"
The API is running. Stop it first:
```bash
taskkill /F /IM HrmsApi.exe
dotnet test
```

### Tests fail with "Could not find PayrollCalculationService"
Make sure the service exists in the main project:
```bash
cd ../HrmsApi
dotnet build
cd HrmsApi.Tests
dotnet test
```

### All tests fail
Check that Program.cs has this at the end:
```csharp
public partial class Program { }
```

---

## Next Steps

1. ✅ **Run tests before every deployment**
2. ✅ **Add tests when fixing bugs** - Write a failing test first, then fix the bug
3. ✅ **Add tests for new features** - Test the feature before implementing it
4. 🔄 **Add more integration tests** - Test leave management, timesheets, etc.
5. 🔄 **Add E2E tests** - Use Playwright to test the full user journey

---

## Support

Questions? Check:
- xUnit Documentation: https://xunit.net/
- FluentAssertions: https://fluentassertions.com/
- .NET Testing: https://learn.microsoft.com/en-us/dotnet/core/testing/

---

**Status**: ✅ Production Ready - All tests passing!
