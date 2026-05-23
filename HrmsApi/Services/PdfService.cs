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
    private readonly IWebHostEnvironment _env;

    private const string CompanyName  = "ESYNC SOFTWARE SERVICES SDN.BHD";
    private const string CompanyRegNo = "(1158750-D)";

    public PdfService(HrmsDbContext db, IWebHostEnvironment env)
    {
        _db  = db;
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePayslipPdfAsync(int payrollId)
    {
        var payroll = await _db.Payrolls
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Include(p => p.Employee).ThenInclude(e => e.Bank)
            .Include(p => p.Adjustments)
            .FirstOrDefaultAsync(p => p.Id == payrollId)
            ?? throw new KeyNotFoundException($"Payroll {payrollId} not found");

        var leaveBalances = await _db.EmployeeLeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == payroll.EmployeeId && lb.Year == payroll.Year)
            .ToListAsync();

        var ytdPayrolls = await _db.Payrolls
            .Where(p => p.EmployeeId == payroll.EmployeeId
                     && p.Year       == payroll.Year
                     && p.Month      <= payroll.Month
                     && p.Id         != payrollId
                     && (p.Status == "Processed" || p.Status == "Approved"))
            .ToListAsync();

        decimal ytdBasic  = ytdPayrolls.Sum(p => p.BasicSalary);
        decimal ytdAllow  = ytdPayrolls.Sum(p => p.Allowances);
        decimal ytdEpf    = ytdPayrolls.Sum(p => p.EpfAmount);
        decimal ytdSocso  = ytdPayrolls.Sum(p => p.SocsoAmount);
        decimal ytdTax    = ytdPayrolls.Sum(p => p.TaxAmount);
        decimal ytdDed    = ytdPayrolls.Sum(p => p.Deductions);
        decimal ytdGross  = ytdPayrolls.Sum(p => p.GrossIncome);
        decimal ytdNet    = ytdPayrolls.Sum(p => p.NetSalary);

        var bonusTotal    = payroll.Adjustments
            .Where(a => a.AdjustmentType is "Allowance" or "Bonus" or "Overtime")
            .Sum(a => a.Amount);
        var otherDedTotal = payroll.Deductions + payroll.Adjustments
            .Where(a => a.AdjustmentType == "Deduction").Sum(a => a.Amount);

        var payDate   = new DateTime(payroll.Year, payroll.Month,
                            DateTime.DaysInMonth(payroll.Year, payroll.Month));
        var monthName = new DateTime(payroll.Year, payroll.Month, 1).ToString("MMM, yyyy");

        // Logo — try file system path (works both locally and in Docker/Railway)
        byte[]? logoBytes = null;
        var logoPaths = new[]
        {
            Path.Combine(_env.ContentRootPath, "Assets", "EsyncLogo.png"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "EsyncLogo.png"),
            Path.Combine(_env.ContentRootPath, "EsyncLogo.png")
        };
        foreach (var lp in logoPaths)
        {
            if (File.Exists(lp)) { logoBytes = await File.ReadAllBytesAsync(lp); break; }
        }

        // Leave balance helpers
        var ml = leaveBalances.FirstOrDefault(lb =>
            lb.LeaveType?.Code == "MC" || (lb.LeaveType?.Name?.Contains("Medical") ?? false));
        var al = leaveBalances.FirstOrDefault(lb =>
            lb.LeaveType?.Code == "AL" || (lb.LeaveType?.Name?.Contains("Annual") ?? false));
        var otherLeaves = leaveBalances
            .Where(lb => lb.LeaveType?.Code != "MC" && lb.LeaveType?.Code != "AL"
                      && !(lb.LeaveType?.Name?.Contains("Medical") ?? false)
                      && !(lb.LeaveType?.Name?.Contains("Annual")  ?? false))
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(30);
                page.MarginVertical(25);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Content().Column(main =>
                {
                    // ════════════════════════════════════════════════════════
                    // HEADER — Logo + Company Name
                    // ════════════════════════════════════════════════════════
                    main.Item().PaddingBottom(6).Row(row =>
                    {
                        // Logo
                        row.ConstantItem(90).AlignMiddle().Element(img =>
                        {
                            if (logoBytes != null)
                                img.Image(logoBytes).FitArea();
                            else
                                img.Border(1).BorderColor(Colors.Grey.Medium)
                                   .Background(Colors.Grey.Lighten3)
                                   .AlignCenter().AlignMiddle()
                                   .Text("eSYNC").Bold().FontSize(10);
                        });

                        // Company name
                        row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(c =>
                        {
                            c.Item().Text(CompanyName)
                                .FontSize(18).Bold().FontColor(Colors.Red.Darken2);
                            c.Item().PaddingTop(3).Text(CompanyRegNo)
                                .FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    main.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingBottom(0);

                    // Pay slip title
                    main.Item().PaddingVertical(5).AlignCenter()
                        .Text($"PAY SLIP FOR {new DateTime(payroll.Year, payroll.Month, 1):MMMM, yyyy}".ToUpper())
                        .FontSize(11).Bold();

                    main.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingBottom(0);

                    // ════════════════════════════════════════════════════════
                    // EMPLOYEE DETAILS — 2 columns
                    // ════════════════════════════════════════════════════════
                    main.Item().PaddingVertical(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            void LR(string label, string value)
                            {
                                c.Item().PaddingBottom(3).Text(t =>
                                {
                                    t.Span(label).Bold().FontSize(9);
                                    t.Span(value).FontSize(9);
                                });
                            }
                            LR("NAME:       ", payroll.Employee.Name.ToUpper());
                            LR("NRIC NO:    ", payroll.Employee.IcPassport ?? "—");
                            LR("ACCOUNT No: ", payroll.Employee.AccountNumber ?? "—");
                            LR("DEPARTMENT: ", payroll.Employee.Department?.Name ?? "—");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            void RR(string label, string value)
                            {
                                c.Item().PaddingBottom(3).AlignRight().Text(t =>
                                {
                                    t.Span(label).Bold().FontSize(9);
                                    t.Span(value).FontSize(9);
                                });
                            }
                            RR("DATE: ",        payDate.ToString("dd/MM/yyyy"));
                            RR("Pay slip for: ", monthName);
                            RR("Bank: ",         payroll.Employee.Bank?.Name ?? "—");
                            RR("DESIGNATION: ", payroll.Employee.Designation ?? "—");
                        });
                    });

                    // ════════════════════════════════════════════════════════
                    // MAIN TABLE — Income | Deduction  with  Current + Y-T-D
                    // ════════════════════════════════════════════════════════
                    main.Item().Table(table =>
                    {
                        // 7 columns: desc | curr | ytd | gap | desc | curr | ytd
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3.2f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(1.8f);
                            c.ConstantColumn(5);
                            c.RelativeColumn(3.2f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(1.8f);
                        });

                        // ── Header ───────────────────────────────────────
                        void H(string t, bool center = true)
                        {
                            var c = table.Cell().Border(1).BorderColor(Colors.Black)
                                         .Background(Colors.Grey.Lighten2).Padding(4);
                            if (center) c.AlignCenter().Text(t).Bold().FontSize(9);
                            else        c.AlignRight().Text(t).Bold().FontSize(9);
                        }
                        H("INCOME"); H("Current", false); H("Y-T-D", false);
                        table.Cell(); // gap
                        H("DEDUCTION"); H("CURRENT", false); H("Y-T-D", false);

                        // ── Row helpers ───────────────────────────────────
                        string Fmt(decimal? v) => (v.HasValue && v.Value != 0)
                            ? v.Value.ToString("N2") : "";

                        void IR(string label, decimal? cur, decimal? ytd, bool bold = false)
                        {
                            var l = table.Cell().BorderLeft(1).BorderColor(Colors.Black)
                                         .PaddingHorizontal(4).PaddingVertical(3);
                            var cc = table.Cell().BorderLeft(1).BorderColor(Colors.Black)
                                         .PaddingHorizontal(4).PaddingVertical(3).AlignRight();
                            var y  = table.Cell().BorderLeft(1).BorderRight(1).BorderColor(Colors.Black)
                                         .PaddingHorizontal(4).PaddingVertical(3).AlignRight();
                            if (bold) { l.Text(label).Bold(); cc.Text(Fmt(cur)).Bold(); y.Text(Fmt(ytd)).Bold(); }
                            else      { l.Text(label);        cc.Text(Fmt(cur));        y.Text(Fmt(ytd)); }
                        }

                        void DR(string label, decimal? cur, decimal? ytd, bool bold = false)
                        {
                            table.Cell(); // gap
                            var l  = table.Cell().BorderLeft(1).BorderColor(Colors.Black)
                                          .PaddingHorizontal(4).PaddingVertical(3);
                            var cc = table.Cell().BorderLeft(1).BorderColor(Colors.Black)
                                          .PaddingHorizontal(4).PaddingVertical(3).AlignRight();
                            var y  = table.Cell().BorderLeft(1).BorderRight(1).BorderColor(Colors.Black)
                                          .PaddingHorizontal(4).PaddingVertical(3).AlignRight();
                            if (bold) { l.Text(label).Bold(); cc.Text(Fmt(cur)).Bold(); y.Text(Fmt(ytd)).Bold(); }
                            else      { l.Text(label);        cc.Text(Fmt(cur));        y.Text(Fmt(ytd)); }
                        }

                        void ER() { IR("", null, null); } // empty income row

                        // Employer EPF (13%)
                        var empEpf   = Math.Round(payroll.BasicSalary * 0.13m, 2);
                        var empSocso = Math.Round(payroll.BasicSalary * 0.0175m, 2);

                        // ── Data rows ─────────────────────────────────────
                        IR("BASIC PAY",            payroll.BasicSalary, ytdBasic + payroll.BasicSalary);
                        DR("EPF",                  payroll.EpfAmount,   ytdEpf   + payroll.EpfAmount);

                        IR("Bonus",                bonusTotal > 0 ? bonusTotal : null, bonusTotal > 0 ? bonusTotal : null);
                        DR("SOCSO",                payroll.SocsoAmount, ytdSocso + payroll.SocsoAmount);

                        IR("MA / HRA / DA",        payroll.Allowances > 0 ? payroll.Allowances : null,
                                                   payroll.Allowances > 0 ? ytdAllow + payroll.Allowances : null);
                        DR("PCB",                  payroll.TaxAmount,  ytdTax + payroll.TaxAmount);

                        ER();
                        DR("Other Deductions",     otherDedTotal > 0 ? otherDedTotal : null,
                                                   otherDedTotal > 0 ? ytdDed + otherDedTotal : null);
                        ER();
                        DR("",                     null, null);
                        ER();
                        DR("Employer EPF",         empEpf,   null);
                        ER();
                        DR("Employer SOCSO",       empSocso, null);
                        ER();
                        DR("",                     null, null);
                        ER();
                        DR("Gross Income",         payroll.GrossIncome, ytdGross + payroll.GrossIncome, true);
                        ER();
                        DR("Net Income",           payroll.NetSalary,   ytdNet   + payroll.NetSalary,   true);

                        // ── Gross Total / End of Month Pay row ────────────
                        var grossTotal    = payroll.BasicSalary + payroll.Allowances + bonusTotal;
                        var ytdGrossTotal = ytdBasic + ytdAllow + grossTotal;

                        table.Cell().ColumnSpan(3)
                            .Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).Padding(5)
                            .Row(r =>
                            {
                                r.RelativeItem().Text("Gross Total").Bold().FontSize(9);
                                r.ConstantItem(58).AlignRight().Text(grossTotal.ToString("N2")).Bold().FontSize(9);
                                r.ConstantItem(58).AlignRight().Text(ytdGrossTotal.ToString("N2")).Bold().FontSize(9);
                            });

                        table.Cell(); // gap

                        table.Cell().ColumnSpan(3)
                            .Border(1).BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3).Padding(5)
                            .Row(r =>
                            {
                                r.RelativeItem().Text("End of Month Pay").Bold().FontSize(9);
                                r.ConstantItem(75).AlignRight()
                                    .Text($"{payroll.NetSalary:N2}").Bold().FontSize(9);
                            });
                    });

                    // ════════════════════════════════════════════════════════
                    // LEAVE SUMMARY
                    // ════════════════════════════════════════════════════════
                    main.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        var mlUsed = ml?.UsedDays  ?? 0;
                        var mlBal  = ml?.BalanceDays ?? 0;
                        var alUsed = al?.UsedDays  ?? 0;
                        var alBal  = al?.BalanceDays ?? 0;
                        var otherUsed = otherLeaves.Sum(lb => lb.UsedDays);
                        var otherBal  = otherLeaves.Sum(lb => lb.BalanceDays);

                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text($"Medical Leave = {mlUsed},  Balance = {mlBal},  YTD = {mlUsed}").FontSize(8);
                        table.Cell().Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text($"Annual Leave = {alUsed},  Balance = {alBal},  YTD = {alUsed}").FontSize(8);
                        table.Cell().ColumnSpan(2).Border(1).BorderColor(Colors.Black).Padding(4)
                            .Text($"Other Leave = {otherUsed},  Balance = {otherBal},  YTD = {otherUsed}").FontSize(8);
                    });

                    // ════════════════════════════════════════════════════════
                    // FOOTER
                    // ════════════════════════════════════════════════════════
                    main.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor(Colors.Black).Width(120).PaddingBottom(2).Text("");
                            c.Item().PaddingTop(3).Text("Employee Signature").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().BorderBottom(1).BorderColor(Colors.Black).Width(120).PaddingBottom(2).Text("");
                            c.Item().PaddingTop(3).AlignRight().Text("Authorised Signature").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    main.Item().PaddingTop(10).AlignCenter()
                        .Text("This is a computer-generated payslip. No signature is required.")
                        .FontSize(7).Italic().FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}
