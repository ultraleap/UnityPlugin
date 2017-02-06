using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

[AddComponentMenu("")]
[LeapGuiTag("Baked")]
public class LeapGuiBakedRenderer : LeapGuiRenderer,
  ISupportsFeature<LeapGuiMeshFeature>,
  ISupportsFeature<LeapGuiTextureFeature>,
  ISupportsFeature<LeapGuiTintFeature>,
  ISupportsFeature<LeapGuiBlendShapeFeature> {

  [SerializeField]
  private Shader _shader;

  [SerializeField]
  private LayerMask _layer;

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

  //Feature lists
  private List<LeapGuiMeshFeature> _meshFeatures = new List<LeapGuiMeshFeature>();
  private List<LeapGuiTextureFeature> _textureFeatures = new List<LeapGuiTextureFeature>();
  private List<LeapGuiBlendShapeFeature> _blendShapeFeatures = new List<LeapGuiBlendShapeFeature>();

  //Textures
  private Dictionary<UVChannelFlags, Rect[]> _atlasedRects;

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
      info[0] = info[0].OrWorse(FeatureSupportInfo.Warning("Uv3 will be ignored because the baker is using it."));
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

    Graphics.DrawMesh(_bakedMesh, gui.transform.localToWorldMatrix, _material, 0);
  }

  public override void OnEnableRendererEditor() {
    ensureObjectsAreValid();
  }

  public override void OnDisableRendererEditor() {
    DestroyImmediate(_bakedMesh);
    DestroyImmediate(_material);

    _bakedMesh = null;
    _material = null;
  }

  public override void OnUpdateRendererEditor() {
    ensureObjectsAreValid();

    _bakedMesh.Clear();

    if (gui.GetFeatures(_meshFeatures)) {
      Profiler.BeginSample("Bake Verts");
      bakeVerts();
      Profiler.EndSample();

      Profiler.BeginSample("Bake Colors");
      bakeColors();
      Profiler.EndSample();
    }

    if (gui.GetFeatures(_textureFeatures)) {
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

    //Make the mesh bounds huge, since the bounds don't properly reflect visual representation most of the time
    _bakedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);

    OnUpdateRenderer();
  }

  private void ensureObjectsAreValid() {
    if (_bakedMesh == null) {
      _bakedMesh = new Mesh();
      _bakedMesh.name = "Baked Gui Mesh";
    }

    if (_shader == null) {
      _shader = Shader.Find("LeapGui/Defaults/Baked");
    }

    if (_material != null) {
      DestroyImmediate(_material);
    }

    _material = new Material(_shader);
    _material.name = "Baked Gui Material";
  }

  private List<Vector3> _tempVertList = new List<Vector3>();
  private List<int> _tempTriList = new List<int>();
  private void bakeVerts() {
    _tempVertList.Clear();
    _tempTriList.Clear();

    foreach (var meshFeature in _meshFeatures) {
      foreach (var data in meshFeature.data) {
        if (data.mesh == null) continue;

        var topology = MeshCache.GetTopology(data.mesh);

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
    Texture2D[] atlasTextures;
    AtlasUtil.DoAtlas(_textureFeatures, borderAmount,
                  out atlasTextures,
                  out _atlasedRects);

    for (int i = 0; i < _textureFeatures.Count; i++) {
      _material.SetTexture(_textureFeatures[i].propertyName, atlasTextures[i]);
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
          mesh.GetUVsOrDefault(pair.Key.Index(), tempUvList);

          Rect[] atlasedUvs;
          if (_atlasedRects != null && _atlasedRects.TryGetValue(pair.Key, out atlasedUvs)) {
            MeshUtil.RemapUvs(tempUvList, atlasedUvs[elementIndex]);
          }

          uvs[pair.Key].AddRange(tempUvList);
        }
      }
    }

    //Asign uvs to baked mesh
    foreach (var pair in uvs) {
      _bakedMesh.SetUVsAuto(pair.Key.Index(), pair.Value);
      _material.EnableKeyword(LeapGuiMeshFeature.GetUvFeature(pair.Key));
    }
  }

  private void bakeUv3() {
    List<Vector4> uv3 = new List<Vector4>();

    foreach (var feature in _meshFeatures) {
      for (int i = 0; i < feature.data.Count; i++) {
        var mesh = feature.data[i].mesh;
        if (mesh == null) continue;

        uv3.Append(mesh.vertexCount, new Vector4(0, 0, 0, i));
      }
    }

    gui.GetFeatures(_blendShapeFeatures);
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
}
