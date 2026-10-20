using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeyHeyThere.PixelUISciFiFree
{
    /// <summary>
    /// The game pieces of the Pixel UI kits, for the demo's menus and play screens: menu lines, save
    /// slots, switches, steppers, key prompts, item slots, toasts, portraits and a quick-pick wheel, and
    /// the buttons that pick a screen. A theme without them (the free sampler's) has the first screen alone.
    /// </summary>
    public partial class UIDemo
    {
        string page;

        /// <summary>The screen to build: the one picked, or the first.</summary>
        string Page(string[] screens) => theme.Has("KeyCap") && Array.IndexOf(screens, page) >= 0 ? page : screens[0];

        /// <summary>A toggle button per screen, at the screen's right edge and `y` down, the one shown pressed.</summary>
        void Screens(float y, float gap, string[] screens, string[] icons = null)
        {
            if (!theme.Has("KeyCap"))
                return;
            var row = Row(screen, gap);
            Fit(row);
            row.anchorMin = row.anchorMax = row.pivot = Vector2.one;
            row.anchoredPosition = new Vector2(-6, -y);
            for (int i = 0; i < screens.Length; i++)
            {
                var name = screens[i];
                var b = Button(row, name, icons?[i]);
                b.on = name == Page(screens);
                b.onClick.AddListener(() =>
                {
                    page = name;
                    Show(theme);
                });
            }
        }

        /// <summary>Of the buttons, the one clicked stays pressed (a ButtonGroup).</summary>
        static void OneOf(params PixelButton[] buttons)
        {
            foreach (var b in buttons)
                b.onClick.AddListener(() =>
                {
                    foreach (var other in buttons)
                        other.on = other == b;
                });
        }

        /// <summary>A line of a menu: the band and pointer show on the one picked.</summary>
        PixelButton MenuLine(Transform parent, string text, bool on)
        {
            var b = Make("MenuItem", parent).GetComponent<PixelButton>();
            b.label.alignment = TextAlignmentOptions.TopLeft;
            b.text = text;
            b.on = on;
            return b;
        }

        PixelButton SaveSlot(Transform parent, string icon, string text, bool on)
        {
            var b = Make("SaveSlot", parent).GetComponent<PixelButton>();
            b.label.alignment = TextAlignmentOptions.TopLeft;
            b.text = text;
            b.SetIcon(Sprite(icons, icon));
            b.on = on;
            return b;
        }

        /// <summary>A toggle switch, its text on the left and the switch at the right end.</summary>
        Toggle Switch(Transform parent, string text, bool on)
        {
            var t = Make("Switch", parent).GetComponent<Toggle>();
            t.transform.Find("Label").GetComponent<TMP_Text>().text = text;
            t.isOn = on;
            return t;
        }

        /// <summary>A label this wide, then &lt; value &gt; stepping through the options.</summary>
        void Stepper(Transform parent, string text, string[] options, int index, float labelWidth)
        {
            var row = Row(parent, 2);
            MinSize(Label(row, text), labelWidth);
            var left = Make("StepLeft", row).GetComponent<PixelButton>();
            left.text = "";
            var value = Label(row, options[index]);
            value.Text.alignment = TextAlignmentOptions.Top;
            Expand(value);
            var right = Make("StepRight", row).GetComponent<PixelButton>();
            right.text = "";
            void Step(int by)
            {
                index = (index + by + options.Length) % options.Length;
                value.text = options[index];
            }
            left.onClick.AddListener(() => Step(-1));
            right.onClick.AddListener(() => Step(1));
        }

        /// <summary>Keys on their caps, then what they do ("" for nothing).</summary>
        RectTransform Prompt(Transform parent, string text, params string[] keys)
        {
            var row = Row(parent, 2);
            foreach (var k in keys)
                Label(row, k, "KeyCap");
            if (text != "")
                Label(row, text);
            return row;
        }

        /// <summary>A 24x24 item slot (ItemSlotSelected when picked) with the item's icon and its count above 1.</summary>
        RectTransform ItemSlot(Transform parent, string item, int count = 1, bool picked = false)
        {
            var slot = Make(picked ? "ItemSlotSelected" : "ItemSlot", parent);
            if (item == "" || !Has(item))
                return slot;
            Tip(Icon(slot, item), Title(item));
            if (count > 1)
                Corner(slot, count.ToString(), CountAt, true);
            return slot;
        }

        /// <summary>A Count over a slot, out of its layout: `at` from the slot's icon's top left to the text's top right (or left).</summary>
        PixelLabel Corner(RectTransform slot, string text, Vector2 at, bool right)
        {
            var pad = slot.GetComponent<VerticalLayoutGroup>().padding;
            var l = Label(slot, text, "Count");
            var r = Free((RectTransform)l.transform);
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(right ? 1 : 0, 1);
            r.anchoredPosition = new Vector2(pad.left + at.x, -pad.top - at.y);
            return l;
        }

        /// <summary>A Panel in the tooltip's look, for an item's card.</summary>
        RectTransform Card()
        {
            var card = Make("Panel", screen);
            var tip = theme.Make("Tooltip", screen);
            var from = tip.GetComponent<Image>();
            var to = card.GetComponent<Image>();
            (to.sprite, to.type, to.color) = (from.sprite, from.type, from.color);
            card.GetComponent<VerticalLayoutGroup>().padding = tip.GetComponent<HorizontalLayoutGroup>().padding;
            Destroy(tip.gameObject);
            return card;
        }

        /// <summary>A notice: an icon and a line.</summary>
        RectTransform Toast(Transform parent, string item, string text)
        {
            var toast = Make("ToastPanel", parent);
            var row = Row(toast, 4);
            Icon(row, item);
            Label(row, text);
            return toast;
        }

        /// <summary>The icon at twice its size (up to 32x32) in the portrait frame.</summary>
        RectTransform Portrait(Transform parent, string item)
        {
            var frame = Make(theme.Has("PortraitPanel") ? "PortraitPanel" : "InsetPanel", parent);
            var sprite = Sprite(icons, item);
            Icon(frame, item, sprite == null ? 1 : Mathf.Max(1, 32 / (int)Mathf.Max(sprite.rect.width, sprite.rect.height)));
            return Centre(frame);
        }

        /// <summary>A quick-pick wheel: slots round a centre naming the one picked, its key under the name.</summary>
        void Radial(Vector2 centre, float radius, string[] items, int picked, string name, float nameWidth, string key)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var a = -Mathf.PI / 2 + 2 * Mathf.PI * i / items.Length;
                var at = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius - new Vector2(12, 12);
                Place(ItemSlot(screen, items[i], 1, i == picked), Mathf.Round(at.x), Mathf.Round(at.y));
            }
            var label = Label(screen, name);
            label.Text.alignment = TextAlignmentOptions.Top;
            MinSize(label, nameWidth);
            Place((RectTransform)label.transform, centre.x - nameWidth / 2, centre.y - 12);
            Place(Prompt(screen, "", key), centre.x, centre.y + 1).pivot = new Vector2(0.5f, 1);
        }
    }
}
