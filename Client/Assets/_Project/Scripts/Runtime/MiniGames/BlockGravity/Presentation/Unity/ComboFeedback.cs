using System;
using UnityEngine;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class ComboFeedback
    {
        private readonly Func<bool> enabled;
        private readonly Action pulse;
        public ComboFeedback(Func<bool> enabled, Action pulse)
        { this.enabled = enabled ?? throw new ArgumentNullException(nameof(enabled)); this.pulse = pulse ?? throw new ArgumentNullException(nameof(pulse)); }
        public void OnClear(int combo) { if (combo >= 2 && enabled()) pulse(); }
        public static void PulseDevice()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            using (var callbackPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                            using (var callbackActivity = callbackPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                            using (var window = callbackActivity.Call<AndroidJavaObject>("getWindow"))
                            using (var view = window.Call<AndroidJavaObject>("getDecorView"))
                            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                                view.Call<bool>("performHapticFeedback", version.GetStatic<int>("SDK_INT") >= 30 ? 16 : 1);
                        }
                        catch (AndroidJavaException) { /* Unsupported device: gameplay continues. */ }
                    }));
                }
            }
            catch (AndroidJavaException) { /* No Android activity available. */ }
#endif
        }
    }
}
