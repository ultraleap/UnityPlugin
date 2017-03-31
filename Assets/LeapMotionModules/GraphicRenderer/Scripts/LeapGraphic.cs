using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Space;
using Leap.Unity.Query;

[ExecuteInEditMode]
[DisallowMultipleComponent]
public abstract partial class LeapGraphic : MonoBehaviour, ISpaceComponent {

  #region INSPECTOR FIELDS
  [SerializeField, HideInInspector]
  protected LeapSpaceAnchor _anchor;

  [SerializeField, HideInInspector]
  protected List<LeapFeatureData> _featureData = new List<LeapFeatureData>();

  [SerializeField, HideInInspector]
  protected LeapGraphicGroup _attachedGroup;

  [SerializeField, HideInInspector]
  protected SerializableType _preferredRendererType;
  #endregion

  #region PUBLIC API

  // Used only by the renderer, gets set to true if the representation
  // of this graphic might change.  Should get reset to false by the renderer
  // once the representation is up to date.
  [NonSerialized]
  private bool _isRepresentationDirty = true;
  public bool isRepresentationDirty {
    get {
#if UNITY_EDITOR
      if (Application.isPlaying) {
#endif
        return _isRepresentationDirty;
#if UNITY_EDITOR
      } else {
        return true;
      }
#endif
    }
    set {
      _isRepresentationDirty = value;
    }
  }

  public LeapSpaceAnchor anchor {
    get {
      return _anchor;
    }
  }

  public ITransformer transformer {
    get {
      return _anchor == null ? IdentityTransformer.single : _anchor.transformer;
    }
  }

  public List<LeapFeatureData> featureData {
    get {
      return _featureData;
    }
  }

  public LeapGraphicGroup attachedGroup {
    get {
      return _attachedGroup;
    }
  }

  public bool isAttachedToGroup {
    get {
      return _attachedGroup != null;
    }
  }

  public Type preferredRendererType {
    get {
      return _preferredRendererType;
    }
  }

  public T GetFirstFeatureData<T>() where T : LeapFeatureData {
    T dataObj = _featureData.Query().OfType<T>().FirstOrDefault();
    if (dataObj == null) {
      throw new InvalidOperationException("The graphic " + this + " does not have a feature " + typeof(T).Name + ".");
    }
    return dataObj;
  }

  public virtual void OnAttachedToGroup(LeapGraphicGroup group, LeapSpaceAnchor anchor) {
#if UNITY_EDITOR
    editor.OnAttachedToGroup(group, anchor);
#endif

    _attachedGroup = group;
    _anchor = anchor;
  }

  public virtual void OnDetachedFromGroup() {
    _attachedGroup = null;
    _anchor = null;

    foreach (var dataObj in featureData) {
      dataObj.feature = null;
    }
  }

  public virtual void OnAssignFeatureData(List<LeapFeatureData> data) {
    _featureData = data;
  }
  #endregion

  #region UNITY CALLBACKS
  protected virtual void OnValidate() {
    isRepresentationDirty = true;

    //Delete any null references
    for (int i = _featureData.Count; i-- != 0;) {
      if (_featureData[i] == null) {
        _featureData.RemoveAt(i);
      }
    }

    //Destroy any components that are not referenced by me
    var allComponents = GetComponents<LeapFeatureData>();
    foreach (var component in allComponents) {
      if (!_featureData.Contains(component)) {
        InternalUtility.Destroy(component);
      }
    }

    foreach (var dataObj in _featureData) {
      dataObj.graphic = this;
    }

#if UNITY_EDITOR
    editor.OnValidate();
#endif
  }

  protected virtual void OnDestroy() {
    foreach (var dataObj in _featureData) {
      if (dataObj != null) InternalUtility.Destroy(dataObj);
    }
  }

  protected virtual void OnEnable() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
#endif
      if (!isAttachedToGroup) {
        var parentRenderer = GetComponentInParent<LeapGraphicRenderer>();
        if (parentRenderer != null) {
          parentRenderer.TryAddGraphic(this);
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
      if (!isAttachedToGroup) {
        var parentRenderer = GetComponentInParent<LeapGraphicRenderer>();
        if (parentRenderer != null) {
          parentRenderer.TryAddGraphic(this);
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
      if (isAttachedToGroup) {
        _attachedGroup.TryRemoveGraphic(this);
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

#if UNITY_EDITOR
  protected LeapGraphic() {
    editor = new EditorApi(this);
  }

  protected LeapGraphic(EditorApi editor) {
    this.editor = editor;
  }
#endif

  #endregion
}
