/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public interface ILeapInternalGraphicGroup {
    LeapGraphicRenderer renderer { set; }
  }

  public partial class LeapGraphicGroup {

#if UNITY_EDITOR
    public readonly EditorApi editor;

    public LeapGraphicGroup(LeapGraphicRenderer renderer, Type renderingMethodType) {
      _groupName = LeapGraphicTagAttribute.GetTagName(renderingMethodType);

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

      /// <summary>
      /// This method changes the rendering method used for this group.  You must provide the type
      /// of the rendering method to switch to, as well as if features should be added to match
      /// the existing feature data objects present in the graphics that are attached.
      ///
      /// This is an editor only method, as rendering types cannot be changed at runtime!
      /// </summary>
      public void ChangeRenderingMethod(Type renderingMethodType, bool addFeatures) {
        AssertHelper.AssertEditorOnly();
        Assert.IsNotNull(renderingMethodType);

        if (_group._renderingMethod.Value != null) {
          _group._renderingMethod.Value.OnDisableRendererEditor();
          _group._renderingMethod.Value = null;
        }

        _group._renderingMethod.Value = Activator.CreateInstance(renderingMethodType) as LeapRenderingMethod;
        Assert.IsNotNull(_group._renderingMethod.Value);

        ILeapInternalRenderingMethod renderingMethodInternal = _group._renderingMethod.Value;
        renderingMethodInternal.renderer = _group._renderer;
        renderingMethodInternal.group = _group;

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

      /// <summary>
      /// Adds a feature of a specific type to this group.  Even if the feature is not
      /// a supported feature it will still be added, it will just not be supported and
      /// will show up in red in the inspector.
      ///
      /// This is an editor only api, as features cannot be added/removed at runtime.
      /// </summary>
      public LeapGraphicFeatureBase AddFeature(Type featureType) {
        AssertHelper.AssertEditorOnly();
        _group._renderer.editor.ScheduleRebuild();

        Undo.RecordObject(_group.renderer, "Added feature");

        var feature = Activator.CreateInstance(featureType) as LeapGraphicFeatureBase;
        _group._features.Add(feature);

        _group.RebuildFeatureData();
        _group.RebuildFeatureSupportInfo();

        return feature;
      }

      /// <summary>
      /// Removes the feature at the given index.  This is an editor only api, as features
      /// cannot be added/removed at runtime.
      /// </summary>
      public void RemoveFeature(int featureIndex) {
        AssertHelper.AssertEditorOnly();

        Undo.RecordObject(_group.renderer, "Removed feature");

        _group._features.RemoveAt(featureIndex);

        _group.RebuildFeatureData();
        _group.RebuildFeatureSupportInfo();

        _group._renderer.editor.ScheduleRebuild();
      }

      /// <summary>
      /// Forces a rebuild of all the picking meshes for all attached graphics.
      /// The picking meshes are used to allow graphics to be accurately picked
      /// even though they might be inside of warped spaces.
      /// </summary>
      public void RebuildEditorPickingMeshes() {
        using (new ProfilerSample("Rebuild Picking Meshes")) {
          foreach (var graphic in _group._graphics) {
            graphic.editor.RebuildEditorPickingMesh();
          }
        }
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
          if (_group._graphics[i] == null || _group.graphics[i].attachedGroup != _group) {
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
    }
#endif
  }
}
