using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity.Query;

[ExecuteInEditMode]
public class LeapGui : MonoBehaviour {
  public const string FEATURE_PREFIX = "LEAP_GUI_";
  public const string PROPERTY_PREFIX = "_LeapGui";

  public const string FEATURE_MOVEMENT_TRANSLATION = FEATURE_PREFIX + "MOVEMENT_TRANSLATION";
  public const string FEATURE_MOVEMENT_FULL = FEATURE_PREFIX + "MOVEMENT_FULL";

  #region INSPECTOR FIELDS
  [SerializeField]
  private List<LeapGuiFeatureBase> _features = new List<LeapGuiFeatureBase>();

  [SerializeField]
  private LeapGuiSpace _space;

  [SerializeField]
  private LeapGuiRenderer _renderer;
  #endregion

  #region PRIVATE VARIABLES
  [SerializeField, HideInInspector]
  private List<LeapGuiElement> _elements = new List<LeapGuiElement>();

  [SerializeField, HideInInspector]
  private List<AnchorOfConstantSize> _anchors = new List<AnchorOfConstantSize>();

  [SerializeField, HideInInspector]
  private List<Transform> _anchorParents = new List<Transform>();

  [SerializeField, HideInInspector]
  private List<SupportInfo> _supportInfo = new List<SupportInfo>();

  [SerializeField, HideInInspector]
  private bool _addRemoveSupported;

  private List<LeapGuiElement> _toAdd = new List<LeapGuiElement>();
  private List<LeapGuiElement> _toRemove = new List<LeapGuiElement>();

  private List<LeapGuiElement> _tempElementList = new List<LeapGuiElement>();
  private List<int> _tempIndexList = new List<int>();
  #endregion

  #region PUBLIC API
  public List<LeapGuiFeatureBase> features {
    get {
      return _features;
    }
  }

  public LeapGuiSpace space {
    get {
      return _space;
    }
  }

#if UNITY_EDITOR
  public new LeapGuiRenderer renderer {
#else
  public LeapGuiRenderer renderer {
#endif
    get {
      return _renderer;
    }
  }

  public List<LeapGuiElement> elements {
    get {
      return _elements;
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

  public List<SupportInfo> supportInfo {
    get {
      return _supportInfo;
    }
  }

  public bool addRemoveSupported {
    get {
      return _addRemoveSupported;
    }
  }

  /// <summary>
  /// Tries to add a new gui element to this gui at runtime.
  /// Element is not actually added until the next gui cycle.
  /// </summary>
  public bool TryAddElement(LeapGuiElement element) {
    AssertHelper.AssertRuntimeOnly();
    Assert.IsNotNull(element);
    if (!_addRemoveSupported) {
      return false;
    }

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
    if (!_addRemoveSupported) {
      return false;
    }

    _toRemove.Add(element);
    return true;
  }

  public bool GetSupportedFeatures<T>(List<T> features) where T : LeapGuiFeatureBase {
    features.Clear();
    for (int i = 0; i < _features.Count; i++) {
      var feature = _features[i];
      if (!(feature is T)) continue;
      if (_supportInfo[i].support == SupportType.Error) continue;

      features.Add(feature as T);
    }

    return features.Count != 0;
  }

  //Begin editor-only private api
#if UNITY_EDITOR
  public void SetSpace(Type spaceType) {
    AssertHelper.AssertEditorOnly();

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

  public void AddFeature(Type featureType) {
    AssertHelper.AssertEditorOnly();

    var feature = gameObject.AddComponent(featureType);
    _features.Add(feature as LeapGuiFeatureBase);
  }

  public void SetRenderer(Type rendererType) {
    AssertHelper.AssertEditorOnly();

    UnityEditor.Undo.RecordObject(this, "Changed Gui Renderer");
    UnityEditor.EditorUtility.SetDirty(this);

    if (_renderer != null) {
      _renderer.OnDisableRendererEditor();
      DestroyImmediate(_renderer);
      _renderer = null;
    }

    _renderer = gameObject.AddComponent(rendererType) as LeapGuiRenderer;

    if (_renderer != null) {
      _renderer.gui = this;
      _renderer.OnEnableRendererEditor();
    }
  }

  public void RebuildEditorPickingMeshes() {
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
  }
#endif
  #endregion

  #region UNITY CALLBACKS
  void OnValidate() {
    if (!Application.isPlaying) {
      _addRemoveSupported = true;
      if (_renderer != null) {
        _addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(renderer.GetType());
      }
      if (_space != null) {
        _addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(space.GetType());
      }
    }

    for (int i = _features.Count; i-- != 0;) {
      if (_features[i] == null) {
        _features.RemoveAt(i);
      }
    }

#if UNITY_EDITOR
    for (int i = _features.Count; i-- != 0;) {
      var feature = _features[i];
      if (feature.gameObject != gameObject) {
        LeapGuiFeatureBase movedFeature;
        if (InternalUtility.TryMoveComponent(feature, gameObject, out movedFeature)) {
          _features[i] = movedFeature;
        } else {
          Debug.LogWarning("Could not move feature component " + feature + "!");
          InternalUtility.Destroy(feature);
          _features.RemoveAt(i);
        }
      }
    }

    if (_space != null && _space.gameObject != gameObject) {
      LeapGuiSpace movedSpace;
      if (InternalUtility.TryMoveComponent(_space, gameObject, out movedSpace)) {
        _space = movedSpace;
      } else {
        Debug.LogWarning("Could not move space component " + _space + "!");
        InternalUtility.Destroy(_space);
      }
    }

    if (_renderer != null && _renderer.gameObject != gameObject) {
      LeapGuiRenderer movedRenderer;
      if (InternalUtility.TryMoveComponent(_renderer, gameObject, out movedRenderer)) {
        _renderer = movedRenderer;
      } else {
        Debug.LogWarning("Could not move renderer component " + _renderer + "!");
        InternalUtility.Destroy(_renderer);
      }
    }
#endif

    if (_space != null) _space.gui = this;
    if (_renderer != null) _renderer.gui = this;
  }

  void OnDestroy() {
    if (_renderer != null) InternalUtility.Destroy(_renderer);
    if (_space != null) InternalUtility.Destroy(space);
    foreach (var feature in _features) {
      if (feature != null) InternalUtility.Destroy(feature);
    }
  }

  void Awake() {
    if (_space != null) {
      _space.gui = this;
    }
    if (_renderer != null) {
      _renderer.gui = this;
    }

    foreach (var feature in _features) {
      feature.AssignFeatureReferences();
    }
  }

  void OnEnable() {
    if (Application.isPlaying) {
      _renderer.OnEnableRenderer();
      if (_space != null) _space.BuildElementData(transform);
    }
  }

  void OnDisable() {
    if (Application.isPlaying) {
      _renderer.OnDisableRenderer();
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

#if UNITY_EDITOR
  private void doLateUpdateEditor() {
    rebuildElementList();
    rebuildFeatureData();
    rebuildFeatureSupportInfo();

    if (_space != null) {
      _space.BuildElementData(transform);
    }

    if (_renderer != null && _elements.Count != 0) {
      using (new ProfilerSample("Update Renderer")) {
        _renderer.OnUpdateRendererEditor();
      }
    }
  }
#endif

  private void doLateUpdateRuntime() {
    if (_renderer == null) return;
    if (_space == null) return;

    if (_toRemove.Count != 0) {
      performElementRemoval();
    }

    if (_toAdd.Count != 0) {
      performElementAddition();
    }

    //TODO, optimize this!  Don't do it every frame for the whole thing!
    using (new ProfilerSample("Refresh space data")) {
      _space.RefreshElementData(transform, 0, anchors.Count);
    }

    if (_elements.Count != 0) {
      using (new ProfilerSample("Update Renderer")) {
        _renderer.OnUpdateRenderer();
      }
    }

    foreach (var feature in _features) {
      feature.isDirty = false;
    }
  }

  private void performElementRemoval() {
    using (new ProfilerSample("Remove Elements")) {
      for (int i = 0; i < _elements.Count; i++) {
        var element = _elements[i];
        if (_toRemove.RemoveUnordered(element)) {
          element.OnDetachedFromGui();
          _tempIndexList.Add(i);
        }
      }

      _elements.RemoveAtMany(_tempIndexList);

      foreach (var feature in _features) {
        feature.RemoveDataObjectReferences(_tempIndexList);
      }

      (_space as ISupportsAddRemove).OnRemoveElements(_tempIndexList);
      (_renderer as ISupportsAddRemove).OnRemoveElements(_tempIndexList);

      foreach (var notRemoved in _toRemove) {
        Debug.LogWarning("The element " + notRemoved + " was not removed because it was not part of the gui.");
      }

      _tempIndexList.Clear();
      _toRemove.Clear();
    }
  }

  private void performElementAddition() {
    using (new ProfilerSample("Add Elements")) {

      //TODO, both of these rebuild operations can probably be optimized a ton
      rebuildElementList();
      rebuildFeatureData();

      for (int i = 0; i < _elements.Count; i++) {
        var element = _elements[i];
        if (_toAdd.Remove(element)) {
          _tempElementList.Add(element);
          _tempIndexList.Add(i);
        }
      }

      (_space as ISupportsAddRemove).OnAddElements(_tempElementList, _tempIndexList);
      (_renderer as ISupportsAddRemove).OnAddElements(_tempElementList, _tempIndexList);

      _tempElementList.Clear();
      _tempIndexList.Clear();
      _toAdd.Clear();
    }
  }

  private void rebuildElementList() {
    using (new ProfilerSample("Rebuild Element List")) {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        foreach (var element in _elements) {
          element.OnDetachedFromGui();
        }
      }
#endif

      _elements.Clear();
      _anchors.Clear();
      _anchorParents.Clear();

      rebuildElementListRecursively(transform, transform);
    }
  }

  private void rebuildElementListRecursively(Transform root, Transform currAnchor) {
    int count = root.childCount;
    for (int i = 0; i < count; i++) {
      Transform child = root.GetChild(i);
      if (!child.gameObject.activeSelf) continue;

      var childAnchor = currAnchor;

      var anchorComponent = child.GetComponent<AnchorOfConstantSize>();
      if (anchorComponent != null && anchorComponent.enabled) {
        _anchorParents.Add(currAnchor);
        childAnchor = anchorComponent.transform;
        _anchors.Add(anchorComponent);
      }

      var element = child.GetComponent<LeapGuiElement>();
      if (element != null && element.enabled) {
        element.OnAttachedToGui(this, childAnchor, _elements.Count);
        _elements.Add(element);
      }

      rebuildElementListRecursively(child, childAnchor);
    }
  }

  private void rebuildFeatureData() {
    using (new ProfilerSample("Rebuild Feature Data")) {
      foreach (var feature in _features) {
        feature.ClearDataObjectReferences();
        feature.isDirty = true;
      }

      for (int i = 0; i < _elements.Count; i++) {
        var element = _elements[i];

        List<LeapGuiElementData> dataList = new List<LeapGuiElementData>();
        foreach (var feature in _features) {
          var dataObj = element.data.Query().OfType(feature.GetDataObjectType()).FirstOrDefault();
          if (dataObj != null) {
            element.data.Remove(dataObj);
          } else {
            dataObj = feature.CreateDataObject(element);
          }
          feature.AddDataObjectReference(dataObj);
          dataList.Add(dataObj);
        }

        foreach (var dataObj in element.data) {
          DestroyImmediate(dataObj);
        }

        element.OnAssignFeatureData(dataList);
      }

      //Could be more efficient
      foreach (var feature in _features) {
        feature.AssignFeatureReferences();
      }
    }
  }

  private void rebuildFeatureSupportInfo() {
    using (new ProfilerSample("Rebuild Support Info")) {
      var typeToFeatures = new Dictionary<Type, List<LeapGuiFeatureBase>>();
      foreach (var feature in _features) {
        Type featureType = feature.GetType();
        List<LeapGuiFeatureBase> list;
        if (!typeToFeatures.TryGetValue(featureType, out list)) {
          list = new List<LeapGuiFeatureBase>();
          typeToFeatures[featureType] = list;
        }

        list.Add(feature);
      }


      var featureToInfo = new Dictionary<LeapGuiFeatureBase, SupportInfo>();

      foreach (var pair in typeToFeatures) {
        var featureType = pair.Key;
        var featureList = pair.Value;
        var infoList = new List<SupportInfo>().FillEach(featureList.Count, () => SupportInfo.FullSupport());

        var castList = Activator.CreateInstance(typeof(List<>).MakeGenericType(featureType)) as IList;
        foreach (var feature in featureList) {
          castList.Add(feature);
        }

        try {
          if (_renderer == null) continue;

          var interfaceType = typeof(ISupportsFeature<>).MakeGenericType(featureType);
          if (!interfaceType.IsAssignableFrom(_renderer.GetType())) {
            infoList.FillEach(() => SupportInfo.Error("This renderer does not support this feature."));
            continue;
          }

          var supportDelegate = interfaceType.GetMethod("GetSupportInfo");

          if (supportDelegate == null) {
            Debug.LogError("Could not find support delegate.");
            continue;
          }

          supportDelegate.Invoke(_renderer, new object[] { castList, infoList });
        } finally {
          for (int i = 0; i < featureList.Count; i++) {
            featureToInfo[featureList[i]] = infoList[i];
          }
        }
      }

      _supportInfo = new List<SupportInfo>();
      foreach (var feature in _features) {
        _supportInfo.Add(feature.GetSupportInfo(this).OrWorse(featureToInfo[feature]));
      }
    }
  }
  #endregion
}
