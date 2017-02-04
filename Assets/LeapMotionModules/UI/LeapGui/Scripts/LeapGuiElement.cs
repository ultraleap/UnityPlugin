using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class LeapGuiElement : MonoBehaviour {

  //Used to ensure that gui elements can be enabled/disabled
  void Start() { }

  [HideInInspector]
  public int elementId;

  [HideInInspector]
  public AnchorOfConstantSize anchor;

  [HideInInspector]
  [SerializeField]
  public List<LeapGuiElementData> data = new List<LeapGuiElementData>();

  protected void OnValidate() {
    //Delete any null references
    for (int i = data.Count; i-- != 0;) {
      if (data[i] == null) {
        data.RemoveAt(i);
      }
    }

    //Destroy any components that are not referenced by me
    var allComponents = GetComponents<LeapGuiElementData>();
    foreach (var component in allComponents) {
      if (!data.Contains(component)) {
        InternalUtility.Destroy(component);
      }
    }

#if UNITY_EDITOR
    for (int i = data.Count; i-- != 0;) {
      var component = data[i];
      if (component.gameObject != gameObject) {
        LeapGuiElementData movedData;
        if (InternalUtility.TryMoveComponent(component, gameObject, out movedData)) {
          data[i] = movedData;
        } else {
          Debug.LogWarning("Could not move component " + component + "!");
          InternalUtility.Destroy(component);
          data.RemoveAt(i);
        }
      }
    }
#endif
  }

  void OnDestroy() {
    foreach (var dataObj in data) {
      if (dataObj != null) InternalUtility.Destroy(dataObj);
    }
  }

#if UNITY_EDITOR
  /// <summary>
  /// At edit time a special mesh is set to each element so that they can be
  /// correctly picked in the scene view, even though their graphical 
  /// representation might be part of a different object.
  /// </summary>
  [NonSerialized]
  public Mesh pickingMesh;

  void OnDrawGizmos() {
    if (pickingMesh != null && pickingMesh.vertexCount != 0) {
      Gizmos.color = new Color(0, 0, 0, 0);
      Gizmos.DrawMesh(pickingMesh);
    }
  }
#endif
}
