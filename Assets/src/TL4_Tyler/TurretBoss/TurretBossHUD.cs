using UnityEngine;
using UnityEngine.UI;

/// <summary>Encounter-owned overlay; bind after Begin and destroy with the encounter.</summary>
public sealed class TurretBossHUD : MonoBehaviour
{
    [SerializeField, Min(0f)] private float trailDelay = 0.45f;
    [SerializeField, Min(0.01f)] private float trailSpeed = 0.35f;
    private TurretBossController boss;
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

    public void Bind(TurretBossController encounter)
    {
        boss = encounter;
        defeated = false;
        if (canvas == null) { Build(); }
        previousRatio = boss != null && boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.CurrentHealth / boss.MaxHealth) : 0f;
        trailRatio = previousRatio;
        delay = 0f;
        Refresh();
    }

    private void Update()
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
        fill.color = boss.CoreExposed ? new Color(0.3f, 1f, 0.85f) : new Color(0.62f, 0.25f, 0.85f);
        marker.enabled = !boss.IsMobilePhase && !defeated;
        health.text = boss.CurrentHealth + " / " + boss.MaxHealth;
        phase.text = defeated ? "CORE DESTROYED" :
            boss.State == TurretBossAttackState.Transforming ? "TRANSFORMING" :
            (boss.IsMobilePhase ? "PHASE II" : "PHASE I") + (boss.CoreExposed ? "  /  CORE EXPOSED" : "  /  ARMORED");
    }

    private void Build()
    {
        GameObject overlay = new GameObject("Siege Core HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        overlay.transform.SetParent(transform, false);
        canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform panel = Rect("Boss panel", overlay.transform, new Vector2(0.18f, 0f), new Vector2(0.82f, 0f));
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 28f);
        panel.sizeDelta = new Vector2(0f, 82f);
        Label("SIEGE CORE", panel, new Vector2(0f, 0.61f), Vector2.one, 23, TextAnchor.MiddleLeft);
        phase = Label("", panel, new Vector2(0.38f, 0.61f), Vector2.one, 14, TextAnchor.MiddleRight);
        Image border = Box("Bar frame", panel, Vector2.zero, new Vector2(1f, 0.54f), new Color(0.65f, 0.55f, 0.8f));
        RectTransform track = Rect("Health track", border.transform, Vector2.zero, Vector2.one);
        track.offsetMin = new Vector2(3f, 3f);
        track.offsetMax = new Vector2(-3f, -3f);
        Box("Empty health", track, Vector2.zero, Vector2.one, new Color(0.055f, 0.035f, 0.09f));
        trail = Box("Damage trail", track, Vector2.zero, Vector2.one, new Color(1f, 0.65f, 0.25f));
        fill = Box("Current health", track, Vector2.zero, Vector2.one, Color.magenta);
        marker = Box("Phase two threshold", track, new Vector2(TurretBossController.MobilePhaseThreshold, 0f),
            new Vector2(TurretBossController.MobilePhaseThreshold, 1f), Color.white);
        marker.rectTransform.sizeDelta = new Vector2(2f, 0f);
        health = Label("", track, Vector2.zero, Vector2.one, 16, TextAnchor.MiddleCenter);
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
