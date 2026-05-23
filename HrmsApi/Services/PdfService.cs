using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HrmsApi.Services;

public interface IPdfService
{
    Task<byte[]> GeneratePayslipPdfAsync(int payrollId);
}

public class PdfService : IPdfService
{
    private readonly HrmsDbContext _db;

    // ── Company details — update these to match your company ──────────────────
    private const string CompanyName    = "ESYNC SOFTWARE SERVICES SDN.BHD";
    private const string CompanyRegNo   = "(1158750-D)";
    private const string CompanyAddress = "";

    public PdfService(HrmsDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePayslipPdfAsync(int payrollId)
    {
        var payroll = await _db.Payrolls
            .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
            .Include(p => p.Employee)
                .ThenInclude(e => e.Bank)
            .Include(p => p.Adjustments)
            .FirstOrDefaultAsync(p => p.Id == payrollId)
            ?? throw new KeyNotFoundException($"Payroll {payrollId} not found");

        // Load leave balances for the employee
        var leaveBalances = await _db.EmployeeLeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == payroll.EmployeeId && lb.Year == payroll.Year)
            .ToListAsync();

        // Calculate YTD — sum of all processed/approved payrolls for same employee, same year, up to this month
        var ytdPayrolls = await _db.Payrolls
            .Where(p => p.EmployeeId == payroll.EmployeeId
                     && p.Year == payroll.Year
                     && p.Month <= payroll.Month
                     && (p.Status == "Processed" || p.Status == "Approved")
                     && p.Id != payrollId)
            .ToListAsync();

        decimal ytdBasic      = ytdPayrolls.Sum(p => p.BasicSalary);
        decimal ytdAllowances = ytdPayrolls.Sum(p => p.Allowances);
        decimal ytdEpf        = ytdPayrolls.Sum(p => p.EpfAmount);
        decimal ytdSocso      = ytdPayrolls.Sum(p => p.SocsoAmount);
        decimal ytdTax        = ytdPayrolls.Sum(p => p.TaxAmount);
        decimal ytdDeductions = ytdPayrolls.Sum(p => p.Deductions);
        decimal ytdNet        = ytdPayrolls.Sum(p => p.NetSalary);

        var monthName  = new DateTime(payroll.Year, payroll.Month, 1).ToString("MMM, yyyy");
        var payDate    = new DateTime(payroll.Year, payroll.Month,
                            DateTime.DaysInMonth(payroll.Year, payroll.Month));

        // Allowance adjustments (positive)
        var allowanceAdj = payroll.Adjustments
            .Where(a => a.AdjustmentType == "Allowance" || a.AdjustmentType == "Bonus" || a.AdjustmentType == "Overtime")
            .ToList();

        // Deduction adjustments (negative)
        var deductionAdj = payroll.Adjustments
            .Where(a => a.AdjustmentType == "Deduction")
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(35);
                page.MarginVertical(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── Company Header ────────────────────────────────────
                    col.Item().BorderBottom(2).BorderColor(Colors.Black).PaddingBottom(8).Row(row =>
                    {
                        // Logo placeholder (grey box with "eSYNC" text)
                        row.ConstantItem(80).Border(1).BorderColor(Colors.Grey.Medium)
                            .Background(Colors.Grey.Lighten3)
                            .AlignCenter().AlignMiddle()
                            .Text("eSYNC\nSoftware\nServices")
                            .FontSize(8).Bold().FontColor(Colors.Grey.Darken3);

                        row.RelativeItem().PaddingLeft(12).Column(c =>
                        {
                            c.Item().Text(CompanyName)
                                .FontSize(16).Bold().FontColor(Colors.Red.Darken2);
                            c.Item().PaddingTop(4).Text(CompanyRegNo)
                                .FontSize(10).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    col.Item().PaddingTop(4).AlignCenter()
                        .Text($"PAY SLIP FOR {new DateTime(payroll.Year, payroll.Month, 1):MMMM yyyy}".ToUpper())
                        .FontSize(10).Bold();

                    col.Item().PaddingTop(10).PaddingBottom(6).Row(row =>
                    {
                        // Left — employee info
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(text =>
                            {
                                text.Span("NAME: ").Bold();
                                text.Span(payroll.Employee.Name.ToUpper());
                            });
                            c.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span("NRIC NO: ").Bold();
                                text.Span(payroll.Employee.IcPassport ?? "—");
                            });
                            c.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span("ACCOUNT No: ").Bold();
                                text.Span(payroll.Employee.AccountNumber ?? "—");
                            });
                            c.Item().PaddingTop(2).Text(text =>
                            {
                                text.Span("DEPARTMENT: ").Bold();
                                text.Span(payroll.Employee.Department?.Name ?? "—");
                            });
                        });

                        // Right — pay info
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignRight().Text(text =>
                            {
                                text.Span("DATE: ").Bold();
                                text.Span(payDate.ToString("dd/MM/yyyy"));
                            });
                            c.Item().PaddingTop(2).AlignRight().Text(text =>
                            {
                                text.Span("Pay slip for ").Bold();
                                text.Span(monthName);
                            });
                            c.Item().PaddingTop(2).AlignRight().Text(text =>
                            {
                                text.Span("Bank: ").Bold();
                                text.Span(payroll.Employee.Bank?.Name ?? "—");
                            });
                            c.Item().PaddingTop(2).AlignRight().Text(text =>
                            {
                                text.Span("DESIGNATION: ").Bold();
                                text.Span(payroll.Employee.Designation ?? "—");
                            });
                        });
                    });

                    // ── Main Income / Deduction Table ─────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);   // INCOME description
                            cols.RelativeColumn(2);   // Current
                            cols.RelativeColumn(2);   // Y-T-D
                            cols.ConstantColumn(8);   // divider
                            cols.RelativeColumn(3);   // DEDUCTION description
                            cols.RelativeColumn(2);   // CURRENT
                            cols.RelativeColumn(2);   // Y-T-D
                        });

                        // Header row
                        void HeaderCell(string text, bool right = false)
                        {
                            var cell = table.Cell().Border(1).BorderColor(Colors.Black)
                                .Background(Colors.Grey.Lighten2).Padding(4);
                            if (right) cell.AlignRight().Text(text).Bold().FontSize(9);
                            else       cell.AlignCenter().Text(text).Bold().FontSize(9);
                        }

                        HeaderCell("INCOME");
                        HeaderCell("Current", true);
                        HeaderCell("Y-T-D", true);
                        table.Cell(); // divider
                        HeaderCell("DEDUCTION");
                        HeaderCell("CURRENT", true);
                        HeaderCell("Y-T-D", true);

                        // ── Data rows ─────────────────────────────────────
                        void IncomeRow(string label, decimal? current, decimal? ytd, bool bold = false)
                        {
                            var lCell = table.Cell().BorderLeft(1).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(3);
                            var cCell = table.Cell().BorderLeft(1).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(3).AlignRight();
                            var yCell = table.Cell().BorderLeft(1).BorderRight(1).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(3).AlignRight();

                            if (bold) { lCell.Text(label).Bold(); cCell.Text(current > 0 ? current.Value.ToString("N2") : "").Bold(); yCell.Text(ytd > 0 ? ytd.Value.ToString("N2") : "").Bold(); }
                            else      { lCell.Text(label); cCell.Text(current > 0 ? current.Value.ToString("N2") : ""); yCell.Text(ytd > 0 ? ytd.Value.ToString("N2") : ""); }
                        }

                        void DeductRow(string label, decimal? current, decimal? ytd, bool bold = false)
                        {
                            table.Cell(); // divider between income and deduction
                            var lCell = table.Cell().BorderLeft(1).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(3);
                            var cCell = table.Cell().BorderLeft(1).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(3).AlignRight();
                            var yCell = table.Cell().BorderLeft(1).BorderRight(1).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(3).AlignRight();

                            if (bold) { lCell.Text(label).Bold(); cCell.Text(current > 0 ? current.Value.ToString("N2") : "").Bold(); yCell.Text(ytd > 0 ? ytd.Value.ToString("N2") : "").Bold(); }
                            else      { lCell.Text(label); cCell.Text(current > 0 ? current.Value.ToString("N2") : ""); yCell.Text(ytd > 0 ? ytd.Value.ToString("N2") : ""); }
                        }

                        // Row 1: Basic Pay | EPF
                        IncomeRow("BASIC PAY", payroll.BasicSalary, ytdBasic + payroll.BasicSalary);
                        DeductRow("EPF", payroll.EpfAmount, ytdEpf + payroll.EpfAmount);

                        // Row 2: Allowances | SOCSO
                        IncomeRow("Allowances (MA/HRA/DA)", payroll.Allowances > 0 ? payroll.Allowances : null, payroll.Allowances > 0 ? ytdAllowances + payroll.Allowances : null);
                        DeductRow("SOCSO", payroll.SocsoAmount, ytdSocso + payroll.SocsoAmount);

                        // Row 3: Bonus/Other allowance adjustments | PCB
                        var bonusTotal = allowanceAdj.Sum(a => a.Amount);
                        IncomeRow("Bonus / Other Allowances", bonusTotal > 0 ? bonusTotal : null, bonusTotal > 0 ? bonusTotal : null);
                        DeductRow("PCB (Tax)", payroll.TaxAmount, ytdTax + payroll.TaxAmount);

                        // Row 4: empty | Other Deductions
                        var otherDedTotal = payroll.Deductions + deductionAdj.Sum(a => a.Amount);
                        IncomeRow("", null, null);
                        DeductRow("Other Deductions", otherDedTotal > 0 ? otherDedTotal : null, otherDedTotal > 0 ? ytdDeductions + otherDedTotal : null);

                        // Row 5: empty | Employer EPF
                        var employerEpf = Math.Round(payroll.BasicSalary * 0.13m, 2);
                        IncomeRow("", null, null);
                        DeductRow("Employer EPF", employerEpf, null);

                        // Row 6: empty | Employer SOCSO
                        IncomeRow("", null, null);
                        DeductRow("Employer SOCSO", 0m, null);

                        // Row 7: empty | Gross Income
                        IncomeRow("", null, null);
                        DeductRow("Gross Income", payroll.GrossIncome, ytdPayrolls.Sum(p => p.GrossIncome) + payroll.GrossIncome, true);

                        // Row 8: empty | Net Income
                        IncomeRow("", null, null);
                        DeductRow("Net Income", payroll.NetSalary, ytdNet + payroll.NetSalary, true);

                        // ── Totals row ────────────────────────────────────
                        var grossTotal = payroll.BasicSalary + payroll.Allowances + bonusTotal;
                        var ytdGrossTotal = ytdBasic + ytdAllowances + grossTotal;

                        table.Cell().ColumnSpan(3).BorderTop(1).BorderBottom(1).BorderLeft(1).BorderRight(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).Padding(5).Row(r =>
                            {
                                r.RelativeItem().Text("Gross Total").Bold();
                                r.ConstantItem(60).AlignRight().Text(grossTotal.ToString("N2")).Bold();
                                r.ConstantItem(60).AlignRight().Text(ytdGrossTotal.ToString("N2")).Bold();
                            });

                        table.Cell(); // divider

                        var endOfMonthPay = payroll.NetSalary;
                        table.Cell().ColumnSpan(3).BorderTop(1).BorderBottom(1).BorderLeft(1).BorderRight(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten2).Padding(5).Row(r =>
                            {
                                r.RelativeItem().Text("End of Month Pay").Bold();
                                r.ConstantItem(80).AlignRight().Text($"{endOfMonthPay:N2}").Bold();
                            });
                    });

                    // ── Leave Summary ─────────────────────────────────────
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        var mlBalance = leaveBalances.FirstOrDefault(lb =>
                            lb.LeaveType?.Code == "MC" || lb.LeaveType?.Name?.Contains("Medical") == true);
                        var alBalance = leaveBalances.FirstOrDefault(lb =>
                            lb.LeaveType?.Code == "AL" || lb.LeaveType?.Name?.Contains("Annual") == true);
                        var elBalance = leaveBalances.FirstOrDefault(lb =>
                            lb.LeaveType?.Code == "EL" || lb.LeaveType?.Name?.Contains("Emergency") == true);

                        var mlUsed    = mlBalance?.UsedDays ?? 0;
                        var mlBal     = mlBalance?.BalanceDays ?? 0;
                        var alUsed    = alBalance?.UsedDays ?? 0;
                        var alBal     = alBalance?.BalanceDays ?? 0;

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text($"Medical Leave = {mlUsed}, Balance = {mlBal}, YTD = {mlUsed}").FontSize(8);
                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text($"Annual Leave = {alUsed}, Balance = {alBal}, YTD = {alUsed}").FontSize(8);

                        var otherLeaves = leaveBalances
                            .Where(lb => lb.LeaveType?.Code != "MC" && lb.LeaveType?.Code != "AL"
                                      && lb.LeaveType?.Name?.Contains("Medical") != true
                                      && lb.LeaveType?.Name?.Contains("Annual") != true)
                            .ToList();
                        var otherUsed = otherLeaves.Sum(lb => lb.UsedDays);
                        var otherBal  = otherLeaves.Sum(lb => lb.BalanceDays);

                        table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text($"Other Leave = {otherUsed}, Balance = {otherBal}, YTD = {otherUsed}").FontSize(8);
                    });

                    // ── Footer ────────────────────────────────────────────
                    col.Item().PaddingTop(10).AlignCenter()
                        .Text("This is a computer-generated payslip. No signature is required.")
                        .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                    col.Item().PaddingTop(2).AlignCenter()
                        .Text($"Generated on: {DateTime.Now:dd MMM yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}
