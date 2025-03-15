const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

// Check if build directory exists
const buildDir = path.join(__dirname, 'build');
if (!fs.existsSync(buildDir)) {
  console.log('Build directory not found. Creating it...');
  fs.mkdirSync(buildDir, { recursive: true });
}

// Copy index.html to build directory if it doesn't exist
const indexHtmlPath = path.join(buildDir, 'index.html');
if (!fs.existsSync(indexHtmlPath)) {
  console.log('index.html not found in build directory. Copying from public...');
  const publicIndexHtml = path.join(__dirname, 'public', 'index.html');
  if (fs.existsSync(publicIndexHtml)) {
    fs.copyFileSync(publicIndexHtml, indexHtmlPath);
  } else {
    // Create a minimal index.html
    fs.writeFileSync(indexHtmlPath, `
      <!DOCTYPE html>
      <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>SQL Monitor & Recommend</title>
        </head>
        <body>
          <div id="root"></div>
        </body>
      </html>
    `);
  }
}

// Start the server
console.log('Starting server...');
const server = spawn('node', ['server.js'], { stdio: 'inherit' });

server.on('close', (code) => {
  console.log(`Server process exited with code ${code}`);
});

// Handle termination signals
process.on('SIGINT', () => {
  console.log('Stopping server...');
  server.kill('SIGINT');
  process.exit(0);
});

process.on('SIGTERM', () => {
  console.log('Stopping server...');
  server.kill('SIGTERM');
  process.exit(0);
});

console.log('Server started. Press Ctrl+C to stop.'); 