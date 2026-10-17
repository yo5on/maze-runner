using UnityEngine;
using UnityEngine.SceneManagement;
using MazeRunner.Settings;

namespace MazeRunner.Audio
{
    public enum VolumeChannel
    {
        Music,
        Sfx,
    }

    /// <summary>
    /// Scales one AudioSource by the player's Music or Sound Effects setting.
    ///
    /// The volume configured on the source (in the scene or prefab) is kept as its maximum:
    /// the source plays at configuredVolume * setting, so 100% sounds exactly as authored and
    /// 0% is silent. Clips and authored volumes are never changed.
    ///
    /// The project has no AudioMixer, so this is the central volume path. AudioManager tags
    /// its two sources explicitly; every other AudioSource in a newly loaded scene is tagged
    /// automatically (looping sources as music, the rest as sound effects), and runtime
    /// spawners call <see cref="AttachAll"/> on what they instantiate.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class VolumeChannelSource : MonoBehaviour
    {
        [SerializeField] private VolumeChannel channel = VolumeChannel.Sfx;

        private AudioSource source;
        private float baseVolume = 1f;
        private bool initialized;

        public VolumeChannel Channel
        {
            get => channel;
            set { channel = value; Apply(); }
        }

        /// <summary>The source's configured (100%) volume.</summary>
        public float BaseVolume
        {
            get { Init(); return baseVolume; }
            set { Init(); baseVolume = Mathf.Clamp01(value); Apply(); }
        }

        public static VolumeChannelSource Attach(AudioSource source, VolumeChannel channel)
        {
            if (source == null) return null;
            var tag = source.GetComponent<VolumeChannelSource>();
            if (tag == null) tag = source.gameObject.AddComponent<VolumeChannelSource>();
            tag.source = source;
            tag.channel = channel;
            tag.Init();
            tag.Apply();
            return tag;
        }

        /// <summary>Tags every untagged AudioSource under root (e.g. a freshly spawned enemy).</summary>
        public static void AttachAll(GameObject root)
        {
            if (root == null) return;
            foreach (AudioSource s in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (s.GetComponent<VolumeChannelSource>() == null)
                {
                    Attach(s, s.loop ? VolumeChannel.Music : VolumeChannel.Sfx);
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void HookSceneLoads()
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                foreach (GameObject root in scene.GetRootGameObjects()) AttachAll(root);
            };
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (GameObject root in SceneManager.GetSceneAt(i).GetRootGameObjects()) AttachAll(root);
            }
        }

        private void Init()
        {
            if (initialized) return;
            initialized = true;
            if (source == null) source = GetComponent<AudioSource>();
            baseVolume = source.volume;
        }

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            GameSettings.VolumeChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.VolumeChanged -= Apply;
        }

        private void Apply()
        {
            Init();
            if (source == null) return;
            float setting = channel == VolumeChannel.Music ? GameSettings.MusicVolume : GameSettings.SfxVolume;
            source.volume = baseVolume * setting;
        }
    }
}
