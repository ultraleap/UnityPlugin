/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attributes;
using Leap.Unity.Query;
using UnityEngine;
using UnityEngine.Rendering;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// The base class for LeapPanelGraphic, LeapBoxGraphic, and similar generators.
  /// </summary>
  /// <details>
  /// This base class represents shared code when constructing graphics that:
  /// - Are generally rectangular, supporting dynamic tesselation along the X and Y axes
  /// - Can be intuitively controlled by an associated RectTransform
  /// - Support nine-slicing when using a Sprite as a source for Texture data
  /// </details>
  [DisallowMultipleComponent]
  public abstract class LeapSlicedGraphic : LeapMeshGraphicBase {

    #region Constants

    public const int MAX_RESOLUTION = 128;

    #endregion

    #region Inspector

    [EditTimeOnly]
    [SerializeField]
    protected int _sourceDataIndex = -1;

    //************//
    // Resolution //

    [Tooltip("Specifies whether or not this panel has a specific resolution, or whether this " +
             "panel automatically changes its resolution based on its size")]
    [EditTimeOnly]
    [SerializeField]
    protected ResolutionType _resolutionType = ResolutionType.VerticesPerRectilinearMeter;

    [HideInInspector]
    [SerializeField]
    protected int _resolution_vert_x = 4, _resolution_vert_y = 4;

    [EditTimeOnly]
    [SerializeField]
    protected Vector2 _resolution_verts_per_meter = new Vector2(20, 20);

    [MinValue(0)]
    [EditTimeOnly]
    [SerializeField]
    protected Vector2 _size = new Vector2(0.1f, 0.1f);

    //**************//
    // Nine Slicing //

    [Tooltip("Uses sprite data to generate a nine sliced panel.")]
    [EditTimeOnly]
    [SerializeField]
    protected bool _nineSliced = false;

    #endregion

    #region Properties

    /// <summary>
    /// Returns the current resolution type being used for this panel.
    /// </summary>
    public ResolutionType resolutionType {
      get {
        return _resolutionType;
      }
    }

    /// <summary>
    /// Returns the current local-space rect of this panel.  If there is a 
    /// RectTransform attached to this panel, this value is the same as calling
    /// rectTransform.rect.
    /// </summary>
    public Rect rect {
      get {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) {
          _size = rectTransform.rect.size;
          return rectTransform.rect;
        }
        else {
          return new Rect(-_size / 2, _size);
        }
      }
    }

    /// <summary>
    /// Gets or sets whether or not this panel is currently using nine slicing.
    /// </summary>
    public bool nineSliced {
      get {
        return _nineSliced && canNineSlice;
      }
      set {
        _nineSliced = value;
        setSourceFeatureDirty();
      }
    }

    /// <summary>
    /// Returns whether or not the current source supports nine slicing.
    /// </summary>
    public bool canNineSlice {
      get {
        var spriteData = sourceData as LeapSpriteData;
        return spriteData != null && spriteData.sprite != null;
      }
    }

    /// <summary>
    /// Returns which uv channel is being used for this panel.  It will
    /// always match the uv channel being used by the source.
    /// </summary>
    public UVChannelFlags uvChannel {
      get {
        if (sourceData == null) {
          return UVChannelFlags.UV0;
        }

        var feature = sourceData.feature;
        if (feature is LeapTextureFeature) {
          return (feature as LeapTextureFeature).channel;
        }
        else if (feature is LeapSpriteFeature) {
          return (feature as LeapSpriteFeature).channel;
        }
        else {
          return UVChannelFlags.UV0;
        }
      }
    }

    #endregion

    #region Unity Events

    protected override void Reset() {
      base.Reset();

      assignDefaultSourceValue();
      setSourceFeatureDirty();
    }

    protected override void OnValidate() {
      base.OnValidate();

      if (sourceData == null) {
        assignDefaultSourceValue();
      }

      _resolution_vert_x = Mathf.Max(0, _resolution_vert_x);
      _resolution_vert_y = Mathf.Max(0, _resolution_vert_y);
      _resolution_verts_per_meter = Vector2.Max(_resolution_verts_per_meter, Vector2.zero);

      if (_resolutionType == ResolutionType.Vertices) {
        _resolution_verts_per_meter.x = _resolution_vert_x / rect.width;
        _resolution_verts_per_meter.y = _resolution_vert_y / rect.height;
      }
      else {
        _resolution_vert_x = Mathf.RoundToInt(_resolution_verts_per_meter.x * rect.width);
        _resolution_vert_y = Mathf.RoundToInt(_resolution_verts_per_meter.y * rect.height);
      }

      setSourceFeatureDirty();
    }

    #endregion

    #region Leap Graphic

    /// <summary>
    /// Returns the current feature data object being used as source.
    /// </summary>
    public LeapFeatureData sourceData {
      get {
        if (_sourceDataIndex == -1) {
          assignDefaultSourceValue();
        }
        if (_sourceDataIndex < 0 || _sourceDataIndex >= featureData.Count) {
          return null;
        }
        return featureData[_sourceDataIndex];
      }
#if UNITY_EDITOR
      set {
        _sourceDataIndex = _featureData.IndexOf(value);
        setSourceFeatureDirty();
      }
#endif
    }

    /// <summary>
    /// Returns whether or not a feature data object is a valid object 
    /// that can be used to drive texture data for this panel.  Only
    /// a TextureData object or a SpriteData object are currently valid.
    /// </summary>
    public static bool IsValidDataSource(LeapFeatureData dataSource) {
      return dataSource is LeapTextureData ||
             dataSource is LeapSpriteData;
    }

    protected void assignDefaultSourceValue() {
      _sourceDataIndex = featureData.Query().IndexOf(IsValidDataSource);
    }

    protected void setSourceFeatureDirty() {
      if (sourceData != null) {
        sourceData.MarkFeatureDirty();
      }
    }

    #endregion

    #region Leap Mesh Graphic

    public override void RefreshMeshData() {
      if (sourceData == null) {
        assignDefaultSourceValue();
      }

      Vector4 borderSize = Vector4.zero;
      Vector4 borderUvs = Vector4.zero;

      Rect rect;
      RectTransform rectTransform = GetComponent<RectTransform>();
      if (rectTransform != null) {
        rect = rectTransform.rect;
        _size = rect.size;
      }
      else {
        rect = new Rect(-_size / 2, _size);
      }

      if (_nineSliced && sourceData is LeapSpriteData) {
        var spriteData = sourceData as LeapSpriteData;
        if (spriteData.sprite == null) {
          mesh = null;
          remappableChannels = 0;
          return;
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

      int vertsX, vertsY;
      if (_resolutionType == ResolutionType.Vertices) {
        vertsX = Mathf.RoundToInt(_resolution_vert_x);
        vertsY = Mathf.RoundToInt(_resolution_vert_y);
      }
      else {
        vertsX = Mathf.RoundToInt(rect.width * _resolution_verts_per_meter.x);
        vertsY = Mathf.RoundToInt(rect.height * _resolution_verts_per_meter.y);
      }

      vertsX += _nineSliced ? 4 : 2;
      vertsY += _nineSliced ? 4 : 2;

      vertsX = Mathf.Min(vertsX, MAX_RESOLUTION);
      vertsY = Mathf.Min(vertsY, MAX_RESOLUTION);

      RefreshSlicedMeshData(new Vector2i() { x = vertsX, y = vertsY },
                            borderSize,
                            borderUvs);
    }

    #endregion

    /// <summary>
    /// Set the mesh property equal to the correct mesh given the Sliced Graphic's
    /// current settings.
    /// 
    /// Resolution along the X and Y axes are provided, as well as mesh-space and 
    /// UV-space margins to pass into calculateVertAxis as border0 and border1 to support
    /// nine slicing (see Mesh Data Support).
    /// </summary>
    public abstract void RefreshSlicedMeshData(Vector2i    resolution,
                                               RectMargins meshMargins,
                                               RectMargins uvMargins);

    #region Mesh Data Support

    /// <summary>
    /// Given a vertex index from an edge, the total vertCount and size
    /// along the current dimension, and the distance to the beginning and end borders
    /// along the current dimension (if nine-slicing is enabled ONLY), this method will
    /// return the distance along the current dimension that the vertIdx should be from
    /// the origin of the dimension.
    /// 
    /// If the sliced graphic has nine-slicing enabled, or if alwaysRespectBorder is 
    /// set to true, the second and second-to-last vertices along an edge will reflect
    /// the border margin arguments instead of a uniform tesselation along the whole
    /// width of the dimension. Inner vertices will then respect this non-uniformity,
    /// slicing the rest of the inner width evenly.
    /// 
    /// e.g. Determine the distance along the X axis for:
    /// - vertIdx = 4, the fifth vertex along the X axis
    /// - vertCount = 16, given sixteen total grid columns (16 verts per X-axis row)
    /// - size = 2, given a box width of 2 units
    /// - border0 = 0.2f, given the left border starts at 0.2 units from the left edge
    /// - border1 = 0.2f, given the right border starts at 0.2 units from the right edge
    /// This method will return the X axis coordinate of the fifth vertex accordingly.
    /// To calculate the corresponding Y axis coordinate, simply provide the vertCount,
    /// size, and border information for the Y dimension instead.
    /// </summary>
    protected float calculateVertAxis(int vertIdx, int vertCount, float size, float border0, float border1, bool alwaysRespectBorder = false) {
      if (_nineSliced || alwaysRespectBorder) {
        if (vertIdx == 0) {
          return 0;
        }
        else if (vertIdx == (vertCount - 1)) {
          return size;
        }
        else if (vertIdx == 1) {
          return border0;
        }
        else if (vertIdx == (vertCount - 2)) {
          return size - border1;
        }
        else {
          return ((vertIdx - 1.0f) / (vertCount - 3.0f)) * (size - border0 - border1) + border0;
        }
      }
      else {
        return (vertIdx / (vertCount - 1.0f)) * size;
      }
    }

    #endregion

    #region Supporting Types

    public enum ResolutionType {
      Vertices,
      VerticesPerRectilinearMeter
    }

    public struct Vector2i {
      public int x;
      public int y;
    }
    
    public struct RectMargins {
      /// <summary> Margin width from the left edge. </summary>
      public float left;

      /// <summary> Margin width from the top edge. </summary>
      public float top;

      /// <summary> Margin width from the right edge. </summary>
      public float right;

      /// <summary> Margin width from the bottom edge. </summary>
      public float bottom;

      public RectMargins(float left, float top, float right, float bottom) {
        this.left = left; this.top = top; this.right = right; this.bottom = bottom;
      }

      public static implicit operator Vector4(RectMargins m) {
        return new Vector4(m.left, m.top, m.right, m.bottom);
      }

      public static implicit operator RectMargins(Vector4 v) {
        return new RectMargins(v.x, v.y, v.z, v.w);
      }
    }

    #endregion

  }

}
