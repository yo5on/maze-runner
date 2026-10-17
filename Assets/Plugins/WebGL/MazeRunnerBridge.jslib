// Unity -> page bridge for the Web player (see Assets/Scripts/UI/WebBridge.cs).
mergeInto(LibraryManager.library, {
  MazeRunner_ReportState: function (statePtr) {
    var state = UTF8ToString(statePtr);
    if (typeof window.mazeRunnerOnState === "function") {
      try { window.mazeRunnerOnState(state); } catch (e) { console.warn(e); }
    }
  }
});
