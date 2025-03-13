using Microsoft.EntityFrameworkCore;
using SqlMonitor.Data;
using SqlMonitor.Interfaces;
using SqlMonitor.Services;
using SqlMonitor.BackgroundServices;
using SqlMonitor.Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // This is required for Swagger
builder.Services.AddSwaggerGen();

// Configure settings
builder.Services.Configure<SqlServerSettings>(builder.Configuration.GetSection("SqlServer"));
builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AI"));

// Add database context
builder.Services.AddDbContext<SqlMonitorContext>(options =>
    options.UseSqlServer(builder.Configuration.GetSection("SqlServer:ConnectionString").Value));

// Register services in the correct order to avoid circular dependencies
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

// Register HttpClient for AIQueryAnalysisService first
builder.Services.AddHttpClient<AIQueryAnalysisService>();
builder.Services.AddScoped<IAIQueryAnalysisService, AIQueryAnalysisService>();

// Then register services that depend on IAIQueryAnalysisService
builder.Services.AddScoped<IIndexMonitorService, IndexMonitorService>();
builder.Services.AddScoped<IQueryPerformanceService, QueryPerformanceService>();
builder.Services.AddScoped<INotificationService, EmailNotificationService>();

// Register background services
builder.Services.AddHostedService<IndexMonitorBackgroundService>();
builder.Services.AddHostedService<QueryPerformanceBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
