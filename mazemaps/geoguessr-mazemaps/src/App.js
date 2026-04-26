import React, { useEffect, useState } from 'react';
import * as Mazemap from 'mazemap';
import { MazeMapWrapper, makeMazeMapInstance } from './MazeMap';
import GuessButton from './components/GuessButton';
import './App.css';

function App() {
  const [map, setMap] = useState(null);
  const [playerGuessMarker, setPlayerGuessMarker] = useState(null);
  const [actualMarker, setActualMarker] = useState(null);
  const [hasGuessed, setHasGuessed] = useState(false);

  useEffect(() => {
    // Initialize Mazemap when component mounts
    const campusId = 159; // Clayton Campus
    const mazemapInstance = makeMazeMapInstance({
      campuses: campusId,
      center: { lng: 145.133963, lat: -37.911785 },
      zoom: 15.4,
      // Restrict to levels P4 and above (including parking levels P1-P4)
      zLevel: 0, // Start at ground level (0 = LG)
      minZLevel: -4, // Minimum level allowed (-4 = P4, include parking levels)
      maxZLevel: 12 // Maximum level allowed (Clayton campus has up to 12 levels)
    });
    
    setMap(mazemapInstance);

  // Add the actual marker to the map only after guess is made
  useEffect(() => {
    if (map && actualMarker && hasGuessed) {
      addMarkerToMap(actualMarker, 'actual');
    }
  }, [map, actualMarker, hasGuessed]);

  // Handle guess button click
  const handleGuessClick = () => {
    if (playerGuessMarker) {
      setHasGuessed(true);
      console.log('Guess submitted! Showing actual location.');
    } else {
      alert('Please place a marker on the map first!');
    }
  };

  // Handle new game button click
  const handleNewGame = () => {
    setHasGuessed(false);
    setPlayerGuessMarker(null);
    // Remove markers from map
    if (map._playerMarker) {
      map._playerMarker.remove();
      map._playerMarker = null;
    }
    if (map._actualMarker) {
      map._actualMarker.remove();
      map._actualMarker = null;
    }
    console.log('New game started!');
  };

  // Helper function to convert z-level to readable name
  const getZLevelName = (zLevel) => {
    if (zLevel === -4) return 'P4 (Parking Level 4)';
    if (zLevel === -3) return 'P3 (Parking Level 3)';
    if (zLevel === -2) return 'P2 (Parking Level 2)';
    if (zLevel === -1) return 'P1 (Parking Level 1)';
    if (zLevel === 0) return 'LG (Lower Ground)';
    if (zLevel === 1) return 'G (Ground)';
    if (zLevel === 2) return '1 (First Floor)';
    if (zLevel === 3) return '2 (Second Floor)';
    if (zLevel === 4) return '3 (Third Floor)';
    if (zLevel === 5) return '4 (Fourth Floor)';
    if (zLevel === 6) return '5 (Fifth Floor)';
    if (zLevel === 7) return '6 (Sixth Floor)';
    if (zLevel === 8) return '7 (Seventh Floor)';
    if (zLevel === 9) return '8 (Eighth Floor)';
    if (zLevel === 10) return '9 (Ninth Floor)';
    if (zLevel === 11) return '10 (Tenth Floor)';
    if (zLevel === 12) return '11 (Eleventh Floor)';
    if (zLevel < -4) return `B${Math.abs(zLevel)} (Basement ${Math.abs(zLevel)})`;
    return `${zLevel} (Level ${zLevel})`;
  };

  // Handle map click events and add/update marker
  const handleMapClick = (event) => {
    // Use the official MazeMap API to get comprehensive click data
    const clickData = event.target.getMapClickData ? event.target.getMapClickData(event) : null;
    
    // Extract z-level information (either from clickData or fallback)
    let zLevel = 'unknown';
    let zLevelName = 'unknown';
    
    if (clickData && clickData.zLevel !== undefined) {
      zLevel = clickData.zLevel;
      zLevelName = getZLevelName(zLevel);
    } else {
      // Fallback to manual extraction if getMapClickData is not available
      zLevel = event.target.getZLevel ? event.target.getZLevel() : 'unknown';
      zLevelName = typeof zLevel === 'number' ? getZLevelName(zLevel) : zLevel;
    }
    
    // Create new marker data
    const playerGuessMarker = {
      id: Date.now(),
      lng: event.lngLat.lng,
      lat: event.lngLat.lat,
      zLevel: zLevel,
      zLevelName: zLevelName,
      timestamp: new Date().toLocaleTimeString(),
      options: {
        imgUrl: 'images/handthing.svg',
        imgScale: 1.7,
        color: 'white',
        size: 60,
        innerCircle: false,
        shape: 'marker',
        zLevel: zLevel
      }
    };
    
    // Update marker state (replace existing player guess marker)
    setPlayerGuessMarker(playerGuessMarker);
    
    // Add player guess marker to the map
    addMarkerToMap(playerGuessMarker, 'player');
    
    console.log('Player guess marker placed at:', {
      coordinates: event.lngLat,
      zLevel: zLevel,
      zLevelName: zLevelName,
      lng: event.lngLat.lng,
      lat: event.lngLat.lat,
      clickData: clickData
    });
    
  };
  // Function to add marker to the map
  const addMarkerToMap = (markerData, markerType = 'player') => {
    if (!map) {
      console.error('Map not available for marker placement');
      return;
    }

    console.log(`Adding ${markerType} marker to map:`, markerData);

    try {
      // Create new marker using the official MazeMap API
      const newMarker = new Mazemap.MazeMarker(markerData.options)
        .setLngLat([markerData.lng, markerData.lat]) // Set the LngLat coordinates
        .addTo(map); // Add to the map

      console.log(`${markerType} marker created and added to map:`, newMarker);

      // Store reference to marker based on type
      if (markerType === 'player') {
        // Remove existing player marker if it exists
        if (map._playerMarker) {
          console.log('Removing existing player marker');
          map._playerMarker.remove();
        }
        map._playerMarker = newMarker;
      } else if (markerType === 'actual') {
        // Store actual marker reference
        map._actualMarker = newMarker;
      }
    } catch (error) {
      console.error(`Error creating ${markerType} marker:`, error);
    }
  };

  return (
    <div className="App">
      <header className="App-header">
        <h1>MNET CampusGuessr + Monash MazeMaps</h1>
        <p>
          {!hasGuessed 
            ? "Click on the map to place your guess, then click 'Submit Guess' to see the actual location!" 
            : "Game complete! Click 'New Game' to play again."
          }
        </p>
        {actualMarker && hasGuessed && (
          <div className="marker-info" style={{ marginBottom: '10px', padding: '10px', borderRadius: '5px' }}>
            <h3>Actual Location</h3>
            <p><strong>Level:</strong> {actualMarker.zLevelName}</p>
            <p><strong>Coordinates:</strong> {actualMarker.lng.toFixed(6)}, {actualMarker.lat.toFixed(6)}</p>
          </div>
        )}
        {playerGuessMarker && (
          <div className="marker-info">
            <h3>Your Guess</h3>
            <p><strong>Level:</strong> {playerGuessMarker.zLevelName}</p>
            <p><strong>Coordinates:</strong> {playerGuessMarker.lng.toFixed(6)}, {playerGuessMarker.lat.toFixed(6)}</p>
            <p><strong>ZLevel:</strong> {playerGuessMarker.zLevel}, {playerGuessMarker.zLevelName}</p>
          </div>
        )}
      </header>
      <main className="App-main" style={{ position: 'relative' }}>
        {map && <MazeMapWrapper map={map} onMapClick={handleMapClick} />}
        
        {/* Guess Button Component */}
        <GuessButton
          hasGuessed={hasGuessed}
          playerGuessMarker={playerGuessMarker}
          onGuessClick={handleGuessClick}
          onNewGame={handleNewGame}
        />
      </main>
    </div>
  );
}

export default App;
