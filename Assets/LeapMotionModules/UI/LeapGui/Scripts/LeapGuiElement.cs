using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiElement : MonoBehaviour {

  //Used to ensure that gui elements can be enabled/disabled
  void Start() { }

  [HideInInspector]
  public int elementId;

  //[HideInInspector]
  public AnchorOfConstantSize anchor;

  [HideInInspector]
  [SerializeField]
  public List<LeapGuiElementData> data;

#if UNITY_EDITOR
  /// <summary>
  /// At edit time a special mesh is set to each element so that they can be
  /// correctly picked in the scene view, even though their graphical 
  /// representation might be part of a different object.
  /// </summary>
  [NonSerialized]
  public Mesh pickingMesh;

  void OnDrawGizmos() {
    if (pickingMesh != null) {
      Gizmos.color = new Color(0, 0, 0, 0);
      Gizmos.DrawMesh(pickingMesh);
    }
  }
#endif
}
