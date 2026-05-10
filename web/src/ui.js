/**
 * Guess button, widget size controls, pin, expand/minimize, outside click, tooltip height check.
 */

import { getPinActive, getGuessingState, setPinActive } from "./state.js";
import { queueMapResize } from "./mapCore.js";

// --------------------------------------------------------------- GUESS BUTTON

export function updateGuessButtonState(hasMarker) {
  const button = document.getElementById("guess-button");
  if (!button) return;
  if (!getGuessingState()) {
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

export function syncGuessButtonWidth() {
  const widget = document.getElementById("maze-map-widget");
  const button = document.getElementById("guess-button");
  if (!widget || !button) return;

  const widgetWidth = widget.offsetWidth;
  if (widgetWidth > 0) {
    button.style.width = widgetWidth + "px";
  }
}

/**
 * Wire guess button. Pass submitGuess so the button can call it on click.
 * @param {function} submitGuess - () => void
 */
export function initializeGuessButton(submitGuess) {
  const button = document.getElementById("guess-button");
  if (!button) return;

  button.addEventListener("click", () => {
    if (!button.disabled && typeof submitGuess === "function") {
      submitGuess();
    }
  });

  syncGuessButtonWidth();
  requestAnimationFrame(syncGuessButtonWidth);
  setTimeout(syncGuessButtonWidth, 100);
  setTimeout(syncGuessButtonWidth, 300);

  const widget = document.getElementById("maze-map-widget");
  if (widget) {
    const observer = new MutationObserver(() => {
      syncGuessButtonWidth();
    });
    observer.observe(widget, {
      attributes: true,
      attributeFilter: ["class", "style"],
    });

    if (typeof ResizeObserver !== "undefined") {
      const resizeObserver = new ResizeObserver(() => {
        syncGuessButtonWidth();
        checkMazeMapWidgetHeight();
      });
      resizeObserver.observe(widget);
    }

    window.addEventListener("resize", () => {
      syncGuessButtonWidth();
      checkMazeMapWidgetHeight();
    });
  }
}

// --------------------------------------------------------------- WIDGET SIZE CONTROLS

export function getCurrentSize() {
  const widget = document.getElementById("maze-map-widget");
  if (!widget) return "mm-size-s";
  const sizes = ["mm-size-s", "mm-size-m", "mm-size-l", "mm-size-round-end"];
  for (let i = 0; i < sizes.length; i++) {
    if (widget.classList.contains(sizes[i])) return sizes[i];
  }
  return "mm-size-s";
}

export function setWidgetSize(size) {
  const widget = document.getElementById("maze-map-widget");
  if (!widget) return;
  const sizes = ["mm-size-s", "mm-size-m", "mm-size-l", "mm-size-round-end"];
  for (let i = 0; i < sizes.length; i++) {
    widget.classList.remove(sizes[i]);
  }
  widget.classList.add(size);
  const mapUI = document.getElementById("maze-map-ui");
  if (mapUI) {
    if (size === "mm-size-round-end") {
      mapUI.classList.add("mm-round-end");
    } else {
      mapUI.classList.remove("mm-round-end");
    }
  }
  updateControlDisabled();
  queueMapResize();
  syncGuessButtonWidth();
  checkMazeMapWidgetHeight();
}

function cycleSize(direction) {
  const order = ["mm-size-s", "mm-size-m", "mm-size-l"];
  const current = getCurrentSize();
  let idx = order.indexOf(current);
  if (idx === -1) idx = 0;
  if (direction === "next") {
    idx = Math.min(order.length - 1, idx + 1);
  } else if (direction === "prev") {
    idx = Math.max(0, idx - 1);
  }
  setWidgetSize(order[idx]);
  checkMazeMapWidgetHeight();
}

function resetMapView() {
  const map = window.mazeMapInstance;
  if (!map || typeof map.flyTo !== "function") return;
  const view = window.currentMapView || { lng: 145.1361, lat: -37.9106, zoom: 16 };
  map.flyTo({ center: { lng: view.lng, lat: view.lat }, zoom: view.zoom });
}

function updateControlDisabled() {
  const root = document.getElementById("maze-map-ui") || document;
  const btnExpand = root.querySelector(".mm-controls .mm-expand");
  const btnMinimize = root.querySelector(".mm-controls .mm-minimise");
  const size = getCurrentSize();
  if (btnExpand) {
    btnExpand.disabled = size === "mm-size-l";
  }
  if (btnMinimize) {
    btnMinimize.disabled = size === "mm-size-s";
  }
}

function handleOutsideClick(event) {
  if (!getGuessingState()) return;
  if (getPinActive()) return;
  const widget = document.getElementById("maze-map-widget");
  const controls = document.querySelector("#maze-map-ui .mm-controls");
  const size = getCurrentSize();
  if (!widget) return;
  const clickedInsideWidget = widget.contains(event.target);
  const clickedInsideControls = controls
    ? controls.contains(event.target)
    : false;
  if (
    !clickedInsideWidget &&
    !clickedInsideControls &&
    size !== "mm-size-s"
  ) {
    setWidgetSize("mm-size-s");
  }
  checkMazeMapWidgetHeight();
}

export function checkMazeMapWidgetHeight() {
  const widget = document.getElementById("maze-map-widget");
  if (!widget) return;

  const widgetHeight = widget.offsetHeight;
  const viewportHeight = window.innerHeight;
  const ratio = widgetHeight / viewportHeight;

  if (!window._mazeMapWidgetHeightLastTriggerHigh)
    window._mazeMapWidgetHeightLastTriggerHigh = 0;
  if (!window._mazeMapWidgetHeightLastTriggerLow)
    window._mazeMapWidgetHeightLastTriggerLow = 0;
  const now = Date.now();
  const limiterMs = 3000;

  const unityInstance = window.unityInstance || window.gameInstance;

  if (
    window.isMapVisible === false ||
    (() => {
      const mapUI = document.getElementById("maze-map-ui");
      return mapUI && mapUI.style.display === "none";
    })()
  ) {
    return;
  }

  if (
    ratio > 0.7 &&
    now - window._mazeMapWidgetHeightLastTriggerHigh > limiterMs
  ) {
    console.log(
      "Maze map widget height > 70% of viewport (",
      (ratio * 100).toFixed(1),
      "%)"
    );
    console.log("[TooltipBox Debug] hasTooltipBox:", window.hasTooltipBox);
    if (window.hasTooltipBox) {
      if (unityInstance && typeof unityInstance.SendMessage === "function") {
        unityInstance.SendMessage("TooltipBox", "ShowTooltipFromWeb", "5");
      }
    }
    window._mazeMapWidgetHeightLastTriggerHigh = now;
  } else if (
    ratio < 0.2 &&
    now - window._mazeMapWidgetHeightLastTriggerLow > limiterMs
  ) {
    console.log(
      "Maze map widget height < 20% of viewport (",
      (ratio * 100).toFixed(1),
      "%)"
    );
    console.log("[TooltipBox Debug] hasTooltipBox:", window.hasTooltipBox);
    if (window.hasTooltipBox) {
      if (unityInstance && typeof unityInstance.SendMessage === "function") {
        unityInstance.SendMessage("TooltipBox", "ShowTooltipFromWeb", "5");
      }
    }
    window._mazeMapWidgetHeightLastTriggerLow = now;
  }

  window.setHasTooltipBox = function (hasIt) {
    window.hasTooltipBox = !!hasIt;
  };
}

/**
 * Wire expand, minimize, pin, reset, outside click, container resize observer.
 */
export function wireControls() {
  const root = document.getElementById("maze-map-ui") || document;
  const btnExpand = root.querySelector(".mm-controls .mm-expand");
  const btnMinimize = root.querySelector(".mm-controls .mm-minimise");
  const btnPin = root.querySelector(".mm-controls .mm-pin");
  const btnReset = root.querySelector(".mm-controls .mm-reset-map");

  if (btnReset) {
    btnReset.addEventListener("click", () => resetMapView());
  }
  if (btnExpand) {
    btnExpand.addEventListener("click", () => cycleSize("next"));
  }
  if (btnMinimize) {
    btnMinimize.addEventListener("click", () => cycleSize("prev"));
  }
  if (btnPin) {
    btnPin.setAttribute("aria-pressed", "false");
    btnPin.setAttribute("data-tooltip", "Pin map");
    btnPin.addEventListener("click", () => {
      setPinActive(!getPinActive());
      btnPin.setAttribute("aria-pressed", getPinActive() ? "true" : "false");
      btnPin.setAttribute(
        "data-tooltip",
        getPinActive() ? "Pinned: map won't shrink" : "Pin map"
      );
    });
  }

  updateControlDisabled();
  const widgetEl = document.getElementById("maze-map-widget");
  if (widgetEl) {
    const classObserver = new MutationObserver(updateControlDisabled);
    classObserver.observe(widgetEl, {
      attributes: true,
      attributeFilter: ["class"],
    });
  }

  document.addEventListener("click", handleOutsideClick, true);

  const containerEl = document.getElementById("maze-maps-container");
  if (containerEl && typeof ResizeObserver !== "undefined") {
    const ro = new ResizeObserver(() => queueMapResize());
    ro.observe(containerEl);
  }
}
