using HrmsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Data;

public class HrmsDbContext : DbContext
{
    public HrmsDbContext(DbContextOptions<HrmsDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────
    public DbSet<User>                  Users                 => Set<User>();
    public DbSet<Department>            Departments           => Set<Department>();
    public DbSet<BankMaster>            BankMasters           => Set<BankMaster>();
    public DbSet<Employee>              Employees             => Set<Employee>();
    public DbSet<Attendance>            Attendances           => Set<Attendance>();
    public DbSet<Payroll>               Payrolls              => Set<Payroll>();
    public DbSet<PublicHoliday>         PublicHolidays        => Set<PublicHoliday>();
    public DbSet<Timesheet>             Timesheets            => Set<Timesheet>();
    public DbSet<LeaveType>             LeaveTypes            => Set<LeaveType>();
    public DbSet<EmployeeLeaveBalance>  EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();
    public DbSet<LeaveRequest>          LeaveRequests         => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── BankMaster ────────────────────────────────────────
        modelBuilder.Entity<BankMaster>(e =>
        {
            e.ToTable("BankMaster");
        });

        // ── Department ────────────────────────────────────────
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("Departments");
        });

        // ── Employee ──────────────────────────────────────────
        modelBuilder.Entity<Employee>(e =>
        {
            e.Property(x => x.Salary).HasPrecision(18, 2);

            // DepartmentId is a nullable FK — employee may not have a department assigned yet
            e.HasOne(x => x.Department)
             .WithMany(d => d.Employees)
             .HasForeignKey(x => x.DepartmentId)
             .OnDelete(DeleteBehavior.SetNull);

            // BankId is a nullable FK — employee may not have a bank assigned yet
            e.HasOne(x => x.Bank)
             .WithMany(b => b.Employees)
             .HasForeignKey(x => x.BankId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Payroll ───────────────────────────────────────────
        modelBuilder.Entity<Payroll>(e =>
        {
            e.Property(x => x.BasicSalary).HasPrecision(18, 2);
            e.Property(x => x.Allowances).HasPrecision(18, 2);
            e.Property(x => x.Deductions).HasPrecision(18, 2);
            e.Property(x => x.EpfAmount).HasPrecision(18, 2);
            e.Property(x => x.SocsoAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.GrossIncome).HasPrecision(18, 2);
            e.Property(x => x.NetSalary).HasPrecision(18, 2);
        });

        // ── Attendance ────────────────────────────────────────
        modelBuilder.Entity<Attendance>(e =>
        {
            e.Property(x => x.WorkHours).HasPrecision(10, 2);
            
            e.HasOne(x => x.Employee)
             .WithMany(emp => emp.Attendances)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payroll>()
            .HasOne(p => p.Employee)
            .WithMany(e => e.Payrolls)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Unique constraints ────────────────────────────────

        // One attendance record per employee per calendar day
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

        // One payroll record per employee per month/year
        modelBuilder.Entity<Payroll>()
            .HasIndex(p => new { p.EmployeeId, p.Month, p.Year })
            .IsUnique();

        // ── Timesheet ─────────────────────────────────────────
        modelBuilder.Entity<Timesheet>(e =>
        {
            e.Property(x => x.TotalWorkHours).HasPrecision(10, 2);
            
            e.HasOne(x => x.Employee)
             .WithMany()
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);

            // One timesheet per employee per month/year
            e.HasIndex(x => new { x.EmployeeId, x.Year, x.Month }).IsUnique();
        });

        // ── Leave Management ──────────────────────────────────
        modelBuilder.Entity<EmployeeLeaveBalance>(e =>
        {
            e.Property(x => x.TotalDays).HasPrecision(5, 2);
            e.Property(x => x.UsedDays).HasPrecision(5, 2);
            e.Property(x => x.BalanceDays).HasPrecision(5, 2);
            e.Property(x => x.CarryForwardDays).HasPrecision(5, 2);

            e.HasOne(x => x.Employee)
             .WithMany()
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.LeaveType)
             .WithMany(lt => lt.LeaveBalances)
             .HasForeignKey(x => x.LeaveTypeId)
             .OnDelete(DeleteBehavior.Cascade);

            // One balance record per employee per leave type per year
            e.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
        });

        modelBuilder.Entity<LeaveRequest>(e =>
        {
            e.Property(x => x.TotalDays).HasPrecision(5, 2);

            e.HasOne(x => x.Employee)
             .WithMany()
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.LeaveType)
             .WithMany(lt => lt.LeaveRequests)
             .HasForeignKey(x => x.LeaveTypeId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
