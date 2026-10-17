using System;
using UnityEngine;

namespace MazeRunner.Settings
{
    /// <summary>How movement is shown on touch screens (see the Web template's on-screen controls).</summary>
    public enum TouchControlMode
    {
        Joystick = 0, // A: virtual joystick (bottom-left) + Jump (bottom-right)
        Buttons = 1,  // B: Left and Right buttons (bottom-left) + Jump (bottom-right)
    }

    /// <summary>
    /// Player settings shared by every scene: music volume, sound-effects volume and the
    /// touch control layout. Values are loaded from PlayerPrefs before the first scene,
    /// saved on every change and announced through the Changed events so audio sources and
    /// the Web page update immediately.
    /// </summary>
    public static class GameSettings
    {
        public const int DefaultMusicPercent = 80;
        public const int DefaultSfxPercent = 100;
        public const TouchControlMode DefaultControlMode = TouchControlMode.Joystick;

        private const string MusicKey = "settings.musicVolume";
        private const string SfxKey = "settings.sfxVolume";
        private const string ControlKey = "settings.touchControls";

        private static bool loaded;
        private static int musicPercent = DefaultMusicPercent;
        private static int sfxPercent = DefaultSfxPercent;
        private static TouchControlMode controlMode = DefaultControlMode;

        public static event Action VolumeChanged;
        public static event Action<TouchControlMode> ControlModeChanged;

        public static int MusicPercent { get { Load(); return musicPercent; } }
        public static int SfxPercent { get { Load(); return sfxPercent; } }
        public static TouchControlMode ControlMode { get { Load(); return controlMode; } }

        /// <summary>Multiplier for music sources: 0 mutes, 1 plays at the source's configured volume.</summary>
        public static float MusicVolume => MusicPercent / 100f;

        /// <summary>Multiplier for sound-effect sources: 0 mutes, 1 plays at the source's configured volume.</summary>
        public static float SfxVolume => SfxPercent / 100f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            musicPercent = Mathf.Clamp(PlayerPrefs.GetInt(MusicKey, DefaultMusicPercent), 0, 100);
            sfxPercent = Mathf.Clamp(PlayerPrefs.GetInt(SfxKey, DefaultSfxPercent), 0, 100);
            int mode = PlayerPrefs.GetInt(ControlKey, (int)DefaultControlMode);
            controlMode = Enum.IsDefined(typeof(TouchControlMode), mode) ? (TouchControlMode)mode : DefaultControlMode;
        }

        public static void SetMusicPercent(int percent)
        {
            Load();
            percent = Mathf.Clamp(percent, 0, 100);
            if (percent == musicPercent) return;
            musicPercent = percent;
            PlayerPrefs.SetInt(MusicKey, percent);
            PlayerPrefs.Save();
            VolumeChanged?.Invoke();
        }

        public static void SetSfxPercent(int percent)
        {
            Load();
            percent = Mathf.Clamp(percent, 0, 100);
            if (percent == sfxPercent) return;
            sfxPercent = percent;
            PlayerPrefs.SetInt(SfxKey, percent);
            PlayerPrefs.Save();
            VolumeChanged?.Invoke();
        }

        public static void SetControlMode(TouchControlMode mode)
        {
            Load();
            if (mode == controlMode) return;
            controlMode = mode;
            PlayerPrefs.SetInt(ControlKey, (int)mode);
            PlayerPrefs.Save();
            ControlModeChanged?.Invoke(mode);
        }

        public static void ResetToDefaults()
        {
            SetMusicPercent(DefaultMusicPercent);
            SetSfxPercent(DefaultSfxPercent);
            SetControlMode(DefaultControlMode);
        }
    }
}
