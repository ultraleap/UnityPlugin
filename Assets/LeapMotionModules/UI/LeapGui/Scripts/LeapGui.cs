using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

[ExecuteInEditMode]
public class LeapGui : MonoBehaviour {
  public const string FEATURE_PREFIX = "LEAP_GUI_";
  public const string PROPERTY_PREFIX = "_LeapGui";

  public const string FEATURE_MOVEMENT_TRANSLATION = FEATURE_PREFIX + "MOVEMENT_TRANSLATION";
  public const string FEATURE_MOVEMENT_FULL = FEATURE_PREFIX + "MOVEMENT_FULL";

  [SerializeField]
  private List<LeapGuiFeatureBase> _features;

  [SerializeField]
  private LeapGuiRenderer _renderer;

  public List<LeapGuiElement> elements;

  void OnEnable() {
    if (Application.isPlaying) {
      _renderer.OnEnableRenderer();
    }
  }

  void OnDisable() {
    if (Application.isPlaying) {
      _renderer.OnDisableRenderer();
    }
  }

  void LateUpdate() {
    if (_renderer != null) {
      if (Application.isPlaying) {
        _renderer.OnUpdateRenderer();
      } else {
        elements.Clear();
        rebuildElementList(transform);
        rebuildFeatureData();
        _renderer.OnUpdateRendererEditor();
      }
    }
  }

  public LeapGuiRenderer GetRenderer() {
    return _renderer;
  }

#if UNITY_EDITOR
  public void SetRenderer(LeapGuiRenderer renderer) {
    if (Application.isPlaying) {
      throw new InvalidOperationException("Cannot change renderer at runtime.");
    }

    if (_renderer != null) {
      _renderer.OnDisableRendererEditor();
      DestroyImmediate(_renderer);
      _renderer = null;
    }

    _renderer = renderer;
    _renderer.gui = this;

    if (_renderer != null) {
      _renderer.OnEnableRendererEditor();
    }
  }
#endif

  public bool GetAllFeaturesOfType<T>(List<T> features) where T : LeapGuiFeatureBase {
    _features.Query().OfType<T>().FillList(features);
    return features.Count != 0;
  }

  public void rebuildElementList(Transform root) {
    int count = root.childCount;
    for (int i = 0; i < count; i++) {
      Transform child = root.GetChild(i);

      var element = child.GetComponent<LeapGuiElement>();
      if (element != null) {
        elements.Add(element);
      }

      rebuildElementList(child);
    }
  }

  private void rebuildFeatureData() {
    foreach (var feature in _features) {
      feature.ClearDataObjectReferences();
    }

    for (int i = 0; i < elements.Count; i++) {
      var element = elements[i];

      //If data points to a different element, copy it and point it to the correct element
      for (int j = 0; j < element.data.Count; j++) {
        var data = element.data[j];
        if (data.element != element) {
          data = Instantiate(data);
          data.element = element;
          element.data[j] = data;
        }
      }

      //First make a map of existing data objects to their correct indexes
      var dataToNewIndex = new Dictionary<LeapGuiElementData, int>();
      foreach (var data in element.data) {
        if (data == null || data.feature == null) {
          continue;
        }

        dataToNewIndex[data] = _features.IndexOf(data.feature);
      }

      //Then make sure the data array has enough spaces for all the data objects
      element.data.Fill(_features.Count, null);

      //Then re-map the existing data objects to the correct index
      foreach (var pair in dataToNewIndex) {
        element.data[pair.Value] = pair.Key;
      }

      //Then construct new data objects if there is not yet one
      for (int j = 0; j < _features.Count; j++) {
        var feature = _features[j];

        if (element.data[j] == null) {
          element.data[j] = feature.CreateDataObject(element);
        }

        //Add the correct reference into the feature list
        feature.AddDataObjectReference(element.data[j]);
      }
    }
  }
}
