using ClosedXML.Excel;
using HrmsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Services;

public interface IExportService
{
    Task<byte[]> ExportPayrollToExcelAsync(int year, int month);
    Task<byte[]> ExportEmployeesToExcelAsync();
    Task<byte[]> ExportAttendanceToExcelAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> ExportTimesheetToExcelAsync(int timesheetId);
}

public class ExportService : IExportService
{
    private readonly HrmsDbContext _db;

    public ExportService(HrmsDbContext db) => _db = db;

    /// <summary>
    /// Exports payroll data to Excel for a specific month
    /// </summary>
    public async Task<byte[]> ExportPayrollToExcelAsync(int year, int month)
    {
        var payrolls = await _db.Payrolls
            .Include(p => p.Employee)
            .ThenInclude(e => e.Department)
            .Where(p => p.Year == year && p.Month == month)
            .OrderBy(p => p.Employee.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"Payroll {month}-{year}");

        // Headers
        worksheet.Cell(1, 1).Value = "Employee Name";
        worksheet.Cell(1, 2).Value = "Department";
        worksheet.Cell(1, 3).Value = "Designation";
        worksheet.Cell(1, 4).Value = "Basic Salary";
        worksheet.Cell(1, 5).Value = "Allowances";
        worksheet.Cell(1, 6).Value = "Gross Income";
        worksheet.Cell(1, 7).Value = "EPF";
        worksheet.Cell(1, 8).Value = "SOCSO";
        worksheet.Cell(1, 9).Value = "Tax (PCB)";
        worksheet.Cell(1, 10).Value = "Other Deductions";
        worksheet.Cell(1, 11).Value = "Net Salary";

        // Style headers
        var headerRow = worksheet.Range("A1:K1");
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data rows
        int row = 2;
        foreach (var p in payrolls)
        {
            worksheet.Cell(row, 1).Value = p.Employee.Name;
            worksheet.Cell(row, 2).Value = p.Employee.Department?.Name ?? "Unassigned";
            worksheet.Cell(row, 3).Value = p.Employee.Designation;
            worksheet.Cell(row, 4).Value = p.BasicSalary;
            worksheet.Cell(row, 5).Value = p.Allowances;
            worksheet.Cell(row, 6).Value = p.GrossIncome;
            worksheet.Cell(row, 7).Value = p.EpfAmount;
            worksheet.Cell(row, 8).Value = p.SocsoAmount;
            worksheet.Cell(row, 9).Value = p.TaxAmount;
            worksheet.Cell(row, 10).Value = p.Deductions;
            worksheet.Cell(row, 11).Value = p.NetSalary;

            // Format currency columns
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        // Add totals row
        worksheet.Cell(row, 3).Value = "TOTAL";
        worksheet.Cell(row, 3).Style.Font.Bold = true;
        worksheet.Cell(row, 4).FormulaA1 = $"SUM(D2:D{row - 1})";
        worksheet.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
        worksheet.Cell(row, 6).FormulaA1 = $"SUM(F2:F{row - 1})";
        worksheet.Cell(row, 7).FormulaA1 = $"SUM(G2:G{row - 1})";
        worksheet.Cell(row, 8).FormulaA1 = $"SUM(H2:H{row - 1})";
        worksheet.Cell(row, 9).FormulaA1 = $"SUM(I2:I{row - 1})";
        worksheet.Cell(row, 10).FormulaA1 = $"SUM(J2:J{row - 1})";
        worksheet.Cell(row, 11).FormulaA1 = $"SUM(K2:K{row - 1})";

        var totalRow = worksheet.Range($"C{row}:K{row}");
        totalRow.Style.Font.Bold = true;
        totalRow.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Exports employee directory to Excel
    /// </summary>
    public async Task<byte[]> ExportEmployeesToExcelAsync()
    {
        var employees = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Bank)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Department!.Name)
            .ThenBy(e => e.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Employees");

        // Headers
        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Email";
        worksheet.Cell(1, 3).Value = "Phone";
        worksheet.Cell(1, 4).Value = "Department";
        worksheet.Cell(1, 5).Value = "Designation";
        worksheet.Cell(1, 6).Value = "Join Date";
        worksheet.Cell(1, 7).Value = "Salary";
        worksheet.Cell(1, 8).Value = "Bank";
        worksheet.Cell(1, 9).Value = "Account Number";
        worksheet.Cell(1, 10).Value = "IC/Passport";
        worksheet.Cell(1, 11).Value = "Tax Number";

        // Style headers
        var headerRow = worksheet.Range("A1:K1");
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data rows
        int row = 2;
        foreach (var emp in employees)
        {
            worksheet.Cell(row, 1).Value = emp.Name;
            worksheet.Cell(row, 2).Value = emp.Email;
            worksheet.Cell(row, 3).Value = emp.Phone;
            worksheet.Cell(row, 4).Value = emp.Department?.Name ?? "Unassigned";
            worksheet.Cell(row, 5).Value = emp.Designation;
            worksheet.Cell(row, 6).Value = emp.JoinDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 7).Value = emp.Salary;
            worksheet.Cell(row, 8).Value = emp.Bank?.Name ?? "";
            worksheet.Cell(row, 9).Value = emp.AccountNumber;
            worksheet.Cell(row, 10).Value = emp.IcPassport;
            worksheet.Cell(row, 11).Value = emp.TaxNumber;

            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Exports attendance records to Excel for a date range
    /// </summary>
    public async Task<byte[]> ExportAttendanceToExcelAsync(DateTime startDate, DateTime endDate)
    {
        var records = await _db.Attendances
            .Include(a => a.Employee)
            .ThenInclude(e => e.Department)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Employee.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Attendance");

        // Headers
        worksheet.Cell(1, 1).Value = "Date";
        worksheet.Cell(1, 2).Value = "Employee Name";
        worksheet.Cell(1, 3).Value = "Department";
        worksheet.Cell(1, 4).Value = "Status";
        worksheet.Cell(1, 5).Value = "Check In";
        worksheet.Cell(1, 6).Value = "Check Out";
        worksheet.Cell(1, 7).Value = "Work Hours";
        worksheet.Cell(1, 8).Value = "Remarks";

        // Style headers
        var headerRow = worksheet.Range("A1:H1");
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data rows
        int row = 2;
        foreach (var a in records)
        {
            worksheet.Cell(row, 1).Value = a.Date.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 2).Value = a.Employee.Name;
            worksheet.Cell(row, 3).Value = a.Employee.Department?.Name ?? "Unassigned";
            worksheet.Cell(row, 4).Value = a.Status;
            worksheet.Cell(row, 5).Value = a.CheckIn?.ToString("HH:mm") ?? "";
            worksheet.Cell(row, 6).Value = a.CheckOut?.ToString("HH:mm") ?? "";
            worksheet.Cell(row, 7).Value = a.WorkHours;
            worksheet.Cell(row, 8).Value = a.Remarks;

            // Color code by status
            var statusCell = worksheet.Cell(row, 4);
            switch (a.Status)
            {
                case "Present":
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    break;
                case "Absent":
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                    break;
                case "Leave":
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    break;
                case "HalfDay":
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightCyan;
                    break;
            }

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Exports timesheet with daily attendance details - PERSONNEL TIME SHEET / DAILY ACTIVITIES REPORT format
    /// </summary>
    public async Task<byte[]> ExportTimesheetToExcelAsync(int timesheetId)
    {
        var timesheet = await _db.Timesheets
            .Include(t => t.Employee)
            .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(t => t.Id == timesheetId)
            ?? throw new KeyNotFoundException($"Timesheet {timesheetId} not found");

        // Get attendance records for the month
        var startDate = new DateTime(timesheet.Year, timesheet.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _db.Attendances
            .Where(a => a.EmployeeId == timesheet.EmployeeId && a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ToListAsync();

        // Get public holidays for the month
        var holidays = await _db.PublicHolidays
            .Where(h => h.Year == timesheet.Year && h.Date >= startDate && h.Date <= endDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Timesheet");

        // === HEADER ===
        ws.Cell(2, 1).Value = "PERSONNEL TIME SHEET / DAILY ACTIVITIES REPORT";
        ws.Range(2, 1, 2, 10).Merge();
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // === EMPLOYEE INFO ===
        ws.Cell(3, 1).Value = "EMPLOYEE:";
        ws.Cell(3, 2).Value = timesheet.Employee.Name;
        ws.Range(3, 2, 3, 4).Merge();
        ws.Cell(3, 2).Style.Font.Bold = true;

        ws.Cell(3, 6).Value = "STATUS:";
        ws.Cell(3, 7).Value = timesheet.Status;
        ws.Range(3, 7, 3, 8).Merge();

        ws.Cell(3, 9).Value = $"{startDate:dd-MMM-yyyy} to {endDate:dd-MMM-yyyy}";
        ws.Range(3, 9, 3, 10).Merge();
        ws.Cell(3, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(4, 1).Value = "DESIGNATION:";
        ws.Cell(4, 2).Value = timesheet.Employee.Designation;
        ws.Range(4, 2, 4, 6).Merge();

        // === COLUMN HEADERS ===
        int headerRow = 7;
        ws.Cell(headerRow, 1).Value = "DAY";
        ws.Cell(headerRow, 2).Value = "DATE";
        ws.Cell(headerRow, 3).Value = "TIME IN";
        ws.Cell(headerRow, 4).Value = "ICH: TIME IN";
        ws.Cell(headerRow, 5).Value = "CH: TIME IN";
        ws.Cell(headerRow, 6).Value = "ICH: TIME OUT";
        ws.Cell(headerRow, 7).Value = "CH: TIME OUT";
        ws.Cell(headerRow, 8).Value = "TIME OUT";
        ws.Cell(headerRow, 9).Value = "TOTAL HRS WORKED (IF ANY)";
        ws.Cell(headerRow, 10).Value = "OVERTIME (IF ANY)";

        var headerRange = ws.Range(headerRow, 1, headerRow, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // === EXAMPLE ROW ===
        int exampleRow = headerRow + 1;
        ws.Cell(exampleRow, 1).Value = "EXAMPLE";
        ws.Cell(exampleRow, 2).Value = "01/01/2026";
        ws.Cell(exampleRow, 3).Value = "✓ 00:00";
        ws.Cell(exampleRow, 4).Value = "//////";
        ws.Cell(exampleRow, 5).Value = "//////";
        ws.Cell(exampleRow, 6).Value = "//////";
        ws.Cell(exampleRow, 7).Value = "//////";
        ws.Cell(exampleRow, 8).Value = "//////";
        ws.Cell(exampleRow, 9).Value = "0";
        ws.Cell(exampleRow, 10).Value = "0";

        var exampleRange = ws.Range(exampleRow, 1, exampleRow, 10);
        exampleRange.Style.Fill.BackgroundColor = XLColor.Yellow;
        exampleRange.Style.Font.Bold = true;
        exampleRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // === DAILY ATTENDANCE DATA ===
        int dataStartRow = exampleRow + 1;
        int currentRow = dataStartRow;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayOfWeek = date.DayOfWeek;
            var dayName = date.ToString("dddd");
            var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
            var holiday = holidays.FirstOrDefault(h => h.Date.Date == date.Date);
            var attendance = attendances.FirstOrDefault(a => a.Date.Date == date.Date);

            ws.Cell(currentRow, 1).Value = dayName;
            ws.Cell(currentRow, 2).Value = date.ToString("dd/MM/yyyy");

            if (isWeekend)
            {
                ws.Cell(currentRow, 3).Value = "WEEKEND";
                ws.Range(currentRow, 3, currentRow, 8).Merge();
                ws.Cell(currentRow, 3).Style.Font.Bold = true;
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(currentRow, 1, currentRow, 10).Style.Fill.BackgroundColor = XLColor.LightGreen;
                ws.Cell(currentRow, 9).Value = 0;
                ws.Cell(currentRow, 10).Value = 0;
            }
            else if (holiday != null)
            {
                ws.Cell(currentRow, 3).Value = $"Public Holiday - {holiday.Name}";
                ws.Range(currentRow, 3, currentRow, 8).Merge();
                ws.Cell(currentRow, 3).Style.Font.Bold = true;
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Range(currentRow, 1, currentRow, 10).Style.Fill.BackgroundColor = XLColor.LightBlue;
                ws.Cell(currentRow, 9).Value = 0;
                ws.Cell(currentRow, 10).Value = 0;
            }
            else if (attendance != null)
            {
                var checkInTime = attendance.CheckIn?.ToString("HH:mm") ?? "";
                var checkOutTime = attendance.CheckOut?.ToString("HH:mm") ?? "";

                ws.Cell(currentRow, 3).Value = string.IsNullOrEmpty(checkInTime) ? "" : $"✓ {checkInTime}";
                ws.Cell(currentRow, 4).Value = string.IsNullOrEmpty(checkInTime) ? "" : "//////";
                ws.Cell(currentRow, 5).Value = string.IsNullOrEmpty(checkInTime) ? "" : "//////";
                ws.Cell(currentRow, 6).Value = string.IsNullOrEmpty(checkOutTime) ? "" : "//////";
                ws.Cell(currentRow, 7).Value = string.IsNullOrEmpty(checkOutTime) ? "" : "//////";
                ws.Cell(currentRow, 8).Value = string.IsNullOrEmpty(checkOutTime) ? "" : $"✓ {checkOutTime}";
                ws.Cell(currentRow, 9).Value = attendance.WorkHours;
                ws.Cell(currentRow, 10).Value = 0;

                if (attendance.Status == "Absent")
                    ws.Range(currentRow, 1, currentRow, 10).Style.Fill.BackgroundColor = XLColor.LightPink;
                else if (attendance.Status == "HalfDay")
                    ws.Range(currentRow, 1, currentRow, 10).Style.Fill.BackgroundColor = XLColor.LightCyan;
            }
            else
            {
                ws.Cell(currentRow, 3).Value = "";
                ws.Cell(currentRow, 9).Value = 0;
                ws.Cell(currentRow, 10).Value = 0;
            }

            ws.Range(currentRow, 1, currentRow, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(currentRow, 1, currentRow, 10).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            currentRow++;
        }

        // === TOTALS ROW ===
        int totalsRow = currentRow;
        ws.Cell(totalsRow, 1).Value = "Totals:";
        ws.Cell(totalsRow, 1).Style.Font.Bold = true;
        ws.Cell(totalsRow, 9).Value = timesheet.TotalWorkHours;
        ws.Cell(totalsRow, 9).Style.Font.Bold = true;
        ws.Cell(totalsRow, 9).Style.Fill.BackgroundColor = XLColor.Yellow;
        ws.Cell(totalsRow, 10).Value = 0;
        ws.Range(totalsRow, 1, totalsRow, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        // === SIGNATURE SECTION ===
        int sigRow = totalsRow + 3;
        
        ws.Cell(sigRow, 1).Value = "SUBMITTED BY";
        ws.Range(sigRow, 1, sigRow, 3).Merge();
        ws.Cell(sigRow, 1).Style.Font.Bold = true;
        ws.Cell(sigRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(sigRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(sigRow, 4).Value = "VERIFIED BY";
        ws.Range(sigRow, 4, sigRow, 6).Merge();
        ws.Cell(sigRow, 4).Style.Font.Bold = true;
        ws.Cell(sigRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(sigRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(sigRow, 7).Value = "APPROVED BY";
        ws.Range(sigRow, 7, sigRow, 10).Merge();
        ws.Cell(sigRow, 7).Style.Font.Bold = true;
        ws.Cell(sigRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(sigRow, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        int detailsRow = sigRow + 2;
        
        ws.Cell(detailsRow, 1).Value = "NAME: " + timesheet.Employee.Name;
        ws.Range(detailsRow, 1, detailsRow, 3).Merge();
        ws.Cell(detailsRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(detailsRow, 4).Value = "NAME:";
        ws.Range(detailsRow, 4, detailsRow, 6).Merge();
        ws.Cell(detailsRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(detailsRow, 7).Value = "NAME:";
        ws.Range(detailsRow, 7, detailsRow, 10).Merge();
        ws.Cell(detailsRow, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(detailsRow + 1, 1).Value = $"DESIGNATION: {timesheet.Employee.Designation}";
        ws.Range(detailsRow + 1, 1, detailsRow + 1, 3).Merge();
        ws.Cell(detailsRow + 1, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(detailsRow + 1, 4).Value = "DESIGNATION:";
        ws.Range(detailsRow + 1, 4, detailsRow + 1, 6).Merge();
        ws.Cell(detailsRow + 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(detailsRow + 1, 7).Value = "DESIGNATION:";
        ws.Range(detailsRow + 1, 7, detailsRow + 1, 10).Merge();
        ws.Cell(detailsRow + 1, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(detailsRow + 2, 1).Value = $"DATE: {DateTime.Now:dd-MMM-yyyy}";
        ws.Range(detailsRow + 2, 1, detailsRow + 2, 3).Merge();
        ws.Cell(detailsRow + 2, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(detailsRow + 2, 4).Value = "DATE:";
        ws.Range(detailsRow + 2, 4, detailsRow + 2, 6).Merge();
        ws.Cell(detailsRow + 2, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Cell(detailsRow + 2, 7).Value = "DATE:";
        ws.Range(detailsRow + 2, 7, detailsRow + 2, 10).Merge();
        ws.Cell(detailsRow + 2, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        int infoRow = detailsRow + 4;
        ws.Cell(infoRow, 1).Value = "NO OF ANNUAL LEAVE";
        ws.Cell(infoRow, 3).Value = "01/2";
        ws.Cell(infoRow, 4).Value = "NO OF MEDICAL LEAVE";
        ws.Cell(infoRow, 6).Value = "01/4";
        ws.Cell(infoRow, 7).Value = "NOTE:";

        ws.Column(1).Width = 12;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 10;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 12;
        ws.Column(8).Width = 10;
        ws.Column(9).Width = 15;
        ws.Column(10).Width = 15;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
