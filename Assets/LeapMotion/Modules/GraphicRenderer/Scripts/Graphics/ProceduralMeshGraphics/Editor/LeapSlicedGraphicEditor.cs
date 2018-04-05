/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(LeapSlicedGraphic), editorForChildClasses: true)]
  public class LeapSlicedGraphicEditor : LeapGraphicEditorBase<LeapSlicedGraphic> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDrawer("_sourceDataIndex", drawSourceData);

      specifyCustomDrawer("_resolution_verts_per_meter", drawResolution);

      specifyCustomDecorator("_size", decorateSize);
      specifyCustomPostDecorator("_size", postDecorateSize);

      specifyCustomDrawer("_nineSliced", drawSize);
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      serializedObject.ApplyModifiedProperties();
      foreach (var target in targets) {
        if (!target.canNineSlice) {
          target.nineSliced = false;
        }
      }
      serializedObject.Update();
    }

    private void drawSourceData(SerializedProperty property) {
      serializedObject.ApplyModifiedProperties();

      var mainGroup = targets.Query().
                              Select(t => t.attachedGroup).
                              FirstOrDefault(g => g != null);

      //If no element is connected to a gui, we can't draw anything
      if (mainGroup == null) {
        return;
      }

      //If any of the elements are not connected to the same gui, we can't draw anything
      if (targets.Query().Any(p => p.attachedGroup != mainGroup)) {
        return;
      }

      var features = new List<LeapGraphicFeatureBase>();
      foreach (var feature in mainGroup.features) {
        if (feature is LeapTextureFeature || feature is LeapSpriteFeature) {
          features.Add(feature);
        }
      }

      if (features.Count == 1) {
        return;
      }

      int index = -1;
      foreach (var target in targets) {
        //If any of the targets have no source data, you can't use them
        if (target.sourceData == null) {
          return;
        }

        int dataIndex = features.IndexOf(target.sourceData.feature);

        if (index == -1) {
          index = dataIndex;
        }
        else if (index != dataIndex) {
          index = -1;
          break;
        }
      }

      string[] options = features.Query().Select(f => {
        if (f is LeapTextureFeature) {
          return (f as LeapTextureFeature).propertyName + " (Texture)";
        }
        else {
          return (f as LeapSpriteFeature).propertyName + " (Sprite)";
        }
      }).ToArray();

      EditorGUI.BeginChangeCheck();

      if (index == -1) {
        EditorGUI.showMixedValue = true;
      }

      int newIndex = EditorGUILayout.Popup("Data Source", index, options);

      EditorGUI.showMixedValue = false;

      if (EditorGUI.EndChangeCheck()) {
        foreach (var target in targets) {
          List<LeapFeatureData> data = target.featureData.Query().Where(f => f is LeapTextureData || f is LeapSpriteData).ToList();

          Undo.RecordObject(target, "Setting source data");
          target.sourceData = data[newIndex];
        }
      }

      serializedObject.Update();
    }

    private void drawResolution(SerializedProperty property) {
      LeapPanelOutlineGraphic.ResolutionType mainType = targets[0].resolutionType;
      bool allSameType = targets.Query().All(p => p.resolutionType == mainType);

      if (!allSameType) {
        return;
      }

      Rect rect = EditorGUILayout.GetControlRect();

      GUIContent resolutionContent = new GUIContent("Resolution");
      if (mainType == LeapPanelOutlineGraphic.ResolutionType.Vertices) {
        resolutionContent.tooltip = "How many vertices this panel should have in the x and y direction.  These values ignore the edges (0 is a valid resolution).";
      }
      else {
        resolutionContent.tooltip = "How many vertices this panel should spawn relative to the width and height of the panel.  The panel will always have enough vertices to form a quad.";
      }
      EditorGUI.LabelField(rect, resolutionContent);

      rect.x += EditorGUIUtility.labelWidth - 2;
      rect.width -= EditorGUIUtility.labelWidth;
      rect.width *= 2.0f / 3.0f;

      float originalWidth = EditorGUIUtility.labelWidth;
      EditorGUIUtility.labelWidth = 14;

      Rect left = rect;
      left.width /= 2;
      Rect right = left;
      right.x += right.width + 1;

      if (mainType == LeapPanelOutlineGraphic.ResolutionType.Vertices) {
        SerializedProperty x = serializedObject.FindProperty("_resolution_vert_x");
        SerializedProperty y = serializedObject.FindProperty("_resolution_vert_y");

        x.intValue = EditorGUI.IntField(left, "X", x.intValue);
        y.intValue = EditorGUI.IntField(right, "Y", y.intValue);
      }
      else {
        Vector2 value = property.vector2Value;

        value.x = EditorGUI.FloatField(left, "X", value.x);
        value.y = EditorGUI.FloatField(right, "Y", value.y);

        property.vector2Value = value;
      }

      EditorGUIUtility.labelWidth = originalWidth;
    }

    private void decorateSize(SerializedProperty property) {
      EditorGUI.BeginDisabledGroup(targets.Query().Any((t) => t.GetComponent<RectTransform>() != null));
    }

    private void postDecorateSize(SerializedProperty property) {
      EditorGUI.EndDisabledGroup();
    }

    private void drawSize(SerializedProperty property) {
      using (new GUILayout.HorizontalScope()) {
        var canAllNineSlice = targets.Query().All(p => p.canNineSlice);
        using (new EditorGUI.DisabledGroupScope(!canAllNineSlice)) {
          EditorGUILayout.PropertyField(property);
        }

        if (targets.Length == 1) {
          var rectTransform = target.GetComponent<RectTransform>();
          if (rectTransform == null) {
            if (GUILayout.Button("Add Rect Transform", GUILayout.MaxWidth(150))) {
              Vector2 initialSize = target.rect.size;

              rectTransform = target.gameObject.AddComponent<RectTransform>();
              rectTransform.sizeDelta = initialSize;
            }
          }
        }
      }
    }
  }
}
