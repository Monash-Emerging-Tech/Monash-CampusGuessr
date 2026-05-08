import { initializeMazeMap } from "./mapCore.js";
import {
  createSingleMarker,
  drawGuessToActualLine,
  clearMapStateFromUnity,
} from "./markersAndLines.js";
import {
  submitGuess,
  addActualLocationFromUnity,
  showMapFromUnity,
  hideMapFromUnity,
  setGuessingStateFromUnity,
} from "./unityBridge.js";
import {
  updateGuessButtonState,
  initializeGuessButton,
  wireControls,
  setWidgetSize,
} from "./ui.js";

// --------------------------------------------------------------- MAP INIT (with click handler)

function onMapClick(map, lngLat, zLevel) {
  createSingleMarker(map, lngLat, zLevel);
  updateGuessButtonState(true);
  drawGuessToActualLine();
}

window.addEventListener("load", () => {
  setTimeout(() => initializeMazeMap(onMapClick), 1000);
});

// --------------------------------------------------------------- DOM READY

function bootstrap() {
  wireControls();
  initializeGuessButton(submitGuess);
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", bootstrap);
} else {
  bootstrap();
}

// --------------------------------------------------------------- UNITY GLOBALS

window.submitGuess = submitGuess;
window.addActualLocationFromUnity = addActualLocationFromUnity;
window.showMapFromUnity = showMapFromUnity;
window.hideMapFromUnity = hideMapFromUnity;
window.setGuessingStateFromUnity = setGuessingStateFromUnity;
window.mmSetWidgetSize = setWidgetSize;

window.clearMapStateFromUnity = () => {
  clearMapStateFromUnity();
  updateGuessButtonState(false);
};

// Called from Unity via jslib setMapPackViewFromUnity.
// Same campus then jumpTo only (no flicker). Different campus then destroy and reinit.
window.mmSetMapPackView = function (campusId, lat, lng, zoom) {
  const map = window.mazeMapInstance;
  const currentView = window.currentMapView;

  if (map && currentView && currentView.campusId === campusId) {
    map.jumpTo({ center: { lng, lat }, zoom });
    window.currentMapView = { lat, lng, zoom, campusId };
    return;
  }

  // Different campus: tear down the existing map and reinit with the new one.
  window.pendingMapCenter = { lat, lng, zoom, campusId };
  if (map) {
    map.remove();
    window.mazeMapInstance = null;
    // map.remove() empties the container div but leaves it in the DOM (standard Mapbox behaviour).
    // Recreate it defensively in case a future SDK version removes it entirely.
    if (!document.getElementById("map")) {
      const container = document.createElement("div");
      container.id = "map";
      const parent = document.getElementById("maze-maps-container");
      if (parent) parent.appendChild(container);
    }
  }
  initializeMazeMap(window._mazeMapClickHandler);
};
