import React from 'react';
import { Typography, Box, Chip, Paper, Divider } from '@mui/material';
import CircleIcon from '@mui/icons-material/Circle';

const ServerStatus = ({ database }) => {
  if (!database) {
    return (
      <Typography variant="body1">
        No database selected
      </Typography>
    );
  }

  const isOnline = database.Status === 'ONLINE';

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Server Status
      </Typography>
      
      <Box mb={2}>
        <Typography variant="subtitle2" gutterBottom>
          Database:
        </Typography>
        <Typography variant="body1">
          {database.Name}
        </Typography>
      </Box>
      
      <Box mb={2}>
        <Typography variant="subtitle2" gutterBottom>
          Server:
        </Typography>
        <Typography variant="body1">
          {database.Server}
        </Typography>
      </Box>
      
      <Box mb={2}>
        <Typography variant="subtitle2" gutterBottom>
          Status:
        </Typography>
        <Box display="flex" alignItems="center">
          <CircleIcon 
            fontSize="small" 
            sx={{ 
              color: isOnline ? 'success.main' : 'error.main',
              mr: 1,
              animation: isOnline ? 'pulse 2s infinite' : 'none',
              '@keyframes pulse': {
                '0%': { opacity: 0.6 },
                '50%': { opacity: 1 },
                '100%': { opacity: 0.6 }
              }
            }} 
          />
          <Chip 
            label={database.Status} 
            color={isOnline ? 'success' : 'error'} 
            size="small" 
          />
        </Box>
      </Box>
      
      <Divider sx={{ my: 2 }} />
      
      <Box>
        <Typography variant="subtitle2" gutterBottom>
          Connection Details:
        </Typography>
        <Paper variant="outlined" sx={{ p: 1.5, backgroundColor: '#f8f8f8' }}>
          <Typography variant="body2" component="div">
            <Box display="flex" justifyContent="space-between">
              <span>Connection Type:</span>
              <span>Windows Authentication</span>
            </Box>
            <Box display="flex" justifyContent="space-between" mt={0.5}>
              <span>Version:</span>
              <span>{database.version || 'SQL Server 14.0.100'}</span>
            </Box>
            <Box display="flex" justifyContent="space-between" mt={0.5}>
              <span>Last Refresh:</span>
              <span>{new Date().toLocaleTimeString()}</span>
            </Box>
          </Typography>
        </Paper>
      </Box>
    </Box>
  );
};

export default ServerStatus; 