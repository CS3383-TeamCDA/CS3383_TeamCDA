using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent diagonal divisions over the shared boss health track.</summary>
public sealed class BossBarSegments : MaskableGraphic
{
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        Color32 shade = new Color(0.12f, 0.015f, 0.12f, 0.85f);
        float slant = r.height * 0.65f;
        for (float x = r.xMin + 18f; x + slant + 1f < r.xMax; x += 18f)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, r.yMin), shade, Vector2.zero);
            mesh.AddVert(new Vector3(x + 1f, r.yMin), shade, Vector2.zero);
            mesh.AddVert(new Vector3(x + slant + 1f, r.yMax), shade, Vector2.zero);
            mesh.AddVert(new Vector3(x + slant, r.yMax), shade, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
