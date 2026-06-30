using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TodoApp.Api.Data;
using TodoApp.Api.DTOs;
using TodoApp.Api.Exceptions;
using TodoApp.Api.Models;
using TodoApp.Api.Middleware;
using TodoApp.Api.Repositories;
using TodoApp.Api.Services;
using TaskPriority = TodoApp.Api.Models.TaskPriority;
using TaskStatus = TodoApp.Api.Models.TaskStatus;

var builder = WebApplication.CreateBuilder(args);

// Enums serialized as strings; model validation uses standardized error shape
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ctx =>
    {
        var correlationId = ctx.HttpContext.Items[RequestLoggingMiddleware.CorrelationIdKey] as string
            ?? ctx.HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        var details = ctx.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => new FieldError
            {
                Field = e.Key,
                Message = string.IsNullOrEmpty(err.ErrorMessage) ? "The field is invalid." : err.ErrorMessage
            }))
            .ToList();

        return new BadRequestObjectResult(new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = "VALIDATION_ERROR",
                Message = "One or more fields are invalid.",
                Details = details,
                TraceId = ctx.HttpContext.TraceIdentifier,
                CorrelationId = correlationId
            }
        });
    };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Global exception handler — maps custom AppException subclasses to HTTP responses
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// PasswordHasher<T> is stateless; singleton is appropriate
builder.Services.AddSingleton<PasswordHasher<User>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
        // JWT middleware short-circuits before IExceptionHandler for 401 challenges;
        // override here so missing/invalid tokens also use the standardized error shape.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                var correlationId = ctx.HttpContext.Items[RequestLoggingMiddleware.CorrelationIdKey] as string
                    ?? ctx.HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "UNAUTHORIZED",
                        Message = "Authentication is required. Provide a valid Bearer token.",
                        TraceId = ctx.HttpContext.TraceIdentifier,
                        CorrelationId = correlationId
                    }
                });
            }
        };
    });
builder.Services.AddAuthorization();

// CORS: explicit origin only (wildcard is incompatible with credentialed requests)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Swagger/OpenAPI with JWT Bearer support so /swagger can call protected endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TodoApp API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)."
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
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

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<RequestLoggingMiddleware>();

// Apply migrations and seed demo data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed demo user + sample tasks if the database is empty.
    // Done via DI scope after migration (not HasData) so PasswordHasher<T> is available.
    if (!db.Users.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
        var demoUser = new User { Username = "demo", CreatedAt = DateTime.UtcNow };
        demoUser.PasswordHash = hasher.HashPassword(demoUser, "demo1234");
        db.Users.Add(demoUser);
        db.SaveChanges();

        var now = DateTime.UtcNow;
        db.Tasks.AddRange(
            new TaskItem
            {
                UserId = demoUser.Id, Title = "Review project spec", Status = TaskStatus.Done,
                Priority = TaskPriority.High, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-2)
            },
            new TaskItem
            {
                UserId = demoUser.Id, Title = "Implement backend API", Description = "Controllers, services, repositories",
                Status = TaskStatus.InProgress, Priority = TaskPriority.High,
                DueDate = now.AddDays(2), CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-1)
            },
            new TaskItem
            {
                UserId = demoUser.Id, Title = "Build Vue frontend", Status = TaskStatus.Todo,
                Priority = TaskPriority.Medium, DueDate = now.AddDays(5),
                CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1)
            },
            new TaskItem
            {
                UserId = demoUser.Id, Title = "Write README", Status = TaskStatus.Todo,
                Priority = TaskPriority.Low, CreatedAt = now, UpdatedAt = now
            }
        );
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoApp API v1"));

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Lightweight health endpoint for Docker healthcheck (Section 9)
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

app.Run();

// Required so WebApplicationFactory<Program> in integration tests can reference this class
public partial class Program { }
