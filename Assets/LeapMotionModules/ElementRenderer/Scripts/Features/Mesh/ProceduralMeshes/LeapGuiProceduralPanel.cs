using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Attributes;

public class LeapGuiProceduralPanel : ProceduralMeshSource {
  public const int MAX_VERTS = 128;

  [SerializeField]
  private ResolutionType _resolutionType = ResolutionType.Vertices;

  [HideInInspector]
  [SerializeField]
  private int _resolution_vert_x, _resolution_vert_y;

  [SerializeField]
  private Vector2 _resolution_verts_per_meter = new Vector2(20, 20);

  [MinValue(0)]
  [SerializeField]
  private Vector2 _size = new Vector2(0.1f, 0.1f);

  [Tooltip("Uses sprite data to generate a nine sliced panel.")]
  [SerializeField]
  private bool _nineSliced = false;

  public ResolutionType resolutionType {
    get {
      return _resolutionType;
    }
  }

  public Rect rect {
    get {
      RectTransform rectTransform = GetComponent<RectTransform>();
      if (rectTransform != null) {
        _size = rectTransform.rect.size;
        return rectTransform.rect;
      } else {
        return new Rect(-_size / 2, _size);
      }
    }
  }

  public bool canNineSlice {
    get {
      var spriteData = GetComponent<LeapGuiElement>().Sprite();
      return spriteData != null && spriteData.sprite != null;
    }
  }

  public void OnValidate() {
    _resolution_vert_x = Mathf.Max(0, _resolution_vert_x);
    _resolution_vert_y = Mathf.Max(0, _resolution_vert_y);
    _resolution_verts_per_meter = Vector2.Max(_resolution_verts_per_meter, Vector2.zero);

    if (_resolutionType == ResolutionType.Vertices) {
      _resolution_verts_per_meter.x = _resolution_vert_x / rect.width;
      _resolution_verts_per_meter.y = _resolution_vert_y / rect.height;
    } else {
      _resolution_vert_x = Mathf.RoundToInt(_resolution_verts_per_meter.x * rect.width);
      _resolution_vert_y = Mathf.RoundToInt(_resolution_verts_per_meter.y * rect.height);
    }
  }

  public override bool TryGenerateMesh(LeapGuiMeshData meshFeature,
                                   out Mesh mesh,
                                   out UVChannelFlags remappableChannels) {
    Vector4 borderSize = Vector4.zero;
    Vector4 borderUvs = Vector4.zero;

    Rect rect;
    RectTransform rectTransform = GetComponent<RectTransform>();
    if (rectTransform != null) {
      rect = rectTransform.rect;
      _size = rect.size;
    } else {
      rect = new Rect(-_size / 2, _size);
    }

    if (_nineSliced) {
      var spriteData = meshFeature.element.Sprite();
      if (spriteData == null || spriteData.sprite == null) {
        mesh = null;
        remappableChannels = 0;
        return false;
      }

      var sprite = spriteData.sprite;

      Vector4 border = sprite.border;
      borderSize = border / sprite.pixelsPerUnit;

      borderUvs = border;
      borderUvs.x /= sprite.textureRect.width;
      borderUvs.z /= sprite.textureRect.width;
      borderUvs.y /= sprite.textureRect.height;
      borderUvs.w /= sprite.textureRect.height;
    }

    List<Vector3> verts = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<int> tris = new List<int>();

    int vertsX, vertsY;
    if (_resolutionType == ResolutionType.Vertices) {
      vertsX = Mathf.RoundToInt(_resolution_vert_x);
      vertsY = Mathf.RoundToInt(_resolution_vert_y);
    } else {
      vertsX = Mathf.RoundToInt(rect.width * _resolution_verts_per_meter.x);
      vertsY = Mathf.RoundToInt(rect.height * _resolution_verts_per_meter.y);
    }

    vertsX += _nineSliced ? 4 : 2;
    vertsY += _nineSliced ? 4 : 2;

    vertsX = Mathf.Min(vertsX, MAX_VERTS);
    vertsY = Mathf.Min(vertsY, MAX_VERTS);

    for (int vy = 0; vy < vertsY; vy++) {
      for (int vx = 0; vx < vertsX; vx++) {
        Vector2 vert;
        vert.x = calculateVertAxis(vx, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(vy, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector2(rect.x, rect.y));

        Vector2 uv;
        uv.x = calculateVertAxis(vx, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(vy, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
    }

    for (int vy = 0; vy < vertsY - 1; vy++) {
      for (int vx = 0; vx < vertsX - 1; vx++) {
        int vertIndex = vy * vertsX + vx;

        tris.Add(vertIndex);
        tris.Add(vertIndex + 1 + vertsX);
        tris.Add(vertIndex + 1);

        tris.Add(vertIndex);
        tris.Add(vertIndex + vertsX);
        tris.Add(vertIndex + 1 + vertsX);
      }
    }

    mesh = new Mesh();
    mesh.name = "Panel Mesh";
    mesh.hideFlags = HideFlags.HideAndDontSave;
    mesh.SetVertices(verts);
    mesh.SetTriangles(tris, 0);
    mesh.SetUVs(0, uvs); //TODO, how to get correct channel??
    mesh.RecalculateBounds();

    remappableChannels = UVChannelFlags.UV0;

    return true;
  }

  private float calculateVertAxis(int dv, int vertCount, float size, float border0, float border1) {
    if (_nineSliced) {
      if (dv == 0) {
        return 0;
      } else if (dv == (vertCount - 1)) {
        return size;
      } else if (dv == 1) {
        return border0;
      } else if (dv == (vertCount - 2)) {
        return size - border1;
      } else {
        return ((dv - 1.0f) / (vertCount - 3.0f)) * (size - border0 - border1) + border0;
      }
    } else {
      return (dv / (vertCount - 1.0f)) * size;
    }
  }

  public enum ResolutionType {
    Vertices,
    VerticesPerMeter
  }
}
