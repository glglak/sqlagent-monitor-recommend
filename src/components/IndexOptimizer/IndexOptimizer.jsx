import React, { useState, useEffect } from 'react';
import { useDatabase } from '../../context/DatabaseContext';
import { fetchMissingIndexes, simulateIndexCreation, checkDatabaseStatus } from '../../api/sqlApi';
import { 
  Container, 
  Typography, 
  Box, 
  CircularProgress, 
  Paper, 
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert
} from '@mui/material';

const IndexOptimizer = () => {
  const { selectedDatabase } = useDatabase();
  const [missingIndexes, setMissingIndexes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedIndex, setSelectedIndex] = useState(null);
  const [openDialog, setOpenDialog] = useState(false);
  const [creationResult, setCreationResult] = useState(null);
  const [databaseStatus, setDatabaseStatus] = useState(null);

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

  useEffect(() => {
    const loadMissingIndexes = async () => {
      if (!selectedDatabase) return;
      
      setLoading(true);
      try {
        // Check if database is online
        const status = await checkDatabaseStatus(selectedDatabase.id);
        if (status.status === 'offline') {
          setError(`Database ${selectedDatabase.name} is offline. Cannot fetch missing indexes.`);
          setMissingIndexes([]);
          setLoading(false);
          return;
        }
        
        console.log('Fetching missing indexes for database:', selectedDatabase.id);
        const data = await fetchMissingIndexes(selectedDatabase.id);
        console.log('Received missing indexes:', data);
        setMissingIndexes(data);
        setLoading(false);
      } catch (err) {
        console.error('Failed to load missing indexes:', err);
        setError('Failed to load missing indexes: ' + err.message);
        setLoading(false);
      }
    };

    loadMissingIndexes();
  }, [selectedDatabase, databaseStatus]);

  const handleCreateIndex = async (index) => {
    setSelectedIndex(index);
    
    try {
      // Check if database is online
      const status = await checkDatabaseStatus(selectedDatabase.id);
      if (status.status === 'offline') {
        setError(`Database ${selectedDatabase.name} is offline. Cannot create index.`);
        return;
      }
      
      console.log('Creating index:', index);
      const result = await simulateIndexCreation(selectedDatabase.id, index);
      console.log('Index creation result:', result);
      setCreationResult(result);
      setOpenDialog(true);
      
      // Update the index in the list to mark it as created
      const updatedIndexes = missingIndexes.map(idx => 
        idx.id === index.id ? { ...idx, created: true } : idx
      );
      setMissingIndexes(updatedIndexes);
    } catch (err) {
      console.error('Failed to create index:', err);
      setError('Failed to create index: ' + err.message);
    }
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
  };

  const getImpactColor = (impact) => {
    switch (impact) {
      case 'High':
        return 'error';
      case 'Medium':
        return 'warning';
      case 'Low':
        return 'info';
      default:
        return 'default';
    }
  };

  return (
    <Container maxWidth="lg">
      <Typography variant="h4" component="h1" gutterBottom>
        Index Optimizer
      </Typography>
      
      {selectedDatabase && (
        <Typography variant="h6" gutterBottom>
          Analyzing: {selectedDatabase.name}
          <Chip 
            label={databaseStatus?.status || selectedDatabase.status} 
            color={databaseStatus?.status === 'offline' ? 'error' : 'success'} 
            size="small" 
            sx={{ ml: 1 }}
          />
        </Typography>
      )}
      
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      
      {databaseStatus?.status === 'offline' ? (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Database is offline. Please bring the database online to analyze indexes.
        </Alert>
      ) : loading ? (
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
          <CircularProgress />
        </Box>
      ) : missingIndexes.length === 0 ? (
        <Alert severity="info">
          No missing indexes found. Your database schema appears to be well optimized.
        </Alert>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Table</TableCell>
                <TableCell>Columns</TableCell>
                <TableCell>Include Columns</TableCell>
                <TableCell>Impact</TableCell>
                <TableCell>Improvement</TableCell>
                <TableCell>Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {missingIndexes.map((index) => (
                <TableRow key={index.id}>
                  <TableCell>{index.table}</TableCell>
                  <TableCell>
                    {index.columns.map(col => (
                      <Chip 
                        key={col} 
                        label={col} 
                        size="small" 
                        sx={{ m: 0.5 }} 
                      />
                    ))}
                  </TableCell>
                  <TableCell>
                    {index.includeColumns.length > 0 ? (
                      index.includeColumns.map(col => (
                        <Chip 
                          key={col} 
                          label={col} 
                          size="small" 
                          variant="outlined" 
                          sx={{ m: 0.5 }} 
                        />
                      ))
                    ) : (
                      <Typography variant="body2" color="text.secondary">None</Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Chip 
                      label={index.estimatedImpact} 
                      color={getImpactColor(index.estimatedImpact)} 
                      size="small" 
                    />
                  </TableCell>
                  <TableCell>{index.improvementPercent}%</TableCell>
                  <TableCell>
                    <Button
                      variant="contained"
                      color="primary"
                      size="small"
                      onClick={() => handleCreateIndex(index)}
                      disabled={index.created || databaseStatus?.status === 'offline'}
                    >
                      {index.created ? 'Created' : 'Create Index'}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
      
      <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
        <DialogTitle>Index Creation Result</DialogTitle>
        <DialogContent>
          {creationResult && (
            <Box>
              <Alert severity="success" sx={{ mb: 2 }}>
                {creationResult.message}
              </Alert>
              
              <Typography variant="subtitle1" gutterBottom>
                Index Name: {creationResult.indexName}
              </Typography>
              
              <Typography variant="subtitle1" gutterBottom>
                Performance Improvement: {creationResult.performanceImprovement}
              </Typography>
              
              {selectedIndex && (
                <Paper variant="outlined" sx={{ p: 2, mt: 2, backgroundColor: '#f5f5f5' }}>
                  <Typography variant="subtitle2" gutterBottom>
                    SQL Statement:
                  </Typography>
                  <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap' }}>
                    {selectedIndex.createStatement}
                  </Typography>
                </Paper>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog} color="primary">
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default IndexOptimizer; 