using UnityEngine;
using UnityEngine.UI;

/// <summary>Encounter-owned overlay; bind after Begin and destroy with the encounter.</summary>
public class BossFightHUD : MonoBehaviour
{
    [SerializeField, Min(0f)] private float trailDelay = 0.45f;
    [SerializeField, Min(0.01f)] private float trailSpeed = 0.35f;
    private EnemyBase boss;
    private string bossName;
    private Sprite portraitSprite;
    private Image portraitImage;
    private Image portraitOverlay;
    private System.Func<string> status;
    private Canvas canvas;
    private Image fill;
    private Image trail;
    private Image marker;
    private Text phase;
    private Text health;
    private float previousRatio;
    private float trailRatio;
    private float delay;
    private bool defeated;

    public void Bind(EnemyBase encounter, string displayName, Sprite portrait, System.Func<string> statusText = null)
    {
        boss = encounter;
        bossName = displayName;
        portraitSprite = portrait;
        status = statusText;
        defeated = false;
        if (canvas == null) { Build(); }
        previousRatio = boss != null && boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.CurrentHealth / boss.MaxHealth) : 0f;
        trailRatio = previousRatio;
        delay = 0f;
        Refresh();
    }

    public void SetPortraitOverlay(Sprite sprite)
    {
        if (portraitImage == null || sprite == null) { return; }
        // Fit the assembled turret head (dome plus barrel) inside the portrait frame.
        portraitImage.rectTransform.anchorMin = new Vector2(0.02f, 0.17f);
        portraitImage.rectTransform.anchorMax = new Vector2(0.66f, 0.88f);
        if (portraitOverlay == null)
        {
            portraitOverlay = Box("Portrait cannon", portraitImage.transform.parent,
                new Vector2(0.38f, 0.29f), new Vector2(0.98f, 0.7f), Color.white);
            portraitOverlay.preserveAspect = true;
        }
        portraitOverlay.sprite = sprite;
    }

    protected virtual void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (canvas == null) { return; }
        // Retain the empty victory bar if the defeated wreckage is later destroyed.
        if (boss == null)
        {
            canvas.enabled = defeated;
            return;
        }
        canvas.enabled = true;
        defeated = boss.IsDefeated;
        float ratio = boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.CurrentHealth / boss.MaxHealth) : 0f;
        if (ratio < previousRatio) { delay = trailDelay; }
        if (ratio > previousRatio) { trailRatio = ratio; }
        previousRatio = ratio;
        if (delay > 0f) { delay = Mathf.Max(0f, delay - Time.deltaTime); }
        else { trailRatio = Mathf.MoveTowards(trailRatio, ratio, trailSpeed * Time.deltaTime); }
        fill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        trail.rectTransform.anchorMax = new Vector2(trailRatio, 1f);
        TurretBossController turret = boss as TurretBossController;
        fill.color = turret != null && turret.CoreExposed ? new Color(0.3f, 1f, 0.85f) : new Color(1f, 0.08f, 0.38f);
        marker.enabled = false;
        health.text = boss.CurrentHealth + " / " + boss.MaxHealth;
        phase.text = defeated ? "DEFEATED" : status != null ? status() : "BOSS";
    }

    private void Build()
    {
        GameObject overlay = new GameObject("Boss Fight HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        overlay.transform.SetParent(transform, false);
        canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform panel = Rect("Boss panel", overlay.transform, new Vector2(0.38f, 0f), new Vector2(0.97f, 0f));
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 23f);
        panel.sizeDelta = new Vector2(0f, 66f);
        Color pink = new Color(1f, 0.08f, 0.38f);
        Image portraitFrame = Box("Boss portrait frame", panel, Vector2.zero, new Vector2(0f, 1f), pink);
        portraitFrame.rectTransform.sizeDelta = new Vector2(66f, 0f);
        portraitFrame.rectTransform.pivot = new Vector2(0f, 0.5f);
        RectTransform portraitInner = Rect("Portrait inset", portraitFrame.transform, Vector2.zero, Vector2.one);
        portraitInner.offsetMin = new Vector2(3f, 3f);
        portraitInner.offsetMax = new Vector2(-3f, -3f);
        Box("Portrait background", portraitInner, Vector2.zero, Vector2.one, new Color(0.04f, 0.015f, 0.08f, 0.95f));
        Image portrait = Box("Boss headshot", portraitInner, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Color.white);
        portraitImage = portrait;
        portrait.sprite = portraitSprite;
        portrait.preserveAspect = true;
        portrait.enabled = portraitSprite != null;
        RectTransform bar = Rect("Boss bar", panel, Vector2.zero, Vector2.one);
        bar.offsetMin = new Vector2(74f, 0f);
        Text title = Label(bossName.ToUpperInvariant(), bar, new Vector2(0f, 0.66f), Vector2.one, 17, TextAnchor.MiddleLeft);
        title.color = pink;
        title.fontStyle = FontStyle.Bold;
        phase = Label("", bar, new Vector2(0.5f, 0.66f), Vector2.one, 11, TextAnchor.MiddleRight);
        phase.color = pink;
        Image border = Box("Bar frame", bar, new Vector2(0f, 0.16f), new Vector2(1f, 0.65f), pink);
        RectTransform track = Rect("Health track", border.transform, Vector2.zero, Vector2.one);
        track.offsetMin = new Vector2(3f, 3f);
        track.offsetMax = new Vector2(-3f, -3f);
        Box("Empty health", track, Vector2.zero, Vector2.one, new Color(0.04f, 0.015f, 0.08f, 0.95f));
        trail = Box("Damage trail", track, Vector2.zero, Vector2.one, new Color(1f, 0.5f, 0.6f));
        fill = Box("Current health", track, Vector2.zero, Vector2.one, pink);
        Rect("Diagonal segments", track, Vector2.zero, Vector2.one).gameObject.AddComponent<BossBarSegments>();
        marker = Box("Phase two threshold", track, Vector2.zero, Vector2.zero, Color.clear);
        health = Label("", bar, Vector2.zero, new Vector2(1f, 0.16f), 10, TextAnchor.MiddleRight);
        Box("Lower neon trim", bar, new Vector2(0f, 0.06f), new Vector2(0.8f, 0.08f), pink);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static Image Box(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        Image image = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(string value, Transform parent, Vector2 min, Vector2 max, int size, TextAnchor alignment)
    {
        Text text = Rect("Label", parent, min, max).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
