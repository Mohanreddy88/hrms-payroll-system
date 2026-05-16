using HrmsApi.Data;
using HrmsApi.Middleware;
using HrmsApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// Check for command-line utilities
if (args.Length > 0 && args[0] == "generate-hash")
{
    HrmsApi.HashGenerator.GenerateHash();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// ── Railway Environment Variables Override ──────────────
// Railway provides DATABASE_URL directly as environment variable
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse postgresql://user:pass@host:port/db to .NET connection string format
    var match = System.Text.RegularExpressions.Regex.Match(
        databaseUrl, 
        @"postgresql://([^:]+):([^@]+)@([^:]+):(\d+)/(.+)"
    );
    
    if (match.Success)
    {
        var connString = $"Host={match.Groups[3].Value};Port={match.Groups[4].Value};Database={match.Groups[5].Value};Username={match.Groups[1].Value};Password={match.Groups[2].Value};SSL Mode=Require;Trust Server Certificate=true";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connString;
        builder.Configuration["UsePostgreSQL"] = "true";
    }
}

var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrEmpty(jwtSecretKey))
{
    builder.Configuration["Jwt:Key"] = jwtSecretKey;
}

var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER");
var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
if (!string.IsNullOrEmpty(smtpUser))
{
    builder.Configuration["Email:SmtpUser"] = smtpUser;
    builder.Configuration["Email:SmtpPassword"] = smtpPassword;
    builder.Configuration["Email:FromEmail"] = smtpUser;
}

// ── Database ────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var usePostgres = builder.Configuration.GetValue<bool>("UsePostgreSQL");

builder.Services.AddDbContext<HrmsDbContext>(options =>
{
    if (usePostgres || connectionString?.Contains("postgres") == true)
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// ── JWT Authentication ──────────────────────────────────
var jwtConfig = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtConfig["Issuer"],
        ValidAudience            = jwtConfig["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(key),
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ── CORS — allow Angular dev server and production ─────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        // Check if running in production (behind Nginx proxy)
        var isProduction = builder.Environment.IsProduction() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"));
        
        if (isProduction)
        {
            // In production (Railway/Nginx), allow all origins since Nginx handles the proxy
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // In development, restrict to localhost Angular dev server
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// ── Services ─────────────────────────────────────────────
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IPayrollCalculationService, PayrollCalculationService>();
builder.Services.AddControllers();

// ── Swagger/OpenAPI ───────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HRMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without 'Bearer' prefix)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ─────────────────────────────────────────────────
var app = builder.Build();

// ── Auto-run Database Migration on Startup (Railway) ─────
// Apply migrations and seed data automatically
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<HrmsDbContext>();
        
        logger.LogInformation("🔄 Checking database state...");
        
        // Check if migrations history exists but is empty - this causes issues
        var historyExists = context.Database.ExecuteSqlRaw(@"
            SELECT 1 FROM information_schema.tables 
            WHERE table_name = '__EFMigrationsHistory'") > -1;
            
        if (historyExists)
        {
            var migrationsCount = context.Database.SqlQueryRaw<int>(
                @"SELECT COUNT(*)::int FROM ""__EFMigrationsHistory""").FirstOrDefault();
                
            if (migrationsCount == 0)
            {
                logger.LogWarning("⚠️ Found empty migrations history table - dropping it to force recreation");
                context.Database.ExecuteSqlRaw(@"DROP TABLE IF EXISTS ""__EFMigrationsHistory"" CASCADE");
            }
        }
        
        logger.LogInformation("🔄 Applying database migrations...");
        context.Database.Migrate();
        logger.LogInformation("✅ Database migrations applied!");
        
        // Seed leave types if they don't exist
        if (!context.LeaveTypes.Any())
        {
            logger.LogInformation("🌱 Seeding leave types...");
            context.LeaveTypes.AddRange(new[]
            {
                new HrmsApi.Models.LeaveType { Name = "Annual Leave", Code = "AL", DefaultDaysPerYear = 14, IsActive = true, RequiresApproval = true, IsPaid = true, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Medical Leave", Code = "ML", DefaultDaysPerYear = 14, IsActive = true, RequiresApproval = true, IsPaid = true, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Emergency Leave", Code = "EL", DefaultDaysPerYear = 2, IsActive = true, RequiresApproval = true, IsPaid = true, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Casual Leave", Code = "CL", DefaultDaysPerYear = 3, IsActive = true, RequiresApproval = true, IsPaid = true, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Maternity Leave", Code = "MTL", DefaultDaysPerYear = 98, IsActive = true, RequiresApproval = true, IsPaid = true, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Paternity Leave", Code = "PTL", DefaultDaysPerYear = 7, IsActive = true, RequiresApproval = true, IsPaid = true, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Unpaid Leave", Code = "UL", DefaultDaysPerYear = 0, IsActive = true, RequiresApproval = true, IsPaid = false, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Study Leave", Code = "SL", DefaultDaysPerYear = 5, IsActive = true, RequiresApproval = true, IsPaid = true, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Hajj Leave", Code = "HL", DefaultDaysPerYear = 0, IsActive = true, RequiresApproval = true, IsPaid = false, CreatedAt = DateTime.UtcNow },
                new HrmsApi.Models.LeaveType { Name = "Replacement Leave", Code = "RL", DefaultDaysPerYear = 0, IsActive = true, RequiresApproval = true, IsPaid = true, CreatedAt = DateTime.UtcNow }
            });
            context.SaveChanges();
            logger.LogInformation("✅ Leave types seeded (10 types)");
        }
        
        // Seed default admin user if no users exist
        if (!context.Users.Any())
        {
            logger.LogInformation("🌱 Seeding default admin user...");
            var adminUser = new HrmsApi.Models.User
            {
                Username = "admin",
                Email = "admin@hrms.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
            context.SaveChanges();
            logger.LogInformation("✅ Default admin user created (Username: admin, Password: Admin@123)");
        }
        
        logger.LogInformation("🎉 Database ready!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database setup error - continuing anyway");
        // Don't throw - let app start
    }
}

// ── Middleware Pipeline ───────────────────────────────────
app.UseGlobalExceptionHandler();   // Custom exception middleware

// Enable Swagger for both Development and Production
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRMS API v1"));

// app.UseHttpsRedirection(); // Disabled — use HTTP for local dev to avoid cert issues

// Enable static file serving for uploaded files
app.UseStaticFiles();

// CORS MUST come before Authentication/Authorization so preflight OPTIONS
// requests are handled before the JWT middleware can reject them.
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible for integration testing
public partial class Program { }
