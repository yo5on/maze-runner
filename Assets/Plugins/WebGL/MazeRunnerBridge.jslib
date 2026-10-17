// Unity -> page bridge for the Web player (see Assets/Scripts/UI/WebBridge.cs).
mergeInto(LibraryManager.library, {
  MazeRunner_ReportState: function (statePtr) {
    var state = UTF8ToString(statePtr);
    if (typeof window.mazeRunnerOnState === "function") {
      try { window.mazeRunnerOnState(state); } catch (e) { console.warn(e); }
    }
  },

  // Main menu Quit: the page decides where to go (the preview page).
  MazeRunner_Quit: function () {
    if (typeof window.mazeRunnerQuit === "function") {
      window.mazeRunnerQuit();
    } else {
      window.location.assign("preview/");
    }
  }
});
