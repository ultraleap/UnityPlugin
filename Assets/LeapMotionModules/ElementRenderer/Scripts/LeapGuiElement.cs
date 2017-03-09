using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class LeapGuiElement : MonoBehaviour {

  #region INSPECTOR FIELDS
  [SerializeField, HideInInspector]
  private int _elementId;

  [SerializeField, HideInInspector]
  private Transform _anchor;

  [SerializeField, HideInInspector]
  private List<LeapGuiElementData> _data = new List<LeapGuiElementData>();

  [SerializeField, HideInInspector]
  private LeapGuiGroup _attachedGroup;
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
  public int elementId {
    get {
      return _elementId;
    }
  }

  public Transform anchor {
    get {
      return _anchor;
    }
  }

  public List<LeapGuiElementData> data {
    get {
      return _data;
    }
  }

  public LeapGuiGroup attachedGroup {
    get {
      return _attachedGroup;
    }
  }

  public bool IsAttachedToGroup {
    get {
      return _attachedGroup != null;
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

  public virtual void OnAttachedToGui(LeapGuiGroup group, Transform anchor, int elementId) {
    _attachedGroup = group;
    _anchor = anchor;
    _elementId = elementId;
  }

  public virtual void OnDetachedFromGui() {
    _attachedGroup = null;
    _anchor = null;
    _elementId = -1;
  }

  public virtual void OnAssignFeatureData(List<LeapGuiElementData> data) {
    _data = data;
  }
  #endregion

  #region UNITY CALLBACKS
  protected void OnValidate() {
    //Delete any null references
    for (int i = _data.Count; i-- != 0;) {
      if (_data[i] == null) {
        _data.RemoveAt(i);
      }
    }

    //Destroy any components that are not referenced by me
    var allComponents = GetComponents<LeapGuiElementData>();
    foreach (var component in allComponents) {
      if (!_data.Contains(component)) {
        InternalUtility.Destroy(component);
      }
    }

#if UNITY_EDITOR
    for (int i = _data.Count; i-- != 0;) {
      var component = _data[i];
      if (component.gameObject != gameObject) {
        LeapGuiElementData movedData;
        if (InternalUtility.TryMoveComponent(component, gameObject, out movedData)) {
          _data[i] = movedData;
        } else {
          Debug.LogWarning("Could not move component " + component + "!");
          InternalUtility.Destroy(component);
          _data.RemoveAt(i);
        }
      }
    }
#endif

    foreach (var dataObj in _data) {
      dataObj.element = this;
    }
  }

  void OnDestroy() {
    foreach (var dataObj in _data) {
      if (dataObj != null) InternalUtility.Destroy(dataObj);
    }
  }

  void OnEnable() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
#endif
      if (!IsAttachedToGroup) {
        //TRY connect at runtime, TODO!
      }
#if UNITY_EDITOR
    }
#endif
  }

  void OnDisable() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
#endif
      if (IsAttachedToGroup) {
        _attachedGroup.TryRemoveElement(this);
      }
#if UNITY_EDITOR
    }
#endif
  }

#if UNITY_EDITOR
  void OnDrawGizmos() {
    if (_pickingMesh != null && _pickingMesh.vertexCount != 0) {
      Gizmos.color = new Color(1, 0, 0, 0);
      Gizmos.DrawMesh(_pickingMesh);
    }
  }
#endif
  #endregion
}
