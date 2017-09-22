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
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.GraphicalRenderer;

/// <summary>
/// Currently Unity has a strange bug where custom editor objects are not
/// properly cleaned up if they are inside a namespace -___- 
/// so currently these editors are going to be in the empty namespace.
/// </summary>

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGraphic), editorForChildClasses: true, isFallback = true)]
public class LeapGraphicEditor : LeapGraphicEditorBase<LeapGraphic> { }

public abstract class LeapGraphicEditorBase<T> : CustomEditorBase<T> where T : LeapGraphic {
  //Used ONLY for feature data drawers
  public static LeapGraphicFeatureBase currentFeature { get; private set; }

  private SerializedProperty _featureList;
  private SerializedProperty _featureTable;

  protected override void OnEnable() {
    base.OnEnable();

    hideField("_anchor");
    hideField("_featureData");
    hideField("_attachedRenderer");
    hideField("_attachedGroupIndex");
    hideField("_preferredRendererType");
    hideField("_favoriteGroupName");

    _featureList = serializedObject.FindProperty("_featureData");
    _featureTable = MultiTypedListUtil.GetTableProperty(_featureList);

    dontShowScriptField();
  }

  public override void OnInspectorGUI() {
    LeapGraphicGroup mainGroup = null;
    LeapGraphicGroup sharedGroup = null;

    if (targets.Query().All(g => g.isAttachedToGroup)) {
      var mainRenderer = targets[0].attachedGroup.renderer;
      if (targets.Query().All(g => g.attachedGroup.renderer == mainRenderer)) {
        mainGroup = targets[0].attachedGroup;
        if (targets.Query().All(g => g.attachedGroup == mainGroup)) {
          sharedGroup = mainGroup;
        }
      }
    }

    drawScriptAndGroupGui(mainGroup);

    base.OnInspectorGUI();

    drawFeatureData(sharedGroup);
  }

  protected void drawScriptAndGroupGui(LeapGraphicGroup mainGroup) {
    using (new GUILayout.HorizontalScope()) {
      drawScriptField();

      Color originalColor = GUI.color;
      try {
        string buttonText;
        if (!targets.Query().All(g => g.attachedGroup == mainGroup)) {
          buttonText = "-";
        } else if (mainGroup == null) {
          buttonText = "None";
          GUI.color = Color.yellow;
        } else {
          buttonText = mainGroup.name;
        }

        var renderer = targets.Query().
                               Select(t => t.GetComponentInParent<LeapGraphicRenderer>()).
                               UniformOrDefault();

        if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.Width(60))) {
          GenericMenu groupMenu = new GenericMenu();
          groupMenu.AddItem(new GUIContent("None"), false, () => {
            foreach (var graphic in targets) {
              serializedObject.ApplyModifiedProperties();
              graphic.TryDetach();

              EditorUtility.SetDirty(graphic);

              serializedObject.SetIsDifferentCacheDirty();
              serializedObject.Update();
            }
          });

          if (renderer != null) {
            int index = 0;
            foreach (var group in renderer.groups.Query().Where(g => g.renderingMethod.IsValidGraphic(targets[0]))) {
              groupMenu.AddItem(new GUIContent(index.ToString() + ": " + group.name), false, () => {

                bool areFeaturesUnequal = false;
                var typesA = group.features.Query().Select(f => f.GetType()).ToList();
                foreach (var graphic in targets) {
                  if (!graphic.isAttachedToGroup) {
                    continue;
                  }

                  var typesB = graphic.attachedGroup.features.Query().Select(f => f.GetType()).ToList();
                  if (!Utils.AreEqualUnordered(typesA, typesB)) {
                    areFeaturesUnequal = true;
                    break;
                  }
                }

                if (areFeaturesUnequal && LeapGraphicPreferences.promptWhenGroupChange) {
                  if (!EditorUtility.DisplayDialog("Features Are Different!",
                                                   "The group you are moving to has a different feature set than the current group, " +
                                                   "this can result in data loss!  Are you sure you want to change group?",
                                                   "Continue",
                                                   "Cancel")) {
                    return;
                  }
                }

                foreach (var graphic in targets) {
                  serializedObject.ApplyModifiedProperties();

                  if (graphic.isAttachedToGroup) {
                    if (graphic.TryDetach()) {
                      group.TryAddGraphic(graphic);
                    }
                  } else {
                    group.TryAddGraphic(graphic);
                  }

                  EditorUtility.SetDirty(graphic);
                  EditorUtility.SetDirty(renderer);

                  serializedObject.SetIsDifferentCacheDirty();
                  serializedObject.Update();
                }

                renderer.editor.ScheduleRebuild();
              });
              index++;
            }
          }

          groupMenu.ShowAsContext();
        }
      } finally {
        GUI.color = originalColor;
      }
    }
  }

  protected void drawFeatureData(LeapGraphicGroup sharedGroup) {
    using (new ProfilerSample("Draw Leap Gui Graphic Editor")) {
      if (targets.Length == 0) return;
      var mainGraphic = targets[0];

      if (mainGraphic.featureData.Count == 0) {
        return;
      }

      if (mainGraphic.attachedGroup != null) {
        SpriteAtlasUtil.ShowInvalidSpriteWarning(mainGraphic.attachedGroup.features);
      }

      int maxGraphics = LeapGraphicPreferences.graphicMax;
      if (targets.Query().Any(e => e.attachedGroup != null && e.attachedGroup.graphics.IndexOf(e) >= maxGraphics)) {
        string noun = targets.Length == 1 ? "This graphic" : "Some of these graphics";
        string rendererName = targets.Length == 1 ? "its renderer" : "their renderers";
        EditorGUILayout.HelpBox(noun + " may not be properly displayed because there are too many graphics on " + rendererName + ".  " +
                                "Either lower the number of graphics or increase the maximum graphic count by visiting " +
                                "Edit->Preferences.", MessageType.Warning);
      }

      //If we are not all attached to the same group we cannot show features
      if (!targets.Query().Select(g => g.attachedGroup).AllEqual()) {
        return;
      }

      EditorGUILayout.Space();

      using (new GUILayout.HorizontalScope()) {
        EditorGUILayout.LabelField("Feature Data: ", EditorStyles.boldLabel);

        if (sharedGroup != null) {
          var meshRendering = sharedGroup.renderingMethod as LeapMesherBase;
          if (meshRendering != null && meshRendering.IsAtlasDirty && !EditorApplication.isPlaying) {
            if (GUILayout.Button("Refresh Atlas", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight))) {
              meshRendering.RebuildAtlas(new ProgressBar());
              sharedGroup.renderer.editor.ScheduleRebuild();
            }
          }
        }
      }

      for (int i = 0; i < _featureTable.arraySize; i++) {
        var idIndex = _featureTable.GetArrayElementAtIndex(i);
        var dataProp = MultiTypedListUtil.GetReferenceProperty(_featureList, idIndex);
        EditorGUILayout.LabelField(LeapGraphicTagAttribute.GetTagName(dataProp.type));

        if (mainGraphic.attachedGroup != null) {
          currentFeature = mainGraphic.attachedGroup.features[i];
        }

        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(dataProp, includeChildren: true);

        EditorGUI.indentLevel--;

        currentFeature = null;
      }

      serializedObject.ApplyModifiedProperties();
    }
  }

  protected bool HasFrameBounds() {
    return targets.Query().
                   Any(t => t.editor.pickingMesh != null);
  }

  protected Bounds OnGetFrameBounds() {
    return targets.Query().
                   Select(e => e.editor.pickingMesh).
                   ValidUnityObjs().
                   Select(m => m.bounds).
                   Fold((a, b) => {
                     a.Encapsulate(b);
                     return a;
                   });
  }
}

