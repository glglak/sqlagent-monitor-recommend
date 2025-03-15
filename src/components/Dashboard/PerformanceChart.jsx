import React from 'react';
import { Box, Typography, Alert } from '@mui/material';
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

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

const PerformanceChart = ({ metrics }) => {
  if (!metrics || !metrics.timestamps || metrics.timestamps.length === 0) {
    return (
      <Alert severity="info">
        No performance data available. Data will appear here once collected.
      </Alert>
    );
  }

  // Check if we have the status property indicating the database is offline
  if (metrics.status === 'offline') {
    return (
      <Alert severity="warning">
        {metrics.message || 'Database is offline. Performance metrics are not available.'}
      </Alert>
    );
  }

  const chartData = {
    labels: metrics.timestamps,
    datasets: [
      {
        label: 'CPU Utilization (%)',
        data: metrics.cpu,
        borderColor: 'rgb(76, 175, 80)',
        backgroundColor: 'rgba(76, 175, 80, 0.1)',
        tension: 0.3,
        fill: true,
      },
      {
        label: 'Memory Usage (%)',
        data: metrics.memory,
        borderColor: 'rgb(33, 150, 243)',
        backgroundColor: 'rgba(33, 150, 243, 0.1)',
        tension: 0.3,
        fill: true,
      },
      {
        label: 'Disk I/O Latency (ms)',
        data: metrics.diskIO,
        borderColor: 'rgb(255, 152, 0)',
        backgroundColor: 'rgba(255, 152, 0, 0.1)',
        tension: 0.3,
        fill: true,
        yAxisID: 'y1',
      },
      {
        label: 'Network I/O (MB/s)',
        data: metrics.networkIO,
        borderColor: 'rgb(156, 39, 176)',
        backgroundColor: 'rgba(156, 39, 176, 0.1)',
        tension: 0.3,
        fill: true,
        yAxisID: 'y1',
      },
    ],
  };

  const options = {
    responsive: true,
    interaction: {
      mode: 'index',
      intersect: false,
    },
    stacked: false,
    plugins: {
      title: {
        display: true,
        text: 'Performance Metrics Over Time',
      },
      tooltip: {
        callbacks: {
          label: function(context) {
            let label = context.dataset.label || '';
            if (label) {
              label += ': ';
            }
            if (context.parsed.y !== null) {
              label += context.parsed.y;
            }
            return label;
          }
        }
      }
    },
    scales: {
      y: {
        type: 'linear',
        display: true,
        position: 'left',
        title: {
          display: true,
          text: 'Percentage (%)',
        },
        min: 0,
        max: 100,
      },
      y1: {
        type: 'linear',
        display: true,
        position: 'right',
        title: {
          display: true,
          text: 'Value',
        },
        min: 0,
        grid: {
          drawOnChartArea: false,
        },
      },
    },
  };

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Performance History
      </Typography>
      <Box sx={{ height: 400 }}>
        <Line data={chartData} options={options} />
      </Box>
    </Box>
  );
};

export default PerformanceChart; 