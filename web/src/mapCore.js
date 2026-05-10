/**
 * Map helpers, Maze Map initialization, and debounced resize.
 */

import { getGuessingState } from "./state.js";

// --------------------------------------------------------------- MAP HELPERS

export function getLeafletNamespace() {
  return (
    window.L ||
    (window.mazemap && window.mazemap.L) ||
    (window.MazeMap && window.MazeMap.L) ||
    (window.Maze && window.Maze.L) ||
    null
  );
}

export function getLeafletMap(map) {
  if (!map) return null;
  if (typeof map.getMap === "function") {
    return map.getMap();
  }
  const candidates = [
    map._map,
    map.map,
    map.leafletMap,
    map._leafletMap,
    map._mapObject,
    map._mapRef,
  ];
  for (let i = 0; i < candidates.length; i++) {
    if (candidates[i] && typeof candidates[i].addLayer === "function") {
      return candidates[i];
    }
  }
  return null;
}

export function getMapboxMap(map) {
  if (!map) return null;
  if (
    typeof map.addSource === "function" &&
    typeof map.addLayer === "function"
  ) {
    return map;
  }
  if (typeof map.getMap === "function") {
    const inner = map.getMap();
    if (
      inner &&
      typeof inner.addSource === "function" &&
      typeof inner.addLayer === "function"
    ) {
      return inner;
    }
  }
  const candidates = [map._map, map.map, map._mapObject, map._mapRef];
  for (let i = 0; i < candidates.length; i++) {
    const candidate = candidates[i];
    if (
      candidate &&
      typeof candidate.addSource === "function" &&
      typeof candidate.addLayer === "function"
    ) {
      return candidate;
    }
  }
  return null;
}

// --------------------------------------------------------------- MAZE MAP INITIALIZATION

// Default floor (zLevel) to show when entering each campus.
// If a third campus is added, this table needs a manual update.
const CAMPUS_DEFAULT_ZLEVEL = { 159: 1, 413: 10 };

function isMazeMapReady() {
  if (typeof mazemap !== "undefined" && typeof mazemap.Map === "function")
    return true;
  if (
    typeof window.mazemap !== "undefined" &&
    typeof window.mazemap.Map === "function"
  )
    return true;
  if (
    typeof window.MazeMap !== "undefined" &&
    typeof window.MazeMap.Map === "function"
  )
    return true;
  for (const key in window) {
    try {
      if (Object.prototype.hasOwnProperty.call(window, key)) {
        if (String(key).toLowerCase().includes("maze")) {
          const obj = window[key];
          if (
            obj &&
            typeof obj === "object" &&
            typeof obj.Map === "function"
          ) {
            window.mazemap = obj;
            return true;
          }
        }
      }
    } catch (e) {
      console.error("Error checking MazeMap readiness:", e);
    }
  }
  return false;
}

/**
 * Initialize the Maze Map. Calls onMapClick(map, lngLat, zLevel) when the user clicks the map (only when guessing).
 * @param {function} onMapClick - (map, lngLat, zLevel) => void
 */
export function initializeMazeMap(onMapClick) {
  if (!isMazeMapReady()) {
    if (window.mazeMapRetryCount === undefined) window.mazeMapRetryCount = 0;
    if (window.mazeMapRetryCount < 10) {
      window.mazeMapRetryCount++;
      setTimeout(() => initializeMazeMap(onMapClick), 1000);
      return;
    }
    console.error("Maze Maps failed to load after 10 attempts");
    return;
  }

  // Cache the click handler so reinit (campus switch) can reuse it without Unity re-registering.
  if (onMapClick) window._mazeMapClickHandler = onMapClick;
  const handler = window._mazeMapClickHandler;

  try {
    const MazeLibrary = window.Maze || mazemap;
    const pendingCenter = window.pendingMapCenter;
    const campusId = pendingCenter ? (pendingCenter.campusId || 159) : 159;
    const lat = pendingCenter ? pendingCenter.lat : -37.9106;
    const lng = pendingCenter ? pendingCenter.lng : 145.1361;
    const zoom = pendingCenter ? pendingCenter.zoom : 16;

    const map = new MazeLibrary.Map({
      container: "map",
      campuses: campusId,
      center: { lng, lat },
      zoom,
      minZLevel: 0,
      maxZLevel: 12,
    });
    window.mazeMapInstance = map;
    window.currentMapView = { lat, lng, zoom, campusId, zLevel: CAMPUS_DEFAULT_ZLEVEL[campusId] ?? 1 };

    map.on("load", () => {
      console.log("Maze Maps ready for interaction");
    });

    map.on("click", (e) => {
      if (!getGuessingState()) {
        console.log("Map click ignored - guessing disabled");
        return;
      }
      if (typeof handler === "function") {
        handler(map, e.lngLat, map.zLevel);
      }
    });
  } catch (error) {
    console.error(
      "Failed to initialize Maze Maps:",
      error && error.message ? error.message : error
    );
  }
}

// --------------------------------------------------------------- MAP RESIZE

let mapResizeScheduled = false;

export function queueMapResize() {
  if (mapResizeScheduled) return;
  mapResizeScheduled = true;
  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      mapResizeScheduled = false;
      resizeMazeMap();
    });
  });
  setTimeout(() => {
    mapResizeScheduled = false;
    resizeMazeMap();
  }, 200);
}

export function resizeMazeMap() {
  const map = window.mazeMapInstance;
  if (map && typeof map.resize === "function") {
    try {
      map.resize();
    } catch (e) {
      console.error("Error during map.resize():", e);
    }
    const mapboxMap = getMapboxMap(map);
    if (mapboxMap) {
      const guessMarker = map._clickMarker;
      const actualMarker = map._actualLocationMarker;
      const guessLngLat =
        guessMarker &&
        (typeof guessMarker.getLngLat === "function"
          ? guessMarker.getLngLat()
          : guessMarker._storedLngLat);
      const actualLngLat =
        actualMarker &&
        (typeof actualMarker.getLngLat === "function"
          ? actualMarker.getLngLat()
          : actualMarker._storedLngLat);
      if (
        guessLngLat &&
        actualLngLat &&
        typeof mapboxMap.fitBounds === "function"
      ) {
        const bounds = [
          [guessLngLat.lng, guessLngLat.lat],
          [actualLngLat.lng, actualLngLat.lat],
        ];
        mapboxMap.fitBounds(bounds, { padding: 80 });
      } else if (actualLngLat && typeof map.flyTo === "function") {
        map.flyTo({
          center: [actualLngLat.lng, actualLngLat.lat],
          zoom: map.getZoom(),
        });
      }
    }
  }
}