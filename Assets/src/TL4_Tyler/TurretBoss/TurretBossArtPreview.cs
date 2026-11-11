using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Isolated art review scene; intentionally independent of the combat prefab.</summary>
public sealed class TurretBossArtPreview : MonoBehaviour
{
    [SerializeField] private TurretBossVisualRig visualPrefab;
    private TurretBossVisualRig planted;
    private TurretBossVisualRig mobile;
    private bool exposed;
    private bool mirrored;
    private Sprite groundSprite;
    [SerializeField, Min(0.1f)] private float previewWalkSpeed = 0.65f;

    private void Start()
    {
        if (visualPrefab == null) { return; }
        planted = Instantiate(visualPrefab, new Vector3(-4.5f, 0f, 0f), Quaternion.identity, transform);
        planted.SetForm(false);
        mobile = Instantiate(visualPrefab, new Vector3(4f, 0f, 0f), Quaternion.identity, transform);
        mobile.SetForm(false);
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject view = new GameObject("Art preview camera");
            view.transform.SetParent(transform);
            view.tag = "MainCamera";
            camera = view.AddComponent<Camera>();
        }
        camera.transform.position = new Vector3(0f, 2.1f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.backgroundColor = new Color(0.025f, 0.03f, 0.07f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        groundSprite = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        GameObject ground = new GameObject("Foot contact baseline");
        ground.transform.SetParent(transform, false);
        ground.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        ground.transform.localScale = new Vector3(24f, 0.035f, 1f);
        SpriteRenderer line = ground.AddComponent<SpriteRenderer>();
        line.sprite = groundSprite;
        line.color = new Color(0.2f, 0.26f, 0.36f);
        planted.SetExposed(false);
        mobile.SetExposed(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || planted == null || mobile == null) { return; }
        float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
            (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        Vector3 position = mobile.transform.localPosition;
        if (mobile.IsMobile && !mobile.IsTransforming)
        {
            position.x = Mathf.Clamp(position.x + horizontal * previewWalkSpeed * Time.deltaTime, 2f, 6f);
        }
        mobile.transform.localPosition = position;
        if (keyboard.tKey.wasPressedThisFrame) { mobile.TransitionToForm(!mobile.IsMobile); }
        if (keyboard.bKey.wasPressedThisFrame)
        {
            planted.PlayCannonAttack();
            mobile.PlayCannonAttack();
        }
        if (keyboard.lKey.wasPressedThisFrame)
        {
            planted.PlayLauncherShot();
            mobile.PlayLauncherShot();
        }
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            exposed = !exposed;
            planted.AnimateCore(exposed);
            mobile.AnimateCore(exposed);
        }
        if (keyboard.fKey.wasPressedThisFrame)
        {
            mirrored = !mirrored;
            planted.transform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
            mobile.transform.localScale = planted.transform.localScale;
        }
    }

    private void OnDestroy()
    {
        if (groundSprite != null) { Destroy(groundSprite); }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20f, 20f, 1000f, 30f), "SIEGE CORE ART PREVIEW | Left: reference | Right: transform preview | A/D: walk | T: transform right boss | Space: shutters | F: mirror");
        GUI.Label(new Rect(20f, 50f, 1000f, 30f), "B: cannon charge/fire | L: launcher shot (independent) | Space: slide shutters | T: deploy/return");
    }
}
