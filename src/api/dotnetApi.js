import axios from 'axios';

// Configure the base URL for the .NET 8 API
const API_BASE_URL = process.env.REACT_APP_DOTNET_API_URL || 'http://localhost:5000/api';

console.log('Connecting to .NET 8 API at:', API_BASE_URL);

// Create an axios instance with default config
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  },
  timeout: 70000 // Increase to 70 seconds to account for rate limit retries
});

// Add request interceptor to log timeouts
apiClient.interceptors.request.use(
  config => {
    console.log(`Making request to ${config.url} with timeout ${config.timeout}ms`);
    return config;
  },
  error => {
    return Promise.reject(error);
  }
);

// Helper function to check if response is HTML instead of JSON
const isHtmlResponse = (responseText) => {
  return responseText && typeof responseText === 'string' && 
         (responseText.trim().startsWith('<!DOCTYPE') || 
          responseText.trim().startsWith('<html'));
};

// Helper function to extract wait time from error message
const extractWaitTime = (message) => {
  const match = message.match(/retry after (\d+) seconds/);
  return match ? parseInt(match[1]) : 60;
};

// Configure axios to handle errors better
apiClient.interceptors.response.use(
  response => response,
  error => {
    // Check if the error response contains HTML
    if (error.response && error.response.data && isHtmlResponse(error.response.data)) {
      console.error('Received HTML instead of JSON. .NET API server might be misconfigured or not running.');
      return Promise.reject(new Error('.NET API server returned HTML instead of JSON. The server might be misconfigured or not running.'));
    }
    
    // Handle rate limit errors specially
    if (error.response?.status === 429) {
      const waitTime = extractWaitTime(error.response.data?.error?.message || '');
      const errorMessage = `Azure OpenAI rate limit exceeded. Please wait ${waitTime} seconds before trying again.`;
      console.error(errorMessage);
      return Promise.reject(new Error(errorMessage));
    }
    
    // Log detailed error information
    if (error.code === 'ECONNREFUSED') {
      console.error(`Connection refused to ${error.config?.url}. Is the .NET 8 API running?`);
    } else if (error.response) {
      console.error(`API request failed with status ${error.response.status}:`, error.response.data);
    } else if (error.request) {
      console.error('No response received from API:', error.request);
    } else {
      console.error('API request setup error:', error.message);
    }
    
    console.error('Full request URL:', error.config?.baseURL + error.config?.url);
    return Promise.reject(error);
  }
);

// Database operations - all of these just pass through to the .NET 8 API
export const fetchDatabases = async () => {
  try {
    console.log(`Fetching databases from .NET 8 API: ${API_BASE_URL}/databases`);
    const response = await apiClient.get('/databases');
    console.log('Databases response:', response.data);
    return response.data;
  } catch (error) {
    console.error('Error fetching databases from .NET 8 API:', error);
    throw error;
  }
};

export const checkDatabaseStatus = async (databaseId) => {
  try {
    console.log(`Checking status for database ${databaseId} via .NET 8 API`);
    const response = await apiClient.get(`/databases/${databaseId}/status`);
    console.log('Database status response:', response.data);
    return response.data;
  } catch (error) {
    console.error('Error checking database status via .NET 8 API:', error);
    // If the API call fails, assume the database is offline
    return { status: 'offline' };
  }
};

export const fetchPerformanceMetrics = async (databaseId) => {
  try {
    console.log(`Fetching performance metrics for database ${databaseId} via .NET 8 API`);
    const response = await apiClient.get(`/databases/${databaseId}/performance`);
    console.log('Performance metrics response:', response.data);
    return response.data;
  } catch (error) {
    console.error('Error fetching performance metrics via .NET 8 API:', error);
    throw error;
  }
};

export const fetchSlowQueries = async (databaseId) => {
  try {
    console.log(`Fetching slow queries for database ${databaseId} via .NET 8 API`);
    const response = await apiClient.get(`/databases/${databaseId}/slowqueries`);
    console.log('Slow queries response:', response.data);
    return response.data;
  } catch (error) {
    console.error('Error fetching slow queries via .NET 8 API:', error);
    throw error;
  }
};

export const fetchMissingIndexes = async (databaseId) => {
  try {
    console.log(`Fetching missing indexes for database ${databaseId} via .NET 8 API`);
    const response = await apiClient.get(`/databases/${databaseId}/missingindexes`);
    console.log('Missing indexes response:', response.data);
    return response.data;
  } catch (error) {
    console.error('Error fetching missing indexes via .NET 8 API:', error);
    throw error;
  }
};

export const simulateSlowQuery = async (databaseId) => {
  try {
    console.log('Simulating slow query for database', databaseId, 'via .NET 8 API');
    const response = await apiClient.post(`/databases/${databaseId}/simulate/slowquery`);
    console.log('Slow query simulation response:', response.data);
    return response.data;
  } catch (error) {
    console.error('Error simulating slow query via .NET 8 API:', error);
    throw error;
  }
};