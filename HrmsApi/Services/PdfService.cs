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
            .FirstOrDefaultAsync(p => p.Id == payrollId)
            ?? throw new KeyNotFoundException($"Payroll record {payrollId} not found");

        var monthName = new DateTime(payroll.Year, payroll.Month, 1).ToString("MMMM yyyy");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                // Header
                page.Header().Element(ComposeHeader);

                // Content
                page.Content().Element(content => ComposeContent(content, payroll, monthName));

                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated on: ").FontSize(9).FontColor(Colors.Grey.Darken2);
                    text.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(9).FontColor(Colors.Grey.Darken2);
                    text.Span(" | This is a computer-generated document. No signature required.").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("HRMS Payroll System").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("Employee Payslip").FontSize(14).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).AlignRight().Column(column =>
                {
                    column.Item().Text(monthName).FontSize(12).Bold();
                    column.Item().Text($"#{payroll.Id:D6}").FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });
        }

        void ComposeContent(IContainer container, Payroll payroll, string monthName)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Employee Information
                column.Item().Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Employee Information").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().PaddingTop(5).Text(text =>
                        {
                            text.Span("Name: ").Bold();
                            text.Span(payroll.Employee.Name);
                        });
                        col.Item().Text(text =>
                        {
                            text.Span("Employee ID: ").Bold();
                            text.Span($"EMP-{payroll.Employee.Id:D6}");
                        });
                        col.Item().Text(text =>
                        {
                            text.Span("Department: ").Bold();
                            text.Span(payroll.Employee.Department?.Name ?? "N/A");
                        });
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text(text =>
                        {
                            text.Span("Designation: ").Bold();
                            text.Span(payroll.Employee.Designation);
                        });
                        col.Item().AlignRight().Text(text =>
                        {
                            text.Span("Pay Period: ").Bold();
                            text.Span(monthName);
                        });
                        col.Item().AlignRight().Text(text =>
                        {
                            text.Span("Generated: ").Bold();
                            text.Span(payroll.GeneratedOn.ToString("dd MMM yyyy"));
                        });
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Earnings Section
                column.Item().PaddingTop(10).Text("Earnings").FontSize(13).Bold().FontColor(Colors.Green.Darken1);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Description").Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Amount (RM)").Bold();

                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Basic Salary");
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{payroll.BasicSalary:N2}");

                    table.Cell().Padding(5).Text("Allowances");
                    table.Cell().Padding(5).AlignRight().Text($"{payroll.Allowances:N2}");

                    table.Cell().Background(Colors.Green.Lighten4).Padding(5).Text("Gross Income").Bold();
                    table.Cell().Background(Colors.Green.Lighten4).Padding(5).AlignRight().Text($"{payroll.GrossIncome:N2}").Bold();
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Deductions Section
                column.Item().PaddingTop(10).Text("Deductions").FontSize(13).Bold().FontColor(Colors.Red.Darken1);
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Description").Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Amount (RM)").Bold();

                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("EPF (Employee Contribution)");
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{payroll.EpfAmount:N2}");

                    table.Cell().Padding(5).Text("SOCSO");
                    table.Cell().Padding(5).AlignRight().Text($"{payroll.SocsoAmount:N2}");

                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tax (PCB)");
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{payroll.TaxAmount:N2}");

                    table.Cell().Padding(5).Text("Other Deductions");
                    table.Cell().Padding(5).AlignRight().Text($"{payroll.Deductions:N2}");

                    var totalDeductions = payroll.EpfAmount + payroll.SocsoAmount + payroll.TaxAmount + payroll.Deductions;
                    table.Cell().Background(Colors.Red.Lighten4).Padding(5).Text("Total Deductions").Bold();
                    table.Cell().Background(Colors.Red.Lighten4).Padding(5).AlignRight().Text($"{totalDeductions:N2}").Bold();
                });

                column.Item().PaddingVertical(15).LineHorizontal(2).LineColor(Colors.Grey.Darken1);

                // Net Salary
                column.Item().Background(Colors.Blue.Darken2).Padding(15).Row(row =>
                {
                    row.RelativeItem().Text("NET SALARY").FontSize(16).Bold().FontColor(Colors.White);
                    row.RelativeItem().AlignRight().Text($"RM {payroll.NetSalary:N2}").FontSize(18).Bold().FontColor(Colors.White);
                });
            });
        }
    }
}
