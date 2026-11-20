using System.Collections;
using System.Collections.Generic;
using EchoShift.Game.UI;
using EchoShift.Simulation;
using UnityEngine;
using UnityEngine.UI;
using F = EchoShift.Game.UI.UIFactory;

namespace EchoShift.Game.Board
{
    /// <summary>
    /// Draws a <see cref="GridSimulation"/> with UI images and animates each turn.
    /// It only reads the simulation; it never changes it.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        GridSimulation _sim;
        LevelDef L => _sim.Level;
        RectTransform _area;
        RectTransform _board;
        float _cell;
        Vector2 _lastAreaSize;

        Image[] _ground;
        Image[] _decor;
        Text[] _decorText;
        Image[] _devBase;
        Image[] _devMark;
        Text[] _devText;
        Image _exitRing;
        Image _exitCore;
        Image _crystal;
        Image[] _platforms;
        Image _player;
        RectTransform _playerEye;
        readonly List<Image> _echoes = new List<Image>();
        readonly List<Text> _echoNumbers = new List<Text>();

        Vec2i _facing = new Vec2i(1, 0);
        Coroutine _anim;
        public bool IsAnimating { get; private set; }

        public float MoveDuration => GameApp.Instance.Save.Data.settings.reduceMotion ? 0.06f : 0.12f;

        public void Build(RectTransform area, GridSimulation sim)
        {
            _area = area;
            _sim = sim;
            _board = F.Rect(area, "Board");
            _board.anchorMin = _board.anchorMax = new Vector2(0.5f, 0.5f);
            _board.pivot = new Vector2(0.5f, 0.5f);

            int n = L.Width * L.Height;
            var groundLayer = F.Rect(_board, "Ground");
            var deviceLayer = F.Rect(_board, "Devices");
            var entityLayer = F.Rect(_board, "Entities");
            foreach (var layer in new[] { groundLayer, deviceLayer, entityLayer }) F.Stretch(layer);

            _ground = new Image[n];
            _decor = new Image[n];
            _decorText = new Text[n];
            for (int y = 0; y < L.Height; y++)
            {
                for (int x = 0; x < L.Width; x++)
                {
                    var p = new Vec2i(x, y);
                    int i = L.Index(p);
                    var tile = L.Tiles[i];
                    _ground[i] = F.Image(groundLayer, GroundColor(tile, x, y), null, 24f, "Tile " + x + "," + y);

                    if (tile == Tile.Spikes || tile == Tile.SpikesOdd || tile == Tile.SpikesEven)
                        _decor[i] = F.Image(groundLayer, Theme.Danger, SpriteFactory.Triangle, 0f, "Spikes");
                    else if (tile == Tile.Crumble)
                        _decor[i] = F.Image(groundLayer, Theme.WithAlpha(Theme.TextMuted, 0.35f), SpriteFactory.Diamond, 0f, "Cracks");
                    else if (LevelDef.IsWind(tile))
                        _decorText[i] = F.Text(groundLayer, WindArrow(tile), Theme.Heading, Theme.WithAlpha(Theme.WorldAccent[1], 0.8f), TextAnchor.MiddleCenter, FontStyle.Bold, "Wind");
                }
            }

            int d = L.Devices.Count;
            _devBase = new Image[d];
            _devMark = new Image[d];
            _devText = new Text[d];
            for (int i = 0; i < d; i++)
            {
                var dev = L.Devices[i];
                var c = Theme.Channel[dev.Channel];
                switch (dev.Kind)
                {
                    case DeviceKind.Plate:
                        _devBase[i] = F.Image(deviceLayer, c, SpriteFactory.Ring, 0f, "Plate");
                        _devMark[i] = F.Image(deviceLayer, c, SpriteFactory.Circle, 0f, "PlateFill");
                        break;
                    case DeviceKind.Door:
                        _devBase[i] = F.Image(deviceLayer, c, null, 20f, "Door");
                        _devMark[i] = F.Image(deviceLayer, Theme.WithAlpha(Theme.Background, 0.5f), null, 8f, "DoorBar");
                        break;
                    case DeviceKind.Switch:
                    case DeviceKind.TimedSwitch:
                        _devBase[i] = F.Image(deviceLayer, Theme.Surface, null, 20f, "Switch");
                        _devMark[i] = F.Image(deviceLayer, c, SpriteFactory.Circle, 0f, "Knob");
                        if (dev.Kind == DeviceKind.TimedSwitch)
                            _devText[i] = F.Text(deviceLayer, "", Theme.Caption, Theme.Background, TextAnchor.MiddleCenter, FontStyle.Bold, "Timer");
                        break;
                    case DeviceKind.Bridge:
                        _devBase[i] = F.Image(deviceLayer, c, null, 16f, "Bridge");
                        _devMark[i] = F.Image(deviceLayer, Theme.WithAlpha(Theme.Background, 0.35f), null, 4f, "Plank");
                        break;
                }
            }

            _exitRing = F.Image(deviceLayer, Theme.Echo, SpriteFactory.Ring, 0f, "Exit");
            _exitCore = F.Image(deviceLayer, Theme.Echo, SpriteFactory.Circle, 0f, "ExitCore");
            if (L.HasCrystal) _crystal = F.Image(deviceLayer, Theme.Star, SpriteFactory.Diamond, 0f, "Crystal");

            _platforms = new Image[L.Platforms.Count];
            for (int k = 0; k < _platforms.Length; k++)
                _platforms[k] = F.Image(deviceLayer, Theme.Hex("B8C4E0"), null, 22f, "Platform");

            _entityLayer = entityLayer;
            _player = F.Image(entityLayer, Theme.Player, SpriteFactory.Circle, 0f, "Player");
            var eye = F.Image(_player.transform, Theme.Background, SpriteFactory.Circle, 0f, "Eye");
            _playerEye = eye.rectTransform;

            SyncEchoCount();
            Layout();
            Refresh(false);
        }

        RectTransform _entityLayer;

        void Update()
        {
            if (_area == null) return;
            if (_area.rect.size != _lastAreaSize)
            {
                Layout();
                Refresh(false);
            }
            if (_exitCore != null && _sim.State.ExitOpen)
            {
                float pulse = 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
                _exitCore.color = Theme.WithAlpha(Theme.Echo, pulse);
            }
        }

        // ------------------------------------------------------------------ layout

        void Layout()
        {
            _lastAreaSize = _area.rect.size;
            if (_lastAreaSize.x <= 0 || _lastAreaSize.y <= 0) return;
            _cell = Mathf.Floor(Mathf.Min(_lastAreaSize.x / L.Width, _lastAreaSize.y / L.Height));
            _cell = Mathf.Min(_cell, 170f);
            _board.sizeDelta = new Vector2(_cell * L.Width, _cell * L.Height);

            for (int y = 0; y < L.Height; y++)
            {
                for (int x = 0; x < L.Width; x++)
                {
                    var p = new Vec2i(x, y);
                    int i = L.Index(p);
                    var tile = L.Tiles[i];
                    float inset = tile == Tile.Wall ? 0.02f : 0.05f;
                    Place(_ground[i].rectTransform, p, 1f - inset * 2f);
                    if (_decor[i] != null) Place(_decor[i].rectTransform, p, tile == Tile.Crumble ? 0.3f : 0.5f);
                    if (_decorText[i] != null)
                    {
                        Place(_decorText[i].rectTransform, p, 1f);
                        _decorText[i].fontSize = Mathf.RoundToInt(_cell * 0.5f);
                    }
                }
            }

            for (int i = 0; i < L.Devices.Count; i++)
            {
                var dev = L.Devices[i];
                switch (dev.Kind)
                {
                    case DeviceKind.Plate:
                        Place(_devBase[i].rectTransform, dev.Pos, 0.66f);
                        Place(_devMark[i].rectTransform, dev.Pos, 0.4f);
                        break;
                    case DeviceKind.Door:
                        Place(_devBase[i].rectTransform, dev.Pos, 0.9f);
                        Place(_devMark[i].rectTransform, dev.Pos, new Vector2(0.6f, 0.12f));
                        break;
                    case DeviceKind.Switch:
                    case DeviceKind.TimedSwitch:
                        Place(_devBase[i].rectTransform, dev.Pos, 0.8f);
                        Place(_devMark[i].rectTransform, dev.Pos, 0.46f);
                        if (_devText[i] != null)
                        {
                            Place(_devText[i].rectTransform, dev.Pos, 0.6f);
                            _devText[i].fontSize = Mathf.RoundToInt(_cell * 0.3f);
                        }
                        break;
                    case DeviceKind.Bridge:
                        Place(_devBase[i].rectTransform, dev.Pos, 0.92f);
                        Place(_devMark[i].rectTransform, dev.Pos, new Vector2(0.8f, 0.08f));
                        break;
                }
            }

            Place(_exitRing.rectTransform, L.Exit, 0.84f);
            Place(_exitCore.rectTransform, L.Exit, 0.6f);
            if (_crystal != null) Place(_crystal.rectTransform, L.Crystal, 0.46f);

            float entity = _cell * 0.64f;
            _player.rectTransform.sizeDelta = new Vector2(entity, entity);
            _playerEye.sizeDelta = new Vector2(entity * 0.28f, entity * 0.28f);
            foreach (var e in _echoes) e.rectTransform.sizeDelta = new Vector2(_cell * 0.6f, _cell * 0.6f);
            foreach (var t in _echoNumbers) t.fontSize = Mathf.RoundToInt(_cell * 0.3f);
            for (int k = 0; k < _platforms.Length; k++) _platforms[k].rectTransform.sizeDelta = new Vector2(_cell * 0.88f, _cell * 0.88f);
        }

        public Vector2 CellCenter(Vec2i p)
        {
            return new Vector2((p.X + 0.5f) * _cell - _cell * L.Width / 2f, -(p.Y + 0.5f) * _cell + _cell * L.Height / 2f);
        }

        void Place(RectTransform rt, Vec2i p, float scale) => Place(rt, p, new Vector2(scale, scale));

        void Place(RectTransform rt, Vec2i p, Vector2 scale)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_cell * scale.x, _cell * scale.y);
            rt.anchoredPosition = CellCenter(p);
        }

        // ------------------------------------------------------------------ state

        /// <summary>Creates or removes echo images after an echo is made or the level restarts.</summary>
        public void SyncEchoCount()
        {
            int want = _sim.State.Echoes.Length;
            while (_echoes.Count < want)
            {
                int n = _echoes.Count + 1;
                var img = F.Image(_entityLayer, Theme.WithAlpha(Theme.Echo, 0.55f), SpriteFactory.Circle, 0f, "Echo " + n);
                img.transform.SetSiblingIndex(0); // under the player
                var num = F.Text(img.transform, n.ToString(), Theme.Caption, Theme.Background, TextAnchor.MiddleCenter, FontStyle.Bold, "Number");
                F.Stretch(num.rectTransform);
                _echoes.Add(img);
                _echoNumbers.Add(num);
            }
            while (_echoes.Count > want)
            {
                int last = _echoes.Count - 1;
                Destroy(_echoes[last].gameObject);
                _echoes.RemoveAt(last);
                _echoNumbers.RemoveAt(last);
            }
            if (_cell > 0) Layout();
        }

        /// <summary>Updates every visual to the simulation's state. Entities slide when animate is true.</summary>
        public void Refresh(bool animate)
        {
            var s = _sim.State;

            for (int i = 0; i < _ground.Length; i++)
            {
                var tile = L.Tiles[i];
                var p = new Vec2i(i % L.Width, i / L.Width);
                bool broken = tile == Tile.Crumble && s.Crumble[i] == 2;
                _ground[i].enabled = tile != Tile.Gap && !broken;
                if (_decor[i] != null)
                {
                    if (tile == Tile.SpikesOdd || tile == Tile.SpikesEven)
                    {
                        // Show whether the spikes will be up after the next move.
                        bool upNext = _sim.IsSpikeUp(p, s.Turn + 1);
                        _decor[i].color = Theme.WithAlpha(Theme.Danger, upNext ? 1f : 0.22f);
                    }
                    else if (tile == Tile.Crumble)
                    {
                        _decor[i].enabled = !broken;
                        _decor[i].color = Theme.WithAlpha(s.Crumble[i] == 1 ? Theme.Danger : Theme.TextMuted, s.Crumble[i] == 1 ? 0.7f : 0.35f);
                    }
                }
            }

            for (int i = 0; i < L.Devices.Count; i++)
            {
                var dev = L.Devices[i];
                bool on = s.DeviceActive[i];
                var c = Theme.Channel[dev.Channel];
                switch (dev.Kind)
                {
                    case DeviceKind.Plate:
                        _devMark[i].color = Theme.WithAlpha(c, on ? 1f : 0.15f);
                        break;
                    case DeviceKind.Door:
                        _devBase[i].color = Theme.WithAlpha(c, on ? 0.18f : 1f);
                        _devMark[i].enabled = !on;
                        break;
                    case DeviceKind.Switch:
                    case DeviceKind.TimedSwitch:
                        _devMark[i].color = Theme.WithAlpha(c, on ? 1f : 0.35f);
                        _devMark[i].rectTransform.localScale = Vector3.one * (on ? 1.15f : 0.85f);
                        if (_devText[i] != null) _devText[i].text = on ? s.DeviceTimer[i].ToString() : "";
                        break;
                    case DeviceKind.Bridge:
                        _devBase[i].color = Theme.WithAlpha(c, on ? 0.95f : 0.14f);
                        _devMark[i].enabled = on;
                        break;
                }
            }

            _exitRing.color = s.ExitOpen ? Theme.Echo : Theme.WithAlpha(Theme.TextMuted, 0.45f);
            _exitCore.enabled = s.ExitOpen;
            if (_crystal != null) _crystal.enabled = !s.CrystalTaken;

            for (int k = 0; k < _platforms.Length; k++)
                _platforms[k].rectTransform.anchoredPosition = CellCenter(s.PlatformPos[k]);

            if (_anim != null) StopCoroutine(_anim);
            if (animate && isActiveAndEnabled) _anim = StartCoroutine(AnimateEntities());
            else SnapEntities();
        }

        public void SetFacing(Move m)
        {
            if (m == Move.Wait) return;
            _facing = Moves.Delta(m);
        }

        void SnapEntities()
        {
            var s = _sim.State;
            _player.rectTransform.anchoredPosition = CellCenter(s.Player);
            for (int e = 0; e < _echoes.Count && e < s.Echoes.Length; e++)
                _echoes[e].rectTransform.anchoredPosition = CellCenter(s.Echoes[e]);
            PlaceEye();
            IsAnimating = false;
        }

        void PlaceEye()
        {
            float r = _player.rectTransform.sizeDelta.x * 0.2f;
            _playerEye.anchoredPosition = new Vector2(_facing.X * r, -_facing.Y * r);
        }

        IEnumerator AnimateEntities()
        {
            IsAnimating = true;
            var s = _sim.State;
            var items = new List<RectTransform> { _player.rectTransform };
            var targets = new List<Vector2> { CellCenter(s.Player) };
            for (int e = 0; e < _echoes.Count && e < s.Echoes.Length; e++)
            {
                items.Add(_echoes[e].rectTransform);
                targets.Add(CellCenter(s.Echoes[e]));
            }
            var starts = new List<Vector2>();
            foreach (var rt in items) starts.Add(rt.anchoredPosition);

            // Bumps: a short nudge toward the blocked tile for anything that didn't move.
            var nudges = new Vector2[items.Count];
            foreach (var ev in _sim.Events)
            {
                if (ev.Kind != SimEventKind.Bumped) continue;
                int idx = ev.Index == GridSimulation.PlayerIndex ? 0 : ev.Index + 1;
                if (idx < 0 || idx >= nudges.Length) continue;
                var dir = ev.To - ev.From;
                nudges[idx] = new Vector2(dir.X, -dir.Y) * _cell * 0.18f;
            }

            PlaceEye();
            float d = MoveDuration;
            for (float t = 0; t < d; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / d);
                float bump = Mathf.Sin(Mathf.Clamp01(t / d) * Mathf.PI);
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == null) continue;
                    items[i].anchoredPosition = Vector2.Lerp(starts[i], targets[i], k) + nudges[i] * bump;
                }
                yield return null;
            }
            SnapEntities();
            _anim = null;
        }

        public RectTransform PlayerRect => _player.rectTransform;

        // ------------------------------------------------------------------ styling

        static Color GroundColor(Tile t, int x, int y)
        {
            switch (t)
            {
                case Tile.Wall: return Theme.Wall;
                case Tile.Gap: return new Color(0, 0, 0, 0);
                case Tile.Crumble: return Theme.Hex("3A3552");
                case Tile.WindUp:
                case Tile.WindDown:
                case Tile.WindLeft:
                case Tile.WindRight:
                    return Theme.Hex("23395E");
                default:
                    return (x + y) % 2 == 0 ? Theme.Floor : Theme.FloorAlt;
            }
        }

        static string WindArrow(Tile t)
        {
            switch (t)
            {
                case Tile.WindUp: return "↑";
                case Tile.WindDown: return "↓";
                case Tile.WindLeft: return "←";
                default: return "→";
            }
        }
    }
}
