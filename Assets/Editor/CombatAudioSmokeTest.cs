using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MazeRunner.Audio;
using MazeRunner.Enemies;
using MazeRunner.Levels;
using MazeRunner.Player;
using MazeRunner.Settings;
using MazeRunner.UI;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Plays each level in Play Mode and checks the persistent audio and the player's stomp:
    /// - audio: one AudioManager, its clips assigned, music playing, the Music / Sound Effects settings
    ///   scale only their own sources (0 = silent), and music paused by a menu comes back after a scene
    ///   reload (retry / restart) on the same instance without restarting the track; the pause menu,
    ///   game over + retry and a main-menu round trip keep one AudioManager with its music playing;
    /// - stomp: landing on a regular enemy's head deals exactly 1 damage and bounces the player
    ///   (boss stomps are covered by BossEncounterSmokeTest).
    /// Results: Logs/combat-audio-smoke.txt. Audio is checked as state (clips, isPlaying, volumes):
    /// batch mode has no audio device, so this does not prove anything is audible.
    ///
    /// Menu: Maze Runner > Run Combat + Audio Smoke Test
    /// CLI:  Unity -batchmode -projectPath . -executeMethod MazeRunner.EditorTools.CombatAudioSmokeTest.RunFromCommandLine
    ///       (no -quit: it leaves the editor itself, exit code 1 on any failure)
    /// </summary>
    [InitializeOnLoad]
    public static class CombatAudioSmokeTest
    {
        private const string ActiveKey = "CombatSmoke.Active";
        private const string LevelKey = "CombatSmoke.Level";
        private const string ExitKey = "CombatSmoke.Exit";
        private const string ResultFile = "Logs/combat-audio-smoke.txt";
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Levels/Level1.unity", "Assets/Scenes/Levels/Level2.unity", "Assets/Scenes/Levels/Level3.unity",
        };

        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo MusicField = typeof(AudioManager).GetField("musicSource", Private);
        private static readonly FieldInfo SfxField = typeof(AudioManager).GetField("sfxSource", Private);
        private static readonly FieldInfo EnemyHealthField = typeof(GoblinEnemy).GetField("currentHealth", Private);
        private static readonly FieldInfo BehaviourField = typeof(GoblinEnemy).GetField("behaviour", Private);
        private static readonly FieldInfo InvincibleField = typeof(PlayerHealth).GetField("invincibilityTimer", Private);
        private static readonly MethodInfo GameOverMethod = typeof(PlayerHealth).GetMethod("GameOver", Private);

        static CombatAudioSmokeTest()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Maze Runner/Run Combat + Audio Smoke Test")]
        public static void Run() => Begin(false);

        public static void RunFromCommandLine() => Begin(true);

        private static void Begin(bool exitWhenDone)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultFile, "Combat + audio smoke test " + System.DateTime.Now + "\n");
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(ExitKey, exitWhenDone);
            SessionState.SetInt(LevelKey, 0);
            StartLevel();
        }

        private static void StartLevel()
        {
            int level = SessionState.GetInt(LevelKey, 0);
            if (level >= Scenes.Length) { Finish(); return; }
            EditorSceneManager.OpenScene(Scenes[level], OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void Finish()
        {
            SessionState.SetBool(ActiveKey, false);
            bool failed = File.ReadAllText(ResultFile).Contains("FAIL");
            File.AppendAllText(ResultFile, failed ? "RESULT: FAILED\n" : "RESULT: ALL PASSED\n");
            Debug.Log("[CombatSmoke]\n" + File.ReadAllText(ResultFile));
            if (SessionState.GetBool(ExitKey, false)) EditorApplication.Exit(failed ? 1 : 0);
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                var go = new GameObject("CombatSmokeDriver");
                Object.DontDestroyOnLoad(go); // survives the scene reload in the audio check
                go.AddComponent<Driver>().Level = SessionState.GetInt(LevelKey, 0);
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetInt(LevelKey, SessionState.GetInt(LevelKey, 0) + 1);
                EditorApplication.delayCall += StartLevel;
            }
        }

        private static void Log(string line)
        {
            File.AppendAllText(ResultFile, line + "\n");
            Debug.Log("[CombatSmoke] " + line);
        }

        private class Driver : MonoBehaviour
        {
            public int Level;
            private string L => "L" + (Level + 1);
            private int errorsLogged;
            private string firstError = "";

            private GameObject player;
            private PlayerCombat combat;
            private PlayerHealth health;
            private Rigidbody2D playerBody;
            private Collider2D playerCollider;

            private void Check(bool ok, string what, string detail = "") =>
                Log((ok ? "PASS " : "FAIL ") + L + " " + what + (detail.Length > 0 ? " - " + detail : ""));

            private void OnLog(string message, string stack, LogType type)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                if (errorsLogged++ == 0) firstError = message;
            }

            private IEnumerator Start()
            {
                Application.logMessageReceived += OnLog;
                yield return new WaitForSeconds(1f);
                CheckMoonAtStart();
                yield return Audio();
                yield return new WaitForSeconds(1f);
                FindPlayer();
                if (combat == null) Check(false, "player has PlayerCombat");
                else
                {
                    yield return Stomp();
                    yield return Moon();
                }
                Check(errorsLogged == 0, "no errors or exceptions logged", errorsLogged + " " + firstError);
                Application.logMessageReceived -= OnLog;
                Time.timeScale = 1f;
                EditorApplication.ExitPlaymode();
            }

            // ------------------------------------------------------------------- audio

            private IEnumerator Audio()
            {
                AudioManager[] all = FindObjectsByType<AudioManager>(FindObjectsSortMode.None);
                AudioManager m = AudioManager.Instance;
                Check(m != null && all.Length == 1, "exactly one AudioManager", all.Length + " found");
                if (m == null) yield break;
                var music = (AudioSource)MusicField.GetValue(m);
                var sfx = (AudioSource)SfxField.GetValue(m);
                Check(music != null && music.clip != null && music.loop, "music source has a looping clip", music != null && music.clip != null ? music.clip.name : "none");
                Check(music != null && music.isPlaying, "music is playing");
                foreach (string clip in new[] { "jumpClip", "damageClip", "enemyDefeatClip", "levelCompleteClip", "buttonClickClip", "swordHitClip", "respawnClip" })
                {
                    var c = (AudioClip)typeof(AudioManager).GetField(clip, Private).GetValue(m);
                    Check(c != null, "sound " + clip + " assigned", c != null ? c.name : "missing");
                }

                int musicWas = GameSettings.MusicPercent, sfxWas = GameSettings.SfxPercent;
                GameSettings.SetMusicPercent(100);
                GameSettings.SetSfxPercent(100);
                float musicFull = music.volume, sfxFull = sfx.volume;
                GameSettings.SetMusicPercent(0);
                Check(music.volume == 0f && Mathf.Approximately(sfx.volume, sfxFull) && sfxFull > 0f, "Music 0% mutes music only",
                    "music " + music.volume + " sfx " + sfx.volume);
                GameSettings.SetMusicPercent(100);
                GameSettings.SetSfxPercent(0);
                Check(sfx.volume == 0f && Mathf.Approximately(music.volume, musicFull) && musicFull > 0f, "Sound Effects 0% mutes effects only",
                    "music " + music.volume + " sfx " + sfx.volume);
                GameSettings.SetMusicPercent(musicWas);
                GameSettings.SetSfxPercent(sfxWas);

                // A pause / game-over screen pauses the music; a restart (scene reload) must bring it back.
                yield return new WaitForSeconds(0.5f);
                float before = music.time;
                m.PauseMusic();
                Check(!music.isPlaying, "PauseMusic pauses");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                yield return null;
                yield return new WaitForSeconds(0.5f);
                Check(AudioManager.Instance == m && FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length == 1,
                    "same AudioManager after a scene reload");
                Check(music.isPlaying && music.time >= before, "music resumes after a reload without restarting",
                    "time " + before.ToString("0.00") + " -> " + music.time.ToString("0.00"));

                // The real screens: pause menu, game over + retry, and a round trip through the main menu.
                var pause = FindAnyObjectByType<PauseMenuController>();
                Check(pause != null, "level has a pause menu");
                if (pause != null)
                {
                    pause.Pause();
                    yield return new WaitForSecondsRealtime(0.3f);
                    Check(!music.isPlaying && Time.timeScale == 0f, "pause menu pauses the music");
                    pause.Resume();
                    yield return null;
                    Check(music.isPlaying && Time.timeScale == 1f, "resume brings the music back");
                }

                var playerHealth = FindAnyObjectByType<PlayerHealth>();
                if (playerHealth != null)
                {
                    GameOverMethod.Invoke(playerHealth, null);
                    yield return new WaitForSecondsRealtime(0.3f);
                    Check(!music.isPlaying, "game over pauses the music");
                    playerHealth.RestartLevel();
                    yield return null;
                    yield return new WaitForSeconds(0.5f);
                    Check(music.isPlaying && AudioManager.Instance == m && FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length == 1,
                        "retry after game over plays the music on the one AudioManager");
                }

                string level = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene("MainMenu");
                yield return null;
                yield return new WaitForSeconds(0.5f);
                Check(AudioManager.Instance == m && FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length == 1 && music.isPlaying,
                    "main menu keeps the one AudioManager and its music");
                int buttons = 0, silent = 0;
                foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    buttons++;
                    if (b.GetComponent<UIButtonSound>() == null) silent++;
                }
                Check(buttons > 0 && silent == 0, "every main-menu button clicks", buttons + " buttons, " + silent + " without UIButtonSound");
                SceneManager.LoadScene(level);
                yield return null;
                yield return new WaitForSeconds(0.5f);
                Check(AudioManager.Instance == m && FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length == 1 && music.isPlaying,
                    "back from the menu: still one AudioManager, music playing");
            }

            // ------------------------------------------------------------------- stomp

            private void FindPlayer()
            {
                combat = FindAnyObjectByType<PlayerCombat>();
                if (combat == null) return;
                player = combat.gameObject;
                health = player.GetComponent<PlayerHealth>();
                playerBody = player.GetComponent<Rigidbody2D>();
                playerCollider = player.GetComponent<Collider2D>();
            }

            private static bool CanBlock(GoblinEnemy e)
            {
                var b = BehaviourField.GetValue(e) as EnemyBehaviour;
                return b != null && b.GetType().GetMethod("TryBlock").DeclaringType != typeof(EnemyBehaviour);
            }

            /// <summary>Drops the player onto a regular enemy's head: one stomp deals exactly 1 damage and bounces.</summary>
            private IEnumerator Stomp()
            {
                GoblinEnemy target = null;
                foreach (GoblinEnemy e in FindObjectsByType<GoblinEnemy>(FindObjectsSortMode.None))
                {
                    if (!e.IsBoss && !e.IsDead && e.MaxHealth >= 2 && !CanBlock(e)) { target = e; break; }
                }
                if (target == null) { Log("INFO " + L + " no regular 2+ HP non-blocking enemy to stomp"); yield break; }
                Log("INFO " + L + " stomp target " + target.name + " (" + target.MaxHealth + " HP)");

                InvincibleField.SetValue(health, 1000f);
                EnemyHealthField.SetValue(target, target.MaxHealth);
                Bounds tb = target.GetComponent<Collider2D>().bounds;
                Vector2 pos = new Vector2(tb.center.x, tb.max.y + (player.transform.position.y - playerCollider.bounds.min.y) + 0.3f);
                playerBody.position = pos;
                player.transform.position = pos;
                playerBody.linearVelocity = new Vector2(0f, -5f);
                Physics2D.SyncTransforms();
                int before = target.CurrentHealth;
                bool bounced = false;
                for (float t = 0f; t < 1f && target.CurrentHealth == before; t += Time.deltaTime) yield return null;
                for (float t = 0f; t < 0.2f && !bounced; t += Time.deltaTime) { bounced = playerBody.linearVelocity.y > 1f; yield return null; }
                Check(target.CurrentHealth == before - 1, "a stomp deals exactly 1 damage", before + " -> " + target.CurrentHealth);
                Check(bounced, "the player bounces off a stomped enemy");
                EnemyHealthField.SetValue(target, target.MaxHealth);
            }

            // ------------------------------------------------------------------- moon

            /// <summary>At the spawn, with the camera settled on the player, the moon is where it always was: upper right.</summary>
            private void CheckMoonAtStart()
            {
                var moon = FindAnyObjectByType<SkyMoon>();
                var cam = UnityEngine.Camera.main;
                Vector3 vp = moon != null && cam != null ? cam.WorldToViewportPoint(moon.transform.position) : Vector3.zero;
                Check(Mathf.Abs(vp.y - 0.74f) < 0.05f && vp.x > 0.6f, "the moon starts in the upper-right sky", "viewport " + vp.ToString("F2"));
            }

            /// <summary>The moon keeps its world height through jumps, camera smoothing and a respawn snap; only its X follows the camera.</summary>
            private IEnumerator Moon()
            {
                var moon = FindAnyObjectByType<SkyMoon>();
                var cam = UnityEngine.Camera.main;
                if (moon == null || cam == null) { Check(false, "level has a SkyMoon and a main camera"); yield break; }
                yield return null;
                float y0 = moon.transform.position.y;
                float dx0 = moon.transform.position.x - cam.transform.position.x;
                float camMin = cam.transform.position.y, camMax = camMin, camX0 = cam.transform.position.x;
                float yDrift = 0f, xDrift = 0f;
                // A big jump to the right: the camera moves both ways.
                playerBody.position += new Vector2(6f, 0f);
                player.transform.position = playerBody.position;
                playerBody.linearVelocity = new Vector2(0f, 14f);
                for (float t = 0f; t < 1.5f; t += Time.deltaTime)
                {
                    yield return null;
                    camMin = Mathf.Min(camMin, cam.transform.position.y);
                    camMax = Mathf.Max(camMax, cam.transform.position.y);
                    yDrift = Mathf.Max(yDrift, Mathf.Abs(moon.transform.position.y - y0));
                    xDrift = Mathf.Max(xDrift, Mathf.Abs(moon.transform.position.x - cam.transform.position.x - dx0));
                }
                float camDx = cam.transform.position.x - camX0;
                Check(camMax - camMin > 1f && Mathf.Abs(camDx) > 1f, "moon trial: the camera moved up/down and sideways", "camera y " + camMin.ToString("0.00") + ".." + camMax.ToString("0.00") + ", x moved " + camDx.ToString("0.00"));
                Check(yDrift == 0f, "the moon's world Y stays fixed while the camera moves vertically", "largest Y change " + yDrift.ToString("0.0000"));
                Check(xDrift < 0.001f, "the moon's X follows the camera horizontally", "largest drift from its screen spot " + xDrift.ToString("0.0000"));
                health.Respawn();
                yield return null;
                yield return null;
                Check(moon.transform.position.y == y0, "a respawn camera snap leaves the moon's Y unchanged", y0.ToString("0.000") + " -> " + moon.transform.position.y.ToString("0.000"));
            }
        }
    }
}
