using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity;

[ExecuteInEditMode]
public class LeapGui : MonoBehaviour {
  public const string FEATURE_PREFIX = "LEAP_GUI_";
  public const string PROPERTY_PREFIX = "_LeapGui";

  public const string FEATURE_MOVEMENT_TRANSLATION = FEATURE_PREFIX + "MOVEMENT_TRANSLATION";
  public const string FEATURE_MOVEMENT_FULL = FEATURE_PREFIX + "MOVEMENT_FULL";

  #region INSPECTOR FIELDS
  [SerializeField]
  private LeapGuiSpace _space;

  [SerializeField]
  private List<LeapGuiGroup> _groups = new List<LeapGuiGroup>();
  #endregion

  #region PRIVATE VARIABLES
  //We serialize just for ease of use
  [SerializeField]
  private int _selectedGroup = 0;

  [SerializeField]
  private List<AnchorOfConstantSize> _anchors = new List<AnchorOfConstantSize>();

  [SerializeField]
  private List<Transform> _anchorParents = new List<Transform>();

  [NonSerialized]
  private List<LeapGuiElement> _tempElementList = new List<LeapGuiElement>();
  [NonSerialized]
  private bool _hasFinishedSetup = false;
  [NonSerialized]
  private int _previousHierarchyHash;

  private DelayedAction _delayedHeavyRebuild;
  #endregion

  #region PUBLIC RUNTIME API

  public LeapGuiSpace space {
    get {
      return _space;
    }
  }

  public List<LeapGuiGroup> groups {
    get {
      return _groups;
    }
  }

  public List<AnchorOfConstantSize> anchors {
    get {
      return _anchors;
    }
  }

  public List<Transform> anchorParents {
    get {
      return _anchorParents;
    }
  }

  public bool hasFinishedSetup {
    get {
      return _hasFinishedSetup;
    }
  }

  public bool TryAddElement(LeapGuiElement element) {
    //First try to attatch to a group that is preferred
    foreach (var group in groups) {
      Type rendererType = group.renderer.GetType();
      Type preferredType = element.preferredRendererType;
      if (preferredType == rendererType || rendererType.IsSubclassOf(preferredType)) {
        if (group.TryAddElement(element)) {
          return true;
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

  #region PUBLIC EDITOR API
#if UNITY_EDITOR
  public void CreateGroup(Type rendererType) {
    AssertHelper.AssertEditorOnly();
    Assert.IsNotNull(rendererType);

    var group = gameObject.AddComponent<LeapGuiGroup>();
    group.Init(this, rendererType);

    _selectedGroup = _groups.Count;
    _groups.Add(group);
  }

  public void DestroySelectedGroup() {
    AssertHelper.AssertEditorOnly();

    var toDestroy = _groups[_selectedGroup];
    _groups.RemoveAt(_selectedGroup);

    if (_selectedGroup >= _groups.Count && _selectedGroup != 0) {
      _selectedGroup--;
    }

    InternalUtility.Destroy(toDestroy);
  }

  public void ScheduleEditorUpdate() {
    AssertHelper.AssertEditorOnly();

    //Dirty the hash by changing it to something else
    _previousHierarchyHash++;
  }

  public void SetSpace(Type spaceType) {
    AssertHelper.AssertEditorOnly();
    ScheduleEditorUpdate();

    UnityEditor.Undo.RecordObject(this, "Change Gui Space");
    UnityEditor.EditorUtility.SetDirty(this);

    if (_space != null) {
      DestroyImmediate(_space);
      _space = null;
    }

    _space = gameObject.AddComponent(spaceType) as LeapGuiSpace;

    if (_space != null) {
      _space.gui = this;
    }
  }

  public void RebuildEditorPickingMeshes() {
    if (_space == null) {
      return;
    }

    foreach (var group in _groups) {
      group.RebuildEditorPickingMeshes();
    }
  }
#endif
  #endregion

  #region UNITY CALLBACKS
  void OnValidate() {

#if UNITY_EDITOR
    //TODO, handle drag-drop of leap gui for groups!

    if (_space != null && _space.gameObject != gameObject) {
      LeapGuiSpace movedSpace;
      if (InternalUtility.TryMoveComponent(_space, gameObject, out movedSpace)) {
        _space = movedSpace;
      } else {
        Debug.LogWarning("Could not move space component " + _space + "!");
        InternalUtility.Destroy(_space);
      }
    }
#endif

    _space = _space ?? GetComponent<LeapGuiSpace>() ?? gameObject.AddComponent<LeapGuiRectSpace>();
    _space.gui = this;

    //TODO: assign groups automatically
  }

  private void Reset() {
    OnValidate();
  }

  void OnDestroy() {
    if (_space != null) InternalUtility.Destroy(space);
    foreach (var group in _groups) {
      InternalUtility.Destroy(group);
    }
    _delayedHeavyRebuild.Dispose();
  }

  void Awake() {
    if (_space != null) {
      _space.gui = this;
    }
    //TODO: assign references 
  }

  void OnEnable() {
    if (Application.isPlaying) {
      //TODO: enable each group too
      if (_space != null) _space.BuildElementData(transform);
    }
  }

  void OnDisable() {
    if (Application.isPlaying) {
      //TODO: disable all groups
    }
  }

  void LateUpdate() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
      doLateUpdateRuntime();
    } else {
      doLateUpdateEditor();
    }
#else
    doLateUpdateRuntime();
#endif
  }
  #endregion

  #region PRIVATE IMPLEMENTATION

  private LeapGui() {
    _delayedHeavyRebuild = new DelayedAction(() => doEditorUpdateLogic(fullRebuild: true, heavyRebuild: true));
  }

#if UNITY_EDITOR
  private void doLateUpdateEditor() {
    bool needsRebuild = false;

    using (new ProfilerSample("Calculate Should Rebuild")) {
      foreach (var group in _groups) {
        foreach (var feature in group.features) {
          if (feature.isDirty) {
            needsRebuild = true;
            break;
          }
        }
      }

      int hierarchyHash = HashUtil.GetHierarchyHash(transform);
      if (_previousHierarchyHash != hierarchyHash) {
        _previousHierarchyHash = hierarchyHash;
        needsRebuild = true;
      }
    }

    if (needsRebuild) {
      _delayedHeavyRebuild.Reset();
    }

    doEditorUpdateLogic(needsRebuild, heavyRebuild: false);
  }

  private void doEditorUpdateLogic(bool fullRebuild, bool heavyRebuild) {
    if (fullRebuild) {
      _anchors.Clear();
      _anchorParents.Clear();
      rebuildAnchorInfo(transform, transform);
      _space.BuildElementData(transform);
      collectUnattachedElements();

      foreach (var group in _groups) {
        group.RebuildFeatureData();
        group.RebuildFeatureSupportInfo();
        group.UpdateRendererEditor(heavyRebuild);
      }

      _hasFinishedSetup = true;
    }

    foreach (var group in _groups) {
      group.UpdateRenderer();
    }
  }
#endif

  private void rebuildAnchorInfo(Transform root, Transform currAnchor) {
    int count = root.childCount;
    for (int i = 0; i < count; i++) {
      Transform child = root.GetChild(i);
      if (!child.gameObject.activeSelf) continue;

      var childAnchor = currAnchor;

      var anchorComponent = child.GetComponent<AnchorOfConstantSize>();
      if (anchorComponent != null && anchorComponent.enabled) {
        _anchors.Add(anchorComponent);
        _anchorParents.Add(currAnchor);

        childAnchor = anchorComponent.transform;
      }

      rebuildAnchorInfo(child, childAnchor);
    }
  }

  private void collectUnattachedElements() {
    GetComponentsInChildren(_tempElementList);

    foreach (var element in _tempElementList) {
      if (element.IsAttachedToGroup) {
        //procede to validate

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
    if (_space == null) return;

    //TODO, optimize this!  Don't do it every frame for the whole thing!
    using (new ProfilerSample("Refresh space data")) {
      _space.RefreshElementData(transform, 0, _anchors.Count);
    }

    foreach (var group in _groups) {
      group.UpdateRenderer();
    }

    _hasFinishedSetup = true;
  }
  #endregion
}
