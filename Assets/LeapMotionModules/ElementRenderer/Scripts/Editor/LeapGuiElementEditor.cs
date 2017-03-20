using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiElementData), editorForChildClasses: true, isFallback = true)]
public class DefaultFeatureDataEditor : CustomEditorBase {
  protected override void OnEnable() {
    base.OnEnable();
    dontShowScriptField();
  }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiElement), editorForChildClasses: true, isFallback = true)]
public class LeapGuiElementEditor : LeapGuiElementEditorBase<LeapGuiElement> { }

public abstract class LeapGuiElementEditorBase<T> : CustomEditorBase<T> where T : LeapGuiElement {
  List<LeapGuiElement> elements = new List<LeapGuiElement>();
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
    targets.Query().Where(e => e != null).Select(e => e as LeapGuiElement).FillList(elements);

    LeapGuiGroup mainGroup = null;
    LeapGuiGroup sharedGroup = null;

    if (elements.Query().All(e => e.IsAttachedToGroup)) {
      var mainGui = elements[0].attachedGroup.gui;
      if (elements.Query().All(e => e.attachedGroup.gui == mainGui)) {
        mainGroup = elements[0].attachedGroup;
        if (elements.Query().All(e => e.attachedGroup == mainGroup)) {
          sharedGroup = mainGroup;
        }
      }
    }

    drawScriptAndGroupGui(mainGroup);

    base.OnInspectorGUI();

    drawFeatureData(sharedGroup);
  }

  protected void drawScriptAndGroupGui(LeapGuiGroup mainGroup) {
    using (new GUILayout.HorizontalScope()) {
      drawScriptField();

      if (mainGroup == null) {
        return;
      }

      string buttonText;
      if (!elements.Query().All(e => e.attachedGroup == mainGroup)) {
        buttonText = "-";
      } else {
        buttonText = LeapGuiTagAttribute.GetTag(mainGroup.renderer.GetType());
      }

      if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.Width(60))) {
        GenericMenu groupMenu = new GenericMenu();
        int index = 0;
        foreach (var group in mainGroup.gui.groups.Query().Where(g => g.renderer.IsValidElement(elements[0]))) {
          string tag = LeapGuiTagAttribute.GetTag(group.renderer.GetType());
          groupMenu.AddItem(new GUIContent(index.ToString() + ": " + tag), false, () => {
            foreach (var element in elements) {
              Undo.RecordObject(element, "Change element group");
              EditorUtility.SetDirty(element);

              if (element.attachedGroup.TryRemoveElement(element)) {
                group.TryAddElement(element);
              }
            }

            mainGroup.gui.ScheduleEditorUpdate();
          });
          index++;
        }
        groupMenu.ShowAsContext();
      }
    }
  }

  protected void drawFeatureData(LeapGuiGroup sharedGroup) {
    using (new ProfilerSample("Draw Leap Gui Element Editor")) {
      if (elements.Count == 0) return;
      var mainElement = elements[0];

      if (mainElement.data.Count == 0) {
        return;
      }

      if (tempArray.Length != elements.Count) {
        tempArray = new Object[elements.Count];
      }

      int maxElements = LeapGuiPreferences.elementMax;
      if (elements.Query().Any(e => e.attachedGroup != null && e.attachedGroup.elements.IndexOf(e) >= maxElements)) {
        string noun = elements.Count == 1 ? "This element" : "Some of these elements";
        string guiName = elements.Count == 1 ? "its gui" : "their guis";
        EditorGUILayout.HelpBox(noun + " may not be properly displayed because there are too many elements on " + guiName + ".  " +
                                "Either lower the number of elements or increase the maximum element count by visiting " +
                                "Edit->Preferences.", MessageType.Warning);
      }

      while (editorCache.Count < mainElement.data.Count) {
        editorCache.Add(null);
      }

      EditorGUILayout.Space();

      using (new GUILayout.HorizontalScope()) {
        EditorGUILayout.LabelField("Feature Data: ", EditorStyles.boldLabel);

        if (sharedGroup != null) {
          var bakedRenderer = sharedGroup.renderer as LeapGuiBakedRenderer;
          if (bakedRenderer != null && bakedRenderer.IsAtlasDirty) {
            if (GUILayout.Button("Refresh Atlas", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight))) {
              bakedRenderer.RebuildAtlas(new ProgressBar());
              sharedGroup.gui.ScheduleEditorUpdate();
            }
          }
        }
      }

      for (int i = 0; i < mainElement.data.Count; i++) {
        var mainDataObj = mainElement.data[i];
        var mainDataType = mainDataObj.GetType();
        var typeIndex = mainElement.data.Query().Where(d => d.GetType() == mainDataObj.GetType()).IndexOf(mainDataObj);

        tempList.Clear();
        tempList.Add(mainDataObj);

        elements.Query().
                 Skip(1).
                 Select(e =>
                   e.data.Query().
                          OfType(mainDataType).
                          ElementAtOrDefault(typeIndex)).
                 Where(d => d != null).
                 Cast<Object>().
                 AppendList(tempList);

        if (tempList.Count != elements.Count) {
          //Not all elements had a matching data object, so we don't display
          continue;
        }

        tempList.CopyTo(tempArray);

        Editor editor = editorCache[i];
        CreateCachedEditor(tempArray, null, ref editor);
        editorCache[i] = editor;
        editor.serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField(LeapGuiTagAttribute.GetTag(mainDataType));
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
    return true;
  }

  protected Bounds OnGetFrameBounds() {
    Bounds[] allBounds = targets.Query().
                                 Select(e => e.pickingMesh).
                                 Where(m => m != null).
                                 Select(m => m.bounds).
                                 ToArray();

    Bounds bounds = allBounds[0];
    for (int i = 1; i < allBounds.Length; i++) {
      bounds.Encapsulate(allBounds[i]);
    }
    return bounds;
  }
}
