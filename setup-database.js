const sql = require('mssql');
require('dotenv').config();

// Database configuration
const config = {
  server: process.env.DB_SERVER || 'KARIM-DERAZ',
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  options: {
    encrypt: process.env.DB_ENCRYPT === 'true',
    trustServerCertificate: true,
    enableArithAbort: true
  },
  driver: 'tedious'
};

// Add authentication method based on environment variables
if (process.env.USE_WINDOWS_AUTH === 'true') {
  console.log('Using Windows Authentication');
  config.authentication = {
    type: 'ntlm',
    options: {
      domain: process.env.DOMAIN || '',
      userName: process.env.WINDOWS_USERNAME || '',
      password: process.env.WINDOWS_PASSWORD || ''
    }
  };
} else {
  console.log('Using SQL Server Authentication');
  config.user = process.env.DB_USER;
  config.password = process.env.DB_PASSWORD;
}

async function setupDatabase() {
  try {
    console.log('Connecting to SQL Server with config:', JSON.stringify({
      ...config,
      authentication: config.authentication ? { type: config.authentication.type } : undefined,
      password: config.password ? '***' : undefined
    }));
    
    let pool = await sql.connect(config);
    
    // Check if SqlMonitor database exists, create if not
    let result = await pool.request().query(`
      IF NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = 'SqlMonitor')
      BEGIN
        CREATE DATABASE SqlMonitor
        PRINT 'SqlMonitor database created.'
      END
      ELSE
      BEGIN
        PRINT 'SqlMonitor database already exists.'
      END
    `);
    
    console.log('Connecting to SqlMonitor database...');
    // Connect to SqlMonitor database
    await pool.close();
    
    pool = await sql.connect({
      ...config,
      database: 'SqlMonitor'
    });
    
    // Create necessary tables
    await pool.request().query(`
      -- Create tables for performance data collection if they don't exist
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PerformanceMetrics')
      BEGIN
          CREATE TABLE PerformanceMetrics (
              MetricID INT IDENTITY(1,1) PRIMARY KEY,
              CollectionDate DATETIME2 DEFAULT GETDATE(),
              DatabaseName NVARCHAR(128),
              CPUUtilization DECIMAL(5,2),
              MemoryUtilization DECIMAL(5,2),
              DiskIOReadLatency DECIMAL(10,2),
              DiskIOWriteLatency DECIMAL(10,2)
          );
          PRINT 'PerformanceMetrics table created.'
      END
      ELSE
      BEGIN
          PRINT 'PerformanceMetrics table already exists.'
      END

      -- Create table for query performance data if it doesn't exist
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QueryPerformance')
      BEGIN
          CREATE TABLE QueryPerformance (
              QueryID INT IDENTITY(1,1) PRIMARY KEY,
              CollectionDate DATETIME2 DEFAULT GETDATE(),
              DatabaseName NVARCHAR(128),
              QueryText NVARCHAR(MAX),
              ExecutionCount BIGINT,
              TotalWorkerTime BIGINT,
              TotalElapsedTime BIGINT,
              TotalLogicalReads BIGINT,
              LastExecutionTime DATETIME2,
              Fixed BIT DEFAULT 0,
              FixApplied NVARCHAR(255),
              FixDate DATETIME2
          );
          PRINT 'QueryPerformance table created.'
      END
      ELSE
      BEGIN
          PRINT 'QueryPerformance table already exists.'
      END
    `);
    
    // Insert sample data if tables are empty
    await pool.request().query(`
      -- Insert some initial performance data if the table is empty
      IF NOT EXISTS (SELECT TOP 1 1 FROM PerformanceMetrics)
      BEGIN
          -- Sample data for SampleEcommerce
          INSERT INTO PerformanceMetrics (
              CollectionDate, 
              DatabaseName, 
              CPUUtilization, 
              MemoryUtilization, 
              DiskIOReadLatency, 
              DiskIOWriteLatency
          )
          VALUES 
              (DATEADD(HOUR, -1, GETDATE()), 'SampleEcommerce', 45.2, 62.8, 12.5, 8.3),
              (DATEADD(HOUR, -2, GETDATE()), 'SampleEcommerce', 42.7, 60.1, 11.8, 7.9),
              (DATEADD(HOUR, -3, GETDATE()), 'SampleEcommerce', 48.3, 65.4, 13.2, 8.7),
              (DATEADD(HOUR, -4, GETDATE()), 'SampleEcommerce', 51.6, 68.9, 14.1, 9.2),
              (DATEADD(HOUR, -5, GETDATE()), 'SampleEcommerce', 39.8, 58.2, 10.5, 7.1),
              (DATEADD(HOUR, -6, GETDATE()), 'SampleEcommerce', 37.5, 55.6, 9.8, 6.7);

          -- Sample data for SampleHR
          INSERT INTO PerformanceMetrics (
              CollectionDate, 
              DatabaseName, 
              CPUUtilization, 
              MemoryUtilization, 
              DiskIOReadLatency, 
              DiskIOWriteLatency
          )
          VALUES 
              (DATEADD(HOUR, -1, GETDATE()), 'SampleHR', 32.1, 48.5, 8.7, 5.2),
              (DATEADD(HOUR, -2, GETDATE()), 'SampleHR', 30.5, 46.2, 8.1, 4.9),
              (DATEADD(HOUR, -3, GETDATE()), 'SampleHR', 35.8, 52.3, 9.4, 5.8),
              (DATEADD(HOUR, -4, GETDATE()), 'SampleHR', 38.2, 55.7, 10.1, 6.3),
              (DATEADD(HOUR, -5, GETDATE()), 'SampleHR', 28.9, 43.1, 7.5, 4.5),
              (DATEADD(HOUR, -6, GETDATE()), 'SampleHR', 26.4, 40.8, 6.9, 4.1);
          
          PRINT 'Sample performance data inserted.'
      END
      ELSE
      BEGIN
          PRINT 'Performance data already exists.'
      END
    `);
    
    console.log('Database setup completed successfully.');
    await pool.close();
    
  } catch (err) {
    console.error('Error setting up database:', err);
    process.exit(1);
  }
}

setupDatabase().then(() => {
  console.log('Setup complete, continuing with application startup...');
}).catch(err => {
  console.error('Setup failed:', err);
  process.exit(1);
});