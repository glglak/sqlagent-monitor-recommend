import React, { useState, useEffect } from 'react';
import { useDatabase } from '../../context/DatabaseContext';
import { fetchPerformanceMetrics, checkDatabaseStatus } from '../../api/dotnetApi';
import { Container, Grid, Paper, Typography, Box, CircularProgress, Alert } from '@mui/material';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';
import PerformanceMetrics from './PerformanceMetrics';
import ServerStatus from './ServerStatus';
import './Dashboard.css';
import UtilizationCard from './UtilizationCard';
import PerformanceChart from './PerformanceChart';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

const Dashboard = () => {
  const { selectedDatabase, loading: dbLoading, error: dbError, refreshDatabase } = useDatabase();
  const [metrics, setMetrics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [databaseStatus, setDatabaseStatus] = useState(null);

  console.log('Dashboard rendering with:', { selectedDatabase, loading, error });

  // Check database status periodically
  useEffect(() => {
    if (!selectedDatabase) return;
    
    const checkStatus = async () => {
      try {
        const status = await checkDatabaseStatus(selectedDatabase.Name);
        console.log('Current database status:', status);
        setDatabaseStatus(status);
        
        // If the status has changed, refresh the database in context
        if (status.status !== selectedDatabase.Status) {
          console.log('Database status changed, refreshing...');
          refreshDatabase(selectedDatabase.Name, status.status);
        }
      } catch (err) {
        console.error('Failed to check database status:', err);
      }
    };
    
    // Check immediately
    checkStatus();
    
    // Then check every 10 seconds
    const intervalId = setInterval(checkStatus, 10000);
    
    return () => clearInterval(intervalId);
  }, [selectedDatabase, refreshDatabase]);

  useEffect(() => {
    const getMetrics = async () => {
      if (!selectedDatabase) {
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError(null);
        console.log('Fetching metrics for database:', selectedDatabase.Name);
        const data = await fetchPerformanceMetrics(selectedDatabase.Name);
        console.log('Received metrics:', data);
        setMetrics(data);
      } catch (err) {
        console.error('Error fetching metrics:', err);
        setError(err.message || 'Failed to load performance metrics');
      } finally {
        setLoading(false);
      }
    };

    getMetrics();

    // Refresh metrics every 30 seconds
    const intervalId = setInterval(getMetrics, 30000);
    return () => clearInterval(intervalId);
  }, [selectedDatabase]);

  // Calculate current utilization values (last value in each array)
  const getCurrentValues = () => {
    if (!metrics || !metrics.cpu || !metrics.memory || !metrics.diskIO || !metrics.networkIO) {
      return {
        cpu: 0,
        memory: 0,
        diskIO: 0,
        networkIO: 0
      };
    }

    return {
      cpu: metrics.cpu[metrics.cpu.length - 1] || 0,
      memory: metrics.memory[metrics.memory.length - 1] || 0,
      diskIO: metrics.diskIO[metrics.diskIO.length - 1] || 0,
      networkIO: metrics.networkIO[metrics.networkIO.length - 1] || 0
    };
  };

  const currentValues = getCurrentValues();

  if (dbLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh">
        <CircularProgress />
      </Box>
    );
  }

  if (!selectedDatabase) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="info">Please select a database to view the dashboard.</Alert>
      </Container>
    );
  }

  // Check if database is offline
  if (selectedDatabase.status === 'offline') {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={4} lg={3}>
            <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column', height: 240 }}>
              <ServerStatus database={selectedDatabase} />
            </Paper>
          </Grid>
          <Grid item xs={12}>
            <Alert severity="warning">
              Database is offline. Performance metrics are not available.
            </Alert>
          </Grid>
        </Grid>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Grid container spacing={3}>
        {/* Server Status */}
        <Grid item xs={12} md={4} lg={3}>
          <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column', height: 240 }}>
            <ServerStatus database={selectedDatabase} />
          </Paper>
        </Grid>

        {/* CPU Utilization */}
        <Grid item xs={12} md={4} lg={3}>
          <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column', height: 240 }}>
            <UtilizationCard 
              title="CPU Utilization" 
              value={currentValues.cpu} 
              unit="%" 
              color="#4caf50"
              loading={loading}
            />
          </Paper>
        </Grid>

        {/* Memory Usage */}
        <Grid item xs={12} md={4} lg={3}>
          <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column', height: 240 }}>
            <UtilizationCard 
              title="Memory Usage" 
              value={currentValues.memory} 
              unit="%" 
              color="#2196f3"
              loading={loading}
            />
          </Paper>
        </Grid>

        {/* Disk I/O */}
        <Grid item xs={12} md={6} lg={3}>
          <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column', height: 240 }}>
            <UtilizationCard 
              title="Disk I/O Latency" 
              value={currentValues.diskIO} 
              unit="ms" 
              color="#ff9800"
              loading={loading}
              isLatency={true}
            />
          </Paper>
        </Grid>

        {/* Performance Charts */}
        <Grid item xs={12}>
          <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
            {loading ? (
              <Box display="flex" justifyContent="center" p={3}>
                <CircularProgress />
              </Box>
            ) : error ? (
              <Alert severity="error">{error}</Alert>
            ) : (
              <PerformanceChart metrics={metrics} />
            )}
          </Paper>
        </Grid>
      </Grid>
    </Container>
  );
};

export default Dashboard; 