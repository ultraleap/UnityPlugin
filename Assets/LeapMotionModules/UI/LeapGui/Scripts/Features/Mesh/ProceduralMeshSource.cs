using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LeapGuiElement))]
public abstract class ProceduralMeshSource : MonoBehaviour {

  // Just to make sure all procedural mesh sources can be enabled/disabled
  protected virtual void Start() { }

  /// <summary>
  /// Returns whether or not this generator can generate a mesh for the given
  /// feature.  This method can return false if the generator does not have 
  /// the correct data needed in order for accurate mesh generation.  If 
  /// no procedural source is found that can provide a mesh, the user must 
  /// provide a mesh asset to the gui element directly.
  /// </summary>
  public abstract bool CanGenerateMeshForElement(LeapGuiMeshData meshFeature);

  /// <summary>
  /// Returns a new mesh for the provided gui feature.
  /// </summary>
  public abstract Mesh GetMesh(LeapGuiMeshData meshFeature);
}