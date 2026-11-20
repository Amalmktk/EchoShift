using EchoShift.Simulation;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EchoShift.Game
{
    /// <summary>
    /// Works with either Unity input backend, so the project runs whichever one is active.
    /// The Android back button arrives as the Escape key in both.
    /// </summary>
    public static class InputHelper
    {
        public static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) return true;
#endif
            return false;
        }

        /// <summary>Arrow keys or WASD move, Space waits, Z undoes, E makes an echo, R restarts. For testing in the editor.</summary>
        public static bool TryKeyboardMove(out Move move)
        {
            move = Move.Wait;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) { move = Move.Up; return true; }
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) { move = Move.Down; return true; }
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) { move = Move.Left; return true; }
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) { move = Move.Right; return true; }
                if (k.spaceKey.wasPressedThisFrame) { move = Move.Wait; return true; }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { move = Move.Up; return true; }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { move = Move.Down; return true; }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) { move = Move.Left; return true; }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) { move = Move.Right; return true; }
            if (Input.GetKeyDown(KeyCode.Space)) { move = Move.Wait; return true; }
#endif
            return false;
        }

        public static bool UndoKey() => KeyDown("z");
        public static bool EchoKey() => KeyDown("e");
        public static bool RestartKey() => KeyDown("r");

        static bool KeyDown(string key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                switch (key)
                {
                    case "z": if (k.zKey.wasPressedThisFrame) return true; break;
                    case "e": if (k.eKey.wasPressedThisFrame) return true; break;
                    case "r": if (k.rKey.wasPressedThisFrame) return true; break;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            switch (key)
            {
                case "z": if (Input.GetKeyDown(KeyCode.Z)) return true; break;
                case "e": if (Input.GetKeyDown(KeyCode.E)) return true; break;
                case "r": if (Input.GetKeyDown(KeyCode.R)) return true; break;
            }
#endif
            return false;
        }
    }
}
