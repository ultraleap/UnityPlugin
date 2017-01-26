using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

public class LeapGuiBakedRenderer : LeapGuiRenderer {

  [SerializeField]
  private Shader _shader;

  [SerializeField]
  private MotionType _motionType = MotionType.None;

  [SerializeField]
  private int borderAmount = 0;

  private List<LeapGuiMeshFeature> _meshFeatures = new List<LeapGuiMeshFeature>();
  private List<LeapGuiTextureFeature> _textureFeatures = new List<LeapGuiTextureFeature>();

  private Dictionary<LeapGuiTextureFeature, Texture2D> _atlases = new Dictionary<LeapGuiTextureFeature, Texture2D>();
  private Dictionary<UVChannelFlags, Rect[]> _atlasedUvs = new Dictionary<UVChannelFlags, Rect[]>();

  private Mesh _bakedMesh;
  private Material _material;
  private GameObject _displayObject;
  private MeshFilter _filter;
  private MeshRenderer _renderer;

  public override void OnEnableRenderer() {
  }

  public override void OnDisableRenderer() {
  }

  public override void OnUpdateRenderer() {
  }

  public override void OnEnableRendererEditor() {
    ensureObjectsAreValid();
  }

  public override void OnDisableRendererEditor() {
    DestroyImmediate(_displayObject);
    DestroyImmediate(_bakedMesh);
    DestroyImmediate(_material);

    _bakedMesh = null;
    _material = null;
    _displayObject = null;
    _filter = null;
    _renderer = null;
  }

  public override void OnUpdateRendererEditor() {
    ensureObjectsAreValid();

    _bakedMesh.Clear();

    if (gui.GetAllFeaturesOfType(_meshFeatures)) {
      bakeVerts();
      bakeColors();
    }

    if (gui.GetAllFeaturesOfType(_textureFeatures)) {
      atlasTextures();
    }

    bakeUvs();
  }

  private void ensureObjectsAreValid() {
    if (_displayObject == null) {
      _displayObject = new GameObject("Baked Gui Mesh");
    }

    _displayObject.transform.SetParent(gui.transform);
    _displayObject.transform.localPosition = Vector3.zero;
    _displayObject.transform.localRotation = Quaternion.identity;
    _displayObject.transform.localScale = Vector3.one;

    if (_bakedMesh == null) {
      _bakedMesh = new Mesh();
      _bakedMesh.name = "Baked Gui Mesh";
    }

    if (_shader == null) {
      _shader = Shader.Find("Unlit/LeapGuiShader");
    }

    if (_material == null) {
      _material = new Material(_shader);
      _material.name = "Baked Gui Material";
    }

    if (_filter == null) {
      _filter = _displayObject.AddComponent<MeshFilter>();
    }

    _filter.mesh = _bakedMesh;

    if (_renderer == null) {
      _renderer = _displayObject.AddComponent<MeshRenderer>();
    }

    _renderer.sharedMaterial = _material;
  }

  private void bakeVerts() {
    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();

    foreach (var meshFeature in _meshFeatures) {
      foreach (var data in meshFeature.data) {
        if (data.mesh == null) continue;

        data.mesh.GetIndices(0).Query().Select(i => i + verts.Count).AppendList(tris);

        //TODO: bake out space if no motion is enabled
        data.mesh.vertices.Query().Select(v => {
          return elementVertToBakedVert(data.element.transform, v);
        }).AppendList(verts);
      }
    }

    _bakedMesh.SetVertices(verts);
    _bakedMesh.SetTriangles(tris, 0);
  }

  private void bakeColors() {
    List<Color> colors = new List<Color>();

    foreach (var feature in _meshFeatures) {
      foreach (var data in feature.data) {
        if (data.mesh == null) continue;

        int vertexCount = data.mesh.vertexCount;
        Color totalTint = data.color * feature.tint;

        var vertexColors = data.mesh.colors;
        if (vertexColors.Length != vertexCount) {
          colors.Append(vertexCount, totalTint);
        } else {
          vertexColors.Query().Select(c => c * totalTint).AppendList(colors);
        }
      }
    }

    _bakedMesh.SetColors(colors);
  }

  private void atlasTextures() {
    var whiteTexture = new Texture2D(3, 3, TextureFormat.ARGB32, mipmap: false);
    whiteTexture.SetPixels(new Color[3 * 3].Fill(Color.white));
    whiteTexture.Apply();

    Texture2D[] textures = new Texture2D[gui.elementCount];
    var _originalToBordered = new Dictionary<Texture2D, Texture2D>();

    foreach (var uvChannel in allUvChannels) {
      var feature = _textureFeatures.Query().FirstOrDefault(f => f.channel == uvChannel);
      if (feature != null) {

        //Do atlas
        feature.data.Query().
             Select(d => {
               if (d.texture == null) {
                 return whiteTexture;
               }

               Texture2D bordered;
               if (!_originalToBordered.TryGetValue(d.texture, out bordered)) {
                 bordered = Instantiate(d.texture);
                 bordered.AddBorder(borderAmount);
                 _originalToBordered[d.texture] = bordered;
               }

               return bordered;
             }).
             FillArray(textures);

        var atlas = new Texture2D(1, 1, TextureFormat.ARGB32, mipmap: true);
        var atlasedUvs = atlas.PackTextures(textures, 1);

        //TODO: correct uv rects to account for extra border

        _atlases[feature] = atlas;
        _atlasedUvs[uvChannel] = atlasedUvs;
      }
    }
  }

  private void bakeUvs() {
    var uvs = new Dictionary<UVChannelFlags, List<Vector4>>();

    //Build up uv dictionary 
    foreach (var feature in _meshFeatures) {
      foreach (var enabledChannel in feature.enabledUvChannels) {
        if (!uvs.ContainsKey(enabledChannel)) {
          uvs.Add(enabledChannel, new List<Vector4>());
        }
      }
    }

    //Extract uvs from feature data
    List<Vector4> tempUvList = new List<Vector4>();
    foreach (var feature in _meshFeatures) {
      foreach (var data in feature.data) {
        var mesh = data.mesh;
        foreach (var pair in uvs) {
          mesh.GetUVs(GetUvChannelIndex(pair.Key), tempUvList);

          if (tempUvList.Count != mesh.vertexCount) {
            tempUvList.Fill(mesh.vertexCount, Vector4.zero);
          }

          //TODO: atlas correction of uvs if textures are atlased

          uvs[pair.Key].AddRange(tempUvList);
        }
      }
    }

    //Asign uvs to baked mesh
    foreach (var pair in uvs) {
      if (pair.Value.Query().Any(v => v.w != 0)) {
        _bakedMesh.SetUVs(GetUvChannelIndex(pair.Key), pair.Value);
      } else if (pair.Value.Query().Any(v => v.z != 0)) {
        _bakedMesh.SetUVs(GetUvChannelIndex(pair.Key),
                          pair.Value.Query().
                                     Select(v => (Vector3)v).
                                     ToList());
      } else {
        _bakedMesh.SetUVs(GetUvChannelIndex(pair.Key),
                          pair.Value.Query().
                                     Select(v => (Vector2)v).
                                     ToList());
      }
    }
  }

  private Vector3 elementVertToBakedVert(Transform elementTransform, Vector3 vert) {
    vert = elementTransform.TransformPoint(vert);
    vert = gui.transform.InverseTransformPoint(vert);
    //vert -= gui.transform.InverseTransformPoint(elementTransform.position);
    return vert;
  }

  public enum MotionType {
    None,
    TranslationOnly,
    Full
  }

  private IEnumerable<UVChannelFlags> allUvChannels {
    get {
      yield return UVChannelFlags.UV0;
      yield return UVChannelFlags.UV1;
      yield return UVChannelFlags.UV2;
      yield return UVChannelFlags.UV3;
    }
  }

  private int GetUvChannelIndex(UVChannelFlags channel) {
    switch (channel) {
      case UVChannelFlags.UV0:
        return 0;
      case UVChannelFlags.UV1:
        return 1;
      case UVChannelFlags.UV2:
        return 2;
      case UVChannelFlags.UV3:
        return 3;
    }
    return -1;
  }
}
