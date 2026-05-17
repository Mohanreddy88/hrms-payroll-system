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

// ── Resend API config (replaces SMTP — works on Railway) ────────────────
var resendApiKey  = Environment.GetEnvironmentVariable("RESEND_API_KEY");
var resendFrom    = Environment.GetEnvironmentVariable("RESEND_FROM") ?? "onboarding@resend.dev";
var smtpFromName  = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "HRMS Payroll";

if (!string.IsNullOrEmpty(resendApiKey))
{
    builder.Configuration["Resend:ApiKey"]    = resendApiKey;
    builder.Configuration["Resend:FromEmail"] = resendFrom;
    builder.Configuration["Email:FromName"]   = smtpFromName;
}

// ── Database ────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var usePostgres = builder.Configuration.GetValue<bool>("UsePostgreSQL");

// Enable legacy timestamp behaviour globally: Npgsql will accept DateTime with
// Kind=Local or Kind=Unspecified and store them as UTC without throwing.
// This is the safest fix for an existing codebase where every timestamp column
// is 'timestamp with time zone' and all DateTimes should be treated as UTC.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
            // In production (Railway/Nginx), allow ALL origins (*) since Nginx handles the proxy
            policy.SetIsOriginAllowed(_ => true)  // Allow * (any origin)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
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
builder.Services.AddHttpClient(); // Required by EmailService for Resend HTTP API
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IPayrollCalculationService, PayrollCalculationService>();

// Background email queue — singleton hosted service that survives request lifetime
// Controllers inject IEmailQueue and call Enqueue() — returns instantly, no 504
builder.Services.AddSingleton<BackgroundEmailService>();
builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<BackgroundEmailService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<BackgroundEmailService>());

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
        
        logger.LogInformation("🔄 Checking database connection...");
        
        // Skip migrations - tables were created via SQL script
        // Just verify we can connect
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new Exception("Cannot connect to database");
        }
        
        logger.LogInformation("✅ Database connected!");
        
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
