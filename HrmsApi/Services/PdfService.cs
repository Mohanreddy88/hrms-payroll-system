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
    Task<byte[]> GenerateEaFormPdfAsync(int employeeId, int year);
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
                    main.Item().PaddingTop(14).BorderTop(1).BorderColor(Colors.Grey.Lighten1)
                        .PaddingTop(6).Column(c =>
                    {
                        c.Item().AlignCenter()
                            .Text("*** This is a computer-generated payslip. No signature is required. ***")
                            .FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                        c.Item().PaddingTop(3).AlignCenter()
                            .Text($"Generated on: {DateTime.Now:dd MMM yyyy  HH:mm:ss}  |  HRMS Payroll System  |  {CompanyName}")
                            .FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CP8A — Borang EA (Malaysia Annual Income Statement)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<byte[]> GenerateEaFormPdfAsync(int employeeId, int year)
    {
        var employee = await _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var payrolls = await _db.Payrolls
            .Where(p => p.EmployeeId == employeeId
                     && p.Year       == year
                     && (p.Status    == "Approved" || p.Status == "Processed"))
            .OrderBy(p => p.Month)
            .ToListAsync();

        var totalGross      = payrolls.Sum(p => p.GrossIncome);
        var totalBasic      = payrolls.Sum(p => p.BasicSalary);
        var totalAllowances = payrolls.Sum(p => p.Allowances);
        var totalEpf        = payrolls.Sum(p => p.EpfAmount);
        var totalSocso      = payrolls.Sum(p => p.SocsoAmount);
        var totalTax        = payrolls.Sum(p => p.TaxAmount);
        var totalDeductions = payrolls.Sum(p => p.Deductions);

        const string companyAddress = "CT-09-12 CORPORATE TOWER SUBANG SQUARE JALAN SS15/4G\n47500 SUBANG JAYA SELANGOR";
        const string companyPhone   = "0356326915";
        const string companyRegNo   = "E 9145397208";
        const string officerName    = "HR DEPARTMENT";

        string Amt(decimal v) => v == 0 ? "0.00" : v.ToString("N2");
        string AZ(decimal v)  => v == 0 ? "" : v.ToString("N2");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginTop(20);
                page.MarginBottom(20);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── CP8A header bar ───────────────────────────────────
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("(C.P.8A - Pin. 2025 )").FontSize(7);
                            c.Item().PaddingTop(3).Text(t =>
                            {
                                t.Span("Serial No.").FontSize(7);
                            });
                            c.Item().PaddingTop(3).Text(t =>
                            {
                                t.Span("Employer No.  ").FontSize(7);
                                t.Span(companyRegNo).FontSize(7).Bold();
                            });
                        });

                        r.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("MALAYSIA").Bold().FontSize(10);
                            c.Item().AlignCenter().Text("INCOME TAX").Bold().FontSize(10);
                            c.Item().AlignCenter().Text("SALARY STATEMENT FROM EMPLOYMENT").Bold().FontSize(9);
                            c.Item().AlignCenter().Text($"FOR THE YEAR ENDING 31 DECEMBER {year}").Bold().FontSize(9);
                        });

                        r.ConstantItem(100).Column(c =>
                        {
                            c.Item().Border(1).BorderColor(Colors.Black).Padding(4)
                                .AlignCenter().Text("Private Employee Salary Statement")
                                .FontSize(7).Bold().BackgroundColor(Colors.Black).FontColor(Colors.White);
                            c.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("No. Employee Income Tax (TIN)  ").FontSize(7);
                            });
                            c.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("IG ").FontSize(7);
                                t.Span(string.IsNullOrEmpty(employee.TaxNumber) ? "—" : employee.TaxNumber).FontSize(7).Bold();
                            });
                            c.Item().PaddingTop(2).Text("LHDNM Negeri SELANGOR").FontSize(7);
                        });

                        r.ConstantItem(30).AlignMiddle().AlignRight()
                            .Text("EA").FontSize(30).Bold();
                    });

                    col.Item().PaddingTop(4)
                        .Background(Colors.Black).Padding(5)
                        .AlignCenter()
                        .Text("THIS FORM EA NEEDS TO BE PREPARED TO BE SUBMITTED TO THE EMPLOYEE FOR INCOME TAX PURPOSES")
                        .FontSize(8).Bold().FontColor(Colors.White);

                    // ── Section A ─────────────────────────────────────────
                    col.Item().PaddingTop(6).Border(1).BorderColor(Colors.Black).Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten2).Padding(4).Row(r =>
                        {
                            r.ConstantItem(16).AlignMiddle()
                                .Border(1).BorderColor(Colors.Black).Background(Colors.Black)
                                .AlignCenter().AlignMiddle().Text("A").Bold().FontColor(Colors.White);
                            r.RelativeItem().PaddingLeft(6).AlignMiddle()
                                .Text("EMPLOYEE DETAILS").Bold().FontSize(9);
                        });

                        section.Item().Padding(6).Table(grid =>
                        {
                            grid.ColumnsDefinition(c => { for (int i = 0; i < 12; i++) c.RelativeColumn(); });

                            grid.Cell().ColumnSpan(1).Text("1.").Bold();
                            grid.Cell().ColumnSpan(11).Text(t => { t.Span("Full Name of Employee / Retiree (Mr./Ms./Ms.)  ").Bold(); t.Span(employee.Name.ToUpper()).Bold(); });

                            grid.Cell().ColumnSpan(1).Text("2.").Bold();
                            grid.Cell().ColumnSpan(5).Text(t => { t.Span("Position  ").Bold(); t.Span(employee.Designation ?? "—"); });
                            grid.Cell().ColumnSpan(1).Text("3.").Bold();
                            grid.Cell().ColumnSpan(5).Text(t => { t.Span("No. Staff / No. Salary  ").Bold(); t.Span(employee.EmployeeCode); });

                            grid.Cell().ColumnSpan(1).Text("4.").Bold();
                            grid.Cell().ColumnSpan(5).Text(t => { t.Span("New Identification No.  ").Bold(); t.Span(employee.IcPassport ?? "—"); });
                            grid.Cell().ColumnSpan(1).Text("5.").Bold();
                            grid.Cell().ColumnSpan(5).Text(t => { t.Span("Passport No.  ").Bold(); t.Span(employee.IcPassport ?? "—"); });

                            grid.Cell().ColumnSpan(1).Text("6.").Bold();
                            grid.Cell().ColumnSpan(5).Text(t => { t.Span("KWSP No.  ").Bold(); t.Span("0"); });
                            grid.Cell().ColumnSpan(1).Text("7.").Bold();
                            grid.Cell().ColumnSpan(5).Text(t => { t.Span("PERKESO No.  ").Bold(); t.Span("0"); });

                            grid.Cell().ColumnSpan(1).Text("8.").Bold();
                            grid.Cell().ColumnSpan(5).Text(t => { t.Span("The number of children eligible for tax relief  ").Bold(); t.Span("0"); });
                            grid.Cell().ColumnSpan(1).Text("9.").Bold();
                            grid.Cell().ColumnSpan(5).Text(t => { t.Span("If working for less than a year, state:").Bold(); });

                            grid.Cell().ColumnSpan(6).Text("");
                            grid.Cell().ColumnSpan(1).Text("");
                            grid.Cell().ColumnSpan(5).Column(c =>
                            {
                                c.Item().Text(t => { t.Span("(a) Date of employment  ").Bold(); t.Span(employee.JoinDate.Year == year ? employee.JoinDate.ToString("dd/MM/yyyy") : ""); });
                                c.Item().Text(t => { t.Span("(b) Date of termination of employment").Bold(); });
                            });
                        });
                    });

                    // ── Section B ─────────────────────────────────────────
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Black).Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten2).Padding(4).Row(r =>
                        {
                            r.ConstantItem(16).Border(1).BorderColor(Colors.Black).Background(Colors.Black)
                                .AlignCenter().AlignMiddle().Text("B").Bold().FontColor(Colors.White);
                            r.RelativeItem().PaddingLeft(6).AlignMiddle()
                                .Text("EMPLOYMENT INCOME, BENEFITS AND PLACE OF RESIDENCE").Bold().FontSize(9);
                        });

                        section.Item().PaddingHorizontal(6).PaddingTop(3)
                            .Text("(Not Including Allowances / Perquisites / Grants / Tax Exempt Benefits)").Italic();

                        section.Item().Padding(4).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(9); c.RelativeColumn(3); });

                            void BRow(string num, string label, decimal val, bool bold = false)
                            {
                                var left = t.Cell().PaddingVertical(1).PaddingLeft(4);
                                var right = t.Cell().PaddingVertical(1).AlignRight().PaddingRight(4);
                                if (bold) { left.Text(t2 => { t2.Span(num).Bold(); t2.Span("  " + label).Bold(); }); right.Text("RM  " + Amt(val)).Bold(); }
                                else      { left.Text(t2 => { t2.Span(num).Bold(); t2.Span("  " + label); }); right.Text(AZ(val) == "" ? "" : "RM  " + AZ(val)); }
                            }

                            BRow("1.", "(a) Gross pay, wages or holiday pay (including overtime pay)", totalBasic);
                            BRow("",  "(b) Fees (including directors' fees), commissions or bonuses", totalAllowances);
                            BRow("",  "(c) Gross tips, perquisites, acceptance of consolation or other allowances (Regarding payment:...)", 0);
                            BRow("",  "(d) Income tax paid by the employer on behalf of the employee", 0);
                            BRow("",  "(e) Employee Share Option Scheme (ESOS) Benefits", 0);
                            BRow("",  "(f)  Reward for the period from ............... to...............", 0);
                            BRow("2.", "Details of arrears and other payments for previous years in the current year", 0);
                            BRow("",  "Income Tax  (a) ............    (b) ............", 0);
                            BRow("3.", "Benefits in the form of Goods (Specify: ...)", 0);
                            BRow("4.", "Residential value (Address:...)", 0);
                            BRow("5.", "Refunds from unapproved Provident / Pension Funds", 0);
                            BRow("6.", "Compensation for job loss", 0);

                            t.Cell().ColumnSpan(1).PaddingVertical(2).PaddingLeft(4).Text("TOTAL").Bold();
                            t.Cell().PaddingVertical(2).PaddingRight(4).AlignRight()
                                .BorderTop(1).BorderColor(Colors.Black)
                                .Text($"RM  {Amt(totalGross)}").Bold();
                        });
                    });

                    // ── Section C ─────────────────────────────────────────
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Black).Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten2).Padding(4).Row(r =>
                        {
                            r.ConstantItem(16).Border(1).BorderColor(Colors.Black).Background(Colors.Black)
                                .AlignCenter().AlignMiddle().Text("C").Bold().FontColor(Colors.White);
                            r.RelativeItem().PaddingLeft(6).AlignMiddle()
                                .Text("PENSIONS AND OTHERS").Bold().FontSize(9);
                        });

                        section.Item().Padding(4).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(9); c.RelativeColumn(3); });
                            void CRow(string num, string label, decimal val)
                            {
                                t.Cell().PaddingVertical(1).PaddingLeft(4).Text(t2 => { t2.Span(num).Bold(); t2.Span("  " + label); });
                                t.Cell().PaddingVertical(1).AlignRight().PaddingRight(4).Text(AZ(val) == "" ? "" : "RM  " + AZ(val));
                            }
                            CRow("1.", "Pension", 0);
                            CRow("2.", "Annuity or other periodic payments", 0);
                            t.Cell().PaddingVertical(2).PaddingLeft(4).Text("TOTAL").Bold();
                            t.Cell().PaddingVertical(2).PaddingRight(4).AlignRight()
                                .BorderTop(1).BorderColor(Colors.Black)
                                .Text($"RM  {Amt(totalGross)}").Bold();
                        });
                    });

                    // ── Section D ─────────────────────────────────────────
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Black).Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten2).Padding(4).Row(r =>
                        {
                            r.ConstantItem(16).Border(1).BorderColor(Colors.Black).Background(Colors.Black)
                                .AlignCenter().AlignMiddle().Text("D").Bold().FontColor(Colors.White);
                            r.RelativeItem().PaddingLeft(6).AlignMiddle()
                                .Text("TOTAL DEDUCTION").Bold().FontSize(9);
                        });

                        section.Item().Padding(4).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(9); c.RelativeColumn(3); });
                            void DRow(string num, string label, decimal val)
                            {
                                t.Cell().PaddingVertical(1).PaddingLeft(4).Text(t2 => { t2.Span(num).Bold(); t2.Span("  " + label); });
                                t.Cell().PaddingVertical(1).AlignRight().PaddingRight(4).Text(AZ(val) == "" ? "" : "RM  " + AZ(val));
                            }
                            DRow("1.", "Monthly tax deduction (PCB) paid to IRBM", totalTax);
                            DRow("2.", "CP38 deduction instructions paid to IRBM", 0);
                            DRow("3.", "Zakat paid through salary deduction", 0);
                            DRow("4.", "Approved donations/ gifts/ contributions paid through payroll deductions", 0);
                            t.Cell().PaddingVertical(1).PaddingLeft(4).Text(t2 => { t2.Span("5.").Bold(); t2.Span("  Amount of deduction claimed by employees through TP1 Form related to:"); });
                            t.Cell().PaddingVertical(1).AlignRight().PaddingRight(4).Text("");
                            t.Cell().PaddingLeft(20).Text("a) Release  RM 0.00");
                            t.Cell().Text("");
                            t.Cell().PaddingLeft(20).Text("b) Zakat other than that paid through monthly salary deductions  RM 0.00");
                            t.Cell().Text("");
                            DRow("6.", "Amount of relief for eligible children", 0);
                        });
                    });

                    // ── Section E ─────────────────────────────────────────
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Black).Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten2).Padding(4).Row(r =>
                        {
                            r.ConstantItem(16).Border(1).BorderColor(Colors.Black).Background(Colors.Black)
                                .AlignCenter().AlignMiddle().Text("E").Bold().FontColor(Colors.White);
                            r.RelativeItem().PaddingLeft(6).AlignMiddle()
                                .Text("CONTRIBUTIONS PAID BY EMPLOYEE TO APPROVED SAVINGS / PENSION FUNDS AND SOCSO").Bold().FontSize(9);
                        });

                        section.Item().Padding(4).Column(c =>
                        {
                            c.Item().PaddingBottom(2).Text(t =>
                            {
                                t.Span("1.  Fund Name ").Bold();
                                t.Span("Employees' Provident Fund").Underline();
                            });
                            c.Item().PaddingLeft(12).PaddingBottom(2)
                                .Text("Contribution amount that must be paid (specify the employee's share only)");
                            c.Item().PaddingLeft(12).PaddingBottom(4).Text(t =>
                            {
                                t.Span("RM ").Bold();
                                t.Span(Amt(totalEpf)).Bold();
                            });
                            c.Item().PaddingBottom(2).Text(t =>
                            {
                                t.Span("2.  SOCSO: ").Bold();
                                t.Span("Contribution amount that must be paid (specify the employee's share only)");
                            });
                            c.Item().PaddingLeft(12).Text(t =>
                            {
                                t.Span("RM ").Bold();
                                t.Span(Amt(totalSocso)).Bold();
                            });
                        });
                    });

                    // ── Section F ─────────────────────────────────────────
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Black).Column(section =>
                    {
                        section.Item().Background(Colors.Grey.Lighten2).Padding(4).Row(r =>
                        {
                            r.ConstantItem(16).Border(1).BorderColor(Colors.Black).Background(Colors.Black)
                                .AlignCenter().AlignMiddle().Text("F").Bold().FontColor(Colors.White);
                            r.RelativeItem().PaddingLeft(6).AlignMiddle()
                                .Text("AMOUNT OF TAX-EXEMPT ALLOWANCES / PERQUISITES / GRANTS / BENEFITS").Bold().FontSize(9);
                        });

                        section.Item().Padding(8).Border(1).BorderColor(Colors.Black).Padding(6).Column(c =>
                        {
                            void FRow(string label, string val)
                            {
                                c.Item().PaddingBottom(3).Row(r =>
                                {
                                    r.ConstantItem(140).Text(label).Bold();
                                    r.RelativeItem().Text(val);
                                });
                            }
                            FRow("Name of officer", officerName);
                            FRow("Position", "HR");
                            FRow("Employer Name/ Address", $"{CompanyName}\n{companyAddress}");
                            FRow("Employer Phone No.", companyPhone);
                        });
                    });

                    col.Item().PaddingTop(6).Text($"Date: {DateTime.Now:yyyy-MM-dd}").FontSize(8);
                });
            });
        });

        return doc.GeneratePdf();
    }
}
