using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

public class LeapGuiBakedRenderer : LeapGuiRenderer {

  [SerializeField]
  private Shader _shader;

  [SerializeField]
  private MotionType _motionType = MotionType.Translation;

  [SerializeField]
  private int borderAmount = 0;

  [Header("Debug")]
  [SerializeField]
  private Mesh _bakedMesh;

  [SerializeField]
  private Material _material;

  [SerializeField]
  private GameObject _displayObject;

  [SerializeField]
  private MeshFilter _filter;

  [SerializeField]
  private MeshRenderer _renderer;

  private List<LeapGuiMeshFeature> _meshFeatures = new List<LeapGuiMeshFeature>();
  private List<LeapGuiTextureFeature> _textureFeatures = new List<LeapGuiTextureFeature>();

  private Dictionary<LeapGuiTextureFeature, Texture2D> _atlases;
  private Dictionary<UVChannelFlags, Rect[]> _atlasedUvs;

  //Tinting
  private const string TINT = LeapGui.PROPERTY_PREFIX + "Tints";
  private List<Color> _tintColors = new List<Color>();

  //Cylindrical space
  private const string CYLINDRICAL_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Cylindrical_ElementParameters";
  private List<Vector4> _cylindrical_elementParameters = new List<Vector4>();

  public override void OnEnableRenderer() {
  }

  public override void OnDisableRenderer() {
  }

  public override void OnUpdateRenderer() {
    if (gui.space is LeapGuiCylindricalSpace) {
      var cylindricalSpace = gui.space as LeapGuiCylindricalSpace;

      _cylindrical_elementParameters.Clear();
      foreach (var element in gui.elements) {
        Vector4 guiPos = gui.transform.InverseTransformPoint(element.transform.position);

        var parameters = cylindricalSpace.GetElementParameters(element.anchor, element.transform.position);

        _cylindrical_elementParameters.Add(parameters);
      }

      _material.SetFloat(LeapGuiCylindricalSpace.RADIUS_PROPERTY, cylindricalSpace.radius);
      _material.SetVectorArray(CYLINDRICAL_PARAMETERS, _cylindrical_elementParameters);
    }
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

    if (DoesNeedUv3()) {
      bakeUv3();
    }

    switch (_motionType) {
      case MotionType.Translation:
        _material.EnableKeyword(LeapGui.FEATURE_MOVEMENT_TRANSLATION);
        break;
      case MotionType.Full:
        _material.EnableKeyword(LeapGui.FEATURE_MOVEMENT_FULL);
        break;
    }

    if (gui.space is LeapGuiCylindricalSpace) {
      _material.EnableKeyword(LeapGuiCylindricalSpace.FEATURE_NAME);
    }

    OnUpdateRenderer();
  }

  private void ensureObjectsAreValid() {
    if (_displayObject == null) {
      _displayObject = new GameObject("Baked Gui Mesh");
      _displayObject.transform.SetParent(gui.transform);
      _displayObject.transform.SetSiblingIndex(0);
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

    if (_material != null) {
      DestroyImmediate(_material);
    }

    _material = new Material(_shader);
    _material.name = "Baked Gui Material";

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
          return elementVertToBakedVert(data.element, v);
        }).AppendList(verts);
      }
    }

    _bakedMesh.SetVertices(verts);
    _bakedMesh.SetTriangles(tris, 0);
  }

  private void bakeColors() {
    //If no mesh feature wants colors, don't bake them!
    if (!_meshFeatures.Query().Any(f => f.color)) {
      return;
    }

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
    _material.EnableKeyword(LeapGuiMeshFeature.COLORS_FEATURE);
  }

  private void atlasTextures() {
    _atlases = new Dictionary<LeapGuiTextureFeature, Texture2D>();
    _atlasedUvs = new Dictionary<UVChannelFlags, Rect[]>();

    var whiteTexture = new Texture2D(3, 3, TextureFormat.ARGB32, mipmap: false);
    whiteTexture.SetPixels(new Color[3 * 3].Fill(Color.white));
    whiteTexture.Apply();

    Texture2D[] textures = new Texture2D[gui.elements.Count];
    var _originalToBordered = new Dictionary<Texture2D, Texture2D>();

    foreach (var uvChannel in allUvChannels) {
      var feature = _textureFeatures.Query().FirstOrDefault(f => f.channel == uvChannel);
      if (feature != null) {

        //Do atlasing
        feature.data.Query().
             Select(d => {
               if (d.texture == null) {
                 return whiteTexture;
               }

               Texture2D bordered;
               if (!_originalToBordered.TryGetValue(d.texture, out bordered)) {
                 d.texture.EnsureReadWriteEnabled();
                 bordered = Instantiate(d.texture);
                 bordered.AddBorder(borderAmount);
                 _originalToBordered[d.texture] = bordered;
               }

               return bordered;
             }).
             FillArray(textures);

        var atlas = new Texture2D(1, 1, TextureFormat.ARGB32, mipmap: true);
        var atlasedUvs = atlas.PackTextures(textures, 1);

        //Correct uvs to account for the added border
        for (int i = 0; i < atlasedUvs.Length; i++) {
          float dx = 1.0f / atlas.width;
          float dy = 1.0f / atlas.height;
          Rect r = atlasedUvs[i];

          if (textures[i] != whiteTexture) {
            dx *= borderAmount;
            dy *= borderAmount;
          }

          r.x += dx;
          r.y += dy;
          r.width -= dx * 2;
          r.height -= dy * 2;
          atlasedUvs[i] = r;
        }

        _atlases[feature] = atlas;
        _atlasedUvs[uvChannel] = atlasedUvs;
      }
    }

    foreach (var feature in _textureFeatures) {
      _material.SetTexture(feature.propertyName, _atlases[feature]);
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
      for (int elementIndex = 0; elementIndex < feature.data.Count; elementIndex++) {
        var data = feature.data[elementIndex];
        var mesh = data.mesh;
        if (mesh == null) {
          continue;
        }

        foreach (var pair in uvs) {
          mesh.GetUVs(GetUvChannelIndex(pair.Key), tempUvList);

          if (tempUvList.Count != mesh.vertexCount) {
            tempUvList.Fill(mesh.vertexCount, Vector4.zero);
          }

          Rect[] atlasedUvs;
          if (_atlasedUvs != null && _atlasedUvs.TryGetValue(pair.Key, out atlasedUvs)) {
            Rect elementRect = atlasedUvs[elementIndex];
            tempUvList.Query().Select(uv => new Vector4(elementRect.x + uv.x * elementRect.width,
                                                        elementRect.y + uv.y * elementRect.height,
                                                        uv.z,
                                                        uv.w)).
                               AppendList(uvs[pair.Key]);
          } else {
            uvs[pair.Key].AddRange(tempUvList);
          }
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

      _material.EnableKeyword(LeapGuiMeshFeature.GetUvFeature(pair.Key));
    }
  }

  private void bakeUv3() {
    List<Vector4> uv3 = new List<Vector4>();

    foreach (var feature in _meshFeatures) {
      for (int i = 0; i < feature.data.Count; i++) {
        var mesh = feature.data[i].mesh;

        //TODO, support a single blend shape
        uv3.Append(mesh.vertexCount, new Vector4(0, 0, 0, i));
      }
    }

    _bakedMesh.SetUVs(3, uv3);
  }

  /// <summary>
  /// Returns whether or not this baker will need to use uv3 to store additional
  /// information like element id or blend shape vertex offset
  /// </summary>
  public bool DoesNeedUv3() {
    if (_motionType != MotionType.None) return true;
    //TODO: add more conditions!
    return false;
  }

  private Vector3 elementVertToBakedVert(LeapGuiElement element, Vector3 vert) {
    switch (_motionType) {
      case MotionType.None:
        return gui.space.TransformPoint(vert);
      case MotionType.Translation:
        Vector3 worldVert = element.transform.TransformPoint(vert);
        Vector3 guiVert = gui.transform.InverseTransformPoint(worldVert);
        return guiVert - gui.transform.InverseTransformPoint(element.transform.position);
    }

    throw new NotImplementedException();
  }

  public enum MotionType {
    None,
    Translation,
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
