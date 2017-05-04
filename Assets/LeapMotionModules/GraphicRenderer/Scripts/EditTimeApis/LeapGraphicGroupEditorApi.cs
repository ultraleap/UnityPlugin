using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphicGroup {

#if UNITY_EDITOR
    public readonly EditorApi editor;

    public LeapGraphicGroup(LeapGraphicRenderer renderer, Type renderingMethodType) {
      AssertHelper.AssertEditorOnly();
      Assert.IsNotNull(renderer);
      Assert.IsNotNull(renderingMethodType);
      _renderer = renderer;

      editor = new EditorApi(this);
      editor.ChangeRenderingMethod(renderingMethodType, addFeatures: true);
    }

    public class EditorApi {
      private readonly LeapGraphicGroup _group;

      public EditorApi(LeapGraphicGroup group) {
        _group = group;
      }

      public void OnValidate() {
        if (!Application.isPlaying) {
          _group._addRemoveSupported = true;
          if (_group._renderingMethod.Value != null) {
            _group._addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(_group._renderingMethod.Value.GetType());
          }
        }

        for (int i = _group._features.Count; i-- != 0;) {
          if (_group._features[i] == null) {
            _group._features.RemoveAt(i);
          }
        }

        Assert.IsNotNull(_group._renderingMethod.Value, "Rendering method of a group should never be null.");
      }

      public void OnDestroy() {
        if (_group._renderingMethod.Value != null) {
          _group._renderingMethod.Value.OnDisableRendererEditor();
        }
      }

      public void ChangeRenderingMethod(Type renderingMethodType, bool addFeatures) {
        AssertHelper.AssertEditorOnly();
        Assert.IsNotNull(renderingMethodType);

        if (_group._renderingMethod.Value != null) {
          _group._renderingMethod.Value.OnDisableRendererEditor();
          _group._renderingMethod.Value = null;
        }

        _group._renderingMethod.Value = Activator.CreateInstance(renderingMethodType) as LeapRenderingMethod;
        Assert.IsNotNull(_group._renderingMethod.Value);
        _group._renderingMethod.Value.renderer = _group._renderer;
        _group._renderingMethod.Value.group = _group;

        if (addFeatures) {
          List<Type> dataObjTypes = new List<Type>();
          var allGraphics = _group.renderer.GetComponentsInChildren<LeapGraphic>();
          foreach (var graphic in allGraphics) {
            if (_group._renderingMethod.Value.IsValidGraphic(graphic)) {

              List<Type> types = new List<Type>();
              for (int i = 0; i < graphic.featureData.Count; i++) {
                var dataObj = graphic.featureData[i];
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

        _group._renderingMethod.Value.OnEnableRendererEditor();

        OnValidate();
      }

      public LeapGraphicFeatureBase AddFeature(Type featureType) {
        AssertHelper.AssertEditorOnly();
        _group._renderer.editor.ScheduleEditorUpdate();

        Undo.RecordObject(_group.renderer, "Added feature");

        var feature = Activator.CreateInstance(featureType) as LeapGraphicFeatureBase;
        _group._features.Add(feature);

        _group.RebuildFeatureData();
        _group.RebuildFeatureSupportInfo();

        return feature;
      }

      public void RemoveFeature(int featureIndex) {
        AssertHelper.AssertEditorOnly();

        Undo.RecordObject(_group.renderer, "Removed feature");

        _group._features.RemoveAt(featureIndex);

        _group.RebuildFeatureData();
        _group.RebuildFeatureSupportInfo();

        _group._renderer.editor.ScheduleEditorUpdate();
      }

      public void ValidateGraphicList() {
        //Make sure there are no duplicates, that is not allowed!
        var set = Pool<HashSet<LeapGraphic>>.Spawn();
        try {
          for (int i = _group._graphics.Count; i-- != 0;) {
            var graphic = _group._graphics[i];
            if (set.Contains(graphic)) {
              Debug.LogWarning("Removing duplicate graphic " + graphic);
              _group._graphics.RemoveAt(i);
            } else {
              set.Add(graphic);
            }
          }
        } finally {
          set.Clear();
          Pool<HashSet<LeapGraphic>>.Recycle(set);
        }

        for (int i = _group._graphics.Count; i-- != 0;) {
          if (_group._graphics[i] == null) {
            _group._graphics.RemoveAt(i);
            continue;
          }

          if (!_group._graphics[i].transform.IsChildOf(_group.renderer.transform)) {
            _group.TryRemoveGraphic(_group._graphics[i]);
            continue;
          }
        }
      }


      public void UpdateRendererEditor() {
        AssertHelper.AssertEditorOnly();

        _group._renderingMethod.Value.OnUpdateRendererEditor();
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
