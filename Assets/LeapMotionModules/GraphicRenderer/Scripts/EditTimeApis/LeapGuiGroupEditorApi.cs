using System;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity;

public partial class LeapGraphicGroup : LeapGraphicComponentBase<LeapGraphicRenderer> {

#if UNITY_EDITOR
  public readonly EditorApi editor;

  public class EditorApi {
    private readonly LeapGraphicGroup _group;

    public EditorApi(LeapGraphicGroup group) {
      _group = group;
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

      ChangeRenderingMethod(renderingMethodType);
    }

    public void ChangeRenderingMethod(Type renderingMethodType) {
      AssertHelper.AssertEditorOnly();
      Assert.IsNotNull(renderingMethodType);

      if (_group._renderingMethod != null) {
        _group._renderingMethod.OnDisableRendererEditor();
        InternalUtility.Destroy(_group._renderingMethod);
        _group._renderingMethod = null;
      }

      _group._renderingMethod = _group.gameObject.AddComponent(renderingMethodType) as LeapRenderingMethod;
      Assert.IsNotNull(_group._renderingMethod);
      _group._renderingMethod.renderer = _group._renderer;
      _group._renderingMethod.group = _group;
      _group._renderingMethod.OnEnableRendererEditor();
    }

    public LeapGraphicFeatureBase AddFeature(Type featureType) {
      AssertHelper.AssertEditorOnly();
      _group._renderer.editor.ScheduleEditorUpdate();

      var feature = _group.gameObject.AddComponent(featureType) as LeapGraphicFeatureBase;
      _group._features.Add(feature);

      EditorUtility.SetDirty(_group);

      return feature;
    }

    public void RemoveFeature(LeapGraphicFeatureBase feature) {
      AssertHelper.AssertEditorOnly();
      Assert.IsTrue(_group._features.Contains(feature));

      _group._features.Remove(feature);
      InternalUtility.Destroy(feature);
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
