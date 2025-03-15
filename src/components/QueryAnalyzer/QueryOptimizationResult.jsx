import React from 'react';
import { 
  Box, 
  Typography, 
  Paper, 
  Divider, 
  Chip, 
  Grid,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Alert
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import CodeIcon from '@mui/icons-material/Code';
import SpeedIcon from '@mui/icons-material/Speed';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';

const QueryOptimizationResult = ({ result }) => {
  if (!result) {
    return null;
  }

  const {
    originalQuery,
    optimizedQuery,
    explanation,
    indexRecommendations,
    performanceBefore = {},
    performanceAfter = null,
    improvementPercent = 0,
    aiPowered = false,
    optimizedQueryWorks = false
  } = result;

  // If we have an error (no optimized query), show error view
  if (!optimizedQuery) {
    return (
      <Paper elevation={3} sx={{ p: 3, mt: 3 }}>
        <Box display="flex" alignItems="center" mb={2}>
          <AutoFixHighIcon color="error" sx={{ mr: 1 }} />
          <Typography variant="h5" component="h2">
            Query Optimization Failed
          </Typography>
        </Box>

        <Alert severity="error" sx={{ mb: 3 }}>
          {explanation}
        </Alert>

        <Typography variant="subtitle1" gutterBottom>
          Original Query:
        </Typography>
        <Paper 
          elevation={0} 
          sx={{ 
            p: 2, 
            bgcolor: 'grey.100', 
            maxHeight: '300px', 
            overflow: 'auto',
            fontFamily: 'monospace',
            whiteSpace: 'pre-wrap',
            fontSize: '0.875rem'
          }}
        >
          {originalQuery || 'No query available'}
        </Paper>
      </Paper>
    );
  }

  // Format execution time in ms or seconds
  const formatTime = (ms) => {
    if (!ms && ms !== 0) return 'N/A';
    return ms >= 1000 
      ? `${(ms / 1000).toFixed(2)} s` 
      : `${Math.round(ms)} ms`;
  };

  // Format logical reads
  const formatReads = (reads) => {
    if (!reads && reads !== 0) return 'N/A';
    return reads >= 1000 
      ? `${(reads / 1000).toFixed(1)}K` 
      : reads;
  };

  // Calculate improvement percentages safely
  const calculateImprovement = (before, after) => {
    if (!before || !after) return 0;
    return Math.round((before - after) / before * 100);
  };

  // Default values for performance metrics
  const defaultMetrics = {
    executionTime: 0,
    cpuTime: 0,
    logicalReads: 0
  };

  // Ensure we have objects with default values
  const before = { ...defaultMetrics, ...performanceBefore };
  const after = { ...defaultMetrics, ...performanceAfter };

  return (
    <Paper elevation={3} sx={{ p: 3, mt: 3 }}>
      <Box display="flex" alignItems="center" mb={2}>
        <AutoFixHighIcon color="primary" sx={{ mr: 1 }} />
        <Typography variant="h5" component="h2">
          Query Optimization Results
        </Typography>
        {aiPowered && (
          <Chip 
            label="AI-Powered" 
            color="primary" 
            size="small" 
            sx={{ ml: 2 }} 
            icon={<AutoFixHighIcon />}
          />
        )}
      </Box>

      {!aiPowered && (
        <Alert severity="info" sx={{ mb: 3 }}>
          Azure OpenAI integration is not configured. This is a simulated optimization.
          To enable AI-powered optimizations, add your Azure OpenAI credentials to the .env file.
        </Alert>
      )}

      {!optimizedQueryWorks && aiPowered && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          The AI-optimized query could not be executed successfully. The performance improvement is estimated.
        </Alert>
      )}

      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} md={4}>
          <Paper 
            elevation={1} 
            sx={{ 
              p: 2, 
              height: '100%', 
              display: 'flex', 
              flexDirection: 'column', 
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'success.light',
              color: 'white'
            }}
          >
            <Typography variant="h6" gutterBottom>
              Performance Improvement
            </Typography>
            <Box display="flex" alignItems="center">
              <Typography variant="h3">
                {improvementPercent}%
              </Typography>
              <TrendingUpIcon sx={{ ml: 1, fontSize: '2rem' }} />
            </Box>
            <Typography variant="body2" sx={{ mt: 1 }}>
              Faster execution
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} md={8}>
          <TableContainer component={Paper} elevation={1}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Metric</TableCell>
                  <TableCell align="right">Original Query</TableCell>
                  <TableCell align="right">Optimized Query</TableCell>
                  <TableCell align="right">Improvement</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell component="th" scope="row">
                    <Box display="flex" alignItems="center">
                      <SpeedIcon fontSize="small" sx={{ mr: 1 }} />
                      Execution Time
                    </Box>
                  </TableCell>
                  <TableCell align="right">{formatTime(before.executionTime)}</TableCell>
                  <TableCell align="right">{formatTime(after.executionTime)}</TableCell>
                  <TableCell align="right">
                    <Box display="flex" alignItems="center" justifyContent="flex-end">
                      <TrendingDownIcon color="success" fontSize="small" sx={{ mr: 0.5 }} />
                      {calculateImprovement(before.executionTime, after.executionTime)}%
                    </Box>
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell component="th" scope="row">
                    <Box display="flex" alignItems="center">
                      <SpeedIcon fontSize="small" sx={{ mr: 1 }} />
                      CPU Time
                    </Box>
                  </TableCell>
                  <TableCell align="right">{formatTime(before.cpuTime)}</TableCell>
                  <TableCell align="right">{formatTime(after.cpuTime)}</TableCell>
                  <TableCell align="right">
                    <Box display="flex" alignItems="center" justifyContent="flex-end">
                      <TrendingDownIcon color="success" fontSize="small" sx={{ mr: 0.5 }} />
                      {calculateImprovement(before.cpuTime, after.cpuTime)}%
                    </Box>
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell component="th" scope="row">
                    <Box display="flex" alignItems="center">
                      <SpeedIcon fontSize="small" sx={{ mr: 1 }} />
                      Logical Reads
                    </Box>
                  </TableCell>
                  <TableCell align="right">{formatReads(before.logicalReads)}</TableCell>
                  <TableCell align="right">{formatReads(after.logicalReads)}</TableCell>
                  <TableCell align="right">
                    <Box display="flex" alignItems="center" justifyContent="flex-end">
                      <TrendingDownIcon color="success" fontSize="small" sx={{ mr: 0.5 }} />
                      {calculateImprovement(before.logicalReads, after.logicalReads)}%
                    </Box>
                  </TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
        </Grid>
      </Grid>

      <Divider sx={{ my: 3 }} />

      <Accordion defaultExpanded>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Box display="flex" alignItems="center">
            <CodeIcon sx={{ mr: 1 }} />
            <Typography variant="h6">Query Comparison</Typography>
          </Box>
        </AccordionSummary>
        <AccordionDetails>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle1" gutterBottom>Original Query</Typography>
              <Paper 
                elevation={0} 
                sx={{ 
                  p: 2, 
                  bgcolor: 'grey.100', 
                  maxHeight: '300px', 
                  overflow: 'auto',
                  fontFamily: 'monospace',
                  whiteSpace: 'pre-wrap',
                  fontSize: '0.875rem'
                }}
              >
                {originalQuery || 'No original query available'}
              </Paper>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle1" gutterBottom>
                Optimized Query
                {aiPowered && <Chip label="AI-Generated" size="small" color="primary" sx={{ ml: 1 }} />}
              </Typography>
              <Paper 
                elevation={0} 
                sx={{ 
                  p: 2, 
                  bgcolor: 'success.50', 
                  border: '1px solid',
                  borderColor: 'success.light',
                  maxHeight: '300px', 
                  overflow: 'auto',
                  fontFamily: 'monospace',
                  whiteSpace: 'pre-wrap',
                  fontSize: '0.875rem'
                }}
              >
                {optimizedQuery || 'No optimized query available'}
              </Paper>
            </Grid>
          </Grid>
        </AccordionDetails>
      </Accordion>

      <Accordion>
        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
          <Box display="flex" alignItems="center">
            <AutoFixHighIcon sx={{ mr: 1 }} />
            <Typography variant="h6">Optimization Explanation</Typography>
          </Box>
        </AccordionSummary>
        <AccordionDetails>
          <Typography variant="body1" component="div" sx={{ whiteSpace: 'pre-line' }}>
            {explanation || 'No detailed explanation available.'}
          </Typography>
        </AccordionDetails>
      </Accordion>

      {indexRecommendations && indexRecommendations.length > 0 && (
        <Accordion>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Box display="flex" alignItems="center">
              <AutoFixHighIcon sx={{ mr: 1 }} />
              <Typography variant="h6">Index Recommendations</Typography>
            </Box>
          </AccordionSummary>
          <AccordionDetails>
            <Box>
              {indexRecommendations.map((index, i) => (
                <Paper key={i} elevation={0} sx={{ p: 2, mb: 2, bgcolor: 'info.50', border: '1px solid', borderColor: 'info.light' }}>
                  <Typography variant="body2" fontFamily="monospace" whiteSpace="pre-wrap">
                    {index}
                  </Typography>
                </Paper>
              ))}
            </Box>
          </AccordionDetails>
        </Accordion>
      )}
    </Paper>
  );
};

export default QueryOptimizationResult; 