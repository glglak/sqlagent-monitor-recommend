import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { Box, Toolbar, Container, Typography, AppBar } from '@mui/material';
import Dashboard from './components/Dashboard/Dashboard';
import Sidebar from './components/Sidebar/Sidebar';
import QueryAnalyzer from './components/QueryAnalyzer/QueryAnalyzer';
import IndexRecommendations from './components/IndexRecommendations/IndexRecommendations';
import { DatabaseProvider } from './context/DatabaseContext';
import DatabaseSelector from './components/DatabaseSelector/DatabaseSelector';
import StorageIcon from '@mui/icons-material/Storage';

const theme = createTheme({
  palette: {
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
    background: {
      default: '#f5f5f5',
    },
  },
});

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <DatabaseProvider>
        <Router>
          <Box sx={{ display: 'flex' }}>
            <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
              <Toolbar sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Box display="flex" alignItems="center">
                  <StorageIcon sx={{ mr: 1 }} />
                  <Typography variant="h6" noWrap component="div">
                    SQL Performance Monitor & Recommend
                  </Typography>
                </Box>
                <DatabaseSelector />
              </Toolbar>
            </AppBar>
            <Sidebar />
            <Box
              component="main"
              sx={{
                backgroundColor: (theme) =>
                  theme.palette.mode === 'light'
                    ? theme.palette.grey[100]
                    : theme.palette.grey[900],
                flexGrow: 1,
                height: '100vh',
                overflow: 'auto',
                pt: 8, // Add padding top to account for the AppBar
              }}
            >
              <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
                <Routes>
                  <Route path="/" element={<Dashboard />} />
                  <Route path="/query-analyzer" element={<QueryAnalyzer />} />
                  <Route path="/index-recommendations" element={<IndexRecommendations />} />
                </Routes>
              </Container>
            </Box>
          </Box>
        </Router>
      </DatabaseProvider>
    </ThemeProvider>
  );
}

export default App;