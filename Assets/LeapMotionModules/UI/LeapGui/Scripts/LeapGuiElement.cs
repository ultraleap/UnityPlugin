using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class LeapGuiElement : MonoBehaviour {

  #region INSPECTOR FIELDS
  [HideInInspector]
  public int elementId;

  [HideInInspector]
  public AnchorOfConstantSize anchor;

  [HideInInspector]
  [SerializeField]
  public List<LeapGuiElementData> data = new List<LeapGuiElementData>();

  [HideInInspector]
  [SerializeField]
  public LeapGui attachedGui;
  #endregion

  #region PRIVATE VARIABLES
  /// <summary>
  /// At edit time a special mesh is set to each element so that they can be
  /// correctly picked in the scene view, even though their graphical 
  /// representation might be part of a different object.
  /// </summary>
#if UNITY_EDITOR
  [NonSerialized]
  private Mesh _pickingMesh;
#endif
  #endregion

  #region PUBLIC API
  public bool IsAttachedToGui {
    get {
      return attachedGui != null;
    }
  }

#if UNITY_EDITOR
  public Mesh pickingMesh {
    get {
      return _pickingMesh;
    }
    set {
      _pickingMesh = value;
    }
  }
#endif

  public virtual void OnAttachedToGui(LeapGui gui, AnchorOfConstantSize anchor, int elementId) {
    attachedGui = gui;
    this.anchor = anchor;
    this.elementId = elementId;
  }

  public virtual void OnDetachedFromGui() {
    attachedGui = null;
  }
  #endregion

  #region UNITY MESSAGES
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

  void OnEnable() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
#endif
      if (!IsAttachedToGui) {
        GetComponentInParent<LeapGui>().TryAddElement(this);
      }
#if UNITY_EDITOR
    }
#endif
  }

  void OnDisable() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
#endif
      if (IsAttachedToGui) {
        attachedGui.TryRemoveElement(this);
      }
#if UNITY_EDITOR
    }
#endif
  }

#if UNITY_EDITOR
  void OnDrawGizmos() {
    if (_pickingMesh != null && _pickingMesh.vertexCount != 0) {
      Gizmos.color = new Color(0, 0, 0, 0);
      Gizmos.DrawMesh(_pickingMesh);
    }
  }
#endif
  #endregion
}
