/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Space;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [ExecuteInEditMode]
  public partial class LeapGraphicRenderer : MonoBehaviour, ISerializationCallbackReceiver {
    public const string FEATURE_PREFIX = "GRAPHIC_RENDERER_";
    public const string PROPERTY_PREFIX = "_GraphicRenderer";

    public const string FEATURE_MOVEMENT_TRANSLATION = FEATURE_PREFIX + "MOVEMENT_TRANSLATION";
    public const string FEATURE_MOVEMENT_FULL = FEATURE_PREFIX + "MOVEMENT_FULL";

    #region INSPECTOR FIELDS
    [SerializeField]
    private LeapSpace _space;
    private bool _lastSpaceWasNull = true;

    [SerializeField]
    private List<LeapGraphicGroup> _groups = new List<LeapGraphicGroup>();
    #endregion

    #region PUBLIC RUNTIME API

    /// <summary>
    /// Returns the leap space that is currently attached to this graphic renderer.
    /// </summary>
    public LeapSpace space {
      get {
        return _space;
      }
    }

    /// <summary>
    /// Returns a list of all graphic groups contained withinin this renderer.  This getter
    /// returns a regular List object for simplicity and speed, but it is still not allowed
    /// to mutate this list in any way.
    /// </summary>
    public List<LeapGraphicGroup> groups {
      get {
        return _groups;
      }
    }

    /// <summary>
    /// Searches the group list for a group with the given name.  If there is no
    /// group with the given name, this method will return null.
    /// </summary>
    public LeapGraphicGroup FindGroup(string name) {
      return _groups.Query().FirstOrDefault(g => g.name == name);
    }

    /// <summary>
    /// Tries to add the given graphic to any group attached to this graphic.  First, it
    /// will try to be attached to a group that has its preferred renderer type, and if 
    /// there are multiple such groups it will choose the group with the smallest graphic
    /// count.
    /// 
    /// If no group has the preferred renderer type, it will try to attach to a group
    /// that supports this type of graphic, again choosing the group with the smallest
    /// graphic count.
    /// 
    /// If no such group is found, the attach will fail and this method will return false.
    /// </summary>
    public bool TryAddGraphic(LeapGraphic graphic) {
      LeapGraphicGroup targetGroup = null;

      //First just try to attach to a group that is its favorite
      foreach (var group in groups) {
        if (group.name == graphic.favoriteGroupName) {
          if (group.TryAddGraphic(graphic)) {
            return true;
          }
        }
      }

      //Then try to attatch to a group that is of the preferred type
      //Choose the preferred group with the least graphics
      Type preferredType = graphic.preferredRendererType;
      if (preferredType != null) {
        foreach (var group in groups) {
          Type rendererType = group.renderingMethod.GetType();
          if (preferredType == rendererType ||
              rendererType.IsSubclassOf(preferredType)) {
            if (targetGroup == null || group.toBeAttachedCount < targetGroup.toBeAttachedCount) {
              targetGroup = group;
            }
          }
        }
      }

      if (targetGroup != null && targetGroup.TryAddGraphic(graphic)) {
        return true;
      }

      //If we failed, just try to attach to any group that will take us
      foreach (var group in groups) {
        if (group.renderingMethod.IsValidGraphic(graphic)) {
          if (targetGroup == null || group.toBeAttachedCount < targetGroup.toBeAttachedCount) {
            targetGroup = group;
          }
        }
      }

      if (targetGroup != null && targetGroup.TryAddGraphic(graphic)) {
        return true;
      }

      //Unable to find any group that would accept the graphic :(
      return false;
    }

    #endregion

    #region UNITY CALLBACKS
    private void OnValidate() {
#if UNITY_EDITOR
      if (!InternalUtility.IsPrefab(this)) {
        if (!Application.isPlaying) {
          editor.ScheduleRebuild();
        }
        editor.OnValidate();
      }
#endif
    }

    private void OnDestroy() {
#if UNITY_EDITOR
      editor.OnDestroy();
#endif
    }

    private void OnEnable() {
#if UNITY_EDITOR
      Vector2[] uv = null;
      foreach (var group in _groups) {
        foreach (var feature in group.features) {
          LeapSpriteFeature spriteFeature = feature as LeapSpriteFeature;
          if (spriteFeature != null) {
            foreach (var data in spriteFeature.featureData) {
              uv = data.sprite.uv;
            }
          }
        }
      }

      UnityEditor.Undo.undoRedoPerformed -= onUndoRedoPerformed;
      UnityEditor.Undo.undoRedoPerformed += onUndoRedoPerformed;
#endif

      if (Application.isPlaying) {
        if (_space != null) {
          _space.RebuildHierarchy();
          _space.RecalculateTransformers();
          _lastSpaceWasNull = false;
        }

        foreach (var group in _groups) {
          group.OnEnable();
        }
      }
    }

    private void OnDisable() {
      if (Application.isPlaying) {
        foreach (var group in _groups) {
          group.OnDisable();
        }
      }

#if UNITY_EDITOR
      UnityEditor.Undo.undoRedoPerformed += onUndoRedoPerformed;
#endif
    }

    private void LateUpdate() {
#if UNITY_EDITOR
      //No updates for prefabs!
      if (InternalUtility.IsPrefab(this)) {
        return;
      }

      if (!Application.isPlaying) {
        editor.DoLateUpdateEditor();
      } else
#endif
      {
        doLateUpdateRuntime();
      }
    }
    #endregion

    #region PRIVATE IMPLEMENTATION

    private LeapGraphicRenderer() {
#if UNITY_EDITOR
      editor = new EditorApi(this);
#endif
    }

    private void doLateUpdateRuntime() {
      // Validate the attached space to support it changing at runtime.
      validateSpaceComponent();

      if (_space != null) {
        //TODO, optimize this!  Don't do it every frame for the whole thing!
        using (new ProfilerSample("Refresh space data")) {
          _space.RecalculateTransformers();
        }
      }

      foreach (var group in _groups) {
        group.UpdateRenderer();
      }
    }

    public void validateSpaceComponent() {
      var origSpace = _space;
      
      var spaces = Pool<List<LeapSpace>>.Spawn();
      spaces.Clear();
      try {
        GetComponents<LeapSpace>(spaces);
        _space = spaces.Query().FirstOrDefault(s => s.enabled);
      }
      finally {
        spaces.Clear();
        Pool<List<LeapSpace>>.Recycle(spaces);
      }

      // Support Undo/Redo with runtime space changes in-editor
      bool didUndoRedo = false;
      #if UNITY_EDITOR
      if (_didUndoRedoThisFrame) {
        didUndoRedo = true;
        _didUndoRedoThisFrame = false;
      }
      #endif

      if (Application.isPlaying
          && (origSpace != _space
              || (_space == null && !_lastSpaceWasNull))
              || didUndoRedo
              ) {
        onRuntimeSpaceChanged();
      }

      _lastSpaceWasNull = _space == null;
    }

#if UNITY_EDITOR
    private bool _didUndoRedoThisFrame = false;

    private void onUndoRedoPerformed() {
      _didUndoRedoThisFrame = true;
    }
#endif

    private void onRuntimeSpaceChanged() {
      // The space was modified, so refresh a bunch of things..

      if (_space != null) {
        _space.RebuildHierarchy();
        _space.RecalculateTransformers();
      }

      // Need to update material keywords appropriately.
      // This involves re-preparing materials, which happens OnEnable,
      // so we'll "power-cycle" the whole renderer for simplicity's sake.
      OnDisable();
      OnEnable();
      

      foreach (var group in _groups) {
        group.RefreshGraphicAnchors();
      }
    }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize() {
      foreach (var group in _groups) {
        (group as ILeapInternalGraphicGroup).renderer = this;
      }
    }
    #endregion
  }
}
