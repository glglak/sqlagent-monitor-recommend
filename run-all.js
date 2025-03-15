const { spawn } = require('child_process');
const path = require('path');
const readline = require('readline');
const net = require('net');
const fs = require('fs');
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
killProcessOnPort(5000);

// Configuration
const config = {
  dotnet: {
    command: process.platform === 'win32' ? 'dotnet.exe' : 'dotnet',
    args: ['run', '--project', './sqlagent-monitor-recommend/sqlagent-monitor-recommend.csproj'], // Update this path to your actual .NET project
    name: '.NET 8 API',
    color: colors.green,
    env: {
      ...process.env,
      ASPNETCORE_URLS: 'http://localhost:5000'
    }
  },
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
      
      // Look for the .NET API startup message
      if (name === '.NET 8 API' && line.includes('Now listening on:')) {
        console.log(`${colors.green}${colors.bright}.NET 8 API is running!${colors.reset}`);
        console.log(`${colors.green}.NET 8 API: http://localhost:5000${colors.reset}`);
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

// Print welcome message
console.log(`${colors.cyan}${colors.bright}SQL Performance Monitor & Recommend${colors.reset}`);
console.log(`${colors.cyan}Starting both .NET 8 API and React frontend...${colors.reset}`);
console.log(`${colors.yellow}Press Ctrl+C or type 'exit' to stop all processes${colors.reset}`);

// Function to start the application
function startApp() {
  // Start .NET 8 API first
  startProcess(
    config.dotnet.name,
    config.dotnet.command,
    config.dotnet.args,
    config.dotnet.color,
    config.dotnet.env
  );
  
  // Wait a bit for the .NET API to start before launching React
  console.log(`${colors.yellow}Waiting for .NET 8 API to start...${colors.reset}`);
  setTimeout(() => {
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
    });
  }, 5000); // Wait 5 seconds for .NET to start
}

// Check if the .NET project path exists and update if needed
function findDotNetProject() {
  // Default project path from config
  let projectPath = config.dotnet.args[2];
  
  // Check if the path exists
  if (!fs.existsSync(projectPath)) {
    console.log(`${colors.yellow}Could not find .NET project at ${projectPath}${colors.reset}`);
    console.log(`${colors.yellow}Searching for .NET projects...${colors.reset}`);
    
    // Try to find .csproj files
    const findProjects = () => {
      const projects = [];
      
      // Function to recursively search for .csproj files
      const searchDir = (dir, depth = 0) => {
        if (depth > 3) return; // Limit search depth
        
        try {
          const files = fs.readdirSync(dir);
          
          for (const file of files) {
            const filePath = path.join(dir, file);
            const stat = fs.statSync(filePath);
            
            if (stat.isDirectory()) {
              searchDir(filePath, depth + 1);
            } else if (file.endsWith('.csproj')) {
              projects.push(filePath);
            }
          }
        } catch (err) {
          // Ignore permission errors
        }
      };
      
      searchDir('.');
      return projects;
    };
    
    const projects = findProjects();
    
    if (projects.length > 0) {
      projectPath = projects[0];
      console.log(`${colors.green}Found .NET project: ${projectPath}${colors.reset}`);
      config.dotnet.args[2] = projectPath;
    } else {
      console.log(`${colors.red}No .NET projects found. Please update the project path in run-all.js${colors.reset}`);
      console.log(`${colors.yellow}Starting only the React app...${colors.reset}`);
      config.dotnet = null; // Don't start .NET
    }
  }
}

// Find the .NET project and start the app
findDotNetProject();
startApp(); 