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
    private List<LeapGraphic> _tempGraphicList = new List<LeapGraphic>();
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
      editor.OnValidate();
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

    private void Awake() {
      //TODO: assign references 
    }

    private void OnEnable() {
      if (Application.isPlaying) {
        //TODO: enable each group too
        if (_space != null) {
          _space.RebuildHierarchy();
          _space.RecalculateTransformers();
        }
      }
    }

    private void OnDisable() {
      if (Application.isPlaying) {
        //TODO: disable all groups
      }
    }

    private void LateUpdate() {
#if UNITY_EDITOR
      if (Application.isPlaying) {
        doLateUpdateRuntime();
      } else {
        editor.DoLateUpdateEditor();
      }
#else
    doLateUpdateRuntime();
#endif
    }
    #endregion

    #region PRIVATE IMPLEMENTATION

    private LeapGraphicRenderer() {
#if UNITY_EDITOR
      editor = new EditorApi(this);
#endif
    }

    private void validateGraphics() {
      GetComponentsInChildren(includeInactive: true, result: _tempGraphicList);

      HashSet<LeapGraphic> set = Pool<HashSet<LeapGraphic>>.Spawn();
      foreach (var group in _groups) {
        for (int i = group.graphics.Count; i-- != 0;) {
          if (group.graphics[i] == null) {
            group.graphics.RemoveAt(i);
          } else {
            set.Add(group.graphics[i]);
          }
        }

        foreach (var graphic in _tempGraphicList) {
          if (graphic.isAttachedToGroup) {
            //If the graphic claims it is attached to this group, but it really isn't, remove
            //it and re-add it.
            bool graphicThinksItsInGroup = graphic.attachedGroup == group;
            bool isActuallyInGroup = set.Contains(graphic);

            //Also re add it if it is attached to a completely different renderer!
            if (graphicThinksItsInGroup != isActuallyInGroup ||
                graphic.attachedGroup.renderer != this) {
              group.TryRemoveGraphic(graphic);
              group.TryAddGraphic(graphic);
            }
          }
        }

        set.Clear();
      }
      Pool<HashSet<LeapGraphic>>.Recycle(set);

      foreach (var graphic in _tempGraphicList) {
        if (graphic.isAttachedToGroup) {
          //procede to validate

          //If the graphic is anchored to the wrong anchor, detach and reattach
          var anchor = _space == null ? null : LeapSpaceAnchor.GetAnchor(graphic.transform);
          if (graphic.anchor != anchor) {
            var group = graphic.attachedGroup;

            if (group.TryRemoveGraphic(graphic)) {
              group.TryAddGraphic(graphic);
            }
          }

          //Debug.Log(graphic.gameObject.activeInHierarchy + " : " + graphic.gameObject.activeSelf);
          if (!graphic.enabled || !graphic.gameObject.activeInHierarchy) {
            graphic.attachedGroup.TryRemoveGraphic(graphic);
          }
        }

        if (!graphic.isAttachedToGroup && graphic.enabled && graphic.gameObject.activeInHierarchy) {
          TryAddGraphic(graphic);
        }
      }
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
