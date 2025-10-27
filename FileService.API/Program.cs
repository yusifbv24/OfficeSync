using FileService.Application.Behaviors;
using FileService.Application.DTOs;
using FileService.Application.Interfaces;
using FileService.Application.Mappings;
using FileService.Infrastructure.Clients;
using FileService.Infrastructure.Configurations;
using FileService.Infrastructure.Data;
using FileService.Infrastructure.Repositories;
using FileService.Infrastructure.Services;
using FluentValidation;
using MediatR.NotificationPublishers;
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
        "logs/file-service-.txt",
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
        // Use camelCase for JSON property names (JavaScript convention)
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        // Allow case-insensitive property matching when deserializing
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;

        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });


    builder.Services.AddDbContext<FileServiceDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("FileService.Infrastructure")));


    builder.Services.AddEndpointsApiExplorer();


    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "File Service API",
            Version = "v1",
            Description = "Microservice for managing files and its' accesses",
            Contact = new OpenApiContact
            {
                Name = "File Service Team",
                Email = "fileservice@company.local"
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


    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
    };
});

    builder.Services.AddAuthorization();



    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(
            Assembly.Load("FileService.Application"));

        cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher);
    });


    builder.Services.AddValidatorsFromAssemblyContaining<Program>();


    builder.Services.AddHealthChecks()
        .AddDbContextCheck<FileServiceDbContext>("database");


    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddProfile<MappingProfile>();
    }, typeof(MappingProfile).Assembly);


    builder.Services.AddScoped<IFileRepository, FileRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    builder.Services.AddScoped<IThumbnailService, ThumbnailService>();
    builder.Services.AddScoped<IHashService, HashService>();
    builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>();
    builder.Services.AddHttpClient<IChannelServiceClient, ChannelServiceClient>();

    builder.Services.Configure<FileStorageOptions>(
        builder.Configuration.GetSection("FileStorage"));

    builder.Services.Configure<ServiceEndpoints>(
        builder.Configuration.GetSection("ServiceEndpoints"));


    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();


    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<FileServiceDbContext>();
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied succesfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while checking migrations");
            throw;
        }
    }


    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "File Service API V1");
            options.RoutePrefix = string.Empty;
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

    Log.Information("Starting File Service on {Environment}", app.Environment.EnvironmentName);
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