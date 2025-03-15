import React from 'react';
import { useDatabase } from '../../context/DatabaseContext';
import { 
  FormControl, 
  InputLabel, 
  Select, 
  MenuItem, 
  Box, 
  CircularProgress,
  Typography,
  Chip,
  IconButton,
  Tooltip
} from '@mui/material';
import StorageIcon from '@mui/icons-material/Storage';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ErrorIcon from '@mui/icons-material/Error';
import RefreshIcon from '@mui/icons-material/Refresh';

const DatabaseSelector = () => {
  const { 
    databases, 
    selectedDatabase, 
    selectDatabase, 
    loading, 
    error,
    refreshDatabaseList
  } = useDatabase();

  const handleChange = (event) => {
    const databaseId = event.target.value;
    console.log('Database selected:', databaseId);
    selectDatabase(databaseId);
  };

  const handleRefresh = () => {
    refreshDatabaseList();
  };

  if (loading) {
    return (
      <Box display="flex" alignItems="center">
        <CircularProgress size={20} sx={{ mr: 1 }} />
        <Typography variant="body2" color="white">Loading databases...</Typography>
      </Box>
    );
  }

  if (error) {
    return (
      <Box display="flex" alignItems="center">
        <ErrorIcon sx={{ mr: 1, color: 'error.light' }} />
        <Typography variant="body2" color="white" sx={{ mr: 1 }}>Error loading databases</Typography>
        <Tooltip title="Refresh database list">
          <IconButton size="small" color="inherit" onClick={handleRefresh}>
            <RefreshIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>
    );
  }

  if (!databases || databases.length === 0) {
    return (
      <Box display="flex" alignItems="center">
        <ErrorIcon sx={{ mr: 1, color: 'warning.light' }} />
        <Typography variant="body2" color="white" sx={{ mr: 1 }}>No databases found</Typography>
        <Tooltip title="Refresh database list">
          <IconButton size="small" color="inherit" onClick={handleRefresh}>
            <RefreshIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>
    );
  }

  return (
    <Box display="flex" alignItems="center">
      <StorageIcon sx={{ mr: 1, color: 'white' }} />
      <FormControl variant="outlined" size="small" sx={{ minWidth: 200, mr: 1 }}>
        <InputLabel id="database-select-label" sx={{ color: 'rgba(255, 255, 255, 0.7)' }}>Database</InputLabel>
        <Select
          labelId="database-select-label"
          id="database-select"
          value={selectedDatabase ? selectedDatabase.id : ''}
          onChange={handleChange}
          label="Database"
          sx={{ 
            color: 'white',
            '.MuiOutlinedInput-notchedOutline': {
              borderColor: 'rgba(255, 255, 255, 0.3)',
            },
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: 'rgba(255, 255, 255, 0.5)',
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: 'white',
            },
            '.MuiSvgIcon-root': {
              color: 'white',
            }
          }}
          MenuProps={{
            PaperProps: {
              style: {
                maxHeight: 300
              }
            }
          }}
        >
          {databases.map((db) => (
            <MenuItem key={db.id} value={db.id}>
              <Box display="flex" alignItems="center" width="100%" justifyContent="space-between">
                <Typography variant="body2">{db.name}</Typography>
                <Chip 
                  size="small" 
                  label={db.Status} 
                  color={db.Status === 'ONLINE' ? 'success' : 'error'}
                  icon={db.Status === 'ONLINE' ? <CheckCircleIcon /> : <ErrorIcon />}
                  sx={{ ml: 1 }}
                />
              </Box>
            </MenuItem>
          ))}
        </Select>
      </FormControl>
      <Tooltip title="Refresh database list">
        <IconButton 
          size="small" 
          color="inherit" 
          onClick={handleRefresh}
          disabled={loading}
        >
          {loading ? <CircularProgress size={20} color="inherit" /> : <RefreshIcon />}
        </IconButton>
      </Tooltip>
    </Box>
  );
};

export default DatabaseSelector; 