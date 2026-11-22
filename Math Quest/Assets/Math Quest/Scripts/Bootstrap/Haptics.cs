using System;
using UnityEngine;

namespace MathQuest.Bootstrap
{
    /// <summary>
    /// Phone vibration. On Android it talks to the system vibrator directly, so it can follow a strength curve
    /// (a hit that fades out) instead of the fixed one-second buzz that Handheld.Vibrate gives. In the editor and
    /// on other platforms it does nothing.
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
            PlayWaveform(timings, amplitudes);
        }

        /// <summary>A short, single buzz.</summary>
        public static void Tap(float duration = 0.05f, float strength = 0.6f)
        {
            if (!Enabled || duration <= 0f || strength <= 0f)
                return;

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

        // Most phone motors give nothing you can feel below about a third of their range, so a fading hit would
        // vanish half way. Anything that should vibrate is lifted into the range that can be felt (80 to 255).
        private const int FeelableFloor = 80;

        private static int ToAmplitude(float strength)
        {
            strength = Mathf.Clamp01(strength);
            return strength <= 0.02f ? 0 : Mathf.RoundToInt(Mathf.Lerp(FeelableFloor, 255f, strength));
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool initialized;
        private static bool initFailed;
        private static AndroidJavaObject vibrator;
        private static AndroidJavaClass effectClass;
        private static AndroidJavaObject alarmAttributes;
        private static int sdk;
        private static bool hasAmplitudeControl;

        // The game's vibrations are sent as the "alarm" type. On many phones (Tecno, itel and others) the haptic
        // setting is off out of the box and silently drops every other type, so a camera shake would never be felt.
        // The player's own switch is Haptics.Enabled.
        private const int UsageAlarm = 4;

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

                bool hasVibrator = vibrator != null && vibrator.Call<bool>("hasVibrator");

                if (hasVibrator && sdk >= 26)
                {
                    effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    hasAmplitudeControl = vibrator.Call<bool>("hasAmplitudeControl");
                }

                if (hasVibrator)
                    alarmAttributes = MakeAttributes(UsageAlarm);

                if (!hasVibrator)
                    vibrator = null; // a phone or tablet with no motor: nothing to do
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Haptics] Could not reach the vibrator: {e.Message}");
                vibrator = null;
                initFailed = true;
            }

            return vibrator != null;
        }

        private static AndroidJavaObject MakeAttributes(int usage)
        {
            using (var builder = new AndroidJavaObject("android.media.AudioAttributes$Builder"))
            {
                using (builder.Call<AndroidJavaObject>("setUsage", usage)) { }
                return builder.Call<AndroidJavaObject>("build");
            }
        }

        private static long TotalMilliseconds(long[] timings)
        {
            long total = 0;
            foreach (long t in timings)
                total += t;
            return total;
        }

        private static void PlayWaveform(long[] timings, int[] amplitudes)
        {
            if (!Init())
            {
                // Only when reaching the vibrator went wrong (not when the phone simply has none).
                if (initFailed)
                    Handheld.Vibrate();
                return;
            }

            try
            {
                Send(timings, amplitudes, alarmAttributes);
            }
            catch (Exception e)
            {
                // Something about the alarm type failed on this phone: try without it, then the plain system buzz.
                Debug.LogWarning($"[Haptics] Could not vibrate as an alarm, trying the default type: {e.Message}");
                try
                {
                    Send(timings, amplitudes, null);
                }
                catch (Exception second)
                {
                    Debug.LogWarning($"[Haptics] Could not vibrate, using the plain buzz instead: {second.Message}");
                    Handheld.Vibrate();
                }
            }
        }

        private static void Send(long[] timings, int[] amplitudes, AndroidJavaObject attributes)
        {
            long total = TotalMilliseconds(timings);

            if (sdk >= 26 && hasAmplitudeControl)
            {
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                    Vibrate(effect, attributes);
            }
            else if (sdk >= 26)
            {
                // No strength control on this phone: one buzz for the whole length.
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", total, -1))
                    Vibrate(effect, attributes);
            }
            else if (attributes != null)
            {
                vibrator.Call("vibrate", total, attributes);
            }
            else
            {
                vibrator.Call("vibrate", total);
            }
        }

        private static void Vibrate(AndroidJavaObject effect, AndroidJavaObject attributes)
        {
            if (attributes != null)
                vibrator.Call("vibrate", effect, attributes);
            else
                vibrator.Call("vibrate", effect);
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
