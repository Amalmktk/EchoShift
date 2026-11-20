using System;
using System.Collections;
using System.Collections.Generic;
using EchoShift.Game.Board;
using EchoShift.Game.UI;
using EchoShift.Simulation;
using UnityEngine;
using UnityEngine.UI;
using F = EchoShift.Game.UI.UIFactory;

namespace EchoShift.Game.Screens
{
    /// <summary>The gameplay screen: top bar, board, move and echo counters, and controls.</summary>
    public class GameScreen : Page
    {
        public LevelDef Level;

        GridSimulation _sim;
        BoardView _board;
        RectTransform _safe;
        RectTransform _controls;
        RectTransform _boardArea;
        Text _moveText;
        Text _echoText;
        Button _undo;
        Button _echo;
        Text _coachText;
        Image _coachPanel;

        readonly Queue<Move> _queue = new Queue<Move>();
        bool _ended;
        int _moves;
        int _undos;
        int _restarts;
        int _hintsUsed;
        float _startTime;
        bool _sawPlateActive;

        public GridSimulation Sim => _sim;

        float SizeScale
        {
            get
            {
                switch (App.Save.Data.settings.buttonSize)
                {
                    case 0: return 0.85f;
                    case 2: return 1.15f;
                    default: return 1f;
                }
            }
        }

        public override void Build()
        {
            _sim = new GridSimulation(Level);
            _startTime = Time.realtimeSinceStartup;
            _safe = F.SafeContent(Root);

            BuildTopBar();

            _boardArea = F.Rect(_safe, "BoardArea");
            var touch = _boardArea.gameObject.AddComponent<Image>();
            touch.color = new Color(0, 0, 0, 0);
            var swipe = _boardArea.gameObject.AddComponent<SwipeArea>();
            swipe.Swiped += OnInputMove;

            var info = F.Rect(_safe, "Info");
            _moveText = F.Text(info, "", Theme.Body, Theme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold, "Moves");
            F.Stretch(_moveText.rectTransform, Theme.S2, 0, 0, 0);
            _echoText = F.Text(info, "", Theme.Body, Theme.Echo, TextAnchor.MiddleRight, FontStyle.Bold, "Echoes");
            F.Stretch(_echoText.rectTransform, 0, 0, Theme.S2, 0);

            _controls = F.Rect(_safe, "Controls");
            BuildControls();
            LayoutZones(info);

            _board = _boardArea.gameObject.AddComponent<BoardView>();
            Canvas.ForceUpdateCanvases();
            _board.Build(_boardArea, _sim);

            BuildCoach();
            UpdateHud();

            int attempt = PlayerPrefs.GetInt("attempts." + Level.Id, 0) + 1;
            PlayerPrefs.SetInt("attempts." + Level.Id, attempt);
            App.Analytics.Log("level_start", "level_id", Level.Id, "attempt", attempt);
            if (Level.Number <= 3) App.Analytics.Log("tutorial_step", "step", Level.Number);
        }

        // ------------------------------------------------------------------ layout

        void BuildTopBar()
        {
            var bar = F.Rect(_safe, "TopBar");
            F.Top(bar, 180f);

            var pause = F.IconButton(bar, "II", OpenPause, "Pause");
            PlaceIcon(pause, 0, true);

            var restart = F.IconButton(bar, "↺", Restart, "Restart");
            PlaceIcon(restart, 0, false);
            var hint = F.IconButton(bar, "?", () => RequestHint("hud"), "Hint");
            PlaceIcon(hint, 1, false);

            var title = F.Text(bar, Level.Number + ". " + Level.Name, Theme.Body, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            F.Stretch(title.rectTransform, 220, 0, 380, 0);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 28;
            title.resizeTextMaxSize = Theme.Body;
        }

        static void PlaceIcon(Button b, int slot, bool left)
        {
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(left ? 0 : 1, 0.5f);
            rt.pivot = new Vector2(left ? 0 : 1, 0.5f);
            rt.sizeDelta = new Vector2(140, 140);
            float x = Theme.S2 + slot * 164f;
            rt.anchoredPosition = new Vector2(left ? x : -x, 0);
        }

        float ControlsHeight => (App.Save.Data.settings.dpad ? 520f : 300f) * SizeScale;

        void LayoutZones(RectTransform info)
        {
            F.Bottom(_controls, ControlsHeight, Theme.S1);
            F.Bottom(info, 100f, ControlsHeight + Theme.S1);
            F.Stretch(_boardArea, Theme.S1, 190f, Theme.S1, ControlsHeight + Theme.S1 + 110f);
        }

        void BuildControls()
        {
            foreach (Transform child in _controls) Destroy(child.gameObject);
            var settings = App.Save.Data.settings;
            float h = Theme.ActionButtonHeight * SizeScale;

            // Action buttons: Undo, Wait, Echo. Echo sits under the thumb's natural side.
            var row = F.HStack(_controls, Theme.S1, new RectOffset(48, 48, 0, 0), TextAnchor.LowerCenter, "Actions");
            F.Bottom(row, h);
            _undo = F.Button(row, Strings.T("game.undo"), ButtonStyle.Secondary, Undo, "Undo");
            var wait = F.Button(row, Strings.T("game.wait"), ButtonStyle.Secondary, () => OnInputMove(Move.Wait), "Wait");
            _echo = F.Button(row, Strings.T("game.echo"), ButtonStyle.Echo, MakeEcho, "Echo");
            F.Layout(_echo, -1, -1, 1.4f);
            F.Layout(_undo, -1, -1, 1f);
            F.Layout(wait, -1, -1, 1f);
            if (settings.leftHanded) _echo.transform.SetSiblingIndex(0);

            if (settings.dpad)
            {
                float size = 200f * SizeScale;
                var pad = F.Rect(_controls, "DPad");
                pad.anchorMin = pad.anchorMax = new Vector2(0.5f, 1f);
                pad.pivot = new Vector2(0.5f, 1f);
                pad.sizeDelta = new Vector2(size * 3, size * 1.6f);
                pad.anchoredPosition = Vector2.zero;
                PadButton(pad, "↑", Move.Up, new Vector2(0, size * 0.55f), size);
                PadButton(pad, "←", Move.Left, new Vector2(-size * 1.05f, 0), size);
                PadButton(pad, "→", Move.Right, new Vector2(size * 1.05f, 0), size);
                PadButton(pad, "↓", Move.Down, new Vector2(0, -size * 0.55f), size);
            }
        }

        void PadButton(RectTransform pad, string label, Move move, Vector2 pos, float size)
        {
            var b = F.Button(pad, label, ButtonStyle.Secondary, () => OnInputMove(move), "Pad " + move);
            F.Center((RectTransform)b.transform, new Vector2(size, size * 0.5f), pos);
            F.Label(b).fontSize = Theme.Heading;
        }

        public override void OnRevealed()
        {
            // Settings may have changed controls while an overlay was open.
            BuildControls();
            LayoutZones((RectTransform)_safe.Find("Info"));
            UpdateHud();
        }

        // ------------------------------------------------------------------ input

        void Update()
        {
            if (App.Nav.Top != this || _sim == null) return;

            if (InputHelper.TryKeyboardMove(out var m)) OnInputMove(m);
            else if (InputHelper.UndoKey()) Undo();
            else if (InputHelper.EchoKey()) MakeEcho();
            else if (InputHelper.RestartKey()) Restart();

            if (!_ended && _queue.Count > 0 && !_board.IsAnimating) DoMove(_queue.Dequeue());
        }

        void OnInputMove(Move m)
        {
            if (_ended || App.Nav.Top != this) return;
            if (_board.IsAnimating)
            {
                if (_queue.Count < 2) _queue.Enqueue(m);
                return;
            }
            DoMove(m);
        }

        void DoMove(Move m)
        {
            var result = _sim.Step(m);
            if (result == StepResult.LimitReached)
            {
                App.Audio.Play(Sfx.Locked);
                Haptics.Light();
                App.Toast(Strings.T("game.limit_toast"));
                StartCoroutine(Pulse(_moveText.rectTransform));
                _queue.Clear();
                return;
            }
            if (result != StepResult.Ok) return;

            _moves++;
            _board.SetFacing(m);
            PlayTurnFeedback();
            _board.Refresh(true);
            UpdateHud();

            if (_sim.State.Status == GameStatus.Won)
            {
                _ended = true;
                _queue.Clear();
                StartCoroutine(AfterAnimation(OnWin));
            }
            else if (_sim.State.Status == GameStatus.Failed)
            {
                _ended = true;
                _queue.Clear();
                StartCoroutine(AfterAnimation(OnFail));
            }
        }

        void PlayTurnFeedback()
        {
            bool echoStepped = false;
            foreach (var e in _sim.Events)
            {
                switch (e.Kind)
                {
                    case SimEventKind.Moved:
                        if (e.Index == GridSimulation.PlayerIndex) { App.Audio.Play(Sfx.Step); Haptics.Light(); }
                        else echoStepped = true;
                        break;
                    case SimEventKind.Bumped:
                        if (e.Index == GridSimulation.PlayerIndex) { App.Audio.Play(Sfx.Bump); Haptics.Light(); }
                        break;
                    case SimEventKind.Toggled:
                        App.Audio.Play(Sfx.Toggle);
                        break;
                    case SimEventKind.DeviceChanged:
                        App.Audio.Play(e.Active ? Sfx.DoorOpen : Sfx.DoorClose, 0.8f);
                        break;
                    case SimEventKind.ExitChanged:
                        if (e.Active) App.Audio.Play(Sfx.DoorOpen);
                        break;
                    case SimEventKind.CrystalTaken:
                        App.Audio.Play(Sfx.Crystal);
                        Haptics.Light();
                        break;
                }
            }
            if (echoStepped) App.Audio.Play(Sfx.EchoStep);
        }

        IEnumerator AfterAnimation(Action then)
        {
            yield return null;
            while (_board.IsAnimating) yield return null;
            yield return new WaitForSeconds(0.15f);
            then();
        }

        // ------------------------------------------------------------------ commands

        public void Undo()
        {
            if (App.Nav.Top != this && !(App.Nav.Top is RetryPopup)) return;
            if (!_sim.Undo()) return;
            _ended = false;
            _undos++;
            _queue.Clear();
            App.Audio.Play(Sfx.Undo);
            _board.Refresh(false);
            UpdateHud();
        }

        public void MakeEcho()
        {
            if (_ended || App.Nav.Top != this) return;
            if (Level.EchoLimit == 0) return;
            int runLength = _sim.CurrentRun.Count;
            if (!_sim.MakeEcho())
            {
                App.Audio.Play(Sfx.Locked);
                App.Toast(Strings.T(_sim.EchoCount >= Level.EchoLimit ? "game.echo_full_toast" : "game.echo_empty_toast"));
                return;
            }
            _queue.Clear();
            App.Audio.Play(Sfx.EchoMade);
            Haptics.Medium();
            App.Analytics.Log("echo_created", "level_id", Level.Id, "echo_index", _sim.EchoCount, "moves", runLength);
            _board.SyncEchoCount();
            _board.Refresh(false);
            UpdateHud();
        }

        public void Restart()
        {
            if (App.Nav.Top is PausePopup || App.Nav.Top is RetryPopup) App.Nav.CloseOverlays();
            if (App.Nav.Top != this) return;
            App.Analytics.Log("level_restart", "level_id", Level.Id, "echoes_used", _sim.EchoCount, "turn", _sim.State.Turn);
            _sim.Restart();
            _restarts++;
            _ended = false;
            _queue.Clear();
            App.Audio.Play(Sfx.Restart);
            _board.SyncEchoCount();
            _board.Refresh(false);
            UpdateHud();
        }

        public void OpenPause()
        {
            if (App.Nav.Top != this || _ended) return;
            App.Nav.Show<PausePopup>(p => p.Game = this);
        }

        public override void OnBack() => OpenPause();

        // ------------------------------------------------------------------ hints

        /// <summary>Opens the hint overlay, unlocking the first step for free.</summary>
        public void RequestHint(string source)
        {
            if (App.Hints.UnlockedSteps(Level.Id) == 0)
            {
                App.Hints.UnlockNext(Level.Id);
                LogHint(source);
            }
            App.Nav.Show<HintPopup>(p => { p.Game = this; p.Source = source; });
        }

        /// <summary>Unlocks the next step, paying with a hint or a rewarded ad.</summary>
        public void UnlockNextHint(string source, Action onUnlocked)
        {
            if (App.Hints.NextStep(Level.Id) > 3) return;
            if (App.Hints.CanUnlockNext(Level.Id))
            {
                App.Hints.UnlockNext(Level.Id);
                LogHint(source);
                onUnlocked();
                return;
            }
            App.Nav.Show<RewardOfferPopup>(p => p.OnReward = () =>
            {
                App.Hints.Grant(1);
                App.Hints.UnlockNext(Level.Id);
                LogHint(source);
                onUnlocked();
            });
        }

        void LogHint(string source)
        {
            _hintsUsed++;
            App.Analytics.Log("hint_used", "level_id", Level.Id, "step", App.Hints.UnlockedSteps(Level.Id), "source", source);
        }

        // ------------------------------------------------------------------ end of level

        void OnWin()
        {
            int stars = _sim.Stars();
            int moves = _sim.CurrentRun.Count;
            foreach (var run in _sim.Echoes) moves += run.Count;

            App.Audio.Play(Sfx.Win);
            Haptics.Success();
            App.Save.RecordWin(Level.Id, stars, moves, _sim.State.CrystalTaken);
            if (Level.Number >= 3 && !App.Save.Data.tutorialDone)
            {
                App.Save.Data.tutorialDone = true;
                App.Save.Save();
                App.Analytics.Log("tutorial_step", "step", "done");
            }
            App.Ads.NoteLevelCompleted();
            App.Analytics.Log("level_complete", "level_id", Level.Id, "total_moves", _moves, "stars", stars,
                "echoes_used", _sim.EchoCount, "restarts", _restarts, "undos", _undos, "hints_used", _hintsUsed,
                "time_s", Mathf.RoundToInt(Time.realtimeSinceStartup - _startTime));
            App.Review.MaybeAsk();

            App.Nav.Show<LevelCompletePopup>(p =>
            {
                p.Level = Level;
                p.Stars = stars;
                p.Moves = moves;
                p.EchoesUsed = _sim.EchoCount;
                p.Crystal = _sim.State.CrystalTaken;
            });
        }

        void OnFail()
        {
            App.Audio.Play(Sfx.Fail);
            Haptics.Medium();
            App.Analytics.Log("level_fail", "level_id", Level.Id, "cause", _sim.State.FailCause, "turn", _sim.State.Turn);
            App.Nav.Show<RetryPopup>(p => p.Game = this);
        }

        /// <summary>Where Next goes after a level: the next level, a world unlock, or the level list.</summary>
        public static void GoToLevelAfter(LevelDef level)
        {
            var app = GameApp.Instance;
            var next = app.Levels.Next(level);
            if (next == null)
            {
                app.Nav.Show<LevelSelectScreen>(s => s.World = level.World);
                app.Toast(Strings.T("complete.end_toast"));
                return;
            }
            if (next.World != level.World)
            {
                app.Nav.Show<LevelSelectScreen>(s => s.World = next.World);
                app.Nav.Show<WorldUnlockedPopup>(p => p.World = next.World);
                return;
            }
            app.Nav.Show<GameScreen>(g => g.Level = next);
        }

        // ------------------------------------------------------------------ HUD and coach marks

        void UpdateHud()
        {
            _moveText.text = Strings.T("game.move", _sim.CurrentRun.Count, Level.MovesPerRun);
            _moveText.color = _sim.MovesLeft <= 3 ? Theme.Player : Theme.TextMuted;
            _echoText.text = Level.EchoLimit > 0 ? Strings.T("game.echoes", _sim.EchoCount, Level.EchoLimit) : "";
            if (_undo != null) _undo.interactable = _sim.CanUndo;
            if (_echo != null)
            {
                _echo.gameObject.SetActive(Level.EchoLimit > 0);
                F.Label(_echo).text = Strings.T("game.echo") + "  " + (Level.EchoLimit - _sim.EchoCount);
            }
            UpdateCoach();
        }

        void BuildCoach()
        {
            if (Level.Number > 3) return;
            _coachPanel = F.Panel(_safe, Theme.WithAlpha(Theme.Surface, 0.92f), 999f, "Coach");
            _coachPanel.raycastTarget = false;
            F.Top(_coachPanel.rectTransform, 120f, 200f, 120f);
            _coachText = F.Text(_coachPanel.transform, "", Theme.Body, Theme.Echo, TextAnchor.MiddleCenter, FontStyle.Bold);
            F.Stretch(_coachText.rectTransform, 32, 0, 32, 0);
        }

        void UpdateCoach()
        {
            if (_coachText == null) return;
            string key = null;
            switch (Level.Number)
            {
                case 1:
                    key = _moves == 0 ? "coach.l1.a" : "coach.l1.b";
                    break;
                case 2:
                    for (int i = 0; i < Level.Devices.Count; i++)
                        if (Level.Devices[i].Kind == DeviceKind.Plate && _sim.State.DeviceActive[i]) _sawPlateActive = true;
                    key = _sawPlateActive ? "coach.l2.b" : "coach.l2.a";
                    break;
                case 3:
                    key = _sim.EchoCount == 0 ? "coach.l3.a" : "coach.l3.b";
                    if (_sim.EchoCount == 0 && _sim.CanEcho && OnPlate()) StartCoroutine(Pulse((RectTransform)_echo.transform));
                    break;
            }
            _coachText.text = key != null ? Strings.T(key) : "";
            _coachPanel.gameObject.SetActive(key != null && !_ended);
        }

        bool OnPlate()
        {
            for (int i = 0; i < Level.Devices.Count; i++)
                if (Level.Devices[i].Kind == DeviceKind.Plate && Level.Devices[i].Pos == _sim.State.Player) return true;
            return false;
        }

        IEnumerator Pulse(RectTransform rt)
        {
            if (rt == null) yield break;
            for (float t = 0; t < 0.4f && rt != null; t += Time.unscaledDeltaTime)
            {
                rt.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t / 0.4f * Mathf.PI));
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }
    }
}
