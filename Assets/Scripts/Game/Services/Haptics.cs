using UnityEngine;

namespace EchoShift.Game
{
    /// <summary>Short vibrations on Android 8+, falling back to the default vibration elsewhere.</summary>
    public static class Haptics
    {
        public static SaveSystem Save;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static int _sdk = -1;
#endif

        public static void Light() => Vibrate(12, 60);
        public static void Medium() => Vibrate(30, 140);
        public static void Success() => Vibrate(60, 200);

        static void Vibrate(long ms, int amplitude)
        {
            if (Save == null || !Save.Data.settings.vibration) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (_vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        _sdk = version.GetStatic<int>("SDK_INT");
                }
                if (_vibrator == null) return;
                if (_sdk >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", ms);
                }
            }
            catch (System.Exception)
            {
                Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID
        // Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the Android manifest.
        static void EnsureVibratePermission() => Handheld.Vibrate();
#endif
    }
}
