using UnityEngine;

namespace EchoShift.Game.UI
{
    /// <summary>
    /// Clean vector-style shapes drawn into textures at start-up: rounded rectangles,
    /// circles, rings, diamonds and triangles. Swap for real art later without touching layouts.
    /// </summary>
    public static class SpriteFactory
    {
        const int Size = 128;
        /// <summary>Corner radius baked into the rounded sprite, in texture pixels.</summary>
        public const float BakedRadius = 48f;

        static Sprite _rounded, _circle, _ring, _diamond, _triangle, _square;

        public static Sprite Rounded => _rounded != null ? _rounded : (_rounded = Make(RoundedAlpha, new Vector4(BakedRadius, BakedRadius, BakedRadius, BakedRadius)));
        public static Sprite Circle => _circle != null ? _circle : (_circle = Make(CircleAlpha, Vector4.zero));
        public static Sprite Ring => _ring != null ? _ring : (_ring = Make(RingAlpha, Vector4.zero));
        public static Sprite Diamond => _diamond != null ? _diamond : (_diamond = Make(DiamondAlpha, Vector4.zero));
        public static Sprite Triangle => _triangle != null ? _triangle : (_triangle = Make(TriangleAlpha, Vector4.zero));
        public static Sprite Square => _square != null ? _square : (_square = Make((x, y) => 1f, Vector4.zero));

        delegate float AlphaFn(float x, float y);

        static Sprite Make(AlphaFn fn, Vector4 border)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float a = Mathf.Clamp01(fn(x + 0.5f, y + 0.5f));
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        // Signed-distance helpers: return coverage 0..1 with a one-pixel soft edge.
        static float Edge(float distanceInside) => Mathf.Clamp01(distanceInside + 0.5f);

        static float RoundedAlpha(float x, float y)
        {
            float r = BakedRadius;
            float half = Size / 2f;
            float qx = Mathf.Abs(x - half) - (half - r);
            float qy = Mathf.Abs(y - half) - (half - r);
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
            return Edge(-outside);
        }

        static float CircleAlpha(float x, float y)
        {
            float half = Size / 2f;
            float d = new Vector2(x - half, y - half).magnitude;
            return Edge(half - 1f - d);
        }

        static float RingAlpha(float x, float y)
        {
            float half = Size / 2f;
            float d = new Vector2(x - half, y - half).magnitude;
            float outer = half - 1f - d;
            float inner = d - (half - 15f);
            return Mathf.Min(Edge(outer), Edge(inner));
        }

        static float DiamondAlpha(float x, float y)
        {
            float half = Size / 2f;
            float d = Mathf.Abs(x - half) + Mathf.Abs(y - half);
            return Edge((half - 2f - d) * 0.7071f);
        }

        static float TriangleAlpha(float x, float y)
        {
            // Point up: apex at the top centre, base along the bottom.
            float h = Size - 8f;
            float yy = y - 4f;
            if (yy < 0 || yy > h) return 0f;
            float halfWidth = (h - yy) / h * (Size / 2f - 4f);
            return Edge(halfWidth - Mathf.Abs(x - Size / 2f));
        }
    }
}
