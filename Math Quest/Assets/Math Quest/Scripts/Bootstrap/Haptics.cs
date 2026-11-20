using System;
using UnityEngine;

namespace MathQuest.Bootstrap
{
    /// <summary>
    /// Phone vibration. On Android it talks to the system vibrator directly, so it can follow a strength curve
    /// (a hit that fades out) instead of the fixed one-second buzz that Handheld.Vibrate gives. In the editor and
    /// on other platforms it does nothing, except raise <see cref="Requested"/>, so it can be tested.
    ///
    ///   Haptics.PlayCurve(0.3f, curve, 0.8f);   // vibrate for 0.3s, following the curve, at 80% strength
    ///   Haptics.Cancel();                        // stop at once
    ///   Haptics.Enabled = false;                 // the player's on/off switch (remembered between sessions)
    /// </summary>
    public static class Haptics
    {
        private const string PrefKey = "MathQuest.Haptics";

        // How long each step of a vibration lasts. A curve is played as a staircase of steps this long.
        private const float StepSeconds = 0.04f;

        /// <summary>Raised whenever a vibration is asked for (duration in seconds, strength 0 to 1). Used by tests.</summary>
        public static event Action<float, float> Requested;

        /// <summary>The player's on/off switch. On unless they turned it off.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (!value)
                    Cancel();
            }
        }

        /// <summary>Vibrates for the given time, following the curve (left is the start, right the end, height is strength).</summary>
        public static void PlayCurve(float duration, AnimationCurve curve, float strength)
        {
            if (!Enabled || duration <= 0f || strength <= 0f)
                return;

            BuildWaveform(duration, curve, strength, out long[] timings, out int[] amplitudes);
            Requested?.Invoke(duration, strength);
            PlayWaveform(timings, amplitudes);
        }

        /// <summary>A short, single buzz.</summary>
        public static void Tap(float duration = 0.05f, float strength = 0.6f)
        {
            if (!Enabled || duration <= 0f || strength <= 0f)
                return;

            Requested?.Invoke(duration, strength);
            PlayWaveform(new[] { (long)Mathf.RoundToInt(duration * 1000f) }, new[] { ToAmplitude(strength) });
        }

        /// <summary>Turns a duration, a curve and a strength into steps: how long each lasts and how strong it is (0 to 255).</summary>
        public static void BuildWaveform(float duration, AnimationCurve curve, float strength, out long[] timings, out int[] amplitudes)
        {
            int steps = Mathf.Clamp(Mathf.CeilToInt(duration / StepSeconds), 1, 100);
            timings = new long[steps];
            amplitudes = new int[steps];

            long stepMilliseconds = Math.Max(1, Mathf.RoundToInt(duration * 1000f / steps));
            for (int i = 0; i < steps; i++)
            {
                float t = (i + 0.5f) / steps;
                float height = curve != null ? curve.Evaluate(t) : 1f;
                timings[i] = stepMilliseconds;
                amplitudes[i] = ToAmplitude(strength * height);
            }
        }

        private static int ToAmplitude(float strength) => Mathf.Clamp(Mathf.RoundToInt(strength * 255f), 0, 255);

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool initialized;
        private static AndroidJavaObject vibrator;
        private static AndroidJavaClass effectClass;
        private static int sdk;
        private static bool hasAmplitudeControl;

        private static bool Init()
        {
            if (initialized)
                return vibrator != null;

            initialized = true;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    sdk = version.GetStatic<int>("SDK_INT");

                    if (sdk >= 31)
                    {
                        using (var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager"))
                            vibrator = manager.Call<AndroidJavaObject>("getDefaultVibrator");
                    }
                    else
                    {
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }

                if (vibrator != null && sdk >= 26)
                {
                    effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    hasAmplitudeControl = vibrator.Call<bool>("hasAmplitudeControl");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Vibration is not available: {e.Message}");
                vibrator = null;
            }

            return vibrator != null;
        }

        private static void PlayWaveform(long[] timings, int[] amplitudes)
        {
            if (!Init())
                return;

            try
            {
                long total = 0;
                foreach (long t in timings)
                    total += t;

                if (sdk >= 26 && hasAmplitudeControl)
                {
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                        vibrator.Call("vibrate", effect);
                }
                else if (sdk >= 26)
                {
                    // No strength control on this phone: one buzz for the whole length.
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", total, -1))
                        vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", total);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not vibrate: {e.Message}");
            }
        }

        /// <summary>Stops any vibration that is still running.</summary>
        public static void Cancel()
        {
            if (!Init())
                return;

            try { vibrator.Call("cancel"); }
            catch (Exception e) { Debug.LogWarning($"Could not stop the vibration: {e.Message}"); }
        }
#else
        private static void PlayWaveform(long[] timings, int[] amplitudes) { }

        /// <summary>Stops any vibration that is still running.</summary>
        public static void Cancel() { }
#endif

        // Never called. Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the Android build, which
        // the calls above need. Without the permission the phone refuses to vibrate.
#pragma warning disable IDE0051 // unused on purpose
        private static void KeepVibratePermission() => Handheld.Vibrate();
#pragma warning restore IDE0051
    }
}
