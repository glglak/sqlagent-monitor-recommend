import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { 
  Drawer, 
  List, 
  ListItem, 
  ListItemIcon, 
  ListItemText, 
  Toolbar,
  Divider,
  Box
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import SpeedIcon from '@mui/icons-material/Speed';
import TuneIcon from '@mui/icons-material/Tune';

const drawerWidth = 240;

const Sidebar = () => {
  const location = useLocation();
  
  const menuItems = [
    { text: 'Dashboard', icon: <DashboardIcon />, path: '/' },
    { text: 'Query Analyzer', icon: <SpeedIcon />, path: '/query-analyzer' },
    { text: 'Index Recommendations', icon: <TuneIcon />, path: '/index-recommendations' }
  ];

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: drawerWidth,
        flexShrink: 0,
        [`& .MuiDrawer-paper`]: { 
          width: drawerWidth, 
          boxSizing: 'border-box',
          backgroundColor: '#f8f9fa'
        },
      }}
    >
      <Toolbar /> {/* This creates space for the AppBar */}
      <Box sx={{ overflow: 'auto', mt: 2 }}>
        <List>
          {menuItems.map((item) => (
            <ListItem 
              button 
              key={item.text} 
              component={Link} 
              to={item.path}
              selected={location.pathname === item.path}
              sx={{
                '&.Mui-selected': {
                  backgroundColor: 'rgba(25, 118, 210, 0.08)',
                  borderRight: '3px solid #1976d2',
                  '&:hover': {
                    backgroundColor: 'rgba(25, 118, 210, 0.12)',
                  }
                },
                '&:hover': {
                  backgroundColor: 'rgba(0, 0, 0, 0.04)',
                }
              }}
            >
              <ListItemIcon>
                {item.icon}
              </ListItemIcon>
              <ListItemText primary={item.text} />
            </ListItem>
          ))}
        </List>
        <Divider sx={{ my: 2 }} />
        <Box sx={{ p: 2, fontSize: '0.875rem', color: 'text.secondary' }}>
          <Box sx={{ fontWeight: 'bold', mb: 1 }}>Connection Info</Box>
          <Box sx={{ mb: 0.5 }}>Server: {window.location.hostname}</Box>
          <Box>Status: Active</Box>
        </Box>
      </Box>
    </Drawer>
  );
};

export default Sidebar; 