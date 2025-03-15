import axios from 'axios';

// In a real application, this would connect to your backend API
// For this demo, we'll use mock data and simulate API calls

// Use the proxy configuration from package.json in development
const API_BASE_URL = '/api';

// Helper function to check if response is HTML instead of JSON
const isHtmlResponse = (responseText) => {
  return responseText && typeof responseText === 'string' && 
         (responseText.trim().startsWith('<!DOCTYPE') || 
          responseText.trim().startsWith('<html'));
};

// Configure axios to handle errors better
axios.interceptors.response.use(
  response => response,
  error => {
    // Check if the error response contains HTML
    if (error.response && error.response.data && isHtmlResponse(error.response.data)) {
      console.error('Received HTML instead of JSON. API server might be misconfigured.');
      return Promise.reject(new Error('API server returned HTML instead of JSON. The server might be misconfigured.'));
    }
    return Promise.reject(error);
  }
);

export const fetchDatabases = async () => {
  try {
    console.log('Fetching databases...');
    const response = await axios.get(`${API_BASE_URL}/databases`);
    console.log('Databases response:', response.data);
    
    // Validate response
    if (isHtmlResponse(response.data)) {
      throw new Error('API server returned HTML instead of JSON. The server might be misconfigured.');
    }
    
    return response.data;
  } catch (error) {
    console.error('Error fetching databases:', error);
    throw error;
  }
};

// Add a function to check real-time database status
export const checkDatabaseStatus = async (databaseId) => {
  try {
    console.log(`Checking status for database ${databaseId}...`);
    const response = await axios.get(`${API_BASE_URL}/status/${databaseId}`);
    console.log('Database status response:', response.data);
    
    // Validate response
    if (isHtmlResponse(response.data)) {
      console.error('Received HTML instead of JSON for database status');
      return { status: 'offline' };
    }
    
    return response.data;
  } catch (error) {
    console.error('Error checking database status:', error);
    // If the API call fails, assume the database is offline
    return { status: 'offline' };
  }
};

export const fetchPerformanceMetrics = async (databaseId) => {
  try {
    console.log(`Fetching performance metrics for database ${databaseId}...`);
    // First check if the database is online
    const statusResponse = await checkDatabaseStatus(databaseId);
    if (statusResponse.status === 'offline') {
      return { 
        status: 'offline',
        message: 'Database is offline. No metrics available.',
        timestamps: [],
        cpu: [],
        memory: [],
        diskIO: [],
        networkIO: []
      };
    }
    
    const response = await axios.get(`${API_BASE_URL}/performance/${databaseId}`);
    console.log('Performance metrics response:', response.data);
    
    // Validate response
    if (isHtmlResponse(response.data)) {
      throw new Error('API server returned HTML instead of JSON. The server might be misconfigured.');
    }
    
    return response.data;
  } catch (error) {
    console.error('Error fetching performance metrics:', error);
    throw error;
  }
};

export const fetchSlowQueries = async (databaseId) => {
  try {
    console.log(`Fetching slow queries for database ${databaseId}...`);
    // First check if the database is online
    const statusResponse = await checkDatabaseStatus(databaseId);
    if (statusResponse.status === 'offline') {
      return [];
    }
    
    const response = await axios.get(`${API_BASE_URL}/slowqueries/${databaseId}`);
    console.log('Slow queries response:', response.data);
    
    // Validate response
    if (isHtmlResponse(response.data)) {
      throw new Error('API server returned HTML instead of JSON. The server might be misconfigured.');
    }
    
    return response.data;
  } catch (error) {
    console.error('Error fetching slow queries:', error);
    throw error;
  }
};

export const fetchMissingIndexes = async (databaseId) => {
  try {
    console.log(`Fetching missing indexes for database ${databaseId}...`);
    // First check if the database is online
    const statusResponse = await checkDatabaseStatus(databaseId);
    if (statusResponse.status === 'offline') {
      return [];
    }
    
    const response = await axios.get(`${API_BASE_URL}/missingindexes/${databaseId}`);
    console.log('Missing indexes response:', response.data);
    
    // Validate response
    if (isHtmlResponse(response.data)) {
      throw new Error('API server returned HTML instead of JSON. The server might be misconfigured.');
    }
    
    return response.data;
  } catch (error) {
    console.error('Error fetching missing indexes:', error);
    throw error;
  }
};

export const simulateSlowQuery = async (databaseId) => {
  try {
    console.log(`Simulating slow query for database ${databaseId}...`);
    // First check if the database is online
    const statusResponse = await checkDatabaseStatus(databaseId);
    if (statusResponse.status === 'offline') {
      throw new Error('Database is offline. Cannot simulate query.');
    }
    
    const response = await axios.post(`${API_BASE_URL}/simulate/slowquery`, { databaseId });
    console.log('Slow query simulation response:', response.data);
    
    // Validate response
    if (isHtmlResponse(response.data)) {
      throw new Error('API server returned HTML instead of JSON. The server might be misconfigured.');
    }
    
    // After simulating a slow query, refresh the slow queries list
    await axios.post(`${API_BASE_URL}/refresh/slowqueries`, { databaseId });
    
    return response.data;
  } catch (error) {
    console.error('Error simulating slow query:', error);
    throw error;
  }
};

export const simulateIndexCreation = async (databaseId, indexDetails) => {
  try {
    console.log(`Creating index for database ${databaseId}:`, indexDetails);
    // First check if the database is online
    const statusResponse = await checkDatabaseStatus(databaseId);
    if (statusResponse.status === 'offline') {
      throw new Error('Database is offline. Cannot create index.');
    }
    
    const response = await axios.post(`${API_BASE_URL}/simulate/createindex`, {
      databaseId,
      ...indexDetails
    });
    console.log('Index creation response:', response.data);
    
    // Validate response
    if (isHtmlResponse(response.data)) {
      throw new Error('API server returned HTML instead of JSON. The server might be misconfigured.');
    }
    
    return response.data;
  } catch (error) {
    console.error('Error creating index:', error);
    throw error;
  }
};

export const applyQueryFix = async (queryId, databaseId, fixType) => {
  try {
    console.log(`Applying fix for query ${queryId} in database ${databaseId}: ${fixType}`);
    // First check if the database is online
    const statusResponse = await checkDatabaseStatus(databaseId);
    if (statusResponse.status === 'offline') {
      throw new Error('Database is offline. Cannot apply fix.');
    }
    
    const response = await axios.post(`${API_BASE_URL}/fix/query/${queryId}`, { 
      databaseId, 
      fixType 
    });
    console.log('Apply fix response:', response.data);
    
    // Validate response
    if (isHtmlResponse(response.data)) {
      throw new Error('API server returned HTML instead of JSON. The server might be misconfigured.');
    }
    
    return response.data;
  } catch (error) {
    console.error('Error applying query fix:', error);
    throw error;
  }
}; 