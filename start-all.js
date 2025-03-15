const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');
const readline = require('readline');
const os = require('os');
const net = require('net');
const axios = require('axios');
require('dotenv').config();

// Colors for console output
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  dim: '\x1b[2m',
  underscore: '\x1b[4m',
  blink: '\x1b[5m',
  reverse: '\x1b[7m',
  hidden: '\x1b[8m',
  
  black: '\x1b[30m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  magenta: '\x1b[35m',
  cyan: '\x1b[36m',
  white: '\x1b[37m',
  
  bgBlack: '\x1b[40m',
  bgRed: '\x1b[41m',
  bgGreen: '\x1b[42m',
  bgYellow: '\x1b[43m',
  bgBlue: '\x1b[44m',
  bgMagenta: '\x1b[45m',
  bgCyan: '\x1b[46m',
  bgWhite: '\x1b[47m'
};

// Function to find an available port
function findAvailablePort(startPort, callback) {
  const server = net.createServer();
  server.listen(startPort, () => {
    const port = server.address().port;
    server.close(() => callback(port));
  });
  
  server.on('error', () => {
    // Port is in use, try the next one
    findAvailablePort(startPort + 1, callback);
  });
}

// Kill processes on specific ports (Windows only)
function killProcessOnPort(port) {
  if (process.platform === 'win32') {
    try {
      console.log(`${colors.yellow}Attempting to kill process on port ${port}...${colors.reset}`);
      const { execSync } = require('child_process');
      execSync(`for /f "tokens=5" %a in ('netstat -ano ^| find ":${port}" ^| find "LISTENING"') do taskkill /F /PID %a`);
      console.log(`${colors.green}Successfully killed process on port ${port}${colors.reset}`);
    } catch (error) {
      console.log(`${colors.green}No process found on port ${port} or failed to kill: ${error.message}${colors.reset}`);
    }
  }
}

// Try to kill processes on common ports
killProcessOnPort(3000);

// Configuration
const config = {
  react: {
    command: process.platform === 'win32' ? 'npm.cmd' : 'npm',
    args: ['start'],
    name: 'React App',
    color: colors.blue,
    env: {
      ...process.env,
      PORT: '0'  // Will be set dynamically
    }
  }
};

// Keep track of running processes
const processes = {};

// Function to start a process
function startProcess(name, command, args, color, env) {
  console.log(`${color}Starting ${name}...${colors.reset}`);
  
  const proc = spawn(command, args, {
    stdio: 'pipe',
    shell: true,
    env: env || process.env
  });
  
  processes[name] = proc;
  
  // Handle stdout
  proc.stdout.on('data', (data) => {
    const lines = data.toString().trim().split('\n');
    lines.forEach(line => {
      console.log(`${color}[${name}] ${line}${colors.reset}`);
      
      // Look for the port in React's output
      if (name === 'React App' && line.includes('Local:')) {
        const match = line.match(/http:\/\/localhost:(\d+)/);
        if (match && match[1]) {
          const port = match[1];
          console.log(`${colors.cyan}${colors.bright}React App is running on port ${port}${colors.reset}`);
          console.log(`${colors.cyan}React App: http://localhost:${port}${colors.reset}`);
        }
      }
    });
  });
  
  // Handle stderr
  proc.stderr.on('data', (data) => {
    const lines = data.toString().trim().split('\n');
    lines.forEach(line => {
      console.log(`${color}[${name}] ${colors.dim}${line}${colors.reset}`);
    });
  });
  
  // Handle process exit
  proc.on('close', (code) => {
    if (code !== 0) {
      console.log(`${colors.red}[${name}] Process exited with code ${code}${colors.reset}`);
    } else {
      console.log(`${color}[${name}] Process exited normally${colors.reset}`);
    }
    
    delete processes[name];
    
    // If all processes have exited, exit the script
    if (Object.keys(processes).length === 0) {
      console.log(`${colors.yellow}All processes have exited. Shutting down.${colors.reset}`);
      process.exit(0);
    }
  });
  
  proc.on('error', (err) => {
    console.log(`${colors.red}[${name}] Failed to start process: ${err.message}${colors.reset}`);
    delete processes[name];
  });
  
  return proc;
}

// Function to stop all processes
function stopAll() {
  console.log(`${colors.yellow}Stopping all processes...${colors.reset}`);
  
  Object.entries(processes).forEach(([name, proc]) => {
    if (proc && !proc.killed) {
      console.log(`${colors.yellow}Stopping ${name}...${colors.reset}`);
      
      if (process.platform === 'win32') {
        // On Windows, we need to use taskkill to kill the process tree
        spawn('taskkill', ['/pid', proc.pid, '/f', '/t']);
      } else {
        // On Unix-like systems, we can just kill the process group
        proc.kill('SIGINT');
      }
    }
  });
}

// Setup readline interface for user input
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

// Handle user input
rl.on('line', (input) => {
  if (input.toLowerCase() === 'exit' || input.toLowerCase() === 'quit' || input.toLowerCase() === 'q') {
    console.log(`${colors.yellow}User requested exit. Stopping all processes...${colors.reset}`);
    stopAll();
    setTimeout(() => {
      process.exit(0);
    }, 1000);
  }
});

// Handle process exit
process.on('exit', () => {
  stopAll();
});

// Handle Ctrl+C
process.on('SIGINT', () => {
  console.log(`${colors.yellow}Received SIGINT. Stopping all processes...${colors.reset}`);
  stopAll();
  setTimeout(() => {
    process.exit(0);
  }, 1000);
});

// Handle uncaught exceptions
process.on('uncaughtException', (err) => {
  console.log(`${colors.red}Uncaught exception: ${err.message}${colors.reset}`);
  stopAll();
  setTimeout(() => {
    process.exit(1);
  }, 1000);
});

// Function to check if the .NET 8 API is running
async function checkDotNetApi() {
  const API_URL = process.env.REACT_APP_DOTNET_API_URL || 'http://localhost:5000/api';
  
  console.log(`${colors.yellow}Checking if .NET 8 API is running at ${API_URL}...${colors.reset}`);
  
  try {
    await axios.get(`${API_URL}/databases`);
    console.log(`${colors.green}.NET 8 API is running and accessible!${colors.reset}`);
    return true;
  } catch (error) {
    console.log(`${colors.red}Error connecting to .NET 8 API:${colors.reset}`);
    
    if (error.code === 'ECONNREFUSED') {
      console.log(`${colors.red}Connection refused to ${API_URL}${colors.reset}`);
      console.log(`${colors.yellow}Make sure your .NET 8 API is running and listening on the correct port.${colors.reset}`);
    } else if (error.response) {
      console.log(`${colors.red}Status: ${error.response.status}${colors.reset}`);
      console.log(`${colors.red}Response: ${JSON.stringify(error.response.data)}${colors.reset}`);
    } else {
      console.log(`${colors.red}${error.message}${colors.reset}`);
    }
    
    return false;
  }
}

// Print welcome message
console.log(`${colors.cyan}${colors.bright}SQL Performance Monitor & Recommend${colors.reset}`);

// Main function to start the application
async function startApp() {
  // Check if the .NET 8 API is running
  const apiRunning = await checkDotNetApi();
  
  if (!apiRunning) {
    console.log(`${colors.yellow}Warning: .NET 8 API is not accessible.${colors.reset}`);
    console.log(`${colors.yellow}The React app will start, but it may not function correctly.${colors.reset}`);
    console.log(`${colors.yellow}Please make sure your .NET 8 API is running at ${process.env.REACT_APP_DOTNET_API_URL || 'http://localhost:5000/api'}${colors.reset}`);
    
    // Ask the user if they want to continue
    rl.question(`${colors.yellow}Do you want to continue anyway? (y/n) ${colors.reset}`, (answer) => {
      if (answer.toLowerCase() !== 'y' && answer.toLowerCase() !== 'yes') {
        console.log(`${colors.yellow}Exiting...${colors.reset}`);
        process.exit(0);
      }
      
      startReactApp();
    });
  } else {
    startReactApp();
  }
}

// Function to start the React app
function startReactApp() {
  console.log(`${colors.cyan}Starting React frontend...${colors.reset}`);
  console.log(`${colors.yellow}Press Ctrl+C or type 'exit' to stop all processes${colors.reset}`);
  
  // Find an available port for React
  findAvailablePort(3000, (port) => {
    console.log(`${colors.cyan}Found available port for React: ${port}${colors.reset}`);
    
    // Set the port in the environment
    config.react.env.PORT = port.toString();
    
    // Start React app
    startProcess(
      config.react.name,
      config.react.command,
      config.react.args,
      config.react.color,
      config.react.env
    );
    
    console.log(`${colors.cyan}${colors.bright}React frontend started!${colors.reset}`);
    console.log(`${colors.yellow}Type 'exit' to stop all processes${colors.reset}`);
  });
}

// Start the application
startApp(); 