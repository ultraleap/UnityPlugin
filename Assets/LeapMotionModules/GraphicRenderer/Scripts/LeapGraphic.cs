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
  public abstract partial class LeapGraphic : MonoBehaviour, ISpaceComponent, ISerializationCallbackReceiver {

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
    private bool _willBeAttached = false;
    [NonSerialized]
    private bool _willBeDetached = false;
    [NonSerialized]
    private LeapGraphicGroup _groupToBeAttachedTo = null;
    [NonSerialized]
    private bool _isRepresentationDirty = true;

    /// <summary>
    /// An internal flag that returns true if the visual representation of
    /// this graphic needs to be updated.  You can set this to true to request
    /// a regeneration of the graphic during the next update cycle of the
    /// renderer.  Note however, than not all renderers support updating the
    /// representation at runtime.
    /// </summary>
    public bool isRepresentationDirty {
      get {
        return _isRepresentationDirty;
      }
      set {
        _isRepresentationDirty = value;
      }
    }

    /// <summary>
    /// A simple utility getter that returns true if isRepresentationDirty
    /// is true, OR it is currently edit time.
    /// </summary>
    public bool isRepresentationDirtyOrEditTime {
      get {
#if UNITY_EDITOR
        if (!Application.isPlaying) {
          return true;
        }
#endif
        return _isRepresentationDirty;
      }
    }

    /// <summary>
    /// Returns the space anchor for this graphic.  This will be null if
    /// the graphic is not currently part of a space.  The anchor cannot
    /// be changed dynamically at runtime.
    /// </summary>
    public LeapSpaceAnchor anchor {
      get {
        return _anchor;
      }
    }

    /// <summary>
    /// A utility getter that returns a transformer for this graphic.  Even
    /// if the space anchor for this graphic is null, this will still return
    /// a valid transformer.  In the null case, the transformer is always the 
    /// identity transformer.
    /// </summary>
    public ITransformer transformer {
      get {
        return _anchor == null ? IdentityTransformer.single : _anchor.transformer;
      }
    }

    /// <summary>
    /// Returns a list of feature data attached to this graphic.  If this graphic
    /// is attached to a group, this feature data matches 1-to-1 with the features
    /// attached to this group.
    /// </summary>
    public IList<LeapFeatureData> featureData {
      get {
        return _featureData;
      }
    }

    /// <summary>
    /// Returns the group this graphic is attached to.
    /// </summary>
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

    /// <summary>
    /// Returns whether or not this graphic is attached to any group.  Can still
    /// return false at runtime even if TryAddGraphic has just completed successfully
    /// due to the runtime delay for addition/removal of graphics.
    /// </summary>
    public bool isAttachedToGroup {
      get {
        return attachedGroup != null;
      }
    }

    /// <summary>
    /// Returns the type this graphic prefers to be attached to.  When calling
    /// LeapGraphicRenderer.TryAddGraphic it will prioritize being attached to 
    /// groups with this renderer type if possible.
    /// </summary>
    public Type preferredRendererType {
      get {
        return _preferredRendererType;
      }
    }

    /// <summary>
    /// This method tries to detach this graphic from whatever group it is 
    /// currently attached to.  It can fail if the graphic is not attached
    /// to any group, or if the group it is attached to does not support
    /// adding/removing graphics at runtime.
    /// </summary>
    public bool TryDetach() {
      var attachedGroup = this.attachedGroup;
      if (attachedGroup == null) {
        return false;
      } else {
        return attachedGroup.TryRemoveGraphic(this);
      }
    }

    /// <summary>
    /// Gets a single feature data object of a given type T.  This will return
    /// null if there is no feature data object attached to this graphic of type T.
    /// </summary>>
    public T GetFeatureData<T>() where T : LeapFeatureData {
      return _featureData.Query().OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Called by the system to notify that this graphic will be attached within 
    /// the next frame. This is only called at runtime.
    /// </summary>
    public virtual void NotifyWillBeAttached(LeapGraphicGroup toBeAttachedTo) {
      Assert.IsFalse(_willBeAttached);
      Assert.IsNull(_groupToBeAttachedTo);

      _willBeAttached = true;
      _groupToBeAttachedTo = toBeAttachedTo;
    }

    /// <summary>
    /// Called by the system to notify that a previous notification that this 
    /// graphic would be attached has been canceled due to a call to TryRemoveGraphic.
    /// </summary>
    public virtual void CancelWillBeAttached() {
      Assert.IsTrue(_willBeAttached);
      Assert.IsNotNull(_groupToBeAttachedTo);

      _willBeAttached = false;
      _groupToBeAttachedTo = null;
    }

    /// <summary>
    /// Called by the system to notify that this graphic will be detached within 
    /// the next frame. This is only called at runtime.
    /// </summary>
    public virtual void NotifyWillBeDetached(LeapGraphicGroup toBeDetachedFrom) {
      Assert.IsFalse(_willBeDetached);

      _willBeDetached = true;
    }

    /// <summary>
    /// Called by the system to notify that a previous notification that this 
    /// graphic would be detached has been canceled due to a call to TryAddGraphic.
    /// </summary>
    public virtual void CancelWillBeDetached() {
      Assert.IsTrue(_willBeDetached);

      _willBeDetached = false;
    }

    /// <summary>
    /// Called by the system when this graphic is attached to a group.  This method is
    /// invoked both at runtime and at edit time.
    /// </summary>
    public virtual void OnAttachedToGroup(LeapGraphicGroup group, LeapSpaceAnchor anchor) {
#if UNITY_EDITOR
      editor.OnAttachedToGroup(group, anchor);
#endif
      _willBeAttached = false;
      _groupToBeAttachedTo = null;

      _attachedRenderer = group.renderer;
      _attachedGroupIndex = _attachedRenderer.groups.IndexOf(group);
      _anchor = anchor;

      patchReferences();
    }

    /// <summary>
    /// Called by the system when this graphic is detached from a group.  This method
    /// is invoked both at runtime and at edit time.
    /// </summary>
    public virtual void OnDetachedFromGroup() {
      _willBeDetached = false;

      _attachedRenderer = null;
      _attachedGroupIndex = -1;
      _anchor = null;

      for (int i = 0; i < _featureData.Count; i++) {
        _featureData[i].feature = null;
      }
    }

    /// <summary>
    /// Called by the system whenever feature data is re-assigned to this graphic.  This
    /// is only called at edit time.
    /// </summary>
    public virtual void OnAssignFeatureData(List<LeapFeatureData> data) {
      _featureData.Clear();
      foreach (var dataObj in data) {
        _featureData.Add(dataObj);
      }
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
        //If we are not attached, and if we are about not about to become attached
        if (!isAttachedToGroup && !_willBeAttached) {
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
        //If we are not attached, and if we are about not about to become attached
        if (!isAttachedToGroup && !_willBeAttached) {
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
        } else if (_willBeAttached) {
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

    private T getFeatureDataOrThrow<T>() where T : LeapFeatureData {
      var data = _featureData.Query().OfType<T>().FirstOrDefault();
      if (data == null) {
        throw new Exception("There is not a feature data object of type " + typeof(T).Name + " attached to this graphic.");
      }
      return data;
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
    #endregion
  }
}
