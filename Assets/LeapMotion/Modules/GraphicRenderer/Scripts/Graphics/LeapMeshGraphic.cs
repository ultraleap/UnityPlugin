/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// This class is a base class for all graphics that can be represented by a mesh object.
  /// </summary>
  [DisallowMultipleComponent]
  public abstract partial class LeapMeshGraphicBase : LeapGraphic {

    [Tooltip("The tint to apply to the vertex colors of this mesh.  This value cannot be changed " +
             "at edit time.  You can use a RuntimeTintFeature if you want to change the color of " +
             "an object at runtime.")]
    [EditTimeOnly]
    public Color vertexColor = Color.white;

    /// <summary>
    /// Returns the mesh that represents this graphic.  It can have any topology, any number of 
    /// uv channels, vertex colors, or normals.  Note that the specific rendering method you might 
    /// use might not support all of these features!
    /// </summary>
    public Mesh mesh { get; protected set; }

    /// <summary>
    /// Returns an enum mask that represents the union of all channels that are allowed to be remapped
    /// for this mesh.  If a uv channel is included in this mask, the matching uv coordinates are allowed
    /// to be remapped by any rendering method to implement texture atlasing.
    /// 
    /// You would exclude a uv channel from this mask if you dont want the uv coordinates to change in
    /// any way.  This can happen if your coordinates are already referencing a pre-built atlas, or if
    /// the values do not actually reference texture coordinates at all.
    /// </summary>
    public UVChannelFlags remappableChannels { get; protected set; }

    /// <summary>
    /// When this method is called, the mesh property and the remappableChannels property must be assigned
    /// to valid states correctly matching the current state of the object.  This is the method where you
    /// would do procedural mesh generation, or dynamic creations of mesh objects.
    /// </summary>
    public abstract void RefreshMeshData();

    public LeapMeshGraphicBase() {
#if UNITY_EDITOR
      editor = new MeshEditorApi(this);
#endif
    }
  }

  /// <summary>
  /// This class is the trivial implementation of LeapMeshGraphicBase.  It references a mesh asset
  /// directly to represent this graphic, and allows the user to directly specify which channels 
  /// are allowed to be remapped.
  /// </summary>
  public class LeapMeshGraphic : LeapMeshGraphicBase {

    [Tooltip("The mesh that will represent this graphic")]
    [EditTimeOnly]
    [SerializeField]
    private Mesh _mesh;

    [Tooltip("All channels that are allowed to be remapped into atlas coordinates.")]
    [EditTimeOnly]
    [EnumFlags]
    [SerializeField]
    private UVChannelFlags _remappableChannels = UVChannelFlags.UV0 |
                                                 UVChannelFlags.UV1 |
                                                 UVChannelFlags.UV2 |
                                                 UVChannelFlags.UV3;

    /// <summary>
    /// Call this method at edit time or at runtime to set the specific mesh to be used to
    /// represent this graphic.  This method cannot fail at edit time, but might fail at runtime
    /// if the attached group does not support changing the representation of a graphic dynamically.
    /// </summary>
    public void SetMesh(Mesh mesh) {
      if (isAttachedToGroup && !attachedGroup.addRemoveSupported) {
        Debug.LogWarning("Changing the representation of the graphic is not supported by this rendering type");
      }

      isRepresentationDirty = true;
      _mesh = mesh;
    }

    public override void RefreshMeshData() {
      //For the trivial MeshGraphic, simply copy data into the properties.
      mesh = _mesh;
      remappableChannels = _remappableChannels;
    }
  }
}
