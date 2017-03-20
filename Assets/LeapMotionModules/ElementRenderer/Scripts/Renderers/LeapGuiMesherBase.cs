using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;



public abstract class LeapGuiMesherBase : LeapGuiRenderer<LeapGuiMeshElementBase>,
  ISupportsFeature<LeapGuiTextureFeature>,
  ISupportsFeature<LeapGuiSpriteFeature>,
  ISupportsFeature<LeapGuiTintFeature>,
  ISupportsFeature<LeapGuiBlendShapeFeature> {

  [Serializable]
  private class JaggedRects : JaggedArray<Rect> {
    public JaggedRects(int length) : base(length) { }
  }

  public const string UV_0_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_UV_0";
  public const string UV_1_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_UV_1";
  public const string UV_2_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_UV_2";
  public const string UV_3_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_UV_3";
  public const string COLORS_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_COLORS";
  public const string NORMALS_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_NORMALS";

  public static string GetUvFeature(UVChannelFlags flags) {
    switch (flags) {
      case UVChannelFlags.UV0:
        return UV_0_FEATURE;
      case UVChannelFlags.UV1:
        return UV_1_FEATURE;
      case UVChannelFlags.UV2:
        return UV_2_FEATURE;
      case UVChannelFlags.UV3:
        return UV_3_FEATURE;
      default:
        throw new InvalidOperationException();
    }
  }

  #region INSPECTOR FIELDS
  //BEGIN MESH SETTINGS
  [EditTimeOnly, SerializeField, HideInInspector]
  private bool _useUv0 = true;

  [EditTimeOnly, SerializeField, HideInInspector]
  private bool _useUv1 = false;

  [EditTimeOnly, SerializeField, HideInInspector]
  private bool _useUv2 = false;

  [EditTimeOnly, SerializeField, HideInInspector]
  private bool _useUv3 = false;

  [EditTimeOnly, SerializeField, HideInInspector]
  private bool _useColors = false;

  [EditTimeOnly, SerializeField, HideInInspector]
  private Color _globalTint = Color.white;

  [EditTimeOnly, SerializeField, HideInInspector]
  private bool _useNormals = false;

  //BEGIN RENDERING SETTINGS
  [SerializeField]
  protected Shader _shader;

  [SerializeField]
  private AtlasBuilder _atlas = new AtlasBuilder();
  #endregion

  //Current state of builder
  protected LeapGuiMeshElementBase _currElement;
  protected int _currIndex;
  protected Mesh _currMesh;

  //Current mesh data being written to
  protected List<Vector3> _verts = new List<Vector3>();
  protected List<Vector3> _normals = new List<Vector3>();
  protected List<int> _tris = new List<int>();
  protected List<Color> _colors = new List<Color>();
  protected List<Vector4>[] _uvs = new List<Vector4>[4] {
    new List<Vector4>(),
    new List<Vector4>(),
    new List<Vector4>(),
    new List<Vector4>()
  };

  //Internal cache of requirements 
  protected bool _doesRequireColors;
  protected bool _doesRequireNormals;
  protected bool _doesRequireSpecialUv3;
  protected List<UVChannelFlags> _requiredUvChannels = new List<UVChannelFlags>();

  //Feature lists
  protected List<LeapGuiTextureFeature> _textureFeatures = new List<LeapGuiTextureFeature>();
  protected List<LeapGuiSpriteFeature> _spriteFeatures = new List<LeapGuiSpriteFeature>();
  protected List<LeapGuiTintFeature> _tintFeatures = new List<LeapGuiTintFeature>();
  protected List<LeapGuiBlendShapeFeature> _blendShapeFeatures = new List<LeapGuiBlendShapeFeature>();

  //### Generated Data ###
  [SerializeField, HideInInspector]
  protected Material _material;
  [SerializeField, HideInInspector]
  protected RendererMeshData _meshes;
  [SerializeField, HideInInspector]
  protected RendererTextureData _packedTextures;

  //#### Sprite/Texture Remapping ####
  [SerializeField, HideInInspector]
  private JaggedRects _channelMapping = new JaggedRects(4);

  //#### Tinting ####
  protected const string TINTS_PROPERTY = LeapGui.PROPERTY_PREFIX + "Tints";
  protected List<Color> _tintColors = new List<Color>();

  //#### Blend Shapes ####
  protected const string BLEND_SHAPE_AMOUNTS_PROPERTY = LeapGui.PROPERTY_PREFIX + "BlendShapeAmounts";
  protected List<float> _blendShapeAmounts = new List<float>();

  public virtual bool IsAtlasDirty {
    get {
      return _atlas.isDirty;
    }
  }

  public virtual void RebuildAtlas(ProgressBar progress) {
    progress.Begin(2, "Rebuilding Atlas", "", () => {
      Texture2D[] packedTextures;
      Rect[][] channelMapping;
      _atlas.RebuildAtlas(progress, out packedTextures, out channelMapping);

      //Copy mapping into local array
      //Local array is used for both textures and sprites
      for (int i = 0; i < 4; i++) {
        var mapping = channelMapping[i];
        if (mapping != null) {
          mapping = mapping.Clone() as Rect[];
        }
        _channelMapping[i] = mapping;
      }

      progress.Begin(2, "", "", () => {
        progress.Step("Saving To Asset");
        _packedTextures.AssignTextures(packedTextures, _textureFeatures.Query().Select(f => f.propertyName).ToArray());

        progress.Begin(_textureFeatures.Count, "", "Loading Into Scene", () => {
          foreach (var feature in _textureFeatures) {
            _material.SetTexture(feature.propertyName, _packedTextures.GetTexture(feature.propertyName));
          }
        });
      });
    });
  }

  public virtual void GetSupportInfo(List<LeapGuiTextureFeature> features, List<SupportInfo> info) {
    for (int i = 0; i < features.Count; i++) {
      var feature = features[i];
      if (group.features.Query().OfType<LeapGuiSpriteFeature>().Any(s => s.channel == feature.channel)) {
        info[i] = info[i].OrWorse(SupportInfo.Error("Texture features cannot currently share uvs with sprite features."));
      }
    }
  }

  public virtual void GetSupportInfo(List<LeapGuiSpriteFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);

#if UNITY_EDITOR
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
#endif

    for (int i = 0; i < features.Count; i++) {
      var feature = features[i];
      if (group.features.Query().OfType<LeapGuiTextureFeature>().Any(s => s.channel == feature.channel)) {
        info[i] = info[i].OrWorse(SupportInfo.Error("Sprite features cannot currently share uvs with texture features."));
      }
    }
  }

  public virtual void GetSupportInfo(List<LeapGuiTintFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);
  }

  public virtual void GetSupportInfo(List<LeapGuiBlendShapeFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);
  }

  public override void OnEnableRenderer() {
    loadAllSupportedFeatures();

    //Sprite textures cannot be accessed at edit time, so we need to make sure
    //to upload them to the material right as the renderer is enabled
    if (_spriteFeatures != null) {
      uploadSpriteTextures();
    }
  }

  public override void OnDisableRenderer() { }

  public override void OnEnableRendererEditor() { }

  public override void OnDisableRendererEditor() {
    DeleteAsset(ref _meshes);
    DeleteAsset(ref _packedTextures);
  }

  public override void OnUpdateRenderer() {
    updateTinting();
    updateBlendShapes();
  }

  public override void OnUpdateRendererEditor(bool isHeavyUpdate) {
    base.OnUpdateRendererEditor(isHeavyUpdate);

    EnsureAssetSaved(ref _meshes, "Mesh Data");
    EnsureAssetSaved(ref _packedTextures, "Texture Data");

    setupForBuilding();
    buildMesh();
  }

  #region UPDATE
  protected virtual void updateTinting() {
    foreach (var tintFeature in _tintFeatures) {
      if (!tintFeature.isDirty) continue;


      using (new ProfilerSample("Update Tinting")) {
        tintFeature.data.Query().Select(d => d.tint).FillList(_tintColors);
        _material.SetColorArraySafe(TINTS_PROPERTY, _tintColors);
      }
    }
  }

  protected virtual void updateBlendShapes() {
    foreach (var blendShapeFeature in _blendShapeFeatures) {
      if (!blendShapeFeature.isDirty) continue;

      using (new ProfilerSample("Update Blend Shapes")) {
        blendShapeFeature.data.Query().Select(d => d.amount).FillList(_blendShapeAmounts);
        _material.SetFloatArraySafe(BLEND_SHAPE_AMOUNTS_PROPERTY, _blendShapeAmounts);
      }
    }
  }
  #endregion

  #region GENERATION SETUP
  protected virtual void setupForBuilding() {
    using (new ProfilerSample("Mesh Setup")) {
      MeshCache.Clear();
      loadAllSupportedFeatures();
      prepareMeshes();
      prepareMaterial();

      if (_doesRequireColors) {
        _material.EnableKeyword(COLORS_FEATURE);
      }

      if (_doesRequireNormals) {
        _material.EnableKeyword(NORMALS_FEATURE);
      }

      foreach (var channel in _requiredUvChannels) {
        _material.EnableKeyword(GetUvFeature(channel));
      }

      if (_textureFeatures.Count != 0) {
        _atlas.UpdateTextureList(_textureFeatures);

        foreach (var feature in _textureFeatures) {
          _material.SetTexture(feature.propertyName, _packedTextures.GetTexture(feature.propertyName));
        }
      }

      if (_spriteFeatures.Count != 0) {
#if UNITY_EDITOR
        Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget);
#endif
        extractSpriteRects();
        uploadSpriteTextures();
      }

      if (_tintFeatures.Count != 0) {
        _material.EnableKeyword(LeapGuiTintFeature.FEATURE_NAME);
      }

      if (_blendShapeFeatures.Count != 0) {
        _material.EnableKeyword(LeapGuiBlendShapeFeature.FEATURE_NAME);
      }
    }
  }

  protected virtual void loadAllSupportedFeatures() {
    using (new ProfilerSample("Load All Supported Features")) {
      group.GetSupportedFeatures(_textureFeatures);
      group.GetSupportedFeatures(_spriteFeatures);
      group.GetSupportedFeatures(_tintFeatures);
      group.GetSupportedFeatures(_blendShapeFeatures);

      _doesRequireColors = doesRequireMeshColors();
      _doesRequireNormals = doesRequireMeshNormals();
      _doesRequireSpecialUv3 = doesRequireSpecialUv3();

      _requiredUvChannels.Clear();
      foreach (var channel in MeshUtil.allUvChannels) {
        if (channel == UVChannelFlags.UV3 && _doesRequireSpecialUv3) continue;

        if (doesRequireUvChannel(channel)) {
          _requiredUvChannels.Add(channel);
        }
      }
    }
  }

  protected virtual void prepareMeshes() {
    _meshes.Clear();
    _currMesh = null;
  }

  protected virtual void prepareMaterial() {
    Material newMaterial;
    if (_material != null) {
      newMaterial = new Material(_material);
      DestroyImmediate(_material);
    } else {
      newMaterial = new Material(_shader);
    }

    _material = newMaterial;
    _material.shader = _shader;
    _material.name = "Procedural Gui Material";

    if (_channelMapping == null) {
      _channelMapping = new JaggedRects(4);
    }
  }

  protected virtual void extractSpriteRects() {
    using (new ProfilerSample("Extract Sprite Rects")) {
      if (_channelMapping == null) {
        _channelMapping = new JaggedRects(4);
      }

      foreach (var spriteFeature in _spriteFeatures) {
        var rects = new Rect[group.elements.Count];
        _channelMapping[spriteFeature.channel.Index()] = rects;

        for (int i = 0; i < spriteFeature.data.Count; i++) {
          var dataObj = spriteFeature.data[i];
          var sprite = dataObj.sprite;
          if (sprite == null) continue;

          Vector2[] uvs = SpriteAtlasUtil.GetAtlasedUvs(sprite);
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
  }

  protected virtual void uploadSpriteTextures() {
    using (new ProfilerSample("Upload Sprite Textures")) {
      foreach (var spriteFeature in _spriteFeatures) {
        var tex = spriteFeature.data.Query().
                                     Select(d => d.sprite).
                                     Where(s => s != null).
                                     Select(s => {
#if UNITY_EDITOR
                                       var atlas = SpriteUtility.GetSpriteTexture(s, getAtlasData: true);

                                       //Cannot reference atlas texture in material at runtime because it doesn't
                                       //actually exist as an asset, so we create a direct copy of the atlas
                                       //instead for preview.  We don't save it, then load the correct atlas 
                                       //at runtime.
                                       var safeAtlas = new Texture2D(atlas.width, atlas.height, atlas.format, atlas.mipmapCount > 1);
                                       safeAtlas.name = "Temp atlas texture";
                                       safeAtlas.hideFlags = HideFlags.HideAndDontSave;
                                       Graphics.CopyTexture(atlas, safeAtlas);
                                       return safeAtlas;
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
  }
  #endregion

  #region MESH GENERATION
  protected virtual void buildMesh() {
    using (new ProfilerSample("Build Mesh")) {
      beginMesh();
      for (_currIndex = 0; _currIndex < group.elements.Count; _currIndex++) {
        _currElement = group.elements[_currIndex] as LeapGuiMeshElementBase;
        buildElement();
      }
      finishMesh();
    }
  }

  protected virtual void buildElement() {
    using (new ProfilerSample("Build Element")) {

      refreshMeshData();
      if (_currElement.mesh == null) return;

      buildTopology();

      if (_doesRequireColors) {
        buildColors();
      }

      foreach (var channel in _requiredUvChannels) {
        buildUvs(channel);
      }

      if (_doesRequireSpecialUv3) {
        buildSpecialUv3();
      }

      foreach (var blendShapeFeature in _blendShapeFeatures) {
        buildBlendShapes(blendShapeFeature.data[_currIndex]);
      }
    }
  }

  protected virtual void refreshMeshData() {
    using (new ProfilerSample("Refresh Mesh Data")) {
      _currElement.RefreshMeshData();
    }
  }

  protected virtual void buildTopology() {
    using (new ProfilerSample("Build Topology")) {
      var topology = MeshCache.GetTopology(_currElement.mesh);

      int vertOffset = _verts.Count;
      for (int i = 0; i < topology.tris.Length; i++) {
        _tris.Add(topology.tris[i] + vertOffset);
      }

      if (_doesRequireNormals) {
        var normals = MeshCache.GetNormals(_currElement.mesh);
        for (int i = 0; i < topology.verts.Length; i++) {
          Vector3 meshVert;
          Vector3 meshNormal;
          elementVertNormalToMeshVertNormal(topology.verts[i], normals[i], out meshVert, out meshNormal);

          _verts.Add(meshVert);
          _normals.Add(meshNormal);
        }
      } else {
        for (int i = 0; i < topology.verts.Length; i++) {
          _verts.Add(elementVertToMeshVert(topology.verts[i]));
        }
      }
    }
  }

  protected virtual void buildColors() {
    using (new ProfilerSample("Build Colors")) {
      Color totalTint = _globalTint * _currElement.tint;

      var colors = MeshCache.GetColors(_currElement.mesh);
      for (int i = 0; i < colors.Length; i++) {
        _colors.Add(colors[i] * totalTint);
      }
    }
  }

  protected virtual void buildUvs(UVChannelFlags channel) {
    using (new ProfilerSample("Build Uvs")) {
      var uvs = MeshCache.GetUvs(_currElement.mesh, channel);
      var targetList = _uvs[channel.Index()];

      targetList.AddRange(uvs);

      if (_channelMapping != null &&
         (_currElement.remappableChannels & channel) != 0) {
        var atlasedUvs = _channelMapping[channel.Index()];
        if (atlasedUvs != null && _currIndex < atlasedUvs.Length) {
          MeshUtil.RemapUvs(targetList, atlasedUvs[_currIndex], uvs.Count);
        }
      }
    }
  }

  protected virtual void buildSpecialUv3() {
    using (new ProfilerSample("Build Special Uv3")) {
      _uvs[3].Append(_currElement.mesh.vertexCount, new Vector4(0, 0, 0, _currIndex));
    }
  }

  protected virtual void buildBlendShapes(LeapGuiBlendShapeData blendShapeData) {
    using (new ProfilerSample("Build Blend Shapes")) {
      var shape = blendShapeData.blendShape;

      int offset = _verts.Count - shape.vertexCount;

      var verts = shape.vertices;
      for (int i = 0; i < verts.Length; i++) {
        Vector3 shapeVert = verts[i];
        Vector3 delta = blendShapeDelta(shapeVert, _verts[i + offset]);

        Vector4 currUv = _uvs[3][i + offset];
        currUv.x = delta.x;
        currUv.y = delta.y;
        currUv.z = delta.z;
        _uvs[3][i + offset] = currUv;
      }
    }
  }

  protected virtual Vector3 blendShapeDelta(Vector3 shapeVert, Vector3 originalVert) {
    return elementVertToMeshVert(shapeVert) - originalVert;
  }

  protected virtual void beginMesh() {
    Assert.IsNull(_currMesh, "Cannot begin a new mesh without finishing the current mesh.");

    _currMesh = new Mesh();
    _currMesh.name = "Procedural Gui Mesh";
    _currMesh.hideFlags = HideFlags.None;

    _verts.Clear();
    _normals.Clear();
    _tris.Clear();
    _colors.Clear();
    for (int i = 0; i < 4; i++) {
      _uvs[i].Clear();
    }
  }

  protected virtual void finishMesh() {
    using (new ProfilerSample("Finish Mesh")) {
      //If there is no data, don't actually do anything
      if (_verts.Count == 0) {
        DestroyImmediate(_currMesh);
        _currMesh = null;
        return;
      }

      _currMesh.SetVertices(_verts);
      _currMesh.SetTriangles(_tris, 0);

      if (_normals.Count == _verts.Count) {
        _currMesh.SetNormals(_normals);
      }

      if (_colors.Count == _verts.Count) {
        _currMesh.SetColors(_colors);
      }

      foreach (var channel in _requiredUvChannels) {
        _currMesh.SetUVs(channel.Index(), _uvs[channel.Index()]);
      }

      if (_doesRequireSpecialUv3) {
        _currMesh.SetUVs(3, _uvs[3]);
      }

      postProcessMesh();

      _meshes.AddMesh(_currMesh);
      _currMesh = null;
    }
  }

  protected virtual void postProcessMesh() { }
  #endregion

  #region UTILITY

  protected IEnumerable<UVChannelFlags> enabledUvChannels {
    get {
      if (_useUv0) yield return UVChannelFlags.UV0;
      if (_useUv1) yield return UVChannelFlags.UV1;
      if (_useUv2) yield return UVChannelFlags.UV2;
      if (_useUv3) yield return UVChannelFlags.UV3;
    }
  }

  protected virtual bool doesRequireMeshColors() {
    return _useColors;
  }

  protected virtual bool doesRequireMeshNormals() {
    return _useNormals;
  }

  protected virtual bool doesRequireUvChannel(UVChannelFlags channel) {
    switch (channel) {
      case UVChannelFlags.UV0:
        return _useUv0;
      case UVChannelFlags.UV1:
        return _useUv1;
      case UVChannelFlags.UV2:
        return _useUv2;
      case UVChannelFlags.UV3:
        return _useUv3;
      default:
        throw new ArgumentException("Invalid channel argument.");
    }
  }

  protected virtual bool doesRequireSpecialUv3() {
    return _tintFeatures.Count != 0 ||
           _blendShapeFeatures.Count != 0;
  }

  protected abstract Vector3 elementVertToMeshVert(Vector3 vertex);
  protected abstract void elementVertNormalToMeshVertNormal(Vector3 vertex, Vector3 normal, out Vector3 meshVert, out Vector3 meshNormal);
  #endregion
}
