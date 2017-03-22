using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity;
using Leap.Unity.Query;

[AddComponentMenu("")]
public class LeapGuiGroup : LeapGuiComponentBase<LeapGui> {

  #region INSPECTOR FIELDS
  [SerializeField]
  private LeapGuiRendererBase _renderer;

  [SerializeField]
  private List<LeapGuiFeatureBase> _features = new List<LeapGuiFeatureBase>();
  #endregion

  #region PRIVATE VARIABLES
  [SerializeField, HideInInspector]
  private LeapGui _gui;

  [SerializeField, HideInInspector]
  private List<LeapGuiElement> _elements = new List<LeapGuiElement>();

  [SerializeField, HideInInspector]
  private List<SupportInfo> _supportInfo = new List<SupportInfo>();

  [SerializeField, HideInInspector]
  private bool _addRemoveSupported;
  #endregion

  #region PUBLIC RUNTIME API

  public LeapGui gui {
    get {
      return _gui;
    }
  }

#if UNITY_EDITOR
  public new LeapGuiRendererBase renderer {
#else
  public LeapGuiRendererBase renderer {
#endif
    get {
      return _renderer;
    }
  }

  public List<LeapGuiFeatureBase> features {
    get {
      return _features;
    }
  }

  public List<LeapGuiElement> elements {
    get {
      return _elements;
    }
  }

  /// <summary>
  /// Maps 1-to-1 with the feature list, where each element represents the
  /// support that feature currently has.
  /// </summary>
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

  public bool TryAddElement(LeapGuiElement element) {
    Assert.IsNotNull(element);

    if (!addRemoveSupportedOrEditTime()) {
      return false;
    }

    if (_elements.Contains(element)) {
      return false;
    }

    _elements.Add(element);

    Transform anchor = AnchorOfConstantSize.GetParentAnchorOrGui(element.transform);
    Assert.IsNotNull(anchor);

    element.OnAttachedToGui(this, anchor);

    //TODO: this is gonna need to be optimized
    RebuildFeatureData();
    RebuildFeatureSupportInfo();

#if UNITY_EDITOR
    if (!Application.isPlaying) {
      _gui.ScheduleEditorUpdate();
    }

    if (_renderer is ISupportsAddRemove) {
      (_renderer as ISupportsAddRemove).OnAddElement();
    }
#else
    (_renderer as ISupportsAddRemove).OnAddElement();
#endif

    return true;
  }

  public bool TryRemoveElement(LeapGuiElement element) {
    Assert.IsNotNull(element);

    if (!addRemoveSupportedOrEditTime()) {
      return false;
    }

    if (!_elements.Contains(element)) {
      return false;
    }

    element.OnDetachedFromGui();
    _elements.Remove(element);

    //TODO: this is gonna need to be optimized
    RebuildFeatureData();
    RebuildFeatureSupportInfo();

#if UNITY_EDITOR
    if (!Application.isPlaying) {
      _gui.ScheduleEditorUpdate();
    }

    if (_renderer is ISupportsAddRemove) {
      (_renderer as ISupportsAddRemove).OnRemoveElement();
    }
#else
    (_renderer as ISupportsAddRemove).OnRemoveElement();
#endif

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

  public void UpdateRenderer() {
    _renderer.OnUpdateRenderer();

    foreach (var feature in _features) {
      feature.isDirty = false;
    }
  }

  #endregion

  #region PUBLIC EDITOR API
  public void RebuildFeatureData() {
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

  public void RebuildFeatureSupportInfo() {
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

#if UNITY_EDITOR
  public void Init(LeapGui gui, Type rendererType) {
    AssertHelper.AssertEditorOnly();
    Assert.IsNotNull(gui);
    Assert.IsNotNull(rendererType);
    _gui = gui;

    ChangeRenderer(rendererType);
  }

  public void ChangeRenderer(Type rendererType) {
    AssertHelper.AssertEditorOnly();
    Assert.IsNotNull(rendererType);

    if (_renderer != null) {
      _renderer.OnDisableRendererEditor();
      InternalUtility.Destroy(_renderer);
      _renderer = null;
    }

    _renderer = gameObject.AddComponent(rendererType) as LeapGuiRendererBase;
    Assert.IsNotNull(_renderer);
    _renderer.gui = _gui;
    _renderer.group = this;
    _renderer.OnEnableRendererEditor();
  }

  public LeapGuiFeatureBase AddFeature(Type featureType) {
    AssertHelper.AssertEditorOnly();
    _gui.ScheduleEditorUpdate();

    var feature = gameObject.AddComponent(featureType) as LeapGuiFeatureBase;
    _features.Add(feature);

    EditorUtility.SetDirty(this);
    _gui.ScheduleEditorUpdate();

    return feature;
  }

  public void RemoveFeature(LeapGuiFeatureBase feature) {
    AssertHelper.AssertEditorOnly();
    Assert.IsTrue(_features.Contains(feature));

    _features.Remove(feature);
    InternalUtility.Destroy(feature);
    _gui.ScheduleEditorUpdate();
  }

  public void ValidateElementList() {
    for (int i = _elements.Count; i-- != 0;) {
      if (_elements[i] == null) {
        _elements.RemoveAt(i);
        continue;
      }

      if (!_elements[i].transform.IsChildOf(transform)) {
        TryRemoveElement(_elements[i]);
        continue;
      }
    }
  }

  public void UpdateRendererEditor(bool heavyRebuild) {
    AssertHelper.AssertEditorOnly();

    _renderer.OnUpdateRendererEditor(heavyRebuild);
  }

  public void RebuildEditorPickingMeshes() {
    if (gui.space == null) {
      return;
    }

    using (new ProfilerSample("Rebuild Picking Meshes")) {
      foreach (var element in _elements) {
        element.RebuildEditorPickingMesh();
      }
    }
  }
#endif
  #endregion

  #region UNITY CALLBACKS

  protected override void OnValidate() {
    base.OnValidate();

    if (_gui == null) {
      _gui = GetComponent<LeapGui>();
    }

    if (!Application.isPlaying) {
      _addRemoveSupported = true;
      if (_renderer != null) {
        _addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(renderer.GetType());
      }
      if (_gui.space != null) {
        _addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(_gui.space.GetType());
      }
    }

    for (int i = _features.Count; i-- != 0;) {
      if (_features[i] == null) {
        _features.RemoveAt(i);
      }
    }

    if (_renderer != null) {
      _renderer.gui = _gui;
      _renderer.group = this;
    }
  }

#if UNITY_EDITOR
  protected override void OnDestroyedByUser() {
    base.OnDestroyedByUser();

    if (_renderer != null) {
      _renderer.OnDisableRendererEditor();
      InternalUtility.Destroy(_renderer);
    }

    foreach (var feature in _features) {
      InternalUtility.Destroy(feature);
    }
  }
#endif

  private void OnEnable() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      return;
    }
#endif

    _renderer.OnEnableRenderer();
  }

  private void OnDisable() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      return;
    }
#endif

    _renderer.OnDisableRenderer();
  }

  #endregion

  #region PRIVATE IMPLEMENTATION

  private bool addRemoveSupportedOrEditTime() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      return true;
    }
#endif
    return _addRemoveSupported;
  }
  #endregion
}
