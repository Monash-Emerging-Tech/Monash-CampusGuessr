# MNET CampusGuessr

A Unity-based geoguessr game integrated with Monash University's MazeMaps API. Players explore 360° locations and guess their positions on an interactive campus map.

## Overview

The project has two parts:

- **Unity** – Game logic, 360° locations, round and score management, and communication with the web layer.
- **Web** – Served from the `web/` folder (Vite + Node.js): HTML/CSS/JS, MazeMaps integration, map UI, and the bridge that Unity calls (e.g. show/hide map, submit guess, place markers).

For **prerequisites, install steps, Vite setup, and the development workflow**, see **[INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)** (Unity version, Node.js, npm, building the web app, and how to run and build the project).

## Members

Rohan
Oscar
Ange
Madison
Moon
Zaina

## Features

- **360° Location Exploration**: Immersive skybox-based location viewing
- **Interactive Campus Maps**: Integration with Monash MazeMaps API
- **Multi-Round Gameplay**: Configurable rounds with scoring system
- **Real-time Scoring**: Distance-based scoring with customizable curves
- **WebGL Support**: Deployable as web application
- **Debug Tools**: Comprehensive debugging and logging system

## Architecture

### Core Components

#### GameLogic.cs

- Main game controller and round management
- Singleton pattern for persistent game state
- Event system for UI updates and game progression
- Score calculation and round transitions

#### LocationManager.cs

- Manages location data from JSON resources
- Handles map pack selection by name (e.g., "all", "europe", "asia")
- Assigns skybox materials for 360° viewing
- Provides map pack validation and debugging
- MonoBehaviour-based for Unity integration

#### MapInteractionManager.cs

- Unity-JavaScript communication bridge
- Handles map clicks and coordinate processing
- Score calculation based on distance
- Singleton pattern for persistent map state

#### MapUIController.cs

- Controls map scaling and animation
- Manages map interaction UI elements
- Handles button states and user interactions
- Integrates with the web map overlay

#### ScoreBannerController.cs

- Dynamic score banner with automatic resizing
- Displays map pack name with overflow handling
- Balanced expansion (grows both left and right)
- Minimum width enforcement and real-time updates

#### GuessButtonManager.cs

- Manages the in-Unity guess / Next button (text, colors, interactability)
- States: waiting for pin ("Place your pin on the map"), ready ("Guess"), results ("Next Round")
- Listens to game state so the button reflects guessing vs results phase

#### RotateCamera.cs

- Handles 360° exploration: click-and-drag to rotate the camera around the location
- Configurable sensitivity and vertical angle limits
- Used so the player can look around the skybox before opening the map

#### RoundResultBarManager.cs

- Spawns the round result bar at the end of each round (score, "Next Round" action)
- Subscribes to GameLogic events (OnRoundStarted, OnScoreUpdated, etc.)
- Manages showing and hiding the bar in the game scene

#### BreakdownController.cs

- Runs on the breakdown / end-of-game scene
- Displays all previous rounds with location names and scores
- Uses ScoreData (ScriptableObject) to read the round history

#### MenuSceneBridge.cs

- Simple bridge for the main menu: Start button calls `GameLogic.Instance.LoadGame()` to load the game scene
- Assign to the Start button’s onClick in the Inspector

#### ScoreData (ScriptableObject)

- Persistent score and round data: current total, round score, and lists of previous locations and scores
- Used by GameLogic for no-repeat locations and by BreakdownController for the breakdown view
- Created via **Assets → Create → Game → Score Data**

#### Other scripts

- **DebugLogScript** – In-game debug overlay (see Troubleshooting).
- **SettingsMenuManager**, **VolumeManager**, **MouseSensitivityManager**, **ToggleSwitch** – Settings and options.
- **TooltipTrigger**, **FancyTextController** – UI polish. **CameraZoomButtons**, **ScoreBar** – In-game UI helpers.

### Web application (`web/`)

The web side is a Vite project that serves the game page, loads the Unity WebGL build, and hosts the MazeMaps UI and Unity–JavaScript bridge.

- **`web/src/app.js`** – Entry point; wires map init, DOM controls, and Unity globals.
- **`web/src/state.js`** – Shared state (e.g. pin, guessing state).
- **`web/src/mapCore.js`** – Map helpers, Maze Map init, and map resize.
- **`web/src/markersAndLines.js`** – Markers, guess line, z-level labels, clear map state.
- **`web/src/unityBridge.js`** – Unity instance, submit guess, add actual location, show/hide map, set guessing state.
- **`web/src/ui.js`** – Guess button, widget size controls, pin, expand/collapse, tooltip logic.

JavaScript functions exposed to Unity (on `window`) include: `showMapFromUnity`, `hideMapFromUnity`, `addActualLocationFromUnity`, `setGuessingStateFromUnity`, `clearMapStateFromUnity`, `submitGuess`, `mmSetWidgetSize`.

To install dependencies, run the dev server, or build for production, see **[INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)**.

**Repository sync note**

- If you add or change web assets (for example: new files or folders under `web/src/`, `web/public/`, `assets/`, the Unity `Build/` output, `TemplateData/`, or `StreamingAssets/`), make sure to update both repositories:
  - Keep the development repository (`geoguessr`) updated so developers can run and rebuild locally.
  - Copy the built runtime assets (Unity `Build/` and any new static files) into the hosting repository (`geoguessrHost`) so the live site serves the correct resources.

Keeping both repositories in sync avoids missing files or 404s when the site is deployed.

## Setup Instructions

For **full setup** (Unity version, Node.js, web package, Vite, and development workflow), see **[INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md)**.

When you clone the repository, the Unity project already includes the required GameObjects, prefabs, scene setup, and references. Open the project in Unity and follow the integration guide to run the web app and build.

### Prerequisites

- Unity 2022.3 or later
- WebGL Build Support module
- Monash University network access (for MazeMaps API)

## Game Flow

1. **Game Initialization**:
   - LocationManager loads location data from JSON and resolves the selected map pack
   - GameLogic initializes round counter, total score, and subscribes to MapInteractionManager events
   - MapInteractionManager is ready for Unity–JavaScript communication (map show/hide, guess submission)

2. **Round Start** (on game start or after “Next Round”):
   - A random location is chosen from the current map pack (no repeat within the same game)
   - Skybox material is applied for 360° viewing; actual location coordinates are sent to the web map (hidden until after guess)
   - Web map is shown in guessing mode: player can place a pin; actual location marker is not yet visible

3. **Player Interaction**:
   - Player explores the 360° location using camera controls
   - Player opens the map (e.g. via in-game button), places a guess marker on the campus map, and submits the guess

4. **Guess Submission**:
   - Web sends the guess (lat, lng, z-level) to Unity; MapInteractionManager calculates distance and score using the configurable curve
   - Unity sends the actual location to the web so both markers and the line between them are shown
   - Guessing is disabled on the map; results UI appears with the round score and total score

5. **Round Completion**:
   - Player clicks “Next Round” (or equivalent); map is hidden and the round is ended
   - Score is added to the running total; round counter is incremented
   - If more rounds remain, a new round starts (new random location, map cleared and shown again in guess mode); otherwise the game ends (e.g. breakdown/final results scene)

## Configuration

### GameLogic Settings

- `totalRounds`: Number of rounds per game
- `mapPackName`: Name of map pack to use (e.g., "all", "europe", "asia")
- `enableDebugLogs`: Enable/disable debug output

### MapInteractionManager Settings

- `maxGuessDistance`: Maximum scoring distance (meters)
- `maxScore`: Maximum points per round
- `minScore`: Minimum points per round
- `scoreCurve`: Animation curve for score calculation

### LocationManager Settings

- `jsonResourcePath`: Path to location data in Resources folder

### ScoreBannerController Settings

- `enableDynamicResizing`: Enable/disable automatic banner resizing
- `mapPackText`: Text component for map pack name display
- `backgroundRect`: Background RectTransform reference
- `containerRect`: Main container RectTransform reference
- `mapTextContainerRect`: Map text container RectTransform reference

## Building and Deployment

### Web app build and deploy

- **Development**: `cd web && npm run dev` (see INTEGRATION_GUIDE).
- **Production**: `cd web && npm run build`; deploy the `web/dist/` folder. Ensure the Unity WebGL build output is in `web/public/unity/` before building so it is included in `dist/`.

### Deployment considerations

- Ensure MazeMaps API access from the target domain
- Test on target browsers and devices

## Troubleshooting

### Common Issues

1. **LocationManager is null**:
   - Ensure LocationManager prefab is assigned to GameLogic
   - Verify LocationManager script is attached to prefab
   - Check that locationData.json exists in Resources folder

2. **Map not loading**:
   - Check internet connection
   - Verify MazeMaps API access
   - Check browser console for JavaScript errors

3. **Scoring issues**:
   - Ensure actual location is set before guess submission
   - Verify coordinate format (latitude, longitude)
   - Check scoring curve configuration

4. **UI not updating**:
   - Verify UI GameObject references in GameLogic
   - Check event subscription in UI controllers
   - Enable debug logs to trace issues

### Debug Tools

- **DebugLogScript**: Displays real-time game state
- **Console Logging**: Comprehensive logging in all scripts
- **Browser Developer Tools**: JavaScript debugging
- **Unity Console**: C# error tracking

## Development

### Adding New Locations

1. Add location data to `locationData.json`
2. Create skybox material for location
3. Place material in `Assets/Resources/Materials/Locations/`
4. Update map pack configuration if needed

### Customizing UI

- Modify prefabs in `Assets/Prefabs/`
- Update web app HTML/CSS in `web/`
- Adjust UI scaling and positioning

### Extending Functionality

- Add new game modes in GameLogic
- Implement additional scoring systems
- Create new map pack types
- Add analytics and tracking

## Contributing

1. Fork the repository
2. Create feature branch
3. Make changes with proper documentation
4. Test thoroughly before submitting
5. Submit pull request with detailed description

## License

This project is developed for Monash University MNET and follows institutional guidelines.

## Support

For technical support:

- Check troubleshooting section above
- Review Unity console and browser logs
- Enable debug mode for detailed information
- Contact development team for complex issues
