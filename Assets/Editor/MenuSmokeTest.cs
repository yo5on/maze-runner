using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Customization;
using MazeRunner.Enemies;
using MazeRunner.Levels;
using MazeRunner.Player;
using MazeRunner.UI;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Plays the menus and the progression in one Play Mode session with a virtual keyboard (the real
    /// input path: Input System -> EventSystem / player controller) and checks:
    /// - progression: a new player starts Level 1, a returning player continues the saved current level,
    ///   a locked or missing saved level falls back to the furthest unlocked one, completing a level
    ///   unlocks the next and moves Play on to it, quitting mid-level makes Play restart that level;
    /// - main menu: default focus on Play, the focus frame, arrow navigation, Settings opened with Enter
    ///   and closed with Escape (focus back on Settings), a slider moved with the arrows, Play with Space;
    /// - in a level: no menu focus during play, Escape pauses with Resume focused, Space resumes without
    ///   making the player jump, Settings from the pause menu and back, Game Over (Escape does not resume
    ///   under it, Enter restarts), Level Complete focus;
    /// - Level Select (focus on the next level, Escape back) and character selection (Right browses,
    ///   Confirm focused, Escape back).
    /// Saved progress and settings are put back afterwards. Results: Logs/menu-smoke.txt.
    ///
    /// Menu: Maze Runner > Run Menu Smoke Test
    /// CLI:  Unity -batchmode -projectPath . -executeMethod MazeRunner.EditorTools.MenuSmokeTest.RunFromCommandLine
    ///       (no -quit: it leaves the editor itself, exit code 1 on any failure)
    /// </summary>
    [InitializeOnLoad]
    public static class MenuSmokeTest
    {
        private const string ActiveKey = "MenuSmoke.Active";
        private const string ExitKey = "MenuSmoke.Exit";
        private const string ResultFile = "Logs/menu-smoke.txt";
        private const string UnlockPrefix = "MazeRunner.LevelUnlocked.";
        private const string ClearedPrefix = "MazeRunner.LevelCleared.";
        private static readonly string[] SavedKeys =
        {
            UnlockPrefix + 0, UnlockPrefix + 1, UnlockPrefix + 2, UnlockPrefix + 3,
            ClearedPrefix + 0, ClearedPrefix + 1, ClearedPrefix + 2,
            LevelProgressionManager.CurrentLevelKey,
        };

        static MenuSmokeTest()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Maze Runner/Run Menu Smoke Test")]
        public static void Run() => Begin(false);

        public static void RunFromCommandLine() => Begin(true);

        private static void Begin(bool exitWhenDone)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultFile, "Menu smoke test " + System.DateTime.Now + "\n");
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(ExitKey, exitWhenDone);
            foreach (string key in SavedKeys)
                SessionState.SetString("MenuSmoke." + key, PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key).ToString() : "");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                var go = new GameObject("MenuSmokeDriver");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<Driver>();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                Finish();
            }
        }

        private static void Finish()
        {
            SessionState.SetBool(ActiveKey, false);
            bool restored = true;
            foreach (string key in SavedKeys)
            {
                string v = SessionState.GetString("MenuSmoke." + key, "");
                if (v.Length == 0) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetInt(key, int.Parse(v));
                restored &= PlayerPrefs.HasKey(key) == (v.Length > 0)
                    && (v.Length == 0 || PlayerPrefs.GetInt(key) == int.Parse(v));
            }
            PlayerPrefs.Save();
            File.AppendAllText(ResultFile, restored ? "PASS saved progression restoration\n" : "FAIL saved progression restoration\n");
            string text = File.ReadAllText(ResultFile);
            bool failed = text.Contains("FAIL") || !text.Contains("DONE");
            File.AppendAllText(ResultFile, failed ? "RESULT: FAILED\n" : "RESULT: ALL PASSED\n");
            Debug.Log("[MenuSmoke]\n" + File.ReadAllText(ResultFile));
            if (SessionState.GetBool(ExitKey, false)) EditorApplication.Exit(failed ? 1 : 0);
        }

        private static void Log(string line)
        {
            File.AppendAllText(ResultFile, line + "\n");
            Debug.Log("[MenuSmoke] " + line);
        }

        private class Driver : MonoBehaviour
        {
            private Keyboard keyboard;
            private int errors;
            private string firstError = "";
            private string knownIssue;

            private static void Check(bool ok, string what, string detail = "") =>
                Log((ok ? "PASS " : "FAIL ") + what + (detail.Length > 0 ? " - " + detail : ""));

            private void OnLog(string message, string stack, LogType type)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                // Known, unrelated to menus: the Customization scene's HealthDisplay has no heart container.
                if (message.Contains("heartContainer not assigned")) { knownIssue = message; return; }
                if (errors++ == 0) firstError = message;
            }

            private IEnumerator Start()
            {
                Application.logMessageReceived += OnLog;
                keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
                // Batch mode has no focused game view: let device input through anyway.
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                yield return null;
                IEnumerator steps = Steps();
                while (true)
                {
                    bool more;
                    try { more = steps.MoveNext(); }
                    catch (System.Exception e) { Check(false, "test step threw", e.ToString()); break; }
                    if (!more) break;
                    yield return steps.Current;
                }
                Check(errors == 0, "no errors or exceptions logged", errors + " " + firstError);
                if (knownIssue != null) Log("INFO known pre-existing error (not counted): " + knownIssue);
                Log("DONE");
                Application.logMessageReceived -= OnLog;
                Time.timeScale = 1f;
                EditorApplication.ExitPlaymode();
            }

            // ---------------------------------------------------------------- helpers

            private IEnumerator Press(Key key)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                yield return null;
            }

            private IEnumerator MoveMouse()
            {
                Mouse mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
                if (!mouse.enabled) InputSystem.EnableDevice(mouse);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(10f, 10f), delta = new Vector2(30f, 0f) });
                yield return null;
                yield return null;
            }

            private static GameObject Selected => EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            private static string Name(GameObject go) => go != null ? go.name : "nothing";

            private static IEnumerator Wait(float seconds)
            {
                for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
            }

            private static IEnumerator WaitForScene(string name, float timeout = 10f)
            {
                for (float t = 0f; t < timeout && SceneManager.GetActiveScene().name != name; t += Time.unscaledDeltaTime) yield return null;
                yield return Wait(0.6f); // Start and the menu set-up run
            }

            private static Button ButtonCalling(string method) =>
                FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b =>
                    Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == method));

            /// <summary>Moves the focus with Down (then Up) until <paramref name="target"/> has it.</summary>
            private IEnumerator FocusWithArrows(GameObject target)
            {
                for (int i = 0; i < 8 && Selected != target; i++) yield return Press(Key.DownArrow);
                for (int i = 0; i < 8 && Selected != target; i++) yield return Press(Key.UpArrow);
            }

            private static bool FrameOn(GameObject target)
            {
                if (target == null) return false;
                Transform frame = target.transform.Find("KeyboardFocusFrame");
                return frame != null && frame.gameObject.activeInHierarchy;
            }

            // ---------------------------------------------------------------- the run

            private IEnumerator Steps()
            {
                Texture2D logoTexture = Resources.Load<Texture2D>("Brand/MazeRunnerLogo");
                RawImage menuLogo = Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None)
                    .FirstOrDefault(image => image.name == "ScreenLogo");
                Check(logoTexture != null && menuLogo != null && menuLogo.texture == logoTexture,
                    "Main Menu displays the official logo texture");
                Check(PlayableCharacters.All.Count == 6
                    && PlayableCharacters.All.All(c => !string.IsNullOrWhiteSpace(c.DisplayName))
                    && PlayableCharacters.All.Select(c => c.DisplayName).Distinct(System.StringComparer.OrdinalIgnoreCase).Count() == PlayableCharacters.All.Count,
                    "all playable characters have distinct display names");
                Check(EnemyIntel.All.Length == 12
                    && EnemyIntel.All.All(e => !string.IsNullOrWhiteSpace(e.Name))
                    && EnemyIntel.All.Select(e => e.Name).Distinct(System.StringComparer.OrdinalIgnoreCase).Count() == EnemyIntel.All.Length,
                    "all enemy types and bosses have distinct display names");
                Check(!PlayableCharacters.All.Select(c => c.DisplayName)
                    .Intersect(EnemyIntel.All.Select(e => e.Name), System.StringComparer.OrdinalIgnoreCase).Any(),
                    "playable character names do not duplicate enemy names");
                bool hadSavedCharacter = PlayerPrefs.HasKey(PlayableCharacters.SaveKey);
                string savedCharacterId = PlayerPrefs.GetString(PlayableCharacters.SaveKey, string.Empty);
                bool characterIdsStillSave = true;
                try
                {
                    foreach (var character in PlayableCharacters.All)
                    {
                        PlayableCharacters.SaveSelection(character);
                        characterIdsStillSave &= PlayableCharacters.Selected.Id == character.Id;
                    }
                }
                finally
                {
                    if (hadSavedCharacter) PlayerPrefs.SetString(PlayableCharacters.SaveKey, savedCharacterId);
                    else PlayerPrefs.DeleteKey(PlayableCharacters.SaveKey);
                    PlayerPrefs.Save();
                }
                Check(characterIdsStillSave, "character choices still save and restore by stable ID");

                var progress = LevelProgressionManager.Instance;

                // 1. Progression rules (one source of truth: unlocks + the saved current level).
                progress.ResetProgress();
                Check(progress.ResumeLevelIndex == 0, "fresh progress: Play starts Level 1", "resume " + progress.ResumeLevelIndex);
                Check(progress.IsLevelUnlocked(0) && !progress.IsLevelCleared(0)
                    && !progress.IsLevelUnlocked(1) && !progress.IsLevelCleared(1)
                    && !progress.IsLevelUnlocked(2) && !progress.IsLevelCleared(2),
                    "fresh progress: only uncleared Level 1 is unlocked");

                SceneManager.LoadScene("LevelSelect");
                yield return WaitForScene("LevelSelect");
                Button freshLevel1 = LevelButton(1), freshLevel2 = LevelButton(2), freshLevel3 = LevelButton(3);
                Check(freshLevel1 != null && freshLevel1.interactable
                    && LevelLabel(freshLevel1) == "Level 1"
                    && freshLevel1.colors.normalColor == new Color32(0x1b, 0x2b, 0x4a, 0xff),
                    "fresh Level Select: Level 1 is normally styled and uncleared");
                Check(freshLevel2 != null && !freshLevel2.interactable
                    && LevelLabel(freshLevel2) == "Level 2",
                    "fresh Level Select: Level 2 is locked");
                Check(freshLevel3 != null && !freshLevel3.interactable
                    && LevelLabel(freshLevel3) == "Level 3",
                    "fresh Level Select: Level 3 is locked");
                Check(!progress.StartLevel(1) && !progress.StartLevel(2), "locked levels cannot be started");
                SceneManager.LoadScene("MainMenu");
                yield return WaitForScene("MainMenu");

                progress.StartLevel(0);
                yield return WaitForScene("Level1");
                progress.CompleteCurrentLevel();
                Check(progress.IsLevelCleared(0) && progress.IsLevelUnlocked(1) && !progress.IsLevelCleared(1),
                    "completing Level 1 clears it and unlocks uncleared Level 2");
                PlayerPrefs.DeleteKey(ClearedPrefix + 0);
                Check(progress.IsLevelCleared(0), "legacy saved unlock restores the prior level's cleared state");
                PlayerPrefs.SetInt(ClearedPrefix + 0, 1);
                Time.timeScale = 1f;
                Check(progress.ResumeLevelIndex == 1, "returning player: Play continues the saved current level (Level 2)");
                PlayerPrefs.SetInt(LevelProgressionManager.CurrentLevelKey, 2);
                Check(progress.ResumeLevelIndex == 1, "a saved level that is still locked is never loaded", "resume " + progress.ResumeLevelIndex);
                PlayerPrefs.DeleteKey(LevelProgressionManager.CurrentLevelKey);
                Check(progress.ResumeLevelIndex == 1, "no saved current level: Play continues the furthest unlocked level");
                PlayerPrefs.SetInt(LevelProgressionManager.CurrentLevelKey, 1);
                PlayerPrefs.Save();

                // 2. Main menu by keyboard.
                SceneManager.LoadScene("MainMenu");
                yield return WaitForScene("MainMenu");
                Button play = ButtonCalling("PlayGame");
                Button settings = ButtonCalling("OpenSettings");
                Check(play != null && Selected == play.gameObject, "main menu: Play has the focus when it opens", Name(Selected));
                Check(!FrameOn(Selected), "main menu: no focus frame before any key is pressed");
                Check(play != null && play.colors.selectedColor == play.colors.normalColor,
                    "main menu default selection keeps its normal button styling");
                TMP_Text playLabel = play != null ? play.GetComponentInChildren<TMP_Text>() : null;
                Check(playLabel != null && playLabel.text.Trim() == "Play", "main menu: Play is labelled just Play, with no level number", playLabel != null ? playLabel.text : "");
                GameObject before = Selected;
                yield return Press(Key.DownArrow);
                Check(Selected != null && Selected != before && MenuNavigator.IsOpen("MainMenuPanel"), "main menu: Down moves the focus", Name(before) + " -> " + Name(Selected));
                Check(FrameOn(Selected), "main menu: a key press shows the focus frame");
                yield return MoveMouse();
                Check(Selected != null && !FrameOn(Selected), "main menu: moving the mouse hides the focus frame");
                yield return Press(Key.S);
                yield return Press(Key.W);
                yield return Press(Key.UpArrow);
                Check(Selected == before, "main menu: W/S and Up bring the focus back", Name(Selected));
                Check(FrameOn(Selected), "main menu: keys after the mouse show the focus frame again");

                // Settings: open with Enter, a slider with the arrows, close with Escape.
                if (settings != null)
                {
                    yield return FocusWithArrows(settings.gameObject);
                    Check(Selected == settings.gameObject, "main menu: arrows reach Settings", Name(Selected));
                    yield return Press(Key.Enter);
                    Check(SettingsMenu.IsOpen, "Enter opens Settings");
                    Check(!FrameOn(Selected), "Settings opens without inheriting the prior focus outline");
                    var menu = FindAnyObjectByType<SettingsMenu>();
                    var hint = menu != null ? (TMP_Text)typeof(SettingsMenu).GetField("hint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu) : null;
                    Check(hint != null && hint.preferredHeight <= hint.rectTransform.rect.height, "Settings: the controls hint fits its box (clear of the buttons)",
                        hint != null ? hint.preferredHeight.ToString("0") + " / " + hint.rectTransform.rect.height.ToString("0") : "no hint");
                    Slider slider = Selected != null ? Selected.GetComponent<Slider>() : null;
                    Check(slider != null, "Settings: a slider has the focus", Name(Selected));
                    Check(slider != null && slider.targetGraphic.color == Color.white,
                        "Settings slider does not show keyboard focus before navigation");
                    if (slider != null)
                    {
                        float v0 = slider.value;
                        yield return Press(slider.value >= slider.maxValue ? Key.LeftArrow : Key.RightArrow);
                        Check(!Mathf.Approximately(slider.value, v0), "Settings: arrows move the focused slider", v0 + " -> " + slider.value);
                        Check(slider.targetGraphic.color == PaletteButton.GreenHover,
                            "Settings slider shows focus styling after keyboard navigation");
                        slider.value = v0;
                    }
                    yield return Press(Key.DownArrow);
                    Check(Selected != null && MenuNavigator.IsOpen("SettingsPanel"), "Settings: Down moves the focus inside Settings", Name(Selected));
                    Check(FrameOn(Selected), "Settings keyboard navigation shows its focus outline");
                    yield return Press(Key.Escape);
                    Check(!SettingsMenu.IsOpen, "Escape closes Settings");
                    Check(Selected == settings.gameObject, "closing Settings gives the focus back to the Settings button", Name(Selected));
                    Check(!FrameOn(Selected), "returning to Main Menu resets the focus outline");
                }

                // Level Select: the next level focused, Escape back.
                Button levelSelect = ButtonCalling("OpenLevelSelect");
                yield return FocusWithArrows(levelSelect.gameObject);
                yield return Press(Key.Enter);
                yield return WaitForScene("LevelSelect");
                Check(SceneManager.GetActiveScene().name == "LevelSelect", "Enter on Level Select opens it");
                Button level2 = ButtonCalling("SelectLevel") != null
                    ? LevelButton(2)
                    : null;
                Check(level2 != null && Selected == level2.gameObject, "Level Select: the next level to play has the focus", Name(Selected));
                Button level1 = LevelButton(1), level3 = LevelButton(3);
                Check(level1 != null && level1.interactable && LevelLabel(level1) == "Level 1"
                    && level1.colors.normalColor == PaletteButton.Green,
                    "legacy saved progress shows completed Level 1 in green");
                Check(level2 != null && level2.interactable && LevelLabel(level2) == "Level 2"
                    && level2.colors.normalColor == new Color32(0x1b, 0x2b, 0x4a, 0xff),
                    "unlocked Level 2 remains normally styled until completed");
                Check(level3 != null && !level3.interactable && LevelLabel(level3) == "Level 3",
                    "Level 3 remains locked until Level 2 is completed");
                Check(!FrameOn(Selected), "Level Select opens without a keyboard focus outline");
                yield return Press(Key.Escape);
                yield return WaitForScene("MainMenu");
                Check(SceneManager.GetActiveScene().name == "MainMenu", "Level Select: Escape goes back to the main menu");

                // Character selection: Confirm or Back focused, Right browses, Escape back.
                SceneManager.LoadScene("Customization");
                yield return WaitForScene("Customization");
                Check(Selected != null && MenuNavigator.IsOpen("CustomizationPanel"), "character select: a button has the focus", Name(Selected));
                Check(!FrameOn(Selected), "character selection opens without a keyboard focus outline");
                TMP_Text heroName = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "OptionLabel" || t.name.Contains("Option"));
                Transform characterLayout = GameObject.Find("SelectionLayout")?.transform;
                bool rosterNamesMatch = characterLayout != null;
                bool rosterPreviewsPresent = characterLayout != null;
                foreach (var character in PlayableCharacters.All)
                {
                    Transform card = characterLayout != null ? characterLayout.Find("Card " + character.DisplayName) : null;
                    TMP_Text cardName = card != null
                        ? card.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Name")
                        : null;
                    Image portrait = card != null
                        ? card.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name == "Portrait")
                        : null;
                    rosterNamesMatch &= cardName != null && cardName.text == character.DisplayName;
                    rosterPreviewsPresent &= portrait != null && portrait.sprite != null;
                }
                int shownIndex = PlayableCharacters.IndexOf(PlayableCharacters.Selected.Id);
                Image characterPreview = characterLayout != null
                    ? characterLayout.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name == "CharacterPreview")
                    : null;
                Check(rosterNamesMatch, "character roster labels match the central display names");
                Check(rosterPreviewsPresent, "every character roster card has a preview sprite");
                Check(heroName != null && shownIndex >= 0 && heroName.text == PlayableCharacters.All[shownIndex].DisplayName
                    && characterPreview != null && characterPreview.sprite != null,
                    "selected character preview label matches its display name");
                string hero0 = heroName != null ? heroName.text : "";
                yield return Press(Key.RightArrow);
                Check(FrameOn(Selected), "character selection keyboard input shows its focus outline");
                int nextShownIndex = (shownIndex + 1) % PlayableCharacters.All.Count;
                Check(heroName != null && heroName.text == PlayableCharacters.All[nextShownIndex].DisplayName,
                    "character select: Right shows the next hero name", hero0 + " -> " + (heroName != null ? heroName.text : "?"));
                Check(Selected != null && Selected.GetComponentInChildren<TMP_Text>() != null && Selected.GetComponentInChildren<TMP_Text>().text.ToUpperInvariant().Contains("CONFIRM"),
                    "character select: a new hero puts the focus on Confirm", Name(Selected));
                GameObject confirm = Selected;
                yield return Press(Key.DownArrow);
                Selectable cs = confirm != null ? confirm.GetComponent<Selectable>() : null;
                string nav = cs != null ? cs.navigation.mode + " down=" + Name(cs.navigation.selectOnDown != null ? cs.navigation.selectOnDown.gameObject : null) : "";
                Check(Selected != null && Selected != confirm && MenuNavigator.IsOpen("CustomizationPanel"), "character select: Down moves between its buttons", Name(confirm) + " -> " + Name(Selected) + " (" + nav + ")");
                yield return Press(Key.UpArrow);
                Check(Selected == confirm, "character select: Up comes back", Name(Selected));
                yield return Press(Key.Escape);
                yield return WaitForScene("MainMenu");
                Check(SceneManager.GetActiveScene().name == "MainMenu", "character select: Escape goes back without confirming");

                // 3. Play with Space loads the saved current level.
                play = ButtonCalling("PlayGame");
                yield return FocusWithArrows(play.gameObject);
                Check(Selected == play.gameObject, "main menu: the focus is back on Play", Name(Selected));
                yield return Press(Key.Space);
                yield return WaitForScene("Level2");
                Check(SceneManager.GetActiveScene().name == "Level2", "Play (Space) loads the saved current level, Level 2", SceneManager.GetActiveScene().name);
                yield return Wait(1f);

                // 4. Gameplay and the pause menu.
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                var body = player.GetComponent<Rigidbody2D>();
                var health = player.GetComponent<PlayerHealth>();
                Check(MenuNavigator.OpenMenu == null && Selected == null, "gameplay: no menu has the keyboard", Name(Selected));
                yield return Press(Key.Escape);
                Check(Time.timeScale == 0f && MenuNavigator.IsOpen("PauseMenuPanel"), "Escape pauses");
                Button resume = ButtonCalling("Resume");
                Check(resume != null && Selected == resume.gameObject, "pause: Resume has the focus", Name(Selected));
                Check(!FrameOn(Selected), "Pause opens without a keyboard focus outline");
                float y0 = body.position.y;
                yield return Press(Key.Space);
                Check(Time.timeScale == 1f && !MenuNavigator.IsOpen("PauseMenuPanel"), "Space on Resume resumes");
                float top = y0;
                for (float t = 0f; t < 0.6f; t += Time.deltaTime) { top = Mathf.Max(top, body.position.y); yield return null; }
                Check(top - y0 < 0.3f, "the Space that pressed Resume did not make the player jump", "rose " + (top - y0).ToString("0.00"));
                Check(Selected == null, "gameplay again: nothing keeps the focus", Name(Selected));

                yield return Press(Key.Escape);
                GameObject pauseSettings = GameObject.Find("SettingsButton");
                if (pauseSettings != null)
                {
                    yield return FocusWithArrows(pauseSettings);
                    yield return Press(Key.Enter);
                    Check(SettingsMenu.IsOpen, "pause: Enter on Settings opens it");
                    Check(!FrameOn(Selected), "Pause Settings opens without a focus outline");
                    yield return Press(Key.Escape);
                    Check(!SettingsMenu.IsOpen && Time.timeScale == 0f && MenuNavigator.IsOpen("PauseMenuPanel"), "Escape from Settings returns to the pause menu (still paused)");
                    Check(Selected == pauseSettings, "the pause menu's focus is back on Settings", Name(Selected));
                }
                yield return Press(Key.Escape);
                Check(Time.timeScale == 1f, "Escape on the pause menu resumes");

                // 5. Death and respawn, then Game Over.
                health.Die();
                yield return Wait(4f);
                Check(health.CurrentHealth > 0 && body.simulated, "first death respawns the player");
                Check(MenuNavigator.OpenMenu == null, "respawn: no menu open");
                health.Die();
                yield return Wait(0.5f);
                Check(MenuNavigator.IsOpen("GameOverPanel"), "second death shows Game Over");
                Check(Selected != null && Selected.transform.IsChildOf(MenuNavigator.OpenMenu.transform), "Game Over: a button has the focus", Name(Selected));
                Check(!FrameOn(Selected), "Game Over opens without a keyboard focus outline");
                yield return Press(Key.Escape);
                Check(Time.timeScale == 0f && MenuNavigator.IsOpen("GameOverPanel"), "Escape does not resume the game under Game Over");
                yield return Press(Key.Enter); // the focused Restart
                yield return WaitForScene("Level2");
                yield return Wait(0.5f);
                Check(Time.timeScale == 1f && MenuNavigator.OpenMenu == null, "Enter on Restart restarts the level");
                Check(progress.ResumeLevelIndex == 1, "restarting keeps Level 2 as the current level");

                // 6. Level complete: unlocks Level 3, Play moves on to it.
                progress.CompleteCurrentLevel();
                yield return Wait(0.3f);
                Check(MenuNavigator.IsOpen("LevelCompletePanel"), "completing the level shows Level Complete");
                Check(Selected != null && Selected.transform.IsChildOf(MenuNavigator.OpenMenu.transform), "Level Complete: a button has the focus", Name(Selected));
                Check(progress.IsLevelUnlocked(2) && progress.ResumeLevelIndex == 2, "completing Level 2 unlocks Level 3 and Play continues there", "resume " + progress.ResumeLevelIndex);
                Check(progress.IsLevelCleared(1) && !progress.IsLevelCleared(2), "Level 2 is cleared while unlocked Level 3 is not");
                Check(!FrameOn(Selected), "Level Complete opens without a keyboard focus outline");

                // 7. Quitting mid-level: Play restarts that level.
                Time.timeScale = 1f;
                progress.StartLevel(2);
                yield return WaitForScene("Level3");
                yield return Press(Key.Escape);
                Button quit = ButtonCalling("ReturnToMainMenu");
                if (quit != null && quit.transform.IsChildOf(MenuNavigator.OpenMenu.transform))
                {
                    yield return FocusWithArrows(quit.gameObject);
                    yield return Press(Key.Enter);
                }
                else { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }
                yield return WaitForScene("MainMenu");
                Check(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1f, "pause > Main Menu returns to the menu, unpaused");
                Check(progress.ResumeLevelIndex == 2, "after quitting Level 3, Play continues Level 3", "resume " + progress.ResumeLevelIndex);
                yield return Press(Key.Enter); // Play has the focus
                yield return WaitForScene("Level3");
                Check(SceneManager.GetActiveScene().name == "Level3", "Play restarts the level that was quit", SceneManager.GetActiveScene().name);
                Check(!progress.IsLevelCleared(2), "Level 3 remains uncleared before completion");
                progress.CompleteCurrentLevel();
                Check(progress.IsLevelCleared(2), "completing Level 3 marks it cleared");
                Time.timeScale = 1f;
            }
        }

        private static Button LevelButton(int level) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b =>
                b.GetComponentInChildren<TMP_Text>() != null && b.GetComponentInChildren<TMP_Text>().text.StartsWith("Level " + level));

        private static string LevelLabel(Button button) =>
            button != null && button.GetComponentInChildren<TMP_Text>() != null
                ? button.GetComponentInChildren<TMP_Text>().text.Trim()
                : string.Empty;
    }
}
