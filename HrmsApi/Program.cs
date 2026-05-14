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

// ── CORS — allow Angular dev server ─────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
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
// This automatically applies EF Core migrations when app starts
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<HrmsDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("🔄 Checking database migrations...");
        
        // Check if tables already exist (from manual migration)
        var canConnect = context.Database.CanConnect();
        if (canConnect)
        {
            // If migration fails because tables exist, just mark it as applied
            try
            {
                context.Database.Migrate();
                logger.LogInformation("✅ Database migrations applied successfully!");
            }
            catch (Exception migEx) when (migEx.Message.Contains("already exists") || migEx.Message.Contains("42P07"))
            {
                logger.LogWarning("⚠️ Tables already exist - marking migration as complete");
                
                // Manually insert migration record to mark it as applied
                context.Database.ExecuteSqlRaw(
                    @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                      VALUES ('20260514070632_InitialCreate', '8.0.0')
                      ON CONFLICT (""MigrationId"") DO NOTHING");
                      
                logger.LogInformation("✅ Migration marked as complete!");
            }
        }
        else
        {
            logger.LogWarning("⚠️ Cannot connect to database - skipping migrations");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error with database migrations - continuing anyway");
        // Don't throw - let app start even if migrations fail
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
