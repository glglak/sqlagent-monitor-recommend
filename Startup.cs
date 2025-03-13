using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlMonitor.Services;
using SqlMonitor.BackgroundServices;
using SqlMonitor.Models;
using SqlMonitor.Interfaces;
using SqlMonitor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.IO;
using System.Reflection;

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
            
            // Add database context
            services.AddDbContext<SqlMonitorContext>(options =>
                options.UseSqlServer(Configuration.GetSection("SqlServer:ConnectionString").Value));
            
            // Register services
            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<IIndexMonitorService, IndexMonitorService>();
            services.AddScoped<IQueryPerformanceService, QueryPerformanceService>();
            services.AddScoped<IAIQueryAnalysisService, AIQueryAnalysisService>();
            services.AddScoped<INotificationService, EmailNotificationService>();
            
            // Register HTTP client for AI service
            services.AddHttpClient<IAIQueryAnalysisService, AIQueryAnalysisService>();
            
            // Register background services
            services.AddHostedService<IndexMonitorBackgroundService>();
            services.AddHostedService<QueryPerformanceBackgroundService>();
            
            // Add controllers and API endpoints
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            
            // Add Swagger with more detailed configuration
            services.AddSwaggerGen(c =>
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
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
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


