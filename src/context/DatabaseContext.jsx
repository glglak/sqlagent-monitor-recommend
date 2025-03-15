import React, { createContext, useState, useEffect, useContext } from 'react';
import { fetchDatabases, checkDatabaseStatus } from '../api/dotnetApi';

// Create the context
const DatabaseContext = createContext();

// Create the provider component
export const DatabaseProvider = ({ children }) => {
  const [databases, setDatabases] = useState([]);
  const [selectedDatabase, setSelectedDatabase] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const getDatabases = async () => {
    try {
      setLoading(true);
      setError(null);
      console.log('Fetching databases...');
      
      const data = await fetchDatabases();
      console.log('Received databases from API:', data);
      
      if (data && Array.isArray(data) && data.length > 0) {
        // Transform the data to ensure consistent structure
        const transformedData = data.map(db => ({
          ...db,
          id: db.Name, // Use Name as the id
          name: db.Name // Keep name for display
        }));
        setDatabases(transformedData);
        
        // If there's at least one database, select the first one
        if (transformedData.length > 0) {
          setSelectedDatabase(transformedData[0]);
        }
      } else {
        // If API returns empty array, show error
        setError('No databases found. Please ensure your SQL Server is running and accessible.');
      }
    } catch (err) {
      console.error('Error in database context:', err);
      setError(err.message || 'Failed to load databases');
    } finally {
      setLoading(false);
    }
  };

  // Fetch databases on component mount
  useEffect(() => {
    getDatabases();
  }, []);

  const selectDatabase = (databaseName) => {
    console.log('Selecting database:', databaseName);
    const database = databases.find(db => db.Name === databaseName || db.id === databaseName);
    if (database) {
      setSelectedDatabase(database);
    } else {
      console.error(`Database with name/id ${databaseName} not found`);
    }
  };

  const refreshDatabase = (databaseName, newStatus) => {
    console.log('Refreshing database:', databaseName, 'with new status:', newStatus);
    setDatabases(prevDatabases => {
      const updatedDatabases = prevDatabases.map(db => {
        if (db.Name === databaseName) {
          return { ...db, Status: newStatus };
        }
        return db;
      });
      
      // Also update selected database if it's the one being refreshed
      if (selectedDatabase && selectedDatabase.Name === databaseName) {
        setSelectedDatabase(prev => ({ ...prev, Status: newStatus }));
      }
      
      return updatedDatabases;
    });
  };

  // Periodically check the status of all databases
  useEffect(() => {
    if (databases.length === 0) return;
    
    const checkAllDatabasesStatus = async () => {
      try {
        const updatedDatabases = [...databases];
        let selectedUpdated = false;
        
        for (let i = 0; i < updatedDatabases.length; i++) {
          const db = updatedDatabases[i];
          try {
            const status = await checkDatabaseStatus(db.id);
            
            if (status.status !== db.status) {
              console.log(`Database ${db.name} status changed from ${db.status} to ${status.status}`);
              updatedDatabases[i] = { ...db, status: status.status };
              
              // Update selected database if it's the one that changed
              if (selectedDatabase && selectedDatabase.id === db.id) {
                setSelectedDatabase({ ...selectedDatabase, status: status.status });
                selectedUpdated = true;
              }
            }
          } catch (err) {
            console.error(`Error checking status for database ${db.name}:`, err);
          }
        }
        
        // Only update the databases state if there was a change
        if (!selectedUpdated) {
          setDatabases(updatedDatabases);
        }
      } catch (err) {
        console.error('Error checking database statuses:', err);
      }
    };
    
    // Check every 30 seconds
    const intervalId = setInterval(checkAllDatabasesStatus, 30000);
    
    return () => clearInterval(intervalId);
  }, [databases, selectedDatabase]);

  // Function to refresh the list of databases
  const refreshDatabaseList = async () => {
    try {
      setLoading(true);
      setError(null);
      console.log('Refreshing database list...');
      
      const data = await fetchDatabases();
      console.log('Received updated databases from API:', data);
      
      if (data && Array.isArray(data) && data.length > 0) {
        setDatabases(data);
        
        // If the currently selected database is no longer in the list, select the first one
        if (selectedDatabase && !data.find(db => db.id === selectedDatabase.id)) {
          setSelectedDatabase(data[0]);
        }
      } else {
        // If API returns empty array, show error
        setError('No databases found. Please ensure your SQL Server is running and accessible.');
      }
    } catch (err) {
      console.error('Error refreshing database list:', err);
      setError(err.message || 'Failed to refresh database list');
    } finally {
      setLoading(false);
    }
  };

  const value = {
    databases,
    selectedDatabase,
    loading,
    error,
    selectDatabase,
    refreshDatabase,
    refreshDatabaseList: getDatabases
  };

  return (
    <DatabaseContext.Provider value={value}>
      {children}
    </DatabaseContext.Provider>
  );
};

// Create a custom hook to use the context
export const useDatabase = () => {
  const context = useContext(DatabaseContext);
  if (!context) {
    throw new Error('useDatabase must be used within a DatabaseProvider');
  }
  return context;
};

export default DatabaseContext;