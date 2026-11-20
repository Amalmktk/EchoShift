using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EchoShift.Game.UI
{
    public enum ButtonStyle
    {
        Primary,
        Secondary,
        Ghost,
        Echo,
        Danger
    }

    /// <summary>Small helpers that build Unity UI from code using the design tokens.</summary>
    public static class UIFactory
    {
        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        // ------------------------------------------------------------------ rects

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Anchors to the top edge with a fixed height.</summary>
        public static RectTransform Top(RectTransform rt, float height, float offset = 0, float sidePad = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(sidePad, -offset - height);
            rt.offsetMax = new Vector2(-sidePad, -offset);
            return rt;
        }

        /// <summary>Anchors to the bottom edge with a fixed height.</summary>
        public static RectTransform Bottom(RectTransform rt, float height, float offset = 0, float sidePad = 0)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(sidePad, offset);
            rt.offsetMax = new Vector2(-sidePad, offset + height);
            return rt;
        }

        public static RectTransform Center(RectTransform rt, Vector2 size, Vector2 offset = default)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            return rt;
        }

        // ------------------------------------------------------------------ graphics

        public static Image Image(Transform parent, Color color, Sprite sprite = null, float radius = 0f, string name = "Image")
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null) img.sprite = sprite;
            if (radius > 0f)
            {
                img.sprite = SpriteFactory.Rounded;
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = SpriteFactory.BakedRadius / radius;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(Transform parent, Color color, float radius = Theme.PanelRadius, string name = "Panel")
        {
            var img = Image(parent, color, null, radius, name);
            img.raycastTarget = true;
            return img;
        }

        public static Text Text(Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, string name = "Text")
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.lineSpacing = 1.1f;
            return t;
        }

        // ------------------------------------------------------------------ buttons

        public static Button Button(Transform parent, string label, ButtonStyle style, Action onClick, string name = null)
        {
            Color bg, fg;
            switch (style)
            {
                case ButtonStyle.Primary: bg = Theme.Player; fg = Theme.Background; break;
                case ButtonStyle.Echo: bg = Theme.Echo; fg = Theme.Background; break;
                case ButtonStyle.Danger: bg = Theme.Danger; fg = Theme.Text; break;
                case ButtonStyle.Ghost: bg = new Color(0, 0, 0, 0); fg = Theme.TextMuted; break;
                default: bg = Theme.Surface2; fg = Theme.Text; break;
            }

            // A huge radius makes Unity shrink the corners to fit, which gives a pill shape.
            var img = Image(parent, bg, null, 999f, name ?? ("Button " + label));
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var text = Text(img.transform, label, Theme.Button, fg, TextAnchor.MiddleCenter, FontStyle.Bold, "Label");
            Stretch(text.rectTransform, 24, 0, 24, 0);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            img.gameObject.AddComponent<PressScale>();
            button.onClick.AddListener(() =>
            {
                GameApp.Instance?.Audio.Play(Sfx.Tap, 0.6f);
                onClick?.Invoke();
            });
            return button;
        }

        public static Text Label(Button b) => b.transform.Find("Label").GetComponent<Text>();

        /// <summary>A round button showing a short symbol, for the top bars.</summary>
        public static Button IconButton(Transform parent, string symbol, Action onClick, string name)
        {
            var b = Button(parent, symbol, ButtonStyle.Secondary, onClick, name);
            Label(b).fontSize = Theme.Heading - 8;
            return b;
        }

        // ------------------------------------------------------------------ layout

        public static RectTransform VStack(Transform parent, float spacing, RectOffset padding, TextAnchor align = TextAnchor.UpperCenter, string name = "Stack")
        {
            var rt = Rect(parent, name);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset();
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform HStack(Transform parent, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.MiddleCenter, string name = "Row")
        {
            var rt = Rect(parent, name);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding ?? new RectOffset();
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return rt;
        }

        public static LayoutElement Layout(Component c, float height = -1, float width = -1, float flexibleWidth = -1)
        {
            // TryGetComponent instead of ?? : in the editor a missing component is a "fake null" that ?? ignores.
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            if (height >= 0) { le.preferredHeight = height; le.minHeight = height; }
            if (width >= 0) { le.preferredWidth = width; le.minWidth = width; }
            if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
            return le;
        }

        /// <summary>A vertical scrolling list. Returns the content rect to add children to.</summary>
        public static RectTransform ScrollList(Transform parent, float spacing, RectOffset padding)
        {
            var viewport = Rect(parent, "Scroll");
            Stretch(viewport);
            var bg = viewport.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            var content = VStack(viewport, spacing, padding, TextAnchor.UpperCenter, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        /// <summary>A child that follows the device safe area (notches, gesture bar).</summary>
        public static RectTransform SafeContent(Transform parent)
        {
            var rt = Rect(parent, "Safe");
            Stretch(rt);
            rt.gameObject.AddComponent<SafeArea>();
            return rt;
        }

        public static Image Dim(Transform parent, Action onTap = null)
        {
            var img = Image(parent, Theme.Dim, null, 0f, "Dim");
            Stretch(img.rectTransform);
            img.raycastTarget = true;
            if (onTap != null)
            {
                var b = img.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.navigation = new Navigation { mode = Navigation.Mode.None };
                b.onClick.AddListener(() => onTap());
            }
            return img;
        }
    }

    /// <summary>Scales a button to 95% while pressed.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData e) => transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        public void OnPointerUp(PointerEventData e) => transform.localScale = Vector3.one;
        public void OnPointerExit(PointerEventData e) => transform.localScale = Vector3.one;
    }

    /// <summary>Keeps a rect inside Screen.safeArea.</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect _applied;
        Vector2Int _screen;

        void OnEnable() => Apply();
        void Update()
        {
            if (Screen.safeArea != _applied || _screen.x != Screen.width || _screen.y != Screen.height) Apply();
        }

        void Apply()
        {
            var area = Screen.safeArea;
            _applied = area;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rt.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Matches width on tall phones and height on wider screens such as tablets.</summary>
    public class CanvasFit : MonoBehaviour
    {
        CanvasScaler _scaler;
        Vector2Int _screen;

        void Awake() => _scaler = GetComponent<CanvasScaler>();

        void Update()
        {
            if (_screen.x == Screen.width && _screen.y == Screen.height) return;
            _screen = new Vector2Int(Screen.width, Screen.height);
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            _scaler.matchWidthOrHeight = aspect > 1080f / 1920f ? 1f : 0f;
        }
    }
}
