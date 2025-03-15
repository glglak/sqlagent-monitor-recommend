# SQL Performance Monitor & Recommend

A comprehensive tool for monitoring SQL Server performance and getting optimization recommendations using real-time data from your SQL Server instances.

## Architecture

This application follows a three-tier architecture:

1. **Frontend**: React.js application that provides the user interface
2. **Backend API**: .NET 8 API that handles business logic and data access
3. **Database**: SQL Server instances that store the data

The React frontend communicates with the .NET 8 API, which in turn connects to SQL Server and Azure OpenAI for query optimization.

## Features

- Real-time database status monitoring
- Live performance metrics visualization
- Actual slow query analysis from your databases
- Index optimization recommendations based on real usage patterns
- Performance simulation tools with actual query execution
- **AI-Powered Query Optimization** using Azure OpenAI

## Current Implementation Status

The application connects to your actual SQL Server instances through the .NET 8 API and retrieves real performance data:

- Lists all user databases from your SQL Server
- Shows real-time status (online/offline) of each database
- Displays actual CPU, memory, disk I/O, and network metrics
- Identifies slow queries using Query Store or DMVs
- Recommends missing indexes based on actual query patterns
- Uses Azure OpenAI to analyze and optimize slow queries

## Prerequisites

- Node.js (v14 or higher) for the React frontend
- .NET 8 SDK for the backend API
- SQL Server instance (2016 or newer recommended for Query Store features)
- Windows Authentication or SQL Server Authentication credentials with appropriate permissions
- Azure OpenAI API access (for AI-powered query optimization)

## Required SQL Server Permissions

The SQL login used by the application needs the following permissions:
- VIEW SERVER STATE
- VIEW DATABASE STATE for each database you want to monitor
- For Query Store features: VIEW DATABASE STATE on the specific databases

## Setup

### Frontend Setup

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/sql-monitor-recommend.git
   cd sql-monitor-recommend
   ```

2. Install dependencies:
   ```
   npm install
   ```

3. Create a `.env` file in the root directory with your .NET API endpoint:
   ```
   REACT_APP_DOTNET_API_URL=http://localhost:5000/api
   ```

### Backend Setup

1. Configure your .NET 8 API with the following settings in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "SqlServer": "Server=your_server_name;Integrated Security=true;TrustServerCertificate=true"
     },
     "AzureOpenAI": {
       "Endpoint": "https://your-resource-name.openai.azure.com",
       "ApiKey": "your-azure-openai-api-key",
       "Deployment": "gpt-4",
       "ApiVersion": "2023-05-15"
     },
     "Cors": {
       "AllowedOrigins": ["http://localhost:3000"]
     }
   }
   ```

## Running the Application

### Option 1: Run Both Services with a Single Command

The easiest way to run both the .NET 8 API and React frontend is with a single command:

```
npm run run-all
```

This will:
1. Kill any processes using ports 3000 and 5000
2. Start your .NET 8 API on port 5000
3. Start the React app on an available port (usually 3000)
4. Display the URLs for both services

### Option 2: Run Services Separately

#### Start the .NET 8 Backend First

Start your .NET 8 API using your preferred method:

```
dotnet run --project YourDotNetApiProject.csproj
```

Make sure it's running on the URL specified in your frontend's `.env` file (default: http://localhost:5000/api).

#### Then Start the React Frontend

```
npm start
```

### Checking API Connection

To check if your .NET 8 API is running and accessible:

```
npm run check-api
```

## Development

- React frontend: http://localhost:3000
- .NET 8 API: http://localhost:5000/api

## How It Works

The application follows this flow:

1. **Frontend**: React components request data from the .NET 8 API
2. **Backend API**: 
   - Connects to SQL Server to retrieve database information
   - Uses DMVs to collect performance metrics
   - Analyzes slow queries and missing indexes
   - Communicates with Azure OpenAI for query optimization
3. **Database**: SQL Server provides the actual performance data

## API Endpoints

Your .NET 8 API should implement these endpoints:

- `GET /api/databases` - Get all databases
- `GET /api/databases/{databaseId}/status` - Check database status
- `GET /api/databases/{databaseId}/performance` - Get performance metrics
- `GET /api/databases/{databaseId}/slowqueries` - Get slow queries
- `GET /api/databases/{databaseId}/missingindexes` - Get missing indexes
- `POST /api/databases/{databaseId}/simulate/slowquery` - Simulate a slow query
- `POST /api/databases/{databaseId}/simulate/createindex` - Simulate index creation
- `POST /api/databases/{databaseId}/fix/query/{queryId}` - Apply fix to a slow query
- `POST /api/openai/optimize-query` - Optimize a query using Azure OpenAI

## Troubleshooting

### API Connection Issues

If the frontend can't connect to the .NET 8 API:
1. Check that the API is running
2. Verify the API URL in the `.env` file
3. Check that CORS is properly configured in the API
4. Look for errors in the browser console
5. Run `npm run check-api` to diagnose connection issues

### SQL Server Connection Issues

If the .NET API can't connect to SQL Server:
1. Verify your SQL Server is running and accessible
2. Check the connection details in your API's configuration
3. Ensure the SQL login has appropriate permissions
4. Check for firewall restrictions

### Azure OpenAI Connection Issues

If the AI-powered query optimization isn't working:
1. Verify your Azure OpenAI credentials in the API's configuration
2. Check that your Azure OpenAI deployment is active
3. Ensure you have sufficient quota and credits in your Azure account
4. Check the API logs for specific error messages

### Dashboard Metrics Not Showing

If the utilization percentages or metrics are not displaying correctly:
1. Check the browser console for any errors
2. Verify that your SQL Server version supports the DMVs being queried
3. Ensure the SQL login has VIEW SERVER STATE permission

## License

MIT 