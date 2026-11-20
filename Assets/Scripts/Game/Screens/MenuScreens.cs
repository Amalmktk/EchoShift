using System.Collections;
using System.Linq;
using EchoShift.Game.UI;
using EchoShift.Simulation;
using UnityEngine;
using UnityEngine.UI;
using F = EchoShift.Game.UI.UIFactory;

namespace EchoShift.Game.Screens
{
    static class Logo
    {
        public static void Build(RectTransform parent, float y, float scale = 1f)
        {
            var echo = F.Text(parent, "ECHO", Mathf.RoundToInt(Theme.Title * 1.4f * scale), Theme.Echo, TextAnchor.MiddleCenter, FontStyle.Bold, "LogoEcho");
            F.Center(echo.rectTransform, new Vector2(1000, 180 * scale), new Vector2(0, y + 80 * scale));
            var shift = F.Text(parent, "SHIFT", Mathf.RoundToInt(Theme.Title * 1.4f * scale), Theme.Player, TextAnchor.MiddleCenter, FontStyle.Bold, "LogoShift");
            F.Center(shift.rectTransform, new Vector2(1000, 180 * scale), new Vector2(0, y - 80 * scale));
        }
    }

    /// <summary>Logo while services start, then consent, then the first level or the main menu.</summary>
    public class SplashScreen : Page
    {
        bool _done;

        public override void Build()
        {
            var safe = F.SafeContent(Root);
            Logo.Build(safe, 80f);
            var by = F.Text(safe, GameConfig.StudioName, Theme.Caption, Theme.TextMuted, TextAnchor.MiddleCenter);
            F.Bottom(by.rectTransform, 120, Theme.S3);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return new WaitForSeconds(1.0f);
            App.Ads.Start(Proceed);
            // If consent never answers (offline), carry on anyway.
            yield return new WaitForSeconds(8f);
            Proceed();
        }

        void Proceed()
        {
            if (_done || this == null) return;
            _done = true;
            var save = App.Save.Data;
            bool firstLaunch = !save.tutorialDone && save.levelsCompleted == 0;
            var first = App.Levels.All.FirstOrDefault();
            if (firstLaunch && first != null) App.Nav.Show<GameScreen>(g => g.Level = first);
            else App.Nav.Show<MainMenuScreen>();
        }

        public override void OnBack() { }
    }

    public class MainMenuScreen : Page
    {
        public override void Build()
        {
            var safe = F.SafeContent(Root);

            var top = F.HStack(safe, Theme.S1, new RectOffset(48, 48, 0, 0), TextAnchor.MiddleRight, "TopRow");
            F.Top(top, 140f, Theme.S2);
            top.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var shop = F.Button(top, Strings.T("shop.title"), ButtonStyle.Secondary, () => App.Nav.Show<ShopScreen>(s => s.Source = "menu"), "Shop");
            F.Layout(shop, 130, 260);
            var settings = F.Button(top, Strings.T("settings.title"), ButtonStyle.Secondary, () => App.Nav.Show<SettingsScreen>(), "Settings");
            F.Layout(settings, 130, 300);

            Logo.Build(safe, 380f);

            var cont = App.Levels.ContinueLevel(App.Save);
            int world = cont != null ? cont.World : 1;
            int stars = App.Levels.StarsInWorld(world, App.Save);
            int max = App.Levels.InWorld(world).Count * 3;
            var progress = F.Text(safe, "World " + world + " · " + WorldName(world) + "\n" + Strings.T("world.stars", stars, max),
                Theme.Body, Theme.TextMuted, TextAnchor.MiddleCenter);
            F.Center(progress.rectTransform, new Vector2(1000, 160), new Vector2(0, 80));

            var stack = F.VStack(safe, Theme.S1, new RectOffset(120, 120, 0, 0), TextAnchor.UpperCenter, "Buttons");
            stack.anchorMin = new Vector2(0, 0);
            stack.anchorMax = new Vector2(1, 0.5f);
            stack.offsetMin = new Vector2(0, 120);
            stack.offsetMax = new Vector2(0, -120);

            bool hasProgress = App.Save.Data.levels.Any(l => l.completed);
            if (hasProgress && cont != null)
            {
                var c = F.Button(stack, Strings.T("menu.continue") + "  ·  " + cont.Number, ButtonStyle.Primary, () => App.Nav.Show<GameScreen>(g => g.Level = cont));
                F.Layout(c, Theme.MenuButtonHeight + 20);
            }
            var play = F.Button(stack, Strings.T("menu.play"), hasProgress ? ButtonStyle.Secondary : ButtonStyle.Primary, () => App.Nav.Show<WorldSelectScreen>());
            F.Layout(play, Theme.MenuButtonHeight);
            var how = F.Button(stack, Strings.T("menu.howto"), ButtonStyle.Secondary, () => App.Nav.Show<HowToPlayScreen>());
            F.Layout(how, Theme.MenuButtonHeight);

            if (App.Hints.DailyHintAvailable)
            {
                Button daily = null;
                daily = F.Button(stack, Strings.T("menu.daily"), ButtonStyle.Echo, () =>
                {
                    if (App.Hints.ClaimDailyHint())
                    {
                        App.Audio.Play(Sfx.Crystal);
                        App.Toast(Strings.T("toast.daily"));
                    }
                    daily.gameObject.SetActive(false);
                });
                F.Layout(daily, Theme.MenuButtonHeight - 20);
            }
        }

        public static string WorldName(int world) =>
            world >= 1 && world <= GameConfig.WorldNames.Length ? GameConfig.WorldNames[world - 1] : "World " + world;

        public override void OnBack()
        {
            App.Nav.Show<ConfirmPopup>(c =>
            {
                c.TitleText = Strings.T("menu.quit.title");
                c.BodyText = Strings.T("menu.quit.body");
                c.ConfirmText = Strings.T("menu.quit.confirm");
                c.OnConfirm = App.Quit;
            });
        }
    }

    public class WorldSelectScreen : Page
    {
        public override void Build()
        {
            var safe = F.SafeContent(Root);
            Header.Build(safe, Strings.T("world.title"), OnBack);
            var body = F.Rect(safe, "Body");
            F.Stretch(body, 0, 180, 0, 0);
            var list = F.ScrollList(body, Theme.S2, new RectOffset(60, 60, 24, 96));

            int worldCount = Mathf.Max(GameConfig.WorldNames.Length, App.Levels.Worlds.DefaultIfEmpty(1).Max());
            for (int w = 1; w <= worldCount; w++)
            {
                int world = w;
                var levels = App.Levels.InWorld(world);
                bool exists = levels.Count > 0;
                bool unlocked = exists && App.Levels.IsWorldUnlocked(world, App.Save);
                var accent = Theme.WorldAccent[(world - 1) % Theme.WorldAccent.Length];

                var card = F.Panel(list, unlocked ? Theme.Surface : Theme.WithAlpha(Theme.Surface, 0.55f), Theme.PanelRadius, "World " + world);
                F.Layout(card, 300);
                var button = card.gameObject.AddComponent<Button>();
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                card.gameObject.AddComponent<PressScale>();
                button.onClick.AddListener(() =>
                {
                    if (unlocked) { App.Audio.Play(Sfx.Tap, 0.6f); App.Nav.Show<LevelSelectScreen>(s => s.World = world); }
                    else
                    {
                        App.Audio.Play(Sfx.Locked);
                        App.Toast(Strings.T(exists ? "world.locked_toast" : "world.coming_toast"));
                    }
                });

                var strip = F.Image(card.transform, unlocked ? accent : Theme.WithAlpha(accent, 0.3f), null, 24f, "Accent");
                strip.rectTransform.anchorMin = new Vector2(0, 0);
                strip.rectTransform.anchorMax = new Vector2(0, 1);
                strip.rectTransform.pivot = new Vector2(0, 0.5f);
                strip.rectTransform.offsetMin = new Vector2(36, 36);
                strip.rectTransform.offsetMax = new Vector2(60, -36);

                var name = F.Text(card.transform, world + ". " + MainMenuScreen.WorldName(world), Theme.Heading - 6,
                    unlocked ? Theme.Text : Theme.TextMuted, TextAnchor.UpperLeft, FontStyle.Bold, "Name");
                F.Stretch(name.rectTransform, 110, 54, 48, 120);

                string detail;
                if (!exists) detail = Strings.T("world.coming");
                else if (!unlocked) detail = Strings.T("common.locked") + "  ·  " + Strings.T("world.level_count", levels.Count);
                else detail = Strings.T("world.level_count", levels.Count) + "  ·  " +
                              Strings.T("world.stars", App.Levels.StarsInWorld(world, App.Save), levels.Count * 3);
                var info = F.Text(card.transform, detail, Theme.Body, Theme.TextMuted, TextAnchor.LowerLeft, FontStyle.Normal, "Detail");
                F.Stretch(info.rectTransform, 110, 150, 48, 54);
            }
        }

        public override void OnBack() => App.Nav.Show<MainMenuScreen>();
    }

    public class LevelSelectScreen : Page
    {
        public int World = 1;

        public override void Build()
        {
            var safe = F.SafeContent(Root);
            Header.Build(safe, "World " + World + " · " + MainMenuScreen.WorldName(World), OnBack);

            var levels = App.Levels.InWorld(World);
            var body = F.Rect(safe, "Body");
            F.Stretch(body, 0, 200, 0, 220);
            var list = F.ScrollList(body, 0, new RectOffset(60, 60, 12, 60));
            var gridRt = F.Rect(list, "Grid");
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(210, 230);
            grid.spacing = new Vector2(36, 36);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            int rows = Mathf.CeilToInt(levels.Count / 4f);
            F.Layout(gridRt, rows * 266f);

            var current = App.Levels.ContinueLevel(App.Save);
            foreach (var level in levels) Tile(gridRt, level, level == current);

            // Neighbouring worlds.
            var nav = F.HStack(safe, Theme.S2, new RectOffset(60, 60, 0, 0), TextAnchor.MiddleCenter, "WorldNav");
            F.Bottom(nav, 150f, Theme.S2);
            var worlds = App.Levels.Worlds.ToList();
            int idx = worlds.IndexOf(World);
            var prev = F.Button(nav, "←  World " + (World - 1), ButtonStyle.Secondary, () => App.Nav.Show<LevelSelectScreen>(s => s.World = World - 1));
            prev.interactable = idx > 0 && App.Levels.IsWorldUnlocked(World - 1, App.Save);
            var next = F.Button(nav, "World " + (World + 1) + "  →", ButtonStyle.Secondary, () => App.Nav.Show<LevelSelectScreen>(s => s.World = World + 1));
            next.interactable = idx >= 0 && idx < worlds.Count - 1 && App.Levels.IsWorldUnlocked(World + 1, App.Save);
        }

        void Tile(RectTransform grid, LevelDef level, bool current)
        {
            bool unlocked = App.Levels.IsLevelUnlocked(level, App.Save);
            var progress = App.Save.Progress(level.Id);

            var bg = current ? Theme.Echo : unlocked ? Theme.Surface2 : Theme.WithAlpha(Theme.Surface, 0.6f);
            var tile = F.Panel(grid, bg, 28f, "Level " + level.Number);
            var button = tile.gameObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            tile.gameObject.AddComponent<PressScale>();
            button.onClick.AddListener(() =>
            {
                if (unlocked) { App.Audio.Play(Sfx.Tap, 0.6f); App.Nav.Show<GameScreen>(g => g.Level = level); }
                else { App.Audio.Play(Sfx.Locked); App.Toast(Strings.T("levels.locked_toast")); }
            });

            var fg = current ? Theme.Background : unlocked ? Theme.Text : Theme.TextMuted;
            var num = F.Text(tile.transform, level.Number.ToString(), Theme.Heading, fg, TextAnchor.MiddleCenter, FontStyle.Bold, "Number");
            F.Stretch(num.rectTransform, 0, 20, 0, 70);

            if (!unlocked)
            {
                var lockText = F.Text(tile.transform, Strings.T("common.locked"), Theme.Caption - 6, Theme.TextMuted, TextAnchor.MiddleCenter);
                F.Stretch(lockText.rectTransform, 0, 150, 0, 20);
                return;
            }

            int stars = progress != null ? progress.stars : 0;
            for (int i = 0; i < 3; i++)
            {
                var s = F.Image(tile.transform, i < stars ? Theme.Star : Theme.WithAlpha(fg, 0.25f), SpriteFactory.Diamond, 0f, "Star");
                var rt = s.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(44, 44);
                rt.anchoredPosition = new Vector2((i - 1) * 54f, 50f);
            }
            if (level.IsBoss)
            {
                var boss = F.Text(tile.transform, "BOSS", Theme.Caption - 10, current ? Theme.Background : Theme.Danger, TextAnchor.UpperCenter, FontStyle.Bold);
                F.Stretch(boss.rectTransform, 0, 10, 0, 180);
            }
        }

        public override void OnBack() => App.Nav.Show<WorldSelectScreen>();
    }

    /// <summary>Back button and title for full screens that are not overlays.</summary>
    static class Header
    {
        public static void Build(RectTransform safe, string title, System.Action onBack)
        {
            var header = F.Rect(safe, "Header");
            F.Top(header, 180f);
            var back = F.IconButton(header, "←", () => { GameApp.Instance.Audio.Play(Sfx.Back); onBack(); }, "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0, 0.5f);
            brt.sizeDelta = new Vector2(140, 140);
            brt.anchoredPosition = new Vector2(Theme.S2, 0);
            var t = F.Text(header, title, Theme.Heading - 6, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            F.Stretch(t.rectTransform, 200, 0, 200, 0);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 32;
            t.resizeTextMaxSize = Theme.Heading - 6;
        }
    }
}
