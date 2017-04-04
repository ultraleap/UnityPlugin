using System;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphicRenderer : MonoBehaviour {

    //Need to keep this outside of the #if guards or else Unity will throw a fit
    //about the serialized format changing between editor and build
    [SerializeField]
    private int _selectedGroup = 0;

#if UNITY_EDITOR
    public readonly EditorApi editor;

    public class EditorApi {
      private LeapGraphicRenderer _renderer;

      [NonSerialized]
      private Hash _previousHierarchyHash;

      private DelayedAction _delayedHeavyRebuild;

      public EditorApi(LeapGraphicRenderer renderer) {
        _renderer = renderer;

        _delayedHeavyRebuild = new DelayedAction(() => DoEditorUpdateLogic(fullRebuild: true, heavyRebuild: true));
        InternalUtility.OnAnySave += onAnySave;
      }

      public void OnValidate() {
        //TODO, handle drag-drop for groups!
      }

      public void OnDestroy() {
        InternalUtility.OnAnySave -= onAnySave;
        _delayedHeavyRebuild.Dispose();
      }

      public void CreateGroup(Type rendererType) {
        AssertHelper.AssertEditorOnly();
        Assert.IsNotNull(rendererType);

        var group = _renderer.gameObject.AddComponent<LeapGraphicGroup>();
        group.editor.Init(_renderer, rendererType);

        _renderer._selectedGroup = _renderer._groups.Count;
        _renderer._groups.Add(group);
      }

      public void DestroySelectedGroup() {
        AssertHelper.AssertEditorOnly();

        var toDestroy = _renderer._groups[_renderer._selectedGroup];
        _renderer._groups.RemoveAt(_renderer._selectedGroup);

        if (_renderer._selectedGroup >= _renderer._groups.Count && _renderer._selectedGroup != 0) {
          _renderer._selectedGroup--;
        }

        InternalUtility.Destroy(toDestroy);
      }

      public void ScheduleEditorUpdate() {
        AssertHelper.AssertEditorOnly();

        //Dirty the hash by changing it to something else
        _previousHierarchyHash++;
      }

      public void RebuildEditorPickingMeshes() {
        if (_renderer._space != null) {
          _renderer._space.RebuildHierarchy();
          _renderer._space.RecalculateTransformers();
        }

        _renderer.validateGraphics();

        foreach (var group in _renderer._groups) {
          group.editor.ValidateGraphicList();
          group.RebuildFeatureData();
          group.RebuildFeatureSupportInfo();
          group.editor.RebuildEditorPickingMeshes();
        }
      }

      public void DoLateUpdateEditor() {
        validateSpaceComponent();

        bool needsRebuild = false;

        using (new ProfilerSample("Calculate Should Rebuild")) {
          foreach (var group in _renderer._groups) {
            foreach (var feature in group.features) {
              if (feature.isDirty) {
                needsRebuild = true;
                break;
              }
            }
          }

          Hash hierarchyHash = Hash.GetHierarchyHash(_renderer.transform);

          if (_renderer._space != null) {
            hierarchyHash.Add(_renderer._space.GetSettingHash());
          }

          if (_previousHierarchyHash != hierarchyHash) {
            _previousHierarchyHash = hierarchyHash;
            needsRebuild = true;
          }
        }

        if (needsRebuild) {
          _delayedHeavyRebuild.Reset();
        }

        DoEditorUpdateLogic(needsRebuild, heavyRebuild: false);
      }

      public void DoEditorUpdateLogic(bool fullRebuild, bool heavyRebuild) {
        validateSpaceComponent();

        if (fullRebuild) {
          if (_renderer._space != null) {
            _renderer._space.RebuildHierarchy();
            _renderer._space.RecalculateTransformers();
          }

          _renderer.validateGraphics();

          foreach (var group in _renderer._groups) {
            group.editor.ValidateGraphicList();
            group.RebuildFeatureData();
            group.RebuildFeatureSupportInfo();
            group.editor.UpdateRendererEditor(heavyRebuild);
          }

          _renderer._hasFinishedSetup = true;
        }

        foreach (var group in _renderer._groups) {
          group.UpdateRenderer();
        }
      }

      private void onAnySave() {
        DoEditorUpdateLogic(fullRebuild: true, heavyRebuild: false);
      }

      private void validateSpaceComponent() {
        if (_renderer._space != null && !_renderer._space.enabled) {
          _renderer._space = null;
        }

        if (_renderer._space == null) {
          var potentialSpace = _renderer.GetComponent<LeapSpace>();
          if (potentialSpace != null && potentialSpace.enabled) {
            _renderer._space = potentialSpace;
          }
        }
      }
    }
#endif
  }
}
