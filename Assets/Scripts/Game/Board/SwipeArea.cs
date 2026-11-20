using System;
using EchoShift.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EchoShift.Game.Board
{
    /// <summary>
    /// Turns a drag anywhere on the board into one move. A move fires as soon as the finger has
    /// travelled far enough, so quick players never wait for the finger to lift.
    /// </summary>
    public class SwipeArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event Action<Move> Swiped;

        Vector2 _origin;
        bool _down;
        bool _fired;

        float Threshold => Mathf.Max(24f, Mathf.Min(Screen.width, Screen.height) * 0.045f);

        public void OnPointerDown(PointerEventData e)
        {
            _origin = e.position;
            _down = true;
            _fired = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_down || _fired) return;
            var delta = e.position - _origin;
            if (delta.magnitude < Threshold) return;
            _fired = true;
            Swiped?.Invoke(ToMove(delta));
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (_down && !_fired)
            {
                var delta = e.position - _origin;
                if (delta.magnitude >= Threshold) Swiped?.Invoke(ToMove(delta));
            }
            _down = false;
        }

        static Move ToMove(Vector2 d)
        {
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? Move.Right : Move.Left;
            return d.y > 0 ? Move.Up : Move.Down;
        }
    }
}
