# UpgradeSegmentBarDocumentation

**Project:** Escape the 90s (Unity / C# PC build)
**Feature:** TL5 – Meta Progression & Persistent Upgrade System (shop UI)
**Class:** `UpgradeSegmentBar` (`MonoBehaviour`)
**Scope of this deliverable:** a segmented level bar for one upgrade, where the + button buys the next level immediately.

---

## 1. Description

Shows an upgrade's level as a row of segments. Each segment is an `Image`; the first *level* segments are enabled and the rest are hidden. Pressing + calls `MetaProgressionManager.PurchaseUpgrade(type)` and then refreshes the bar, so a segment appears only if the purchase succeeded (enough currency, below max level).

### Members

| Member | Purpose |
| --- | --- |
| `UpgradeType type` | Which upgrade this bar displays (set in the Inspector). |
| `Button plusButton` | The + button for this upgrade. |
| `Image[] segments` | The segment images, left to right. Their count should equal the upgrade's `MaxLevel`. |
| `void Refresh()` | Reads `GetUpgradeLevel(type)` and enables that many segments. |

The component subscribes to the button in `OnEnable`, unsubscribes in `OnDisable`, and refreshes on enable so the bar is correct when the shop opens.

---

## 2. Setup

1. Cut the supplied bar art into two sprites: the frame and a single segment.
2. Under `UpgradeMenu`, create a `HealthBar` object between the - and + buttons with an `Image` using the frame sprite.
3. Add a child `Segments` object with a **Horizontal Layout Group** (Control Child Size and Child Force Expand on width and height, small spacing, padding to sit inside the frame).
4. Add one child `Image` per level using the segment sprite (duplicate with Ctrl+D).
5. Add `UpgradeSegmentBar` to `HealthBar`. Set `type`, drag in the + button, and drag the segment images into `segments` (lock the Inspector to select all at once).
6. Repeat for Speed, Damage (Attack) and Mag Range.

---

## 3. Behaviour

| Situation | Result |
| --- | --- |
| Shop opens | Bar shows the saved level. |
| + pressed, purchase succeeds | Currency is deducted, level rises by one, progress is saved, one more segment appears. |
| + pressed, not enough currency or max level | No change; bar unchanged. |
| `MetaProgressionManager.Instance` is null | Bar shows empty; + does nothing. |
| More levels than segments | Extra levels are not shown. |

---

## 4. Notes and Limits
- **`Instance` must be set.** `MetaProgressionManager.Instance` is never assigned in the current class, so the bar will stay empty until it is.
- **Segment count** must be set by hand to match each upgrade's `MaxLevel`.
- **Not run in Unity:** this code has not been compiled or tested in this environment.