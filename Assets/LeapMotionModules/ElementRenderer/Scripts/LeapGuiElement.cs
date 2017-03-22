using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public abstract partial class LeapGuiElement : MonoBehaviour {

  #region INSPECTOR FIELDS
  [SerializeField, HideInInspector]
  protected Transform _anchor;

  [SerializeField, HideInInspector]
  protected List<LeapGuiElementData> _data = new List<LeapGuiElementData>();

  [SerializeField, HideInInspector]
  protected LeapGuiGroup _attachedGroup;

  [SerializeField, HideInInspector]
  protected SerializableType _preferredRendererType;
  #endregion

  #region PRIVATE VARIABLES
  /// <summary>
  /// At edit time a special mesh is set to each element so that they can be
  /// correctly picked in the scene view, even though their graphical 
  /// representation might be part of a different object.
  /// </summary>
#if UNITY_EDITOR
  [NonSerialized]
  protected Mesh _pickingMesh;
#endif
  #endregion

  #region PUBLIC API
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

  public Type preferredRendererType {
    get {
      return _preferredRendererType;
    }
  }

  public virtual void OnAttachedToGui(LeapGuiGroup group, Transform anchor) {
#if UNITY_EDITOR
    editor.OnAttachedToGui(group, anchor);
#endif

    _attachedGroup = group;
    _anchor = anchor;
  }

  public virtual void OnDetachedFromGui() {
    _attachedGroup = null;
    _anchor = null;

    foreach (var dataObj in data) {
      dataObj.feature = null;
    }
  }

  public virtual void OnAssignFeatureData(List<LeapGuiElementData> data) {
    _data = data;
  }
  #endregion

  #region UNITY CALLBACKS
  protected virtual void OnValidate() {
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

    foreach (var dataObj in _data) {
      dataObj.element = this;
    }

#if UNITY_EDITOR
    editor.OnValidate();
#endif
  }

  protected virtual void OnDestroy() {
    foreach (var dataObj in _data) {
      if (dataObj != null) InternalUtility.Destroy(dataObj);
    }
  }

  protected virtual void OnEnable() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
#endif
      if (!IsAttachedToGroup) {
        var parentGui = GetComponentInParent<LeapGui>();
        if (parentGui != null) {
          parentGui.TryAddElement(this);
        }
      }
#if UNITY_EDITOR
    }
#endif
  }

  protected virtual void Start() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
#endif
      if (!IsAttachedToGroup) {
        var parentGui = GetComponentInParent<LeapGui>();
        if (parentGui != null) {
          parentGui.TryAddElement(this);
        }
      }
#if UNITY_EDITOR
    }
#endif
  }

  protected virtual void OnDisable() {
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

  protected virtual void OnDrawGizmos() {
#if UNITY_EDITOR
    editor.OnDrawGizmos();
#endif
  }
  #endregion

  #region PRIVATE IMPLEMENTATION

  protected LeapGuiElement() {
    editor = new EditorApi(this);
  }

  protected LeapGuiElement(EditorApi editor) {
    this.editor = editor;
  }

  #endregion
}
