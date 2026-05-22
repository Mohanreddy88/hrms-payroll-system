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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── BankMaster ────────────────────────────────────────
        modelBuilder.Entity<BankMaster>(e =>
        {
            e.ToTable("BankMaster");
        });

        // ── User ──────────────────────────────────────────────
        // DB has: Id, Username, Email, PasswordHash, Role, IsActive, CreatedAt
        // Model has extra: PasswordResetToken, PasswordResetTokenExpiry — ignore them
        modelBuilder.Entity<User>(e =>
        {
            e.Ignore(x => x.PasswordResetToken);
            e.Ignore(x => x.PasswordResetTokenExpiry);

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
        // DB has no: ProfilePicture, IcPassport, TaxNumber — ignore them
        modelBuilder.Entity<Employee>(e =>
        {
            e.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.Property(x => x.Salary).HasPrecision(18, 2);

            // Column name mappings
            e.Property(x => x.Name).HasColumnName("FirstName");
            e.Property(x => x.Phone).HasColumnName("PhoneNumber");
            e.Property(x => x.JoinDate).HasColumnName("JoiningDate");
            e.Property(x => x.AccountNumber).HasColumnName("BankAccountNumber");

            // Properties not in live DB — ignore
            e.Ignore(x => x.ProfilePicture);
            e.Ignore(x => x.IcPassport);
            e.Ignore(x => x.TaxNumber);

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
        // DB columns: ProcessedDate, ApprovedDate (NOT ProcessedOn, ApprovedOn)
        // DB has no: GeneratedOn, PaidLeaveDays, UnpaidLeaveDays — ignore them
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

            // Column name mappings
            e.Property(x => x.ApprovedOn).HasColumnName("ApprovedDate");
            e.Property(x => x.ProcessedOn).HasColumnName("ProcessedDate");

            // Properties not in live DB — ignore
            e.Ignore(x => x.GeneratedOn);
            e.Ignore(x => x.PaidLeaveDays);
            e.Ignore(x => x.UnpaidLeaveDays);

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
        // DB has: Id, PayrollId, AttendancePeriodId, HoursWorked — no CreatedAt
        modelBuilder.Entity<PayrollAttendancePeriod>(e =>
        {
            e.Property(x => x.HoursWorked).HasPrecision(10, 2);
            e.Ignore(x => x.CreatedAt);

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
        // DB has: Id, PayrollId, LeaveRequestId, LeaveDays, DeductionAmount — no IsPaid, no CreatedAt
        modelBuilder.Entity<PayrollLeaveRequest>(e =>
        {
            e.Property(x => x.LeaveDays).HasPrecision(5, 2);
            e.Property(x => x.DeductionAmount).HasPrecision(10, 2);
            e.Ignore(x => x.IsPaid);
            e.Ignore(x => x.CreatedAt);

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
        // DB has: Id, PayrollId, Type, Description, Amount, CreatedBy, CreatedAt
        // Model uses AdjustmentType — map to "Type"
        modelBuilder.Entity<PayrollAdjustment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.AdjustmentType).HasColumnName("Type");

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
        // DB has: Id, EmployeeId, StartDate, EndDate, TotalDays, WorkingDays,
        //         PresentDays, AbsentDays, LeaveDays, HolidayDays, TotalHours, CreatedAt
        // Model has extra: Status, SubmittedAt, ApprovedAt, RejectedAt, ApprovedBy,
        //                  RejectedBy, RejectionReason, Remarks — ignore them all
        modelBuilder.Entity<AttendancePeriod>(e =>
        {
            e.Ignore(x => x.Status);
            e.Ignore(x => x.SubmittedAt);
            e.Ignore(x => x.ApprovedAt);
            e.Ignore(x => x.RejectedAt);
            e.Ignore(x => x.ApprovedBy);
            e.Ignore(x => x.RejectedBy);
            e.Ignore(x => x.RejectionReason);
            e.Ignore(x => x.Remarks);

            e.HasOne(x => x.Employee)
             .WithMany()
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate }).IsUnique();
        });

        // ── AttendancePeriodDay ───────────────────────────────
        // DB has: Id, AttendancePeriodId, Date, DayType, Hours, Remarks
        // Model has extra: Note, IsPublicHoliday, IsWeekend — ignore them
        modelBuilder.Entity<AttendancePeriodDay>(e =>
        {
            e.Property(x => x.Hours).HasPrecision(5, 2);
            e.Ignore(x => x.Note);
            e.Ignore(x => x.IsPublicHoliday);
            e.Ignore(x => x.IsWeekend);

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
        // DB has: Id, UserId, Title, Message, Type, IsRead, CreatedAt, ReadAt
        // Model has: Link (with [Column("link")]) — not in DB, ignore it
        modelBuilder.Entity<Notification>(e =>
        {
            e.Ignore(x => x.Link);
        });

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
        // DB has: Id, EmployeeId, LeaveTypeId, StartDate, EndDate, TotalDays,
        //         Reason, Status, ApprovedBy, ApprovedDate, ApproverRemarks, CreatedAt
        // Model has: RequestedOn, ApprovedOn, ApprovalRemarks, CancelledOn, CancellationReason
        // Map: ApprovedOn→ApprovedDate, ApprovalRemarks→ApproverRemarks
        // Ignore: RequestedOn, CancelledOn, CancellationReason
        modelBuilder.Entity<LeaveRequest>(e =>
        {
            e.Property(x => x.TotalDays).HasPrecision(5, 2);
            e.Property(x => x.ApprovedOn).HasColumnName("ApprovedDate");
            e.Property(x => x.ApprovalRemarks).HasColumnName("ApproverRemarks");
            e.Ignore(x => x.RequestedOn);
            e.Ignore(x => x.CancelledOn);
            e.Ignore(x => x.CancellationReason);

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
