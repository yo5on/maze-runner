#if MAZE_AI_TEST
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using MazeRunner.Customization;
using MazeRunner.Enemies;

namespace MazeRunner.Testing
{
    /// <summary>
    /// Test-only probe, compiled only into builds made with -webBuildDefines MAZE_AI_TEST.
    /// The page drives it with SendMessage("AITestProbe", method, arg) and reads results from the console.
    /// </summary>
    [DefaultExecutionOrder(10000)] // LateUpdate after the enemies have posed their rigs, so the arm log shows the final pose
    public class AITestProbe : MonoBehaviour
    {
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("AITestProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<AITestProbe>();
        }

        private static GoblinEnemy Find(string name)
        {
            foreach (var g in FindObjectsByType<GoblinEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (g.name == name) return g;
            return null;
        }

        private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Flags).GetValue(o);

        public void Report(string _)
        {
            var sb = new StringBuilder("[PROBE] goblins\n");
            foreach (var g in FindObjectsByType<GoblinEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var rb = g.GetComponent<Rigidbody2D>();
                var anim = g.GetComponent<Animator>();
                var platform = Field<Collider2D>(g, "platform");
                string state = "?";
                if (anim != null && anim.isActiveAndEnabled)
                {
                    var info = anim.GetCurrentAnimatorStateInfo(0);
                    foreach (var s in new[] { "Idle", "Walking", "Running", "Slashing", "Hurt", "Dying", "Idle Blinking" })
                        if (info.IsName(s)) state = s;
                }
                float swordA = -1f;
                foreach (var sr in g.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.name == "Sword") swordA = sr.color.a;
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "{0} active={1} x={2:F2} y={3:F2} vx={4:F2} dir={5} min={6:F2} max={7:F2} whole={8} plat={9} platX=[{10:F2},{11:F2}] boss={12} alerted={13} state={14} sword={15:F2} scaleX={16:F2} dead={17}\n",
                    g.name, g.gameObject.activeInHierarchy, g.transform.position.x, g.transform.position.y,
                    rb != null ? rb.linearVelocity.x : 0f, Field<int>(g, "direction"),
                    Field<float>(g, "patrolMinX"), Field<float>(g, "patrolMaxX"), g.PatrolsWholePlatform,
                    platform != null ? platform.GetHashCode().ToString() : "none",
                    platform != null ? platform.bounds.min.x : 0f, platform != null ? platform.bounds.max.x : 0f,
                    Field<bool>(g, "isBoss"), g.IsBossAlerted, state, swordA, g.transform.localScale.x, Field<bool>(g, "isDead"));
            }
            Debug.Log(sb.ToString());
        }

        public void Kill(string names)
        {
            // "a|b" kills several goblins in the same frame (simultaneous deaths).
            foreach (string n in names.Split('|'))
            {
                var g = Find(n);
                if (g != null) g.TakeDamage(999);
                Debug.Log("[PROBE] kill " + n + " found=" + (g != null));
            }
        }

        public void Damage(string names)
        {
            foreach (string n in names.Split('|'))
            {
                var g = Find(n);
                if (g != null) g.TakeDamage(1);
                Debug.Log("[PROBE] damage " + n + " found=" + (g != null));
            }
        }

        public void Select(string id)
        {
            int index = PlayableCharacters.IndexOf(id);
            if (index < 0) { Debug.Log("[PROBE] unknown character " + id); return; }
            PlayableCharacters.SaveSelection(PlayableCharacters.All[index]);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Debug.Log("[PROBE] selected " + id);
        }

        public void Disable(string name)
        {
            var g = Find(name);
            if (g != null) g.gameObject.SetActive(false);
            Debug.Log("[PROBE] disable " + name + " found=" + (g != null));
        }

        public void DestroyGoblin(string name)
        {
            var g = Find(name);
            if (g != null) Destroy(g.gameObject);
            Debug.Log("[PROBE] destroy " + name + " found=" + (g != null));
        }

        public void TeleportPlayer(string xy)
        {
            var p = FindAnyObjectByType<MazeRunner.Player.PlayerController>();
            string[] parts = xy.Split(',');
            if (p == null || parts.Length != 2) return;
            var pos = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture), p.transform.position.z);
            p.transform.position = pos;
            var rb = p.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.position = pos; rb.linearVelocity = Vector2.zero; }
            Debug.Log("[PROBE] teleport " + xy);
        }

        private static string RigInfo(MazeRunner.Player.PlayerController p)
        {
            if (p == null) return "none";
            Transform rig = p.transform.Find(PlayerRigSettings.RigName);
            if (rig == null) return "missing";
            var marker = rig.GetComponent<PlayerRigMarker>();
            var anim = rig.GetComponent<Animator>();
            return (marker != null ? marker.RigId : "Fallen_Angels(scene)") + "/" + (anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "no-controller");
        }

        private static string RigState(MazeRunner.Player.PlayerController p)
        {
            Transform rig = p != null ? p.transform.Find(PlayerRigSettings.RigName) : null;
            var anim = rig != null ? rig.GetComponent<Animator>() : null;
            if (anim == null) return "?";
            var info = anim.GetCurrentAnimatorStateInfo(0);
            foreach (var s in new[] { "Idle", "Running", "Jump Start", "Jump Loop", "Falling Down", "Hurt", "Dying" })
                if (info.IsName(s)) return s;
            return "other";
        }

        public void PlayerInfo(string _)
        {
            var p = FindAnyObjectByType<MazeRunner.Player.PlayerController>();
            var skin = p != null ? p.GetComponent<PlayerCharacterSkin>() : null;
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "[PROBE] player scene={3} rig={4} anim={5} pos={0} skin={1} saved={2}\n",
                p != null ? (Vector2)p.transform.position : Vector2.zero,
                skin != null && skin.Current != null ? skin.Current.Id : "none", PlayerPrefs.GetString(PlayableCharacters.SaveKey, "(unset)"),
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, RigInfo(p), RigState(p));
            if (p != null)
                foreach (var sr in p.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.sprite != null)
                        sb.Append(sr.name).Append('=').Append(sr.sprite.name).Append('#').Append(sr.sprite.texture.GetHashCode()).Append(sr.enabled && sr.gameObject.activeInHierarchy ? "" : "(hidden)").Append("; ");
            Debug.Log(sb.ToString());
        }

        /// <summary>Settings and every AudioSource's effective volume (channel, configured base, playing).</summary>
        public void AudioReport(string _)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "[PROBE] audio music={0}% sfx={1}% controls={2} prefs={3}/{4}/{5}\n",
                MazeRunner.Settings.GameSettings.MusicPercent, MazeRunner.Settings.GameSettings.SfxPercent,
                MazeRunner.Settings.GameSettings.ControlMode,
                PlayerPrefs.GetInt("settings.musicVolume", -1), PlayerPrefs.GetInt("settings.sfxVolume", -1), PlayerPrefs.GetInt("settings.touchControls", -1));
            foreach (var src in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var tag = src.GetComponent<MazeRunner.Audio.VolumeChannelSource>();
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} channel={1} base={2:F2} volume={3:F3} playing={4} clip={5}\n",
                    src.name, tag != null ? tag.Channel.ToString() : "NONE", tag != null ? tag.BaseVolume : -1f, src.volume,
                    src.isPlaying, src.clip != null ? src.clip.name : (src.resource != null ? src.resource.name : "-"));
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>Scene, player health, enemies left, unlocks, and where every enemy / moving platform is.</summary>
        public void LevelInfo(string _)
        {
            var sb = new StringBuilder();
            var health = FindAnyObjectByType<MazeRunner.Player.PlayerHealth>();
            var progression = MazeRunner.Levels.LevelProgressionManager.Instance;
            var tracker = MazeRunner.Levels.EnemyTracker.Instance;
            sb.AppendFormat(CultureInfo.InvariantCulture, "[PROBE] level scene={0} hp={1}/{2} remaining={3} index={4} unlocked={5}{6}{7} timeScale={8}\n",
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                health != null ? health.CurrentHealth : -1, health != null ? health.MaxHealth : -1,
                tracker != null ? tracker.RemainingEnemyCount : -1, progression != null ? progression.CurrentLevelIndex : -2,
                progression != null && progression.IsLevelUnlocked(0) ? 1 : 0, progression != null && progression.IsLevelUnlocked(1) ? 1 : 0,
                progression != null && progression.IsLevelUnlocked(2) ? 1 : 0, Time.timeScale);
            foreach (var g in FindObjectsByType<GoblinEnemy>(FindObjectsInactive.Exclude))
            {
                var platform = Field<Collider2D>(g, "platform");
                sb.AppendFormat(CultureInfo.InvariantCulture, "E {0} x={1:F2} y={2:F2} plat={3} top={4:F2} dead={5}\n", g.name, g.transform.position.x, g.transform.position.y,
                    platform != null ? platform.name : "none", platform != null ? platform.bounds.max.y : 0f, Field<bool>(g, "isDead"));
            }
            foreach (var m in FindObjectsByType<MazeRunner.Levels.MovingPlatform>(FindObjectsInactive.Exclude))
                sb.AppendFormat(CultureInfo.InvariantCulture, "M {0} x={1:F2} y={2:F2}\n", m.name, m.transform.position.x, m.transform.position.y);
            Debug.Log(sb.ToString());
        }

        public void LoadScene(string scene)
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        public void ResetProgress(string _)
        {
            if (MazeRunner.Levels.LevelProgressionManager.Instance != null) MazeRunner.Levels.LevelProgressionManager.Instance.ResetProgress();
            Debug.Log("[PROBE] progress reset");
        }

        /// <summary>Rendered size of every enemy (visible body sprites, slash effects excluded) and its collider.</summary>
        public void EnemySizes(string _)
        {
            var sb = new StringBuilder("[PROBE] sizes\n");
            foreach (var g in FindObjectsByType<GoblinEnemy>(FindObjectsInactive.Exclude))
            {
                Bounds? bounds = null;
                foreach (var sr in g.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (sr.sprite == null || !sr.enabled || sr.color.a <= 0.01f || sr.name.Contains("FX")) continue;
                    if (bounds == null) bounds = sr.bounds; else { var b = bounds.Value; b.Encapsulate(sr.bounds); bounds = b; }
                }
                var col = g.GetComponent<Collider2D>();
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} scale={1:F2} spriteH={2:F2} spriteW={3:F2} colH={4:F2} colW={5:F2}\n", g.name, Mathf.Abs(g.transform.localScale.y),
                    bounds?.size.y ?? 0f, bounds?.size.x ?? 0f, col != null ? col.bounds.size.y : 0f, col != null ? col.bounds.size.x : 0f);
            }
            Debug.Log(sb.ToString());
        }

        private void OnEnable() { EnemyBehaviour.Traced += OnTraced; }
        private void OnDisable() { EnemyBehaviour.Traced -= OnTraced; }

        private void OnTraced(EnemyBehaviour b, string what)
        {
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[TRACE] {0:F2} {1} {2}", Time.time, b != null ? b.name : "?", what));
            // Start recording an alchemist's arms (see LateUpdate) when it winds up a throw or a stab.
            if (b is AlchemistBehaviour && what.Contains("windup")) { throwWatch = b; throwFrames = 0; }
        }

        /// <summary>Each enemy's personality, whether a special is running, animator state and speed.</summary>
        public void Brains(string _)
        {
            var sb = new StringBuilder("[PROBE] brains\n");
            foreach (var g in FindObjectsByType<GoblinEnemy>(FindObjectsInactive.Exclude))
            {
                var b = g.GetComponent<EnemyBehaviour>();
                var anim = g.GetComponent<Animator>();
                string state = "?";
                float t = 0f;
                if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null)
                {
                    var info = anim.GetCurrentAnimatorStateInfo(0);
                    t = info.normalizedTime;
                    foreach (var s in new[] { "Idle", "Idle Blinking", "Walking", "Running", "Slashing", "Run Slashing", "Kicking", "Throwing", "Sliding", "Hurt", "Dying" })
                        if (info.IsName(s)) state = s;
                }
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} brain={1} busy={2} x={3:F2} y={4:F2} vx={5:F2} hp={6}/{7} anim={8}@{9:F2} dead={10}\n",
                    g.name, b != null ? b.GetType().Name : "none", b != null && b.IsBusy, g.transform.position.x, g.transform.position.y,
                    g.GetComponent<Rigidbody2D>().linearVelocity.x, g.CurrentHealth, g.MaxHealth, state, t, g.IsDead);
            }
            Debug.Log(sb.ToString());
        }

        public void HealPlayer(string _)
        {
            var h = FindAnyObjectByType<MazeRunner.Player.PlayerHealth>();
            if (h != null) h.Heal(99);
        }

        /// <summary>Hurts the player by the given amount (default 1), the same way an enemy hit does.</summary>
        public void HurtPlayer(string amount)
        {
            var h = FindAnyObjectByType<MazeRunner.Player.PlayerHealth>();
            if (h != null) h.TakeDamage(int.TryParse(amount, out int n) && n > 0 ? n : 1);
            HudInfo("");
        }

        /// <summary>Asks an enemy's behaviour to block a hit, as a stomp would ("Skeleton_Warrior_2_01").</summary>
        public void TryBlock(string name)
        {
            var g = Find(name);
            var b = g != null ? g.GetComponent<EnemyBehaviour>() : null;
            Debug.Log("[PROBE] tryblock " + name + " blocked=" + (b != null && b.TryBlock(1)));
        }

        private EnemyBehaviour throwWatch;
        private int throwFrames;


        /// <summary>
        /// While an alchemist winds up a throw or stab: every frame, where the potion in hand and the knife are
        /// relative to its feet ("fwd" = towards the way it faces, "up" = height), and where a thrown potion starts.
        /// </summary>
        private void LateUpdate()
        {
            if (throwWatch == null) return;
            if (++throwFrames > 120) { throwWatch = null; return; }
            Transform root = throwWatch.transform;
            int facing = root.lossyScale.x < 0f ? -1 : 1;
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "[PROBE] arm t={0:F3} facing={1} alchemistX={2:F2}", Time.time, facing > 0 ? "right" : "left", root.position.x);
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.name != "HeldPotion" && sr.name != "Sword") continue;
                Vector3 d = sr.transform.position - root.position;
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0}(fwd={1:F2},up={2:F2})", sr.name == "Sword" ? "knife" : "bottle", d.x * facing, d.y);
            }
            // The throwing (right) arm as the behaviour drives it: requested offset, the bone's angle, and where the hand is.
            object throwArm = throwWatch.GetType().GetField("throwArm", Flags)?.GetValue(throwWatch);
            if (throwArm != null)
            {
                var bone = throwArm.GetType().GetField("bone", Flags)?.GetValue(throwArm) as Transform;
                var angle = throwArm.GetType().GetField("Angle")?.GetValue(throwArm);
                sb.AppendFormat(CultureInfo.InvariantCulture, " throwArm[{0} offset={1} z={2:F0}]", bone != null ? bone.name : "null", angle, bone != null ? bone.localEulerAngles.z : -1f);
            }
            Transform hand = null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>()) if (t.name == "Right Hand") hand = t;
            if (hand != null)
            {
                Vector3 h = hand.position - root.position;
                sb.AppendFormat(CultureInfo.InvariantCulture, " rightHand(fwd={0:F2},up={1:F2})", h.x * facing, h.y);
            }
            // How far the knife's grip (its point nearest the left fist) is from that fist, in world units.
            Transform leftHand = null, knife = null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>()) { if (t.name == "Left Hand") leftHand = t; else if (t.name == "Sword") knife = t; }
            if (leftHand != null && knife != null)
            {
                var la = leftHand.GetComponent<SpriteRenderer>(); var ka = knife.GetComponent<SpriteRenderer>();
                Vector3 fist = la != null ? la.bounds.center : leftHand.position;
                Vector3 nearest = ka != null ? ka.bounds.ClosestPoint(fist) : knife.position;
                sb.AppendFormat(CultureInfo.InvariantCulture, " knifeGap={0:F3}", Vector2.Distance(nearest, fist));
            }
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr.name != "Potion") continue;
                Vector3 d = sr.transform.position - root.position;
                sb.AppendFormat(CultureInfo.InvariantCulture, " flying(fwd={0:F2},up={1:F2},worldX={2:F2},size={3:F2}x{4:F2})", d.x * facing, d.y, sr.transform.position.x, sr.bounds.size.x, sr.bounds.size.y);
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>Zooms the game camera for close-up screenshots (orthographic size, e.g. "2.5"; the levels use 7).</summary>
        public void CameraZoom(string size)
        {
            var cam = UnityEngine.Camera.main;
            if (cam != null) cam.orthographicSize = float.Parse(size, CultureInfo.InvariantCulture);
            Debug.Log("[PROBE] camera size " + (cam != null ? cam.orthographicSize : -1f));
        }

        /// <summary>Slow motion for watching an attack frame by frame, e.g. "0.2"; "1" is normal speed.</summary>
        public void TimeScale(string scale)
        {
            Time.timeScale = float.Parse(scale, CultureInfo.InvariantCulture);
            Debug.Log("[PROBE] timeScale " + Time.timeScale);
        }

        /// <summary>Kills the player outright, like falling off the level.</summary>
        public void KillPlayer(string _)
        {
            var h = FindAnyObjectByType<MazeRunner.Player.PlayerHealth>();
            if (h != null) h.Die();
            HudInfo("");
        }

        /// <summary>Health and spare lives as PlayerHealth counts them, and the sprites the HUD hearts show.</summary>
        public void HudInfo(string _)
        {
            var h = FindAnyObjectByType<MazeRunner.Player.PlayerHealth>();
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "[PROBE] hud scene={0} hp={1}/{2} spare={3} dying={4} timeScale={5} hearts=",
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                h != null ? h.CurrentHealth : -1, h != null ? h.MaxHealth : -1, h != null ? h.SpareLives : -1,
                h != null && h.IsDying, Time.timeScale);
            var display = FindAnyObjectByType<MazeRunner.UI.HealthDisplay>();
            if (display != null)
            {
                var container = Field<GameObject>(display, "heartContainer");
                foreach (Transform child in container.transform)
                {
                    var img = child.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) sb.Append(img.sprite != null ? img.sprite.name : "null").Append(',');
                }
                sb.Append(" displays=").Append(FindObjectsByType<MazeRunner.UI.HealthDisplay>(FindObjectsSortMode.None).Length);
            }
            else sb.Append("no HealthDisplay");
            sb.Append(" pickups=");
            foreach (var c in FindObjectsByType<MazeRunner.Collectibles.Collectible>(FindObjectsSortMode.None))
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}({1:F2},{2:F2}) ", c.name, c.transform.position.x, c.transform.position.y);
            Debug.Log(sb.ToString());
        }

        /// <summary>Held shields (grip parent, raised, world size) and the alchemists' knife/potion state.</summary>
        public void WeaponInfo(string _)
        {
            var sb = new StringBuilder("[PROBE] weapons\n");
            foreach (var s in FindObjectsByType<HeldShield>(FindObjectsSortMode.None))
            {
                var sr = s.Visual != null ? s.Visual.GetComponent<SpriteRenderer>() : null;
                sb.AppendFormat(CultureInfo.InvariantCulture, "shield {0} on {1}/{2} raised={3} pos=({4:F2},{5:F2}) size=({6:F2}x{7:F2}) sprite={8} flipX={9}\n",
                    s.transform.root.name, s.transform.parent.parent != null ? s.transform.parent.parent.name : "-", s.transform.parent.name, s.Raised,
                    s.transform.position.x, s.transform.position.y, sr != null ? sr.bounds.size.x : 0f, sr != null ? sr.bounds.size.y : 0f,
                    sr != null && sr.sprite != null ? sr.sprite.name : "-", s.transform.lossyScale.x < 0f);
            }
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr.name == "HeldPotion" || sr.name == "Potion")
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} parent={1} pos=({2:F2},{3:F2}) size=({4:F2}x{5:F2}) rot={6:F0}\n", sr.name,
                        sr.transform.parent != null ? sr.transform.parent.name : "-", sr.transform.position.x, sr.transform.position.y, sr.bounds.size.x, sr.bounds.size.y, sr.transform.eulerAngles.z);
                else if (sr.name == "Sword" && sr.transform.root != null && sr.GetComponentInParent<AlchemistBehaviour>() != null)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "alchemist knife on {0}/{1} pos=({2:F2},{3:F2}) rot={4:F0} visible={5}\n",
                        sr.transform.parent.parent != null ? sr.transform.parent.parent.name : "-", sr.transform.parent.name,
                        sr.transform.position.x, sr.transform.position.y, sr.transform.eulerAngles.z, sr.enabled);
                else if (sr.name == "Shield" && !sr.enabled) sb.Append("built-in shield hidden on ").Append(sr.transform.root.name).Append('\n');
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>Player size on screen (sprite height / camera view height), rig scale, animator state, dying flag.</summary>
        public void PlayerState(string _)
        {
            var p = FindAnyObjectByType<MazeRunner.Player.PlayerController>();
            if (p == null) { Debug.Log("[PROBE] pstate none"); return; }
            Bounds? bounds = null;
            foreach (var sr in p.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.sprite == null || !sr.enabled || sr.color.a <= 0.01f || sr.name.Contains("FX")) continue;
                if (bounds == null) bounds = sr.bounds; else { var b = bounds.Value; b.Encapsulate(sr.bounds); bounds = b; }
            }
            var cam = UnityEngine.Camera.main;
            Transform rig = p.transform.Find(PlayerRigSettings.RigName);
            var anim = rig != null ? rig.GetComponent<Animator>() : null;
            string state = "?";
            float t = 0f;
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                var info = anim.GetCurrentAnimatorStateInfo(0);
                t = info.normalizedTime;
                foreach (var s in new[] { "Idle", "Running", "Jump Start", "Jump Loop", "Falling Down", "Hurt", "Dying" })
                    if (info.IsName(s)) state = s;
            }
            var h = p.GetComponent<MazeRunner.Player.PlayerHealth>();
            var rb = p.GetComponent<Rigidbody2D>();
            float height = bounds?.size.y ?? 0f;
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[PROBE] pstate scene={0} pos=({1:F2},{2:F2}) rootScale={3:F2} rigScale={4:F2} spriteH={5:F2} camSize={6:F2} screenFrac={7:F3} camDX={8:F2} anim={9}@{10:F2} dying={11} hp={12} ctrl={13} sim={14}",
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, p.transform.position.x, p.transform.position.y,
                p.transform.lossyScale.y, rig != null ? Mathf.Abs(rig.localScale.y) : 0f, height,
                cam != null ? cam.orthographicSize : 0f, cam != null ? height / (2f * cam.orthographicSize) : 0f,
                cam != null ? cam.transform.position.x - p.transform.position.x : 0f, state, t,
                h != null && h.IsDying, h != null ? h.CurrentHealth : -1, p.enabled, rb != null && rb.simulated));
        }
    }
}
#endif
