import React from 'react';
import { Line } from 'react-chartjs-2';
import { Box, Typography, Tabs, Tab } from '@mui/material';
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

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

const PerformanceMetrics = ({ metrics }) => {
  const [selectedTab, setSelectedTab] = React.useState(0);

  const handleTabChange = (event, newValue) => {
    setSelectedTab(newValue);
  };

  if (!metrics) {
    return <Typography>No metrics available</Typography>;
  }

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top',
      },
      title: {
        display: true,
        text: 'Database Performance Metrics',
      },
    },
    scales: {
      y: {
        beginAtZero: true,
      },
    },
  };

  const cpuData = {
    labels: metrics.timestamps,
    datasets: [
      {
        label: 'CPU Utilization (%)',
        data: metrics.cpu,
        borderColor: 'rgb(255, 99, 132)',
        backgroundColor: 'rgba(255, 99, 132, 0.5)',
        tension: 0.3,
      },
    ],
  };

  const memoryData = {
    labels: metrics.timestamps,
    datasets: [
      {
        label: 'Memory Utilization (%)',
        data: metrics.memory,
        borderColor: 'rgb(53, 162, 235)',
        backgroundColor: 'rgba(53, 162, 235, 0.5)',
        tension: 0.3,
      },
    ],
  };

  const diskIOData = {
    labels: metrics.timestamps,
    datasets: [
      {
        label: 'Disk I/O (ms)',
        data: metrics.diskIO,
        borderColor: 'rgb(75, 192, 192)',
        backgroundColor: 'rgba(75, 192, 192, 0.5)',
        tension: 0.3,
      },
    ],
  };

  const networkIOData = {
    labels: metrics.timestamps,
    datasets: [
      {
        label: 'Network I/O (MB/s)',
        data: metrics.networkIO,
        borderColor: 'rgb(255, 159, 64)',
        backgroundColor: 'rgba(255, 159, 64, 0.5)',
        tension: 0.3,
      },
    ],
  };

  const chartData = [cpuData, memoryData, diskIOData, networkIOData];
  const tabLabels = ['CPU', 'Memory', 'Disk I/O', 'Network I/O'];

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Performance Metrics
      </Typography>
      
      <Tabs
        value={selectedTab}
        onChange={handleTabChange}
        indicatorColor="primary"
        textColor="primary"
        variant="fullWidth"
      >
        {tabLabels.map((label, index) => (
          <Tab key={index} label={label} />
        ))}
      </Tabs>
      
      <Box height={300} mt={2}>
        <Line options={chartOptions} data={chartData[selectedTab]} />
      </Box>
    </Box>
  );
};

export default PerformanceMetrics; 