using EchoShift.Game.Screens;
using EchoShift.Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EchoShift.Game
{
    /// <summary>
    /// Starts the whole game from code, so any scene works, including an empty one.
    /// Holds the services every screen uses.
    /// </summary>
    public class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }

        public SaveSystem Save { get; private set; }
        public LevelCatalog Levels { get; private set; }
        public HintService Hints { get; private set; }
        public AdsService Ads { get; private set; }
        public StoreService Store { get; private set; }
        public AnalyticsService Analytics { get; private set; }
        public AudioService Audio { get; private set; }
        public ReviewService Review { get; private set; }
        public Navigator Nav { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("EchoShift");
            DontDestroyOnLoad(go);
            go.AddComponent<GameApp>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            Save = new SaveSystem();
            Save.Load();
            Haptics.Save = Save;

            Analytics = new AnalyticsService();
            Analytics.Start();

            Levels = new LevelCatalog();
            Levels.Load();

            Hints = new HintService(Save);
            Ads = new AdsService(Save, Analytics);
            Store = new StoreService(Save, Analytics);
            Store.Start();
            Audio = gameObject.AddComponent<AudioService>();
            Audio.Init(Save);
            Review = new ReviewService(Save, this);

            EnsureCamera();
            EnsureEventSystem();

            Nav = Navigator.Create(transform);
            Nav.Show<SplashScreen>();
        }

        void Update()
        {
            if (InputHelper.BackPressed()) Nav.Back();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            Save?.Save();
            // Leaving the app pauses the game, so returning never drops the player into live play.
            var game = Nav != null ? Nav.Find<GameScreen>() : null;
            if (game != null && Nav.Top == game) game.OpenPause();
        }

        public void Toast(string message) => Nav.Toast(message);

        public void Quit()
        {
            Save.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                DontDestroyOnLoad(go);
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Background;
            cam.orthographic = true;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
