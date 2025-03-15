import React from 'react';
import { Box, Typography, CircularProgress } from '@mui/material';

const UtilizationCard = ({ title, value, unit, color, loading, isLatency = false }) => {
  // For latency metrics, lower is better, so we invert the percentage for visual display
  // Assuming max latency we want to show is 20ms
  const maxLatency = 20;
  const percentage = isLatency 
    ? Math.max(0, Math.min(100, 100 - (value / maxLatency * 100))) 
    : value;

  return (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Typography variant="h6" gutterBottom component="div">
        {title}
      </Typography>
      
      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', flexGrow: 1 }}>
          <CircularProgress size={60} />
        </Box>
      ) : (
        <Box sx={{ 
          display: 'flex', 
          flexDirection: 'column', 
          alignItems: 'center', 
          justifyContent: 'center',
          flexGrow: 1
        }}>
          <Box sx={{ position: 'relative', display: 'inline-flex' }}>
            <CircularProgress
              variant="determinate"
              value={percentage}
              size={100}
              thickness={5}
              sx={{ color }}
            />
            <Box
              sx={{
                top: 0,
                left: 0,
                bottom: 0,
                right: 0,
                position: 'absolute',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Typography
                variant="h4"
                component="div"
                color="text.secondary"
              >
                {value}
              </Typography>
            </Box>
          </Box>
          <Typography variant="body1" color="text.secondary" sx={{ mt: 1 }}>
            {unit}
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
            {isLatency ? 'Lower is better' : 'Current'}
          </Typography>
        </Box>
      )}
    </Box>
  );
};

export default UtilizationCard; 