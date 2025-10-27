using FluentValidation;
using MediatR;
using MessagingService.Application.Behaviors;
using MessagingService.Application.Interfaces;
using MessagingService.Application.Mappings;
using MessagingService.Infrastructure.Data;
using MessagingService.Infrastructure.HttpClients;
using MessagingService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/messaging-service-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Serialize enums as strings for readability
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter());
        });

    var connectionString = builder.Configuration.GetConnectionString("PostgreSql")
        ?? throw new InvalidOperationException("PostgreSQL connection string not found");

    builder.Services.AddDbContext<MessagingDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("MessagingService.Infrastructure");
        });
    });

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Messaging Service API",
            Version = "v1",
            Description = "Microservice for managing messages, reactions, and attachments",
            Contact = new OpenApiContact
            {
                Name = "Messaging Service Team",
                Email = "messagingservice@company.local"
            }
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
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
                Array.Empty<string>()
            }
        });
    });

    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSettings["SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey is not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(
            Assembly.Load("MessagingService.Application"));
    });

    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    builder.Services.AddValidatorsFromAssembly(
        Assembly.Load("MessagingService.Application"));

    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddProfile<MessageMappingProfile>();
    }, typeof(MessageMappingProfile).Assembly);


    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient<IChannelServiceClient, ChannelServiceClient>();
    builder.Services.AddHttpClient<IFileServiceClient, FileServiceClient>();
    builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<MessagingDbContext>("database");

    var app = builder.Build();


    using(var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<MessagingDbContext>();
            var logger=services.GetRequiredService<ILogger<Program>>();
            await DbInitializer.InitializeAsync(context, logger);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occured while initializing the database");
            throw;
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Messaging Service API v1");
            options.RoutePrefix = string.Empty; // Swagger at root URL
        });
    }

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Starting Messaging Service on {Environment}", app.Environment.EnvironmentName);
    Log.Information("Database: PostgreSQL");
    Log.Information("Service is listening on: {Urls}", string.Join(", ", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}