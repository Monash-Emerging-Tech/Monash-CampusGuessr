# MNET CampusGuessr + Monash MazeMaps

A React application that integrates MazeMap (Monash University's indoor mapping system) with basic map click functionality.

## Features

- **Interactive MazeMap**: Displays Monash University Clayton Campus using MazeMap JS API
- **Map Click Functionality**: Click anywhere on the map to capture coordinates
- **Console Logging**: Click coordinates are logged to the browser console

## Map Click Functionality

The application now supports basic map click events that:

- **Capture Coordinates**: Longitude and latitude of each click
- **Track Z-Level**: Floor/level information where the click occurred
- **Console Logging**: Logs click data to the browser console for debugging
- **Simple Event Handling**: Clean, minimal implementation focused on data capture
- **Level Restrictions**: Only shows levels LG (Lower Ground) and above, excluding basement levels

### Z-Level Restrictions

The map is configured to display parking levels and building levels:

- **P4 (Parking Level 4)**: Level -4
- **P3 (Parking Level 3)**: Level -3
- **P2 (Parking Level 2)**: Level -2
- **P1 (Parking Level 1)**: Level -1
- **LG (Lower Ground)**: Level 0
- **G (Ground)**: Level 1
- **1 (First Floor)**: Level 2
- **2 (Second Floor)**: Level 3
- **3 (Third Floor)**: Level 4
- **4 (Fourth Floor)**: Level 5
- **5 (Fifth Floor)**: Level 6
- **6 (Sixth Floor)**: Level 7
- **7 (Seventh Floor)**: Level 8
- **8 (Eighth Floor)**: Level 9
- **9 (Ninth Floor)**: Level 10
- **10 (Tenth Floor)**: Level 11
- **11 (Eleventh Floor)**: Level 12

Only parking levels (P1-P4) and building levels (LG-11) are accessible. Other basement levels are automatically excluded from the map view.

### Custom Level Selector

The application includes a custom level selector that:

- **Replaces the default MazeMap control** with a clean, custom interface
- **Shows parking levels (P1-P4) and building levels (LG-11)** - only relevant levels are displayed
- **Provides clear level names** (e.g., "P1 (Parking Level 1)", "LG (Lower Ground)", "G (Ground)")
- **Prevents access to other basement levels** - users cannot navigate to irrelevant basement areas
- **Maintains full functionality** for all accessible levels including parking

### How It Works

1. **Event Handling**: Click events are captured by the MazeMap instance
2. **Data Processing**: Coordinates are extracted from the click event
3. **Console Output**: Click data is logged to the browser console
4. **Clean Architecture**: Simple event flow without complex state management

### Technical Implementation

- **MazeMap Integration**: Click events are wired up in `makeMazeMapInstance()`
- **React Event Handling**: Uses props to pass click handlers down to the map component
- **Event Cleanup**: Properly removes event listeners to prevent memory leaks
- **Minimal State**: No unnecessary state management for this basic functionality

### Interactive Markers

The application includes a marker system for placing guesses:

- **Single Marker**: Only one marker can exist on the map at a time
- **Guess Storage**: Each marker stores longitude, latitude, and z-level
- **Official API**: Uses MazeMap.MazeMarker for reliable marker placement
- **Z-Level Support**: Markers automatically appear on the correct floor level
- **Guess Display**: Shows current guess information in the header

### Enhanced Click Data

The application leverages MazeMap's official `getMapClickData()` method to provide:

- **Z-Level Information**: Floor/level where the click occurred
- **Building ID**: Identifier for the building structure
- **Room ID**: Specific room identifier if available
- **POI ID**: Point of Interest identifier if available
- **Coordinates**: Precise longitude and latitude
- **Fallback Support**: Graceful degradation if the method isn't available

## Getting Started

1. Install dependencies: `npm install`
2. Start the development server: `npm start`
3. Open the browser console (F12 → Console tab)
4. Click anywhere on the map to see coordinates logged to the console

## File Structure

- `src/MazeMap/index.jsx`: Core MazeMap component with click event handling
- `src/App.js`: Main application with simple click handler
- `src/App.css`: Basic styling for the application

## Console Output

When you click on the map, you'll see output like:

```
Marker placed at: {
  coordinates: {lng: 145.133963, lat: -37.911785},
  zLevel: -1,
  zLevelName: "P1 (Parking Level 1)",
  lng: 145.133963,
  lat: -37.911785,
  clickData: {
    zLevel: -1,
    buildingId: "building_123",
    roomId: "room_456",
    poiId: "poi_789"
  }
}
```

The `zLevel` represents the floor/level of the building where you clicked, and `zLevelName` provides a human-readable description including parking levels. Each click also places a marker on the map representing your guess location. The `clickData` object contains additional information from MazeMap's official API.

## Future Enhancements

- Add visual markers on the map for clicked locations
- Store click data in state for display in the UI
- Implement distance calculations between clicks
- Add click history tracking
- Create game mechanics for geoguessr-style gameplay
