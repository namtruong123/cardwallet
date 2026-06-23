using System.Text;
using CardWallet.Application.DTOs.Auth;
using CardWallet.Application.DTOs.CardRates;
using CardWallet.Application.DTOs.Wallets;
using CardWallet.Application.Interfaces;
using CardWallet.Application.Services;
using CardWallet.Application.Validators.Auth;
using CardWallet.Application.Validators.CardRates;
using CardWallet.Application.Validators.Wallets;
using CardWallet.Infrastructure.Data;
using CardWallet.Infrastructure.Repositories;
using CardWallet.Infrastructure.Security;
using CardWallet.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 32)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 2,
                maxRetryDelay: TimeSpan.FromSeconds(2),
                errorNumbersToAdd: null);
        }
    );
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<DepositRequest>, DepositRequestValidator>();
builder.Services.AddScoped<IValidator<WithdrawRequest>, WithdrawRequestValidator>();
builder.Services.AddScoped<IValidator<CreateCardRateRequest>, CreateCardRateRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateCardRateRequest>, UpdateCardRateRequestValidator>();

builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ICardRateRepository, CardRateRepository>();
builder.Services.AddScoped<ICardRateService, CardRateService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ISearchAliasRepository, SearchAliasRepository>();
builder.Services.AddScoped<IAdminSearchAliasService, AdminSearchAliasService>();
builder.Services.AddScoped<IBlogPostRepository, BlogPostRepository>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IPartnerTaskRepository, PartnerTaskRepository>();
builder.Services.AddScoped<ITaskSubmissionRepository, TaskSubmissionRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<CardWallet.Application.Interfaces.ICardTransactionRepository, CardWallet.Infrastructure.Repositories.CardTransactionRepository>();
builder.Services.AddScoped<CardWallet.Application.Interfaces.IParentCardApiClient, CardWallet.Api.Clients.ParentCardApiClient>();
builder.Services.AddScoped<CardWallet.Application.Interfaces.ICardExchangeService, CardWallet.Application.Services.CardExchangeService>();
if (builder.Configuration.GetValue("Workers:CardExchange:Enabled", true))
{
    builder.Services.AddHostedService<CardWallet.Api.Workers.CardExchangeWorker>();
}

if (builder.Configuration.GetValue("Workers:CardTransactionReconciliation:Enabled", true))
{
    builder.Services.AddHostedService<CardWallet.Api.Workers.CardTransactionReconciliationWorker>();
}

var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("Admin") || context.User.HasClaim("canManageUsers", "True")));
    options.AddPolicy("CanTransferPoints", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanApproveKycWithdraw", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("Admin") || context.User.HasClaim("canApproveKycWithdraw", "True")));
});

var app = builder.Build();

var migrateOnStartup = builder.Configuration.GetValue("Database:MigrateOnStartup", true);
if (migrateOnStartup)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Database migration skipped: " + ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SubdomainRoutingMiddleware>();

app.UseRouting();

app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

// Optional startup seed: useful for first deploy when the database is empty.
// Keep disabled in Production unless Admin:SeedOnStartup is explicitly enabled.
var seedAdminOnStartup = app.Environment.IsDevelopment() || builder.Configuration.GetValue("Admin:SeedOnStartup", false);
if (seedAdminOnStartup)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userRepo = scope.ServiceProvider.GetRequiredService<CardWallet.Application.Interfaces.IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<CardWallet.Application.Interfaces.IPasswordHasher>();

        var adminEmail = config["Admin:Email"] ?? "admin@example.com";
        var adminPhone = config["Admin:Phone"] ?? "0900000000";
        var adminPassword = config["Admin:Password"] ?? "AdminPass123";

        var existingByEmail = db.Users.Include(x => x.Wallet).FirstOrDefault(x => x.Email == adminEmail);
        var existingByPhone = db.Users.Include(x => x.Wallet).FirstOrDefault(x => x.PhoneNumber == adminPhone);
        var adminUser = existingByEmail ?? existingByPhone;

        if (adminUser == null)
        {
            adminUser = new CardWallet.Domain.Entities.User
            {
                FullName = "Administrator",
                Email = adminEmail,
                PhoneNumber = adminPhone,
                CreatedAt = DateTime.UtcNow,
                Wallet = new CardWallet.Domain.Entities.Wallet()
            };

            userRepo.AddAsync(adminUser).GetAwaiter().GetResult();
        }

        adminUser.FullName = "Administrator";
        adminUser.Email = adminEmail;
        if (existingByPhone == null || existingByPhone.Id == adminUser.Id)
        {
            adminUser.PhoneNumber = adminPhone;
        }
        adminUser.PasswordHash = hasher.Hash(adminPassword);
        adminUser.Status = "Active";
        adminUser.Role = "Admin";
        adminUser.CanManageUsers = true;
        adminUser.CanManageTasks = true;
        adminUser.CanApproveTasks = true;
        adminUser.CanApproveKycWithdraw = true;
        adminUser.CanTransferPoints = true;
        adminUser.CanManageBlog = true;
        adminUser.CanExportReports = true;
        adminUser.FailedLoginAttempts = 0;
        adminUser.LockoutEndAt = null;
        adminUser.IsDeleted = false;
        adminUser.DeletedAt = null;
        adminUser.Wallet ??= new CardWallet.Domain.Entities.Wallet { UserId = adminUser.Id };

        const long initialTotalSupply = 50_000_000;
        var currentSupply = db.Wallets.Sum(x => x.Balance);
        if (currentSupply == 0)
        {
            adminUser.Wallet.Balance = initialTotalSupply;
            adminUser.Wallet.UpdatedAt = DateTime.UtcNow;
        }

        userRepo.SaveChangesAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        // swallow errors in seed to avoid startup failure; log if needed
        Console.WriteLine("Admin seed skipped: " + ex.Message);
    }
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
