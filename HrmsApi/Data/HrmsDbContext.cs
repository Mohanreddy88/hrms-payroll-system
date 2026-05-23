using HrmsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Data;

public class HrmsDbContext : DbContext
{
    public HrmsDbContext(DbContextOptions<HrmsDbContext> options) : base(options) { }

    // ── DbSets ───────────────────────────────────────��────────
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
    public DbSet<AttendancePeriod>      AttendancePeriods     => Set<AttendancePeriod>();
    public DbSet<AttendancePeriodDay>   AttendancePeriodDays  => Set<AttendancePeriodDay>();
    public DbSet<PayrollAttendancePeriod> PayrollAttendancePeriods => Set<PayrollAttendancePeriod>();
    public DbSet<PayrollLeaveRequest>   PayrollLeaveRequests  => Set<PayrollLeaveRequest>();
    public DbSet<PayrollAdjustment>     PayrollAdjustments    => Set<PayrollAdjustment>();
    public DbSet<Notification>          Notifications         => Set<Notification>();
    public DbSet<AuditLog>              AuditLogs             => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── BankMaster ────────────────────────────────────────
        modelBuilder.Entity<BankMaster>(e =>
        {
            e.ToTable("BankMaster");
        });

        // ── User ──────────────────────────────────────────────
        // DB has: Id, Username, Email, PasswordHash, Role, IsActive, CreatedAt,
        //         PasswordResetToken, PasswordResetTokenExpiry — all mapped
        modelBuilder.Entity<User>(e =>
        {

            e.Property(x => x.Email).HasMaxLength(255);
            e.HasIndex(x => x.Email)
             .IsUnique()
             .HasFilter("\"Email\" IS NOT NULL AND \"Email\" <> ''");
        });

        // ── Department ────────────────────────────────────────
        // DB has: Id, Name, Description, IsActive, CreatedAt — all match
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("Departments");
        });

        // ── Employee ──────────────────────────────────────────
        // DB columns: FirstName, LastName, PhoneNumber, JoiningDate, BankAccountNumber
        // Model uses: Name, Phone, JoinDate, AccountNumber — map them
        // DB has all model columns directly — Name, Phone, JoinDate, AccountNumber match exactly
        // IcPassport, TaxNumber, ProfilePicture also exist in DB — all mapped, no ignores
        modelBuilder.Entity<Employee>(e =>
        {
            e.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.Property(x => x.Salary).HasPrecision(18, 2);

            e.HasOne(x => x.Department)
             .WithMany(d => d.Employees)
             .HasForeignKey(x => x.DepartmentId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Bank)
             .WithMany(b => b.Employees)
             .HasForeignKey(x => x.BankId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Payroll ───────────────────────────────────────────
        // DB has all model columns directly — no renames, no ignores needed
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
            e.Property(x => x.AttendanceHours).HasPrecision(10, 2);
            e.Property(x => x.ExpectedHours).HasPrecision(10, 2);

            e.HasOne(x => x.Approver)
             .WithMany()
             .HasForeignKey(x => x.ApprovedBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Processor)
             .WithMany()
             .HasForeignKey(x => x.ProcessedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payroll>()
            .HasOne(p => p.Employee)
            .WithMany(e => e.Payrolls)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // One payroll record per employee per month/year
        modelBuilder.Entity<Payroll>()
            .HasIndex(p => new { p.EmployeeId, p.Month, p.Year })
            .IsUnique();

        // ── PayrollAttendancePeriod ───────────────────────────
        // DB has: Id, PayrollId, AttendancePeriodId, HoursWorked, CreatedAt — all mapped
        modelBuilder.Entity<PayrollAttendancePeriod>(e =>
        {
            e.Property(x => x.HoursWorked).HasPrecision(10, 2);

            e.HasOne(x => x.Payroll)
             .WithMany(p => p.AttendancePeriods)
             .HasForeignKey(x => x.PayrollId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.AttendancePeriod)
             .WithMany()
             .HasForeignKey(x => x.AttendancePeriodId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.PayrollId, x.AttendancePeriodId }).IsUnique();
        });

        // ── PayrollLeaveRequest ───────────────────────────────
        // DB has: Id, PayrollId, LeaveRequestId, LeaveDays, IsPaid, DeductionAmount, CreatedAt — all mapped
        modelBuilder.Entity<PayrollLeaveRequest>(e =>
        {
            e.Property(x => x.LeaveDays).HasPrecision(5, 2);
            e.Property(x => x.DeductionAmount).HasPrecision(10, 2);

            e.HasOne(x => x.Payroll)
             .WithMany(p => p.LeaveRequests)
             .HasForeignKey(x => x.PayrollId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.LeaveRequest)
             .WithMany()
             .HasForeignKey(x => x.LeaveRequestId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.PayrollId, x.LeaveRequestId }).IsUnique();
        });

        // ── PayrollAdjustment ─────────────────────────────────
        // DB has: Id, PayrollId, AdjustmentType, Description, Amount, CreatedBy, CreatedAt — all match
        modelBuilder.Entity<PayrollAdjustment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(10, 2);

            e.HasOne(x => x.Payroll)
             .WithMany(p => p.Adjustments)
             .HasForeignKey(x => x.PayrollId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Creator)
             .WithMany()
             .HasForeignKey(x => x.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Attendance ────────────────────────────────────────
        // DB has: Id, EmployeeId, Date, TimeIn, TimeOut, WorkHours, Status, Remarks, CreatedAt
        modelBuilder.Entity<Attendance>(e =>
        {
            e.Property(x => x.WorkHours).HasPrecision(10, 2);
            e.Property(x => x.CheckIn).HasColumnName("TimeIn");
            e.Property(x => x.CheckOut).HasColumnName("TimeOut");

            e.HasOne(x => x.Employee)
             .WithMany(emp => emp.Attendances)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // One attendance record per employee per calendar day
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

        // ── AttendancePeriod ──────────────────────────────────
        // DB has: Id, EmployeeId, StartDate, EndDate, Status, CreatedAt,
        //         SubmittedAt, ApprovedAt, RejectedAt, ApprovedBy,
        //         RejectedBy, RejectionReason, Remarks — all mapped
        modelBuilder.Entity<AttendancePeriod>(e =>
        {
            e.HasOne(x => x.Employee)
             .WithMany()
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate }).IsUnique();
        });

        // ── AttendancePeriodDay ───────────────────────────────
        // DB has: Id, AttendancePeriodId, Date, Hours, Note,
        //         Remarks, IsPublicHoliday, IsWeekend — all mapped
        modelBuilder.Entity<AttendancePeriodDay>(e =>
        {
            e.Property(x => x.Hours).HasPrecision(5, 2);

            e.HasOne(x => x.AttendancePeriod)
             .WithMany(ap => ap.Days)
             .HasForeignKey(x => x.AttendancePeriodId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.AttendancePeriodId, x.Date }).IsUnique();
        });

        // ── PublicHoliday ─────────────────────────────────────
        // DB has: Id, Name, Date, Year, IsNational, State, Description, CreatedAt — all match
        // (no ignores needed)

        // ── Notification ──────────────────────────────────────
        // DB has: Id, UserId, Title, Message, Type, Link, IsRead, CreatedAt, ReadAt — all mapped
        // No ignores needed

        // ── Timesheet ─────────────────────────────────────────
        modelBuilder.Entity<Timesheet>(e =>
        {
            e.Property(x => x.TotalWorkHours).HasPrecision(10, 2);

            e.HasOne(x => x.Employee)
             .WithMany()
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.EmployeeId, x.Year, x.Month }).IsUnique();
        });

        // ── LeaveType ─────────────────────────────────────────
        // DB has: Id, Name, Code, Description, DefaultDaysPerYear, IsActive,
        //         RequiresApproval, IsPaid, CreatedAt — all match

        // ── EmployeeLeaveBalance ──────────────────────────────
        // DB has: Id, EmployeeId, LeaveTypeId, Year, TotalDays, UsedDays,
        //         BalanceDays, CarryForwardDays, CreatedAt, UpdatedAt — all match
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

            e.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
        });

        // ── LeaveRequest ──────────────────────────────────────
        // DB has all model columns directly — RequestedOn, ApprovedOn, ApprovalRemarks,
        // CancelledOn, CancellationReason all match — no renames, no ignores
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
