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
using Leap.Unity.Space;

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

    [SerializeField]
    private List<LeapGraphicGroup> _groups = new List<LeapGraphicGroup>();
    #endregion

    #region PUBLIC RUNTIME API

    /// <summary>
    /// Returns the leap space that is currently attached to this graphic renderer.  The only
    /// way to attach a space to a graphic renderer is by adding the space component onto the
    /// same gameObject at edit time.  There is no support for adding or removing spaces at
    /// runtime.
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
    /// Tries to add the given graphic to any group attached to this graphic.  A graphic will
    /// be attached to a group that has its preferred renderer type first, but if none are 
    /// preferred, it will be attached to the first compatible group found. 
    /// </summary>
    public bool TryAddGraphic(LeapGraphic graphic) {
      //First try to attatch to a group that is preferred
      Type preferredType = graphic.preferredRendererType;
      if (preferredType != null) {
        foreach (var group in groups) {
          Type rendererType = group.renderingMethod.GetType();
          if (preferredType == rendererType || rendererType.IsSubclassOf(preferredType)) {
            if (group.TryAddGraphic(graphic)) {
              return true;
            }
          }
        }
      }

      //If we failed, try to attach to a group that will take us
      foreach (var group in groups) {
        if (group.renderingMethod.IsValidGraphic(graphic)) {
          if (group.TryAddGraphic(graphic)) {
            return true;
          }
        }
      }

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
#endif

      if (Application.isPlaying) {
        if (_space != null) {
          _space.RebuildHierarchy();
          _space.RecalculateTransformers();
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

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize() {
      foreach (var group in _groups) {
        group.renderer = this;
      }
    }
    #endregion
  }
}
