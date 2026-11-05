using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Standalone demo attack. Uses EnemyBase damage without player combat dependencies.</summary>
public class DeleteExeAttack : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    [SerializeField, Min(0f)] private float cooldown = .35f;
    private float nextAttackTime;
    private float feedbackUntil;
    private Vector3 deletedPosition;
    private GUIStyle labelStyle;

    private void Update()
    {
        if (Time.timeScale <= 0f || Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame || Time.unscaledTime < nextAttackTime) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Camera view = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (view == null) return;
        TryDeleteAt(view, Mouse.current.position.ReadValue());
    }

    public bool TryDeleteAt(Camera view, Vector2 screenPosition)
    {
        if (view == null || Time.unscaledTime < nextAttackTime || !view.pixelRect.Contains(screenPosition)) return false;
        Ray ray = view.ScreenPointToRay(screenPosition);
        EnemyBase selected = null;
        float nearest = float.PositiveInfinity;
        foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            if (enemy.IsDefeated) continue;
            foreach (SpriteRenderer sprite in enemy.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!sprite.enabled || sprite.sprite == null || (view.cullingMask & (1 << sprite.gameObject.layer)) == 0) continue;
                if (sprite.bounds.IntersectRay(ray, out float distance) && distance < nearest)
                {
                    selected = enemy;
                    nearest = distance;
                }
            }
        }
        if (selected == null) return false;
        deletedPosition = selected.transform.position;
        selected.TakeDamage(selected.CurrentHealth);
        nextAttackTime = Time.unscaledTime + cooldown;
        feedbackUntil = Time.unscaledTime + .9f;
        return true;
    }

    private void OnGUI()
    {
        if (labelStyle == null)
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Color original = GUI.color;
        GUI.color = Color.magenta;
        GUI.Label(new Rect(12, Screen.height - 45, 420, 32), "DELETE.EXE  |  Click an enemy to delete", labelStyle);
        Camera view = gameplayCamera != null ? gameplayCamera : Camera.main;
        float remaining = feedbackUntil - Time.unscaledTime;
        if (view != null && remaining > 0f)
        {
            Vector3 point = view.WorldToScreenPoint(deletedPosition + Vector3.up * (1f + (.9f - remaining)));
            if (point.z > 0f)
            {
                GUI.color = new Color(1f, 0f, 1f, Mathf.Clamp01(remaining * 3f));
                GUI.Label(new Rect(point.x - 120, Screen.height - point.y - 16, 240, 32), "[ FILE DELETED ]", labelStyle);
                if (remaining > .7f)
                {
                    GUI.DrawTexture(new Rect(point.x - 35, Screen.height - point.y + 28, 70, 3), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(point.x - 1, Screen.height - point.y - 5, 3, 70), Texture2D.whiteTexture);
                }
            }
        }
        GUI.color = original;
    }
}
