using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphicGroup : LeapGraphicComponentBase<LeapGraphicRenderer> {

#if UNITY_EDITOR
    public readonly EditorApi editor;

    public class EditorApi {
      private readonly LeapGraphicGroup _group;

      public EditorApi(LeapGraphicGroup group) {
        _group = group;
      }

      public void OnValidate() {
        if (_group._renderer == null) {
          _group._renderer = _group.GetComponent<LeapGraphicRenderer>();
        }

        if (!Application.isPlaying) {
          _group._addRemoveSupported = true;
          if (_group._renderingMethod != null) {
            _group._addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(_group._renderingMethod.GetType());
          }
          if (_group._renderer.space != null) {
            _group._addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(_group._renderer.space.GetType());
          }
        }

        for (int i = _group._features.Count; i-- != 0;) {
          if (_group._features[i] == null) {
            _group._features.RemoveAt(i);
          }
        }

        AttachedObjectHandler.Validate(_group, ref _group._renderingMethod);
        AttachedObjectHandler.Validate(_group, _group._features);

        if (_group._renderingMethod != null) {
          _group._renderingMethod.renderer = _group._renderer;
          _group._renderingMethod.group = _group;
        }
      }

      public void OnDestroyedByUser() {
        if (_group._renderingMethod != null) {
          _group._renderingMethod.OnDisableRendererEditor();
          InternalUtility.Destroy(_group._renderingMethod);
        }

        foreach (var feature in _group._features) {
          InternalUtility.Destroy(feature);
        }
      }

      public void Init(LeapGraphicRenderer renderer, Type renderingMethodType) {
        AssertHelper.AssertEditorOnly();
        Assert.IsNotNull(renderer);
        Assert.IsNotNull(renderingMethodType);
        _group._renderer = renderer;

        ChangeRenderingMethod(renderingMethodType, addFeatures: true);
      }

      public void ChangeRenderingMethod(Type renderingMethodType, bool addFeatures) {
        AssertHelper.AssertEditorOnly();
        Assert.IsNotNull(renderingMethodType);

        if (_group._renderingMethod != null) {
          _group._renderingMethod.OnDisableRendererEditor();
          InternalUtility.Destroy(_group._renderingMethod);
          _group._renderingMethod = null;
        }

        _group._renderingMethod = InternalUtility.AddComponent(_group.gameObject, renderingMethodType) as LeapRenderingMethod;
        Assert.IsNotNull(_group._renderingMethod);
        _group._renderingMethod.renderer = _group._renderer;
        _group._renderingMethod.group = _group;

        if (addFeatures) {
          List<Type> dataObjTypes = new List<Type>();
          var allGraphics = _group.GetComponentsInChildren<LeapGraphic>();
          foreach (var graphic in allGraphics) {
            if (_group._renderingMethod.IsValidGraphic(graphic)) {

              List<Type> types = new List<Type>();
              foreach (var dataObj in graphic.featureData) {
                var dataType = dataObj.GetType();
                if (!dataObjTypes.Contains(dataType)) {
                  types.Add(dataType);
                }
              }

              foreach (var type in types) {
                if (dataObjTypes.Query().Count(t => t == type) < types.Query().Count(t => t == type)) {
                  dataObjTypes.Add(type);
                }
              }
            }
          }

          foreach (var type in dataObjTypes) {
            var featureType = LeapFeatureData.GetFeatureType(type);
            if (featureType != null) {
              AddFeature(featureType);
            }
          }
        }

        _group._renderingMethod.OnEnableRendererEditor();
      }

      public LeapGraphicFeatureBase AddFeature(Type featureType) {
        AssertHelper.AssertEditorOnly();
        _group._renderer.editor.ScheduleEditorUpdate();

        Undo.RecordObject(_group, "Added feature");

        var feature = InternalUtility.AddComponent(_group.gameObject, featureType) as LeapGraphicFeatureBase;
        _group._features.Add(feature);

        _group.RebuildFeatureData();
        _group.RebuildFeatureSupportInfo();

        return feature;
      }

      public void RemoveFeature(LeapGraphicFeatureBase feature) {
        AssertHelper.AssertEditorOnly();
        Assert.IsTrue(_group._features.Contains(feature));

        Undo.RecordObject(_group, "Removed feature " + feature.name);

        _group._features.Remove(feature);
        InternalUtility.Destroy(feature);

        _group.RebuildFeatureData();
        _group.RebuildFeatureSupportInfo();

        _group._renderer.editor.ScheduleEditorUpdate();
      }

      public void ValidateGraphicList() {
        for (int i = _group._graphics.Count; i-- != 0;) {
          if (_group._graphics[i] == null) {
            _group._graphics.RemoveAt(i);
            continue;
          }

          if (!_group._graphics[i].transform.IsChildOf(_group.transform)) {
            _group.TryRemoveGraphic(_group._graphics[i]);
            continue;
          }
        }
      }


      public void UpdateRendererEditor(bool heavyRebuild) {
        AssertHelper.AssertEditorOnly();

        _group._renderingMethod.OnUpdateRendererEditor(heavyRebuild);
      }

      public void RebuildEditorPickingMeshes() {
        using (new ProfilerSample("Rebuild Picking Meshes")) {
          foreach (var graphic in _group._graphics) {
            graphic.editor.RebuildEditorPickingMesh();
          }
        }
      }
    }
#endif
  }
}
