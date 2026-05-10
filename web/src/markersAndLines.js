/**
 * Guess/actual markers, guess-to-actual line, z-level names, and clear map state.
 */

import { getGuessingState } from "./state.js";
import {
  getLeafletMap,
  getLeafletNamespace,
  getMapboxMap,
} from "./mapCore.js";

// --------------------------------------------------------------- Z-LEVEL NAME HELPER

export function getZLevelName(zLevel) {
  if (zLevel === -4) return "P4 (Parking Level 4)";
  if (zLevel === -3) return "P3 (Parking Level 3)";
  if (zLevel === -2) return "P2 (Parking Level 2)";
  if (zLevel === -1) return "P1 (Parking Level 1)";
  if (zLevel === 0) return "LG (Lower Ground)";
  if (zLevel === 1) return "G (Ground)";
  if (zLevel === 2) return "1 (First Floor)";
  if (zLevel === 3) return "2 (Second Floor)";
  if (zLevel === 4) return "3 (Third Floor)";
  if (zLevel === 5) return "4 (Fourth Floor)";
  if (zLevel === 6) return "5 (Fifth Floor)";
  if (zLevel === 7) return "6 (Sixth Floor)";
  if (zLevel === 8) return "7 (Seventh Floor)";
  if (zLevel === 9) return "8 (Eighth Floor)";
  if (zLevel === 10) return "9 (Ninth Floor)";
  if (zLevel === 11) return "10 (Tenth Floor)";
  if (zLevel === 12) return "11 (Eleventh Floor)";
  if (zLevel < -4)
    return "B" + Math.abs(zLevel) + " (Basement " + Math.abs(zLevel) + ")";
  return zLevel + " (Level " + zLevel + ")";
}

// --------------------------------------------------------------- MARKER AND LINE PLACEMENT

/**
 * Caller should call updateGuessButtonState(true) and drawGuessToActualLine() after placing marker.
 */
export function createSingleMarker(map, lngLat, zLevel) {
  if (map._clickMarker) {
    map._clickMarker.remove();
  }

  map._clickMarker = new Mazemap.MazeMarker({
    size: 60,
    zLevel: zLevel,
    imgUrl: "assets/img/markers/handthing.png",
    imgScale: 1.7,
    color: "white",
    innerCircle: false,
  })
    .setLngLat(lngLat)
    .addTo(map);

  map._clickMarker._storedZLevel = zLevel;
  map._clickMarker._storedLngLat = lngLat;

  console.log("Guess Marker placed at:", lngLat, "on zLevel:", zLevel);
}

function getOrCreateGuessLineLayer(map, zLevel) {
  if (map._guessLineLayer && map._guessLineLayerZ === zLevel) {
    return map._guessLineLayer;
  }

  if (map._guessLineLayer) {
    const leafletMap = getLeafletMap(map);
    if (leafletMap && typeof leafletMap.removeLayer === "function") {
      leafletMap.removeLayer(map._guessLineLayer);
    } else if (typeof map.removeLayer === "function") {
      map.removeLayer(map._guessLineLayer);
    }
  }

  let layer = null;
  if (window.mazemap && typeof window.mazemap.LayerGroup === "function") {
    layer = new window.mazemap.LayerGroup({ zLevel: zLevel });
    layer.addTo(map);
  } else if (
    typeof Mazemap !== "undefined" &&
    typeof Mazemap.LayerGroup === "function"
  ) {
    layer = new Mazemap.LayerGroup({ zLevel: zLevel });
    layer.addTo(map);
  } else {
    const leaflet = getLeafletMap(map);
    const Lns = getLeafletNamespace();
    if (leaflet && Lns && typeof Lns.layerGroup === "function") {
      layer = Lns.layerGroup();
      layer.addTo(leaflet);
    }
  }

  map._guessLineLayer = layer;
  map._guessLineLayerZ = zLevel;
  return layer;
}

export function drawGuessToActualLine() {
  const map = window.mazeMapInstance;
  if (!map) return;
  if (getGuessingState()) {
    clearGuessLine();
    return;
  }

  const guessMarker = map._clickMarker;
  const actualMarker = map._actualLocationMarker;

  if (!guessMarker || !actualMarker) {
    if (map._guessLine) {
      map._guessLine.remove();
      map._guessLine = null;
    }
    return;
  }

  const guessLngLat =
    typeof guessMarker.getLngLat === "function"
      ? guessMarker.getLngLat()
      : guessMarker._storedLngLat;

  const actualLngLat =
    typeof actualMarker.getLngLat === "function"
      ? actualMarker.getLngLat()
      : null;

  if (!guessLngLat || !actualLngLat) return;

  const guessZ = guessMarker._storedZLevel;
  const layer = getOrCreateGuessLineLayer(map, guessZ);
  const leafletMap = getLeafletMap(map);
  const Lns = getLeafletNamespace();
  const mapboxMap = getMapboxMap(map);

  const latlngs = [
    [guessLngLat.lat, guessLngLat.lng],
    [actualLngLat.lat, actualLngLat.lng],
  ];

  if (Lns && (layer || leafletMap)) {
    if (map._guessLine) {
      map._guessLine.setLatLngs(latlngs);
    } else {
      map._guessLine = Lns.polyline(latlngs, {
        color: "#D31F40",
        opacity: 0.9,
        dashArray: "6,6",
        interactive: false,
      });

      if (layer && typeof layer.addLayer === "function") {
        layer.addLayer(map._guessLine);
      } else if (layer && typeof map._guessLine.addTo === "function") {
        map._guessLine.addTo(layer);
      } else if (leafletMap && typeof map._guessLine.addTo === "function") {
        map._guessLine.addTo(leafletMap);
      }
    }
    return;
  }

  if (mapboxMap) {
    const sourceId = "guess-line-source";
    const layerId = "guess-line-layer";
    const geojson = {
      type: "Feature",
      geometry: {
        type: "LineString",
        coordinates: [
          [guessLngLat.lng, guessLngLat.lat],
          [actualLngLat.lng, actualLngLat.lat],
        ],
      },
    };

    if (mapboxMap.getSource && mapboxMap.getSource(sourceId)) {
      mapboxMap.getSource(sourceId).setData(geojson);
    } else {
      mapboxMap.addSource(sourceId, { type: "geojson", data: geojson });
      mapboxMap.addLayer({
        id: layerId,
        type: "line",
        source: sourceId,
        paint: {
          "line-color": "#D31F40",
          "line-width": 3,
          "line-opacity": 0.9,
          "line-dasharray": [2, 2],
        },
      });
    }
  }
}

export function clearGuessLine() {
  const map = window.mazeMapInstance;
  if (!map) return;

  if (map._guessLine) {
    const leafletMap = getLeafletMap(map);
    if (leafletMap) {
      leafletMap.removeLayer(map._guessLine);
    }
    map._guessLine = null;
  }

  const mapboxMap = getMapboxMap(map);
  if (mapboxMap) {
    const sourceId = "guess-line-source";
    const layerId = "guess-line-layer";
    if (mapboxMap.getLayer && mapboxMap.getLayer(layerId)) {
      mapboxMap.removeLayer(layerId);
    }
    if (mapboxMap.getSource && mapboxMap.getSource(sourceId)) {
      mapboxMap.removeSource(sourceId);
    }
  }
}

/**
 * Clears markers and line from map. Caller should call updateGuessButtonState(false) after.
 */
export function clearMapStateFromUnity() {
  const map = window.mazeMapInstance;
  if (!map) return;

  if (map._clickMarker) {
    map._clickMarker.remove();
    map._clickMarker = null;
  }
  if (map._actualLocationMarker) {
    map._actualLocationMarker.remove();
    map._actualLocationMarker = null;
  }

  clearGuessLine();

  if (map._guessLineLayer) {
    const leafletMap = getLeafletMap(map);
    if (leafletMap && typeof leafletMap.removeLayer === "function") {
      leafletMap.removeLayer(map._guessLineLayer);
    } else if (typeof map.removeLayer === "function") {
      map.removeLayer(map._guessLineLayer);
    }
    map._guessLineLayer = null;
    map._guessLineLayerZ = null;
  }

  const view = window.currentMapView;
  if (view && typeof map.jumpTo === "function") {
    map.jumpTo({ center: { lng: view.lng, lat: view.lat }, zoom: view.zoom });
    if (typeof view.zLevel === "number") {
      try { map.setZLevel(view.zLevel); } catch (e) {}
    }
  }
}
