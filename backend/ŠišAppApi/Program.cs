using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ŠišAppApi.Data;
using ŠišAppApi.Services;
using ŠišAppApi.Services.Interfaces;
using ŠišAppApi.Services.Services;
using Stripe;
using MassTransit;
using Mapster;
using MapsterMapper;
using System.Reflection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ŠišAppApi.Hubs;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<ŠišAppApi.Filters.ExceptionFilter>();
builder.Services.AddControllers(x =>
    {
        x.Filters.AddService<ŠišAppApi.Filters.ExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"] ??
            throw new InvalidOperationException("JWT Key is not configured in appsettings.json");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Invalid access token claims.");
                    return;
                }

                await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var isRevoked = await dbContext.RefreshTokens.AnyAsync(rt =>
                    rt.UserId == userId &&
                    !rt.IsDeleted &&
                    rt.Token == $"revoked-jti:{jti}" &&
                    rt.ExpiresAt > DateTime.UtcNow);

                if (isRevoked)
                {
                    context.Fail("Access token has been revoked.");
                }
            }
        };
    });


var stripeSecretKey = builder.Configuration["Stripe:SecretKey"]
    ?? throw new InvalidOperationException("Stripe secret key is not configured");
StripeConfiguration.ApiKey = stripeSecretKey;


builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IPaymentFinalizationService, PaymentFinalizationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISalonService, SalonService>();
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<IServiceCategoryService, ServiceCategoryService>();
builder.Services.AddScoped<ISalonAmenityService, SalonAmenityService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IAdminLogService, AdminLogService>();
builder.Services.AddScoped<IBarberService, BarberService>();
builder.Services.AddScoped<IWorkingHoursService, WorkingHoursService>();
builder.Services.AddScoped<IReviewService, ŠišAppApi.Services.Services.ReviewService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IStripeTransactionService, StripeTransactionService>();
builder.Services.AddScoped<IPayPalOrderService, PayPalOrderService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddHttpClient("PayPal");
builder.Services.AddHostedService<AppointmentStatusWorker>();
builder.Services.AddHostedService<RevokedTokenCleanupWorker>();


builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"]
            ?? throw new InvalidOperationException("RabbitMQ host is not configured");
        var rabbitUser = builder.Configuration["RabbitMQ:Username"]
            ?? throw new InvalidOperationException("RabbitMQ username is not configured");
        var rabbitPass = builder.Configuration["RabbitMQ:Password"]
            ?? throw new InvalidOperationException("RabbitMQ password is not configured");
        cfg.Host(rabbitHost, "/", h => {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
    });
});


var config = TypeAdapterConfig.GlobalSettings;
config.Default.PreserveReference(true);
config.Scan(Assembly.GetExecutingAssembly());
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, Mapper>();




builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ŠišApp API",
        Version = "v1",
        Description = "API za ŠišApp aplikaciju"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});


var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Select(o => o.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    var originsCsv = builder.Configuration["Cors:AllowedOrigins"];
    allowedOrigins = originsCsv?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(o => o.Trim().TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray()
        ?? Array.Empty<string>();
}

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("CORS allowed origins are not configured. Set Cors__AllowedOrigins env var.");
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();


app.UseSwagger();



if (app.Environment.IsDevelopment())
{
    try
    {
        DbInitializer.Seed(app);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ŠišApp API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

