using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif
using Leap.Unity.Query;
using Leap.Unity.Attributes;

[AddComponentMenu("")]
[LeapGuiTag("Baked")]
public class LeapGuiBakedRenderer : LeapGuiRenderer,
  ISupportsFeature<LeapGuiMeshFeature>,
  ISupportsFeature<LeapGuiTextureFeature>,
  ISupportsFeature<LeapGuiSpriteFeature>,
  ISupportsFeature<LeapGuiTintFeature>,
  ISupportsFeature<LeapGuiBlendShapeFeature> {

  #region INSPECTOR FIELDS
  [SerializeField]
  private Shader _shader;

  [SerializeField]
  private LayerMask _layer;

  [SerializeField]
  private MotionType _motionType = MotionType.Translation;

  [SerializeField]
  private PackUtil.Settings _atlasSettings;

  [Header("Debug")]
  [HideInInspector, SerializeField]
  private Mesh _bakedMesh;

  [HideInInspector, SerializeField]
  private Material _material;
  #endregion

  #region PRIVATE VARIABLES
  //Feature lists
  private List<LeapGuiMeshFeature> _meshFeatures = new List<LeapGuiMeshFeature>();
  private List<LeapGuiTextureFeature> _textureFeatures = new List<LeapGuiTextureFeature>();
  private List<LeapGuiSpriteFeature> _spriteFeatures = new List<LeapGuiSpriteFeature>();
  private List<LeapGuiBlendShapeFeature> _blendShapeFeatures = new List<LeapGuiBlendShapeFeature>();

  //## Meshes
  private Dictionary<LeapGuiMeshData, LeapGuiMeshData.MeshData> _meshData = new Dictionary<LeapGuiMeshData, LeapGuiMeshData.MeshData>();

  //## Textures
  //Maps a uv channel to an array of Rects, where each Rect maps the uvs of that element to an atlas
  //This atlas mapping might be created from packing, or due to atlased sprites
  private Dictionary<UVChannelFlags, Rect[]> _packedRects;

  //## Tinting
  private const string TINT = LeapGui.PROPERTY_PREFIX + "Tints";
  private List<Color> _tintColors = new List<Color>();

  //## Blend Shapes
  private const string BLEND_SHAPE = LeapGui.PROPERTY_PREFIX + "BlendShapeAmounts";
  private List<float> _blendShapeAmounts = new List<float>();

  //## Rect soace
  private const string RECT_POSITIONS = LeapGui.PROPERTY_PREFIX + "Rect_ElementPositions";
  private List<Vector4> _rect_elementPositions = new List<Vector4>();

  //## Cylindrical space
  private const string CYLINDRICAL_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Cylindrical_ElementParameters";
  private List<Vector4> _cylindrical_elementParameters = new List<Vector4>();

  //## Spherical space
  private const string SPHERICAL_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Spherical_ElementParameters";
  private List<Vector4> _spherical_elementParameters = new List<Vector4>();
  #endregion

  #region PUBLIC API
  public enum MotionType {
    None,
    Translation,
    Full
  }

  public void GetSupportInfo(List<LeapGuiMeshFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);

    if (doesNeedUv3() && features[0].uv3) {
      info[0] = info[0].OrWorse(SupportInfo.Warning("Uv3 will be ignored because the baker is using it."));
    }
  }

  public void GetSupportInfo(List<LeapGuiTextureFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);
  }

  public void GetSupportInfo(List<LeapGuiSpriteFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);

    Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget);

    for (int i = 0; i < features.Count; i++) {
      var feature = features[i];

      if (!feature.AreAllSpritesPacked()) {
        info[i] = SupportInfo.Error("Not all sprites are packed.");
      }

      if (!feature.AreAllSpritesOnSameTexture()) {
        info[i] = SupportInfo.Error("Not all sprites are packed into same atlas.");
      }
    }
  }

  public void GetSupportInfo(List<LeapGuiTintFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);
  }

  public void GetSupportInfo(List<LeapGuiBlendShapeFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);
  }

  public override SupportInfo GetSpaceSupportInfo(LeapGuiSpace space) {
    if (space is LeapGuiRectSpace ||
        space is LeapGuiCylindricalSpace) {
      return SupportInfo.FullSupport();
    } else {
      return SupportInfo.Error("Baked Renderer does not support " + space.GetType().Name);
    }
  }

  public override void OnEnableRenderer() {
    if (gui.GetSupportedFeatures(_spriteFeatures)) {
      uploadSpriteTextures();
    }
  }

  public override void OnDisableRenderer() { }

  public override void OnUpdateRenderer() {
    if (gui.space is LeapGuiRectSpace) {
      Profiler.BeginSample("Build Array");
      _rect_elementPositions.Clear();
      foreach (var element in gui.elements) {
        var guiSpace = transform.InverseTransformPoint(element.transform.position);
        _rect_elementPositions.Add(guiSpace);
      }
      Profiler.EndSample();

      Profiler.BeginSample("Upload Array");
      _material.SetVectorArray(RECT_POSITIONS, _rect_elementPositions);
      Profiler.EndSample();
    } else if (gui.space is LeapGuiCylindricalSpace) {
      var cylindricalSpace = gui.space as LeapGuiCylindricalSpace;

      _cylindrical_elementParameters.Clear();
      foreach (var element in gui.elements) {
        var parameters = cylindricalSpace.GetElementParameters(element.anchor, element.transform.position);
        _cylindrical_elementParameters.Add(parameters);
      }

      _material.SetFloat(LeapGuiCylindricalSpace.RADIUS_PROPERTY, cylindricalSpace.radius);
      _material.SetVectorArray(CYLINDRICAL_PARAMETERS, _cylindrical_elementParameters);
    } else if (gui.space is LeapGuiSphericalSpace) {
      var sphericalSpace = gui.space as LeapGuiSphericalSpace;

      _spherical_elementParameters.Clear();
      foreach (var element in gui.elements) {
        var parameters = sphericalSpace.GetElementParameters(element.anchor, element.transform.position);
        _spherical_elementParameters.Add(parameters);
      }

      _material.SetFloat(LeapGuiSphericalSpace.RADIUS_PROPERTY, sphericalSpace.radius);
      _material.SetVectorArray(SPHERICAL_PARAMETERS, _spherical_elementParameters);
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

    Profiler.BeginSample("Draw Mesh");
    Graphics.DrawMesh(_bakedMesh, gui.transform.localToWorldMatrix, _material, 0);
    Profiler.EndSample();
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

    if (gui.GetSupportedFeatures(_meshFeatures)) {
      Profiler.BeginSample("Load Meshes");
      loadAllMeshes();
      Profiler.EndSample();

      Profiler.BeginSample("Bake Verts");
      bakeVerts();
      Profiler.EndSample();

      Profiler.BeginSample("Bake Colors");
      bakeColors();
      Profiler.EndSample();
    }

    if (gui.GetSupportedFeatures(_textureFeatures)) {
      Profiler.BeginSample("Pack Textures");
      packTextures();
      Profiler.EndSample();
    }

    if (gui.GetSupportedFeatures(_spriteFeatures)) {
      Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget);

      Profiler.BeginSample("Extract Sprite Rects");
      extractSpriteRects();
      Profiler.EndSample();

      Profiler.BeginSample("Upload Sprite Textures");
      uploadSpriteTextures();
      Profiler.EndSample();
    }

    Profiler.BeginSample("Bake Uvs");
    bakeUvs();
    Profiler.EndSample();

    if (doesNeedUv3()) {
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
    } else if (gui.space is LeapGuiSphericalSpace) {
      _material.EnableKeyword(LeapGuiSphericalSpace.FEATURE_NAME);
    }

    //Make the mesh bounds huge, since the bounds don't properly reflect visual representation most of the time
    _bakedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);

    OnUpdateRenderer();
  }
  #endregion

  #region PRIVATE IMPLEMENTATION
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

  private void loadAllMeshes() {
    foreach (var meshFeature in _meshFeatures) {
      foreach (var meshData in meshFeature.data) {
        _meshData[meshData] = meshData.GetMeshData();
      }
    }
  }

  private List<Vector3> _tempVertList = new List<Vector3>();
  private List<int> _tempTriList = new List<int>();
  private void bakeVerts() {
    _tempVertList.Clear();
    _tempTriList.Clear();

    foreach (var meshFeature in _meshFeatures) {
      foreach (var data in meshFeature.data) {
        var meshData = _meshData[data];
        if (meshData.mesh == null) continue;

        var topology = MeshCache.GetTopology(meshData.mesh);

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
        var meshData = _meshData[data];
        if (meshData.mesh == null) continue;

        int vertexCount = meshData.mesh.vertexCount;
        Color totalTint = data.tint * feature.tint;

        var vertexColors = meshData.mesh.colors;
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

  private void packTextures() {
    Texture2D[] packedTextures;
    PackUtil.DoPack(_textureFeatures, _atlasSettings,
                out packedTextures,
                out _packedRects);

    for (int i = 0; i < _textureFeatures.Count; i++) {
      _material.SetTexture(_textureFeatures[i].propertyName, packedTextures[i]);
    }
  }

  private void extractSpriteRects() {
    if (_packedRects == null) {
      _packedRects = new Dictionary<UVChannelFlags, Rect[]>();
    }

    foreach (var spriteFeature in _spriteFeatures) {
      var rects = new Rect[gui.elements.Count];
      _packedRects[spriteFeature.channel] = rects;

      for (int i = 0; i < spriteFeature.data.Count; i++) {
        var dataObj = spriteFeature.data[i];
        var sprite = dataObj.sprite;
        if (sprite == null) continue;

        Vector2[] uvs = SpriteUtility.GetSpriteUVs(sprite, getAtlasData: true);
        float minX, minY, maxX, maxY;
        minX = maxX = uvs[0].x;
        minY = maxY = uvs[0].y;

        for (int j = 1; j < uvs.Length; j++) {
          minX = Mathf.Min(minX, uvs[j].x);
          minY = Mathf.Min(minY, uvs[j].y);
          maxX = Mathf.Max(maxX, uvs[j].x);
          maxY = Mathf.Max(maxY, uvs[j].y);
        }

        rects[i] = Rect.MinMaxRect(minX, minY, maxX, maxY);
      }
    }
  }

  private void uploadSpriteTextures() {
    foreach (var spriteFeature in _spriteFeatures) {
      var tex = spriteFeature.data.Query().
                                   Select(d => d.sprite).
                                   Where(s => s != null).
                                   Select(s => {
#if UNITY_EDITOR
                                     return SpriteUtility.GetSpriteTexture(s, getAtlasData: true);
#else
                                     return s.texture;
#endif

                                   }).
                                   FirstOrDefault();

      if (tex != null) {
        _material.SetTexture(spriteFeature.propertyName, tex);
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
      for (int elementIndex = 0; elementIndex < feature.data.Count; elementIndex++) {
        var meshData = _meshData[feature.data[elementIndex]];
        if (meshData.mesh == null) continue;

        foreach (var pair in uvs) {
          var channel = pair.Key;
          meshData.mesh.GetUVsOrDefault(channel.Index(), tempUvList);

          Rect[] atlasedUvs;
          if (_packedRects != null &&
              _packedRects.TryGetValue(channel, out atlasedUvs) &&
              (meshData.remappableChannels & channel) != 0) {
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
        var meshData = _meshData[feature.data[i]];
        if (meshData.mesh == null) continue;

        uv3.Append(meshData.mesh.vertexCount, new Vector4(0, 0, 0, i));
      }
    }

    gui.GetSupportedFeatures(_blendShapeFeatures);
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
  private bool doesNeedUv3() {
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
  #endregion
}
