using System.Text;
using System.Threading.RateLimiting;
using Booking.Api.Authentication;
using Booking.Api.Accounting;
using Booking.Api.GoogleBusiness;
using Booking.Api.Identity;
using Booking.Api.Tenancy;
using Booking.Api.Delivery;
using Booking.Api.WidgetAssets;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.RateLimiting;
using Booking.Api.Contracts.Common;
using Booking.Application.Abstractions;
using Booking.Application.Accounting;
using Booking.Application.Analytics;
using Booking.Application.Availability;
using Booking.Application.Delivery;
using Booking.Application.OpeningHours;
using Booking.Application.Reservations;
using Booking.Application.Restaurants;
using Booking.Application.Scheduling;
using Booking.Application.Tables;
using Booking.Application.WidgetSettings;
using Booking.Infrastructure;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

using var jwtLoggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    logging.AddConsole();
});

var jwtOptions = JwtAuthenticationOptionsFactory.Create(
    builder.Configuration,
    builder.Environment,
    jwtLoggerFactory.CreateLogger("JwtAuthentication"));

builder.Services.AddSingleton(jwtOptions);

var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("Zambiq.Booking.Api");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

// Rate limiting for anonymous/public endpoints, partitioned per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    var permitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Public:PermitLimit") ?? 20;
    var windowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:Public:WindowSeconds") ?? 60;
    var queueLimit = builder.Configuration.GetValue<int?>("RateLimiting:Public:QueueLimit") ?? 0;

    options.AddPolicy("public", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = queueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

builder.Services.AddScoped<CreateRestaurantUseCase>();
builder.Services.AddScoped<GetRestaurantUseCase>();
builder.Services.AddScoped<UpdateRestaurantUseCase>();
builder.Services.AddScoped<SetOpeningHoursUseCase>();
builder.Services.AddScoped<GetOpeningHoursUseCase>();
builder.Services.AddScoped<CheckAvailabilityUseCase>();
builder.Services.AddScoped<CreateReservationUseCase>();
builder.Services.AddScoped<GetReservationsUseCase>();
builder.Services.AddScoped<GetReservationUseCase>();
builder.Services.AddScoped<ChangeReservationStatusUseCase>();
builder.Services.AddScoped<ChangeRestaurantReservationStatusUseCase>();
builder.Services.AddScoped<GetReservationAnalyticsUseCase>();
builder.Services.AddScoped<CreateShiftUseCase>();
builder.Services.AddScoped<UpdateShiftUseCase>();
builder.Services.AddScoped<DeleteShiftUseCase>();
builder.Services.AddScoped<GetShiftsUseCase>();
builder.Services.AddScoped<GetStaffShiftsUseCase>();
builder.Services.AddScoped<GetStaffAvailabilityUseCase>();
builder.Services.AddScoped<SetStaffAvailabilityUseCase>();
builder.Services.AddScoped<GetRestaurantAvailabilityUseCase>();
builder.Services.AddScoped<RequestLeaveUseCase>();
builder.Services.AddScoped<GetStaffLeaveUseCase>();
builder.Services.AddScoped<GetRestaurantLeaveUseCase>();
builder.Services.AddScoped<DecideLeaveUseCase>();
builder.Services.AddScoped<IngestDeliveryOrderUseCase>();
builder.Services.AddScoped<GetDeliveryOrdersUseCase>();
builder.Services.AddScoped<ResolveDeliveryIntegrationUseCase>();
builder.Services.AddScoped<GetDeliveryIntegrationsUseCase>();
builder.Services.AddScoped<ConnectDeliveryProviderUseCase>();
builder.Services.AddScoped<SetDeliveryProviderEnabledUseCase>();
builder.Services.AddScoped<DeliveryIngressService>();
builder.Services.AddScoped<GetWidgetOriginsUseCase>();
builder.Services.AddScoped<SetWidgetOriginsUseCase>();
builder.Services.AddScoped<GetWidgetBrandingUseCase>();
builder.Services.AddScoped<SetWidgetBrandingUseCase>();
builder.Services.AddScoped<CreateTableUseCase>();
builder.Services.AddScoped<GetTablesUseCase>();
builder.Services.AddScoped<UpdateTableUseCase>();
builder.Services.AddScoped<DeactivateTableUseCase>();
builder.Services.AddScoped<ReactivateTableUseCase>();
builder.Services.AddScoped<AssignTableUseCase>();
builder.Services.AddScoped<UpdateTableLayoutUseCase>();
builder.Services.AddScoped<AccountingUseCases>();
builder.Services.AddScoped<AccountingCsvService>();
builder.Services.AddScoped<AccountingReportService>();
builder.Services.AddScoped<AccountingConnectorService>();
builder.Services.AddSingleton<IAccountingAssetStorage, FileSystemAccountingAssetStorage>();
builder.Services.AddHostedService<AccountingDailySyncService>();
builder.Services.AddHttpClient("AccountingGoCardless", client =>
    client.BaseAddress = new Uri("https://bankaccountdata.gocardless.com/"));
builder.Services.AddHttpClient("AccountingMollie", client =>
    client.BaseAddress = new Uri("https://api.mollie.com/"));
builder.Services.AddHttpClient("AccountingMollieApi", client =>
    client.BaseAddress = new Uri("https://api.mollie.com/"));
builder.Services.AddSingleton<IWidgetLogoStorage, FileSystemWidgetLogoStorage>();
builder.Services.AddScoped<GoogleBusinessService>();
builder.Services.AddScoped<GoogleBusinessHoursSyncService>();
builder.Services.AddHostedService<GoogleBusinessReviewSyncService>();
builder.Services.AddHttpClient("GoogleOAuth");
builder.Services.AddHttpClient("GoogleMyBusinessV4", client =>
    client.BaseAddress = new Uri("https://mybusiness.googleapis.com/v4/"));
builder.Services.AddHttpClient("GoogleAccountManagementV1", client =>
    client.BaseAddress = new Uri("https://mybusinessaccountmanagement.googleapis.com/v1/"));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<BookingDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireRole(BookingRoles.SuperAdmin));
    options.AddPolicy("RestaurantUser", policy =>
        policy.RequireRole(BookingRoles.Owner, BookingRoles.Staff));
    options.AddPolicy("RestaurantOwner", policy =>
        policy.RequireRole(BookingRoles.Owner));
});

var app = builder.Build();

using (var migrationScope = app.Services.CreateScope())
{
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.Services.SeedDevelopmentIdentityAsync(
    app.Configuration,
    app.Environment);

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
            "Er is een onverwachte fout opgetreden. Probeer het later opnieuw."));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program
{
}
