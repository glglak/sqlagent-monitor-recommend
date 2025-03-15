/**
 * Script to kill processes running on specific ports
 * Works on both Windows and Unix-like systems
 */

const { execSync } = require('child_process');
const os = require('os');

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

// Ports to kill
const ports = [3000, 3001, 3002, 3003];

console.log(`${colors.cyan}${colors.bright}Killing processes on ports: ${ports.join(', ')}${colors.reset}`);

// Function to kill process on a specific port
function killProcessOnPort(port) {
  try {
    console.log(`${colors.yellow}Checking port ${port}...${colors.reset}`);
    
    if (process.platform === 'win32') {
      // Windows
      try {
        const output = execSync(`for /f "tokens=5" %a in ('netstat -ano ^| find ":${port}" ^| find "LISTENING"') do taskkill /F /PID %a`);
        console.log(`${colors.green}Successfully killed process on port ${port}${colors.reset}`);
      } catch (error) {
        // If the command fails, it likely means no process was found
        console.log(`${colors.green}No process found on port ${port}${colors.reset}`);
      }
    } else if (process.platform === 'darwin' || process.platform === 'linux') {
      // macOS or Linux
      try {
        const pid = execSync(`lsof -i :${port} -t`).toString().trim();
        if (pid) {
          execSync(`kill -9 ${pid}`);
          console.log(`${colors.green}Successfully killed process on port ${port} (PID: ${pid})${colors.reset}`);
        } else {
          console.log(`${colors.green}No process found on port ${port}${colors.reset}`);
        }
      } catch (error) {
        // If the command fails, it likely means no process was found
        console.log(`${colors.green}No process found on port ${port}${colors.reset}`);
      }
    } else {
      console.log(`${colors.red}Unsupported platform: ${process.platform}${colors.reset}`);
    }
  } catch (error) {
    console.log(`${colors.red}Error killing process on port ${port}: ${error.message}${colors.reset}`);
  }
}

// Kill processes on all specified ports
ports.forEach(port => {
  killProcessOnPort(port);
});

console.log(`${colors.green}${colors.bright}Done killing processes!${colors.reset}`); 