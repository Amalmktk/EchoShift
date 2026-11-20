using System;
using System.Collections;
using UnityEngine;
#if ECHO_PLAY_REVIEW
using Google.Play.Review;
#endif

namespace EchoShift.Game
{
    /// <summary>Asks for a Play Store review after the 5th completed level, at most once a month.</summary>
    public class ReviewService
    {
        readonly SaveSystem _save;
        readonly MonoBehaviour _host;

        public ReviewService(SaveSystem save, MonoBehaviour host)
        {
            _save = save;
            _host = host;
        }

        public void MaybeAsk()
        {
            if (_save.Data.levelsCompleted < GameConfig.AskReviewAfterLevels) return;
            if (DateTime.TryParse(_save.Data.lastReviewAsk, out var last) &&
                (DateTime.UtcNow - last).TotalDays < GameConfig.DaysBetweenReviewAsks) return;

            _save.Data.lastReviewAsk = SaveSystem.Today();
            _save.Save();
            _host.StartCoroutine(Ask());
        }

        IEnumerator Ask()
        {
#if ECHO_PLAY_REVIEW && UNITY_ANDROID && !UNITY_EDITOR
            var manager = new ReviewManager();
            var request = manager.RequestReviewFlow();
            yield return request;
            if (request.Error != ReviewErrorCode.NoError) yield break;
            var launch = manager.LaunchReviewFlow(request.GetResult());
            yield return launch;
#else
            Debug.Log("Echo Shift: in-app review would be requested here");
            yield break;
#endif
        }
    }
}
