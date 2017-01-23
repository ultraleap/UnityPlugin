using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Attributes;
using Leap.Unity.Query;

namespace Leap.Unity.Gui.Space {

  [ExecuteInEditMode]
  [RequireComponent(typeof(MeshFilter))]
  [RequireComponent(typeof(MeshRenderer))]
  public class GuiMeshBaker : MonoBehaviour {
    public const string GUI_SPACE_SHADER_FEATURE_PREFIX = "GUI_SPACE_";
    public const string GUI_SPACE_PROPERTY_PREFIX = "_GuiSpace_";

    public const string UV_CHANNEL_0_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "UV_0";
    public const string UV_CHANNEL_1_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "UV_1";
    public const string UV_CHANNEL_2_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "UV_2";

    public const string VERTEX_NORMALS_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "NORMALS";
    public const string VERTEX_COLORS_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "VERTEX_COLORS";

    public const string MOTION_TRANSLATION_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "MOVEMENT_TRANSLATION";
    public const string MOTION_FULL_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "MOVEMENT_FULL";

    public const string TINTING_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "TINTING";
    public const string TINTS_PROPERTY = GUI_SPACE_PROPERTY_PREFIX + "Tints";

    public const string BLEND_SHAPE_FEATURE = GUI_SPACE_SHADER_FEATURE_PREFIX + "BLEND_SHAPES";
    public const string BLEND_SHAPE_AMOUNTS_PROPERTY = GUI_SPACE_PROPERTY_PREFIX + "BlendShapeAmounts";

    #region INSPECTOR FIELDS
    [SerializeField]
    private Shader _shader;

    [Header("Mesh Options")]
    [AutoFind(AutoFindLocations.Object)]
    [SerializeField]
    private GuiSpace _space;

    [SerializeField]
    private TextureChannel[] _textureChannels;

    [SerializeField]
    private AtlasSettings _atlasSettings;

    [SerializeField]
    private bool _enableVertexNormals = false;

    [Tooltip("Enabling this will cause the vertex colors of each gui element to be baked into " +
             "the final gui mesh.")]
    [SerializeField]
    private bool _enableVertexColors;

    [Tooltip("Tint the baked vertex colors with this color.")]
    [SerializeField]
    private Color _bakedTint = Color.white;

    /// <summary>
    /// Enabling any of these options will cause UV3's to be baked into the mesh.  The W component
    /// of this vector will be used as an element index, which is used by all of these effects.
    /// In the case that blend shape animation is turned on, the XYX components will be used 
    /// to define that space.
    /// </summary>
    [Header("Animation Options")]
    [Tooltip("Allows gui elements to move around freely.")]
    [SerializeField]
    private bool _enableElementMotion = false;

    [Tooltip("Allows each gui element to move, instead of being stationary.  Also allows " +
             "the properties of the gui space to be changed at runtime.")]
    [SerializeField]
    private MotionType _motionType = MotionType.TranslationOnly;

    [Tooltip("Allows each gui element to recieve a different tint color at runtime.")]
    [SerializeField]
    private bool _enableTinting = false;

    [Tooltip("Allows each gui element to warp to and from a different shape at runtime.  " +
             "Shape data is baked out as a position delta into UV3.")]
    [SerializeField]
    private bool _enableBlendShapes = false;

    [Tooltip("Defines what coordinate space blend shapes are defined in.")]
    [SerializeField]
    private BlendShapeSpace _blendShapeSpace = BlendShapeSpace.Local;

    #endregion

    [SerializeField]
    private Mesh _bakedMesh;

    [SerializeField]
    private Texture2D[] _atlases;

    [SerializeField]
    private Material _material;

    [SerializeField]
    private List<LeapElement> _elements;

    private MeshRenderer _renderer;

    private bool _areTintsDirty = true;
    private List<Color> _tints = new List<Color>();

    private bool _areBlendShapeAmountsDirty = true;
    private List<float> _blendShapeAmounts = new List<float>();

    #region PUBLIC API
    public int textureChannels {
      get {
        return _textureChannels.Length;
      }
    }

    public bool enableVertexColors {
      get {
        return _enableVertexColors;
      }
    }

    public bool enableTinting {
      get {
        return _enableTinting;
      }
    }

    public bool enableBlendShapes {
      get {
        return _enableBlendShapes;
      }
    }

    public void SetTint(int elementId, Color tint) {
      _areTintsDirty = true;
      _tints[elementId] = tint;
    }

    public void SetBlendShapeAmount(int elementId, float shapeAmount) {
      _areBlendShapeAmountsDirty = true;
      _blendShapeAmounts[elementId] = shapeAmount;
    }
    #endregion

    #region UNITY CALLBACKS

    void Awake() {
      _renderer = GetComponent<MeshRenderer>();

      initAnimationData();
    }

#if UNITY_EDITOR
    void Update() {
      if (!Application.isPlaying) {
        bakeMesh();
        initAnimationData();
        setupMaterial();
        updateMaterial();
      }
    }
#endif

    void LateUpdate() {
      if (Application.isPlaying) {
        updateMaterial();
      }
    }

    #endregion

    private void setupMaterial() {
      if (_material == null) {
        _material = new Material(_shader);
        _material.name = "GUI Space Material";
      }

      if (_material.shader != _shader) {
        _material.shader = _shader;
      }
      if (_renderer.sharedMaterial != _material) {
        _renderer.sharedMaterial = _material;
      }

      foreach (var keyword in _material.shaderKeywords.Query().Where(k => k.StartsWith(GUI_SPACE_SHADER_FEATURE_PREFIX))) {
        _material.DisableKeyword(keyword);
      }

      if (_textureChannels.Query().Any(t => t.channel == UVChannel.UV0)) {
        _material.EnableKeyword(UV_CHANNEL_0_FEATURE);
      }

      if (_textureChannels.Query().Any(t => t.channel == UVChannel.UV1)) {
        _material.EnableKeyword(UV_CHANNEL_0_FEATURE);
      }

      if (_textureChannels.Query().Any(t => t.channel == UVChannel.UV2)) {
        _material.EnableKeyword(UV_CHANNEL_0_FEATURE);
      }

      if (_enableVertexNormals) {
        _material.EnableKeyword(VERTEX_NORMALS_FEATURE);
      }

      if (_enableVertexColors) {
        _material.EnableKeyword(VERTEX_COLORS_FEATURE);
      }

      if (_enableElementMotion && _space != null) {
        _material.EnableKeyword(_space.ShaderVariantName);
      }

      if (_enableElementMotion) {
        switch (_motionType) {
          case MotionType.TranslationOnly:
            _material.EnableKeyword(MOTION_TRANSLATION_FEATURE);
            break;
          case MotionType.Full:
            _material.EnableKeyword(MOTION_FULL_FEATURE);
            break;
        }
      }

      if (_enableTinting) {
        _material.EnableKeyword(TINTING_FEATURE);
      }

      if (_enableBlendShapes) {
        _material.EnableKeyword(BLEND_SHAPE_FEATURE);
      }

      for (int i = 0; i < _textureChannels.Length; i++) {
        _material.SetTexture(_textureChannels[i].propertyName, _atlases[i]);
      }
    }

    private void updateMaterial() {
      if (_space != null) {
        _space.BuildPerElementData();
        _space.UpdateMaterial(_material);
      }

      if (_enableTinting && _areTintsDirty) {
        _material.SetColorArray(TINTS_PROPERTY, _tints);
        _areTintsDirty = false;
      }

      if (_enableBlendShapes && _areBlendShapeAmountsDirty) {
        _material.SetFloatArray(BLEND_SHAPE_AMOUNTS_PROPERTY, _blendShapeAmounts);
        _areBlendShapeAmountsDirty = false;
      }
    }

    #region BAKING

    private void initAnimationData() {
      if (_enableTinting) {
        _elements.Query().Select(e => e.tint).FillList(_tints);
        _areTintsDirty = true;
      }

      if (_enableBlendShapes) {
        _elements.Query().Select(e => e.blendShapeAmount).FillList(_blendShapeAmounts);
        _areBlendShapeAmountsDirty = true;
      }
    }

#if UNITY_EDITOR
    private void bakeMesh() {
      _elements.Clear();
      bakeElementHierarchy(transform);

      //Ready mesh for baking, clear out pre-existing data or just create a new mesh
      if (_bakedMesh == null) {
        _bakedMesh = new Mesh();
        _bakedMesh.name = "Baked GUI Mesh";
      } else {
        _bakedMesh.Clear();
      }

      //First bake out vertex and normal information into the mesh
      bakeVerts();

      //Then atlas all textures used by the gui elements
      var packedUvs = atlasTextures();

      //Use the packed uvs to bake out uv coordinates into the baked mesh
      bakeUvs(packedUvs);

      //If vertex colors are enabled, bake them out into the mesh
      if (_enableVertexColors) {
        bakeColors();
      }

      //Bake out blend shapes and element id information into the mesh
      bakeBlendShapes();

      GetComponent<MeshFilter>().sharedMesh = _bakedMesh;
    }

    private void bakeElementHierarchy(Transform root) {
      int childCount = root.childCount;
      for (int i = 0; i < childCount; i++) {
        Transform child = root.GetChild(i);

        var element = child.GetComponent<LeapElement>();
        element.elementId = _elements.Count;
        _elements.Add(element);

        bakeElementHierarchy(child);
      }
    }

    private void bakeVerts() {
      List<Vector3> verts = new List<Vector3>();
      List<Vector3> normals = new List<Vector3>();
      List<int> tris = new List<int>();

      foreach (var element in _elements) {
        var elementMesh = element.GetMesh();
        var elementTransform = element.transform;

        elementMesh.GetIndices(0).Query().Select(i => i + verts.Count).AppendList(tris);

        //TODO: bake out space if no motion is enabled
        elementMesh.vertices.Query().Select(v => {
          v = element.transform.TransformPoint(v);
          v = transform.InverseTransformPoint(v);
          v -= transform.InverseTransformPoint(element.transform.position);
          return v;
        }).AppendList(verts);

        normals.AddRange(elementMesh.normals);
      }

      _bakedMesh.SetVertices(verts);
      _bakedMesh.SetNormals(normals);
      _bakedMesh.SetTriangles(tris, 0);
    }

    private Rect[][] atlasTextures() {
      _atlases = new Texture2D[_textureChannels.Length];

      var whiteTexture = new Texture2D(3, 3, TextureFormat.ARGB32, mipmap: false, linear: true);
      whiteTexture.SetPixels(new Color[3 * 3].Fill(Color.white));
      whiteTexture.Apply();

      //Atlas all textures
      Rect[][] packedUvs = new Rect[_textureChannels.Length][];

      Texture2D[] textureArray = new Texture2D[_elements.Count];
      Dictionary<Texture2D, Texture2D> textureMapping = new Dictionary<Texture2D, Texture2D>();
      for (int i = 0; i < _textureChannels.Length; i++) {

        //PackTextures automatically pools shared textures, but we need to manually
        //pool because we are creating new textures with Border()
        textureMapping.Clear();
        for (int j = 0; j < _elements.Count; j++) {
          var element = _elements[j];
          Texture2D tex = element.GetTexture(i);
          Texture2D mappedTex;
          if (tex == null || !tex.EnsureReadWriteEnabled()) {
            mappedTex = whiteTexture;
          } else {
            if (!textureMapping.TryGetValue(tex, out mappedTex)) {
              mappedTex = Instantiate(tex);
              mappedTex.AddBorder(_atlasSettings.border);
              textureMapping[tex] = mappedTex;
            }
          }

          textureArray[j] = mappedTex;
        }

        var atlas = new Texture2D(1, 1, TextureFormat.ARGB32, mipmap: false);
        atlas.filterMode = _atlasSettings.filterMode;
        atlas.wrapMode = _atlasSettings.wrapMode;

        var uvs = atlas.PackTextures(textureArray, _atlasSettings.padding, _atlasSettings.maximumAtlasSize);

        for (int j = 0; j < textureArray.Length; j++) {
          float dx = 1.0f / atlas.width;
          float dy = 1.0f / atlas.height;

          //Uvs will point to the larger texture, pull the uvs in so they match the original texture size
          //White texture wasn't bordered so the uvs are already correct
          if (textureArray[j] != whiteTexture) {
            dx *= _atlasSettings.border;
            dy *= _atlasSettings.border;
          }

          Rect rect = uvs[j];
          rect.x += dx;
          rect.y += dy;
          rect.width -= dx * 2;
          rect.height -= dy * 2;
          uvs[j] = rect;
        }

        packedUvs[i] = uvs;

        _atlases[i] = atlas;
        GetComponent<MeshRenderer>().sharedMaterial.SetTexture(_textureChannels[i].propertyName, atlas);
      }

      return packedUvs;
    }

    private void bakeUvs(Rect[][] packedUvs) {
      List<Vector2>[] allUvs = new List<Vector2>[_textureChannels.Length];
      List<Vector2> tempUvs = new List<Vector2>();
      for (int texIndex = 0; texIndex < _textureChannels.Length; texIndex++) {
        var remapping = _elements.Query().Zip(packedUvs[texIndex].Query(), (element, packedRect) => {
          var mesh = element.GetMesh();
          mesh.GetUVs(texIndex, tempUvs);

          //If mesh has wrong number of uvs, just fill with zeros
          if (tempUvs.Count != mesh.vertexCount) {
            tempUvs.Fill(mesh.vertexCount, Vector2.zero);
          }

          return tempUvs.Query().Select(uv => new Vector2(packedRect.x + packedRect.width * uv.x,
                                                          packedRect.y + packedRect.height * uv.y));
        });

        allUvs[texIndex] = new List<Vector2>();
        foreach (var remappedUvs in remapping) {
          remappedUvs.AppendList(allUvs[texIndex]);
        }
      }

      for (int i = 0; i < _textureChannels.Length; i++) {
        _bakedMesh.SetUVs(i, allUvs[i]);
      }
    }

    private void bakeColors() {
      List<Color> colors = new List<Color>();

      foreach (var element in _elements) {
        var elementMesh = element.GetMesh();
        int vertexCount = elementMesh.vertexCount;
        Color vertexColorTint = element.vertexColor * _bakedTint;

        var vertexColors = elementMesh.colors;
        if (vertexColors.Length != vertexCount) {
          colors.Append(vertexCount, vertexColorTint);
        } else {
          vertexColors.Query().Select(c => c * vertexColorTint).AppendList(colors);
        }
      }

      _bakedMesh.SetColors(colors);
    }

    private void bakeBlendShapes() {
      _blendShapeAmounts = new List<float>(_elements.Count);

      List<Vector4> uv3 = new List<Vector4>();

      for (int elementID = 0; elementID < _elements.Count; elementID++) {
        var element = _elements[elementID];

        if (_enableBlendShapes && element.blendShape != null) {
          var blendShape = element.blendShape;

          element.GetMesh().vertices.Query().
            Zip(blendShape.vertices.Query(), (v0, v1) => {
              return element.transform.InverseTransformPoint(v1) - v0;
            }).
            Select(delta => new Vector4(delta.x, delta.y, delta.z, elementID)).
            AppendList(uv3);
        } else {
          element.GetMesh().vertices.Query().
            Select(v => new Vector4(0, 0, 0, elementID)).
            AppendList(uv3);
        }
      }

      _bakedMesh.SetUVs(3, uv3);
    }
#endif
    #endregion

    public enum MotionType {
      TranslationOnly,
      Full
    }

    public enum TintChannels {
      One,
      Two
    }

    public enum BlendShapeChannels {
      One,
      Two,
      Three
    }

    public enum ChannelType {
      Float,
      Vector,
      Color,
      Matrix
    }

    public enum BlendShapeSpace {
      Local,
      World
    }

    [Serializable]
    public class TextureChannel {
      public string propertyName;
      public UVChannel channel;
    }

    public enum UVChannel {
      UV0 = UVChannelFlags.UV0,
      UV1 = UVChannelFlags.UV1,
      UV2 = UVChannelFlags.UV2
    }

    [Serializable]
    public class AtlasSettings {
      [Range(0, 16)]
      public int border = 1;

      [MinValue(0)]
      public int padding;

      [MinValue(32)]
      [MaxValue(4096)]
      public int maximumAtlasSize = 2048;

      public FilterMode filterMode = FilterMode.Bilinear;

      public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    }
  }
}
