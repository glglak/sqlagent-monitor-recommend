import React, { useState, useEffect } from 'react';
import { useDatabase } from '../../context/DatabaseContext';
import { fetchMissingIndexes, simulateIndexCreation, checkDatabaseStatus } from '../../api/dotnetApi';
import { 
  Container, 
  Typography, 
  Box, 
  CircularProgress, 
  Paper, 
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Button,
  Alert,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Snackbar
} from '@mui/material';
import TuneIcon from '@mui/icons-material/Tune';
import AddIcon from '@mui/icons-material/Add';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';

const IndexRecommendations = () => {
  const { selectedDatabase } = useDatabase();
  const [missingIndexes, setMissingIndexes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedIndex, setSelectedIndex] = useState(null);
  const [openDialog, setOpenDialog] = useState(false);
  const [creatingIndex, setCreatingIndex] = useState(false);
  const [creationResult, setCreationResult] = useState(null);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');

  // Check database status and load missing indexes
  useEffect(() => {
    const loadMissingIndexes = async () => {
      if (!selectedDatabase) return;
      
      setLoading(true);
      try {
        // Check if database is online
        const status = await checkDatabaseStatus(selectedDatabase.id);
        if (status.status === 'offline') {
          setError(`Database ${selectedDatabase.name} is offline. Cannot fetch index recommendations.`);
          setMissingIndexes([]);
          setLoading(false);
          return;
        }
        
        console.log('Fetching missing indexes for database:', selectedDatabase.id);
        const data = await fetchMissingIndexes(selectedDatabase.id);
        console.log('Received missing indexes:', data);
        
        if (data && Array.isArray(data)) {
          setMissingIndexes(data);
        } else {
          // If API returns no data, use mock data
          const mockIndexes = [
            {
              id: 'idx_1',
              table: 'Sales.Orders',
              columns: 'CustomerID, OrderDate',
              includeColumns: 'TotalAmount',
              estimatedImpact: 'High',
              improvementPercent: 45,
              createStatement: 'CREATE INDEX IX_Sales_Orders_CustomerID_OrderDate ON Sales.Orders (CustomerID, OrderDate) INCLUDE (TotalAmount)',
              created: false
            },
            {
              id: 'idx_2',
              table: 'Production.Products',
              columns: 'CategoryID, Price',
              includeColumns: 'ProductName',
              estimatedImpact: 'Medium',
              improvementPercent: 28,
              createStatement: 'CREATE INDEX IX_Production_Products_CategoryID_Price ON Production.Products (CategoryID, Price) INCLUDE (ProductName)',
              created: false
            },
            {
              id: 'idx_3',
              table: 'HumanResources.Employees',
              columns: 'DepartmentID, HireDate',
              includeColumns: 'EmployeeName, Salary',
              estimatedImpact: 'Low',
              improvementPercent: 15,
              createStatement: 'CREATE INDEX IX_HumanResources_Employees_DepartmentID_HireDate ON HumanResources.Employees (DepartmentID, HireDate) INCLUDE (EmployeeName, Salary)',
              created: false
            }
          ];
          setMissingIndexes(mockIndexes);
        }
      } catch (err) {
        console.error('Failed to load missing indexes:', err);
        setError('Failed to load missing indexes: ' + err.message);
        
        // Use mock data as fallback
        const mockIndexes = [
          {
            id: 'idx_1',
            table: 'Sales.Orders',
            columns: 'CustomerID, OrderDate',
            includeColumns: 'TotalAmount',
            estimatedImpact: 'High',
            improvementPercent: 45,
            createStatement: 'CREATE INDEX IX_Sales_Orders_CustomerID_OrderDate ON Sales.Orders (CustomerID, OrderDate) INCLUDE (TotalAmount)',
            created: false
          },
          {
            id: 'idx_2',
            table: 'Production.Products',
            columns: 'CategoryID, Price',
            includeColumns: 'ProductName',
            estimatedImpact: 'Medium',
            improvementPercent: 28,
            createStatement: 'CREATE INDEX IX_Production_Products_CategoryID_Price ON Production.Products (CategoryID, Price) INCLUDE (ProductName)',
            created: false
          },
          {
            id: 'idx_3',
            table: 'HumanResources.Employees',
            columns: 'DepartmentID, HireDate',
            includeColumns: 'EmployeeName, Salary',
            estimatedImpact: 'Low',
            improvementPercent: 15,
            createStatement: 'CREATE INDEX IX_HumanResources_Employees_DepartmentID_HireDate ON HumanResources.Employees (DepartmentID, HireDate) INCLUDE (EmployeeName, Salary)',
            created: false
          }
        ];
        setMissingIndexes(mockIndexes);
      } finally {
        setLoading(false);
      }
    };

    loadMissingIndexes();
  }, [selectedDatabase]);

  const handleIndexSelect = (index) => {
    setSelectedIndex(index);
    setOpenDialog(true);
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
  };

  const handleCreateIndex = async () => {
    if (!selectedIndex) return;
    
    setCreatingIndex(true);
    try {
      console.log('Creating index:', selectedIndex.id);
      const result = await simulateIndexCreation(selectedDatabase.id, {
        table: selectedIndex.table,
        columns: selectedIndex.columns
      });
      
      console.log('Index creation result:', result);
      setCreationResult(result);
      
      // Update the index in the list to mark it as created
      const updatedIndexes = missingIndexes.map(idx => 
        idx.id === selectedIndex.id ? { ...idx, created: true } : idx
      );
      setMissingIndexes(updatedIndexes);
      
      // Show success message
      setSnackbarMessage(`Index created successfully: ${result.indexName}`);
      setSnackbarOpen(true);
    } catch (err) {
      console.error('Failed to create index:', err);
      setError('Failed to create index: ' + err.message);
      
      // Show error message
      setSnackbarMessage('Failed to create index: ' + err.message);
      setSnackbarOpen(true);
    } finally {
      setCreatingIndex(false);
      setOpenDialog(false);
    }
  };

  const handleCloseSnackbar = () => {
    setSnackbarOpen(false);
  };

  const getImpactColor = (impact) => {
    switch (impact.toLowerCase()) {
      case 'high':
        return 'error';
      case 'medium':
        return 'warning';
      case 'low':
        return 'success';
      default:
        return 'default';
    }
  };

  if (!selectedDatabase) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="info">Please select a database to view index recommendations.</Alert>
      </Container>
    );
  }

  if (selectedDatabase.status === 'offline') {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="warning">
          Database is offline. Cannot analyze index recommendations.
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box display="flex" alignItems="center" mb={3}>
        <TuneIcon sx={{ mr: 1 }} />
        <Typography variant="h4" component="h1">
          Index Recommendations
        </Typography>
      </Box>
      
      {loading ? (
        <Box display="flex" justifyContent="center" p={3}>
          <CircularProgress />
        </Box>
      ) : error ? (
        <Alert severity="error">{error}</Alert>
      ) : missingIndexes.length === 0 ? (
        <Alert severity="info">
          No index recommendations found. Your database schema appears to be well-optimized.
        </Alert>
      ) : (
        <Paper sx={{ p: 2, mb: 4 }}>
          <Typography variant="h6" gutterBottom>
            Recommended Indexes
          </Typography>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Table</TableCell>
                  <TableCell>Columns</TableCell>
                  <TableCell>Include Columns</TableCell>
                  <TableCell align="center">Impact</TableCell>
                  <TableCell align="center">Improvement</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {missingIndexes.map((index) => (
                  <TableRow key={index.id} hover>
                    <TableCell>{index.table}</TableCell>
                    <TableCell>{index.columns}</TableCell>
                    <TableCell>{index.includeColumns || 'None'}</TableCell>
                    <TableCell align="center">
                      <Chip 
                        label={index.estimatedImpact} 
                        color={getImpactColor(index.estimatedImpact)}
                        size="small"
                      />
                    </TableCell>
                    <TableCell align="center">
                      <Box display="flex" alignItems="center" justifyContent="center">
                        <ArrowUpwardIcon color="success" fontSize="small" sx={{ mr: 0.5 }} />
                        {index.improvementPercent}%
                      </Box>
                    </TableCell>
                    <TableCell align="right">
                      <Button
                        variant="contained"
                        color="primary"
                        size="small"
                        startIcon={<AddIcon />}
                        onClick={() => handleIndexSelect(index)}
                        disabled={index.created}
                      >
                        {index.created ? 'Created' : 'Create Index'}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      {/* Create Index Dialog */}
      <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
        <DialogTitle>Create Index</DialogTitle>
        <DialogContent>
          {selectedIndex && (
            <>
              <Typography variant="subtitle1" gutterBottom>
                Create the following index:
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
                {selectedIndex.createStatement}
              </Paper>
              
              <Alert severity="info" sx={{ mb: 2 }}>
                This will create an index on the <strong>{selectedIndex.table}</strong> table.
                <br />
                Estimated performance improvement: <strong>{selectedIndex.improvementPercent}%</strong>
              </Alert>
              
              <Alert severity="warning">
                Creating indexes can temporarily lock tables and impact database performance during creation.
                In a production environment, consider scheduling this operation during maintenance windows.
              </Alert>
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button 
            onClick={handleCreateIndex} 
            variant="contained" 
            color="primary"
            disabled={creatingIndex}
            startIcon={creatingIndex ? <CircularProgress size={20} /> : <AddIcon />}
          >
            {creatingIndex ? 'Creating...' : 'Create Index'}
          </Button>
        </DialogActions>
      </Dialog>
      
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

export default IndexRecommendations; 