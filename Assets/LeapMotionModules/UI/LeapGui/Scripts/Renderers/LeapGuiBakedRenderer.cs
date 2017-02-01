using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public class LeapGuiBakedRenderer : LeapGuiRenderer,
  ISupportsFeature<LeapGuiMeshFeature>,
  ISupportsFeature<LeapGuiTextureFeature>,
  ISupportsFeature<LeapGuiTintFeature>,
  ISupportsFeature<LeapGuiBlendShapeFeature> {

  [SerializeField]
  private Shader _shader;

  [SerializeField]
  private MotionType _motionType = MotionType.Translation;

  [MinValue(0)]
  [SerializeField]
  private int borderAmount = 0;

  [Header("Debug")]
  [HideInInspector, SerializeField]
  private Mesh _bakedMesh;

  [HideInInspector, SerializeField]
  private Material _material;

  [HideInInspector, SerializeField]
  private GameObject _displayObject;

  [HideInInspector, SerializeField]
  private MeshFilter _filter;

  [HideInInspector, SerializeField]
  private MeshRenderer _renderer;

  private List<LeapGuiMeshFeature> _meshFeatures = new List<LeapGuiMeshFeature>();
  private List<LeapGuiTextureFeature> _textureFeatures = new List<LeapGuiTextureFeature>();
  private List<LeapGuiBlendShapeFeature> _blendShapeFeatures = new List<LeapGuiBlendShapeFeature>();

  private Dictionary<LeapGuiTextureFeature, Texture2D> _atlases;
  private Dictionary<UVChannelFlags, Rect[]> _atlasedUvs;

  //Tinting
  private const string TINT = LeapGui.PROPERTY_PREFIX + "Tints";
  private List<Color> _tintColors = new List<Color>();

  //Blend Shapes
  private const string BLEND_SHAPE = LeapGui.PROPERTY_PREFIX + "BlendShapeAmounts";
  private List<float> _blendShapeAmounts = new List<float>();

  //Cylindrical space
  private const string CYLINDRICAL_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Cylindrical_ElementParameters";
  private List<Vector4> _cylindrical_elementParameters = new List<Vector4>();

  public void GetSupportInfo(List<LeapGuiMeshFeature> features, List<FeatureSupportInfo> info) {
    FeatureSupportUtil.OnlySupportFirstFeature(features, info);

    if (DoesNeedUv3() && features[0].uv3) {
      info[0] = FeatureSupportInfo.Warning("Uv3 will be ignored because the baker is using it.");
    }
  }

  public void GetSupportInfo(List<LeapGuiTextureFeature> features, List<FeatureSupportInfo> info) {
    FeatureSupportUtil.OnlySupportFirstFeature(features, info);
  }

  public void GetSupportInfo(List<LeapGuiTintFeature> features, List<FeatureSupportInfo> info) {
    FeatureSupportUtil.OnlySupportFirstFeature(features, info);
  }

  public void GetSupportInfo(List<LeapGuiBlendShapeFeature> features, List<FeatureSupportInfo> info) {
    FeatureSupportUtil.OnlySupportFirstFeature(features, info);
  }

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

    foreach (var feature in gui.features) {
      if (!feature.isDirty) continue;

      if (feature is LeapGuiTintFeature) {
        var tintFeature = feature as LeapGuiTintFeature;
        tintFeature.data.Query().Select(data => data.tint).FillList(_tintColors);
        _material.SetColorArray(TINT, _tintColors);
      } else if (feature is LeapGuiBlendShapeFeature) {
        var blendShapeFeature = feature as LeapGuiBlendShapeFeature;
        blendShapeFeature.data.Query().Select(data => data.amount).FillList(_blendShapeAmounts);
        _material.SetFloatArray(BLEND_SHAPE, _blendShapeAmounts);
      }
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
      Profiler.BeginSample("Bake Verts");
      bakeVerts();
      Profiler.EndSample();

      Profiler.BeginSample("Bake Colors");
      bakeColors();
      Profiler.EndSample();
    }

    if (gui.GetAllFeaturesOfType(_textureFeatures)) {
      Profiler.BeginSample("Atlas Textures");
      atlasTextures();
      Profiler.EndSample();
    }

    Profiler.BeginSample("Bake Uvs");
    bakeUvs();
    Profiler.EndSample();

    if (DoesNeedUv3()) {
      Profiler.BeginSample("Bake Uv3");
      bakeUv3();
      Profiler.EndSample();
    }

    if (gui.features.Query().Any(f => f is LeapGuiTintFeature)) {
      _material.EnableKeyword(LeapGuiTintFeature.FEATURE_NAME);
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

  private List<Vector3> _tempVertList = new List<Vector3>();
  private List<int> _tempTriList = new List<int>();
  private void bakeVerts() {
    _tempVertList.Clear();
    _tempTriList.Clear();

    foreach (var meshFeature in _meshFeatures) {
      foreach (var data in meshFeature.data) {
        if (data.mesh == null) continue;

        CachedTopology topology = getTopology(data.mesh);

        int vertOffset = _tempVertList.Count;
        for (int i = 0; i < topology.tris.Length; i++) {
          _tempTriList.Add(topology.tris[i] + vertOffset);
        }

        for (int i = 0; i < topology.verts.Length; i++) {
          _tempVertList.Add(elementVertToBakedVert(data.element, topology.verts[i]));
        }
      }
    }

    _bakedMesh.SetVertices(_tempVertList);
    _bakedMesh.SetTriangles(_tempTriList, 0);
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
            var target = uvs[pair.Key];
            for (int i = 0; i < tempUvList.Count; i++) {
              var uv = tempUvList[i];
              target.Add(new Vector4(elementRect.x + uv.x * elementRect.width,
                                     elementRect.y + uv.y * elementRect.height,
                                     uv.z,
                                     uv.w));
            }
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

        uv3.Append(mesh.vertexCount, new Vector4(0, 0, 0, i));
      }
    }

    gui.GetAllFeaturesOfType(_blendShapeFeatures);
    var blendShape = _blendShapeFeatures.Query().FirstOrDefault();
    if (blendShape != null) {
      _material.EnableKeyword(LeapGuiBlendShapeFeature.FEATURE_NAME);

      int vertIndex = 0;
      for (int i = 0; i < blendShape.data.Count; i++) {
        var mesh = blendShape.data[i].blendShape;
        var element = gui.elements[i];

        var shapeVerts = mesh.vertices;
        for (int j = 0; j < shapeVerts.Length; j++) {
          Vector3 shapeVert = shapeVerts[j];
          Vector3 delta = elementVertToBakedVert(element, shapeVert) - _tempVertList[vertIndex];

          Vector4 currUv3 = uv3[vertIndex];
          currUv3.x = delta.x;
          currUv3.y = delta.y;
          currUv3.z = delta.z;
          uv3[vertIndex] = currUv3;

          vertIndex++;
        }
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
    if (gui.features.Query().Any(f => f is LeapGuiTintFeature)) return true;
    if (gui.features.Query().Any(f => f is LeapGuiBlendShapeFeature)) return true;
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

  private Dictionary<Mesh, CachedTopology> _topologyCache = new Dictionary<Mesh, CachedTopology>();
  private CachedTopology getTopology(Mesh mesh) {
    CachedTopology topology;
    if (!_topologyCache.TryGetValue(mesh, out topology)) {
      topology.tris = mesh.GetIndices(0);
      topology.verts = mesh.vertices;
      _topologyCache[mesh] = topology;
    }
    return topology;
  }

  private struct CachedTopology {
    public Vector3[] verts;
    public int[] tris;
  }
}
