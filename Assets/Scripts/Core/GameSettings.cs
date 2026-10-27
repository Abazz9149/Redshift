using UnityEngine;
using UnityEngine.Audio;

namespace Redshift
{
    /// <summary>Applies the SETTINGS screen values. Assign an AudioMixer with exposed params "MusicVol" and "SfxVol".</summary>
    public static class GameSettings
    {
        public static AudioMixer Mixer;

        /// <summary>PlayerController listens, so changing CONTROLS in the pause menu applies instantly.</summary>
        public static event System.Action<int> OnControlModeChanged;

        public static void ApplyAll()
        {
            var d = SaveSystem.Data;
            SetMusic(d.musicVolume, false);
            SetSound(d.soundVolume, false);
            SetGraphics(d.graphicsLevel, false);
        }

        public static void SetMusic(float v, bool save = true)
        {
            SaveSystem.Data.musicVolume = Mathf.Clamp01(v);
            if (Mixer) Mixer.SetFloat("MusicVol", ToDb(v));
            if (save) SaveSystem.Save();
        }

        public static void SetSound(float v, bool save = true)
        {
            SaveSystem.Data.soundVolume = Mathf.Clamp01(v);
            if (Mixer) Mixer.SetFloat("SfxVol", ToDb(v));
            else AudioListener.volume = v; // fallback when no mixer is set up
            if (save) SaveSystem.Save();
        }

        /// <summary>0 Low, 1 Medium, 2 High, 3 Ultra. Make sure Project Settings > Quality has 4 levels in this order.</summary>
        public static void SetGraphics(int level, bool save = true)
        {
            int max = QualitySettings.names.Length - 1;
            level = Mathf.Clamp(level, 0, Mathf.Max(0, max));
            SaveSystem.Data.graphicsLevel = level;
            QualitySettings.SetQualityLevel(level, true);
            if (save) SaveSystem.Save();
        }

        /// <summary>0 = Buttons, 1 = Steering wheel, 2 = Gyro / tilt (same as PlayerController).</summary>
        public static void SetControlMode(int mode)
        {
            mode = Mathf.Clamp(mode, 0, 2);
            if (SaveSystem.Data.controlMode == mode) return;
            SaveSystem.Data.controlMode = mode;
            SaveSystem.Save();
            OnControlModeChanged?.Invoke(mode);
        }

        static float ToDb(float v) => v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;
    }
}
