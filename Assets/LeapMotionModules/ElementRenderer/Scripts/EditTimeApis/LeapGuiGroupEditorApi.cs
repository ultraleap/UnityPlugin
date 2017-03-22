using System;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity;

public partial class LeapGuiGroup : LeapGuiComponentBase<LeapGui> {

#if UNITY_EDITOR
  public readonly EditorApi editor;

  public class EditorApi {
    private readonly LeapGuiGroup _group;

    public EditorApi(LeapGuiGroup group) {
      _group = group;
    }

    public void OnDestroyedByUser() {
      if (_group._renderer != null) {
        _group._renderer.OnDisableRendererEditor();
        InternalUtility.Destroy(_group._renderer);
      }

      foreach (var feature in _group._features) {
        InternalUtility.Destroy(feature);
      }
    }

    public void Init(LeapGui gui, Type rendererType) {
      AssertHelper.AssertEditorOnly();
      Assert.IsNotNull(gui);
      Assert.IsNotNull(rendererType);
      _group._gui = gui;

      ChangeRenderer(rendererType);
    }

    public void ChangeRenderer(Type rendererType) {
      AssertHelper.AssertEditorOnly();
      Assert.IsNotNull(rendererType);

      if (_group._renderer != null) {
        _group._renderer.OnDisableRendererEditor();
        InternalUtility.Destroy(_group._renderer);
        _group._renderer = null;
      }

      _group._renderer = _group.gameObject.AddComponent(rendererType) as LeapGuiRendererBase;
      Assert.IsNotNull(_group._renderer);
      _group._renderer.gui = _group._gui;
      _group._renderer.group = _group;
      _group._renderer.OnEnableRendererEditor();
    }

    public LeapGuiFeatureBase AddFeature(Type featureType) {
      AssertHelper.AssertEditorOnly();
      _group._gui.editor.ScheduleEditorUpdate();

      var feature = _group.gameObject.AddComponent(featureType) as LeapGuiFeatureBase;
      _group._features.Add(feature);

      EditorUtility.SetDirty(_group);

      return feature;
    }

    public void RemoveFeature(LeapGuiFeatureBase feature) {
      AssertHelper.AssertEditorOnly();
      Assert.IsTrue(_group._features.Contains(feature));

      _group._features.Remove(feature);
      InternalUtility.Destroy(feature);
      _group._gui.editor.ScheduleEditorUpdate();
    }

    public void ValidateElementList() {
      for (int i = _group._elements.Count; i-- != 0;) {
        if (_group._elements[i] == null) {
          _group._elements.RemoveAt(i);
          continue;
        }

        if (!_group._elements[i].transform.IsChildOf(_group.transform)) {
          _group.TryRemoveElement(_group._elements[i]);
          continue;
        }
      }
    }


    public void UpdateRendererEditor(bool heavyRebuild) {
      AssertHelper.AssertEditorOnly();

      _group._renderer.OnUpdateRendererEditor(heavyRebuild);
    }

    public void RebuildEditorPickingMeshes() {
      if (_group.gui.space == null) {
        return;
      }

      using (new ProfilerSample("Rebuild Picking Meshes")) {
        foreach (var element in _group._elements) {
          element.RebuildEditorPickingMesh();
        }
      }
    }
  }
#endif
}
