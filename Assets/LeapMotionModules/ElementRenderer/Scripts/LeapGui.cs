using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity;
using Leap.Unity.Query;

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
  private List<LeapGuiGroup> _groups;
  #endregion

  #region PRIVATE VARIABLES
  //We serialize just for ease of use
  [SerializeField]
  private int _selectedGroup = 0;

  [SerializeField]
  private List<AnchorOfConstantSize> _anchors = new List<AnchorOfConstantSize>();

  [SerializeField]
  private List<Transform> _anchorParents = new List<Transform>();

  private List<LeapGuiElement> _toAdd = new List<LeapGuiElement>();
  private List<LeapGuiElement> _toRemove = new List<LeapGuiElement>();

  private List<LeapGuiElement> _tempElementList = new List<LeapGuiElement>();
  private List<int> _tempIndexList = new List<int>();

  [NonSerialized]
  private bool _hasFinishedSetup = false;
  [NonSerialized]
  private int _previousHierarchyHash;
  private DelayedAction _delayedHeavyRebuild;
  #endregion

  #region PUBLIC API

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

  /// <summary>
  /// Tries to add a new gui element to this gui at runtime.
  /// Element is not actually added until the next gui cycle.
  /// </summary>
  public bool TryAddElement(LeapGuiElement element) {
    AssertHelper.AssertRuntimeOnly();
    Assert.IsNotNull(element);
    //TO WHICH GROUP AAAA

    _toAdd.Add(element);
    return true;
  }

  /// <summary>
  /// Tries to remove a gui element from this gui at runtime.
  /// Element is not actually removed until the next gui cycle.
  /// </summary>
  public bool TryRemoveElement(LeapGuiElement element) {
    AssertHelper.AssertRuntimeOnly();
    Assert.IsNotNull(element);
    //TO WHICH GROUP AAAA

    _toRemove.Add(element);
    return true;
  }

  //Begin editor-only private api
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
    //TODO
    /*
    if (_space == null) {
      return;
    }

    using (new ProfilerSample("Rebuild Picking Meshes")) {
      List<Vector3> pickingVerts = new List<Vector3>();
      List<int> pickingTris = new List<int>();

      foreach (var element in _elements) {
        pickingVerts.Clear();
        pickingTris.Clear();

        Mesh pickingMesh = element.pickingMesh;
        if (pickingMesh == null) {
          pickingMesh = new Mesh();
          pickingMesh.MarkDynamic();
          pickingMesh.hideFlags = HideFlags.HideAndDontSave;
          pickingMesh.name = "Gui Element Picking Mesh";
          element.pickingMesh = pickingMesh;
        }
        pickingMesh.Clear();

        foreach (var dataObj in element.data) {
          if (dataObj is LeapGuiMeshData) {
            var meshData = dataObj as LeapGuiMeshData;
            meshData.RefreshMeshData();

            Mesh mesh = meshData.mesh;
            if (mesh == null) continue;

            var topology = MeshCache.GetTopology(mesh);
            for (int i = 0; i < topology.tris.Length; i++) {
              pickingTris.Add(topology.tris[i] + pickingVerts.Count);
            }

            ITransformer transformer = _space.GetTransformer(element.anchor);
            for (int i = 0; i < topology.verts.Length; i++) {
              Vector3 localRectVert = transform.InverseTransformPoint(element.transform.TransformPoint(topology.verts[i]));
              pickingVerts.Add(transformer.TransformPoint(localRectVert));
            }
          }
        }

        pickingMesh.SetVertices(pickingVerts);
        pickingMesh.SetTriangles(pickingTris, 0, calculateBounds: true);
        pickingMesh.RecalculateNormals();
      }
    }
    */
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
    //TODO, clean up groups too
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
      //check groups and if their features are dirty

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
    //TODO: write this :P
    /*
    if (_renderer != null && _space != null) {
      if (fullRebuild) {
        rebuildElementList();
        rebuildFeatureData();
        rebuildFeatureSupportInfo();

        _space.BuildElementData(transform);

        using (new ProfilerSample("Update Renderer")) {
          _renderer.OnUpdateRendererEditor(heavyRebuild);
        }

        foreach (var feature in _features) {
          feature.isDirty = false;
        }

        _hasFinishedSetup = true;
      }

      _renderer.OnUpdateRenderer();
    }
    */
  }
#endif

  private void doLateUpdateRuntime() {
    if (_space == null) return;

    if (_toRemove.Count != 0) {
      //doit
    }

    if (_toAdd.Count != 0) {
      //doit
    }

    //TODO, optimize this!  Don't do it every frame for the whole thing!
    using (new ProfilerSample("Refresh space data")) {
      //_space.RefreshElementData(transform, 0, anchors.Count);
    }

    //TODO: update groups and reset feature dirty flags

    _hasFinishedSetup = true;
  }
  #endregion
}
