using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if ECHO_FIREBASE
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
#endif

namespace EchoShift.Game
{
    /// <summary>
    /// Analytics events from chapter 13 of the project docs. Logs to the console until
    /// Firebase is added, then sends to Firebase Analytics as well.
    /// </summary>
    public class AnalyticsService
    {
#if ECHO_FIREBASE
        bool _firebaseReady;
#endif

        public void Start()
        {
#if ECHO_FIREBASE
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                _firebaseReady = task.Result == DependencyStatus.Available;
                if (!_firebaseReady) Debug.LogWarning("Echo Shift: Firebase unavailable: " + task.Result);
#if ECHO_CRASHLYTICS
                if (_firebaseReady) Firebase.Crashlytics.Crashlytics.ReportUncaughtExceptionsAsFatal = true;
#endif
            });
#endif
        }

        /// <summary>Logs an event with name/value pairs, for example Log("level_start", "level_id", "w1_l03", "attempt", 2).</summary>
        public void Log(string eventName, params object[] pairs)
        {
            var values = new Dictionary<string, object>();
            for (int i = 0; i + 1 < pairs.Length; i += 2) values[pairs[i].ToString()] = pairs[i + 1];

            if (Application.isEditor || Debug.isDebugBuild)
            {
                var sb = new StringBuilder("[analytics] ").Append(eventName);
                foreach (var kv in values) sb.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
                Debug.Log(sb.ToString());
            }

#if ECHO_FIREBASE
            if (!_firebaseReady) return;
            var parameters = new List<Parameter>();
            foreach (var kv in values)
            {
                switch (kv.Value)
                {
                    case int n: parameters.Add(new Parameter(kv.Key, n)); break;
                    case long l: parameters.Add(new Parameter(kv.Key, l)); break;
                    case float f: parameters.Add(new Parameter(kv.Key, f)); break;
                    case double d: parameters.Add(new Parameter(kv.Key, d)); break;
                    case bool b: parameters.Add(new Parameter(kv.Key, b ? 1L : 0L)); break;
                    default: parameters.Add(new Parameter(kv.Key, kv.Value?.ToString() ?? "")); break;
                }
            }
            FirebaseAnalytics.LogEvent(eventName, parameters.ToArray());
#endif
        }
    }
}
