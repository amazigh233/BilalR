using System.Text;
using Booking.Api.Authentication;
using Booking.Api.Identity;
using System.Text.Json.Serialization;
using Booking.Api.Contracts.Common;
using Booking.Application.Availability;
using Booking.Application.OpeningHours;
using Booking.Application.Reservations;
using Booking.Application.Restaurants;
using Booking.Infrastructure;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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

builder.Services.AddScoped<CreateRestaurantUseCase>();
builder.Services.AddScoped<GetRestaurantsUseCase>();
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

app.MapControllers();

app.Run();

public partial class Program
{
}
