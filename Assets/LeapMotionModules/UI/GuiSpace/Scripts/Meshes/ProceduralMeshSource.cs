using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gui.Space {

  [RequireComponent(typeof(LeapElement))]
  public abstract class ProceduralMeshSource : MonoBehaviour {

    // Just to make sure all procedural mesh sources can be enabled/disabled
    protected virtual void Start() { }

    /// <summary>
    /// Returns whether or not this generator can generate a mesh for the given
    /// element.  This method can return false if the element does not have 
    /// the correct data needed in order for accurate mesh generation.  If 
    /// no procedural source is found that can provide a mesh, the user must 
    /// provide a mesh asset to the gui element directly.
    /// </summary>
    public abstract bool CanGenerateMeshForElement(LeapElement element);

    /// <summary>
    /// Returns a new mesh for the provided gui element.
    /// </summary>
    public abstract Mesh GetMesh(LeapElement element);

    /// <summary>
    /// Returns whether or not the Mesh object returned by GetMesh has 
    /// uvs that are in atlas space.  If true, the uvs will map directly
    /// to the atlas texture.  If false, they are in "global space" and will
    /// need to be mapped into atlas space by the baker.  This cannot
    /// be done in all cases (like if the texture is a tightly packed sprite).
    /// </summary>
    public abstract bool DoesMeshHaveAtlasUvs(int uvChannel);
  }
}
