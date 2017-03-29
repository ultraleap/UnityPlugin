using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity;
using Leap.Unity.Space;

public partial class LeapGui : MonoBehaviour {

  //Need to keep this outside of the #if guards or else Unity will throw a fit
  //about the serialized format changing between editor and build
  [SerializeField]
  private int _selectedGroup = 0;

#if UNITY_EDITOR
  public readonly EditorApi editor;

  public class EditorApi {
    private LeapGui _gui;

    [NonSerialized]
    private Hash _previousHierarchyHash;

    private DelayedAction _delayedHeavyRebuild;

    public EditorApi(LeapGui gui) {
      _gui = gui;

      _delayedHeavyRebuild = new DelayedAction(() => DoEditorUpdateLogic(fullRebuild: true, heavyRebuild: true));
      InternalUtility.OnAnySave += onAnySave;
    }

    public void OnValidate() {
      //TODO, handle drag-drop of leap gui for groups!
    }

    public void OnDestroy() {
      InternalUtility.OnAnySave -= onAnySave;
      _delayedHeavyRebuild.Dispose();
    }

    public void CreateGroup(Type rendererType) {
      AssertHelper.AssertEditorOnly();
      Assert.IsNotNull(rendererType);

      var group = _gui.gameObject.AddComponent<LeapGuiGroup>();
      group.editor.Init(_gui, rendererType);

      _gui._selectedGroup = _gui._groups.Count;
      _gui._groups.Add(group);
    }

    public void DestroySelectedGroup() {
      AssertHelper.AssertEditorOnly();

      var toDestroy = _gui._groups[_gui._selectedGroup];
      _gui._groups.RemoveAt(_gui._selectedGroup);

      if (_gui._selectedGroup >= _gui._groups.Count && _gui._selectedGroup != 0) {
        _gui._selectedGroup--;
      }

      InternalUtility.Destroy(toDestroy);
    }

    public void ScheduleEditorUpdate() {
      AssertHelper.AssertEditorOnly();

      //Dirty the hash by changing it to something else
      _previousHierarchyHash++;
    }

    public void RebuildEditorPickingMeshes() {
      if (_gui._space != null) {
        _gui._space.RebuildHierarchy();
        _gui._space.RecalculateTransformers();
      }

      _gui.validateElements();

      foreach (var group in _gui._groups) {
        group.editor.ValidateElementList();
        group.RebuildFeatureData();
        group.RebuildFeatureSupportInfo();
        group.editor.RebuildEditorPickingMeshes();
      }
    }

    public void DoLateUpdateEditor() {
      validateSpaceComponent();

      bool needsRebuild = false;

      using (new ProfilerSample("Calculate Should Rebuild")) {
        foreach (var group in _gui._groups) {
          foreach (var feature in group.features) {
            if (feature.isDirty) {
              needsRebuild = true;
              break;
            }
          }
        }

        Hash hierarchyHash = Hash.GetHierarchyHash(_gui.transform);

        if (_gui._space != null) {
          hierarchyHash.Add(_gui._space.GetSettingHash());
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
        if (_gui._space != null) {
          _gui._space.RebuildHierarchy();
          _gui._space.RecalculateTransformers();
        }

        _gui.validateElements();

        foreach (var group in _gui._groups) {
          group.editor.ValidateElementList();
          group.RebuildFeatureData();
          group.RebuildFeatureSupportInfo();
          group.editor.UpdateRendererEditor(heavyRebuild);
        }

        _gui._hasFinishedSetup = true;
      }

      foreach (var group in _gui._groups) {
        group.UpdateRenderer();
      }
    }

    private void onAnySave() {
      DoEditorUpdateLogic(fullRebuild: true, heavyRebuild: false);
    }

    private void validateSpaceComponent() {
      if (_gui._space != null && !_gui._space.enabled) {
        _gui._space = null;
      }

      if (_gui._space == null) {
        var potentialSpace = _gui.GetComponent<LeapSpace>();
        if (potentialSpace != null && potentialSpace.enabled) {
          _gui._space = potentialSpace;
        }
      }
    }
  }
#endif
}
