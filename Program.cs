using Microsoft.EntityFrameworkCore;
using SqlMonitor.Data;
using SqlMonitor.Interfaces;
using SqlMonitor.Services;
using SqlMonitor.BackgroundServices;
using SqlMonitor.Models;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

// Create the WebApplication builder
var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// Add file logging if needed
var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
if (!Directory.Exists(logPath))
{
    Directory.CreateDirectory(logPath);
}

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:3002",
                "http://localhost:3003")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization if needed
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer(); // This is required for Swagger

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SQL Server Monitor and Recommend API",
        Version = "v1",
        Description = "API for monitoring SQL Server performance and providing optimization recommendations",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });
    
    // Include XML comments if you have them
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure settings
builder.Services.Configure<SqlServerSettings>(builder.Configuration.GetSection("SqlServer"));
builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AI"));

// Add database context
builder.Services.AddDbContext<SqlMonitorContext>(options =>
    options.UseSqlServer(builder.Configuration.GetSection("SqlServer:ConnectionString").Value));

// Register required services explicitly
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IIndexMonitorService, IndexMonitorService>();
builder.Services.AddScoped<IQueryPerformanceService, QueryPerformanceService>();
builder.Services.AddScoped<IAIQueryAnalysisService, AIQueryAnalysisService>();

// Email notification service has been removed
// builder.Services.AddScoped<INotificationService, EmailNotificationService>();

builder.Services.AddHttpClient<IAIQueryAnalysisService, AIQueryAnalysisService>();

// Hosted services with fully qualified names to avoid ambiguity
builder.Services.AddHostedService<SqlMonitor.BackgroundServices.IndexMonitorBackgroundService>();
builder.Services.AddHostedService<SqlMonitor.BackgroundServices.QueryPerformanceBackgroundService>();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SQL Monitor API v1"));
}

// Apply CORS before other middleware
app.UseCors("ReactApp");

// Comment out HTTPS redirection for local development if needed
// app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

// Map controllers directly instead of using UseEndpoints
app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/api/health", async (context) =>
{
    await context.Response.WriteAsJsonAsync(new { status = "healthy", message = "API is running" });
});

// Set the URLs explicitly
app.Urls.Add("http://localhost:5000");
// Comment out HTTPS URL to avoid certificate issues
// app.Urls.Add("https://localhost:5001");

// Run the application
app.Run();
