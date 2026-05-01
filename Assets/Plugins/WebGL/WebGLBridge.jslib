mergeInto(LibraryManager.library, {
  showMapFromUnity: function () {
    if (typeof window.showMapFromUnity === "function")
      window.showMapFromUnity();
  },
  hideMapFromUnity: function () {
    if (typeof window.hideMapFromUnity === "function")
      window.hideMapFromUnity();
  },
  addActualLocationFromUnity: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    if (typeof window.addActualLocationFromUnity === "function")
      window.addActualLocationFromUnity(json);
  },
  setGuessingStateFromUnity: function (isGuessing) {
    if (typeof window.setGuessingStateFromUnity === "function")
      window.setGuessingStateFromUnity(!!isGuessing);
  },
  mmSetWidgetSize: function (sizePtr) {
    var size = UTF8ToString(sizePtr);
    if (typeof window.mmSetWidgetSize === "function")
      window.mmSetWidgetSize(size);
  },
  clearMapStateFromUnity: function () {
    if (typeof window.clearMapStateFromUnity === "function")
      window.clearMapStateFromUnity();
  },
  updateScoreFromUnity: function (score, round) {
    if (typeof window.updateScoreFromUnity === "function")
      window.updateScoreFromUnity(score, round);
  },
  showLoading: function (show) {
    if (typeof window.showLoading === "function") window.showLoading(!!show);
  },
  addMarkerFromUnity: function (lat, lng, labelPtr, typePtr) {
    var label = UTF8ToString(labelPtr);
    var type = UTF8ToString(typePtr);
    if (typeof window.addMarkerFromUnity === "function")
      window.addMarkerFromUnity(lat, lng, label, type);
  },
  setMapCenterFromUnity: function(lat, lng, zoom) {
    const map = window.mazeMapInstance;
    if (map && typeof map.jumpTo === "function") {
        map.jumpTo({ center: [lng, lat], zoom: zoom });
    }
  },
  showTooltipFromWeb: function (seconds) {
    if (
      typeof window.unityInstance !== "undefined" &&
      window.unityInstance != null
    ) {
      window.unityInstance.SendMessage(
        "TooltipBox",
        "ShowTooltipFromWeb",
        seconds,
      );
    }
  },
});
