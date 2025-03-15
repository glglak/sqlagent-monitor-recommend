import React from 'react';
import { 
  List, 
  ListItem, 
  ListItemText, 
  ListItemSecondaryAction,
  Typography,
  Chip,
  Box,
  Divider
} from '@mui/material';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import MemoryIcon from '@mui/icons-material/Memory';
import RepeatIcon from '@mui/icons-material/Repeat';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';

const SlowQueryList = ({ queries, selectedQueryId, onSelectQuery }) => {
  if (!queries || queries.length === 0) {
    return <Typography>No slow queries found</Typography>;
  }

  return (
    <List className="query-list">
      {queries.map((query, index) => (
        <React.Fragment key={query.id}>
          <ListItem 
            button
            selected={selectedQueryId === query.id}
            onClick={() => onSelectQuery(query)}
            className="query-list-item"
          >
            <ListItemText
              primary={
                <Typography noWrap className="query-text">
                  {query.query.substring(0, 60)}...
                </Typography>
              }
              secondary={
                <Box mt={1}>
                  <Chip 
                    icon={<AccessTimeIcon fontSize="small" />} 
                    label={`${query.executionTime}ms`} 
                    size="small" 
                    color="primary" 
                    variant="outlined"
                    className="query-chip"
                  />
                  <Chip 
                    icon={<MemoryIcon fontSize="small" />} 
                    label={`${query.logicalReads} reads`} 
                    size="small" 
                    color="secondary" 
                    variant="outlined"
                    className="query-chip"
                  />
                  <Chip 
                    icon={<RepeatIcon fontSize="small" />} 
                    label={`${query.executionCount} executions`} 
                    size="small" 
                    variant="outlined"
                    className="query-chip"
                  />
                </Box>
              }
            />
            <ListItemSecondaryAction>
              {query.fixed && (
                <CheckCircleIcon color="success" />
              )}
            </ListItemSecondaryAction>
          </ListItem>
          {index < queries.length - 1 && <Divider />}
        </React.Fragment>
      ))}
    </List>
  );
};

export default SlowQueryList; 