using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiElement : MonoBehaviour {

  //Used to ensure that gui elements can be enabled/disabled
  void Start() { }

  [HideInInspector]
  public int elementId;

  [HideInInspector]
  public AnchorOfConstantSize anchor;

  [SerializeField]
  public List<LeapGuiElementData> data;


#if UNITY_EDITOR
  private Mesh _pickingMesh;

  /// <summary>
  /// At edit time a special mesh is set to each element so that they can be
  /// correctly picked in the scene view, even though their graphical 
  /// representation might be part of a different object.
  /// </summary>
  public void SetPickingMesh(Mesh mesh) {
    _pickingMesh = mesh;
  }

  void OnDrawGizmos() {
    if (_pickingMesh != null) {
      Gizmos.color = new Color(0, 0, 0, 0);
      Gizmos.DrawMesh(_pickingMesh);
    }
  }
#endif
}
