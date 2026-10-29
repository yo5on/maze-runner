using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MazeRunner.Enemies;
using MazeRunner.Levels;
using MazeRunner.Player;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Plays each level's boss fight in Play Mode with a scripted player and checks the results:
    /// the right boss is there, the goal stays shut while it lives, stomps from above land often but are
    /// sometimes dodged, pause freezes the AI, a player death resets the boss, and defeating everything
    /// opens the goal and completes the level. Results: Logs/boss-smoke.txt.
    ///
    /// Platform safety (the boss and up to three kinds of regular enemy per level): at both edges, the
    /// player's body running into it, eight weak hits, three strong hits, body jumps into it (and stomps
    /// for bosses) - it must stay on its platform, never snap more than 0.35 a physics step, not jitter
    /// against the edge, and move normally afterwards; a strong hit mid-platform staggers it but leaves it on.
    ///
    /// Prediction (the boss, a basic and an elite enemy): real jumps driven through a virtual keyboard
    /// (the player's own controller and physics) - direct stomp jumps, vertical hops beside it, jumps
    /// that land short on purpose, and repeated approaches from one side. It must read the arcs, react
    /// before impact some of the time but not every time, still be stompable, ignore jumps that cannot
    /// land on it, learn from repeated attempts, and use its own defence (Ogre Warlord: sidestep,
    /// Plague Alchemist: evade, Goblin Warchief: dodge / readied counter). Random is seeded per level.
    ///
    /// Menu: Maze Runner > Run Boss Smoke Test
    /// CLI:  Unity -batchmode -projectPath . -executeMethod MazeRunner.EditorTools.BossEncounterSmokeTest.RunFromCommandLine
    ///       (no -quit: it leaves the editor itself, exit code 1 on any failure)
    /// </summary>
    [InitializeOnLoad]
    public static class BossEncounterSmokeTest
    {
        private const string ActiveKey = "BossSmoke.Active";
        private const string LevelKey = "BossSmoke.Level";
        private const string ExitKey = "BossSmoke.Exit";
        private const string LastKey = "BossSmoke.Last";
        private const string SeedKey = "BossSmoke.Seed";
        private const string ResultFile = "Logs/boss-smoke.txt";
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Levels/Level1.unity", "Assets/Scenes/Levels/Level2.unity", "Assets/Scenes/Levels/Level3.unity",
        };
        private static readonly System.Type[] Brains = { typeof(OgreWarlordBehaviour), typeof(AlchemistBossBehaviour), typeof(BossHunterBehaviour) };

        static BossEncounterSmokeTest()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Maze Runner/Run Boss Smoke Test")]
        public static void Run() => Begin(false);

        public static void RunFromCommandLine() => Begin(true);

        private static void Begin(bool exitWhenDone)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultFile, "Boss smoke test " + System.DateTime.Now + "\n");
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(ExitKey, exitWhenDone);
            // -bossSmokeLevel N (1-3) runs one level only, for quicker iteration.
            string[] args = System.Environment.GetCommandLineArgs();
            int only = System.Array.IndexOf(args, "-bossSmokeLevel");
            int first = only >= 0 && only + 1 < args.Length && int.TryParse(args[only + 1], out int n) ? Mathf.Clamp(n, 1, Scenes.Length) - 1 : 0;
            int seedAt = System.Array.IndexOf(args, "-bossSmokeSeed");
            int seed = seedAt >= 0 && seedAt + 1 < args.Length && int.TryParse(args[seedAt + 1], out int parsedSeed) ? parsedSeed : 0;
            SessionState.SetInt(SeedKey, seed);
            SessionState.SetInt(LastKey, only >= 0 ? first : Scenes.Length - 1);
            SessionState.SetInt(LevelKey, first);
            Unlocks.Save();
            StartLevel();
        }

        private static void StartLevel()
        {
            int level = SessionState.GetInt(LevelKey, 0);
            if (level >= Scenes.Length || level > SessionState.GetInt(LastKey, Scenes.Length - 1)) { Finish(); return; }
            EditorSceneManager.OpenScene(Scenes[level], OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void Finish()
        {
            SessionState.SetBool(ActiveKey, false);
            Unlocks.Restore();
            string text = File.ReadAllText(ResultFile);
            bool failed = text.Contains("FAIL");
            File.AppendAllText(ResultFile, failed ? "RESULT: FAILED\n" : "RESULT: ALL PASSED\n");
            Debug.Log("[BossSmoke]\n" + File.ReadAllText(ResultFile));
            if (SessionState.GetBool(ExitKey, false)) EditorApplication.Exit(failed ? 1 : 0);
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                int level = SessionState.GetInt(LevelKey, 0);
                int seed = 1000 + level + 100 * SessionState.GetInt(SeedKey, 0);
                Time.captureDeltaTime = Time.fixedDeltaTime;
                Random.InitState(seed);
                Log("INFO L" + (level + 1) + " deterministic Unity Random seed " + seed + " and capture step " + Time.captureDeltaTime.ToString("0.000") + " applied before scene Start");
                var go = new GameObject("BossSmokeDriver");
                go.AddComponent<Driver>().Level = level;
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                Time.captureDeltaTime = 0f;
                SessionState.SetInt(LevelKey, SessionState.GetInt(LevelKey, 0) + 1);
                EditorApplication.delayCall += StartLevel;
            }
        }

        internal static void Log(string line)
        {
            File.AppendAllText(ResultFile, line + "\n");
            Debug.Log("[BossSmoke] " + line);
        }

        /// <summary>The level unlocks the test run earns are put back afterwards.</summary>
        private static class Unlocks
        {
            private const string Prefix = "MazeRunner.LevelUnlocked.";
            private const string ClearedPrefix = "MazeRunner.LevelCleared.";
            public static void Save()
            {
                for (int i = 0; i < 4; i++)
                {
                    SessionState.SetInt("BossSmoke.Unlock" + i, PlayerPrefs.HasKey(Prefix + i) ? PlayerPrefs.GetInt(Prefix + i) : -1);
                    SessionState.SetInt("BossSmoke.Cleared" + i, PlayerPrefs.HasKey(ClearedPrefix + i) ? PlayerPrefs.GetInt(ClearedPrefix + i) : -1);
                }
                string current = LevelProgressionManager.CurrentLevelKey;
                SessionState.SetInt("BossSmoke.Current", PlayerPrefs.HasKey(current) ? PlayerPrefs.GetInt(current) : -1);
            }
            public static void Restore()
            {
                for (int i = 0; i < 4; i++)
                {
                    int v = SessionState.GetInt("BossSmoke.Unlock" + i, -1);
                    if (v < 0) PlayerPrefs.DeleteKey(Prefix + i); else PlayerPrefs.SetInt(Prefix + i, v);
                    int cleared = SessionState.GetInt("BossSmoke.Cleared" + i, -1);
                    if (cleared < 0) PlayerPrefs.DeleteKey(ClearedPrefix + i); else PlayerPrefs.SetInt(ClearedPrefix + i, cleared);
                }
                int c = SessionState.GetInt("BossSmoke.Current", -1);
                if (c < 0) PlayerPrefs.DeleteKey(LevelProgressionManager.CurrentLevelKey); else PlayerPrefs.SetInt(LevelProgressionManager.CurrentLevelKey, c);
                PlayerPrefs.Save();
            }
        }

        private class Driver : MonoBehaviour
        {
            public int Level;
            private string L => "L" + (Level + 1);
            private GoblinEnemy boss;
            private readonly Dictionary<string, int> traces = new Dictionary<string, int>();
            private bool completed;

            private void Check(bool ok, string what, string detail = "") =>
                Log((ok ? "PASS " : "FAIL ") + L + " " + what + (detail.Length > 0 ? " - " + detail : ""));

            private void OnTrace(EnemyBehaviour who, string what)
            {
                if (boss == null || who == null || who.gameObject != boss.gameObject) return;
                string key = what.Split(' ')[0] == "state" ? what : what.Split(' ')[0];
                traces[key] = traces.TryGetValue(key, out int n) ? n + 1 : 1;
            }

            private void OnAnyTrace(EnemyBehaviour who, string what) => events.Add((who, what, Time.time));

            private void OnLog(string message, string stack, LogType type)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                if (errorsLogged++ == 0) firstError = message;
            }

            private IEnumerator Start()
            {
                EnemyBehaviour.Traced += OnTrace;
                EnemyBehaviour.Traced += OnAnyTrace;
                Application.logMessageReceived += OnLog;
                yield return new WaitForSeconds(1f);
                try { Setup(); }
                catch (System.Exception e) { Check(false, "setup", e.Message); }
                if (boss != null) yield return Scenario();
                Release();
                if (inputSettingsChanged)
                {
                    InputSystem.settings.backgroundBehavior = savedBackground;
                    InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorInput;
                }
                Check(errorsLogged == 0, "no errors or exceptions logged during the level", errorsLogged + " " + firstError);
                Application.logMessageReceived -= OnLog;
                EnemyBehaviour.Traced -= OnTrace;
                EnemyBehaviour.Traced -= OnAnyTrace;
                Time.timeScale = 1f;
                Time.captureDeltaTime = 0f;
                EditorApplication.ExitPlaymode();
            }

            // ---------------------------------------------------------------- shared helpers

            private readonly List<(EnemyBehaviour who, string what, float time)> events = new List<(EnemyBehaviour, string, float)>();
            private int errorsLogged;
            private string firstError = "";
            private Keyboard keyboard;
            private bool keysDrive;
            private PlayerController controller;
            private float savedAccel = -1f, savedDecel;
            private InputSettings.BackgroundBehavior savedBackground;
            private InputSettings.EditorInputBehaviorInPlayMode savedEditorInput;
            private bool inputSettingsChanged;
            private bool unexpectedPlayerDeath;
            private string unexpectedDeathReason = "";

            private static readonly FieldInfo HealthField = typeof(GoblinEnemy).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly FieldInfo AccelField = typeof(PlayerController).GetField("acceleration", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly FieldInfo DecelField = typeof(PlayerController).GetField("deceleration", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly FieldInfo JumpForceField = typeof(PlayerController).GetField("jumpForce", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly FieldInfo ReactionCooldownField = typeof(EnemyBehaviour).GetField("nextReaction", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly FieldInfo PunishCooldownField = typeof(BossHunterBehaviour).GetField("nextPunish", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly FieldInfo DecisionField = typeof(BossHunterBehaviour).GetField("nextDecision", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly FieldInfo ReadMissChanceField = typeof(BossHunterBehaviour).GetField("readMissChance", BindingFlags.Instance | BindingFlags.NonPublic);

            /// <summary>Test-only: puts an enemy back to full health so a long trial cannot kill it.</summary>
            private static void RestoreHealth(GoblinEnemy e) => HealthField.SetValue(e, e.MaxHealth);

            private void KeepAlive() { if (health.CurrentHealth < 4) health.Heal(10); }

            private float JumpSpeed => (float)JumpForceField.GetValue(controller) / playerBody.mass;

            private void Keys(params Key[] keys)
            {
                if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            }

            /// <summary>
            /// Holds left/right and jump: with the virtual keyboard through the player's own controller, or,
            /// if that keyboard does not reach the player (or a jump needs less than full running speed to fit
            /// its platform), by giving the body that speed with the controller's own acceleration switched off.
            /// </summary>
            private void Hold(int dir, bool jump, float speed = 7f)
            {
                if (keysDrive && speed >= 6.99f)
                {
                    var keys = new List<Key>();
                    if (dir > 0) keys.Add(Key.D); else if (dir < 0) keys.Add(Key.A);
                    if (jump) keys.Add(Key.Space);
                    Keys(keys.ToArray());
                    return;
                }
                if (savedAccel < 0f)
                {
                    savedAccel = (float)AccelField.GetValue(controller);
                    savedDecel = (float)DecelField.GetValue(controller);
                    AccelField.SetValue(controller, 0f);
                    DecelField.SetValue(controller, 0f);
                }
                Vector2 v = playerBody.linearVelocity;
                if (jump && controller.IsGrounded && v.y <= 0.1f) v.y = JumpSpeed;
                playerBody.linearVelocity = new Vector2(dir * speed, v.y);
            }

            private void Release()
            {
                Keys();
                if (savedAccel >= 0f && controller != null)
                {
                    AccelField.SetValue(controller, savedAccel);
                    DecelField.SetValue(controller, savedDecel);
                    savedAccel = -1f;
                }
            }

            /// <summary>Checks that a virtual keyboard moves the player through its controller (the real input path).</summary>
            private IEnumerator SetupKeyboard()
            {
                controller = player.GetComponent<PlayerController>();
                keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
                // Batch mode has no focused game view: let device input through anyway (restored afterwards).
                savedBackground = InputSystem.settings.backgroundBehavior;
                savedEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                inputSettingsChanged = true;
                var playerInput = player.GetComponent<PlayerInput>();
                if (playerInput != null && playerInput.currentControlScheme != "Keyboard&Mouse")
                {
                    try { playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard); }
                    catch (System.Exception ex) { Log("INFO " + L + " keyboard scheme switch failed: " + ex.Message); }
                }
                bool leftRoomier = boss.transform.position.x - arena.min.x > arena.max.x - boss.transform.position.x;
                int dir = leftRoomier ? 1 : -1;
                Place(new Vector2(leftRoomier ? arena.min.x + 1.5f : arena.max.x - 1.5f, arena.max.y + 1f), Vector2.zero);
                yield return new WaitForSeconds(0.4f);
                float x0 = player.transform.position.x;
                Keys(dir > 0 ? Key.D : Key.A);
                yield return new WaitForSeconds(0.35f);
                Keys();
                keysDrive = (player.transform.position.x - x0) * dir > 0.8f;
                Log("INFO " + L + " virtual keyboard drives the player's controller: " + keysDrive +
                    (keysDrive ? "" : " (falling back to driving the body directly)"));
            }

            private GameObject player;
            private PlayerHealth health;
            private Rigidbody2D playerBody;
            private Bounds arena;
            private Vector3 bossStart;
            private string stillNote = "";
            private string dropNotes = "";

            private void Setup()
            {
                player = GameObject.FindGameObjectWithTag("Player");
                health = player.GetComponent<PlayerHealth>();
                playerBody = player.GetComponent<Rigidbody2D>();
                GoblinEnemy[] bosses = SceneEnemies().Where(g => g.IsBoss).ToArray();
                Check(bosses.Length == 1, "exactly one boss", bosses.Length + " found");
                if (bosses.Length == 0) return;
                boss = bosses[0];
                var brain = boss.GetComponent<EnemyBehaviour>();
                Check(brain != null && brain.GetType() == Brains[Level], "boss personality", boss.name + " / " + (brain != null ? brain.GetType().Name : "none"));
                Check(boss.Platform != null, "boss stands on an arena platform");
                arena = boss.Platform.bounds;
                bossStart = boss.transform.position;
                Check(arena.size.x >= 20f, "arena is roomy", arena.size.x.ToString("0.0") + " wide");
                var goal = FindAnyObjectByType<LevelGoal>();
                Check(goal != null && goal.transform.position.x > arena.min.x && goal.transform.position.x < arena.max.x,
                    "goal is in the boss arena");
                LevelProgressionManager.Instance.LevelCompleted += _ => completed = true;
                health.OnPlayerDeath += OnPlayerDied;
                trialWaitSpot = playerBody.position;
                safeSpot = new Vector2(arena.center.x, arena.max.y + 1f);
            }

            private bool BossOnArena() =>
                boss != null && boss.transform.position.x >= arena.min.x - 0.05f && boss.transform.position.x <= arena.max.x + 0.05f &&
                boss.transform.position.y >= arena.max.y - 0.6f;

            private Vector2 safeSpot;
            private Vector2 trialWaitSpot;

            private void Update()
            {
                // A test move that drops the player off a platform must not end the run in a game over.
                if (player != null && player.transform.position.y < -6f) Place(safeSpot, Vector2.zero);
                if (unexpectedPlayerDeath && Time.timeScale == 0f) Time.timeScale = 1f;
                // The scripted player never dies by accident (it has one respawn, which the death test uses).
                if (health != null && !expectingDeath && !unexpectedPlayerDeath) KeepAlive();
                // Watchdog: a game stuck paused (a game over) fails the level instead of hanging the run.
                pausedSince = Time.timeScale > 0f ? -1f : pausedSince < 0f ? Time.realtimeSinceStartup : pausedSince;
                if (pausedSince >= 0f && Time.realtimeSinceStartup - pausedSince > 8f)
                {
                    Check(false, "game stayed paused for 8 s (game over?)", "player health " + (health != null ? health.CurrentHealth : -1));
                    Time.timeScale = 1f;
                    EditorApplication.ExitPlaymode();
                }
            }

            private float pausedSince = -1f;
            private bool expectingDeath;

            private void OnPlayerDied()
            {
                if (expectingDeath) return;
                unexpectedPlayerDeath = true;
                unexpectedDeathReason = "player died at " + player.transform.position.ToString("F1");
                Check(false, "the scripted player died unexpectedly", "health " + health.CurrentHealth + " at " + player.transform.position.ToString("F1"));
            }

            private void Place(Vector2 p, Vector2 v)
            {
                playerBody.position = p;
                player.transform.position = p;
                playerBody.linearVelocity = v;
            }

            private IEnumerator Scenario()
            {
                var goal = FindAnyObjectByType<LevelGoal>();
                float floor = arena.max.y;

                // 1. The goal stays shut while the boss lives.
                Place(new Vector2(goal.transform.position.x, floor + 1f), Vector2.zero);
                yield return new WaitForSeconds(0.6f);
                Check(!completed, "goal blocked while the boss lives");
                yield return SetupKeyboard();
                if (unexpectedPlayerDeath) { Log("ABORT " + L + " scenario before fight: " + unexpectedDeathReason); yield break; }

                // 1b. A player waiting at the far end of the arena (just past its entrance) still draws the boss out.
                float farX = boss.transform.position.x > arena.center.x ? arena.min.x + 1.5f : arena.max.x - 1.5f;
                Place(new Vector2(farX, floor + 1f), Vector2.zero);
                float startGap = Mathf.Abs(boss.transform.position.x - farX);
                for (float t = 0f; t < 10f && Mathf.Abs(boss.transform.position.x - farX) > startGap - 4f; t += Time.deltaTime) { KeepAlive(); yield return null; }
                float endGap = Mathf.Abs(boss.transform.position.x - farX);
                Check(endGap <= startGap - 4f, "boss comes for a player waiting at the far end of its arena",
                    startGap.ToString("0.0") + " -> " + endGap.ToString("0.0") + " units apart");
                // Back to its post, so the fight below starts where it always has.
                boss.Body.position = bossStart;
                boss.transform.position = bossStart;
                boss.Body.linearVelocity = Vector2.zero;

                // 2. A fight: the player hangs around the boss and drops onto its head every 2.6 s.
                int startHp = boss.CurrentHealth;
                int drops = 0, landed = 0;
                float minY = float.MaxValue, lastX = boss.transform.position.x, still = 0f, longestStill = 0f;
                bool leftArena = false;
                Place(new Vector2(boss.transform.position.x - 4f, floor + 1f), Vector2.zero);
                for (float t = 0f; t < 26f && boss != null && !boss.IsDead && !unexpectedPlayerDeath; t += Time.deltaTime)
                {
                    if (health.CurrentHealth < 4) health.Heal(10); // keep the scripted player alive
                    if (player.transform.position.y < floor - 1.5f) Place(new Vector2(arena.center.x, floor + 1f), Vector2.zero);
                    if (t >= 2.6f * (drops + 1) && boss.CurrentHealth > 2)
                    {
                        int before = boss.CurrentHealth;
                        Bounds b = boss.BodyCollider.bounds;
                        Place(new Vector2(b.center.x, b.max.y + 2.8f), new Vector2(0f, 2f)); // near the top of a stomp jump (feet 1.8 above the head)
                        drops++;
                        var brainNow = boss.GetComponent<EnemyBehaviour>();
                        MethodInfo incoming = typeof(EnemyBehaviour).GetMethod("StompIncoming", BindingFlags.Instance | BindingFlags.NonPublic);
                        int threat = 0, busy = 0, frames = 0;
                        for (float w = 0f; w < 0.9f; w += Time.deltaTime)
                        {
                            object[] args = { 0.55f, 0f };
                            if ((bool)incoming.Invoke(brainNow, args)) threat++;
                            if (brainNow.IsBusy) busy++;
                            frames++;
                            yield return null;
                        }
                        dropNotes += " [" + threat + "/" + busy + "/" + frames + (boss.CurrentHealth < before ? " hit" : " miss") + " moved " + (boss.transform.position.x - b.center.x).ToString("0.0") + "]";
                        t += 0.9f;
                        if (boss.CurrentHealth < before) landed++;
                        // Step back off to the side so the boss can fight.
                        float side = boss.transform.position.x > arena.center.x ? -3.5f : 3.5f;
                        Place(new Vector2(Mathf.Clamp(boss.transform.position.x + side, arena.min.x + 1f, arena.max.x - 1f), floor + 1f), Vector2.zero);
                    }
                    minY = Mathf.Min(minY, boss.transform.position.y);
                    leftArena |= !BossOnArena();
                    float x = boss.transform.position.x;
                    bool idleAway = !boss.GetComponent<EnemyBehaviour>().IsBusy && Mathf.Abs(player.transform.position.x - x) > 2.5f;
                    still = Mathf.Abs(x - lastX) < 0.001f && idleAway ? still + Time.deltaTime : 0f;
                    if (still > longestStill && still > 3f)
                    {
                        var brain = boss.GetComponent<EnemyBehaviour>();
                        FieldInfo engaged = brain.GetType().GetField("engaged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy) ??
                                            brain.GetType().BaseType.GetField("engaged", BindingFlags.Instance | BindingFlags.NonPublic);
                        stillNote = "boss " + boss.transform.position.ToString("F2") + " player " + player.transform.position.ToString("F2") +
                                    " busy=" + brain.IsBusy + " engaged=" + (engaged != null ? engaged.GetValue(brain) : "?") + " t=" + t.ToString("0.0");
                    }
                    longestStill = Mathf.Max(longestStill, still);
                    lastX = x;
                    yield return null;
                }
                if (unexpectedPlayerDeath) { Log("ABORT " + L + " scenario before prediction: " + unexpectedDeathReason); yield break; }
                string moves = string.Join(", ", traces.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value));
                Log("INFO " + L + " fight: " + drops + " drops, " + landed + " landed, hp " + startHp + " -> " + boss.CurrentHealth + "; moves: " + moves + "; threat/busy/frames per drop:" + dropNotes);
                Check(!leftArena, "boss stayed on its arena during the fight", "lowest y " + minY.ToString("0.00") + " vs floor " + floor.ToString("0.00"));
                Check(landed >= 1, "stomps can land", landed + "/" + drops);
                Check(longestStill < 6f, "boss never stood idle and stuck with the player away", longestStill.ToString("0.0") + " s " + stillNote);
                if (Level > 0) Check(landed < drops || drops < 3, "boss sometimes reads a stomp", landed + "/" + drops + " landed");
                if (Level > 0 && !traces.ContainsKey("phase"))
                {
                    // Not enough scripted stomps landed: bring it to half health and let it react.
                    while (boss.CurrentHealth > boss.MaxHealth / 2) boss.TakeDamage(1);
                    Place(new Vector2(Mathf.Clamp(boss.transform.position.x - 4f, arena.min.x + 1f, arena.max.x - 1f), floor + 1f), Vector2.zero);
                    for (float t = 0f; t < 6f && !traces.ContainsKey("phase"); t += Time.deltaTime) { if (health.CurrentHealth < 4) health.Heal(10); yield return null; }
                }
                if (Level > 0) Check(traces.ContainsKey("phase"), "boss enters phase 2 at half health");
                if (Level == 2)
                {
                    Check(traces.Keys.Any(k => k.Contains("Stalk")), "warchief stalks");
                    Check(traces.ContainsKey("combo") || traces.ContainsKey("feint") || traces.ContainsKey("vault"), "warchief attacks and repositions");
                }

                // 2b. Prediction: real jumps at the boss, deliberate misses, vertical hops, approaches.
                RestoreHealth(boss);
                yield return PredictionTrials(boss, "boss", Level + 1);
                if (unexpectedPlayerDeath) { Log("ABORT " + L + " scenario after boss prediction: " + unexpectedDeathReason); yield break; }
                yield return ApproachTrials(boss, "boss");
                RestoreHealth(boss);

                // 3. Edge safety at both arena edges.
                foreach (int side in new[] { 1, -1 }) yield return EdgeTrial(boss, side, "boss");
                Check(BossOnArena(), "boss still in its arena after the edge trials");

                // 3b. Getting unstuck (cornered at both edges, perched on the player, a move that never ends).
                yield return StuckTrials();
                // 3c. The Goblin Warchief's slide.
                if (Level == 2) yield return SlideTrials();
                RestoreHealth(boss);

                // 4. Pause freezes it.
                Vector3 pausedAt = boss.transform.position;
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(1f);
                Check((boss.transform.position - pausedAt).sqrMagnitude < 0.0001f, "boss frozen while paused");
                Time.timeScale = 1f;

                // 5. A player death restarts the fight cleanly.
                if (boss.CurrentHealth == boss.MaxHealth) boss.TakeDamage(1);
                int hurt = boss.CurrentHealth;
                expectingDeath = true;
                health.Die();
                yield return new WaitForFixedUpdate();
                float fromPost = Vector2.Distance(boss.transform.position, bossStart);
                yield return new WaitForSeconds(4f);
                Check(boss.CurrentHealth == boss.MaxHealth && hurt < boss.MaxHealth, "player death resets the boss's health", hurt + " -> " + boss.CurrentHealth);
                Check(fromPost < 0.5f, "boss returned to its post", fromPost.ToString("0.00") + " from it at the reset, " +
                    Vector2.Distance(boss.transform.position, bossStart).ToString("0.00") + " after 4 s");
                Check(health.CurrentHealth > 0 && player.GetComponent<Rigidbody2D>().simulated, "player respawned");
                expectingDeath = false;
                var bossBrain = boss.GetComponent<EnemyBehaviour>();
                Check(bossBrain.RecentStompAttempts == 0 && bossBrain.ExpectedApproachSide == 0, "the reset boss forgets the last fight's stomps and approaches");
                // The AI picks the fight up again once the player is back in the arena.
                int actsBefore = events.Count(ev => ev.who == bossBrain);
                Place(new Vector2(Mathf.Clamp(boss.transform.position.x - 3.5f, arena.min.x + 1f, arena.max.x - 1f), floor + 1f), Vector2.zero);
                for (float t = 0f; t < 6f && events.Count(ev => ev.who == bossBrain) == actsBefore; t += Time.deltaTime) { KeepAlive(); yield return null; }
                Check(events.Count(ev => ev.who == bossBrain) > actsBefore, "boss AI acts again after the respawn");

                // 6. Regular enemies: platform safety for each kind, prediction for a basic and an elite one.
                yield return RegularEnemyTrials();

                // 7. Defeat everything; the goal now completes the level.
                MethodInfo die = typeof(GoblinEnemy).GetMethod("Die", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (GoblinEnemy g in FindObjectsByType<GoblinEnemy>()) if (!g.IsDead && g != boss) die.Invoke(g, null);
                foreach (WraithEnemy w in FindObjectsByType<WraithEnemy>()) w.TakeDamage(999);
                yield return new WaitForSeconds(0.5f);
                Place(new Vector2(goal.transform.position.x, floor + 1f), Vector2.zero);
                yield return new WaitForSeconds(0.5f);
                Check(!completed && EnemyTracker.Instance.RemainingEnemyCount == 1, "goal still shut with only the boss left",
                    EnemyTracker.Instance.RemainingEnemyCount + " left");
                Place(new Vector2(arena.min.x + 1.5f, floor + 1f), Vector2.zero);
                for (int i = 0; i < 40 && !boss.IsDead; i++) boss.TakeDamage(1);
                Check(boss.IsDead, "boss can be defeated");
                yield return new WaitForSeconds(4f);
                Check(boss == null, "boss removed after its death animation");
                Check(EnemyTracker.Instance.RemainingEnemyCount == 0, "every enemy counted as defeated");
                var bar = FindAnyObjectByType<BossHealthBar>();
                Check(bar == null, "boss health bar removed with the boss");
                Place(new Vector2(goal.transform.position.x, floor + 1f), Vector2.zero);
                yield return new WaitForSecondsRealtime(0.6f); // the level-complete screen pauses the game
                Check(completed, "goal completes the level once the boss is down");
            }

            // ---------------------------------------------------------------- stuck recovery

            private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            private static readonly MethodInfo CancelSpecial = typeof(EnemyBehaviour).GetMethod("CancelSpecial", Hidden);
            private static readonly MethodInfo RunSpecial = typeof(EnemyBehaviour).GetMethod("RunSpecial", Hidden);

            private static IEnumerator Forever() { while (true) yield return null; }

            /// <summary>Puts the boss's body centre at <paramref name="x"/> on the arena floor, standing still and calm.</summary>
            private void PlaceBoss(float x)
            {
                CancelSpecial.Invoke(boss.GetComponent<EnemyBehaviour>(), null);
                float offset = boss.BodyCollider.bounds.center.x - boss.Body.position.x;
                boss.Body.position = new Vector2(x - offset, boss.Body.position.y);
                boss.Body.linearVelocity = Vector2.zero;
            }

            private IEnumerator StuckTrials()
            {
                var brain = boss.GetComponent<EnemyBehaviour>();
                float floor = arena.max.y;
                float half = boss.BodyCollider.bounds.extents.x;

                // Cornered: backed against an arena edge with the player planted right in front of it.
                foreach (int side in new[] { 1, -1 })
                {
                    RestoreHealth(boss);
                    float edgeX = (side > 0 ? arena.max.x : arena.min.x) - side * (half + 0.3f);
                    PlaceBoss(edgeX);
                    float playerX = edgeX - side * (half + 1.3f);
                    Place(new Vector2(playerX, floor + 0.9f), Vector2.zero);
                    int mark = events.Count;
                    bool left = false;
                    float startX = boss.transform.position.x, furthest = 0f;
                    for (float t = 0f; t < 6f; t += Time.deltaTime)
                    {
                        KeepAlive();
                        if (Mathf.Abs(player.transform.position.x - playerX) > 2.5f || player.transform.position.y < floor - 0.5f)
                            Place(new Vector2(playerX, floor + 0.9f), Vector2.zero);
                        left |= !BossOnArena();
                        furthest = Mathf.Max(furthest, Mathf.Abs(boss.transform.position.x - startX));
                        yield return null;
                    }
                    var acts = events.Skip(mark).Where(ev => ev.who == brain).Select(ev => ev.what).ToList();
                    bool answered = acts.Any(a => a.StartsWith("windup") || a.StartsWith("slide") || a.StartsWith("vault") || a.StartsWith("hop") ||
                                                  a.StartsWith("sidestep") || a.StartsWith("evade") || a.StartsWith("dodge") || a.StartsWith("stuck"));
                    string edge = side > 0 ? "right" : "left";
                    Check(answered || furthest > 1f, "boss cornered at the " + edge + " edge fights or repositions out",
                        "moved " + furthest.ToString("0.0") + "; " + string.Join(", ", acts.Take(10)));
                    Check(!left, "boss cornered at the " + edge + " edge stays on its arena");
                }

                // Perched: put on top of a player who stands still, it must get back down to its floor.
                RestoreHealth(boss);
                float px = Mathf.Clamp(arena.center.x, arena.min.x + 4f, arena.max.x - 4f);
                PlaceBoss(arena.min.x + 2f);
                Place(new Vector2(px, floor + 0.9f), Vector2.zero);
                yield return new WaitForSeconds(0.4f);
                Bounds pb = player.GetComponent<Collider2D>().bounds;
                CancelSpecial.Invoke(brain, null);
                float lift = boss.Body.position.y - boss.BodyCollider.bounds.min.y;
                float offset = boss.BodyCollider.bounds.center.x - boss.Body.position.x;
                boss.Body.position = new Vector2(pb.center.x - offset, pb.max.y + lift + 0.05f);
                boss.Body.linearVelocity = Vector2.zero;
                float down = -1f;
                for (float t = 0f; t < 4f; t += Time.deltaTime)
                {
                    KeepAlive();
                    playerBody.position = new Vector2(pb.center.x, playerBody.position.y); // the player holds still
                    playerBody.linearVelocity = new Vector2(0f, playerBody.linearVelocity.y);
                    if (t > 0.3f && boss.StandsOnPlatform) { down = t; break; }
                    yield return null;
                }
                Check(down > 0f, "boss perched on the player's head gets back down to its arena floor", down > 0f ? down.ToString("0.0") + " s" : "still up after 4 s");
                Check(BossOnArena(), "...and stays on its arena");

                // A move that never ends: the watchdog cuts it short and the boss fights on.
                FieldInfo timeout = typeof(EnemyBehaviour).GetField("specialTimeout", Hidden);
                float saved = (float)timeout.GetValue(brain);
                timeout.SetValue(brain, 2f);
                PlaceBoss(arena.center.x);
                Place(new Vector2(Mathf.Clamp(arena.center.x - 4f, arena.min.x + 1f, arena.max.x - 1f), floor + 0.9f), Vector2.zero);
                int before = events.Count;
                RunSpecial.Invoke(brain, new object[] { Forever() });
                float cut = -1f;
                for (float t = 0f; t < 4f; t += Time.deltaTime)
                {
                    KeepAlive();
                    if (cut < 0f && events.Skip(before).Any(ev => ev.who == brain && ev.what == "stuck State")) cut = t;
                    yield return null;
                }
                timeout.SetValue(brain, saved);
                Check(cut > 0f, "a boss move that never ends is cut short by the state timeout", cut > 0f ? cut.ToString("0.0") + " s" : "not cut");
                Check(events.Skip(before).Count(ev => ev.who == brain) > 1, "after the timeout the boss acts again",
                    string.Join(", ", events.Skip(before).Where(ev => ev.who == brain).Select(ev => ev.what).Take(6)));
            }

            // ---------------------------------------------------------------- the warchief's slide

            private IEnumerator SlideTrials()
            {
                var brain = boss.GetComponent<BossHunterBehaviour>();
                System.Type type = typeof(BossHunterBehaviour);
                MethodInfo slide = type.GetMethod("Slide", Hidden);
                MethodInfo engageTarget = type.GetMethod("EngageTarget", Hidden);
                MethodInfo passTarget = type.GetMethod("PassTarget", Hidden);
                FieldInfo nextSlide = type.GetField("nextSlide", Hidden);
                System.Type use = type.GetNestedType("SlideUse", BindingFlags.NonPublic);
                object engage = System.Enum.Parse(use, "Engage"), pass = System.Enum.Parse(use, "Pass");
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                float floor = arena.max.y;
                float half = boss.BodyCollider.bounds.extents.x;

                IEnumerator RunSlide(float x, object how, System.Action<float, bool> done)
                {
                    RunSpecial.Invoke(brain, new object[] { slide.Invoke(brain, new object[] { x, how }) });
                    float fastest = 0f;
                    bool left = false;
                    for (float t = 0f; t < 4f && brain.IsBusy; t += Time.deltaTime)
                    {
                        KeepAlive();
                        fastest = Mathf.Max(fastest, Mathf.Abs(boss.Body.linearVelocity.x));
                        left |= !BossOnArena();
                        yield return null;
                    }
                    done(fastest, left);
                }

                // Far (6 units): telegraphed, then a fast slide in that hits a standing player.
                RestoreHealth(boss);
                float cx = Mathf.Clamp(arena.center.x, arena.min.x + 6f, arena.max.x - 6f);
                PlaceBoss(cx - 3f);
                Place(new Vector2(cx + 3f, floor + 0.9f), Vector2.zero);
                yield return new WaitForSeconds(0.3f);
                PlaceBoss(cx - 3f);
                object[] args = { 0f };
                bool ok = (bool)engageTarget.Invoke(brain, args);
                Check(ok, "slide: a player 6 units away on its floor is in slide range");
                int mark = events.Count;
                float speed = 0f;
                bool off = false;
                if (ok) yield return RunSlide((float)args[0], engage, (s, l) => { speed = s; off = l; });
                var acts = events.Skip(mark).Where(ev => ev.who == brain).Select(ev => ev.what).ToList();
                int warned = acts.FindIndex(a => a.StartsWith("windup")), went = acts.FindIndex(a => a == "slide Engage");
                Check(went > 0 && warned >= 0 && warned < went, "slide: the warning shows before the slide", string.Join(", ", acts.Take(8)));
                Check(speed >= 9f, "slide: it is fast", speed.ToString("0.0") + " u/s");
                Check(acts.Any(a => a.StartsWith("hit player")), "slide: a player standing in the lane is hit");
                Check(acts.Any(a => a == "state Recover"), "slide: after the slide it stands open (or fights on)", string.Join(", ", acts.Skip(Mathf.Max(0, went)).Take(6)));
                Check((float)nextSlide.GetValue(brain) > Time.time - 1f, "slide: its cooldown is set");
                Check(!off, "slide: stays on the arena");
                Check(!Physics2D.GetIgnoreCollision(boss.BodyCollider, playerCollider), "slide: collisions with the player are back afterwards");

                // Near and cornered at each edge: it slides past the player and comes up on the far side.
                foreach (int side in new[] { 1, -1 })
                {
                    RestoreHealth(boss);
                    float edgeX = (side > 0 ? arena.max.x : arena.min.x) - side * (half + 0.3f);
                    PlaceBoss(edgeX);
                    float playerX = edgeX - side * (half + 1.2f);
                    Place(new Vector2(playerX, floor + 0.9f), Vector2.zero);
                    yield return new WaitForSeconds(0.3f);
                    PlaceBoss(edgeX);
                    args = new object[] { 0f };
                    ok = (bool)passTarget.Invoke(brain, args);
                    string edge = side > 0 ? "right" : "left";
                    Check(ok, "slide: cornered at the " + edge + " edge it has a way past the player");
                    if (!ok) continue;
                    yield return RunSlide((float)args[0], pass, (s, l) => off = l);
                    float beyond = (boss.BodyCollider.bounds.center.x - player.transform.position.x) * -side;
                    Check(beyond > 0.5f, "slide: from the " + edge + " corner it comes up past the player", beyond.ToString("0.0"));
                    Check(!off && BossOnArena(), "slide: from the " + edge + " corner it stays on the arena");
                    Check(!Physics2D.GetIgnoreCollision(boss.BodyCollider, playerCollider), "slide: collisions back after sliding past");
                }

                // Toward an edge with the player beyond reach: it stops at the arena limit.
                RestoreHealth(boss);
                PlaceBoss(arena.max.x - 6f);
                Place(new Vector2(arena.max.x - 0.6f, floor + 0.9f), Vector2.zero);
                yield return new WaitForSeconds(0.3f);
                PlaceBoss(arena.max.x - 6f);
                yield return RunSlide(arena.max.x + 3f, engage, (s, l) => off = l);
                Check(!off && BossOnArena(), "slide: aimed past the arena edge it stops on the arena");

                // A player above it (hovering 4 units up) is never slid at.
                RestoreHealth(boss);
                PlaceBoss(cx);
                nextSlide.SetValue(brain, 0f);
                mark = events.Count;
                for (float t = 0f; t < 4f; t += Time.deltaTime)
                {
                    KeepAlive();
                    Place(new Vector2(boss.transform.position.x + 0.5f, floor + 4.5f), Vector2.zero);
                    yield return null;
                }
                Check(!events.Skip(mark).Any(ev => ev.who == brain && (ev.what.Contains("Engage") || ev.what.Contains("Pass"))),
                    "slide: never slides in at (or past) a player above it - only under a stomp");
                Place(new Vector2(Mathf.Clamp(boss.transform.position.x - 4f, arena.min.x + 1f, arena.max.x - 1f), floor + 0.9f), Vector2.zero);
            }

            // ---------------------------------------------------------------- regular enemies

            private IEnumerator RegularEnemyTrials()
            {
                // Prefer the two prediction targets, then fill the platform sample in a stable order.
                GoblinEnemy[] regulars = SceneEnemies()
                    .Where(g => !g.IsBoss && !g.IsDead && g.Platform != null && g.Platform.bounds.size.x - g.BodyCollider.bounds.size.x >= 3.5f && g.GetComponent<EnemyBehaviour>() != null)
                    .GroupBy(g => g.GetComponent<EnemyBehaviour>().GetType())
                    .OrderBy(grp => grp.Key == typeof(GoblinSkirmisherBehaviour) ? 0 : grp.Key == typeof(DuelistBehaviour) ? 1 : 2)
                    .ThenBy(grp => grp.Key.FullName, System.StringComparer.Ordinal)
                    .Select(grp => grp.OrderBy(g => StablePath(g.transform), System.StringComparer.Ordinal).First())
                    .Take(3).ToArray();
                Check(regulars.Length >= 2, "regular enemies to test", string.Join(", ", regulars.Select(DescribeEnemy)));
                Log("INFO " + L + " selected platform trials: " + string.Join("; ", regulars.Select(DescribeEnemy)));

                foreach (GoblinEnemy e in regulars)
                {
                    string who = e.GetComponent<EnemyBehaviour>().GetType().Name.Replace("Behaviour", "");
                    foreach (int side in new[] { 1, -1 })
                    {
                        if (e == null || e.IsDead) break;
                        RestoreHealth(e);
                        yield return EdgeTrial(e, side, who);
                    }
                    if (e == null || e.IsDead) continue;

                    // A strong hit mid-platform: it staggers (slides) but stays on.
                    RestoreHealth(e);
                    Bounds p = e.Platform.bounds;
                    float offset = e.BodyCollider.bounds.center.x - e.Body.position.x;
                    e.Body.position = new Vector2(p.center.x - offset, e.Body.position.y);
                    Place(new Vector2(Stand(p, p.center.x - 6f), p.max.y + 1f), Vector2.zero);
                    yield return new WaitForSeconds(0.4f);
                    float x0 = e.BodyCollider.bounds.center.x;
                    e.ApplyKnockback(new Vector2(30f, 3f));
                    yield return new WaitForSeconds(1.2f);
                    if (e == null) continue;
                    Bounds b = e.BodyCollider.bounds;
                    Check(b.min.y > p.max.y - 0.5f && b.center.x >= p.min.x && b.center.x <= p.max.x && e.IsKnockedBack == false,
                        who + ": a strong hit leaves it on its platform", "x " + x0.ToString("0.00") + " -> " + b.center.x.ToString("0.00") + " on " + p.min.x + ".." + p.max.x);
                    Check(Mathf.Abs(b.center.x - x0) > 0.25f, who + ": a strong hit still staggers it back", (b.center.x - x0).ToString("0.00"));
                }

                // Prediction for one basic and one elite enemy, when the level has them.
                GoblinEnemy basic = SelectPredictionTarget<GoblinSkirmisherBehaviour>();
                GoblinEnemy elite = SelectPredictionTarget<DuelistBehaviour>();
                if (basic != null) yield return PredictionTrials(basic, "basic goblin", 0);
                else Check(false, "basic goblin prediction target available", "SKIPPED: no GoblinSkirmisherBehaviour on a platform at least 5 units wide");
                if (unexpectedPlayerDeath) { Log("ABORT " + L + " regular-enemy trials: " + unexpectedDeathReason); yield break; }
                if (elite != null) yield return PredictionTrials(elite, "elite duelist", 0);
                else Check(false, "elite duelist prediction target available", "SKIPPED: no DuelistBehaviour on a platform at least 5 units wide");
            }

            private static GoblinEnemy[] SceneEnemies() =>
                FindObjectsByType<GoblinEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .OrderBy(g => StablePath(g.transform), System.StringComparer.Ordinal).ToArray();

            private static GoblinEnemy SelectPredictionTarget<T>() where T : EnemyBehaviour => SceneEnemies()
                .Where(g => !g.IsBoss && !g.IsDead && g.Platform != null && g.Platform.bounds.size.x >= 5f && g.GetComponent<T>() != null)
                .OrderBy(g => StablePath(g.transform), System.StringComparer.Ordinal).FirstOrDefault();

            private static string StablePath(Transform target)
            {
                var parts = new List<string>();
                while (target != null)
                {
                    parts.Add(target.GetSiblingIndex().ToString("D4") + ":" + target.name);
                    target = target.parent;
                }
                parts.Reverse();
                return string.Join("/", parts);
            }

            private static string DescribeEnemy(GoblinEnemy target) =>
                target.name + " [" + target.GetComponent<EnemyBehaviour>().GetType().Name + "] " + StablePath(target.transform);

            // ---------------------------------------------------------------- platform safety

            private readonly List<Vector2> samples = new List<Vector2>();
            private readonly List<float> pivots = new List<float>(); // body x (a turn does not move it, unlike the collider's centre)
            private bool sampling;

            private IEnumerator SampleEveryStep(GoblinEnemy e)
            {
                while (sampling && e != null)
                {
                    yield return new WaitForFixedUpdate();
                    if (e == null) break;
                    samples.Add(new Vector2(e.BodyCollider.bounds.center.x, e.BodyCollider.bounds.min.y));
                    pivots.Add(e.Body.position.x);
                }
            }

            /// <summary>
            /// At one edge: the player's body running into it, weak hits, strong hits, body jumps into it
            /// (and stomps for bosses). It must stay on, never snap, not jitter, and move freely afterwards.
            /// </summary>
            private IEnumerator EdgeTrial(GoblinEnemy e, int side, string who)
            {
                string edge = side > 0 ? "right" : "left";
                Bounds p = e.Platform.bounds;
                float floor = p.max.y;
                Bounds b0 = e.BodyCollider.bounds;
                float offset = b0.center.x - e.Body.position.x;
                float start = (side > 0 ? p.max.x : p.min.x) - side * (b0.extents.x + 0.4f);
                e.Body.position = new Vector2(start - offset, e.Body.position.y);
                e.Body.linearVelocity = Vector2.zero;
                safeSpot = new Vector2(Stand(p, start - side * 5f), floor + 1f);
                Place(safeSpot, Vector2.zero);
                yield return new WaitForSeconds(0.3f);

                samples.Clear();
                pivots.Clear();
                sampling = true;
                StartCoroutine(SampleEveryStep(e));

                // 1. The player's body running into it toward the edge for 1.5 s.
                for (float t = 0f; t < 1.5f && e != null; t += Time.deltaTime)
                {
                    KeepAlive();
                    Bounds b = e.BodyCollider.bounds;
                    float behind = b.center.x - side * (b.extents.x + 0.5f);
                    if (t == 0f || Mathf.Abs(player.transform.position.x - behind) > 1.5f || player.transform.position.y < floor - 0.5f)
                        Place(new Vector2(behind, floor + 0.9f), Vector2.zero);
                    Hold(side, false);
                    yield return null;
                }
                Release();
                int shoveSteps = samples.Count;

                // 2. Eight weak hits (a stomp's push) in a row.
                for (int i = 0; i < 8 && e != null && !e.IsDead; i++) { KeepAlive(); e.ApplyKnockback(new Vector2(side * 3f, 0f)); yield return new WaitForSeconds(0.12f); }
                // 3. Three strong hits.
                for (int i = 0; i < 3 && e != null && !e.IsDead; i++) { KeepAlive(); e.ApplyKnockback(new Vector2(side * 30f, 8f)); yield return new WaitForSeconds(0.25f); }
                // 4. The player jumping into its side (which could lift it).
                for (int i = 0; i < 3 && e != null && !e.IsDead; i++)
                {
                    KeepAlive();
                    Bounds b = e.BodyCollider.bounds;
                    Place(new Vector2(b.center.x - side * (b.extents.x + 0.6f), floor + 0.9f), new Vector2(side * 8f, 4f));
                    yield return new WaitForSeconds(0.4f);
                }
                // 5. Bosses: two real stomps while it stands at the edge.
                for (int i = 0; e != null && e.IsBoss && i < 2; i++)
                {
                    if (e.CurrentHealth <= 2) RestoreHealth(e);
                    Bounds b = e.BodyCollider.bounds;
                    Place(new Vector2(b.center.x - side * 0.2f, b.max.y + 1.2f), new Vector2(side * 2f, -3f));
                    yield return new WaitForSeconds(0.6f);
                }
                Place(new Vector2(Stand(p, start - side * 6f), floor + 1f), Vector2.zero);
                yield return new WaitForSeconds(0.4f);
                sampling = false;
                if (unexpectedPlayerDeath || e == null || e.IsDead || samples.Count < 10)
                {
                    string reason = unexpectedPlayerDeath ? "player death: " + unexpectedDeathReason : e == null || e.IsDead ? "target died or was removed" : "only " + samples.Count + " physics samples";
                    Log("ABORT " + L + " " + who + " " + edge + " edge trial: " + reason + "; samples=" + samples.Count + ", shove-steps=" + shoveSteps);
                    Check(false, who + " " + edge + " edge trial completed", reason + "; samples=" + samples.Count + ", shove-steps=" + shoveSteps);
                    yield break;
                }

                bool stayed = samples.All(s => s.x >= p.min.x && s.x <= p.max.x && s.y > floor - 0.5f);
                float lowest = samples.Min(s => s.y), outmost = side > 0 ? samples.Max(s => s.x) : samples.Min(s => s.x);
                float maxStep = 0f;
                for (int i = 1; i < pivots.Count; i++) maxStep = Mathf.Max(maxStep, Mathf.Abs(pivots[i] - pivots[i - 1]));
                int reversals = 0, moving = 0;
                float lastDx = 0f;
                for (int i = 1; i < shoveSteps; i++)
                {
                    float dx = pivots[i] - pivots[i - 1];
                    if (Mathf.Abs(dx) < 0.003f) continue;
                    moving++;
                    if (lastDx != 0f && Mathf.Sign(dx) != Mathf.Sign(lastDx)) reversals++;
                    lastDx = dx;
                }
                Check(stayed, who + " stays on its platform at the " + edge + " edge (body shove, weak and strong hits, body jumps" + (e.IsBoss ? ", stomps)" : ")"),
                    "outmost " + outmost.ToString("0.00") + " lowest feet " + lowest.ToString("0.00") + " platform " + p.min.x.ToString("0.0") + ".." + p.max.x.ToString("0.0") + " top " + floor.ToString("0.00"));
                Check(maxStep < 0.35f, who + " never snaps or teleports at the " + edge + " edge", "largest step " + maxStep.ToString("0.000"));
                Check(reversals <= Mathf.Max(3, moving / 4), who + " does not jitter against the " + edge + " edge while shoved", reversals + " reversals in " + moving + " moving steps of " + shoveSteps);

                // 6. Not locked up: a step inward is allowed at once, and the AI goes on deciding and moving.
                var brain = e.GetComponent<EnemyBehaviour>();
                bool inward = e.MoveHorizontal(-side, 0.01f);
                e.Halt();
                float x0 = e.BodyCollider.bounds.center.x;
                int acts = events.Count(ev => ev.who == brain);
                bool moved = false;
                for (float t = 0f; t < 4f && e != null && !moved; t += Time.deltaTime)
                {
                    KeepAlive();
                    moved = Mathf.Abs(e.BodyCollider.bounds.center.x - x0) > 0.25f || (brain != null && (brain.IsBusy || events.Count(ev => ev.who == brain) > acts));
                    yield return null;
                }
                Check(inward && moved, who + " moves normally again after the " + edge + " edge trial", "step inward allowed " + inward + ", AI moved or acted " + moved +
                    (moved || e == null ? "" : " | v " + e.Body.linearVelocity.ToString("F2") + " feet " + e.BodyCollider.bounds.min.y.ToString("F2") + " on " + e.StandsOnPlatform +
                     " locked " + e.IsAttackLocked + " knocked " + e.IsKnockedBack + " type " + e.Body.bodyType + " sim " + e.Body.simulated + " constraints " + e.Body.constraints +
                     " enabled " + e.enabled + " brain " + (brain != null && brain.enabled) + " player " + player.transform.position.ToString("F1")));
            }

            private static float Stand(Bounds platform, float x) => Mathf.Clamp(x, platform.min.x + 0.5f, platform.max.x - 0.5f);

            // ---------------------------------------------------------------- prediction

            private struct JumpResult
            {
                public bool Valid, ImpactObserved, Threat, Reacted, ReactedBeforeImpact, Landed;
                public int Frames, ReactionCount;
                public float ReactionCooldown, PunishCooldown;
                public string InvalidReason;
                public string Reaction;
            }

            private static readonly string[] Defences = { "anticipated", "read", "sidestep", "evade", "dodge", "evasive", "step", "punish" };

            /// <summary>Standing room on the enemy's platform from its centre toward <paramref name="side"/>.</summary>
            private static float Room(GoblinEnemy e, int side)
            {
                Bounds p = e.Platform.bounds;
                float cx = e.BodyCollider.bounds.center.x;
                return side > 0 ? p.max.x - 0.5f - cx : cx - (p.min.x + 0.5f);
            }

            /// <summary>Seconds from take-off until a full jump comes down to the enemy's head height.</summary>
            private float FlightTime(GoblinEnemy e)
            {
                float g = -Physics2D.gravity.y, vy = JumpSpeed;
                float fall = g * controller.FallGravityScale;
                float drop = Mathf.Max(0.1f, e.Platform.bounds.max.y + vy * vy / (2f * g) - e.BodyCollider.bounds.max.y);
                return vy / g + Mathf.Sqrt(2f * drop / fall);
            }

            /// <summary>
            /// One jump from <paramref name="startOffset"/> beside the enemy's centre, aimed like a player would:
            /// at take-off it leads the enemy's visible walking speed and runs only as fast as the distance
            /// needs (full speed through the keyboard, slower by driving the body). <paramref name="landShortBy"/>
            /// lands that far short of the centre on purpose; NaN jumps straight up. <paramref name="drop"/> instead
            /// puts the player near the top of a stomp straight over its head (an arc that is surely on target).
            /// </summary>
            private IEnumerator Jump(GoblinEnemy e, float startOffset, List<JumpResult> results, float landShortBy, bool drop = false, bool holdTargetX = false)
            {
                var r = new JumpResult();
                void Abort(string reason) { r.InvalidReason = reason; results.Add(r); }
                if (unexpectedPlayerDeath) { Abort("player death: " + unexpectedDeathReason); yield break; }
                if (e == null || e.IsDead) { Abort("target missing or dead before attempt"); yield break; }
                Bounds p = e.Platform.bounds;
                KeepAlive();
                Release();
                float sx = Mathf.Clamp(e.BodyCollider.bounds.center.x + startOffset, p.min.x + 0.5f, p.max.x - 0.5f);
                var brain = e.GetComponent<EnemyBehaviour>();
                // Let a committed move finish with the player safely away, then place the jump start once.
                safeSpot = trialWaitSpot;
                Place(trialWaitSpot, Vector2.zero);
                for (float w = 0f; w < 2.5f && !controller.IsGrounded && !unexpectedPlayerDeath; w += Time.deltaTime) yield return null;
                if (unexpectedPlayerDeath) { Abort("player death while preparing jump: " + unexpectedDeathReason); yield break; }
                if (!controller.IsGrounded)
                {
                    Abort("player did not reach ground at " + player.transform.position.ToString("F2") + " within 2.5 s");
                    yield break;
                }
                // A move can chain while the target sees the player; waiting at the far side avoids restarting it.
                for (float w = 0f; w < 6f && brain.IsBusy && !unexpectedPlayerDeath; w += Time.deltaTime) yield return null;
                if (unexpectedPlayerDeath) { Abort("player death while waiting for target: " + unexpectedDeathReason); yield break; }
                if (brain.IsBusy) { Abort("target remained busy after 6 s at player x=" + player.transform.position.x.ToString("0.00")); yield break; }
                if (!controller.IsGrounded) { Abort("player lost ground while target finished its move"); yield break; }
                // Keep this reposition and jump in the same frame so the target cannot strike during setup.
                Place(new Vector2(sx, p.max.y + 1f), Vector2.zero);
                safeSpot = new Vector2(sx, p.max.y + 1f);
                int hp = e.CurrentHealth;
                Rigidbody2D targetBody = e.Body;
                RigidbodyConstraints2D targetConstraints = targetBody.constraints;
                if (holdTargetX)
                {
                    targetBody.constraints |= RigidbodyConstraints2D.FreezePositionX;
                    targetBody.linearVelocity = new Vector2(0f, targetBody.linearVelocity.y);
                }
                int dir = 0;
                float speed = 0f;
                if (drop)
                {
                    Bounds b = e.BodyCollider.bounds;
                    Place(new Vector2(b.center.x, b.max.y + 2.8f), new Vector2(0f, 2f));
                }
                else if (!float.IsNaN(landShortBy))
                {
                    float px = player.transform.position.x, cx = e.BodyCollider.bounds.center.x, flight = FlightTime(e);
                    float target = cx + (holdTargetX ? 0f : e.Body.linearVelocity.x) * flight + Mathf.Sign(px - cx) * landShortBy;
                    dir = target >= px ? 1 : -1;
                    speed = Mathf.Min(7f, Mathf.Abs(target - px) / flight);
                }
                float start = Time.time, impact = -1f;
                bool left = false, threat = false;
                bool completed = false;
                float observationWindow = Mathf.Max(2.2f, FlightTime(e) + 0.4f);
                try
                {
                    for (float t = 0f; t < observationWindow && e != null && !e.IsDead && !unexpectedPlayerDeath; t += Time.deltaTime)
                    {
                        r.Frames++;
                        KeepAlive();
                        // Body-driven jumps must not relaunch on the same frame they land.
                        if (!drop) Hold(dir, !left || (keysDrive && speed >= 6.99f), speed);
                        if (!controller.IsGrounded) left = true;
                        if (brain.Forecast.OnTarget) threat = true;
                        if (e.CurrentHealth < hp || (left && controller.IsGrounded && t > 0.1f)) { impact = Time.time; completed = true; break; }
                        yield return null;
                    }
                    Release();
                    if (unexpectedPlayerDeath) { Abort("player died during jump: " + unexpectedDeathReason); yield break; }
                    if (e == null || e.IsDead) { Abort("target died or was removed during jump"); yield break; }
                    if (!left) { Abort("player never left the ground"); yield break; }
                    // A full observation window without impact is valid, but cannot count as a pre-impact response or a landed short stomp.
                    if (!completed) impact = Time.time;
                    yield return new WaitForSeconds(0.15f);
                    if (unexpectedPlayerDeath) { Abort("player died while recording jump: " + unexpectedDeathReason); yield break; }
                }
                finally { if (holdTargetX && targetBody != null) targetBody.constraints = targetConstraints; }
                r.Valid = true;
                r.ImpactObserved = completed;
                r.Threat = threat;
                r.Landed = e.CurrentHealth < hp;
                foreach (var ev in events)
                {
                    if (ev.who != brain || ev.time < start || ev.time > impact + 0.1f) continue;
                    string key = ev.what.Split(' ')[0];
                    if (!Defences.Contains(key)) continue;
                    r.Reacted = true;
                    r.ReactionCount++;
                    r.Reaction = r.Reaction == null ? key : r.Reaction;
                    r.ReactedBeforeImpact |= completed && ev.time < impact;
                }
                r.ReactionCooldown = CooldownRemaining(ReactionCooldownField, brain);
                r.PunishCooldown = CooldownRemaining(PunishCooldownField, brain);
                results.Add(r);
                // Step off and wait, as a player resets between attempts (and the enemy finishes what it started).
                if (e != null)
                {
                    int away = Room(e, 1) > Room(e, -1) ? 1 : -1;
                    Place(new Vector2(Stand(e.Platform.bounds, e.BodyCollider.bounds.center.x + away * 4.5f), e.Platform.bounds.max.y + 1f), Vector2.zero);
                }
                yield return new WaitForSeconds(1.2f);
            }

            private static float CooldownRemaining(FieldInfo field, EnemyBehaviour brain) =>
                field == null || brain == null || !field.DeclaringType.IsInstanceOfType(brain)
                    ? -1f : Mathf.Max(0f, (float)field.GetValue(brain) - Time.time);

            /// <summary>
            /// Direct stomp jumps, vertical hops beside it and jumps that land short on purpose. <paramref name="bossLevel"/>
            /// (1-3, 0 for a regular enemy) picks the boss-specific defence to look for.
            /// </summary>
            private IEnumerator PredictionTrials(GoblinEnemy e, string who, int bossLevel)
            {
                var brain = e.GetComponent<EnemyBehaviour>();
                float run = 7f * FlightTime(e);
                float half = e.BodyCollider.bounds.extents.x;
                int direct = bossLevel > 0 ? 10 : 8;
                var hits = new List<JumpResult>();
                var drops = new List<JumpResult>();
                var hops = new List<JumpResult>();
                var misses = new List<JumpResult>();
                int firstEvent = events.Count;
                int attempts = 0;
                int attemptsRun = 0;
                float learnedChance = 0f;
                int planned = direct + 14;

                List<JumpResult> AllResults() => hits.Concat(drops).Concat(hops).Concat(misses).ToList();
                string Metrics(IEnumerable<JumpResult> values)
                {
                    var rs = values.ToList();
                    float reactionCooldown = rs.Where(r => r.Valid && r.ReactionCooldown >= 0f).Select(r => r.ReactionCooldown).DefaultIfEmpty(0f).Max();
                    var punishCooldowns = rs.Where(r => r.Valid && r.PunishCooldown >= 0f).Select(r => r.PunishCooldown).ToArray();
                    float punishCooldown = punishCooldowns.Length > 0 ? punishCooldowns.Max() : -1f;
                    return "attempts=" + rs.Count + ", valid=" + rs.Count(r => r.Valid) + ", threats=" + rs.Count(r => r.Valid && r.Threat) +
                           ", impact-observed=" + rs.Count(r => r.Valid && r.ImpactObserved) + ", frames=" + rs.Sum(r => r.Frames) +
                           ", reactions=" + rs.Sum(r => r.ReactionCount) + ", before-impact=" + rs.Count(r => r.ReactedBeforeImpact) +
                           ", reaction-cooldown-max=" + reactionCooldown.ToString("0.00") + "s" +
                           (punishCooldown >= 0f ? ", punish-cooldown-max=" + punishCooldown.ToString("0.00") + "s" : "");
                }
                void Abort(string reason)
                {
                    var all = AllResults();
                    Log("ABORT " + L + " " + who + " prediction trial: " + Metrics(all) + ", planned=" + planned + ", reason=" + reason);
                    Check(false, who + " prediction trials completed", Metrics(all) + ", planned=" + planned + ", reason=" + reason);
                }

                // Direct stomp jumps from alternate sides, then drops straight over its head.
                for (int i = 0; i < direct + 6; i++)
                {
                    if (unexpectedPlayerDeath) { Abort("player death: " + unexpectedDeathReason); yield break; }
                    if (e == null || e.IsDead) { Abort("target died or was removed before direct/overhead attempt " + (i + 1)); yield break; }
                    if (e.CurrentHealth <= 1) RestoreHealth(e);
                    if (i < direct)
                    {
                        int from = i % 2 == 0 ? -1 : 1;
                        if (Room(e, from) < run && Room(e, -from) > Room(e, from)) from = -from;
                        attemptsRun++;
                        yield return Jump(e, from * Mathf.Min(run, Room(e, from)), hits, 0f);
                        if (!hits[hits.Count - 1].Valid) { Abort(hits[hits.Count - 1].InvalidReason); yield break; }
                    }
                    else
                    {
                        attemptsRun++;
                        yield return Jump(e, 0f, drops, 0f, drop: true);
                        if (!drops[drops.Count - 1].Valid) { Abort(drops[drops.Count - 1].InvalidReason); yield break; }
                    }
                    attempts = Mathf.Max(attempts, brain.RecentStompAttempts);
                    learnedChance = Mathf.Max(learnedChance, brain.ReadChance(0.3f));
                }
                for (int i = 0; i < 4; i++)
                {
                    if (unexpectedPlayerDeath) { Abort("player death: " + unexpectedDeathReason); yield break; }
                    if (e == null || e.IsDead) { Abort("target died or was removed before hop attempt " + (i + 1)); yield break; }
                    if (e.CurrentHealth <= 1) RestoreHealth(e);
                    attemptsRun++;
                    yield return Jump(e, (i % 2 == 0 ? -1f : 1f) * (half + 2.8f), hops, float.NaN);
                    if (!hops[hops.Count - 1].Valid) { Abort(hops[hops.Count - 1].InvalidReason); yield break; }
                }
                float readMissChance = bossLevel == 3 ? (float)ReadMissChanceField.GetValue(brain) : 0f;
                if (bossLevel == 3) ReadMissChanceField.SetValue(brain, 0f);
                try
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (unexpectedPlayerDeath) { Abort("player death: " + unexpectedDeathReason); yield break; }
                        if (e == null || e.IsDead) { Abort("target died or was removed before short-landing attempt " + (i + 1)); yield break; }
                        if (e.CurrentHealth <= 1) RestoreHealth(e);
                        int from = i % 2 == 0 ? -1 : 1;
                        float shortBy = half + 1.3f; // comes down about 1.3 short of its body
                        float startOffset;
                        if (bossLevel == 3)
                        {
                            brain.OnEncounterReset();
                            ReadMissChanceField.SetValue(brain, 0f);
                            PunishCooldownField.SetValue(brain, 0f);
                            DecisionField.SetValue(brain, Time.time + 10f);
                            float bodyOffset = e.BodyCollider.bounds.center.x - e.transform.position.x;
                            if (Mathf.Abs(bodyOffset) > 0.01f) from = bodyOffset > 0f ? -1 : 1;
                            if (Room(e, from) < 2.2f && Room(e, -from) >= 2.2f) from = -from;
                            startOffset = e.transform.position.x + from * 2.2f - e.BodyCollider.bounds.center.x;
                            shortBy = float.NaN; // jump vertically from a point outside the body but within the counter radius
                        }
                        else startOffset = from * Mathf.Min(run + shortBy, Room(e, from));
                        attemptsRun++;
                        yield return Jump(e, startOffset, misses, shortBy, holdTargetX: bossLevel == 3);
                        if (!misses[misses.Count - 1].Valid) { Abort(misses[misses.Count - 1].InvalidReason); yield break; }
                    }
                }
                finally { if (bossLevel == 3) ReadMissChanceField.SetValue(brain, readMissChance); }
                if (attemptsRun != planned || AllResults().Count(r => r.Valid) != planned)
                {
                    Abort("completed " + attemptsRun + " of " + planned + " attempts");
                    yield break;
                }

                string Summary(List<JumpResult> rs) => rs.Count(r => r.Threat) + " read, " + rs.Count(r => r.ReactedBeforeImpact) + " answered before impact, " +
                                                      rs.Count(r => r.Landed) + " landed of " + rs.Count + " [" + string.Join(" ", rs.Select(r => r.Reaction ?? "-")) + "]";
                Log("INFO " + L + " " + who + " prediction observations: " + Metrics(AllResults()));
                Log("INFO " + L + " " + who + " (" + brain.Tier + ") direct: " + Summary(hits) + "; overhead: " + Summary(drops) + "; hops: " + Summary(hops) +
                    "; short: " + Summary(misses) + "; most attempts remembered " + attempts + ", chance 0.30 learned up to " + learnedChance.ToString("0.00") +
                    "; input " + (keysDrive ? "keyboard" : "body"));

                var stomps = hits.Concat(drops).ToList();
                Check(stomps.Count(r => r.Threat) >= 3, who + " reads incoming stomp arcs", stomps.Count(r => r.Threat) + "/" + stomps.Count);
                Check(stomps.Any(r => r.ReactedBeforeImpact), who + " sometimes answers a stomp before it lands", Summary(stomps));
                Check(stomps.Count(r => r.Reacted) < stomps.Count(r => r.Threat), who + " does not dodge every stomp it reads", Summary(stomps));
                Check(stomps.Any(r => r.Landed), who + " can still be stomped", Summary(stomps));
                // The Warchief's landing counter ("read" a landing beside it, "punish" it after) is by design, not a read of the hop.
                Check(!hops.Any(r => r.ReactedBeforeImpact && !r.Threat && r.Reaction != "read" && r.Reaction != "punish"), who + " never reacts to a hop that cannot land on it", Summary(hops));
                Check(attempts >= 3 && learnedChance > 0.3f, who + " learns from repeated stomps", attempts + " attempts, chance 0.30 -> " + learnedChance.ToString("0.00"));

                var mine = events.Skip(firstEvent).Where(ev => ev.who == brain).Select(ev => ev.what.Split(' ')[0]).ToList();
                if (bossLevel == 1) Check(mine.Contains("sidestep"), "Ogre Warlord sidesteps a predicted stomp", string.Join(",", mine.Distinct()));
                if (bossLevel == 2) Check(mine.Contains("evade"), "Plague Alchemist evades a predicted stomp", string.Join(",", mine.Distinct()));
                if (bossLevel == 3)
                {
                    Check(mine.Contains("dodge"), "Goblin Warchief dodges a predicted stomp", string.Join(",", mine.Distinct()));
                    bool allMissed = misses.Count == 4 && misses.All(r => r.Valid && r.ImpactObserved && !r.Landed);
                    Check(allMissed, "Goblin Warchief short-stomp fixture produces four misses", Summary(misses));
                    Check(allMissed && misses.All(r => r.Reaction == "punish"), "Goblin Warchief counters each established short-stomp miss", Summary(misses));
                }
            }

            /// <summary>Four approaches from the left: the enemy must remember that side (the Ogre Warlord then waits turned that way).</summary>
            private IEnumerator ApproachTrials(GoblinEnemy e, string who)
            {
                var brain = e.GetComponent<EnemyBehaviour>();
                Bounds p = e.Platform.bounds;
                // Room on its left for real approaches: it starts near the arena's right end.
                float pivotOffset = e.BodyCollider.bounds.center.x - e.Body.position.x;
                e.Body.position = new Vector2(p.max.x - 3f - pivotOffset, e.Body.position.y);
                yield return new WaitForSeconds(0.3f);
                for (int i = 0; i < 4 && e != null; i++)
                {
                    float cx = e.BodyCollider.bounds.center.x;
                    Place(new Vector2(Mathf.Max(p.min.x + 0.5f, cx - 7f), p.max.y + 1f), Vector2.zero);
                    yield return new WaitForSeconds(0.3f);
                    for (float t = 0f; t < 2f && Mathf.Abs(e.BodyCollider.bounds.center.x - player.transform.position.x) > 2.5f; t += Time.deltaTime)
                    {
                        KeepAlive();
                        Hold(1, false);
                        yield return null;
                    }
                    Release();
                    yield return new WaitForSeconds(0.3f);
                }
                Check(brain.ExpectedApproachSide == -1, who + " remembers the side the player keeps approaching from", "expected side " + brain.ExpectedApproachSide);
                if (brain is OgreWarlordBehaviour)
                {
                    // The player out of its notice range (it to the arena's right end, the player 11 to its
                    // left), coming from neither side: it waits turned toward the side the player used.
                    e.Body.position = new Vector2(p.max.x - 2f - pivotOffset, e.Body.position.y);
                    Place(new Vector2(Mathf.Max(p.min.x + 0.5f, p.max.x - 13f), p.max.y + 1f), Vector2.zero);
                    yield return new WaitForSeconds(1.5f);
                    Check(e.Direction == -1, "Ogre Warlord waits turned toward the expected approach",
                        "facing " + e.Direction + ", player " + (player.transform.position.x - e.transform.position.x).ToString("0.0") + " away, busy " + brain.IsBusy);
                }
            }
        }
    }
}
