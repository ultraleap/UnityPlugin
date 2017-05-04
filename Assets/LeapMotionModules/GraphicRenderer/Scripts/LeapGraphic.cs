/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity;
using Leap.Unity.Space;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [ExecuteInEditMode]
  [DisallowMultipleComponent]
  public abstract partial class LeapGraphic : MonoBehaviour, ISpaceComponent, IEquatable<LeapGraphic>, ISerializationCallbackReceiver {

    #region INSPECTOR FIELDS
    [SerializeField]
    protected LeapSpaceAnchor _anchor;

    [SerializeField]
    protected FeatureDataList _featureData = new FeatureDataList();

    [SerializeField]
    protected LeapGraphicRenderer _attachedRenderer;

    [SerializeField]
    protected int _attachedGroupIndex = -1;

    [SerializeField]
    protected SerializableType _preferredRendererType;
    #endregion

    #region PUBLIC API

    [NonSerialized]
    private NotificationState _notificationState = NotificationState.None;
    [NonSerialized]
    private LeapGraphicGroup _groupToBeAttachedTo = null;

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

    public MultiTypedList<LeapFeatureData> featureData {
      get {
        return _featureData;
      }
    }

    public LeapGraphicGroup attachedGroup {
      get {
        if (_attachedRenderer == null) {
          return null;
        } else {
#if UNITY_EDITOR
          if (_attachedGroupIndex < 0 || _attachedGroupIndex >= _attachedRenderer.groups.Count) {
            _attachedRenderer = null;
            _attachedGroupIndex = -1;
            return null;
          }
#endif
          return _attachedRenderer.groups[_attachedGroupIndex];
        }
      }
    }

    public bool isAttachedToGroup {
      get {
        return attachedGroup != null;
      }
    }

    public Type preferredRendererType {
      get {
        return _preferredRendererType;
      }
    }

    public T GetFirstFeatureData<T>() where T : LeapFeatureData {
      for (int i = 0; i < _featureData.Count; i++) {
        var data = _featureData[i];
        if (data is T) {
          return data as T;
        }
      }

      throw new InvalidOperationException("The graphic " + this + " does not have a feature " + typeof(T).Name + ".");
    }

    /// <summary>
    /// Called to notify that this graphic will be attached within the next frame.
    /// This is only called at runtime.
    /// </summary>
    public virtual void NotifyWillbeAttached(LeapGraphicGroup toBeAttachedTo) {
      Assert.AreEqual(_notificationState, NotificationState.None);
      Assert.IsNull(_groupToBeAttachedTo);

      _notificationState = NotificationState.WillBeAttached;
      _groupToBeAttachedTo = toBeAttachedTo;
    }

    /// <summary>
    /// Called to notify that a previous notification that this graphic would be
    /// attached has been canceled due to a call to TryRemoveGraphic.
    /// </summary>
    public virtual void CancelWillbeAttached() {
      Assert.AreEqual(_notificationState, NotificationState.WillBeAttached);
      Assert.IsNotNull(_groupToBeAttachedTo);

      _notificationState = NotificationState.None;
      _groupToBeAttachedTo = null;
    }

    /// <summary>
    /// Called to notify that this graphic will be detached within the next frame.
    /// This is only called at runtime.
    /// </summary>
    public virtual void NotifyWillBeDetached(LeapGraphicGroup toBeDetachedFrom) {
      Assert.AreEqual(_notificationState, NotificationState.None);
      _notificationState = NotificationState.WillBeDetached;
    }

    /// <summary>
    /// Called to notify that a previous notification that this graphic would be
    /// detached has been canceled due to a call to TryAddGraphic.
    /// </summary>
    public virtual void CancelWillBeDetached() {
      Assert.AreEqual(_notificationState, NotificationState.WillBeDetached);
      _notificationState = NotificationState.None;
    }

    public virtual void OnAttachedToGroup(LeapGraphicGroup group, LeapSpaceAnchor anchor) {
#if UNITY_EDITOR
      editor.OnAttachedToGroup(group, anchor);
#endif
      _notificationState = NotificationState.None;
      _groupToBeAttachedTo = null;

      _attachedRenderer = group.renderer;
      _attachedGroupIndex = _attachedRenderer.groups.IndexOf(group);
      _anchor = anchor;

      patchReferences();
    }

    public virtual void OnDetachedFromGroup() {
      _notificationState = NotificationState.None;

      _attachedRenderer = null;
      _attachedGroupIndex = -1;
      _anchor = null;

      for (int i = 0; i < _featureData.Count; i++) {
        _featureData[i].feature = null;
      }
    }

    public virtual void OnAssignFeatureData(List<LeapFeatureData> data) {
      _featureData.Clear();
      foreach (var dataObj in data) {
        _featureData.Add(dataObj);
      }
    }

    public bool Equals(LeapGraphic other) {
      return GetInstanceID() == other.GetInstanceID();
    }
    #endregion

    #region UNITY CALLBACKS
    protected virtual void OnValidate() {
#if UNITY_EDITOR
      editor.OnValidate();
#endif
      patchReferences();
    }

    protected virtual void OnEnable() {
#if UNITY_EDITOR
      if (InternalUtility.IsPrefab(this)) {
        return;
      }

      if (Application.isPlaying) {
#endif
        //If we are not attached, or if we are about to become detached
        if (!isAttachedToGroup || _notificationState == NotificationState.WillBeDetached) {
          var parentRenderer = GetComponentInParent<LeapGraphicRenderer>();
          if (parentRenderer != null) {
            parentRenderer.TryAddGraphic(this);
          }
        }

        patchReferences();
#if UNITY_EDITOR
      }
#endif
    }

    protected virtual void Start() {
#if UNITY_EDITOR
      if (InternalUtility.IsPrefab(this)) {
        return;
      }

      if (Application.isPlaying) {
#endif
        //If we are not attached, or if we are about to become detached
        if (!isAttachedToGroup || _notificationState == NotificationState.WillBeDetached) {
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
      if (InternalUtility.IsPrefab(this)) {
        return;
      }

      if (Application.isPlaying) {
#endif
        if (isAttachedToGroup) {
          attachedGroup.TryRemoveGraphic(this);
        } else if (_notificationState == NotificationState.WillBeAttached) {
          _groupToBeAttachedTo.TryRemoveGraphic(this);
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

    public virtual void OnBeforeSerialize() { }

    public virtual void OnAfterDeserialize() {
      for (int i = 0; i < _featureData.Count; i++) {
        _featureData[i].graphic = this;
      }
    }

    private void patchReferences() {
      if (isAttachedToGroup) {
        var group = _attachedRenderer.groups[_attachedGroupIndex];
        for (int i = 0; i < _featureData.Count; i++) {
          _featureData[i].feature = group.features[i];
        }
      }
    }

    [Serializable]
    public class FeatureDataList : MultiTypedList<LeapFeatureData, LeapTextureData,
                                                                   LeapSpriteData,
                                                                   LeapRuntimeTintData,
                                                                   LeapBlendShapeData,
                                                                   CustomFloatChannelData,
                                                                   CustomVectorChannelData,
                                                                   CustomColorChannelData,
                                                                   CustomMatrixChannelData> { }

    private enum NotificationState {
      None,
      WillBeAttached,
      WillBeDetached
    }

    #endregion
  }
}
