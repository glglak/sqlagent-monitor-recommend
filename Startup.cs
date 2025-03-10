using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlMonitor.Services;
using SqlMonitor.BackgroundServices;
using SqlMonitor.Models;

namespace SqlMonitor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add configuration
            services.Configure<SqlServerSettings>(Configuration.GetSection("SqlServer"));
            services.Configure<AISettings>(Configuration.GetSection("AI"));
            
            // Register services
            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<IIndexMonitorService, IndexMonitorService>();
            services.AddScoped<IQueryPerformanceService, QueryPerformanceService>();
            services.AddScoped<IAIQueryAnalysisService, AIQueryAnalysisService>();
            
            // Register HTTP client for AI service
            services.AddHttpClient<IAIQueryAnalysisService, AIQueryAnalysisService>();
            
            // Register background services
            services.AddHostedService<IndexMonitorBackgroundService>();
            services.AddHostedService<QueryPerformanceBackgroundService>();
            
            // Add controllers and API endpoints
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}


