using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapFeatureData), editorForChildClasses: true, isFallback = true)]
public class DefaultFeatureDataEditor : CustomEditorBase {
  protected override void OnEnable() {
    base.OnEnable();
    dontShowScriptField();
  }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGraphic), editorForChildClasses: true, isFallback = true)]
public class LeapGraphicEditor : LeapGraphicEditorBase<LeapGraphic> { }

public abstract class LeapGraphicEditorBase<T> : CustomEditorBase<T> where T : LeapGraphic {
  List<Editor> editorCache = new List<Editor>();

  Object[] tempArray = new Object[0];
  List<Object> tempList = new List<Object>();

  protected override void OnEnable() {
    base.OnEnable();

    dontShowScriptField();
  }

  protected void OnDisable() {
    foreach (var editor in editorCache) {
      DestroyImmediate(editor);
    }
    editorCache.Clear();
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

      if (mainGroup == null) {
        return;
      }

      string buttonText;
      if (!targets.Query().All(g => g.attachedGroup == mainGroup)) {
        buttonText = "-";
      } else {
        buttonText = LeapGraphicTagAttribute.GetTag(mainGroup.renderingMethod.GetType());
      }

      if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.Width(60))) {
        GenericMenu groupMenu = new GenericMenu();
        int index = 0;
        foreach (var group in mainGroup.renderer.groups.Query().Where(g => g.renderingMethod.IsValidGraphic(targets[0]))) {
          string tag = LeapGraphicTagAttribute.GetTag(group.renderingMethod.GetType());
          groupMenu.AddItem(new GUIContent(index.ToString() + ": " + tag), false, () => {
            foreach (var graphic in targets) {
              Undo.RecordObject(graphic, "Change graphic group");
              EditorUtility.SetDirty(graphic);

              if (graphic.attachedGroup.TryRemoveGraphic(graphic)) {
                group.TryAddGraphic(graphic);
              }
            }

            mainGroup.renderer.editor.ScheduleEditorUpdate();
          });
          index++;
        }
        groupMenu.ShowAsContext();
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

      if (tempArray.Length != targets.Length) {
        tempArray = new Object[targets.Length];
      }

      int maxGraphics = LeapGraphicPreferences.graphicMax;
      if (targets.Query().Any(e => e.attachedGroup != null && e.attachedGroup.graphics.IndexOf(e) >= maxGraphics)) {
        string noun = targets.Length == 1 ? "This graphic" : "Some of these graphics";
        string rendererName = targets.Length == 1 ? "its renderer" : "their renderers";
        EditorGUILayout.HelpBox(noun + " may not be properly displayed because there are too many graphics on " + rendererName + ".  " +
                                "Either lower the number of graphics or increase the maximum graphic count by visiting " +
                                "Edit->Preferences.", MessageType.Warning);
      }

      while (editorCache.Count < mainGraphic.featureData.Count) {
        editorCache.Add(null);
      }

      EditorGUILayout.Space();

      using (new GUILayout.HorizontalScope()) {
        EditorGUILayout.LabelField("Feature Data: ", EditorStyles.boldLabel);

        if (sharedGroup != null) {
          var bakedRendering = sharedGroup.renderingMethod as LeapBakedRenderer;
          if (bakedRendering != null && bakedRendering.IsAtlasDirty) {
            if (GUILayout.Button("Refresh Atlas", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight))) {
              bakedRendering.RebuildAtlas(new ProgressBar());
              sharedGroup.renderer.editor.ScheduleEditorUpdate();
            }
          }
        }
      }

      for (int i = 0; i < mainGraphic.featureData.Count; i++) {
        var mainDataObj = mainGraphic.featureData[i];
        var mainDataType = mainDataObj.GetType();
        var typeIndex = mainGraphic.featureData.Query().Where(d => d.GetType() == mainDataObj.GetType()).IndexOf(mainDataObj);

        tempList.Clear();
        tempList.Add(mainDataObj);

        targets.Query().
                Skip(1).
                Select(e => e.featureData.Query().
                                          OfType(mainDataType).
                                          ElementAtOrDefault(typeIndex)).
                NonNull().
                Cast<Object>().
                AppendList(tempList);

        if (tempList.Count != targets.Length) {
          //Not all graphics had a matching data object, so we don't display
          continue;
        }

        tempList.CopyTo(tempArray);

        Editor editor = editorCache[i];
        CreateCachedEditor(tempArray, null, ref editor);
        editorCache[i] = editor;
        editor.serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField(LeapGraphicTagAttribute.GetTag(mainDataType));
        EditorGUI.indentLevel++;

        editor.OnInspectorGUI();

        EditorGUI.indentLevel--;
        if (EditorGUI.EndChangeCheck()) {
          editor.serializedObject.ApplyModifiedProperties();
        }
      }
    }
  }

  protected bool HasFrameBounds() {
    return targets.Query().
                   Any(t => t.editor.pickingMesh != null);
  }

  protected Bounds OnGetFrameBounds() {
    return targets.Query().
                   Select(e => e.editor.pickingMesh).
                   NonNull().
                   Select(m => m.bounds).
                   Fold((a, b) => {
                     a.Encapsulate(b);
                     return a;
                   });
  }
}
