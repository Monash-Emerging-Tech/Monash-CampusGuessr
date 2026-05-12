(function () {
  var pinActive = false; // false = inactive (auto-collapse allowed), true = active (pinned)
  var isGuessingState = true; // true = guessing, false = results/end-round
  // --------------------------------------------------------------- MAZE MAP INITIALIZATION
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
    for (var key in window) {
      try {
        if (Object.prototype.hasOwnProperty.call(window, key)) {
          if (String(key).toLowerCase().includes("maze")) {
            var obj = window[key];
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
        /* ignore */
      }
    }
    return false;
  }

  var mazeMapSdkReady = false;

  function initializeMazeMap() {
    if (!isMazeMapReady()) {
      if (window.mazeMapRetryCount === undefined) window.mazeMapRetryCount = 0;
      if (window.mazeMapRetryCount < 10) {
        window.mazeMapRetryCount++;
        return void setTimeout(initializeMazeMap, 1000);
      }
      console.error("Maze Maps SDK failed to load after 10 attempts");
      return;
    }
    mazeMapSdkReady = true;
    console.log("Maze Maps SDK ready — waiting for campus from Unity");
    if (window._pendingMapPackView) {
      var pv = window._pendingMapPackView;
      window._pendingMapPackView = null;
      mmSetMapPackView(pv.campusId, pv.lat, pv.lng, pv.zoom);
    }
  }

  window.addEventListener("load", function () {
    setTimeout(initializeMazeMap, 1000);
  });

  // Called from Unity via setMapPackViewFromUnity in WebGLBridge.jslib.
  // Creates the MazeMap instance for the first time using the campus Unity passes.
  function mmSetMapPackView(campusId, lat, lng, zoom) {
    // SDK not ready yet — queue and apply once initializeMazeMap confirms it
    if (!mazeMapSdkReady) {
      window._pendingMapPackView = { campusId: campusId, lat: lat, lng: lng, zoom: zoom };
      console.log("mmSetMapPackView: SDK not ready, queued campus", campusId);
      return;
    }

    var MazeLibrary = window.Maze || mazemap;

    if (window.mazeMapInstance) {
        if (window._currentCampusId === campusId) {
          window.mazeMapInstance.flyTo({ center: [lng, lat], zoom: zoom });
          console.log("mmSetMapPackView: flyTo", lat, lng, "zoom", zoom);
          return;
        } else {
          console.log("mmSetMapPackView: campus changed from", window._currentCampusId, "to", campusId, "— recreating map");
          window.mazeMapInstance.remove();
          window.mazeMapInstance = null;
          window._currentCampusId = null;
        }
      }

    // First call or campus changed — create map
    console.log("mmSetMapPackView: creating map for campus", campusId, lat, lng, zoom);
    window._currentCampusId = campusId;
    try {
      var map = new MazeLibrary.Map({
        container: "map",
        campuses: campusId,
        center: { lng: lng, lat: lat },
        zoom: zoom,
        minZLevel: 0,
        maxZLevel: 12,
      });
      window.mazeMapInstance = map;
      map.on("load", function () {
        console.log("Maze Maps ready for interaction, campus", campusId);
      });
      map.on("click", function (e) {
        if (!isGuessingState) {
          console.log("Map click ignored - guessing disabled");
          return;
        }
        createSingleMarker(map, e.lngLat, map.zLevel);
      });
    } catch (error) {
      console.error(
        "Failed to initialize Maze Maps:",
        error && error.message ? error.message : error
      );
    }
  }
  window.mmSetMapPackView = mmSetMapPackView;

  // --------------------------------------------------------------- MARKER PLACEMENT
  function createSingleMarker(map, lngLat, zLevel) {
    // Keep a reference on the map object itself (or use a module-level variable)
    // zlevel is +1 from what is shown on the ui eg. ground = 0, level 1 = 2
    if (map._clickMarker) {
      map._clickMarker.remove();
    }

    map._clickMarker = new Mazemap.MazeMarker({
      color: "MazeBlue",
      size: 60,
      zLevel: zLevel,
      imgUrl: "/assets/img/markers/handthing.png",
      imgScale: 1.7,
      color: "white",
      innerCircle: false,
    })
      .setLngLat(lngLat)
      .addTo(map);

    // Store zLevel on marker for easy retrieval later
    map._clickMarker._storedZLevel = zLevel;
    map._clickMarker._storedLngLat = lngLat;

    // Enable guess button when marker is placed
    updateGuessButtonState(true);

    console.log("Guess Marker placed at:", lngLat, "on zLevel:", zLevel);
    drawGuessToActualLine();
  }

  function getLeafletNamespace() {
    return (
      window.L ||
      (window.mazemap && window.mazemap.L) ||
      (window.MazeMap && window.MazeMap.L) ||
      (window.Maze && window.Maze.L) ||
      null
    );
  }

  function getMapboxMap(map) {
    if (!map) return null;
    if (
      typeof map.addSource === "function" &&
      typeof map.addLayer === "function"
    ) {
      return map;
    }
    if (typeof map.getMap === "function") {
      var inner = map.getMap();
      if (
        inner &&
        typeof inner.addSource === "function" &&
        typeof inner.addLayer === "function"
      ) {
        return inner;
      }
    }
    var candidates = [map._map, map.map, map._mapObject, map._mapRef];
    for (var i = 0; i < candidates.length; i++) {
      var candidate = candidates[i];
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

  function getOrCreateGuessLineLayer(map, zLevel) {
    if (map._guessLineLayer && map._guessLineLayerZ === zLevel) {
      return map._guessLineLayer;
    }

    // Remove old layer if zLevel changed
    if (map._guessLineLayer) {
      var leafletMap = getLeafletMap(map);
      if (leafletMap && typeof leafletMap.removeLayer === "function") {
        leafletMap.removeLayer(map._guessLineLayer);
      } else if (typeof map.removeLayer === "function") {
        map.removeLayer(map._guessLineLayer);
      }
    }

    var layer = null;
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
      var leaflet = getLeafletMap(map);
      var Lns = getLeafletNamespace();
      if (leaflet && Lns && typeof Lns.layerGroup === "function") {
        layer = Lns.layerGroup();
        layer.addTo(leaflet);
      }
    }

    map._guessLineLayer = layer;
    map._guessLineLayerZ = zLevel;

    return layer;
  }

  function drawGuessToActualLine() {
    var map = window.mazeMapInstance;
    if (!map) {
      // console.log("[Line] No map instance");
      return;
    }
    if (isGuessingState) {
      // console.log("[Line] Skipped (guessing state true)");
      clearGuessLine();
      return;
    }

    var guessMarker = map._clickMarker;
    var actualMarker = map._actualLocationMarker;

    if (!guessMarker || !actualMarker) {
      // console.log("[Line] Missing markers", {
      //   hasGuess: !!guessMarker,
      //   hasActual: !!actualMarker,
      // });
      if (map._guessLine) {
        map._guessLine.remove();
        map._guessLine = null;
      }
      return;
    }

    var guessLngLat =
      typeof guessMarker.getLngLat === "function"
        ? guessMarker.getLngLat()
        : guessMarker._storedLngLat;

    var actualLngLat =
      typeof actualMarker.getLngLat === "function"
        ? actualMarker.getLngLat()
        : null;

    if (!guessLngLat || !actualLngLat) {
      // console.log("[Line] Missing marker coordinates", {
      //   guessLngLat: guessLngLat,
      //   actualLngLat: actualLngLat,
      // });
      return;
    }

    var guessZ = guessMarker._storedZLevel;

    var layer = getOrCreateGuessLineLayer(map, guessZ);
    var leafletMap = getLeafletMap(map);
    var Lns = getLeafletNamespace();
    var mapboxMap = getMapboxMap(map);

    var latlngs = [
      [guessLngLat.lat, guessLngLat.lng],
      [actualLngLat.lat, actualLngLat.lng],
    ];

    if (Lns && (layer || leafletMap)) {
      if (map._guessLine) {
        map._guessLine.setLatLngs(latlngs);
        // console.log("[Line] Updated existing line (Leaflet)", {
        //   latlngs: latlngs,
        // });
      } else {
        map._guessLine = Lns.polyline(latlngs, {
          color: "#D31F40",
          opacity: 0.9,
          dashArray: "6,6",
          interactive: false,
        });

        if (layer && typeof layer.addLayer === "function") {
          layer.addLayer(map._guessLine);
          // console.log("[Line] Added line via layer.addLayer");
        } else if (layer && typeof map._guessLine.addTo === "function") {
          map._guessLine.addTo(layer);
          // console.log("[Line] Added line via addTo(layer)");
        } else if (leafletMap && typeof map._guessLine.addTo === "function") {
          map._guessLine.addTo(leafletMap);
          // console.log("[Line] Added line via addTo(leafletMap)");
        }
      }
      return;
    }

    if (mapboxMap) {
      var sourceId = "guess-line-source";
      var layerId = "guess-line-layer";
      var geojson = {
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
        // console.log("[Line] Updated existing line (Mapbox)");
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
        // console.log("[Line] Added line (Mapbox)");
      }
      return;
    }

    // console.log("[Line] No layer target", {
    //   hasL: !!Lns,
    //   hasLayer: !!layer,
    //   hasLeaflet: !!leafletMap,
    //   hasMapbox: !!mapboxMap,
    //   mapGetMap: typeof map.getMap,
    //   mapKeys: Object.keys(map || {}).slice(0, 30),
    //   mapCandidates: {
    //     _map: !!map._map,
    //     map: !!map.map,
    //     leafletMap: !!map.leafletMap,
    //     _leafletMap: !!map._leafletMap,
    //     _mapObject: !!map._mapObject,
    //     _mapRef: !!map._mapRef,
    //   },
    //   mazemapHasL: !!(window.mazemap && window.mazemap.L),
    //   MazeMapHasL: !!(window.MazeMap && window.MazeMap.L),
    //   MazeHasL: !!(window.Maze && window.Maze.L),
    // });
  }

  function clearGuessLine() {
    var map = window.mazeMapInstance;
    if (!map) return;

    if (map._guessLine) {
      var leafletMap = getLeafletMap(map);
      if (leafletMap) {
        leafletMap.removeLayer(map._guessLine);
      }
      map._guessLine = null;
    }

    var mapboxMap = getMapboxMap(map);
    if (mapboxMap) {
      var sourceId = "guess-line-source";
      var layerId = "guess-line-layer";
      if (mapboxMap.getLayer && mapboxMap.getLayer(layerId)) {
        mapboxMap.removeLayer(layerId);
      }
      if (mapboxMap.getSource && mapboxMap.getSource(sourceId)) {
        mapboxMap.removeSource(sourceId);
      }
    }
  }

  function clearMapStateFromUnity() {
    var map = window.mazeMapInstance;
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
      var leafletMap = getLeafletMap(map);
      if (leafletMap && typeof leafletMap.removeLayer === "function") {
        leafletMap.removeLayer(map._guessLineLayer);
      } else if (typeof map.removeLayer === "function") {
        map.removeLayer(map._guessLineLayer);
      }
      map._guessLineLayer = null;
      map._guessLineLayerZ = null;
    }

    updateGuessButtonState(false);
  }

  // --------------------------------------------------------------- Z-LEVEL NAME HELPER
  function getZLevelName(zLevel) {
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

  // --------------------------------------------------------------- SUBMIT GUESS
  function submitGuess() {
    var map = window.mazeMapInstance;
    if (!map) {
      console.error("Map instance not available");
      return;
    }

    // Get the current active guess marker
    var marker = map._clickMarker;
    if (!marker) {
      console.warn("No guess marker placed. Please click on the map first.");
      return;
    }

    try {
      // Get coordinates from marker (try getLngLat() first, fallback to stored value)
      var lngLat = null;
      if (typeof marker.getLngLat === "function") {
        lngLat = marker.getLngLat();
      }
      // Fallback to stored value if getLngLat() doesn't work or returns null
      if (!lngLat && marker._storedLngLat) {
        lngLat = marker._storedLngLat;
      }
      if (!lngLat) {
        console.error("Could not get coordinates from marker");
        return;
      }

      // Get zLevel from stored value, marker options, or map
      var zLevel =
        marker._storedZLevel !== undefined
          ? marker._storedZLevel
          : marker.options
          ? marker.options.zLevel
          : map.zLevel || 0;

      // Build JSON payload matching EnhancedMapClickData structure
      var payload = {
        latitude: lngLat.lat,
        longitude: lngLat.lng,
        zLevel: zLevel,
        zLevelName: getZLevelName(zLevel),
      };

      // Send to Unity
      var jsonString = JSON.stringify(payload);

      // Get Unity instance using helper function
      var unityInstance = getUnityInstance();

      if (unityInstance && typeof unityInstance.SendMessage === "function") {
        try {
          unityInstance.SendMessage(
            "MapInteractionManager", // GameObject name
            "SubmitGuess", // Method name
            jsonString // JSON string
          );
          console.log("Guess submitted to Unity:", payload);

          // Disable button after submission (will be re-enabled when new marker is placed)
          updateGuessButtonState(false);
        } catch (error) {
          console.error("Error sending message to Unity:", error);
        }
      } else {
        console.error("Unity instance not found. Cannot submit guess.");
        console.error("Debug info:", {
          unityInstance: typeof window.unityInstance,
          gameInstance: typeof window.gameInstance,
          canvas: document.querySelector("#unity-canvas")
            ? "found"
            : "not found",
        });
      }
    } catch (error) {
      console.error("Error submitting guess:", error);
    }
  }

  // Expose submitGuess to global scope so it can be called from Unity or buttons
  window.submitGuess = submitGuess;

  // --------------------------------------------------------------- UNITY INSTANCE HELPER
  /**
   * Gets the Unity instance, trying multiple methods
   * @returns {Object|null} Unity instance or null if not found
   */
  function getUnityInstance() {
    // Try multiple methods to find Unity instance
    if (
      window.unityInstance &&
      typeof window.unityInstance.SendMessage === "function"
    ) {
      return window.unityInstance;
    }
    if (
      window.gameInstance &&
      typeof window.gameInstance.SendMessage === "function"
    ) {
      return window.gameInstance;
    }
    // Try to find it in the canvas element
    var canvas = document.querySelector("#unity-canvas");
    if (canvas && canvas._unityInstance) {
      return canvas._unityInstance;
    }
    return null;
  }

  // --------------------------------------------------------------- UNITY ACTUAL LOCATION INTEGRATION
  /**
   * Receives actual location data from Unity and adds it to the map
   * @param {string} jsonPayload - JSON string containing actual location data with x, y, z coordinates
   * Format: {"latitude": float, "longitude": float, "zLevel": int, "zLevelName": string}
   */
  function addActualLocationFromUnity(jsonPayload) {
    try {
      var map = window.mazeMapInstance;
      if (!map) {
        console.error(
          "Map instance not available for addActualLocationFromUnity"
        );
        return;
      }

      // Parse JSON payload
      var locationData = JSON.parse(jsonPayload);
      var lat = locationData.latitude;
      var lng = locationData.longitude;
      var zLevel = locationData.zLevel || 0;

      console.log("Received actual location from Unity:", locationData);

      // Remove any existing actual location marker
      if (map._actualLocationMarker) {
        map._actualLocationMarker.remove();
      }

      // Create actual location marker (purple/blue)
      var markerOptions = {
        zLevel: zLevel,
        innerCircle: false,
        color: "#9D9DDC",
        imgUrl: "../assets/img/markers/fat.png",
        imgScale: 1.7,
        size: 60,
      };

      // Create and add marker to map
      var marker = new Mazemap.MazeMarker(markerOptions)
        .setLngLat({ lng: lng, lat: lat })
        .addTo(map);

      // Store marker reference
      map._actualLocationMarker = marker;
      drawGuessToActualLine();

      console.log("Actual location added from Unity:", {
        coordinates: { lat: lat, lng: lng },
        zLevel: zLevel,
        zLevelName: locationData.zLevelName,
      });

      // Optionally center map on marker when placed
      map.flyTo({
        center: [lng, lat],
        zoom: map.getZoom(),
      });
    } catch (error) {
      console.error("Error adding actual location from Unity:", error);
      console.error("Payload received:", jsonPayload);
    }
  }

  // Expose to global scope for Unity to call
  window.addActualLocationFromUnity = addActualLocationFromUnity;

  function getLeafletMap(map) {
    if (!map) return null;
    if (typeof map.getMap === "function") {
      return map.getMap(); // MazeMap to Leaflet
    }
    var candidates = [
      map._map,
      map.map,
      map.leafletMap,
      map._leafletMap,
      map._mapObject,
      map._mapRef,
    ];
    for (var i = 0; i < candidates.length; i++) {
      if (candidates[i] && typeof candidates[i].addLayer === "function") {
        return candidates[i];
      }
    }
    return null;
  }

  // --------------------------------------------------------------- UNITY MAP VISIBILITY CONTROL
  /**
   * Shows the maze map UI (called from Unity)
   */
  function showMapFromUnity() {
    var mapUI = document.getElementById("maze-map-ui");
    if (mapUI) {
      mapUI.style.display = "block";
      // Ensure guess button width matches widget when shown
      syncGuessButtonWidth();
      requestAnimationFrame(syncGuessButtonWidth);
      console.log("Unity call to show Minimap UI");
    } else {
      console.error("maze-map-ui element not found");
    }
  }

  /**
   * Hides the maze map UI (called from Unity)
   */
  function hideMapFromUnity() {
    var mapUI = document.getElementById("maze-map-ui");
    if (mapUI) {
      mapUI.style.display = "none";
      console.log("Unity call to hide Minimap UI");
    } else {
      console.error("maze-map-ui element not found");
    }
  }

  // Expose to global scope for Unity to call
  window.showMapFromUnity = showMapFromUnity;
  window.hideMapFromUnity = hideMapFromUnity;
  window.mmSetWidgetSize = setWidgetSize;
  window.submitGuess = submitGuess;
  window.addActualLocationFromUnity = addActualLocationFromUnity;
  window.setGuessingStateFromUnity = setGuessingStateFromUnity;
  window.clearMapStateFromUnity = clearMapStateFromUnity;

  // --------------------------------------------------------------- GUESS BUTTON MANAGEMENT
  function updateGuessButtonState(hasMarker) {
    var button = document.getElementById("guess-button");
    if (!button) return;
    if (!isGuessingState) {
      button.disabled = true;
      button.style.display = "none";
      return;
    }

    if (hasMarker) {
      button.disabled = false;
      button.textContent = "GUESS";
    } else {
      button.disabled = true;
      button.textContent = "PLACE YOUR PIN ON THE MAP";
    }
    button.style.display = "";
  }

  function syncGuessButtonWidth() {
    var widget = document.getElementById("maze-map-widget");
    var button = document.getElementById("guess-button");
    if (!widget || !button) return;

    // Get computed width of widget
    var widgetWidth = widget.offsetWidth;
    if (widgetWidth > 0) {
      button.style.width = widgetWidth + "px";
    }
  }

  // Initialize guess button
  function initializeGuessButton() {
    var button = document.getElementById("guess-button");
    if (!button) return;

    // Wire up click handler
    button.addEventListener("click", function () {
      if (!button.disabled) {
        submitGuess();
      }
    });

    // Sync width initially and whenever widget size changes
    syncGuessButtonWidth();
    // Retry after layout settles
    requestAnimationFrame(syncGuessButtonWidth);
    setTimeout(syncGuessButtonWidth, 100);
    setTimeout(syncGuessButtonWidth, 300);

    // Watch for widget size changes
    var widget = document.getElementById("maze-map-widget");
    if (widget) {
      var observer = new MutationObserver(function () {
        syncGuessButtonWidth();
      });
      observer.observe(widget, {
        attributes: true,
        attributeFilter: ["class", "style"],
      });

      // ResizeObserver for actual size changes
      if (typeof ResizeObserver !== "undefined") {
        var resizeObserver = new ResizeObserver(function () {
          syncGuessButtonWidth();
        });
        resizeObserver.observe(widget);
      }

      // Also watch for resize events
      window.addEventListener("resize", syncGuessButtonWidth);
    }
  }

  function setGuessingStateFromUnity(isGuessing) {
    isGuessingState = !!isGuessing;
    // console.log("[State] Guessing state updated:", isGuessingState);
    var button = document.getElementById("guess-button");
    if (button) {
      button.disabled = !isGuessingState;
      button.style.display = isGuessingState ? "" : "none";
    }
    var controls = document.querySelector("#maze-map-ui .mm-controls");
    if (controls) {
      controls.style.display = isGuessingState ? "" : "none";
    }
    var map = window.mazeMapInstance;
    updateGuessButtonState(!!(map && map._clickMarker));
    if (isGuessingState) {
      clearGuessLine();
    } else {
      drawGuessToActualLine();
    }
  }

  // --------------------------------------------------------------- UI CONTROLS FOR SIZE TOGGLING
  // Size toggle helpers (no logic wiring yet)
  function setWidgetSize(size) {
    var widget = document.getElementById("maze-map-widget");
    if (!widget) return;
    var sizes = ["mm-size-xs", "mm-size-s", "mm-size-m", "mm-size-l", "mm-size-round-end"];
    for (var i = 0; i < sizes.length; i++) {
      widget.classList.remove(sizes[i]);
    }
    widget.classList.add(size);
    var mapUI = document.getElementById("maze-map-ui");
    if (mapUI) {
      if (size === "mm-size-round-end") {
        mapUI.classList.add("mm-round-end");
      } else {
        mapUI.classList.remove("mm-round-end");
      }
    }
    // Update control states after size change
    updateControlDisabled();
    // Ensure MazeMap fits the new container size
    queueMapResize();
    // Sync guess button width with new widget size
    syncGuessButtonWidth();
  }
  window.mmSetWidgetSize = setWidgetSize;

  // Logic wiring for controls: expand/minimise cycle through sizes
  function getCurrentSize() {
    var widget = document.getElementById("maze-map-widget");
    if (!widget) return "mm-size-s";
    var sizes = ["mm-size-xs", "mm-size-s", "mm-size-m", "mm-size-l", "mm-size-round-end"];
    for (var i = 0; i < sizes.length; i++) {
      if (widget.classList.contains(sizes[i])) return sizes[i];
    }
    return "mm-size-s";
  }

  function cycleSize(direction) {
    var order = ["mm-size-xs", "mm-size-s", "mm-size-m", "mm-size-l"];
    var current = getCurrentSize();
    var idx = order.indexOf(current);
    if (idx === -1) idx = 0;
    if (direction === "next") {
      idx = Math.min(order.length - 1, idx + 1);
    } else if (direction === "prev") {
      idx = Math.max(0, idx - 1);
    }
    setWidgetSize(order[idx]);
  }

  function wireControls() {
    var root = document.getElementById("maze-map-ui") || document;
    var btnExpand = root.querySelector(".mm-controls .mm-expand");
    var btnMinimize = root.querySelector(".mm-controls .mm-minimise");
    var btnPin = root.querySelector(".mm-controls .mm-pin");

    if (btnExpand) {
      btnExpand.addEventListener("click", function () {
        cycleSize("next");
      });
    }
    if (btnMinimize) {
      btnMinimize.addEventListener("click", function () {
        cycleSize("prev");
      });
    }
    if (btnPin) {
      // Toggle pin active/inactive visual state
      btnPin.setAttribute("aria-pressed", "false");
      btnPin.addEventListener("click", function (e) {
        pinActive = !pinActive;
        btnPin.setAttribute("aria-pressed", pinActive ? "true" : "false");
      });
    }

    // Initialize disabled state now and keep it in sync on class changes
    updateControlDisabled();
    var widgetEl = document.getElementById("maze-map-widget");
    if (widgetEl) {
      var classObserver = new MutationObserver(updateControlDisabled);
      classObserver.observe(widgetEl, {
        attributes: true,
        attributeFilter: ["class"],
      });
    }

    // Collapse to small on outside click when pin is inactive
    document.addEventListener("click", handleOutsideClick, true);

    // Observe container size changes to trigger map.resize()
    var containerEl = document.getElementById("maze-maps-container");
    if (containerEl && typeof ResizeObserver !== "undefined") {
      var ro = new ResizeObserver(function () {
        queueMapResize();
      });
      ro.observe(containerEl);
    }
  }

  function updateControlDisabled() {
    var root = document.getElementById("maze-map-ui") || document;
    var btnExpand = root.querySelector(".mm-controls .mm-expand");
    var btnMinimize = root.querySelector(".mm-controls .mm-minimise");
    var size = getCurrentSize();
    if (btnExpand) {
      btnExpand.disabled = size === "mm-size-l";
    }
    if (btnMinimize) {
      btnMinimize.disabled = size === "mm-size-xs";
    }
  }

  function handleOutsideClick(event) {
    if (!isGuessingState) return;
    if (pinActive) return; // pinned: ignore outside clicks
    var widget = document.getElementById("maze-map-widget");
    var controls = document.querySelector("#maze-map-ui .mm-controls");
    var size = getCurrentSize();
    if (!widget) return;
    // Determine if click is inside widget or controls
    var clickedInsideWidget = widget.contains(event.target);
    var clickedInsideControls = controls
      ? controls.contains(event.target)
      : false;
    if (
      !clickedInsideWidget &&
      !clickedInsideControls &&
      size !== "mm-size-s"
    ) {
      setWidgetSize("mm-size-s");
    }
  }

  // --------------------------------------------------------------- MAP RESIZE HANDLING
  // Debounced map.resize to avoid white gaps after container size changes
  var mapResizeScheduled = false;
  function queueMapResize() {
    if (mapResizeScheduled) return;
    mapResizeScheduled = true;
    // Two rafs to wait for layout, plus a timeout fallback
    requestAnimationFrame(function () {
      requestAnimationFrame(function () {
        mapResizeScheduled = false;
        resizeMazeMap();
      });
    });
    setTimeout(function () {
      mapResizeScheduled = false;
      resizeMazeMap();
    }, 200);
  }

  function resizeMazeMap() {
    var map = window.mazeMapInstance;
    if (map && typeof map.resize === "function") {
      try {
        map.resize();
      } catch (e) {
        /* ignore */
      }
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", function () {
      wireControls();
      initializeGuessButton();
    });
  } else {
    wireControls();
    initializeGuessButton();
  }
})();
