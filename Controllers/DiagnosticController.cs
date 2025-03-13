using Microsoft.AspNetCore.Mvc;
using SqlMonitor.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;

namespace SqlMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticController : ControllerBase
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<DiagnosticController> _logger;

        public DiagnosticController(
            ISqlConnectionFactory connectionFactory,
            ILogger<DiagnosticController> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        [HttpGet("dmv-test")]
        public async Task<ActionResult> TestDMVs(CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, object>();
            
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                
                // Test 1: Check if any query stats exist
                try
                {
                    var queryStatsCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM sys.dm_exec_query_stats");
                    results.Add("QueryStatsCount", queryStatsCount);
                }
                catch (Exception ex)
                {
                    results.Add("QueryStatsError", ex.Message);
                }
                
                // Test 2: Check if any cached plans exist
                try
                {
                    var cachedPlansCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM sys.dm_exec_cached_plans");
                    results.Add("CachedPlansCount", cachedPlansCount);
                }
                catch (Exception ex)
                {
                    results.Add("CachedPlansError", ex.Message);
                }
                
                // Test 3: Check SQL Server version
                try
                {
                    var versionInfo = await connection.QueryFirstAsync(
                        "SELECT SERVERPROPERTY('ProductVersion') AS ProductVersion, " +
                        "SERVERPROPERTY('ProductLevel') AS ProductLevel, " +
                        "SERVERPROPERTY('Edition') AS Edition");
                    results.Add("SQLServerVersion", versionInfo);
                }
                catch (Exception ex)
                {
                    results.Add("VersionError", ex.Message);
                }
                
                // Test 4: Check permissions
                try
                {
                    await connection.ExecuteAsync("SELECT * FROM sys.dm_exec_query_stats");
                    results.Add("HasDMVPermissions", true);
                }
                catch (Exception ex)
                {
                    results.Add("HasDMVPermissions", false);
                    results.Add("PermissionError", ex.Message);
                }
                
                // Test 5: Run a simple query and check if it's captured
                try
                {
                    // First check if the database exists
                    var dbExists = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM sys.databases WHERE name = 'SampleEcommerce'");
                    
                    if (dbExists > 0)
                    {
                        // Run a simple query that should be captured
                        await connection.ExecuteAsync(
                            "USE SampleEcommerce; SELECT COUNT(*) FROM dbo.Customers WHERE Email LIKE '%example%';");
                        
                        // Check if it was captured
                        var capturedQuery = await connection.QueryFirstOrDefaultAsync(
                            "SELECT TOP 1 qt.text AS QueryText, " +
                            "qs.execution_count AS ExecutionCount " +
                            "FROM sys.dm_exec_query_stats qs " +
                            "CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt " +
                            "WHERE qt.text LIKE '%Customers%' AND qt.text LIKE '%Email%' " +
                            "ORDER BY qs.last_execution_time DESC");
                        
                        results.Add("CapturedSimpleQuery", capturedQuery != null);
                        results.Add("CapturedQueryDetails", capturedQuery);
                    }
                    else
                    {
                        results.Add("SampleEcommerceExists", false);
                    }
                }
                catch (Exception ex)
                {
                    results.Add("SimpleQueryError", ex.Message);
                }
                
                // Test 6: Check if Query Store is enabled
                try
                {
                    var queryStoreStatus = await connection.QueryAsync(
                        "SELECT DB_NAME(database_id) AS DatabaseName, actual_state_desc " +
                        "FROM sys.database_query_store_options " +
                        "WHERE database_id IN (SELECT database_id FROM sys.databases WHERE name = 'SampleEcommerce')");
                    
                    results.Add("QueryStoreStatus", queryStoreStatus);
                }
                catch (Exception ex)
                {
                    results.Add("QueryStoreError", ex.Message);
                }
                
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing DMVs");
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("run-test-query")]
        public async Task<ActionResult> RunTestQuery(CancellationToken cancellationToken)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                
                // Check if the database exists
                var dbExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = 'SampleEcommerce'");
                
                if (dbExists == 0)
                {
                    return NotFound("SampleEcommerce database not found");
                }
                
                // Check if the procedure exists
                var procExists = await connection.ExecuteScalarAsync<int>(
                    "USE SampleEcommerce; SELECT COUNT(*) FROM sys.procedures WHERE name = 'HeavyQueryForDMV'");
                
                if (procExists == 0)
                {
                    // Create the procedure
                    await connection.ExecuteAsync(@"
                        USE SampleEcommerce;
                        
                        CREATE OR ALTER PROCEDURE dbo.HeavyQueryForDMV
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            
                            SELECT 
                                c.CustomerId, c.FirstName, c.LastName, c.Email,
                                o.OrderId, o.OrderDate, o.TotalAmount,
                                oi.Quantity, oi.UnitPrice,
                                p.ProductName, p.Price
                            FROM dbo.Customers c
                            JOIN Sales.Orders o ON c.CustomerId = o.CustomerId
                            JOIN Sales.OrderItems oi ON o.OrderId = oi.OrderId
                            JOIN dbo.Products p ON oi.ProductId = p.ProductId
                            WHERE c.Email LIKE '%example.com%'
                            ORDER BY o.TotalAmount DESC
                            OPTION (RECOMPILE, MAXDOP 1);
                        END;");
                }
                
                // Run the procedure multiple times
                for (int i = 0; i < 5; i++)
                {
                    await connection.ExecuteAsync("USE SampleEcommerce; EXEC dbo.HeavyQueryForDMV;");
                }
                
                // Check if it was captured
                var capturedQuery = await connection.QueryFirstOrDefaultAsync(
                    "SELECT TOP 1 qt.text AS QueryText, " +
                    "qs.execution_count AS ExecutionCount, " +
                    "qs.total_elapsed_time / qs.execution_count / 1000.0 AS AvgDurationMs " +
                    "FROM sys.dm_exec_query_stats qs " +
                    "CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt " +
                    "WHERE qt.text LIKE '%HeavyQueryForDMV%' OR qt.text LIKE '%Customers%' " +
                    "ORDER BY qs.last_execution_time DESC");
                
                if (capturedQuery != null)
                {
                    return Ok(new { 
                        Message = "Test query executed and captured in DMVs", 
                        QueryDetails = capturedQuery 
                    });
                }
                else
                {
                    return Ok(new { 
                        Message = "Test query executed but not captured in DMVs. This indicates an issue with DMV monitoring." 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running test query");
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }
    }
} 