using System.Collections.Generic;

namespace EchoShift.Game
{
    /// <summary>
    /// Every UI string in one table, keyed for translation later (decision D9).
    /// Level names and hints live in the level files.
    /// </summary>
    public static class Strings
    {
        static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "menu.continue", "Continue" },
            { "menu.play", "Play" },
            { "menu.howto", "How to Play" },
            { "menu.daily", "Free daily hint" },
            { "menu.quit.title", "Quit game?" },
            { "menu.quit.body", "Your progress is saved." },
            { "menu.quit.confirm", "Quit" },

            { "common.back", "Back" },
            { "common.cancel", "Cancel" },
            { "common.close", "Close" },
            { "common.continue", "Continue" },
            { "common.locked", "Locked" },
            { "common.on", "On" },
            { "common.off", "Off" },

            { "world.title", "Worlds" },
            { "world.level_count", "{0} levels" },
            { "world.stars", "{0} / {1} stars" },
            { "world.coming", "Coming soon" },
            { "world.locked_toast", "Beat the previous boss to unlock." },
            { "world.coming_toast", "New world coming in an update." },
            { "world.unlocked", "World unlocked" },
            { "world.new_mechanics.2", "New: bridges, moving platforms, wind, timed switches and crumbling floor." },
            { "world.new_mechanics.default", "New mechanics await." },

            { "levels.locked_toast", "Finish the previous level first." },

            { "game.move", "Move {0}/{1}" },
            { "game.echoes", "Echoes {0}/{1}" },
            { "game.undo", "Undo" },
            { "game.wait", "Wait" },
            { "game.echo", "Echo" },
            { "game.limit_toast", "No moves left. Tap Echo or Undo." },
            { "game.echo_full_toast", "No echoes left. Restart to clear them." },
            { "game.echo_empty_toast", "Move first, then tap Echo." },
            { "game.level", "Level {0}" },

            { "pause.title", "Paused" },
            { "pause.resume", "Resume" },
            { "pause.restart", "Restart" },
            { "pause.hint", "Hint" },
            { "pause.settings", "Settings" },
            { "pause.menu", "Main Menu" },
            { "pause.leave.title", "Leave this level?" },
            { "pause.leave.body", "Progress in this level is lost." },
            { "pause.leave.confirm", "Leave" },

            { "complete.title", "Level complete" },
            { "complete.moves", "{0} moves" },
            { "complete.echoes", "Echoes {0} (par {1})" },
            { "complete.crystal_yes", "Crystal collected" },
            { "complete.crystal_no", "Crystal missed" },
            { "complete.next", "Next" },
            { "complete.replay", "Replay" },
            { "complete.levels", "Levels" },
            { "complete.bonus", "Watch ad: +1 hint" },
            { "complete.end_toast", "New levels arrive every month!" },

            { "retry.title.spikes", "Ouch, spikes!" },
            { "retry.title.fell", "You fell!" },
            { "retry.undo", "Undo last move" },
            { "retry.retry", "Retry" },

            { "hint.title", "Hint {0} of 3" },
            { "hint.left", "Hints left: {0}" },
            { "hint.next", "Next hint" },
            { "hint.ok", "Got it" },
            { "hint.solution", "Solution" },
            { "hint.run", "Run {0}: {1}" },
            { "hint.then_echo", "then tap Echo" },

            { "reward.title", "Need a hint?" },
            { "reward.body", "Watch a short ad to get one hint." },
            { "reward.watch", "Watch ad" },
            { "reward.packs", "Get hint packs" },
            { "reward.no", "No thanks" },
            { "reward.unavailable", "Ad not available. Try again in a moment." },
            { "reward.granted", "+1 hint" },

            { "shop.title", "Shop" },
            { "shop.remove_ads", "Remove ads" },
            { "shop.remove_ads.desc", "No more ads between levels. Rewarded ads stay optional." },
            { "shop.hints10", "10 hints" },
            { "shop.hints30", "30 hints" },
            { "shop.best", "Best value" },
            { "shop.owned", "Owned" },
            { "shop.hints_owned", "You have {0} hints" },
            { "shop.restore", "Restore purchases" },
            { "shop.ads_removed", "Ads removed" },
            { "shop.hints_added", "+{0} hints" },
            { "shop.restored", "Purchases restored" },
            { "shop.failed", "Purchase didn't go through." },
            { "shop.offline", "The store isn't available. Check your connection." },

            { "settings.title", "Settings" },
            { "settings.music", "Music" },
            { "settings.sfx", "Sound effects" },
            { "settings.vibration", "Vibration" },
            { "settings.reduce_motion", "Reduce motion" },
            { "settings.controls", "Move with" },
            { "settings.controls.swipe", "Swipe" },
            { "settings.controls.dpad", "D-pad" },
            { "settings.button_size", "Button size" },
            { "settings.size.0", "Small" },
            { "settings.size.1", "Medium" },
            { "settings.size.2", "Large" },
            { "settings.hand", "Buttons on" },
            { "settings.hand.right", "Right" },
            { "settings.hand.left", "Left" },
            { "settings.language", "Language" },
            { "settings.language.value", "English" },
            { "settings.language.toast", "More languages are coming." },
            { "settings.privacy", "Privacy options" },
            { "settings.privacy.none", "No privacy options are needed in your region." },
            { "settings.about", "About & legal" },
            { "settings.reset", "Reset progress" },
            { "settings.reset.title", "Reset progress?" },
            { "settings.reset.body", "Levels and stars are wiped. Purchases are kept." },
            { "settings.reset.confirm", "Reset" },
            { "settings.reset.done", "Progress reset" },

            { "howto.title", "How to Play" },
            { "howto.1.title", "Move" },
            { "howto.1.body", "Swipe anywhere on the board to move one tile. Nothing moves until you do." },
            { "howto.2.title", "Make an echo" },
            { "howto.2.body", "Every move is recorded. Tap Echo and your past self replays those moves while you start again." },
            { "howto.3.title", "Work together" },
            { "howto.3.body", "Echoes press plates and bump switches. They pass through you and ignore spikes." },
            { "howto.4.title", "Wait and Undo" },
            { "howto.4.body", "Wait lets a turn pass so your echo can catch up. Undo steps back one move, free." },
            { "howto.5.title", "Devices" },
            { "howto.5.body", "Plates, switches and doors with the same colour are linked. Reach the open exit to win." },
            { "howto.next", "Next" },
            { "howto.prev", "Previous" },

            { "credits.title", "About" },
            { "credits.made_by", "Made by {0}" },
            { "credits.privacy", "Privacy policy" },
            { "credits.terms", "Terms" },
            { "credits.fonts", "Type: Unity default font. Icons and art: original." },
            { "credits.version", "Version {0}" },

            { "coach.l1.a", "Swipe to move" },
            { "coach.l1.b", "Grab the crystal, then reach the exit" },
            { "coach.l2.a", "Step on the plate to open the door" },
            { "coach.l2.b", "Doors stay open while you stand in them" },
            { "coach.l3.a", "Walk onto the plate, then tap Echo" },
            { "coach.l3.b", "Your echo repeats your moves. Go!" },

            { "toast.offline", "No internet" },
            { "toast.daily", "+1 free hint" },
        };

        public static string T(string key)
        {
            return En.TryGetValue(key, out var s) ? s : key;
        }

        public static string T(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }
    }
}
