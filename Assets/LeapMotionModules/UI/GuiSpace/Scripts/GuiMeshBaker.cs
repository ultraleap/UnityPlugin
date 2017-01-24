using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
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

    [Disable] //TODO, make this work!
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
    [Disable] //TODO, make this work!
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
    [Disable] //TODO: make this work!
    [SerializeField]
    private BlendShapeSpace _blendShapeSpace = BlendShapeSpace.Local;

    #endregion

    [SerializeField]
    private Mesh _bakedMesh;

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

      initData();
    }

#if UNITY_EDITOR
    void Update() {
      if (!Application.isPlaying) {
        bakeMesh();
        initData();
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

    private void initData() {
      if (Application.isPlaying) {
        //Can only access atlas during playmode
        for (int i = 0; i < _textureChannels.Length; i++) {
          _material.SetTexture(_textureChannels[i].propertyName, _elements[0].GetSprite(i).texture);
        }
      }

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
    private void setupMaterial() {
      //Have to destroy and re-create.  When you set an array, it fixes the size
      //and prevents it from growing.  This in turn prevents additional elements
      //from being properly represented.
      if (_material != null) {
        DestroyImmediate(_material);
      }

      _material = new Material(_shader);
      _material.name = "GUI Space Material";
      _renderer.sharedMaterial = _material;
      if (_renderer.sharedMaterial != _material)

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
        string atlasName;
        Texture2D atlasTexture;
        Packer.GetAtlasDataForSprite(_elements[0].GetSprite(i), out atlasName, out atlasTexture);

        //Gotta make a copy and mark it as 'dont save' or else unity will yell at us
        //because the atlas texture cannot be serialized
        var copy = new Texture2D(atlasTexture.width, atlasTexture.height, atlasTexture.format, atlasTexture.mipmapCount > 1);
        copy.hideFlags = HideFlags.HideAndDontSave;
        copy.wrapMode = atlasTexture.wrapMode;
        copy.filterMode = atlasTexture.filterMode;
        Graphics.CopyTexture(atlasTexture, copy);

        _material.SetTexture(_textureChannels[i].propertyName, copy);
      }
    }

    private void bakeMesh() {
      Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget);

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

      //Use the packed uvs to bake out uv coordinates into the baked mesh
      bakeUvs();

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
        if (element != null) {
          element.elementId = _elements.Count;
          _elements.Add(element);
        }

        bakeElementHierarchy(child);
      }
    }

    private void bakeVerts() {
      List<Vector3> verts = new List<Vector3>();
      List<int> tris = new List<int>();

      foreach (var element in _elements) {
        var elementMesh = element.GetMesh();
        if (elementMesh == null) continue;

        var elementTransform = element.transform;

        elementMesh.GetIndices(0).Query().Select(i => i + verts.Count).AppendList(tris);

        //TODO: bake out space if no motion is enabled
        elementMesh.vertices.Query().Select(v => {
          return elementVertToBakedVert(element.transform, v);
        }).AppendList(verts);
      }

      _bakedMesh.SetVertices(verts);
      _bakedMesh.SetTriangles(tris, 0);
    }

    private void bakeUvs() {
      List<Vector2> tempUvs = new List<Vector2>();
      List<Vector2> remappedUvs = new List<Vector2>();
      for (int texIndex = 0; texIndex < _textureChannels.Length; texIndex++) {
        remappedUvs.Clear();
        foreach (var element in _elements) {
          Mesh mesh = element.GetMesh();
          if (mesh == null) continue;

          Sprite sprite = element.GetSprite(texIndex);

          mesh.GetUVs(texIndex, tempUvs);

          if (sprite != null &&
              sprite.packed &&
              sprite.packingMode == SpritePackingMode.Rectangle &&
              !element.DoesMeshHaveAtlasUvs(texIndex)) {
            Vector2[] uvs = SpriteUtility.GetSpriteUVs(sprite, getAtlasData: true);

            tempUvs.Query().Select(uv => new Vector2(Mathf.Lerp(uvs[0].x, uvs[1].x, uv.x),
                                                     Mathf.Lerp(uvs[0].y, uvs[2].y, uv.y))).
                            AppendList(remappedUvs);
            continue;
          } else {
            remappedUvs.AddRange(tempUvs);
          }
        }

        _bakedMesh.SetUVs(texIndex, remappedUvs);
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

        var mesh = element.GetMesh();
        if (mesh == null) continue;

        if (_enableBlendShapes && element.blendShape != null) {
          var blendShape = element.blendShape;

          mesh.vertices.Query().
            Zip(blendShape.vertices.Query(), (elementVert, shapeVert) => {
              return elementVertToBakedVert(blendShape.transform, shapeVert) - elementVertToBakedVert(element.transform, elementVert);
            }).
            Select(delta => new Vector4(delta.x, delta.y, delta.z, elementID)).
            AppendList(uv3);
        } else {
          mesh.vertices.Query().
            Select(v => new Vector4(0, 0, 0, elementID)).
            AppendList(uv3);
        }
      }

      _bakedMesh.SetUVs(3, uv3);
    }

    private Vector3 elementVertToBakedVert(Transform elementTransform, Vector3 vert) {
      vert = elementTransform.TransformPoint(vert);
      vert = transform.InverseTransformPoint(vert);
      vert -= transform.InverseTransformPoint(elementTransform.position);
      return vert;
    }

#endif
    #endregion

    public enum MotionType {
      TranslationOnly,
      Full
    }

    public enum BlendShapeSpace {
      Local,
      World
    }

    [Serializable]
    public class TextureChannel {
      [Disable] //TODO: more logic for this!
      public string propertyName;
      [Disable] //TODO: make this work!
      public UVChannel channel;
    }

    public enum UVChannel {
      UV0 = UVChannelFlags.UV0,
      UV1 = UVChannelFlags.UV1,
      UV2 = UVChannelFlags.UV2
    }
  }
}
