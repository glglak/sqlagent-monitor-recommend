const axios = require('axios');
require('dotenv').config();

const API_URL = process.env.REACT_APP_DOTNET_API_URL || 'http://localhost:5000/api';

console.log(`Checking .NET 8 API at: ${API_URL}`);

async function checkApi() {
  try {
    const response = await axios.get(`${API_URL}/databases`);
    console.log('API is accessible! Response:');
    console.log(JSON.stringify(response.data, null, 2));
    return true;
  } catch (error) {
    console.error('Error connecting to API:');
    if (error.code === 'ECONNREFUSED') {
      console.error(`Connection refused to ${API_URL}`);
      console.error('Make sure your .NET 8 API is running and listening on the correct port.');
    } else if (error.response) {
      console.error(`Status: ${error.response.status}`);
      console.error('Response:', error.response.data);
    } else {
      console.error(error.message);
    }
    return false;
  }
}

checkApi().then(isRunning => {
  if (!isRunning) {
    console.log('\nTroubleshooting steps:');
    console.log('1. Make sure your .NET 8 API is running');
    console.log('2. Check that it\'s listening on the correct port (default: 5000)');
    console.log('3. Verify that CORS is enabled in your .NET API to allow requests from your React app');
    console.log('4. Check that the API endpoints are implemented correctly');
    console.log('\nTo run this check again: node check-api.js');
  }
}); 