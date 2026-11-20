using UnityEngine;

namespace EchoShift.Game.UI
{
    /// <summary>Design tokens from chapter 9 of the project docs. Sizes are pixels in a 1080-wide frame.</summary>
    public static class Theme
    {
        public static readonly Color Background = Hex("0F1626");
        public static readonly Color Surface = Hex("1C2640");
        public static readonly Color Surface2 = Hex("243152");
        public static readonly Color Line = Hex("33426A");
        public static readonly Color Text = Hex("F2F5FA");
        public static readonly Color TextMuted = Hex("9AA6BF");

        public static readonly Color Echo = Hex("3EE6F0");
        public static readonly Color Player = Hex("FF8A3D");
        public static readonly Color Success = Hex("5BE37D");
        public static readonly Color Danger = Hex("FF4D5E");
        public static readonly Color Star = Hex("FFC857");

        public static readonly Color Floor = Hex("243152");
        public static readonly Color FloorAlt = Hex("283659");
        public static readonly Color Wall = Hex("141D33");
        public static readonly Color WallEdge = Hex("33426A");
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.62f);

        /// <summary>One colour per channel a..e, shared by plates, switches, doors and bridges.</summary>
        public static readonly Color[] Channel =
        {
            Hex("FFB547"), Hex("B48CFF"), Hex("FF6FA8"), Hex("7BD88F"), Hex("8EC5FF")
        };

        public static readonly Color[] WorldAccent =
        {
            Hex("7BD88F"), Hex("8EC5FF"), Hex("FF7A45"), Hex("B8E6FF"), Hex("B48CFF")
        };

        // Type scale (px at 1080 wide).
        public const int Title = 108;
        public const int Heading = 72;
        public const int Button = 54;
        public const int Body = 48;
        public const int Caption = 39;

        // Spacing grid.
        public const float S1 = 24f;
        public const float S2 = 48f;
        public const float S3 = 72f;
        public const float S4 = 96f;

        public const float MenuButtonHeight = 150f;
        public const float ActionButtonHeight = 190f;
        public const float PanelRadius = 36f;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
