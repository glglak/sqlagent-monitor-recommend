# SQL Server Monitor and Recommend

A .NET 8 application that monitors SQL Server databases for performance issues, automatically reindexes fragmented indexes, and provides AI-powered recommendations for slow queries.

## Features

- **Automatic Index Monitoring**: Detects and fixes fragmented indexes
- **Slow Query Detection**: Identifies poorly performing queries
- **AI-Powered Analysis**: Uses Azure OpenAI or Claude to analyze and recommend optimizations for slow queries
- **Email Notifications**: Sends alerts for critical performance issues
- **REST API**: Provides endpoints for monitoring and management
- **Background Processing**: Runs monitoring tasks in the background

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- Azure OpenAI API key (or Claude API key) for AI-powered recommendations

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/sqlagent-monitor-recommend.git
   cd sqlagent-monitor-recommend
   ```

2. Install required packages:
   ```
   dotnet restore
   ```

3. Update the connection strings in `appsettings.Development.json` to point to your SQL Server instance.

4. Create the required databases:
   ```
   dotnet ef database update
   ```

5. Run the application:
   ```
   dotnet run --environment Development
   ```

### Configuration

The application is configured through `appsettings.json` and environment-specific files like `appsettings.Development.json`. Key configuration sections include:

#### SQL Server Settings

```json
"SqlServer": {
  "ConnectionString": "Server=YourServer;Database=SqlMonitor;Trusted_Connection=True;",
  "MonitoringIntervalMinutes": 30,
  "SlowQueryThresholdMs": 1000,
  "IndexFragmentationThreshold": 30,
  "MonitoredDatabases": [
    {
      "Name": "Database1",
      "ConnectionString": "Server=YourServer;Database=Database1;Trusted_Connection=True;"
    },
    {
      "Name": "Database2",
      "ConnectionString": "Server=YourServer;Database=Database2;Trusted_Connection=True;"
    }
  ]
}
```

#### AI Settings

```json
"AI": {
  "Provider": "AzureOpenAI", // or "Claude"
  "ApiKey": "your-api-key",
  "Endpoint": "https://your-resource-name.openai.azure.com/",
  "DeploymentName": "your-deployment-name",
  "ModelName": "gpt-4",
  "MaxTokens": 1000,
  "Temperature": 0.0
}
```

#### Notification Settings

```json
"Notifications": {
  "Email": {
    "Enabled": true,
    "SmtpServer": "smtp.office365.com",
    "Port": 587,
    "Username": "your-email@your-domain.com",
    "Password": "your-email-password",
    "FromAddress": "sql-monitor@your-domain.com",
    "ToAddresses": ["admin@your-domain.com"]
  },
  "SlowQueryThresholds": {
    "Critical": 5000,
    "Warning": 2000
  }
}
```

## API Endpoints

### Index Monitoring

- `GET /api/indexmonitor` - Get all fragmented indexes
- `POST /api/indexmonitor/{database}/{schema}/{table}/{index}/reindex` - Manually trigger reindexing

### Query Performance

- `GET /api/queryperformance/slow-queries/current` - Get current slow queries
- `GET /api/queryperformance/slow-queries/history?startDate={date}&endDate={date}` - Get historical slow queries
- `POST /api/queryperformance/analyze` - Analyze a specific query with AI

## Architecture

The application follows a clean architecture pattern with the following components:

- **Models**: Data structures representing SQL Server objects and monitoring data
- **Interfaces**: Abstractions for services
- **Services**: Core business logic for monitoring and optimization
- **Background Services**: Long-running tasks for periodic monitoring
- **Controllers**: API endpoints for user interaction
- **Data**: Database context for storing monitoring history

## How It Works

### Index Monitoring

1. The `IndexMonitorBackgroundService` runs at configured intervals
2. It uses `IndexMonitorService` to scan for fragmented indexes
3. When fragmentation exceeds the threshold, it automatically reindexes
4. All operations are logged and can be viewed via the API

### Slow Query Detection

1. The `QueryPerformanceBackgroundService` runs at configured intervals
2. It uses `QueryPerformanceService` to identify slow queries
3. Slow queries are analyzed using AI via `AIQueryAnalysisService`
4. Results are stored in the database and notifications are sent
5. Recommendations can be viewed via the API

## Security Considerations

- Connection strings and API keys should be stored securely
- Use Windows Authentication when possible for SQL Server connections
- Ensure proper access controls for the API endpoints
- Consider using Azure Key Vault or similar for production deployments

## Development

### Adding New Features

1. Create interfaces in the `Interfaces` folder
2. Implement services in the `Services` folder
3. Add API endpoints in the `Controllers` folder
4. Update dependency injection in `Program.cs`

### Testing

Run the included tests:

```dotnet test
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Dapper](https://github.com/DapperLib/Dapper) - Used for database access
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - Used for ORM
- [Azure OpenAI](https://azure.microsoft.com/en-us/services/cognitive-services/openai-service/) - Used for AI-powered query analysis

# SQL Database Setup and Monitoring Guide

Below are the SQL queries needed for setting up monitoring and test databases, adding test data, running DMV queries, and simulating performance issues.

## Database Creation

### SQL Monitor Database

```sql
-- Create SQL Monitoring Database
CREATE DATABASE SQLMonitor;
GO

USE SQLMonitor;
GO

-- Create tables for performance data collection
CREATE TABLE PerformanceMetrics (
    MetricID INT IDENTITY(1,1) PRIMARY KEY,
    CollectionDate DATETIME2 DEFAULT GETDATE(),
    SQLServerName NVARCHAR(128),
    CPUUtilization DECIMAL(5,2),
    MemoryUtilization DECIMAL(5,2),
    DiskIOReadLatency DECIMAL(10,2),
    DiskIOWriteLatency DECIMAL(10,2)
);

-- Create table for query performance data
CREATE TABLE QueryPerformance (
    QueryID INT IDENTITY(1,1) PRIMARY KEY,
    CollectionDate DATETIME2 DEFAULT GETDATE(),
    DatabaseName NVARCHAR(128),
    QueryText NVARCHAR(MAX),
    ExecutionCount BIGINT,
    TotalWorkerTime BIGINT,
    TotalElapsedTime BIGINT,
    TotalLogicalReads BIGINT,
    LastExecutionTime DATETIME2
);
```

### Test Database

```sql
-- Create Test Database
CREATE DATABASE TestDB;
GO

USE TestDB;
GO

-- Create test tables
CREATE TABLE Customers (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50),
    Email NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    CreatedDate DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT FOREIGN KEY REFERENCES Customers(CustomerID),
    OrderDate DATETIME2 DEFAULT GETDATE(),
    TotalAmount DECIMAL(10,2),
    Status NVARCHAR(20)
);

CREATE TABLE OrderItems (
    ItemID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT FOREIGN KEY REFERENCES Orders(OrderID),
    ProductName NVARCHAR(100),
    Quantity INT,
    UnitPrice DECIMAL(10,2)
);
```

## Adding Dummy Data

```sql
USE TestDB;
GO

-- Insert dummy data into Customers table
INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber)
VALUES
    ('John', 'Doe', 'john.doe@example.com', '555-123-4567'),
    ('Jane', 'Smith', 'jane.smith@example.com', '555-987-6543'),
    ('Michael', 'Johnson', 'michael.johnson@example.com', '555-456-7890'),
    ('Emily', 'Brown', 'emily.brown@example.com', '555-321-7654'),
    ('William', 'Davis', 'william.davis@example.com', '555-789-0123');

-- Create a stored procedure to generate a larger amount of test data
CREATE PROCEDURE GenerateDummyData
    @CustomerCount INT = 1000,
    @OrdersPerCustomer INT = 5
AS
BEGIN
    DECLARE @i INT = 1;
    DECLARE @CustomerId INT;
    DECLARE @j INT;
    DECLARE @OrderId INT;
    
    WHILE @i <= @CustomerCount
    BEGIN
        INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber)
        VALUES (
            'FirstName' + CAST(@i AS NVARCHAR(10)),
            'LastName' + CAST(@i AS NVARCHAR(10)),
            'user' + CAST(@i AS NVARCHAR(10)) + '@example.com',
            '555-' + RIGHT('000' + CAST(@i % 1000 AS NVARCHAR(3)), 3) + '-' + RIGHT('0000' + CAST((@i * 7) % 10000 AS NVARCHAR(4)), 4)
        );
        
        SET @CustomerId = SCOPE_IDENTITY();
        SET @j = 1;
        
        WHILE @j <= @OrdersPerCustomer
        BEGIN
            INSERT INTO Orders (CustomerID, OrderDate, TotalAmount, Status)
            VALUES (
                @CustomerId,
                DATEADD(DAY, -(@i * @j % 365), GETDATE()),
                RAND() * 1000,
                CASE WHEN RAND() > 0.7 THEN 'Completed' WHEN RAND() > 0.4 THEN 'Processing' ELSE 'Pending' END
            );
            
            SET @OrderId = SCOPE_IDENTITY();
            
            -- Add 1-5 items to each order
            DECLARE @itemCount INT = FLOOR(RAND() * 5) + 1;
            DECLARE @k INT = 1;
            
            WHILE @k <= @itemCount
            BEGIN
                INSERT INTO OrderItems (OrderID, ProductName, Quantity, UnitPrice)
                VALUES (
                    @OrderId,
                    'Product ' + CAST((@i * 10 + @j * 5 + @k) % 100 AS NVARCHAR(10)),
                    FLOOR(RAND() * 10) + 1,
                    RAND() * 100
                );
                
                SET @k = @k + 1;
            END
            
            SET @j = @j + 1;
        END
        
        SET @i = @i + 1;
    END
END;
GO

-- Generate a larger set of test data (1000 customers with 5 orders each)
EXEC GenerateDummyData;
```

## DMV Queries for Performance Monitoring

### Server-Level Performance Metrics

```sql
-- CPU utilization
SELECT TOP(10) 
    record_id,
    DATEADD(ms, -1 * (SELECT cpu_ticks/(cpu_ticks/ms_ticks) FROM sys.dm_os_sys_info), GETDATE()) AS EventTime, 
    SQLProcessUtilization,
    SystemIdle,
    100 - SystemIdle - SQLProcessUtilization AS OtherProcessUtilization
FROM (
    SELECT 
        record.value('(./Record/@id)[1]', 'int') AS record_id,
        record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int') AS SystemIdle,
        record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') AS SQLProcessUtilization
    FROM (
        SELECT CONVERT(xml, record) AS [record] 
        FROM sys.dm_os_ring_buffers 
        WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
        AND record LIKE '%<SystemHealth>%'
    ) AS x
) AS y
ORDER BY record_id DESC;

-- Memory usage
SELECT
    (total_physical_memory_kb / 1024) AS Total_Physical_Memory_MB,
    (available_physical_memory_kb / 1024) AS Available_Physical_Memory_MB,
    (total_page_file_kb / 1024) AS Total_Page_File_MB,
    (available_page_file_kb / 1024) AS Available_Page_File_MB,
    (system_memory_state_desc) AS System_Memory_State
FROM sys.dm_os_sys_memory;

-- Database file I/O statistics
SELECT
    DB_NAME(vfs.database_id) AS DatabaseName,
    mf.physical_name,
    io_stall_read_ms,
    num_of_reads,
    CASE WHEN num_of_reads = 0 
        THEN 0 
        ELSE (io_stall_read_ms / num_of_reads) 
    END AS avg_read_stall_ms,
    io_stall_write_ms,
    num_of_writes,
    CASE WHEN num_of_writes = 0 
        THEN 0 
        ELSE (io_stall_write_ms / num_of_writes) 
    END AS avg_write_stall_ms,
    io_stall_read_ms + io_stall_write_ms AS io_stalls,
    num_of_reads + num_of_writes AS total_io,
    CASE WHEN (num_of_reads + num_of_writes) = 0 
        THEN 0 
        ELSE (io_stall_read_ms + io_stall_write_ms) / (num_of_reads + num_of_writes) 
    END AS avg_io_stall_ms
FROM 
    sys.dm_io_virtual_file_stats(NULL, NULL) AS vfs
    JOIN sys.master_files AS mf 
        ON vfs.database_id = mf.database_id 
        AND vfs.file_id = mf.file_id
ORDER BY 
    avg_io_stall_ms DESC;
```

### Query Performance Metrics

```sql
-- Top 20 most expensive queries by CPU
SELECT TOP 20
    qs.total_worker_time / qs.execution_count AS avg_cpu_time,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS query_text,
    qs.execution_count,
    qs.total_worker_time,
    qs.total_elapsed_time,
    qs.total_logical_reads,
    qs.last_execution_time,
    qp.query_plan
FROM 
    sys.dm_exec_query_stats AS qs
CROSS APPLY 
    sys.dm_exec_sql_text(qs.sql_handle) AS qt
CROSS APPLY 
    sys.dm_exec_query_plan(qs.plan_handle) AS qp
ORDER BY 
    qs.total_worker_time / qs.execution_count DESC;

-- Queries with missing indexes recommendation
SELECT TOP 20
    CONVERT(DECIMAL(18,2), user_seeks * avg_total_user_cost * (avg_user_impact * 0.01)) AS improvement_measure,
    'CREATE INDEX [IX_' + OBJECT_NAME(mid.object_id) + '_' + 
    STUFF((SELECT '_' + c.name
           FROM sys.columns AS c
           WHERE c.object_id = mid.object_id
           AND c.column_id IN (mid.equality_columns, mid.inequality_columns)
           FOR XML PATH('')), 1, 1, '') + '] ON ' + 
    OBJECT_NAME(mid.object_id) + 
    ' (' + 
    ISNULL(mid.equality_columns, '') + 
    CASE WHEN mid.equality_columns IS NOT NULL AND mid.inequality_columns IS NOT NULL THEN ',' ELSE '' END + 
    ISNULL(mid.inequality_columns, '') + 
    ')' + 
    ISNULL(' INCLUDE (' + mid.included_columns + ')', '') AS create_index_statement,
    migs.user_seeks,
    migs.user_scans,
    migs.last_user_seek,
    mid.object_id,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns
FROM 
    sys.dm_db_missing_index_group_stats AS migs WITH (NOLOCK)
    INNER JOIN sys.dm_db_missing_index_groups AS mig WITH (NOLOCK) ON migs.group_handle = mig.index_group_handle
    INNER JOIN sys.dm_db_missing_index_details AS mid WITH (NOLOCK) ON mig.index_handle = mid.index_handle
WHERE 
    mid.database_id = DB_ID()
ORDER BY 
    improvement_measure DESC;

-- Index usage statistics
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    ius.user_seeks,
    ius.user_scans,
    ius.user_lookups,
    ius.user_updates,
    ius.last_user_seek,
    ius.last_user_scan,
    ius.last_user_lookup,
    ius.last_user_update
FROM 
    sys.indexes AS i
    LEFT JOIN sys.dm_db_index_usage_stats AS ius ON i.object_id = ius.object_id AND i.index_id = ius.index_id
WHERE 
    OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
    AND ius.database_id = DB_ID()
ORDER BY 
    OBJECT_NAME(i.object_id), i.name;
```

## Simulating Performance Issues

### Creating a Slow Index Scan

```sql
USE TestDB;
GO

-- Create a table with many rows but no indexes (except the primary key)
CREATE TABLE LargeTable (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    RandomData CHAR(2000) DEFAULT REPLICATE('X', 2000),
    SearchField1 INT,
    SearchField2 VARCHAR(50),
    SearchField3 DATETIME2
);
GO

-- Populate with a large amount of data
CREATE PROCEDURE PopulateLargeTable
    @RowCount INT = 100000
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @i INT = 1;
    
    WHILE @i <= @RowCount
    BEGIN
        INSERT INTO LargeTable (
            SearchField1, 
            SearchField2, 
            SearchField3
        )
        VALUES (
            ABS(CHECKSUM(NEWID())) % 1000000,
            'Value_' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS VARCHAR(20)),
            DATEADD(SECOND, ABS(CHECKSUM(NEWID())) % 63072000, '2020-01-01')
        );
        
        SET @i = @i + 1;
        
        IF (@i % 10000 = 0)
            PRINT 'Inserted ' + CAST(@i AS VARCHAR(10)) + ' rows';
    END;
END;
GO

-- Execute to populate with 100,000 rows
EXEC PopulateLargeTable;
GO

-- Query to perform a full table scan (slow operation)
-- Run this to see poor performance
SELECT * 
FROM LargeTable 
WHERE SearchField1 BETWEEN 10000 AND 50000;

-- Check execution plan to confirm full table scan
-- Then add an index to compare performance
CREATE INDEX IX_LargeTable_SearchField1 ON LargeTable(SearchField1);

-- Run the same query with index to see improved performance
SELECT * 
FROM LargeTable 
WHERE SearchField1 BETWEEN 10000 AND 50000;

-- Force a table scan by using a function on the indexed column
-- This will bypass the index and force a slow execution
SELECT * 
FROM LargeTable 
WHERE ABS(SearchField1) BETWEEN 10000 AND 50000;
```

### Simulating Blocking and Deadlocks

```sql
USE TestDB;
GO

-- Create tables for deadlock example
CREATE TABLE Account1 (
    AccountID INT PRIMARY KEY,
    Balance DECIMAL(18,2)
);

CREATE TABLE Account2 (
    AccountID INT PRIMARY KEY,
    Balance DECIMAL(18,2)
);

-- Insert initial data
INSERT INTO Account1 VALUES (1, 1000.00);
INSERT INTO Account2 VALUES (1, 2000.00);

-- Run these two transactions in separate query windows to simulate a deadlock

-- Transaction 1 (Run in Window 1)
/*
BEGIN TRANSACTION;
    UPDATE Account1 SET Balance = Balance - 100 WHERE AccountID = 1;
    WAITFOR DELAY '00:00:05'; -- Wait 5 seconds
    UPDATE Account2 SET Balance = Balance + 100 WHERE AccountID = 1;
COMMIT TRANSACTION;
*/

-- Transaction 2 (Run in Window 2)
/*
BEGIN TRANSACTION;
    UPDATE Account2 SET Balance = Balance - 200 WHERE AccountID = 1;
    WAITFOR DELAY '00:00:05'; -- Wait 5 seconds
    UPDATE Account1 SET Balance = Balance + 200 WHERE AccountID = 1;
COMMIT TRANSACTION;
*/

-- To simulate a long-running transaction causing blocking
/*
BEGIN TRANSACTION;
    UPDATE LargeTable SET SearchField2 = 'Updated Value' WHERE ID BETWEEN 1 AND 50000;
    WAITFOR DELAY '00:00:30'; -- Wait 30 seconds
COMMIT TRANSACTION;
*/

-- Run this query in another window to see the blocking information
/*
SELECT 
    spid,
    blocked,
    hostname, 
    program_name,
    cmd,
    login_time,
    last_batch,
    status
FROM 
    sys.sysprocesses
WHERE 
    blocked > 0
    OR spid IN (SELECT blocked FROM sys.sysprocesses WHERE blocked > 0);
*/
```

## Setting Up Regular Performance Data Collection

```sql
USE SQLMonitor;
GO

-- Create procedure to collect and store performance metrics
CREATE PROCEDURE CollectPerformanceMetrics
AS
BEGIN
    -- Insert CPU and Memory metrics
    INSERT INTO PerformanceMetrics (SQLServerName, CPUUtilization, MemoryUtilization, DiskIOReadLatency, DiskIOWriteLatency)
    SELECT
        @@SERVERNAME,
        (SELECT TOP 1 SQLProcessUtilization FROM (
            SELECT 
                record.value('(./Record/@id)[1]', 'int') AS record_id,
                record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') AS SQLProcessUtilization
            FROM (
                SELECT CONVERT(xml, record) AS [record] 
                FROM sys.dm_os_ring_buffers 
                WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
                AND record LIKE '%<SystemHealth>%'
            ) AS x
            ORDER BY record_id DESC
        ) AS y),
        (SELECT
            (total_physical_memory_kb - available_physical_memory_kb) * 100.0 / total_physical_memory_kb
        FROM sys.dm_os_sys_memory),
        (SELECT AVG(CASE WHEN num_of_reads = 0 THEN 0 ELSE (io_stall_read_ms / num_of_reads) END)
        FROM sys.dm_io_virtual_file_stats(NULL, NULL)),
        (SELECT AVG(CASE WHEN num_of_writes = 0 THEN 0 ELSE (io_stall_write_ms / num_of_writes) END)
        FROM sys.dm_io_virtual_file_stats(NULL, NULL));
        
    -- Insert top query performance data
    INSERT INTO QueryPerformance (DatabaseName, QueryText, ExecutionCount, TotalWorkerTime, TotalElapsedTime, TotalLogicalReads, LastExecutionTime)
    SELECT TOP 20
        DB_NAME(qt.dbid),
        SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
            ((CASE qs.statement_end_offset
                WHEN -1 THEN DATALENGTH(qt.text)
                ELSE qs.statement_end_offset
            END - qs.statement_start_offset)/2) + 1),
        qs.execution_count,
        qs.total_worker_time,
        qs.total_elapsed_time,
        qs.total_logical_reads,
        qs.last_execution_time
    FROM 
        sys.dm_exec_query_stats AS qs
    CROSS APPLY 
        sys.dm_exec_sql_text(qs.sql_handle) AS qt
    ORDER BY 
        qs.total_worker_time DESC;
END;
GO

-- Create a SQL Agent job to run this procedure regularly (SQL Server Agent must be running)
/*
USE msdb;
GO

-- Create the job
EXEC dbo.sp_add_job
    @job_name = N'Collect SQL Performance Metrics',
    @description = N'Collects SQL Server performance metrics and stores them in SQLMonitor database',
    @category_name = N'Database Maintenance',
    @owner_login_name = N'sa';

-- Add a job step
EXEC sp_add_jobstep
    @job_name = N'Collect SQL Performance Metrics',
    @step_name = N'Run Collection Procedure',
    @subsystem = N'TSQL',
    @command = N'EXEC SQLMonitor.dbo.CollectPerformanceMetrics',
    @database_name = N'SQLMonitor';

-- Create a schedule to run every 15 minutes
EXEC sp_add_schedule
    @schedule_name = N'Every15Minutes',
    @freq_type = 4, -- Daily
    @freq_interval = 1,
    @freq_subday_type = 4, -- Minutes
    @freq_subday_interval = 15;

-- Attach the schedule to the job
EXEC sp_attach_schedule
    @job_name = N'Collect SQL Performance Metrics',
    @schedule_name = N'Every15Minutes';

-- Enable the job
EXEC sp_update_job
    @job_name = N'Collect SQL Performance Metrics',
    @enabled = 1;
*/
```

## Cleanup

```sql
-- When you're done testing, you can use these commands to clean up

-- Drop the test databases
USE master;
GO

DROP DATABASE IF EXISTS TestDB;
DROP DATABASE IF EXISTS SQLMonitor;
``` 