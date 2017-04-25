using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  [ExecuteInEditMode]
  public partial class LeapGraphicRenderer : MonoBehaviour {
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

    #region PRIVATE VARIABLES
    [NonSerialized]
    private bool _hasFinishedSetup = false;

    #endregion

    #region PUBLIC RUNTIME API

    public LeapSpace space {
      get {
        return _space;
      }
    }

    public List<LeapGraphicGroup> groups {
      get {
        return _groups;
      }
    }

    public bool hasFinishedSetup {
      get {
        return _hasFinishedSetup;
      }
    }

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
        editor.OnValidate();
      }
#endif
    }

    private void Reset() {
      //First destroy all groups on this object
      foreach (var group in GetComponents<LeapGraphicGroup>()) {
        DestroyImmediate(group);
      }

      //Then do normal validation
      OnValidate();
    }

    private void OnDestroy() {
      foreach (var group in _groups) {
        InternalUtility.Destroy(group);
      }

#if UNITY_EDITOR
      editor.OnDestroy();
#endif
    }

    private void OnEnable() {
      if (Application.isPlaying) {
        if (_space != null) {
          _space.RebuildHierarchy();
          _space.RecalculateTransformers();
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

      _hasFinishedSetup = true;
    }
    #endregion
  }
}
