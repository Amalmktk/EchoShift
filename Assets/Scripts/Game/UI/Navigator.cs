using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using F = EchoShift.Game.UI.UIFactory;

namespace EchoShift.Game.UI
{
    /// <summary>
    /// A screen, popup or full-page overlay. Screens replace everything; overlays stack on top.
    /// Android back always goes to the top page first.
    /// </summary>
    public abstract class Page : MonoBehaviour
    {
        protected static GameApp App => GameApp.Instance;
        public RectTransform Root { get; private set; }

        /// <summary>Overlays stack on top of the current screen. Screens replace the whole stack.</summary>
        public virtual bool IsOverlay => false;

        internal void Init(RectTransform root) => Root = root;

        public abstract void Build();

        /// <summary>Android back. Overlays close by default; screens decide for themselves.</summary>
        public virtual void OnBack()
        {
            if (IsOverlay) Close();
        }

        /// <summary>Called when the page above this one closes.</summary>
        public virtual void OnRevealed() { }

        public void Close() => App.Nav.Close(this);
    }

    /// <summary>A centred card over a dimmed background.</summary>
    public abstract class Popup : Page
    {
        public override bool IsOverlay => true;
        protected virtual bool CloseOnDimTap => true;
        protected virtual float Width => 920f;

        public override void Build()
        {
            System.Action onDim = OnBack;
            if (!CloseOnDimTap) onDim = () => { };
            F.Dim(Root, onDim);
            var safe = F.SafeContent(Root);
            var card = F.Panel(safe, Theme.Surface, Theme.PanelRadius, "Card");
            var rt = card.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var stack = card.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(64, 64, 64, 64);
            stack.spacing = Theme.S1;
            stack.childControlHeight = true;
            stack.childControlWidth = true;
            stack.childForceExpandHeight = false;
            stack.childForceExpandWidth = true;
            stack.childAlignment = TextAnchor.UpperCenter;
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            F.Layout(card, -1, Width);
            rt.sizeDelta = new Vector2(Width, 0);

            BuildCard(rt);
            App.StartCoroutine(PopIn(rt));
        }

        protected abstract void BuildCard(RectTransform card);

        IEnumerator PopIn(RectTransform rt)
        {
            bool reduce = App.Save.Data.settings.reduceMotion;
            float d = reduce ? 0.01f : 0.2f;
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0; t < d && rt != null; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / d);
                rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, k);
                group.alpha = k;
                yield return null;
            }
            if (rt == null) yield break;
            rt.localScale = Vector3.one;
            group.alpha = 1f;
        }

        // Helpers for popup content.
        protected Text Title(RectTransform card, string text)
        {
            var t = F.Text(card, text, Theme.Heading, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            F.Layout(t, 110);
            return t;
        }

        protected Text Body(RectTransform card, string text, Color? color = null)
        {
            // The card's layout group sizes text from its preferred height.
            return F.Text(card, text, Theme.Body, color ?? Theme.TextMuted, TextAnchor.MiddleCenter);
        }

        protected Button AddButton(RectTransform card, string label, ButtonStyle style, System.Action onClick)
        {
            var b = F.Button(card, label, style, onClick);
            F.Layout(b, Theme.MenuButtonHeight);
            return b;
        }
    }

    /// <summary>An opaque full-screen overlay with a header and Back button (Settings, Shop...).</summary>
    public abstract class FullPage : Page
    {
        public override bool IsOverlay => true;
        protected abstract string TitleText { get; }

        public override void Build()
        {
            var bg = F.Panel(Root, Theme.Background, 0f, "Background");
            F.Stretch(bg.rectTransform);
            var safe = F.SafeContent(Root);

            var header = F.Rect(safe, "Header");
            F.Top(header, 180f);
            var back = F.IconButton(header, "←", () => { App.Audio.Play(Sfx.Back); OnBack(); }, "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0, 0.5f);
            brt.sizeDelta = new Vector2(140, 140);
            brt.anchoredPosition = new Vector2(Theme.S2, 0);
            var title = F.Text(header, TitleText, Theme.Heading, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            F.Stretch(title.rectTransform, 220, 0, 220, 0);

            var body = F.Rect(safe, "Body");
            F.Stretch(body, 0, 180, 0, 0);
            BuildBody(body);
        }

        protected abstract void BuildBody(RectTransform body);
    }

    public class Navigator : MonoBehaviour
    {
        readonly List<Page> _stack = new List<Page>();
        RectTransform _pages;
        RectTransform _toasts;

        public static Navigator Create(Transform parent)
        {
            var canvasGo = new GameObject("UI", typeof(RectTransform));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.layer = 5;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<CanvasFit>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var nav = canvasGo.AddComponent<Navigator>();
            var root = (RectTransform)canvasGo.transform;

            var bg = F.Image(root, Theme.Background, null, 0f, "Background");
            F.Stretch(bg.rectTransform);

            nav._pages = F.Rect(root, "Pages");
            F.Stretch(nav._pages);
            nav._toasts = F.Rect(root, "Toasts");
            F.Stretch(nav._toasts);
            return nav;
        }

        public Page Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        public T Find<T>() where T : Page
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
                if (_stack[i] is T t) return t;
            return null;
        }

        public T Show<T>(Action<T> setup = null) where T : Page
        {
            var rt = F.Rect(_pages, typeof(T).Name);
            F.Stretch(rt);
            var page = rt.gameObject.AddComponent<T>();
            page.Init(rt);
            setup?.Invoke(page);

            if (!page.IsOverlay)
            {
                foreach (var p in _stack)
                    if (p != null) Destroy(p.gameObject);
                _stack.Clear();
            }
            _stack.Add(page);
            page.Build();
            return page;
        }

        public void Close(Page page)
        {
            if (page == null || !_stack.Remove(page)) return;
            Destroy(page.gameObject);
            Top?.OnRevealed();
        }

        /// <summary>Closes every overlay above the current screen.</summary>
        public void CloseOverlays()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (!_stack[i].IsOverlay) break;
                Destroy(_stack[i].gameObject);
                _stack.RemoveAt(i);
            }
            Top?.OnRevealed();
        }

        public void Back()
        {
            Top?.OnBack();
        }

        public void Toast(string message)
        {
            foreach (Transform child in _toasts) Destroy(child.gameObject);
            var safe = F.SafeContent(_toasts);
            var card = F.Panel(safe, Theme.Surface2, 999f, "Toast");
            card.raycastTarget = false;
            var rt = card.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -200);
            var text = F.Text(card.transform, message, Theme.Body - 4, Theme.Text);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            float width = Mathf.Min(960f, F.Font != null ? text.preferredWidth + 120f : 800f);
            rt.sizeDelta = new Vector2(Mathf.Max(400f, width), 130f);
            F.Stretch(text.rectTransform, 48, 0, 48, 0);
            StartCoroutine(FadeToast(safe.gameObject, card));
        }

        IEnumerator FadeToast(GameObject holder, Graphic card)
        {
            var group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            float shown = 2.0f, fade = 0.3f;
            for (float t = 0; t < shown + fade; t += Time.unscaledDeltaTime)
            {
                if (holder == null) yield break;
                group.alpha = t < 0.15f ? t / 0.15f : t > shown ? 1f - (t - shown) / fade : 1f;
                yield return null;
            }
            if (holder != null) Destroy(holder);
        }
    }
}
