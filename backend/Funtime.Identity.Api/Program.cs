using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Funtime.Identity.Api.Data;
using Funtime.Identity.Api.Services;
using Funtime.Identity.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Database - Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Allow JWT token from query string for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Custom Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IStripeService, StripeService>();

// HttpClient for OAuth providers
builder.Services.AddHttpClient();

// SignalR for real-time notifications
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificationService, NotificationService>();

// File Storage - configurable between local and S3
var storageType = builder.Configuration["Storage:Type"] ?? "local";
if (storageType.Equals("s3", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IFileStorageService, AwsS3StorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}

// Configure form options for large file uploads (videos up to 100MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 150 * 1024 * 1024; // 150MB
});

// Configure Kestrel for large uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 150 * 1024 * 1024; // 150MB
});

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Funtime Identity API",
        Version = "v1",
        Description = "Shared authentication and payments service for Funtime Pickleball sites"
    });

    // JWT Authentication in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
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

// CORS - configurable from appsettings.json
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Fallback for development if no origins configured
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files (for uploaded logos)
app.UseStaticFiles();

// CORS must be before other middleware
app.UseCors("ConfiguredOrigins");

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR hubs
app.MapHub<NotificationHub>("/hubs/notifications");

// Apply migrations automatically in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
