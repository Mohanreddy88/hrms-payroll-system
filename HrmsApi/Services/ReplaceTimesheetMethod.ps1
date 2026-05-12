# PowerShell script to replace the timesheet export method
$filePath = "C:\Users\HP\source\repos\walnut\HrmsApi\Services\ExportService.cs"

# Read entire file
$content = Get-Content $filePath -Raw

# Define the old method (to be replaced)
$oldMethod = @'
    /// <summary>
    /// Exports a single timesheet to Excel
    /// </summary>
    public async Task<byte[]> ExportTimesheetToExcelAsync(int timesheetId)
    {
        var timesheet = await _db.Timesheets
            .Include(t => t.Employee)
            .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(t => t.Id == timesheetId)
            ?? throw new KeyNotFoundException($"Timesheet {timesheetId} not found");

        var monthName = new DateTime(timesheet.Year, timesheet.Month, 1).ToString("MMMM yyyy");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"Timesheet {monthName}");

        // Title
        worksheet.Cell(1, 1).Value = "EMPLOYEE TIMESHEET";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Range("A1:B1").Merge();

        // Employee Info
        worksheet.Cell(3, 1).Value = "Employee Name:";
        worksheet.Cell(3, 1).Style.Font.Bold = true;
        worksheet.Cell(3, 2).Value = timesheet.Employee.Name;

        worksheet.Cell(4, 1).Value = "Employee ID:";
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 2).Value = $"EMP-{timesheet.Employee.Id:D6}";

        worksheet.Cell(5, 1).Value = "Department:";
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 2).Value = timesheet.Employee.Department?.Name ?? "N/A";

        worksheet.Cell(6, 1).Value = "Period:";
        worksheet.Cell(6, 1).Style.Font.Bold = true;
        worksheet.Cell(6, 2).Value = monthName;

        // Timesheet Data Headers
        int startRow = 8;
        worksheet.Cell(startRow, 1).Value = "Description";
        worksheet.Cell(startRow, 2).Value = "Days/Hours";
        
        var headerRange = worksheet.Range($"A{startRow}:B{startRow}");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data rows
        int row = startRow + 1;
        
        worksheet.Cell(row, 1).Value = "Total Working Days";
        worksheet.Cell(row, 2).Value = timesheet.TotalWorkingDays;
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        row++;

        worksheet.Cell(row, 1).Value = "Total Present";
        worksheet.Cell(row, 2).Value = timesheet.TotalPresent;
        worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        row++;

        worksheet.Cell(row, 1).Value = "Total Absent";
        worksheet.Cell(row, 2).Value = timesheet.TotalAbsent;
        worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightPink;
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        row++;

        worksheet.Cell(row, 1).Value = "Total Leave";
        worksheet.Cell(row, 2).Value = timesheet.TotalLeave;
        worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightYellow;
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        row++;

        worksheet.Cell(row, 1).Value = "Total Half Day";
        worksheet.Cell(row, 2).Value = timesheet.TotalHalfDay;
        worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightCyan;
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        row++;

        worksheet.Cell(row, 1).Value = "Total Public Holidays";
        worksheet.Cell(row, 2).Value = timesheet.TotalPublicHolidays;
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        row++;

        worksheet.Cell(row, 1).Value = "Total Work Hours";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Value = timesheet.TotalWorkHours;
        worksheet.Cell(row, 2).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        row++;

        // Status and Approval Info
        row += 2;
        worksheet.Cell(row, 1).Value = "Status:";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Value = timesheet.Status;
        row++;

        worksheet.Cell(row, 1).Value = "Generated On:";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Value = timesheet.GeneratedOn.ToString("dd MMM yyyy HH:mm");
        row++;

        if (timesheet.ApprovedBy.HasValue && timesheet.ApprovedOn.HasValue)
        {
            worksheet.Cell(row, 1).Value = "Approved On:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = timesheet.ApprovedOn.Value.ToString("dd MMM yyyy HH:mm");
            row++;
        }

        if (!string.IsNullOrWhiteSpace(timesheet.Remarks))
        {
            worksheet.Cell(row, 1).Value = "Remarks:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value = timesheet.Remarks;
            row++;
        }

        // Add borders to data area
        var dataRange = worksheet.Range($"A{startRow}:B{row - 1}");
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
'@

# Define the new method (replacement)
$newMethod = Get-Content "C:\Users\HP\source\repos\walnut\SIMPLE_REPLACEMENT_INSTRUCTIONS.md" -Raw | Select-String -Pattern '(?s)```csharp\s*///.*?```' | ForEach-Object { $_.Matches[0].Value -replace '```csharp\s*', '' -replace '\s*```', '' }

# Replace
$newContent = $content -replace [regex]::Escape($oldMethod), $newMethod

# Write back
$newContent | Set-Content $filePath -NoNewline

Write-Host "✅ Method replaced successfully!" -ForegroundColor Green
Write-Host "File: $filePath" -ForegroundColor Cyan
Write-Host "Now run: dotnet build" -ForegroundColor Yellow
'@