import React, { useState, useEffect } from 'react';
import { useDatabase } from '../../context/DatabaseContext';
import { 
  fetchSlowQueries, 
  applyQueryFix, 
  simulateSlowQuery, 
  checkDatabaseStatus,
  optimizeQueryWithAI
} from '../../api/dotnetApi';
import { 
  Container, 
  Typography, 
  Box, 
  CircularProgress, 
  Paper, 
  List,
  ListItem,
  ListItemText,
  Chip,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  Divider,
  Snackbar,
  Grid,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow
} from '@mui/material';
import QueryOptimizationResult from './QueryOptimizationResult';
import SpeedIcon from '@mui/icons-material/Speed';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import WarningIcon from '@mui/icons-material/Warning';

const QueryAnalyzer = () => {
  const { selectedDatabase } = useDatabase();
  const [slowQueries, setSlowQueries] = useState([]);
  const [selectedQuery, setSelectedQuery] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [openDialog, setOpenDialog] = useState(false);
  const [fixResult, setFixResult] = useState(null);
  const [databaseStatus, setDatabaseStatus] = useState(null);
  const [simulationInProgress, setSimulationInProgress] = useState(false);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [optimizationResult, setOptimizationResult] = useState(null);
  const [optimizing, setOptimizing] = useState(false);

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

  // Load slow queries
  useEffect(() => {
    const loadSlowQueries = async () => {
      if (!selectedDatabase?.id) {
        console.log('No database selected, skipping slow queries fetch');
        return;
      }
      
      setLoading(true);
      try {
        // Check if database is online
        const status = await checkDatabaseStatus(selectedDatabase.id);
        if (status.status === 'offline') {
          setError(`Database ${selectedDatabase.name} is offline. Cannot fetch slow queries.`);
          setSlowQueries([]);
          setSelectedQuery(null);
          setLoading(false);
          return;
        }
        
        console.log('Fetching slow queries for database:', selectedDatabase.id);
        const data = await fetchSlowQueries(selectedDatabase.id);
        console.log('Received slow queries:', data);
        // Debug log to inspect query structure
        if (data.length > 0) {
          console.log('Sample query object structure:', JSON.stringify(data[0], null, 2));
        }
        setSlowQueries(data);
        if (data.length > 0) {
          setSelectedQuery(data[0]);
        } else {
          setSelectedQuery(null);
        }
        setLoading(false);
      } catch (err) {
        console.error('Failed to load slow queries:', err);
        setError('Failed to load slow queries: ' + err.message);
        setLoading(false);
      }
    };

    loadSlowQueries();
  }, [selectedDatabase?.id, databaseStatus]);

  const handleQuerySelect = (query) => {
    setSelectedQuery(query);
    setOpenDialog(true);
  };

  const handleApplyFix = async () => {
    if (!selectedQuery) return;
    
    try {
      console.log('Applying fix for query:', selectedQuery.id);
      const result = await applyQueryFix(selectedQuery.id, selectedDatabase.id, selectedQuery.suggestedFix);
      console.log('Fix result:', result);
      setFixResult(result);
      setOpenDialog(true);
      
      // Update the query in the list to mark it as fixed
      const updatedQueries = slowQueries.map(q => 
        q.id === selectedQuery.id ? { ...q, fixed: true } : q
      );
      setSlowQueries(updatedQueries);
    } catch (err) {
      console.error('Failed to apply query fix:', err);
      setError('Failed to apply query fix: ' + err.message);
      showSnackbarMessage('Failed to apply query fix: ' + err.message);
    }
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
  };

  const showSnackbarMessage = (message) => {
    setSnackbarMessage(message);
    setSnackbarOpen(true);
  };

  const handleCloseSnackbar = () => {
    setSnackbarOpen(false);
  };

  // Add function to simulate slow query directly from this component
  const handleSimulateSlowQuery = async () => {
    if (!selectedDatabase?.id) {
      console.log('No database selected, cannot simulate slow query');
      showSnackbarMessage('Please select a database first');
      return;
    }
    
    setSimulationInProgress(true);
    setError(null);
    
    try {
      // Check if database is online
      const status = await checkDatabaseStatus(selectedDatabase.id);
      if (status.status === 'offline') {
        const errorMsg = `Database ${selectedDatabase.name} is offline. Cannot simulate slow query.`;
        setError(errorMsg);
        showSnackbarMessage(errorMsg);
        setSimulationInProgress(false);
        return;
      }
      
      console.log('Simulating slow query for database:', selectedDatabase.id);
      const result = await simulateSlowQuery(selectedDatabase.id);
      console.log('Simulation result:', result);
      
      // Show success message
      showSnackbarMessage('Slow query simulated successfully. Refreshing query list...');
      
      // Refresh slow queries after simulation
      const updatedQueries = await fetchSlowQueries(selectedDatabase.id);
      console.log('Updated slow queries after simulation:', updatedQueries);
      setSlowQueries(updatedQueries);
      
      if (updatedQueries.length > 0 && !selectedQuery) {
        setSelectedQuery(updatedQueries[0]);
      }
      
      setSimulationInProgress(false);
    } catch (err) {
      console.error('Failed to simulate slow query:', err);
      const errorMsg = 'Failed to simulate slow query: ' + (err.message || 'Unknown error');
      setError(errorMsg);
      showSnackbarMessage(errorMsg);
      setSimulationInProgress(false);
    }
  };

  const handleOptimizeQuery = async () => {
    if (!selectedQuery) return;
    
    try {
      setOptimizing(true);
      setOptimizationResult(null);
      
      console.log('Optimizing query with AI:', selectedQuery);
      
      const result = await optimizeQueryWithAI(
        selectedDatabase.id,
        selectedQuery
      );
      
      console.log('Optimization result:', result);

      // Only set the optimization result if we got a valid optimized query
      if (result.optimizedQuery) {
        setOptimizationResult(result);
        setOpenDialog(false);

        // If the optimization was successful, update the query in the list
        if (result.optimizedQueryWorks) {
          const updatedQueries = slowQueries.map(q => 
            q.Id === selectedQuery.Id 
              ? { 
                  ...q, 
                  Fixed: true, 
                  OptimizedQuery: result.optimizedQuery,
                  OptimizationSuggestions: result.explanation 
                }
              : q
          );
          setSlowQueries(updatedQueries);
          showSnackbarMessage('Query optimized successfully!');
        }
      } else {
        throw new Error('Failed to optimize query: No optimized query returned');
      }
    } catch (err) {
      console.error('Error optimizing query:', err);
      const errorMessage = err.message || 'Failed to optimize query';
      setError(errorMessage);
      showSnackbarMessage('Failed to optimize query: ' + errorMessage);
      
      // For errors, show a different view in the optimization result
      setOptimizationResult({
        originalQuery: selectedQuery.Query,
        optimizedQuery: null, // Set to null to indicate no optimization
        explanation: `Optimization failed: ${errorMessage}`,
        indexRecommendations: [],
        performanceBefore: {
          executionTime: selectedQuery.ExecutionTime,
          cpuTime: selectedQuery.CpuTime,
          logicalReads: selectedQuery.LogicalReads
        },
        performanceAfter: null, // Set to null to indicate no performance data
        improvementPercent: 0,
        aiPowered: true,
        optimizedQueryWorks: false
      });
      setOpenDialog(false); // Close the dialog on error
    } finally {
      setOptimizing(false);
    }
  };

  if (!selectedDatabase) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="info">Please select a database to view slow queries.</Alert>
      </Container>
    );
  }

  if (selectedDatabase.status === 'offline') {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="warning">
          Database is offline. Cannot analyze queries.
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box display="flex" alignItems="center" mb={3}>
        <SpeedIcon sx={{ mr: 1 }} />
        <Typography variant="h4" component="h1">
          Query Analyzer
        </Typography>
      </Box>
      
      <Box display="flex" justifyContent="flex-end" mb={3}>
        <Button
          variant="contained"
          color="primary"
          onClick={handleSimulateSlowQuery}
          disabled={simulationInProgress}
          startIcon={simulationInProgress ? <CircularProgress size={20} /> : <SpeedIcon />}
        >
          {simulationInProgress ? 'Simulating...' : 'Simulate Slow Query'}
        </Button>
      </Box>
      
      {loading ? (
        <Box display="flex" justifyContent="center" p={3}>
          <CircularProgress />
        </Box>
      ) : error ? (
        <Alert severity="error">{error}</Alert>
      ) : slowQueries.length === 0 ? (
        <Alert severity="info">
          No slow queries found. Your database is performing well, or you may need to simulate some slow queries.
        </Alert>
      ) : (
        <Paper sx={{ p: 2, mb: 4 }}>
          <Typography variant="h6" gutterBottom>
            Slow Queries
          </Typography>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Query</TableCell>
                  <TableCell align="right">Execution Time (ms)</TableCell>
                  <TableCell align="right">CPU Time (ms)</TableCell>
                  <TableCell align="right">Logical Reads</TableCell>
                  <TableCell align="right">Executions</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {slowQueries.map((query, index) => {
                  // Create a unique key using multiple properties or fallback to index
                  const queryKey = [
                    query.id,
                    query.Id,
                    query.QueryText,
                    query.query,
                    query.executionTime,
                    query.ExecutionTime,
                    index // Always include index as final fallback
                  ].filter(Boolean).join('-') || `query-${index}`;

                  return (
                    <TableRow 
                      key={queryKey}
                      hover
                    >
                      <TableCell>
                        <Typography
                          variant="body2"
                          sx={{
                            maxWidth: 400,
                            whiteSpace: 'nowrap',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis'
                          }}
                        >
                          {query.Query || 'No query text available'}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Box display="flex" alignItems="center" justifyContent="flex-end">
                          {query.ExecutionTime > 1000 && (
                            <WarningIcon color="warning" fontSize="small" sx={{ mr: 0.5 }} />
                          )}
                          {query.ExecutionTime || 'N/A'}
                        </Box>
                      </TableCell>
                      <TableCell align="right">{query.CpuTime || 'N/A'}</TableCell>
                      <TableCell align="right">{query.LogicalReads || 'N/A'}</TableCell>
                      <TableCell align="right">{query.ExecutionCount || 'N/A'}</TableCell>
                      <TableCell align="right">
                        <Button
                          variant="contained"
                          color="primary"
                          size="small"
                          startIcon={<AutoFixHighIcon />}
                          onClick={() => handleQuerySelect(query)}
                          disabled={query.Fixed}
                        >
                          {query.Fixed ? 'Fixed' : 'Optimize'}
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      {/* Query Optimization Dialog */}
      <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
        <DialogTitle>Optimize Query</DialogTitle>
        <DialogContent>
          {selectedQuery && (
            <>
              <Typography variant="subtitle1" gutterBottom>
                Query to Optimize:
              </Typography>
              <Paper 
                elevation={0} 
                sx={{ 
                  p: 2, 
                  bgcolor: 'grey.100', 
                  maxHeight: '200px', 
                  overflow: 'auto',
                  fontFamily: 'monospace',
                  whiteSpace: 'pre-wrap',
                  fontSize: '0.875rem',
                  mb: 2
                }}
              >
                {selectedQuery.Query || 'No query text available'}
              </Paper>
              
              <Alert severity="info" sx={{ mb: 2 }}>
                This will use Azure OpenAI to analyze and optimize the query. The optimization process will:
                <ul>
                  <li>Analyze the query structure and table schema</li>
                  <li>Generate an optimized version with better performance</li>
                  <li>Provide index recommendations</li>
                  <li>Test the performance improvement</li>
                </ul>
              </Alert>
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button 
            onClick={handleOptimizeQuery} 
            variant="contained" 
            color="primary"
            disabled={optimizing}
            startIcon={optimizing ? <CircularProgress size={20} /> : <AutoFixHighIcon />}
          >
            {optimizing ? 'Optimizing...' : 'Optimize with AI'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Optimization Results */}
      {optimizationResult && <QueryOptimizationResult result={optimizationResult} />}
      
      {/* Snackbar for notifications */}
      <Snackbar
        open={snackbarOpen}
        autoHideDuration={6000}
        onClose={handleCloseSnackbar}
        message={snackbarMessage}
      />
    </Container>
  );
};

export default QueryAnalyzer; 