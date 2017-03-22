using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity;

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
    private int _previousHierarchyHash;

    private DelayedAction _delayedHeavyRebuild;

    public EditorApi(LeapGui gui) {
      _gui = gui;

      _delayedHeavyRebuild = new DelayedAction(() => DoEditorUpdateLogic(fullRebuild: true, heavyRebuild: true));
    }

    public void OnValidate() {
      //TODO, handle drag-drop of leap gui for groups!

      if (_gui._space != null && _gui._space.gameObject != _gui.gameObject) {
        LeapGuiSpace movedSpace;
        if (InternalUtility.TryMoveComponent(_gui._space, _gui.gameObject, out movedSpace)) {
          _gui._space = movedSpace;
        } else {
          Debug.LogWarning("Could not move space component " + _gui._space + "!");
          InternalUtility.Destroy(_gui._space);
        }
      }
    }

    public void OnDestroy() {
      _delayedHeavyRebuild.Dispose();
    }

    public void CreateGroup(Type rendererType) {
      AssertHelper.AssertEditorOnly();
      Assert.IsNotNull(rendererType);

      var group = _gui.gameObject.AddComponent<LeapGuiGroup>();
      group.Init(_gui, rendererType);

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

    public void SetSpace(Type spaceType) {
      AssertHelper.AssertEditorOnly();
      ScheduleEditorUpdate();

      UnityEditor.Undo.RecordObject(_gui, "Change Gui Space");
      UnityEditor.EditorUtility.SetDirty(_gui);

      if (_gui._space != null) {
        DestroyImmediate(_gui._space);
        _gui._space = null;
      }

      _gui._space = _gui.gameObject.AddComponent(spaceType) as LeapGuiSpace;

      if (_gui._space != null) {
        _gui._space.gui = _gui;
      }
    }

    public void RebuildEditorPickingMeshes() {
      if (_gui._space == null) {
        return;
      }

      foreach (var group in _gui._groups) {
        group.RebuildEditorPickingMeshes();
      }
    }

    public void DoLateUpdateEditor() {
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

        int hierarchyHash = HashUtil.GetHierarchyHash(_gui.transform);
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
      if (fullRebuild) {
        _gui._anchors.Clear();
        _gui._anchorParents.Clear();
        _gui.rebuildAnchorInfo(_gui.transform, _gui.transform);
        _gui._space.BuildElementData(_gui.transform);
        _gui.collectUnattachedElements();

        foreach (var group in _gui._groups) {
          group.ValidateElementList();
          group.RebuildFeatureData();
          group.RebuildFeatureSupportInfo();
          group.UpdateRendererEditor(heavyRebuild);
        }

        _gui._hasFinishedSetup = true;
      }

      foreach (var group in _gui._groups) {
        group.UpdateRenderer();
      }
    }
  }
#endif
}
