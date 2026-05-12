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

            Console.WriteLine("\n✅ Migration completed successfully!");
            Console.WriteLine("\nNote: Attendances, Timesheets, and Payrolls were skipped.");
            Console.WriteLine("You can create fresh data using the application after login.");
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
                INSERT INTO ""Departments"" (""DepartmentId"", ""DepartmentName"", ""Description"", ""CreatedDate"")
                VALUES (@id, @name, @desc, @created)
                ON CONFLICT (""DepartmentId"") DO NOTHING", pgConn);

            insert.Parameters.AddWithValue("id", reader.GetInt32(0));
            insert.Parameters.AddWithValue("name", reader.GetString(1));
            insert.Parameters.AddWithValue("desc", reader.IsDBNull(2) ? (object)DBNull.Value : reader.GetString(2));
            insert.Parameters.AddWithValue("created", reader.GetDateTime(4));
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
                INSERT INTO ""Users"" (""UserId"", ""Username"", ""Email"", ""PasswordHash"", ""Role"", ""IsActive"", ""CreatedDate"", ""LastLoginDate"")
                VALUES (@id, @username, @email, @hash, @role, @active, @created, NULL)
                ON CONFLICT (""UserId"") DO NOTHING", pgConn);

            string username = reader.GetString(1);
            insert.Parameters.AddWithValue("id", reader.GetInt32(0));
            insert.Parameters.AddWithValue("username", username);
            insert.Parameters.AddWithValue("email", username.Contains("@") ? username : username + "@hrms.local");
            insert.Parameters.AddWithValue("hash", reader.GetString(2));
            insert.Parameters.AddWithValue("role", reader.GetString(3));
            insert.Parameters.AddWithValue("active", reader.GetBoolean(5));
            insert.Parameters.AddWithValue("created", reader.GetDateTime(4));
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
            string[] nameParts = fullName.Split(' ', 2);
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[1] : "";

            var insert = new NpgsqlCommand(@"
                INSERT INTO ""Employees"" (""EmployeeId"", ""FirstName"", ""LastName"", ""Email"", ""PhoneNumber"", ""HireDate"",
                    ""JobTitle"", ""DepartmentId"", ""Salary"", ""IsActive"", ""ProfilePicture"")
                VALUES (@id, @fname, @lname, @email, @phone, @hire, @job, @dept, @salary, @active, @pic)
                ON CONFLICT (""EmployeeId"") DO NOTHING", pgConn);

            insert.Parameters.AddWithValue("id", reader.GetInt32(0));
            insert.Parameters.AddWithValue("fname", firstName);
            insert.Parameters.AddWithValue("lname", lastName);
            insert.Parameters.AddWithValue("email", reader.IsDBNull(2) ? "" : reader.GetString(2));
            insert.Parameters.AddWithValue("phone", reader.IsDBNull(3) ? (object)DBNull.Value : reader.GetString(3));
            insert.Parameters.AddWithValue("hire", reader.GetDateTime(6));
            insert.Parameters.AddWithValue("job", reader.IsDBNull(5) ? "Employee" : reader.GetString(5));
            insert.Parameters.AddWithValue("dept", reader.IsDBNull(12) ? (object)DBNull.Value : reader.GetInt32(12));
            insert.Parameters.AddWithValue("salary", reader.IsDBNull(7) ? (object)DBNull.Value : reader.GetDecimal(7));
            insert.Parameters.AddWithValue("active", reader.GetBoolean(8));
            insert.Parameters.AddWithValue("pic", reader.IsDBNull(13) ? (object)DBNull.Value : reader.GetString(13));
            insert.ExecuteNonQuery();
            count++;
        }
        Console.WriteLine($"  ✓ Migrated {count} employees");
    }
}
