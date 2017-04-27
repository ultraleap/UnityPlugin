using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphicRenderer : MonoBehaviour {

    //Need to keep this outside of the #if guards or else Unity will throw a fit
    //about the serialized format changing between editor and build
    [SerializeField]
#pragma warning disable 0414
    private int _selectedGroup = 0;
#pragma warning restore 0414

#if UNITY_EDITOR
    public readonly EditorApi editor;

    public class EditorApi {
      private LeapGraphicRenderer _renderer;

      [NonSerialized]
      private Hash _previousHierarchyHash;

      private DelayedAction _delayedHeavyRebuild;

      public EditorApi(LeapGraphicRenderer renderer) {
        _renderer = renderer;

        _delayedHeavyRebuild = new DelayedAction(() => {
          if (!InternalUtility.IsPrefab(_renderer)) {
            DoEditorUpdateLogic(fullRebuild: true, heavyRebuild: true);
          }
        });
        InternalUtility.OnAnySave += onAnySave;
      }

      public void OnValidate() {
        Assert.IsFalse(InternalUtility.IsPrefab(_renderer), "Should never run editor validation on a prefab");

        for (int i = _renderer._groups.Count; i-- > 0;) {
          if (_renderer._groups[i] == null) {
            _renderer._groups.RemoveAt(i);
          }
        }

        validateSpaceComponent();

        AttachedObjectHandler.Validate(_renderer, _renderer._groups);

        foreach (var group in _renderer._groups) {
          group.editor.OnValidate();
        }

        UnityEditor.EditorApplication.delayCall += () => {
          if (_renderer == null) {
            return;
          }

          //Destroy any features that are not referenced by a group
          var referenced = Pool<HashSet<LeapGraphicFeatureBase>>.Spawn();
          var attached = Pool<List<LeapGraphicFeatureBase>>.Spawn();
          try {
            foreach (var group in _renderer._groups) {
              referenced.UnionWith(group.features);
            }

            _renderer.GetComponents(attached);
            for (int i = attached.Count; i-- != 0;) {
              if (!referenced.Contains(attached[i])) {
                InternalUtility.Destroy(attached[i]);
              }
            }
          } finally {
            referenced.Clear();
            attached.Clear();
            Pool<HashSet<LeapGraphicFeatureBase>>.Recycle(referenced);
            Pool<List<LeapGraphicFeatureBase>>.Recycle(attached);
          }
        };
      }

      public void OnDestroy() {
        InternalUtility.OnAnySave -= onAnySave;
        _delayedHeavyRebuild.Dispose();
      }

      public void CreateGroup(Type rendererType) {
        AssertHelper.AssertEditorOnly();
        Assert.IsNotNull(rendererType);

        var group = InternalUtility.AddComponent<LeapGraphicGroup>(_renderer.gameObject);
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
        //No picking meshes for prefabs
        if (InternalUtility.IsPrefab(_renderer)) {
          return;
        }

        if (!Application.isPlaying) {
          if (_renderer._space != null) {
            _renderer._space.RebuildHierarchy();
            _renderer._space.RecalculateTransformers();
          }

          validateGraphics();

          foreach (var group in _renderer._groups) {
            group.editor.ValidateGraphicList();
            group.RebuildFeatureData();
            group.RebuildFeatureSupportInfo();
            group.editor.RebuildEditorPickingMeshes();
          }
        }

        foreach (var group in _renderer._groups) {
          group.editor.RebuildEditorPickingMeshes();
        }
      }

      public void DoLateUpdateEditor() {
        Assert.IsFalse(InternalUtility.IsPrefab(_renderer), "Should never do editor updates for prefabs");

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
        Assert.IsFalse(InternalUtility.IsPrefab(_renderer), "Should never do editor updates for prefabs");

        using (new ProfilerSample("Validate Space Component")) {
          validateSpaceComponent();
        }

        if (fullRebuild) {
          if (_renderer._space != null) {
            using (new ProfilerSample("Rebuild Space")) {
              _renderer._space.RebuildHierarchy();
              _renderer._space.RecalculateTransformers();
            }
          }

          using (new ProfilerSample("Validate graphics")) {
            validateGraphics();
          }

          foreach (var group in _renderer._groups) {
            using (new ProfilerSample("Validate Graphic List")) {
              group.editor.ValidateGraphicList();
            }

            using (new ProfilerSample("Rebuild Feature Data")) {
              group.RebuildFeatureData();
            }

            using (new ProfilerSample("Rebuild Feature Support Info")) {
              group.RebuildFeatureSupportInfo();
            }

            using (new ProfilerSample("Update Renderer Editor")) {
              group.editor.UpdateRendererEditor(heavyRebuild);
            }
          }

          _renderer._hasFinishedSetup = true;
        }

        using (new ProfilerSample("Update Renderer")) {
          foreach (var group in _renderer._groups) {
            group.UpdateRenderer();
          }
        }
      }

      private void onAnySave() {
        if (_renderer == null || InternalUtility.IsPrefab(_renderer)) {
          InternalUtility.OnAnySave -= onAnySave;
          return;
        }

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

      [NonSerialized]
      private List<LeapGraphic> _tempGraphicList = new List<LeapGraphic>();
      private void validateGraphics() {
        Assert.IsFalse(InternalUtility.IsPrefab(_renderer), "Should never validate graphics for prefabs");

        Undo.RecordObject(_renderer, "Validated renderer data");
        _renderer.GetComponentsInChildren(includeInactive: true, result: _tempGraphicList);

        HashSet<LeapGraphic> set = Pool<HashSet<LeapGraphic>>.Spawn();
        foreach (var group in _renderer._groups) {
          Undo.RecordObject(group, "Modified group fields");

          for (int i = group.graphics.Count; i-- != 0;) {
            if (group.graphics[i] == null) {
              group.graphics.RemoveAt(i);
            } else {
              set.Add(group.graphics[i]);
            }
          }

          foreach (var graphic in _tempGraphicList) {
            if (graphic.isAttachedToGroup) {
              //If the graphic claims it is attached to this group, but it really isn't, remove
              //it and re-add it.
              bool graphicThinksItsInGroup = graphic.attachedGroup == group;
              bool isActuallyInGroup = set.Contains(graphic);

              //Also re add it if it is attached to a completely different renderer!
              if (graphicThinksItsInGroup != isActuallyInGroup ||
                  graphic.attachedGroup.renderer != _renderer) {
                group.TryRemoveGraphic(graphic);
                group.TryAddGraphic(graphic);
              }
            }
          }

          set.Clear();
        }
        Pool<HashSet<LeapGraphic>>.Recycle(set);

        foreach (var graphic in _tempGraphicList) {
          if (graphic.isAttachedToGroup) {
            //procede to validate

            //If the graphic is anchored to the wrong anchor, detach and reattach
            var anchor = _renderer._space == null ? null : LeapSpaceAnchor.GetAnchor(graphic.transform);
            if (graphic.anchor != anchor) {
              var group = graphic.attachedGroup;

              if (group.TryRemoveGraphic(graphic)) {
                group.TryAddGraphic(graphic);
              }
            }

            //Debug.Log(graphic.gameObject.activeInHierarchy + " : " + graphic.gameObject.activeSelf);
            if (!graphic.enabled || !graphic.gameObject.activeInHierarchy) {
              graphic.attachedGroup.TryRemoveGraphic(graphic);
            }
          }

          if (!graphic.isAttachedToGroup && graphic.enabled && graphic.gameObject.activeInHierarchy) {
            _renderer.TryAddGraphic(graphic);
          }
        }
      }
    }
#endif
  }
}
