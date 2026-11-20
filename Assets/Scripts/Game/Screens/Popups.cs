using System;
using System.Collections;
using EchoShift.Game.UI;
using EchoShift.Simulation;
using UnityEngine;
using UnityEngine.UI;
using F = EchoShift.Game.UI.UIFactory;

namespace EchoShift.Game.Screens
{
    public class PausePopup : Popup
    {
        public GameScreen Game;

        protected override void BuildCard(RectTransform card)
        {
            Title(card, Strings.T("pause.title"));
            Body(card, Game.Level.Number + ". " + Game.Level.Name);
            AddButton(card, Strings.T("pause.resume"), ButtonStyle.Primary, Close);
            AddButton(card, Strings.T("pause.restart"), ButtonStyle.Secondary, () => Game.Restart());
            AddButton(card, Strings.T("pause.hint"), ButtonStyle.Secondary, () => Game.RequestHint("pause"));
            AddButton(card, Strings.T("pause.settings"), ButtonStyle.Secondary, () => App.Nav.Show<SettingsScreen>());
            AddButton(card, Strings.T("menu.howto"), ButtonStyle.Secondary, () => App.Nav.Show<HowToPlayScreen>());
            AddButton(card, Strings.T("pause.menu"), ButtonStyle.Ghost, () =>
                App.Nav.Show<ConfirmPopup>(c =>
                {
                    c.TitleText = Strings.T("pause.leave.title");
                    c.BodyText = Strings.T("pause.leave.body");
                    c.ConfirmText = Strings.T("pause.leave.confirm");
                    c.OnConfirm = () => App.Nav.Show<MainMenuScreen>();
                }));
        }
    }

    public class LevelCompletePopup : Popup
    {
        public LevelDef Level;
        public int Stars;
        public int Moves;
        public int EchoesUsed;
        public bool Crystal;

        protected override bool CloseOnDimTap => false;

        Button _bonus;
        readonly Image[] _stars = new Image[3];

        protected override void BuildCard(RectTransform card)
        {
            Title(card, Strings.T("complete.title"));

            var row = F.HStack(card, Theme.S2, null, TextAnchor.MiddleCenter, "Stars");
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            F.Layout(row, 170);
            for (int i = 0; i < 3; i++)
            {
                _stars[i] = F.Image(row, Theme.WithAlpha(Theme.TextMuted, 0.25f), SpriteFactory.Diamond, 0f, "Star " + (i + 1));
                F.Layout(_stars[i], 150, 150);
            }

            Body(card, Strings.T("complete.moves", Moves) + "\n" +
                       Strings.T("complete.echoes", EchoesUsed, Level.ParEchoes) + "\n" +
                       Strings.T(Crystal || !Level.HasCrystal ? "complete.crystal_yes" : "complete.crystal_no"));

            AddButton(card, Strings.T("complete.next"), ButtonStyle.Primary, Next);
            AddButton(card, Strings.T("complete.replay"), ButtonStyle.Secondary, () => App.Nav.Show<GameScreen>(g => g.Level = Level));
            AddButton(card, Strings.T("complete.levels"), ButtonStyle.Secondary, Levels);
            if (App.Ads.IsRewardedReady)
                _bonus = AddButton(card, Strings.T("complete.bonus"), ButtonStyle.Ghost, Bonus);

            App.StartCoroutine(RevealStars());
        }

        IEnumerator RevealStars()
        {
            yield return new WaitForSeconds(0.25f);
            for (int i = 0; i < Stars && i < 3; i++)
            {
                if (_stars[i] == null) yield break;
                _stars[i].color = Theme.Star;
                App.Audio.Play(Sfx.Star, 0.7f);
                var rt = _stars[i].rectTransform;
                for (float t = 0; t < 0.3f && rt != null; t += Time.unscaledDeltaTime)
                {
                    rt.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(t / 0.3f * Mathf.PI));
                    yield return null;
                }
                if (rt != null) rt.localScale = Vector3.one;
            }
        }

        void Next()
        {
            App.Ads.MaybeShowInterstitial(Level.Number, () =>
            {
                GameScreen.GoToLevelAfter(Level);
                if (App.Ads.ShouldOfferRemoveAds()) App.Nav.Show<ShopScreen>(s => s.Source = "remove_ads_offer");
            });
        }

        void Levels() => App.Nav.Show<LevelSelectScreen>(s => s.World = Level.World);

        void Bonus()
        {
            App.Ads.ShowRewarded("level_complete", earned =>
            {
                if (!earned)
                {
                    App.Toast(Strings.T("reward.unavailable"));
                    return;
                }
                App.Hints.Grant(1);
                App.Toast(Strings.T("reward.granted"));
                if (_bonus != null) _bonus.gameObject.SetActive(false);
            });
        }

        public override void OnBack() => Levels();
    }

    public class RetryPopup : Popup
    {
        public GameScreen Game;
        protected override bool CloseOnDimTap => false;

        protected override void BuildCard(RectTransform card)
        {
            string cause = Game.Sim.State.FailCause;
            Title(card, Strings.T(cause == "fell" ? "retry.title.fell" : "retry.title.spikes"));
            AddButton(card, Strings.T("retry.undo"), ButtonStyle.Primary, UndoLast);
            AddButton(card, Strings.T("retry.retry"), ButtonStyle.Secondary, () => { Close(); Game.Restart(); });
            AddButton(card, Strings.T("pause.hint"), ButtonStyle.Secondary, () => Game.RequestHint("retry"));
            AddButton(card, Strings.T("complete.levels"), ButtonStyle.Ghost, () => App.Nav.Show<LevelSelectScreen>(s => s.World = Game.Level.World));
        }

        void UndoLast()
        {
            Close();
            Game.Undo();
        }

        public override void OnBack() => UndoLast();
    }

    public class HintPopup : Popup
    {
        public GameScreen Game;
        public string Source = "hud";

        protected override void BuildCard(RectTransform card)
        {
            var level = Game.Level;
            int unlocked = Mathf.Max(1, App.Hints.UnlockedSteps(level.Id));
            Title(card, Strings.T("hint.title", unlocked));

            for (int step = 1; step <= unlocked; step++)
            {
                string text = step == 3 ? HintService.SolutionText(level) : level.Hints[step - 1];
                var t = Body(card, text, step == unlocked ? Theme.Text : Theme.TextMuted);
                if (step == 3) t.fontSize = Theme.Body - 4;
            }

            var left = Body(card, Strings.T("hint.left", App.Hints.Balance));
            left.fontSize = Theme.Caption;

            if (unlocked < 3)
                AddButton(card, Strings.T("hint.next"), ButtonStyle.Secondary, () =>
                    Game.UnlockNextHint(Source, Reopen));
            AddButton(card, Strings.T("hint.ok"), ButtonStyle.Primary, Close);
        }

        void Reopen()
        {
            var game = Game;
            var source = Source;
            Close();
            App.Nav.Show<HintPopup>(p => { p.Game = game; p.Source = source; });
        }
    }

    public class RewardOfferPopup : Popup
    {
        public Action OnReward;

        protected override void BuildCard(RectTransform card)
        {
            Title(card, Strings.T("reward.title"));
            Body(card, Strings.T("reward.body"));
            AddButton(card, Strings.T("reward.watch"), ButtonStyle.Primary, Watch);
            AddButton(card, Strings.T("reward.packs"), ButtonStyle.Secondary, () => App.Nav.Show<ShopScreen>(s => s.Source = "reward_offer"));
            AddButton(card, Strings.T("reward.no"), ButtonStyle.Ghost, Close);
        }

        void Watch()
        {
            App.Ads.ShowRewarded("hint", earned =>
            {
                if (!earned)
                {
                    App.Toast(Strings.T("reward.unavailable"));
                    return;
                }
                var reward = OnReward;
                Close();
                App.Toast(Strings.T("reward.granted"));
                reward?.Invoke();
            });
        }
    }

    public class WorldUnlockedPopup : Popup
    {
        public int World;
        protected override bool CloseOnDimTap => false;

        protected override void BuildCard(RectTransform card)
        {
            Title(card, Strings.T("world.unlocked"));
            string name = World >= 1 && World <= GameConfig.WorldNames.Length ? GameConfig.WorldNames[World - 1] : "World " + World;
            var t = F.Text(card, World + ". " + name, Theme.Heading, Theme.WorldAccent[Mathf.Clamp(World - 1, 0, 4)], TextAnchor.MiddleCenter, FontStyle.Bold);
            F.Layout(t, 120);
            string key = "world.new_mechanics." + World;
            string mech = Strings.T(key);
            Body(card, mech == key ? Strings.T("world.new_mechanics.default") : mech);
            AddButton(card, Strings.T("common.continue"), ButtonStyle.Primary, Close);
            App.Audio.Play(Sfx.Win, 0.6f);
        }
    }

    public class ConfirmPopup : Popup
    {
        public string TitleText = "";
        public string BodyText = "";
        public string ConfirmText = "OK";
        public bool Danger = true;
        public Action OnConfirm;

        protected override void BuildCard(RectTransform card)
        {
            Title(card, TitleText);
            if (!string.IsNullOrEmpty(BodyText)) Body(card, BodyText);
            AddButton(card, ConfirmText, Danger ? ButtonStyle.Danger : ButtonStyle.Primary, () =>
            {
                var action = OnConfirm;
                Close();
                action?.Invoke();
            });
            AddButton(card, Strings.T("common.cancel"), ButtonStyle.Secondary, Close);
        }
    }
}
