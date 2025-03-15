import React from 'react';
import { Container, Typography, Button, Box, Paper } from '@mui/material';

const TestComponent = () => {
  const [count, setCount] = React.useState(0);
  
  return (
    <Container maxWidth="md">
      <Typography variant="h4" gutterBottom>
        Test Component
      </Typography>
      
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          This component doesn't rely on API data
        </Typography>
        
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Button 
            variant="contained" 
            onClick={() => setCount(count + 1)}
          >
            Click me
          </Button>
          
          <Typography>
            Count: {count}
          </Typography>
        </Box>
      </Paper>
      
      <Paper sx={{ p: 3 }}>
        <Typography variant="body1">
          If you can see this and the button works, React is functioning correctly.
          The issue is likely with API communication or data processing.
        </Typography>
      </Paper>
    </Container>
  );
};

export default TestComponent; 