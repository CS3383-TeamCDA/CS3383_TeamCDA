using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeyHeyThere.PixelUISciFiFree
{
    /// <summary>
    /// The demo's screens over a starfield: the bridge (a ship's HUD, a systems window, a cargo hold, a
    /// target lock and a comms line), the menus (pause, settings with switches and steppers, key
    /// bindings, save slots, a confirm box) and play (a hold and its item card, toasts, a quick-pick
    /// wheel, a hotbar, key prompts, a comms line).
    /// </summary>
    public partial class UIDemo
    {
        static readonly Color Background = new Color(0.02f, 0.03f, 0.06f);
        static readonly string[] Pages = { "Bridge", "Menus", "Play" };
        static readonly Vector2 CountAt = new Vector2(19, 10);

        /// <summary>Stars and a planet behind the UI, so the panels show they let a little through.</summary>
        static Texture2D Backdrop()
        {
            var t = Paper(428, 241, out var rect);
            var rng = new System.Random(7);
            for (int i = 0; i < 140; i++)
            {
                int x = rng.Next(0, 428), y = rng.Next(0, 241);
                var v = 0.25f + 0.75f * (float)rng.NextDouble();
                rect(x, y, 1, 1, new Color(v, v, Mathf.Min(1, v * 1.1f)));
            }
            Circle(t, 330, 180, 70, new Color(0.09f, 0.1f, 0.2f));
            Circle(t, 318, 170, 58, new Color(0.12f, 0.14f, 0.27f));
            t.Apply();
            return t;
        }

        void Layout()
        {
            switch (Page(Pages))
            {
                case "Menus":
                    Menus();
                    break;
                case "Play":
                    Play();
                    break;
                default:
                    Hud();
                    Systems();
                    Cargo();
                    Target();
                    Comms();
                    break;
            }
            Switcher(6, 222, 2);
            Screens(222, 2, Pages);
        }

        void Hud()
        {
            var rows = Place(Column(screen, 1), 6, 5);
            foreach (var (icon, colour, value) in new[] { ("heart", "Red", 72), ("shield", "Blue", 48), ("energy", "Gold", 90) })
            {
                var r = Row(rows, 2);
                Icon(r, icon);
                Centre((RectTransform)Bar(r, colour, value, 70, 8).transform);
            }
            var money = Place(Row(screen, 1), 290, 5);
            foreach (var (icon, text) in new[] { ("credit", "4096"), ("crystal", "12"), ("fuel", "68%") })
            {
                Icon(money, icon);
                Label(money, text);
                Gap(money, 4);
            }
        }

        void Systems()
        {
            var box = Window("SHIP SYSTEMS", 6, 62, 150);
            Slider(box, "Thrust", 70, 36);
            Slider(box, "Shields", 40, 36);
            Check(box, "Autopilot", true);
            Check(box, "Cloak", false);
            Dropdown(box, 0, "Cruise", "Combat", "Stealth");
            Input(box, "Callsign");
            var buttons = Row(box);
            foreach (var t in new[] { "Abort", "Engage" })
                Expand(Button(buttons, t));
        }

        void Cargo()
        {
            var box = Window("CARGO HOLD", 164, 28, 258);
            var pages = Tabs(box, "Items", "Crew");
            var items = Grid(pages[0], 9, 2);
            var stock = new[] { ("fuel", 3), ("battery", 2), ("crystal", 12), ("chip", 4), ("keycard", 1), ("health", 5),
                ("laser", 1), ("rocket", 2), ("cargo", 6), ("wrench", 1), ("satellite", 0), ("asteroid", 9), ("radar", 0),
                ("robot", 0), ("alien", 0), ("star", 0), ("energy", 0), ("planet", 0) }
                .Where(p => Has(p.Item1)).Take(16).ToArray();
            for (int i = 0; i < 18; i++)
            {
                var slot = Make("InsetPanel", items);
                if (i >= stock.Length)
                    continue;
                var (name, count) = stock[i];
                Tip(Icon(slot, name), Title(name));
                var pad = slot.GetComponent<VerticalLayoutGroup>().padding;
                if (count > 1)
                    Over(slot, count.ToString(), new Vector2(pad.left + 19, pad.top + 10), true, null, Color.black);
            }
            foreach (var (icon, text) in new[] { ("astronaut", "Cmdr. Vega"), ("robot", "Unit K-9") })
            {
                var r = Row(pages[1]);
                Icon(r, icon);
                Expand(Label(r, text));
                Button(r, "Assign");
            }
            var hold = Row(box);
            Icon(hold, "cargo");
            Label(hold, "Hold 60%");
            var fill = Bar(hold, "", 60, 0, 8);
            Expand(fill);
            Centre((RectTransform)fill.transform);
            var actions = Row(box);
            foreach (var (icon, text) in new[] { ("check", "Use"), ("trash", "Jettison"), ("info", "Scan") })
                Expand(Button(actions, text, icon));
        }

        void Target()
        {
            var frame = Place(Make(theme.Has("BracketPanel") ? "BracketPanel" : "Panel", screen), 100, 4);
            MinSize(frame, 56, 52);
            frame.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Icon(frame, "reticle_lock", 1, Sprite(sprites, "reticle_lock"));
            Label(frame, "LOCKED").Text.alignment = TMPro.TextAlignmentOptions.Top;
        }

        void Comms()
        {
            var panel = Place(Make("Panel", screen), 6, 187);
            MinSize(panel, 416);
            var r = Row(panel, 6);
            Icon(Make("InsetPanel", r), "astronaut");
            Expand(Label(r, "Incoming transmission from Outpost 7.\nDock at bay 3 and mind the asteroids."));
            Centre((RectTransform)Button(r, "", "arrow_right").transform);
        }

        void Menus()
        {
            var pause = Window("PAUSED", 6, 6, 84);
            OneOf(new[] { "Resume", "Save", "Load", "Settings", "Controls", "Quit" }.Select(t => MenuLine(pause, t, t == "Settings")).ToArray());
            var quit = Window("ABORT?", 6, 118, 84);
            Label(quit, "Progress since\nthe last save\nwill be lost.");
            var answer = Row(quit);
            foreach (var t in new[] { "Yes", "No" })
                Expand(Button(answer, t));

            var box = Window("SETTINGS", 96, 6, 160);
            Slider(box, "Music", 70, 44);
            Slider(box, "Sound", 40, 44);
            Switch(box, "Vsync", true);
            Switch(box, "Subtitles", false);
            Stepper(box, "Difficulty", new[] { "Easy", "Normal", "Hard" }, 1, 44);
            Stepper(box, "Window", new[] { "Windowed", "Fullscreen", "Borderless" }, 1, 44);
            Make("Separator", box);
            var buttons = Row(box);
            foreach (var t in new[] { "Back", "Apply" })
                Expand(Button(buttons, t));

            var keys = Window("CONTROLS", 262, 6, 159);
            foreach (var (action, caps) in new[] { ("Thrust", new[] { "W", "A", "S", "D" }), ("Fire", new[] { "Space" }), ("Dock", new[] { "E" }), ("Pause", new[] { "Esc" }) })
            {
                var r = Row(keys, 2);
                Expand(Label(r, action));
                Prompt(r, "", caps);
            }

            var rebind = Window("PRESS A KEY", 96, 150, 160);
            Label(rebind, "Press a key for Fire").Text.alignment = TextAlignmentOptions.Top;
            Prompt(rebind, "Cancel", "Esc").GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;

            var saves = Window("LOAD GAME", 262, 100, 159);
            OneOf(SaveSlot(saves, "ship", "Kepler Station\nSector 7  2h 14m", true), SaveSlot(saves, "planet", "Red Moon\nSector 4  1h 02m", false),
                SaveSlot(saves, "plus", "New game\n", false));
        }

        void Play()
        {
            var bag = Window("HOLD", 6, 6, 0);
            var grid = Grid(bag, 6, 2);
            var stock = new[] { ("laser", 1), ("battery", 3), ("shield", 1), ("fuel", 5), ("rocket", 4), ("keycard", 2), ("crystal", 12),
                ("health", 2), ("chip", 30), ("wrench", 1) };
            for (int i = 0; i < 18; i++)
                ItemSlot(grid, i < stock.Length ? stock[i].Item1 : "", i < stock.Length ? stock[i].Item2 : 0, i == 1);

            var card = Place(Card(), 176, 6);
            MinSize(card, 110);
            card.GetComponent<VerticalLayoutGroup>().spacing = 3;
            var top = Row(card, 3);
            Icon(top, "battery");
            Centre((RectTransform)Label(top, "Power cell", "Nameplate").transform);
            Label(card, "Restores 40\nenergy to the\nship's shields.");
            var worth = Row(card, 2);
            Icon(worth, "credit");
            Label(worth, "15");
            var use = Row(card, 6);
            Prompt(use, "Use", "E");
            Prompt(use, "Drop", "Q");

            var toasts = Column(screen, 2);
            Fit(toasts);
            toasts.anchorMin = toasts.anchorMax = toasts.pivot = Vector2.one;
            toasts.anchoredPosition = new Vector2(-6, -6);
            foreach (var (icon, text) in new[] { ("star", "Mission updated"), ("credit", "+50 credits"), ("save", "Game saved") })
                Toast(toasts, icon, text);

            Radial(new Vector2(362, 146), 44, new[] { "laser", "rocket", "shield", "health", "battery", "fuel", "keycard", "crystal" }, 3, "Med pack", 60, "Q");

            var talk = Place(Make("Panel", screen), 6, 112);
            MinSize(talk, 290);
            var r = Row(talk, 6);
            Portrait(r, "astronaut");
            var said = Column(r, 2);
            Expand(said);
            Label(Row(said, 0), "Cmdr. Vega", "Nameplate");
            Label(said, "Outpost 7 has lost power. Bring\nthree power cells to the relay.");
            Prompt(said, "Next", "E").GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperRight;

            var hotbar = Place(Make("Panel", screen), 6, 176);
            var keys = Row(hotbar, 2);
            var items = new[] { "laser", "rocket", "shield", "health", "battery", "keycard", "wrench", "crystal" };
            for (int i = 0; i < items.Length; i++)
                Corner(ItemSlot(keys, items[i], 1, i == 2), (i + 1).ToString(), new Vector2(-2, -3), false);

            var hints = Place(Column(screen, 2), 232, 180);
            Prompt(hints, "Hold", "Tab");
            Prompt(hints, "Menu", "Esc");
        }
    }
}
