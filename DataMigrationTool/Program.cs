using System;
using System.Data;
using System.Data.SqlClient;
using Npgsql;

class DataMigration
{
    static string sqlServerConn = "Server=localhost\\SQLEXPRESS;Database=HrmsDb;Trusted_Connection=True;TrustServerCertificate=True;";
    static string postgresConn = "";

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run \"<PostgreSQL_Connection_String>\"");
            return;
        }

        postgresConn = args[0];

        try
        {
            Console.WriteLine("Starting data migration from SQL Server to PostgreSQL...\n");
            Console.WriteLine("Mapping old SQL Server schema to new EF Core schema...\n");

            // Migrate in order (respecting foreign keys)
            MigrateDepartments();
            MigrateUsers();
            MigrateEmployees();
            MigrateAttendances();
            MigrateTimesheets();
            MigratePayrolls();

            Console.WriteLine("\n✅ Migration completed successfully!");
            Console.WriteLine("All data has been migrated to Railway PostgreSQL.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static bool CheckTablesExist()
    {
        Console.WriteLine("Checking if PostgreSQL tables exist...");
        using var pgConn = new NpgsqlConnection(postgresConn);
        pgConn.Open();
        
        var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('Departments', 'Users', 'Employees')", pgConn);
        int tableCount = Convert.ToInt32(cmd.ExecuteScalar());
        
        if (tableCount == 3)
        {
            Console.WriteLine("✓ All required tables exist\n");
            return true;
        }
        else
        {
            Console.WriteLine($"⚠ Only {tableCount}/3 tables found");
            return false;
        }
    }

    static void MigrateDepartments()
    {
        Console.WriteLine("Migrating Departments...");
        using var sqlConn = new SqlConnection(sqlServerConn);
        using var pgConn = new NpgsqlConnection(postgresConn);
        sqlConn.Open();
        pgConn.Open();

        var cmd = new SqlCommand("SELECT Id, Name, Description, IsActive, CreatedAt FROM Departments", sqlConn);
        using var reader = cmd.ExecuteReader();

        int count = 0;
        while (reader.Read())
        {
            var insert = new NpgsqlCommand(@"
                INSERT INTO ""Departments"" (""Name"", ""Description"", ""IsActive"", ""CreatedAt"")
                VALUES (@name, @desc, @active, @created)", pgConn);

            insert.Parameters.AddWithValue("name", reader.GetString(1));
            insert.Parameters.AddWithValue("desc", reader.IsDBNull(2) ? "" : reader.GetString(2));
            insert.Parameters.AddWithValue("active", reader.GetBoolean(3));
            insert.Parameters.AddWithValue("created", reader.GetDateTime(4).ToUniversalTime());
            insert.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"  ✓ Migrated {count} departments");
    }

    static void MigrateUsers()
    {
        Console.WriteLine("Migrating Users...");
        using var sqlConn = new SqlConnection(sqlServerConn);
        using var pgConn = new NpgsqlConnection(postgresConn);
        sqlConn.Open();
        pgConn.Open();

        var cmd = new SqlCommand("SELECT Id, Username, PasswordHash, Role, CreatedAt, IsActive FROM Users", sqlConn);
        using var reader = cmd.ExecuteReader();

        int count = 0;
        while (reader.Read())
        {
            var insert = new NpgsqlCommand(@"
                INSERT INTO ""Users"" (""Username"", ""PasswordHash"", ""Role"", ""IsActive"", ""CreatedAt"")
                VALUES (@username, @hash, @role, @active, @created)", pgConn);

            string username = reader.GetString(1);
            insert.Parameters.AddWithValue("username", username);
            insert.Parameters.AddWithValue("hash", reader.GetString(2));
            insert.Parameters.AddWithValue("role", reader.GetString(3));
            insert.Parameters.AddWithValue("active", reader.GetBoolean(5));
            insert.Parameters.AddWithValue("created", reader.GetDateTime(4).ToUniversalTime());
            insert.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"  ✓ Migrated {count} users");
    }

    static void MigrateEmployees()
    {
        Console.WriteLine("Migrating Employees...");
        using var sqlConn = new SqlConnection(sqlServerConn);
        using var pgConn = new NpgsqlConnection(postgresConn);
        sqlConn.Open();
        pgConn.Open();

        var cmd = new SqlCommand(@"
            SELECT Id, Name, Email, Phone, Department, Designation, JoinDate, Salary, 
                   IsActive, CreatedAt, IcPassport, TaxNumber, DepartmentId, ProfilePicture
            FROM Employees", sqlConn);
        using var reader = cmd.ExecuteReader();

        int count = 0;
        while (reader.Read())
        {
            string fullName = reader.GetString(1);

            var insert = new NpgsqlCommand(@"
                INSERT INTO ""Employees"" (""Name"", ""Email"", ""Phone"", ""DepartmentId"", ""Designation"", ""JoinDate"", 
                    ""Salary"", ""IsActive"", ""CreatedAt"", ""ProfilePicture"", ""IcPassport"", ""TaxNumber"", ""AccountNumber"")
                VALUES (@name, @email, @phone, @dept, @desig, @join, @salary, @active, @created, @pic, @ic, @tax, @acc)", pgConn);

            insert.Parameters.AddWithValue("name", fullName);
            insert.Parameters.AddWithValue("email", reader.IsDBNull(2) ? "" : reader.GetString(2));
            insert.Parameters.AddWithValue("phone", reader.IsDBNull(3) ? "" : reader.GetString(3));
            insert.Parameters.AddWithValue("dept", reader.IsDBNull(12) ? (object)DBNull.Value : reader.GetInt32(12));
            insert.Parameters.AddWithValue("desig", reader.IsDBNull(5) ? "Employee" : reader.GetString(5));
            insert.Parameters.AddWithValue("join", reader.GetDateTime(6).ToUniversalTime());
            insert.Parameters.AddWithValue("salary", reader.IsDBNull(7) ? 0m : reader.GetDecimal(7));
            insert.Parameters.AddWithValue("active", reader.GetBoolean(8));
            insert.Parameters.AddWithValue("created", reader.GetDateTime(9).ToUniversalTime());
            insert.Parameters.AddWithValue("pic", reader.IsDBNull(13) ? "" : reader.GetString(13));
            insert.Parameters.AddWithValue("ic", reader.IsDBNull(10) ? "" : reader.GetString(10));
            insert.Parameters.AddWithValue("tax", reader.IsDBNull(11) ? "" : reader.GetString(11));
            insert.Parameters.AddWithValue("acc", "");
            insert.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"  ✓ Migrated {count} employees");
    }

    static void MigrateAttendances()
    {
        Console.WriteLine("Migrating Attendances...");
        using var sqlConn = new SqlConnection(sqlServerConn);
        using var pgConn = new NpgsqlConnection(postgresConn);
        sqlConn.Open();
        pgConn.Open();

        var cmd = new SqlCommand(@"
            SELECT Id, EmployeeId, Date, Status, Remarks, CreatedAt, CheckIn, CheckOut, WorkHours
            FROM Attendances", sqlConn);
        using var reader = cmd.ExecuteReader();

        int count = 0;
        while (reader.Read())
        {
            var insert = new NpgsqlCommand(@"
                INSERT INTO ""Attendances"" (""EmployeeId"", ""Date"", ""Status"", ""CheckIn"", ""CheckOut"", 
                    ""WorkHours"", ""Remarks"", ""CreatedAt"")
                VALUES (@empid, @date, @status, @checkin, @checkout, @hours, @remarks, @created)
                ON CONFLICT (""EmployeeId"", ""Date"") DO NOTHING", pgConn);

            insert.Parameters.AddWithValue("empid", reader.GetInt32(1));
            insert.Parameters.AddWithValue("date", reader.GetDateTime(2).ToUniversalTime());
            insert.Parameters.AddWithValue("status", reader.IsDBNull(3) ? "Present" : reader.GetString(3));
            insert.Parameters.AddWithValue("checkin", reader.IsDBNull(6) ? (object)DBNull.Value : reader.GetDateTime(6).ToUniversalTime());
            insert.Parameters.AddWithValue("checkout", reader.IsDBNull(7) ? (object)DBNull.Value : reader.GetDateTime(7).ToUniversalTime());
            insert.Parameters.AddWithValue("hours", reader.IsDBNull(8) ? 0m : reader.GetDecimal(8));
            insert.Parameters.AddWithValue("remarks", reader.IsDBNull(4) ? "" : reader.GetString(4));
            insert.Parameters.AddWithValue("created", reader.GetDateTime(5).ToUniversalTime());
            insert.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"  ✓ Migrated {count} attendances");
    }

    static void MigrateTimesheets()
    {
        Console.WriteLine("Migrating Timesheets...");
        using var sqlConn = new SqlConnection(sqlServerConn);
        using var pgConn = new NpgsqlConnection(postgresConn);
        sqlConn.Open();
        pgConn.Open();

        var cmd = new SqlCommand(@"
            SELECT Id, EmployeeId, Month, Year, TotalWorkingDays, TotalPresent, TotalAbsent, 
                   TotalLeave, TotalHalfDay, TotalPublicHolidays, TotalWorkHours, GeneratedOn, 
                   Status, ApprovedBy, ApprovedOn, Remarks, TotalMedicalLeave
            FROM Timesheets", sqlConn);
        using var reader = cmd.ExecuteReader();

        int count = 0;
        while (reader.Read())
        {
            var insert = new NpgsqlCommand(@"
                INSERT INTO ""Timesheets"" (""EmployeeId"", ""Month"", ""Year"", ""TotalWorkingDays"", 
                    ""TotalPresent"", ""TotalMedicalLeave"", ""TotalAbsent"", ""TotalLeave"", 
                    ""TotalHalfDay"", ""TotalPublicHolidays"", ""TotalWorkHours"", ""GeneratedOn"", 
                    ""Status"", ""ApprovedBy"", ""ApprovedOn"", ""Remarks"")
                VALUES (@empid, @month, @year, @workdays, @present, @medical, @absent, @leave, 
                    @halfday, @holidays, @hours, @generated, @status, @approver, @approved, @remarks)
                ON CONFLICT (""EmployeeId"", ""Month"", ""Year"") DO NOTHING", pgConn);

            insert.Parameters.AddWithValue("empid", reader.GetInt32(1));
            insert.Parameters.AddWithValue("month", reader.GetInt32(2));
            insert.Parameters.AddWithValue("year", reader.GetInt32(3));
            insert.Parameters.AddWithValue("workdays", reader.GetInt32(4));
            insert.Parameters.AddWithValue("present", reader.GetInt32(5));
            insert.Parameters.AddWithValue("medical", reader.IsDBNull(16) ? 0 : reader.GetInt32(16));
            insert.Parameters.AddWithValue("absent", reader.GetInt32(6));
            insert.Parameters.AddWithValue("leave", reader.GetInt32(7));
            insert.Parameters.AddWithValue("halfday", reader.GetInt32(8));
            insert.Parameters.AddWithValue("holidays", reader.GetInt32(9));
            insert.Parameters.AddWithValue("hours", reader.GetDecimal(10));
            insert.Parameters.AddWithValue("generated", reader.GetDateTime(11).ToUniversalTime());
            insert.Parameters.AddWithValue("status", reader.IsDBNull(12) ? "Draft" : reader.GetString(12));
            insert.Parameters.AddWithValue("approver", reader.IsDBNull(13) ? (object)DBNull.Value : reader.GetInt32(13));
            insert.Parameters.AddWithValue("approved", reader.IsDBNull(14) ? (object)DBNull.Value : reader.GetDateTime(14).ToUniversalTime());
            insert.Parameters.AddWithValue("remarks", reader.IsDBNull(15) ? (object)DBNull.Value : reader.GetString(15));
            insert.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"  ✓ Migrated {count} timesheets");
    }

    static void MigratePayrolls()
    {
        Console.WriteLine("Migrating Payrolls...");
        using var sqlConn = new SqlConnection(sqlServerConn);
        using var pgConn = new NpgsqlConnection(postgresConn);
        sqlConn.Open();
        pgConn.Open();

        var cmd = new SqlCommand(@"
            SELECT Id, EmployeeId, Month, Year, BasicSalary, Allowances, Deductions, 
                   NetSalary, GeneratedOn, EpfAmount, SocsoAmount, TaxAmount, GrossIncome
            FROM Payrolls", sqlConn);
        using var reader = cmd.ExecuteReader();

        int count = 0;
        int skipped = 0;
        while (reader.Read())
        {
            int employeeId = reader.GetInt32(1);
            
            // Check if employee exists in PostgreSQL
            var checkCmd = new NpgsqlCommand(@"SELECT COUNT(*) FROM ""Employees"" WHERE ""Id"" = @id", pgConn);
            checkCmd.Parameters.AddWithValue("id", employeeId);
            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
            
            if (exists == 0)
            {
                Console.WriteLine($"  ⚠ Skipping payroll for non-existent EmployeeId: {employeeId}");
                skipped++;
                continue;
            }

            var insert = new NpgsqlCommand(@"
                INSERT INTO ""Payrolls"" (""EmployeeId"", ""Month"", ""Year"", ""BasicSalary"", 
                    ""Allowances"", ""Deductions"", ""EpfAmount"", ""SocsoAmount"", ""TaxAmount"", 
                    ""GrossIncome"", ""NetSalary"", ""GeneratedOn"")
                VALUES (@empid, @month, @year, @basic, @allow, @deduct, @epf, @socso, @tax, 
                    @gross, @net, @generated)
                ON CONFLICT (""EmployeeId"", ""Month"", ""Year"") DO NOTHING", pgConn);

            insert.Parameters.AddWithValue("empid", employeeId);
            insert.Parameters.AddWithValue("month", reader.GetInt32(2));
            insert.Parameters.AddWithValue("year", reader.GetInt32(3));
            insert.Parameters.AddWithValue("basic", reader.GetDecimal(4));
            insert.Parameters.AddWithValue("allow", reader.GetDecimal(5));
            insert.Parameters.AddWithValue("deduct", reader.GetDecimal(6));
            insert.Parameters.AddWithValue("epf", reader.GetDecimal(9));
            insert.Parameters.AddWithValue("socso", reader.GetDecimal(10));
            insert.Parameters.AddWithValue("tax", reader.GetDecimal(11));
            insert.Parameters.AddWithValue("gross", reader.GetDecimal(12));
            insert.Parameters.AddWithValue("net", reader.GetDecimal(7));
            insert.Parameters.AddWithValue("generated", reader.GetDateTime(8).ToUniversalTime());
            insert.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"  ✓ Migrated {count} payrolls" + (skipped > 0 ? $" (skipped {skipped})" : ""));
    }
}
