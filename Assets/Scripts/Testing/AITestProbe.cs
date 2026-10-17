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
    }
}
#endif
