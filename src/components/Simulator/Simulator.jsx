import React, { useState, useEffect } from 'react';
import { useDatabase } from '../../context/DatabaseContext';
import { simulateSlowQuery, checkDatabaseStatus, fetchSlowQueries } from '../../api/sqlApi';
import { 
  Container, 
  Typography, 
  Box, 
  Paper, 
  Button, 
  CircularProgress,
  Alert,
  Grid,
  Card,
  CardContent,
  CardActions,
  Divider,
  Chip,
  Snackbar
} from '@mui/material';

const Simulator = () => {
  const { selectedDatabase } = useDatabase();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [simulationResult, setSimulationResult] = useState(null);
  const [databaseStatus, setDatabaseStatus] = useState(null);
  const [slowQueriesCount, setSlowQueriesCount] = useState(0);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');

  // Check database status
  useEffect(() => {
    if (!selectedDatabase) return;
    
    const checkStatus = async () => {
      try {
        const status = await checkDatabaseStatus(selectedDatabase.id);
        console.log('Current database status:', status);
        setDatabaseStatus(status);
      } catch (err) {
        console.error('Failed to check database status:', err);
      }
    };
    
    checkStatus();
    
    // Check every 10 seconds
    const intervalId = setInterval(checkStatus, 10000);
    
    return () => clearInterval(intervalId);
  }, [selectedDatabase]);

  // Get initial slow queries count
  useEffect(() => {
    const getSlowQueriesCount = async () => {
      if (!selectedDatabase || databaseStatus?.status === 'offline') return;
      
      try {
        const queries = await fetchSlowQueries(selectedDatabase.id);
        setSlowQueriesCount(queries.length);
      } catch (err) {
        console.error('Failed to fetch slow queries count:', err);
      }
    };
    
    getSlowQueriesCount();
  }, [selectedDatabase, databaseStatus]);

  const showSnackbarMessage = (message) => {
    setSnackbarMessage(message);
    setSnackbarOpen(true);
  };

  const handleCloseSnackbar = () => {
    setSnackbarOpen(false);
  };

  const handleSimulateSlowQuery = async () => {
    if (!selectedDatabase) return;
    
    setLoading(true);
    setError(null);
    setSimulationResult(null);
    
    try {
      // Check if database is online
      const status = await checkDatabaseStatus(selectedDatabase.id);
      if (status.status === 'offline') {
        const errorMsg = `Database ${selectedDatabase.name} is offline. Cannot simulate slow query.`;
        setError(errorMsg);
        showSnackbarMessage(errorMsg);
        setLoading(false);
        return;
      }
      
      console.log('Simulating slow query for database:', selectedDatabase.id);
      const result = await simulateSlowQuery(selectedDatabase.id);
      console.log('Simulation result:', result);
      setSimulationResult(result);
      
      // Show success message
      showSnackbarMessage('Slow query simulated successfully. Check the Query Analyzer page to see the results.');
      
      // Update slow queries count
      const queries = await fetchSlowQueries(selectedDatabase.id);
      setSlowQueriesCount(queries.length);
    } catch (err) {
      console.error('Failed to simulate slow query:', err);
      const errorMsg = 'Failed to simulate slow query: ' + (err.message || 'Unknown error');
      setError(errorMsg);
      showSnackbarMessage(errorMsg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container maxWidth="lg">
      <Typography variant="h4" component="h1" gutterBottom>
        Performance Simulator
      </Typography>
      
      {selectedDatabase && (
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6">
            Selected Database: {selectedDatabase.name}
            <Chip 
              label={databaseStatus?.status || selectedDatabase.status} 
              color={databaseStatus?.status === 'offline' ? 'error' : 'success'} 
              size="small" 
              sx={{ ml: 1 }}
            />
          </Typography>
          
          <Box>
            <Chip 
              label={`${slowQueriesCount} Slow Queries`} 
              color="primary" 
              variant="outlined" 
            />
          </Box>
        </Box>
      )}
      
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      
      {databaseStatus?.status === 'offline' ? (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Database is offline. Please bring the database online to run simulations.
        </Alert>
      ) : (
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Simulate Slow Query
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                  This will execute a deliberately inefficient query on the selected database to simulate a performance issue.
                  The query will perform a table scan with large string operations.
                </Typography>
                <Divider sx={{ my: 2 }} />
                {simulationResult && (
                  <Alert severity="info" sx={{ mb: 2 }}>
                    <Typography variant="subtitle2">
                      Slow query executed in {simulationResult.executionTime}ms
                    </Typography>
                    <Typography variant="body2">
                      {simulationResult.message}
                    </Typography>
                  </Alert>
                )}
              </CardContent>
              <CardActions>
                <Button 
                  variant="contained" 
                  color="primary"
                  onClick={handleSimulateSlowQuery}
                  disabled={loading || !selectedDatabase || databaseStatus?.status === 'offline'}
                  startIcon={loading ? <CircularProgress size={20} /> : null}
                >
                  {loading ? 'Simulating...' : 'Simulate Slow Query'}
                </Button>
              </CardActions>
            </Card>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Generate Missing Index Opportunity
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                  This will execute queries that would benefit from indexes that don't exist yet.
                  After running this simulation, check the Index Optimizer to see the recommended indexes.
                </Typography>
                <Divider sx={{ my: 2 }} />
                <Alert severity="warning">
                  This feature will be available in the next update.
                </Alert>
              </CardContent>
              <CardActions>
                <Button 
                  variant="contained" 
                  color="secondary"
                  disabled={true}
                >
                  Coming Soon
                </Button>
              </CardActions>
            </Card>
          </Grid>
          
          <Grid item xs={12}>
            <Paper sx={{ p: 2 }}>
              <Typography variant="h6" gutterBottom>
                What Happens Next?
              </Typography>
              <Typography variant="body1" paragraph>
                After simulating a slow query:
              </Typography>
              <ol>
                <li>
                  <Typography variant="body1" paragraph>
                    Go to the <strong>Query Analyzer</strong> page to see the slow query that was just generated.
                  </Typography>
                </li>
                <li>
                  <Typography variant="body1" paragraph>
                    Analyze the query details and apply the suggested fix to improve performance.
                  </Typography>
                </li>
                <li>
                  <Typography variant="body1" paragraph>
                    Check the <strong>Index Optimizer</strong> page to see if any missing indexes were detected.
                  </Typography>
                </li>
              </ol>
            </Paper>
          </Grid>
        </Grid>
      )}
      
      <Snackbar
        open={snackbarOpen}
        autoHideDuration={6000}
        onClose={handleCloseSnackbar}
        message={snackbarMessage}
      />
    </Container>
  );
};

export default Simulator; 