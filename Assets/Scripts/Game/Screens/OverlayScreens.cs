using System;
using EchoShift.Game.UI;
using UnityEngine;
using UnityEngine.UI;
using F = EchoShift.Game.UI.UIFactory;

namespace EchoShift.Game.Screens
{
    public class SettingsScreen : FullPage
    {
        protected override string TitleText => Strings.T("settings.title");

        protected override void BuildBody(RectTransform body)
        {
            var list = F.ScrollList(body, Theme.S1, new RectOffset(60, 60, 12, 96));
            var s = App.Save.Data.settings;

            Toggle(list, "settings.music", () => s.music, v => s.music = v);
            Toggle(list, "settings.sfx", () => s.sfx, v => s.sfx = v);
            Toggle(list, "settings.vibration", () => s.vibration, v => { s.vibration = v; if (v) Haptics.Medium(); });
            Toggle(list, "settings.reduce_motion", () => s.reduceMotion, v => s.reduceMotion = v);

            Cycle(list, "settings.controls", () => Strings.T(s.dpad ? "settings.controls.dpad" : "settings.controls.swipe"),
                () => s.dpad = !s.dpad);
            Cycle(list, "settings.button_size", () => Strings.T("settings.size." + s.buttonSize),
                () => s.buttonSize = (s.buttonSize + 1) % 3);
            Cycle(list, "settings.hand", () => Strings.T(s.leftHanded ? "settings.hand.left" : "settings.hand.right"),
                () => s.leftHanded = !s.leftHanded);
            Row(list, Strings.T("settings.language"), Strings.T("settings.language.value"),
                () => App.Toast(Strings.T("settings.language.toast")));

            Spacer(list);
            Wide(list, Strings.T("settings.privacy"), ButtonStyle.Secondary, () =>
            {
                if (App.Ads.PrivacyOptionsRequired) App.Ads.ShowPrivacyOptions(() => { });
                else App.Toast(Strings.T("settings.privacy.none"));
            });
            Wide(list, Strings.T("shop.restore"), ButtonStyle.Secondary, () =>
                App.Store.Restore(ok => App.Toast(Strings.T(ok ? "shop.restored" : "shop.offline"))));
            Wide(list, Strings.T("settings.about"), ButtonStyle.Secondary, () => App.Nav.Show<CreditsScreen>());
            Wide(list, Strings.T("settings.reset"), ButtonStyle.Ghost, () =>
                App.Nav.Show<ConfirmPopup>(c =>
                {
                    c.TitleText = Strings.T("settings.reset.title");
                    c.BodyText = Strings.T("settings.reset.body");
                    c.ConfirmText = Strings.T("settings.reset.confirm");
                    c.OnConfirm = () =>
                    {
                        App.Save.ResetProgress();
                        App.Analytics.Log("settings_change", "setting", "reset_progress", "value", 1);
                        App.Toast(Strings.T("settings.reset.done"));
                        App.Nav.Show<MainMenuScreen>();
                    };
                }));
        }

        void Toggle(RectTransform list, string key, Func<bool> get, Action<bool> set)
        {
            Text value = null;
            value = Row(list, Strings.T(key), Strings.T(get() ? "common.on" : "common.off"), () =>
            {
                set(!get());
                App.Save.Save();
                value.text = Strings.T(get() ? "common.on" : "common.off");
                App.Analytics.Log("settings_change", "setting", key, "value", get() ? 1 : 0);
            });
        }

        void Cycle(RectTransform list, string key, Func<string> label, Action next)
        {
            Text value = null;
            value = Row(list, Strings.T(key), label(), () =>
            {
                next();
                App.Save.Save();
                value.text = label();
                App.Analytics.Log("settings_change", "setting", key, "value", label());
            });
        }

        Text Row(RectTransform list, string label, string value, Action onClick)
        {
            var row = F.Panel(list, Theme.Surface, 28f, "Row " + label);
            F.Layout(row, 150);
            var l = F.Text(row.transform, label, Theme.Body, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Label");
            F.Stretch(l.rectTransform, 48, 0, 380, 0);
            var b = F.Button(row.transform, value, ButtonStyle.Secondary, onClick, "Value");
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(320, 110);
            rt.anchoredPosition = new Vector2(-24, 0);
            var text = F.Label(b);
            text.fontSize = Theme.Body - 4;
            return text;
        }

        void Wide(RectTransform list, string label, ButtonStyle style, Action onClick)
        {
            var b = F.Button(list, label, style, onClick);
            F.Layout(b, Theme.MenuButtonHeight - 10);
        }

        static void Spacer(RectTransform list)
        {
            var s = F.Rect(list, "Spacer");
            F.Layout(s, Theme.S1);
        }
    }

    public class HowToPlayScreen : FullPage
    {
        protected override string TitleText => Strings.T("howto.title");
        const int Cards = 5;
        int _page;
        Text _title;
        Text _body;
        Text _dots;
        Button _prev;
        Button _next;

        protected override void BuildBody(RectTransform body)
        {
            var card = F.Panel(body, Theme.Surface, Theme.PanelRadius, "Card");
            F.Stretch(card.rectTransform, 60, 40, 60, 360);
            _title = F.Text(card.transform, "", Theme.Heading, Theme.Echo, TextAnchor.UpperCenter, FontStyle.Bold, "CardTitle");
            F.Top(_title.rectTransform, 140, 80, 60);
            _body = F.Text(card.transform, "", Theme.Body + 4, Theme.Text, TextAnchor.UpperCenter, FontStyle.Normal, "CardBody");
            F.Stretch(_body.rectTransform, 80, 260, 80, 80);

            _dots = F.Text(body, "", Theme.Heading, Theme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold, "Dots");
            F.Bottom(_dots.rectTransform, 100, 230);

            var row = F.HStack(body, Theme.S2, new RectOffset(60, 60, 0, 0), TextAnchor.MiddleCenter, "Nav");
            F.Bottom(row, Theme.MenuButtonHeight, 60);
            _prev = F.Button(row, Strings.T("howto.prev"), ButtonStyle.Secondary, () => Show(_page - 1));
            _next = F.Button(row, Strings.T("howto.next"), ButtonStyle.Primary, () =>
            {
                if (_page < Cards - 1) Show(_page + 1);
                else Close();
            });

            var swipe = card.gameObject.AddComponent<Board.SwipeArea>();
            swipe.Swiped += m =>
            {
                if (m == Simulation.Move.Left && _page < Cards - 1) Show(_page + 1);
                if (m == Simulation.Move.Right && _page > 0) Show(_page - 1);
            };

            Show(0);
        }

        void Show(int page)
        {
            _page = Mathf.Clamp(page, 0, Cards - 1);
            _title.text = Strings.T("howto." + (_page + 1) + ".title");
            _body.text = Strings.T("howto." + (_page + 1) + ".body");
            var dots = "";
            for (int i = 0; i < Cards; i++) dots += i == _page ? "● " : "○ ";
            _dots.text = dots.Trim();
            _prev.interactable = _page > 0;
            F.Label(_next).text = _page < Cards - 1 ? Strings.T("howto.next") : Strings.T("common.close");
        }
    }

    public class CreditsScreen : FullPage
    {
        protected override string TitleText => Strings.T("credits.title");

        protected override void BuildBody(RectTransform body)
        {
            var list = F.ScrollList(body, Theme.S2, new RectOffset(60, 60, 24, 96));
            Line(list, GameConfig.GameName, Theme.Heading, Theme.Text, FontStyle.Bold);
            Line(list, Strings.T("credits.made_by", GameConfig.StudioName), Theme.Body, Theme.TextMuted, FontStyle.Normal);
            Line(list, Strings.T("credits.fonts"), Theme.Caption, Theme.TextMuted, FontStyle.Normal);
            var privacy = F.Button(list, Strings.T("credits.privacy"), ButtonStyle.Secondary, () => Application.OpenURL(GameConfig.PrivacyPolicyUrl));
            F.Layout(privacy, Theme.MenuButtonHeight - 10);
            var terms = F.Button(list, Strings.T("credits.terms"), ButtonStyle.Secondary, () => Application.OpenURL(GameConfig.TermsUrl));
            F.Layout(terms, Theme.MenuButtonHeight - 10);
            Line(list, Strings.T("credits.version", Application.version), Theme.Caption, Theme.TextMuted, FontStyle.Normal);
        }

        static void Line(RectTransform list, string text, int size, Color color, FontStyle style)
        {
            var t = F.Text(list, text, size, color, TextAnchor.MiddleCenter, style);
            F.Layout(t, size * 2.2f);
        }
    }

    public class ShopScreen : FullPage
    {
        public string Source = "menu";
        protected override string TitleText => Strings.T("shop.title");
        Text _owned;
        RectTransform _list;

        protected override void BuildBody(RectTransform body)
        {
            App.Analytics.Log("store_view", "source", Source);
            _list = F.ScrollList(body, Theme.S2, new RectOffset(60, 60, 12, 96));
            Fill();
            App.Store.Granted += OnGranted;
        }

        void OnDestroy()
        {
            if (App != null && App.Store != null) App.Store.Granted -= OnGranted;
        }

        void OnGranted(string productId)
        {
            if (this == null) return;
            foreach (Transform child in _list) Destroy(child.gameObject);
            Fill();
        }

        void Fill()
        {
            _owned = F.Text(_list, Strings.T("shop.hints_owned", App.Hints.Balance), Theme.Body, Theme.Echo, TextAnchor.MiddleCenter, FontStyle.Bold);
            F.Layout(_owned, 90);

            Item(GameConfig.ProductRemoveAds, Strings.T("shop.remove_ads"), Strings.T("shop.remove_ads.desc"), App.Store.RemoveAdsOwned, false);
            Item(GameConfig.ProductHints10, Strings.T("shop.hints10"), "", false, false);
            Item(GameConfig.ProductHints30, Strings.T("shop.hints30"), "", false, true);

            var restore = F.Button(_list, Strings.T("shop.restore"), ButtonStyle.Ghost, () =>
                App.Store.Restore(ok => App.Toast(Strings.T(ok ? "shop.restored" : "shop.offline"))));
            F.Layout(restore, Theme.MenuButtonHeight - 20);
        }

        void Item(string productId, string title, string description, bool owned, bool bestValue)
        {
            var card = F.Panel(_list, Theme.Surface, Theme.PanelRadius, "Item " + productId);
            F.Layout(card, string.IsNullOrEmpty(description) ? 220 : 300);

            var t = F.Text(card.transform, title, Theme.Heading - 12, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold, "Title");
            F.Stretch(t.rectTransform, 56, 48, 420, 48);
            if (bestValue)
            {
                var tag = F.Text(card.transform, Strings.T("shop.best"), Theme.Caption, Theme.Star, TextAnchor.LowerLeft, FontStyle.Bold, "Best");
                F.Stretch(tag.rectTransform, 56, 48, 420, 40);
            }
            if (!string.IsNullOrEmpty(description))
            {
                var d = F.Text(card.transform, description, Theme.Caption, Theme.TextMuted, TextAnchor.LowerLeft, FontStyle.Normal, "Description");
                F.Stretch(d.rectTransform, 56, 130, 420, 40);
            }

            string price = App.Store.Price(productId);
            string label = owned ? Strings.T("shop.owned") : string.IsNullOrEmpty(price) ? "—" : price;
            var b = F.Button(card.transform, label, owned ? ButtonStyle.Secondary : ButtonStyle.Primary, () => Buy(productId), "Buy");
            b.interactable = !owned;
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(320, 130);
            rt.anchoredPosition = new Vector2(-48, 0);
        }

        void Buy(string productId)
        {
            if (!App.Store.IsReady)
            {
                App.Toast(Strings.T("shop.offline"));
                return;
            }
            App.Store.Buy(productId, ok =>
            {
                if (!ok)
                {
                    App.Toast(Strings.T("shop.failed"));
                    return;
                }
                App.Audio.Play(Sfx.Purchase);
                Haptics.Success();
                switch (productId)
                {
                    case GameConfig.ProductRemoveAds: App.Toast(Strings.T("shop.ads_removed")); break;
                    case GameConfig.ProductHints10: App.Toast(Strings.T("shop.hints_added", 10)); break;
                    case GameConfig.ProductHints30: App.Toast(Strings.T("shop.hints_added", 30)); break;
                }
            });
        }
    }
}
