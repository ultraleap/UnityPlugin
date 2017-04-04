using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity;
using Leap.Unity.Space;

[ExecuteInEditMode]
public partial class LeapGui : MonoBehaviour {
  public const string FEATURE_PREFIX = "LEAP_GUI_";
  public const string PROPERTY_PREFIX = "_LeapGui";

  public const string FEATURE_MOVEMENT_TRANSLATION = FEATURE_PREFIX + "MOVEMENT_TRANSLATION";
  public const string FEATURE_MOVEMENT_FULL = FEATURE_PREFIX + "MOVEMENT_FULL";

  #region INSPECTOR FIELDS
  [SerializeField]
  private LeapSpace _space;

  [SerializeField]
  private List<LeapGuiGroup> _groups = new List<LeapGuiGroup>();
  #endregion

  #region PRIVATE VARIABLES
  [NonSerialized]
  private List<LeapGuiElement> _tempElementList = new List<LeapGuiElement>();
  [NonSerialized]
  private bool _hasFinishedSetup = false;

  #endregion

  #region PUBLIC RUNTIME API

  public LeapSpace space {
    get {
      return _space;
    }
  }

  public List<LeapGuiGroup> groups {
    get {
      return _groups;
    }
  }

  public bool hasFinishedSetup {
    get {
      return _hasFinishedSetup;
    }
  }

  public bool TryAddElement(LeapGuiElement element) {
    //First try to attatch to a group that is preferred
    Type preferredType = element.preferredRendererType;
    if (preferredType != null) {
      foreach (var group in groups) {
        Type rendererType = group.renderer.GetType();
        if (preferredType == rendererType || rendererType.IsSubclassOf(preferredType)) {
          if (group.TryAddElement(element)) {
            return true;
          }
        }
      }
    }

    //If we failed, try to attach to a group that will take us
    foreach (var group in groups) {
      if (group.renderer.IsValidElement(element)) {
        if (group.TryAddElement(element)) {
          return true;
        }
      }
    }

    return false;
  }

  #endregion

  #region UNITY CALLBACKS
  private void OnValidate() {
    if (_space == null) {
      _space = GetComponent<LeapSpace>();
    }

#if UNITY_EDITOR
    editor.OnValidate();
#endif
  }

  private void Reset() {
    //First destroy all groups on this object
    foreach (var group in GetComponents<LeapGuiGroup>()) {
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

  private LeapGui() {
#if UNITY_EDITOR
    editor = new EditorApi(this);
#endif
  }

  private void validateElements() {
    GetComponentsInChildren(_tempElementList);

    foreach (var element in _tempElementList) {
      if (element.IsAttachedToGroup) {
        //procede to validate

        //If the element is anchored to the wrong anchor, detach and reattach
        var anchor = _space == null ? null : LeapSpaceAnchor.GetAnchor(element.transform);
        if (element.anchor != anchor) {
          var group = element.attachedGroup;

          if (group.TryRemoveElement(element)) {
            group.TryAddElement(element);
          }
        }

        if (!element.attachedGroup.elements.Contains(element)) {
          var group = element.attachedGroup;
          element.OnDetachedFromGui();
          group.TryAddElement(element); //if this fails, handled by later clause
        }

        if (!element.enabled) {
          element.attachedGroup.TryRemoveElement(element);
        }
      }

      if (!element.IsAttachedToGroup && element.enabled) {
        TryAddElement(element);
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
